#!/usr/bin/env python3
"""
FarmsteadVR sound generator.

Every sound here is built from math: sine waves, random noise and simple filters.
There are no recordings and no pieces of anyone else's sounds, so the results belong
to this project and there are no copyright questions.

Run from the project folder:
    python3 Tools/SoundGen/generate_sounds.py

It writes .wav files into Assets/Resources/Sounds/. Unity picks them up automatically.
To replace a sound with a better one later, put a new file with the same name there.
Only the Python standard library is used (no installs needed).
"""
import array
import math
import os
import random
import wave

RATE = 44100
TAU = 2 * math.pi
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "Resources", "Sounds"))


# ---------- small building blocks ----------

def normalize(samples, peak=1.0):
    top = max((abs(s) for s in samples), default=0.0) or 1.0
    return [s * peak / top for s in samples]


def save(name, samples, peak=0.89, loop=False):
    samples = normalize(samples, peak)
    # tiny fades so a sound never starts or stops with a click
    # (not for loops: they are made to join seamlessly, a fade would make a small gap)
    fade = 0 if loop else min(len(samples) // 4, int(0.004 * RATE))
    for i in range(fade):
        samples[i] *= i / fade
        samples[-1 - i] *= i / fade
    data = array.array("h", (int(max(-1.0, min(1.0, s)) * 32767) for s in samples))
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data.tobytes())
    print("  %-18s %.2fs" % (name + ".wav", len(samples) / RATE))


def mix_into(target, source, start_seconds, gain=1.0):
    start = int(start_seconds * RATE)
    if start + len(source) > len(target):
        target.extend([0.0] * (start + len(source) - len(target)))
    for i, s in enumerate(source):
        target[start + i] += s * gain
    return target


def tone(freq_at, seconds, harmonics=((1, 1.0),), env=None):
    """A note. freq_at: number or function of time. harmonics: (multiple, loudness)."""
    n = int(seconds * RATE)
    out = [0.0] * n
    phase = 0.0
    for i in range(n):
        t = i / RATE
        f = freq_at(t) if callable(freq_at) else freq_at
        phase += TAU * f / RATE
        s = 0.0
        for mult, amp in harmonics:
            if f * mult < RATE / 2:
                s += amp * math.sin(phase * mult)
        out[i] = s * (env(t) if env else 1.0)
    return out


def hit_env(decay, attack=0.004):
    """Fast start, then fades away (like hitting or plucking something)."""
    return lambda t: min(1.0, t / attack) * math.exp(-t * decay)


def hump_env(seconds, power=1.0):
    """Swells up and back down (like a breath of air)."""
    return lambda t: max(0.0, math.sin(math.pi * t / seconds)) ** power


def noise(seconds, seed):
    rnd = random.Random(seed)
    return [rnd.uniform(-1.0, 1.0) for _ in range(int(seconds * RATE))]


def lowpass(samples, cutoff_at):
    """Keeps the low (soft) part of a sound. cutoff can change over time."""
    out = []
    y = 0.0
    a = 0.0
    for i, x in enumerate(samples):
        if i % 32 == 0:
            c = cutoff_at(i / RATE) if callable(cutoff_at) else cutoff_at
            a = 1.0 - math.exp(-TAU * c / RATE)
        y += a * (x - y)
        out.append(y)
    return out


def bandpass(samples, center_at, q=1.0):
    """Keeps only a band of frequencies around 'center' (the classic RBJ band-pass filter)."""
    out = []
    x1 = x2 = y1 = y2 = 0.0
    b0 = b2 = a1 = a2 = 0.0
    for i, x in enumerate(samples):
        if i % 32 == 0:
            f = center_at(i / RATE) if callable(center_at) else center_at
            w = TAU * f / RATE
            alpha = math.sin(w) / (2 * q)
            a0 = 1 + alpha
            b0, b2 = alpha / a0, -alpha / a0
            a1, a2 = -2 * math.cos(w) / a0, (1 - alpha) / a0
        y = b0 * x + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1 = x1, x
        y2, y1 = y1, y
        out.append(y)
    return out


# ---------- voices: a buzzing source shaped by vowel "formants" ----------
# Every vowel has a few frequency areas that sound louder (formants). Making the harmonics
# of a buzz louder near those areas makes it sound like "a", "e", "o"... That's the whole trick.

VOWELS = {
    "a": ((800, 110), (1200, 120), (2500, 160)),
    "e": ((500, 90), (1800, 120), (2500, 160)),
    "i": ((300, 80), (2300, 130), (3000, 160)),
    "o": ((500, 90), (900, 100), (2400, 160)),
    "u": ((320, 80), (800, 100), (2300, 160)),
    "y": ((350, 80), (1600, 120), (2400, 160)),
}
FORMANT_LOUDNESS = (1.0, 0.6, 0.25)


def vowel_gain(freq, vowel):
    g = 0.0
    for (center, width), loud in zip(VOWELS[vowel], FORMANT_LOUDNESS):
        g += loud / (1.0 + ((freq - center) / width) ** 2)
    return g


def voiced(seconds, f0_at, vowel_at, amp_at, max_freq=5000.0):
    n = int(seconds * RATE)
    out = [0.0] * n
    phases = [0.0] * 64
    gains = [0.0] * 64
    count = 0
    for i in range(n):
        t = i / RATE
        f0 = f0_at(t)
        if i % 64 == 0:
            vowel = vowel_at(t)
            count = min(63, int(max_freq / f0))
            for h in range(1, count + 1):
                gains[h] = vowel_gain(h * f0, vowel)
        s = 0.0
        step = TAU * f0 / RATE
        for h in range(1, count + 1):
            phases[h] += step * h
            s += gains[h] * math.sin(phases[h])
        out[i] = s * amp_at(t)
    return out


def syllable(seed, base_pitch, vowels, wobble, length=(0.08, 0.12)):
    """One cartoon 'word' sound: optional tiny consonant click + a short vowel."""
    rnd = random.Random(seed)
    vowel = rnd.choice(vowels)
    dur = rnd.uniform(*length)
    pitch = base_pitch * rnd.uniform(0.93, 1.08)
    slope = rnd.uniform(-0.18, 0.14)

    def f0(t):
        return pitch * (1 + slope * t / dur) * (1 + wobble * 0.035 * math.sin(TAU * 16 * t))

    def amp(t):
        shape = min(1.0, t / 0.008) * max(0.0, min(1.0, (dur - t) / 0.03))
        return shape * (1 - wobble * 0.25 * (0.5 + 0.5 * math.sin(TAU * 16 * t)))

    out = []
    if rnd.random() < 0.55:  # a "t"/"k"/"p"-like click in front
        click = bandpass(noise(0.012, seed + 1000), rnd.choice([1800, 2600, 3600]), q=2.0)
        click = normalize([c * (1 - i / len(click)) for i, c in enumerate(click)], 0.35)
        out = mix_into(out, click, 0.0)
    return mix_into(out, normalize(voiced(dur, f0, lambda t: vowel, amp)), 0.008)


# ---------- the sounds ----------

def ambient_wind(seconds=24.0, cross=2.0):
    """Soft wind that loops forever without a seam."""
    rnd = random.Random(3)
    p = [rnd.uniform(0, TAU) for _ in range(4)]
    raw = noise(seconds + cross, seed=7)

    def cutoff(t):
        return 420 + 260 * math.sin(TAU * 0.07 * t + p[0]) + 140 * math.sin(TAU * 0.19 * t + p[1])

    soft = lowpass(lowpass(raw, cutoff), cutoff)
    soft = [s * (0.65 + 0.25 * math.sin(TAU * 0.05 * (i / RATE) + p[2])
                 + 0.10 * math.sin(TAU * 0.23 * (i / RATE) + p[3])) for i, s in enumerate(soft)]
    n, c = int(seconds * RATE), int(cross * RATE)
    loop = soft[:n]
    for i in range(c):  # blend the end into the beginning
        w = i / c
        loop[i] = soft[i] * w + soft[n + i] * (1 - w)
    return loop


def chirp(f_from, f_to, seconds, curve=1.0, vibrato=0.0):
    def f(t):
        x = (t / seconds) ** curve
        return (f_from + (f_to - f_from) * x) * (1 + vibrato * math.sin(TAU * 35 * t))
    return tone(f, seconds, ((1, 1.0), (2, 0.12)), hump_env(seconds, 0.7))


def bird(variant):
    out = []
    if variant == 1:    # four rising chirps
        for k in range(4):
            mix_into(out, chirp(2800 + 80 * k, 4300 - 60 * k, 0.07, 0.8), k * 0.125)
    elif variant == 2:  # quick trill
        for k in range(12):
            mix_into(out, chirp(3600 if k % 2 else 4100, 3800, 0.028), k * 0.04)
    elif variant == 3:  # "tee-oo" whistle
        mix_into(out, chirp(3300, 3350, 0.18, vibrato=0.01), 0.0)
        mix_into(out, chirp(3300, 2200, 0.25, 1.5), 0.2)
    elif variant == 4:  # warble
        out = tone(lambda t: 3000 + 450 * math.sin(TAU * 28 * t), 0.45, ((1, 1.0), (2, 0.1)), hump_env(0.45, 0.6))
    else:               # three falling chirps
        for k in range(3):
            mix_into(out, chirp(4500, 3000, 0.09, 0.6), k * 0.11)
    return out


def whoosh():
    dur = 0.45
    air = bandpass(noise(dur, seed=11), lambda t: 350 + 2600 * (t / dur) ** 1.4, q=1.3)
    air = [s * hump_env(dur, 1.5)(i / RATE) for i, s in enumerate(air)]
    sparkle = tone(lambda t: 500 + 900 * t / dur, dur, ((1, 1.0), (2, 0.3)), hump_env(dur, 2.0))
    return mix_into(normalize(air), normalize(sparkle), 0.0, 0.18)


def knock(base, seed):
    """Hammer on wood: a few 'wooden' (not perfectly musical) tones that die quickly + a click."""
    dur = 0.22
    out = [0.0] * int(dur * RATE)
    for ratio, amp, decay in ((1.0, 1.0, 38), (2.32, 0.55, 55), (4.16, 0.3, 80), (6.3, 0.15, 110)):
        mix_into(out, tone(base * ratio, dur, ((1, amp),), hit_env(decay, 0.001)), 0.0)
    click = bandpass(noise(0.012, seed), 2200, q=1.5)
    click = [c * math.exp(-i / RATE * 400) for i, c in enumerate(click)]
    return mix_into(normalize(out), normalize(click), 0.0, 0.6)


def pop():
    return tone(lambda t: 900 * math.exp(-t * 18) + 250, 0.12, ((1, 1.0), (2, 0.2)), hit_env(30, 0.002))


def coin():
    bell = ((1, 1.0), (3, 0.25), (5, 0.1))
    out = tone(988, 0.09, bell, hit_env(25))
    return mix_into(out, tone(1319, 0.45, bell, hit_env(7)), 0.08)


def pickup():
    out = []
    for k, f in enumerate((523.25, 659.25, 783.99, 1046.5)):
        mix_into(out, tone(f, 0.25, ((1, 1.0), (3, 0.11), (5, 0.04)), hit_env(12)), k * 0.06)
    return out


def success():
    brass = ((1, 1.0), (2, 0.3), (3, 0.15), (4, 0.07))
    out = []
    for k, f in enumerate((392.0, 523.25, 659.25)):
        mix_into(out, tone(f, 0.16, brass, lambda t: min(1, t / 0.01) * max(0, 1 - t / 0.16)), k * 0.14)
    last = tone(lambda t: 783.99 * (1 + 0.01 * math.sin(TAU * 6 * t)), 0.65, brass,
                lambda t: min(1, t / 0.01) * max(0, 1 - t / 0.65) ** 0.7)
    return mix_into(out, last, 0.42)


def tick():
    out = tone(2000, 0.03, ((1, 1.0),), hit_env(160, 0.001))
    return mix_into(out, tone(4000, 0.03, ((1, 1.0),), hit_env(200, 0.001)), 0.0, 0.3)


def nope():
    soft = ((1, 1.0), (3, 0.2))
    out = tone(440, 0.1, soft, hit_env(18))
    return mix_into(out, tone(330, 0.16, soft, hit_env(14)), 0.11)


def rooster_crow():
    """'Ku-ka-re-ku!' for Zhora."""
    out = []
    parts = (("u", 600, 0.12, 0.0), ("a", 700, 0.12, 0.14), ("i", 820, 0.15, 0.28), ("u", 760, 0.55, 0.45))
    for vowel, pitch, dur, start in parts:
        def f0(t, pitch=pitch, dur=dur):
            return pitch * (1 - 0.12 * (t / dur) ** 2) * (1 + 0.015 * math.sin(TAU * 9 * t))

        def amp(t, dur=dur):
            return min(1.0, t / 0.01) * max(0.0, min(1.0, (dur - t) / 0.05))

        mix_into(out, normalize(voiced(dur, f0, lambda t, v=vowel: v, amp)), start)
    breath = bandpass(noise(1.0, seed=21), 2800, q=0.8)
    return mix_into(out, normalize([b * (0.5 + 0.5 * math.sin(TAU * 7 * i / RATE)) for i, b in enumerate(breath)]), 0.0, 0.08)


def bus_engine(seconds=3.0, cross=0.5):
    """Low diesel rumble that loops: 38 Hz fits exactly 114 times into 3 s, the 'chug' 12 times."""
    hum = tone(38.0, seconds, ((1, 1.0), (2, 0.6), (3, 0.45), (4, 0.3), (6, 0.2), (8, 0.12)),
               lambda t: 0.85 + 0.15 * math.sin(TAU * 4 * t))
    raw = lowpass(noise(seconds + cross, seed=41), 500)
    n, c = int(seconds * RATE), int(cross * RATE)
    rumble = raw[:n]
    for i in range(c):  # blend the end of the noise into its beginning
        w = i / c
        rumble[i] = raw[i] * w + raw[n + i] * (1 - w)
    return mix_into(normalize(hum), normalize(rumble), 0.0, 0.35)


def bus_horn():
    """'Beep-beep!' - two notes played together, twice."""
    horn = ((1, 1.0), (3, 0.3), (5, 0.15))
    out = []
    for start in (0.0, 0.38):
        note = tone(330, 0.28, horn, lambda t: min(1, t / 0.01) * max(0, min(1, (0.28 - t) / 0.03)))
        mix_into(out, note, start)
        mix_into(out, tone(415, 0.28, horn, lambda t: min(1, t / 0.01) * max(0, min(1, (0.28 - t) / 0.03))), start, 0.8)
    return out


def page_turn():
    """A paper 'swish' and a soft thump when the page lands."""
    dur = 0.3
    swish = bandpass(noise(dur, seed=51), lambda t: 1500 + 2500 * t / dur, q=0.9)
    swish = [s * hump_env(dur, 1.2)(i / RATE) for i, s in enumerate(swish)]
    thump = tone(120, 0.06, ((1, 1.0),), hit_env(60, 0.002))
    return mix_into(normalize(swish), normalize(thump), 0.26, 0.4)


def door_hiss():
    """Bus door: an air 'pshhh' that fades, and a soft clunk at the end."""
    dur = 0.7
    air = bandpass(noise(dur, seed=61), 3200, q=0.7)
    air = [s * min(1.0, i / RATE / 0.02) * math.exp(-(i / RATE) * 4.5) for i, s in enumerate(air)]
    clunk = tone(90, 0.08, ((1, 1.0), (2, 0.4)), hit_env(45, 0.002))
    return mix_into(normalize(air), normalize(clunk), 0.6, 0.5)


def dig():
    """A shovel going into soil: a scrape, then a soft heavy 'thud' of earth."""
    dur = 0.55
    scrape = bandpass(noise(0.22, seed=71), lambda t: 900 - 1800 * t, q=1.2)
    scrape = [s * min(1.0, i / RATE / 0.01) * math.exp(-(i / RATE) * 9) for i, s in enumerate(scrape)]
    earth = lowpass(noise(dur, seed=72), 380)
    earth = [s * hump_env(dur, 2.5)(i / RATE) for i, s in enumerate(earth)]
    out = mix_into(normalize(scrape), normalize(earth), 0.12, 0.9)
    return mix_into(out, tone(70, 0.12, ((1, 1.0), (2, 0.3)), hit_env(30, 0.004)), 0.14, 0.5)


def rustle():
    """Leaves and grass: many tiny crackles of high noise in a few waves."""
    dur = 0.7
    out = [0.0] * int(dur * RATE)
    rnd = random.Random(81)
    for k in range(60):
        start = rnd.uniform(0.0, dur - 0.05)
        wave_shape = 0.4 + 0.6 * abs(math.sin(math.pi * 3 * start / dur))
        grain = bandpass(noise(0.03, seed=800 + k), rnd.uniform(2500, 6000), q=1.5)
        grain = [g * math.exp(-i / RATE * 140) for i, g in enumerate(grain)]
        mix_into(out, grain, start, wave_shape * rnd.uniform(0.3, 1.0))
    return out


def water_pour():
    """Water from a watering can: a soft hiss with little bubbling notes."""
    dur = 1.3
    hiss = bandpass(noise(dur, seed=91), 2200, q=0.6)
    env = lambda t: min(1.0, t / 0.08) * max(0.0, min(1.0, (dur - t) / 0.3))
    out = normalize([s * env(i / RATE) for i, s in enumerate(hiss)], 0.5)
    rnd = random.Random(92)
    for k in range(26):  # bubbles: tiny notes that slide up
        start = rnd.uniform(0.05, dur - 0.12)
        f = rnd.uniform(500, 1300)
        blip = tone(lambda t, f=f: f * (1 + 2.5 * t), 0.06, ((1, 1.0),), hit_env(60, 0.003))
        mix_into(out, blip, start, rnd.uniform(0.15, 0.4) * env(start))
    return out


def grain_pour():
    """Grain poured into the feeder: a quick shower of tiny 'tik' sounds."""
    dur = 0.9
    out = [0.0] * int(dur * RATE)
    rnd = random.Random(101)
    for k in range(240):
        start = rnd.uniform(0.0, dur - 0.02) ** 1.3
        loud = math.sin(math.pi * min(1.0, start / dur)) ** 0.6
        click = bandpass(noise(0.008, seed=1000 + k), rnd.uniform(3000, 7000), q=2.0)
        click = [c * math.exp(-i / RATE * 600) for i, c in enumerate(click)]
        mix_into(out, click, start, loud * rnd.uniform(0.2, 1.0))
    return out


def crunch():
    """Boris munching a carrot: three crunchy bites."""
    out = []
    for k, start in enumerate((0.0, 0.32, 0.6)):
        bite = bandpass(noise(0.16, seed=111 + k), 1500, q=0.7)
        bite = [b * min(1.0, i / RATE / 0.003) * math.exp(-(i / RATE) * 22) for i, b in enumerate(bite)]
        crack = bandpass(noise(0.04, seed=121 + k), 4200, q=1.2)
        crack = [c * math.exp(-i / RATE * 90) for i, c in enumerate(crack)]
        mix_into(out, normalize(bite), start, 1.0 - 0.2 * k)
        mix_into(out, normalize(crack), start, 0.45)
    return out


def creak():
    """Big old barn doors: a slow wooden creak that wobbles up and down, with a dry rub."""
    dur = 1.9
    def pitch(t):
        return 170 + 90 * math.sin(TAU * 0.6 * t) + 40 * math.sin(TAU * 2.3 * t)
    groan = tone(pitch, dur, ((1, 1.0), (2, 0.7), (3, 0.5), (4, 0.35), (5, 0.25), (7, 0.15)),
                 lambda t: min(1.0, t / 0.1) * max(0.0, min(1.0, (dur - t) / 0.3)) * (0.6 + 0.4 * abs(math.sin(TAU * 7 * t))))
    rub = bandpass(noise(dur, seed=131), 900, q=0.9)
    rub = [r * 0.5 * (0.5 + 0.5 * math.sin(TAU * 11 * i / RATE)) for i, r in enumerate(rub)]
    return mix_into(normalize(groan), normalize(rub), 0.0, 0.35)


def main():
    os.makedirs(OUT, exist_ok=True)
    print("Writing sounds to", OUT)
    save("ambient_wind", ambient_wind(), peak=0.8, loop=True)
    for v in range(1, 6):
        save("bird_%d" % v, bird(v))
    save("whoosh", whoosh())
    save("knock_1", knock(200, 31))
    save("knock_2", knock(240, 32))
    save("pop", pop())
    save("coin", coin())
    save("pickup", pickup())
    save("success", success())
    save("tick", tick())
    save("nope", nope())
    save("bus_engine", bus_engine(), peak=0.8, loop=True)
    save("bus_horn", bus_horn())
    save("page_turn", page_turn())
    save("door_hiss", door_hiss())
    # Boris's voice (goat_bleat, goat_voice_*) was removed: it didn't sound good enough.
    save("rooster_crow", rooster_crow())
    for k in range(1, 9):  # Zhora: high, bright, fast
        save("rooster_voice_%d" % k, syllable(200 + k, 430, "aei", wobble=0.0, length=(0.06, 0.09)))
    save_chore_sounds()
    print("Done.")


def save_chore_sounds():
    """Farm chores: digging, weeding, watering, feeding the hens, Boris eating a carrot."""
    save("dig", dig())
    save("rustle", rustle())
    save("water_pour", water_pour())
    save("grain_pour", grain_pour())
    save("crunch", crunch())
    save("creak", creak())


if __name__ == "__main__":
    main()
