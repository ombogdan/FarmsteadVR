#!/usr/bin/env python3
"""
Removes the far-forest trees that stand on the road.

The distant groves (Assets/Resources/CountryArt/OakGrove.obj and PineGrove.obj) are flat tree
pictures ("cards") in a big ring around the farm. Some of them were placed right on the road
(the road runs along z = -16, 7 m wide). This script deletes those cards and keeps everything else.
Safe to run again (e.g. after the groves are re-made): it only removes what stands on the road.

    python3 Tools/ModelGen/clear_road.py
"""
import os

HERE = os.path.dirname(os.path.abspath(__file__))
ART = os.path.normpath(os.path.join(HERE, "..", "..", "Assets", "Resources", "CountryArt"))
ROAD_Z, ROAD_HALF_WIDTH = -16.0, 4.5  # the road is 7 m wide; trees closer than 1 m to its edge go too


def clear(name):
    path = os.path.join(ART, name + ".obj")
    lines = open(path).read().split("\n")
    verts = [tuple(map(float, l.split()[1:4])) for l in lines if l.startswith("v ")]
    faces = [(k, [int(t.split("/")[0]) - 1 for t in l.split()[1:]]) for k, l in enumerate(lines) if l.startswith("f ")]

    # Faces that share vertices belong to the same tree card.
    parent = list(range(len(verts)))

    def root(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a

    for _, f in faces:
        for i in f[1:]:
            parent[root(f[0])] = root(i)
    members = {}
    for _, f in faces:  # only vertices that are still used by some face
        for i in f:
            members.setdefault(root(i), set()).add(i)
    on_road = set()
    for r, idx in members.items():
        z = sum(verts[i][2] for i in idx) / len(idx)
        if abs(z - ROAD_Z) < ROAD_HALF_WIDTH:
            on_road.add(r)

    drop = {k for k, f in faces if root(f[0]) in on_road}
    if drop:
        with open(path, "w") as out:
            out.write("\n".join(l for k, l in enumerate(lines) if k not in drop))
    print("  %-10s removed %d tree cards from the road (%d faces)" % (name, len(on_road), len(drop)))


if __name__ == "__main__":
    for grove in ("OakGrove", "PineGrove"):
        clear(grove)
