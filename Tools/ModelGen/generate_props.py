#!/usr/bin/env python3
"""
FarmsteadVR prop generator: small 3D models (.obj) and the burlap texture (.png), built from math.

Like the sound generator, nothing here is copied from anywhere: every shape is a curve turned
around an axis ("lathe", like on a potter's wheel) or a tube bent along a path. So the models
belong to this project and there are no copyright questions.

Run from the project folder:
    python3 Tools/ModelGen/generate_props.py

It writes into Assets/Resources/FarmProps/. Unity imports the files by itself.
To replace a model with a nicer one later, put a new .obj with the same name there.
Only the Python standard library is used.

Sizes are in meters (1 unit = 1 m), like everything in the game.
"""
import math
import os
import random
import struct
import zlib

TAU = 2 * math.pi
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "Resources", "FarmProps"))


# ---------- tiny vector math ----------

def add(a, b): return (a[0] + b[0], a[1] + b[1], a[2] + b[2])
def sub(a, b): return (a[0] - b[0], a[1] - b[1], a[2] - b[2])
def mul(a, k): return (a[0] * k, a[1] * k, a[2] * k)
def dot(a, b): return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]
def cross(a, b): return (a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0])
def length(a): return math.sqrt(dot(a, a))


def norm(a):
    l = length(a)
    return (a[0] / l, a[1] / l, a[2] / l) if l > 1e-12 else (0.0, 1.0, 0.0)


def lerp(a, b, t): return a + (b - a) * t


# ---------- a mesh that knows which side is "outside" ----------

class Mesh:
    def __init__(self, name):
        self.name = name
        self.v, self.t, self.group, self.f = [], [], [], []

    def vertex(self, p, uv, group):
        self.v.append(p)
        self.t.append(uv)
        self.group.append(group)
        return len(self.v) - 1

    def tri(self, a, b, c, inside):
        """Adds a triangle facing away from 'inside' (a point inside the solid).
        Faces are counter-clockwise seen from outside, the usual .obj rule."""
        pa, pb, pc = self.v[a], self.v[b], self.v[c]
        n = cross(sub(pb, pa), sub(pc, pa))
        if length(n) < 1e-14:
            return  # a squashed triangle at a tip: nothing to draw
        middle = mul(add(add(pa, pb), pc), 1 / 3)
        if dot(n, sub(middle, inside)) < 0:
            b, c = c, b
        self.f.append((a, b, c))

    def quad(self, a, b, c, d, inside):
        self.tri(a, b, c, inside)
        self.tri(a, c, d, inside)

    def normals(self):
        """Smooth shading: vertices at the same spot in the same group share one normal."""
        sums = {}
        keys = []
        for p, g in zip(self.v, self.group):
            keys.append((g, round(p[0], 5), round(p[1], 5), round(p[2], 5)))
        for a, b, c in self.f:
            n = cross(sub(self.v[b], self.v[a]), sub(self.v[c], self.v[a]))  # longer for bigger faces
            for i in (a, b, c):
                sums[keys[i]] = add(sums.get(keys[i], (0.0, 0.0, 0.0)), n)
        return [norm(sums.get(k, (0.0, 1.0, 0.0))) for k in keys]

    def move(self, offset):
        self.v = [add(p, offset) for p in self.v]

    def bounds(self):
        lo = tuple(min(p[i] for p in self.v) for i in range(3))
        hi = tuple(max(p[i] for p in self.v) for i in range(3))
        return lo, hi

    def save(self):
        n = self.normals()
        path = os.path.join(OUT, self.name + ".obj")
        with open(path, "w") as out:
            out.write("# FarmsteadVR prop, made by Tools/ModelGen/generate_props.py\n")
            out.write("o %s\n" % self.name)
            for p in self.v:
                out.write("v %.6f %.6f %.6f\n" % p)
            for uv in self.t:
                out.write("vt %.5f %.5f\n" % uv)
            for q in n:
                out.write("vn %.5f %.5f %.5f\n" % q)
            for a, b, c in self.f:
                out.write("f %d/%d/%d %d/%d/%d %d/%d/%d\n" % (a + 1, a + 1, a + 1, b + 1, b + 1, b + 1, c + 1, c + 1, c + 1))
        lo, hi = self.bounds()
        size = sub(hi, lo)
        print("  %-18s %5d triangles, %.2f x %.2f x %.2f m" % (self.name + ".obj", len(self.f), size[0], size[1], size[2]))


# ---------- shapes ----------

def lathe(mesh, profile, segments, group, center=(0.0, 0.0, 0.0), wobble=None, uv_scale=(1.0, 1.0)):
    """Turns a profile [(height, radius), ...] (from bottom to top) around the vertical axis."""
    distances = [0.0]
    for (y0, r0), (y1, r1) in zip(profile, profile[1:]):
        distances.append(distances[-1] + math.hypot(y1 - y0, r1 - r0))
    total = distances[-1] or 1.0
    middle_y = sum(y for y, r in profile) / len(profile)
    rows = []
    for (y, r), dist in zip(profile, distances):
        row = []
        for i in range(segments + 1):
            angle = TAU * (i % segments) / segments
            rr = r * (wobble(angle, y) if wobble else 1.0)
            p = (center[0] + rr * math.cos(angle), center[1] + y, center[2] + rr * math.sin(angle))
            row.append(mesh.vertex(p, (i / segments * uv_scale[0], dist / total * uv_scale[1]), group))
        rows.append(row)
    for j in range(len(profile) - 1):
        y0, y1 = profile[j][0], profile[j + 1][0]
        inside_y = (y0 + y1) / 2
        if abs(y1 - y0) < 1e-9:  # a flat ring (a bottom or a lid): "inside" is towards the rest of the shape
            inside_y += 0.001 if middle_y > y0 else -0.001
        inside = (center[0], center[1] + inside_y, center[2])
        for i in range(segments):
            mesh.quad(rows[j][i], rows[j + 1][i], rows[j + 1][i + 1], rows[j][i + 1], inside)


def tube(mesh, path, radius_at, segments, group, closed=False, caps=True, flat=1.0):
    """A tube along a path. radius_at(t) for t in 0..1 along the path; flat < 1 squashes it
    (wide one way, thin the other, like a leaf or a horseshoe)."""
    count = len(path)
    lengths = [0.0]
    for a, b in zip(path, path[1:]):
        lengths.append(lengths[-1] + length(sub(b, a)))
    total = lengths[-1] or 1.0

    def tangent(k):
        if closed:
            return norm(sub(path[(k + 1) % (count - 1)], path[(k - 1) % (count - 1)]))
        return norm(sub(path[min(k + 1, count - 1)], path[max(k - 1, 0)]))

    # "Parallel transport": each ring is turned as little as possible from the previous one,
    # so the tube doesn't twist.
    t0 = tangent(0)
    helper = (0.0, 1.0, 0.0) if abs(t0[1]) < 0.9 else (1.0, 0.0, 0.0)
    side = norm(cross(helper, t0))
    rings = []
    for k in range(count):
        t = tangent(k)
        side = norm(sub(side, mul(t, dot(side, t))))
        up = cross(t, side)
        r = radius_at(lengths[k] / total)
        ring = []
        for i in range(segments + 1):
            a = TAU * (i % segments) / segments
            offset = add(mul(side, math.cos(a) * r), mul(up, math.sin(a) * r * flat))
            ring.append(mesh.vertex(add(path[k], offset), (i / segments, lengths[k] / total), group))
        rings.append(ring)
    for k in range(count - 1):
        inside = mul(add(path[k], path[k + 1]), 0.5)
        for i in range(segments):
            mesh.quad(rings[k][i], rings[k + 1][i], rings[k + 1][i + 1], rings[k][i + 1], inside)
    if caps and not closed:
        for k, step in ((0, 1), (count - 1, -1)):
            middle = mesh.vertex(path[k], (0.5, 0.5), group + 1000)
            inside = add(path[k], mul(sub(path[k + step], path[k]), 0.5))
            ring = [mesh.vertex(mesh.v[i], (0.5, 0.5), group + 1000) for i in rings[k]]
            for i in range(segments):
                mesh.tri(middle, ring[i], ring[i + 1], inside)


def catmull_rom(points, samples_per_span):
    """A smooth curve through all the points."""
    out = []
    pts = [points[0]] + list(points) + [points[-1]]
    for i in range(1, len(pts) - 2):
        p0, p1, p2, p3 = pts[i - 1], pts[i], pts[i + 1], pts[i + 2]
        for s in range(samples_per_span):
            t = s / samples_per_span
            t2, t3 = t * t, t * t * t
            out.append(tuple(0.5 * ((2 * p1[c]) + (-p0[c] + p2[c]) * t + (2 * p0[c] - 5 * p1[c] + 4 * p2[c] - p3[c]) * t2
                                    + (-p0[c] + 3 * p1[c] - 3 * p2[c] + p3[c]) * t3) for c in range(3)))
    out.append(points[-1])
    return out


# ---------- the props ----------

def horseshoe():
    """Grandpa Panas's lucky horseshoe, lying flat. About 13 x 12 cm."""
    m = Mesh("Horseshoe")
    radius, leg = 0.052, 0.046
    path = []
    for k in range(5):  # left heel -> up the left side
        f = k / 5
        path.append((-radius + 0.006 * (1 - f), 0.0, -leg * (1 - f)))
    for k in range(21):  # the round toe
        a = math.pi - math.pi * k / 20
        path.append((radius * math.cos(a), 0.0, radius * math.sin(a)))
    for k in range(1, 6):  # down the right side to the right heel
        f = k / 5
        path.append((radius - 0.006 * f, 0.0, -leg * f))

    def width(t):
        return 0.0105 + 0.0018 * math.sin(math.pi * t)  # a bit wider at the toe

    tube(m, path, width, 10, group=1, flat=0.55)
    lo, hi = m.bounds()
    m.move((-(lo[0] + hi[0]) / 2, -lo[1], -(lo[2] + hi[2]) / 2))
    m.save()


def carrot_root():
    """An orange carrot, the green top starts at height 0 (the soil line)."""
    m = Mesh("CarrotRoot")
    profile = []
    rows = 22
    for j in range(rows + 1):
        t = j / rows                      # 0 = tip, 1 = top
        y = -0.16 * (1 - t)
        r = 0.021 * (t ** 0.75) if j > 0 else 0.0
        profile.append((y, r))
    profile += [(0.003, 0.018), (0.006, 0.011), (0.0068, 0.0)]

    def bumps(angle, y):
        rings = 0.06 if math.sin(y * 260) > 0.8 else 0.0  # little rings on the skin
        return 1.0 + 0.04 * math.sin(3 * angle + 30 * y) - rings

    lathe(m, profile, 14, group=1, wobble=bumps, uv_scale=(1, 2))
    m.save()


def carrot_leaves():
    """The feathery green top of a carrot: stems with little leaves along them. About 20 cm tall."""
    m = Mesh("CarrotLeaves")
    rnd = random.Random(5)
    stems = 6
    for s in range(stems):
        heading = s * TAU / stems + rnd.uniform(-0.25, 0.25)
        lean = rnd.uniform(0.3, 0.62)
        size = rnd.uniform(0.15, 0.2)
        p = (0.0, 0.0, 0.0)
        path = [p]
        directions = []
        for k in range(5):
            bend = lean * (0.35 + 0.65 * k / 4)
            d = (math.sin(bend) * math.cos(heading), math.cos(bend), math.sin(bend) * math.sin(heading))
            directions.append(d)
            p = add(p, mul(d, size / 5))
            path.append(p)
        tube(m, path, lambda t: 0.0021 * (1 - 0.6 * t), 4, group=10 + s)
        for k, f in ((2, 0.85), (3, 0.68), (4, 0.52), (5, 0.36)):  # little leaves along the stem
            base = path[k]
            d = directions[min(k, 4)]
            across = norm(cross(d, (0.0, 1.0, 0.0)))
            for sign in (-1, 1):
                leaf_dir = norm(add(add(mul(d, 0.55), mul(across, 0.8 * sign)), (0.0, 0.25, 0.0)))
                leaf_len = 0.04 * f
                leaf = [add(base, mul(leaf_dir, leaf_len * q / 2)) for q in range(3)]
                tube(m, leaf, lambda t: 0.0068 * math.sin(math.pi * (0.2 + 0.75 * t)) + 0.0005, 4,
                     group=100 + s * 20 + k * 2 + (sign > 0), flat=0.18, caps=False)
    m.save()


def watering_can():
    """A garden watering can: round body, handle over the top, long spout with a sprinkler head."""
    m = Mesh("WateringCan")
    body = [(0.0, 0.0), (0.0, 0.079), (0.006, 0.084), (0.17, 0.081), (0.188, 0.075), (0.203, 0.061),
            (0.213, 0.042), (0.219, 0.02), (0.221, 0.0)]
    lathe(m, body, 24, group=1, uv_scale=(2, 1))
    # the filling hole at the top, a little to the back
    neck = [(0.0, 0.0), (0.0, 0.026), (0.045, 0.026), (0.048, 0.029), (0.052, 0.029), (0.054, 0.02), (0.054, 0.0)]
    lathe(m, neck, 14, group=2, center=(-0.032, 0.18, 0.0))
    # the handle: from the back of the body, up over the top, down to the front
    handle = catmull_rom([(-0.08, 0.075, 0.0), (-0.128, 0.15, 0.0), (-0.105, 0.255, 0.0), (-0.02, 0.29, 0.0),
                          (0.045, 0.27, 0.0), (0.06, 0.205, 0.0)], 6)
    tube(m, handle, lambda t: 0.0085, 10, group=3)
    # the spout: starts inside the body low at the front and goes up and out
    start, end = (0.045, 0.04, 0.0), (0.285, 0.255, 0.0)
    spout = [tuple(lerp(start[c], end[c], k / 14) for c in range(3)) for k in range(15)]

    def spout_radius(t):
        if t < 0.88:
            return lerp(0.0135, 0.0085, t / 0.88)
        return lerp(0.0085, 0.025, ((t - 0.88) / 0.12) ** 0.7)  # the sprinkler head ("rose")

    tube(m, spout, spout_radius, 12, group=4)
    m.save()


def grain_sack():
    """A lumpy burlap sack of grain, tied with a rope at the neck. About 56 cm tall."""
    m = Mesh("GrainSack")
    profile = [(0.0, 0.0), (0.0, 0.17), (0.02, 0.205), (0.06, 0.226), (0.16, 0.236), (0.26, 0.226),
               (0.34, 0.192), (0.40, 0.132), (0.44, 0.078), (0.46, 0.062), (0.48, 0.066), (0.51, 0.09),
               (0.54, 0.106), (0.553, 0.094), (0.56, 0.0)]

    def lumps(angle, y):
        tuft = 0.13 * math.sin(9 * angle) if y > 0.49 else 0.0  # the cloth bunched above the rope
        return 1.0 + 0.045 * math.sin(3 * angle + 7 * y) + 0.025 * math.sin(7 * angle - 11 * y) + tuft

    lathe(m, profile, 28, group=1, wobble=lumps, uv_scale=(3, 2))
    rope = [(0.071 * math.cos(TAU * k / 24), 0.462, 0.071 * math.sin(TAU * k / 24)) for k in range(25)]
    tube(m, rope, lambda t: 0.009, 8, group=2, closed=True, caps=False)
    m.save()


# ---------- the burlap texture ----------

def burlap_png(size=256, cell=8):
    """Woven sack cloth: threads going over and under each other."""
    rnd = random.Random(9)
    shade_x = [rnd.uniform(0.85, 1.12) for _ in range(size // cell)]
    shade_y = [rnd.uniform(0.85, 1.12) for _ in range(size // cell)]
    base = (178, 143, 98)
    gap = (92, 70, 46)
    rows = bytearray()
    for y in range(size):
        rows.append(0)  # PNG: no filter for this row
        for x in range(size):
            cx, cy = x // cell, y // cell
            fx, fy = (x % cell) / (cell - 1), (y % cell) / (cell - 1)
            across_on_top = (cx + cy) % 2 == 0
            # a thread is round: brightest in its middle, darker at its edges
            if across_on_top:
                k = math.sin(math.pi * fy) * shade_y[cy]
                k *= 0.88 + 0.12 * math.sin(fx * math.pi)
            else:
                k = math.sin(math.pi * fx) * shade_x[cx]
                k *= 0.88 + 0.12 * math.sin(fy * math.pi)
            k = max(0.0, min(1.25, k)) ** 0.7
            fibre = rnd.uniform(-10, 10)
            for c in range(3):
                value = gap[c] + (base[c] - gap[c]) * k + fibre
                rows.append(int(max(0, min(255, value))))
    path = os.path.join(OUT, "Burlap.png")

    def chunk(kind, data):
        return struct.pack(">I", len(data)) + kind + data + struct.pack(">I", zlib.crc32(kind + data) & 0xFFFFFFFF)

    with open(path, "wb") as out:
        out.write(b"\x89PNG\r\n\x1a\n")
        out.write(chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 2, 0, 0, 0)))
        out.write(chunk(b"IDAT", zlib.compress(bytes(rows), 9)))
        out.write(chunk(b"IEND", b""))
    print("  %-18s %dx%d" % ("Burlap.png", size, size))


def main():
    os.makedirs(OUT, exist_ok=True)
    print("Writing props to", OUT)
    horseshoe()
    carrot_root()
    carrot_leaves()
    watering_can()
    grain_sack()
    burlap_png()
    print("Done.")


if __name__ == "__main__":
    main()
