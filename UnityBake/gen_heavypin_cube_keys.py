#!/usr/bin/env python3
"""Bake Heavypin fin deploy keys from sanitized Unity .anim clips (Crosswim-style)."""
from __future__ import annotations

import re
from pathlib import Path

FPS = 24.0
FRAME_COUNT = 49  # 0..48 @ 24fps over 2s clips
NAMES = [
    "Cube-1-001",
    "Cube-1-002",
    "Cube-1-003",
    "Cube-1-004",
    "Cube-2-001",
    "Cube-2-002",
    "Cube-2-003",
    "Cube-2-004",
]
ANIM_MAP = {
    "Cube-1-001": "Cube_1_001_Cube_1_001Действие.anim",
    "Cube-1-002": "Cube_1_002_Cube_1_002Действие.anim",
    "Cube-1-003": "Cube_1_003_Cube_1_003Действие.anim",
    "Cube-1-004": "Cube_1_004_Cube_1_004Действие.anim",
    "Cube-2-001": "Cube_2_001_Cube_2_001Действие.anim",
    "Cube-2-002": "Cube_2_002_Cube_2_002Действие.anim",
    "Cube-2-003": "Cube_2_003_Cube_2_003Действие.anim",
    "Cube-2-004": "Cube_2_004_Cube_2_004Действие.anim",
}
ROOT = Path(__file__).resolve().parent
CLIP_DIR = ROOT / "Assets" / "MissilePack" / "AnimClips"
OUT_CS = ROOT.parent / "AGR75Heavypin" / "Runtime" / "HeavypinCubeKeys.cs"


def parse_float_curves(text: str) -> dict[str, list[tuple[float, float]]]:
    curves: dict[str, list[tuple[float, float]]] = {}
    blocks = re.split(r"\n  - curve:\n", text)
    for block in blocks[1:]:
        attr_m = re.search(r"\n    attribute: (.+)\n", block)
        if not attr_m:
            continue
        attr = attr_m.group(1).strip()
        pairs = re.findall(
            r"- serializedVersion: 3\n        time: ([0-9.eE+-]+)\n        value: ([0-9.eE+-]+)",
            block,
        )
        if not pairs:
            continue
        keys = [(float(t), float(v)) for t, v in pairs]
        keys.sort(key=lambda x: x[0])
        curves[attr] = keys
    return curves


def parse_quat_curves(text: str) -> dict[str, list[tuple[float, tuple[float, float, float, float]]]]:
    curves: dict[str, list[tuple[float, tuple[float, float, float, float]]]] = {}
    for block in re.finditer(
        r"m_RotationCurves:\n  - curve:.*?path: (.+?)\n",
        text,
        re.DOTALL,
    ):
        path = block.group(1).strip()
        chunk = block.group(0)
        pairs = re.findall(
            r"time: ([0-9.eE+-]+)\n        value: \{x: ([0-9.eE+-]+), y: ([0-9.eE+-]+), z: ([0-9.eE+-]+), w: ([0-9.eE+-]+)\}",
            chunk,
        )
        if not pairs:
            continue
        keys = [(float(t), (float(x), float(y), float(z), float(w))) for t, x, y, z, w in pairs]
        keys.sort(key=lambda x: x[0])
        curves[path] = keys
    return curves


def sample1d(keys: list[tuple[float, float]], t: float) -> float:
    if not keys:
        return 0.0
    if t <= keys[0][0]:
        return keys[0][1]
    if t >= keys[-1][0]:
        return keys[-1][1]
    for i in range(len(keys) - 1):
        t0, v0 = keys[i]
        t1, v1 = keys[i + 1]
        if t0 <= t <= t1:
            if t1 == t0:
                return v1
            a = (t - t0) / (t1 - t0)
            return v0 + (v1 - v0) * a
    return keys[-1][1]


def sample_quat(keys: list[tuple[float, tuple[float, float, float, float]]], t: float) -> tuple[float, float, float, float]:
    if not keys:
        return (0.0, 0.0, 0.0, 1.0)
    if t <= keys[0][0]:
        return keys[0][1]
    if t >= keys[-1][0]:
        return keys[-1][1]
    for i in range(len(keys) - 1):
        t0, q0 = keys[i]
        t1, q1 = keys[i + 1]
        if t0 <= t <= t1:
            if t1 == t0:
                return q1
            a = (t - t0) / (t1 - t0)
            x = q0[0] + (q1[0] - q0[0]) * a
            y = q0[1] + (q1[1] - q0[1]) * a
            z = q0[2] + (q1[2] - q0[2]) * a
            w = q0[3] + (q1[3] - q0[3]) * a
            n = (x * x + y * y + z * z + w * w) ** 0.5
            if n < 1e-8:
                return q1
            return (x / n, y / n, z / n, w / n)
    return keys[-1][1]


def bake_fin(name: str) -> tuple[list[float], list[float], list[float], list[float], list[float], list[float], list[float]]:
    path = CLIP_DIR / ANIM_MAP[name]
    text = path.read_text(encoding="utf-8")
    floats = parse_float_curves(text)
    quats = parse_quat_curves(text)
    qkeys = quats.get(name, [])

    px: list[float] = []
    py: list[float] = []
    pz: list[float] = []
    rx: list[float] = []
    ry: list[float] = []
    rz: list[float] = []
    rw: list[float] = []

    for f in range(FRAME_COUNT):
        t = f / FPS
        px.append(sample1d(floats.get("m_LocalPosition.x", []), t))
        py.append(sample1d(floats.get("m_LocalPosition.y", []), t))
        pz.append(sample1d(floats.get("m_LocalPosition.z", []), t))
        qx, qy, qz, qw = sample_quat(qkeys, t)
        rx.append(qx)
        ry.append(qy)
        rz.append(qz)
        rw.append(qw)

    return px, py, pz, rx, ry, rz, rw


def write_array(name: str, vals: list[float]) -> str:
    lines = ["        private static readonly float[] " + name + " =", "        {"]
    row: list[str] = []
    for i, v in enumerate(vals):
        row.append(f"{v:.7f}f")
        if len(row) == 8 or i == len(vals) - 1:
            lines.append("            " + ", ".join(row) + ("," if i != len(vals) - 1 else ""))
            row = []
    lines.append("        };")
    lines.append("")
    return "\n".join(lines)


def main() -> None:
    all_px: list[float] = []
    all_py: list[float] = []
    all_pz: list[float] = []
    all_rx: list[float] = []
    all_ry: list[float] = []
    all_rz: list[float] = []
    all_rw: list[float] = []

    for name in NAMES:
        px, py, pz, rx, ry, rz, rw = bake_fin(name)
        d0 = (px[0], py[0], pz[0])
        d1 = (px[-1], py[-1], pz[-1])
        print(f"{name}: p0={d0} pEnd={d1} delta={tuple(d1[i]-d0[i] for i in range(3))}")
        all_px.extend(px)
        all_py.extend(py)
        all_pz.extend(pz)
        all_rx.extend(rx)
        all_ry.extend(ry)
        all_rz.extend(rz)
        all_rw.extend(rw)

    names_cs = ", ".join(f'"{n}"' for n in NAMES)
    sb: list[str] = [
        "// AUTO-GENERATED by UnityBake/gen_heavypin_cube_keys.py — do not edit.",
        "// Crosswim-style absolute local pose per fin frame. Driver sets pos+rot directly.",
        "using UnityEngine;",
        "",
        "namespace Heavypin.Runtime",
        "{",
        "    internal static class HeavypinCubeKeys",
        "    {",
        f"        internal const int FinCount = {len(NAMES)};",
        f"        internal const int FrameCount = {FRAME_COUNT};",
        f"        internal const float Fps = {FPS}f;",
        f"        internal static readonly string[] Names = {{ {names_cs} }};",
        "",
        write_array("Px", all_px),
        write_array("Py", all_py),
        write_array("Pz", all_pz),
        write_array("Rx", all_rx),
        write_array("Ry", all_ry),
        write_array("Rz", all_rz),
        write_array("Rw", all_rw),
        "        private static int Index(int fin, int frame) => fin * FrameCount + frame;",
        "",
        "        internal static Vector3 SamplePos(int fin, float frame)",
        "        {",
        "            int i = Index(fin, ClampFrame(frame));",
        "            return new Vector3(Px[i], Py[i], Pz[i]);",
        "        }",
        "",
        "        internal static Quaternion SampleRot(int fin, float frame)",
        "        {",
        "            if (fin < 0 || fin >= FinCount)",
        "                return Quaternion.identity;",
        "            float f = frame;",
        "            if (f < 0f) f = 0f;",
        "            float last = FrameCount - 1;",
        "            if (f > last) f = last;",
        "            int i0 = (int)f;",
        "            int i1 = i0 + 1;",
        "            if (i1 >= FrameCount) i1 = FrameCount - 1;",
        "            float t = f - i0;",
        "            int a = Index(fin, i0);",
        "            int b = Index(fin, i1);",
        "            Quaternion qa = new Quaternion(Rx[a], Ry[a], Rz[a], Rw[a]);",
        "            Quaternion qb = new Quaternion(Rx[b], Ry[b], Rz[b], Rw[b]);",
        "            return Quaternion.Slerp(qa, qb, t);",
        "        }",
        "",
        "        private static int ClampFrame(float frame)",
        "        {",
        "            if (frame < 0f) return 0;",
        "            if (frame > FrameCount - 1) return FrameCount - 1;",
        "            return (int)frame;",
        "        }",
        "    }",
        "}",
    ]

    OUT_CS.write_text("\n".join(sb), encoding="utf-8")
    print(f"Wrote {OUT_CS}")


if __name__ == "__main__":
    main()
