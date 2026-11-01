"""Feelin' Slimy - procedural pixel-art slime sprite pipeline (Terraria-style jelly, no face, nothing inside).

Usage (from the project root):
    python Tools/SlimeArt/slime_pipeline.py                      # all palettes, all animations
    python Tools/SlimeArt/slime_pipeline.py --palettes calm --only idle land

Outputs:
    Assets/Art/Slime/<Palette>/slime_<palette>_<anim>_<NN>.png   Unity frames (112x112, 64 PPU, centre pivot)
    Assets/Art/Slime/slime_animations.json                       fps / loop / frame-count manifest
    Tools/SlimeArt/preview/<palette>_<anim>.gif                  4x previews
    Tools/SlimeArt/preview/contact_<palette>.png                 every frame on one sheet
"""
from __future__ import annotations

import argparse
import json
import math
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
FRAME = 112
GROUND_ROW = FRAME // 2 + 27
W0, H0, SINK = 29.0, 42.0, 2.0
TAU = 2 * math.pi
LIGHT = np.array([-0.6, 0.8]) / np.hypot(-0.6, 0.8)
BODY_ALPHA = 238
DITHER = 0.0

PALETTES = {
    "calm": dict(
        outline=(18, 24, 82), outline_lit=(30, 44, 128),
        shades=[(30, 50, 158), (40, 76, 198), (54, 104, 226), (78, 136, 244), (114, 170, 252)],
        highlight=(192, 222, 255), spec=(255, 255, 255),
    ),
    "angry": dict(
        outline=(72, 12, 18), outline_lit=(116, 26, 24),
        shades=[(128, 20, 26), (174, 34, 30), (212, 56, 36), (236, 90, 46), (248, 134, 70)],
        highlight=(255, 200, 150), spec=(255, 248, 228),
    ),
}

BAYER = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0 + 1 / 32
BAYER = np.tile(BAYER, (FRAME // 4, FRAME // 4))

_cols, _rows = np.meshgrid(np.arange(FRAME), np.arange(FRAME))
X = _cols + 0.5 - FRAME / 2
Y = GROUND_ROW - (_rows + 0.5)


@dataclass
class Drop:
    x: float
    y: float
    r: float
    k: float = 0.0
    stretch: float = 1.0
    angle: float = 0.0


@dataclass
class Pose:
    w: float = W0
    h: float = H0
    cx: float = 0.0
    base: float = -SINK
    c: float = 0.38
    p_top: float = 2.0
    p_bot: float = 3.2
    lean: float = 0.0
    taper: float = 0.0
    waves: list = field(default_factory=list)
    wave_bottom: float = 0.3
    drops: list = field(default_factory=list)
    bubbles: list = field(default_factory=list)
    flash: float = 0.0
    mix: float = 0.0
    ground: bool = True

    def point(self, u: float, yn: float) -> tuple[float, float]:
        ws = self.w * max(0.25, 1 + self.taper * (0.5 - yn))
        return self.cx + self.lean * min(max(yn, 0), 1) ** 1.5 + u * ws, self.base + yn * self.h


def squash(sy: float, size: float = 1.0, vol: float = 0.65, **kw) -> Pose:
    return Pose(w=W0 * size * sy ** -vol, h=H0 * size * sy, **kw)


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def ease(x: float) -> float:
    x = min(max(x, 0.0), 1.0)
    return x * x * (3 - 2 * x)


def smin(a, b, k):
    if k <= 0:
        return np.minimum(a, b)
    h = np.clip(0.5 + 0.5 * (b - a) / k, 0, 1)
    return b + (a - b) * h - k * h * (1 - h)


def ballistic(x0, y0, vx, vy, g, r0, shrink, t, k0=0.0, kfall=0.0):
    x, y, r = x0 + vx * t, y0 + vy * t - 0.5 * g * t * t, r0 - shrink * t
    if r < 0.8 or (t > 0 and y < r * 0.4):
        return None
    vyt = vy - g * t
    speed = math.hypot(vx, vyt)
    return Drop(x, y, r, max(0.0, k0 - kfall * t), min(2.2, 1 + 0.11 * speed), math.atan2(vyt, vx))


def body_field(p: Pose, x, y):
    yn = (y - p.base) / p.h
    ws = p.w * np.clip(1 + p.taper * (0.5 - yn), 0.25, None)
    u = (x - p.cx - p.lean * np.clip(yn, 0, 1) ** 1.5) / ws
    top = yn >= p.c
    v = np.where(top, (yn - p.c) / (1 - p.c), (yn - p.c) / p.c)
    pw = np.where(top, p.p_top, p.p_bot)
    r = (np.abs(u) ** pw + np.abs(v) ** pw) ** (1 / pw)
    theta = np.arctan2(v, u)
    wob = np.zeros_like(r)
    for amp, freq, phase in p.waves:
        wob += amp * np.sin(freq * theta + phase)
    wob *= p.wave_bottom + (1 - p.wave_bottom) * smoothstep(-0.25, 0.35, v / np.maximum(r, 1e-6))
    rw = r - wob
    glow = np.clip(1 - np.hypot(u, (yn - 0.62) / 0.75), 0, 1)
    return (rw - 1) * min(p.w, p.h * (1 - p.c)), rw, theta, yn, glow


def drop_field(dp: Drop, x, y):
    dx, dy = x - dp.x, y - dp.y
    if dp.stretch != 1:
        ca, sa = math.cos(dp.angle), math.sin(dp.angle)
        dx, dy = (dx * ca + dy * sa) / dp.stretch, -dx * sa + dy * ca
    return np.hypot(dx, dy) - dp.r


def full_field(p: Pose, x, y):
    d, rw, theta, yn, glow = body_field(p, x, y)
    for dp in p.drops:
        d = smin(d, drop_field(dp, x, y), dp.k)
    if p.ground:
        d = np.maximum(d, -y)
    return d, rw, theta, yn, glow


def neighbours(mask, diagonal=False):
    m = np.pad(mask, 1).astype(np.int8)
    n = m[:-2, 1:-1] + m[2:, 1:-1] + m[1:-1, :-2] + m[1:-1, 2:]
    if diagonal:
        n = n + m[:-2, :-2] + m[:-2, 2:] + m[2:, :-2] + m[2:, 2:]
    return n


def shade_rgb(p: Pose, pal: dict, idx, edge, edge_lit, hl, spec):
    shades = np.array(pal["shades"], dtype=np.float32)
    rgb = shades[idx]
    rgb[hl] = pal["highlight"]
    rgb[spec] = pal["spec"]
    rgb[edge] = pal["outline"]
    rgb[edge & edge_lit] = pal["outline_lit"]
    return rgb


def render(p: Pose, pal: dict, pal_to: dict | None = None) -> Image.Image:
    d, rw, theta, yn, glow = full_field(p, X, Y)
    e = 0.5
    gx = full_field(p, X + e, Y)[0] - full_field(p, X - e, Y)[0]
    gy = full_field(p, X, Y + e)[0] - full_field(p, X, Y - e)[0]
    facing = (gx * LIGHT[0] + gy * LIGHT[1]) / np.maximum(np.hypot(gx, gy), 1e-6)

    mask = d < 0
    mask &= neighbours(mask, diagonal=True) > 1
    mask |= neighbours(mask) == 4
    edge = mask & (neighbours(mask) < 4)
    inner = mask & ~edge

    depth = smoothstep(0, 9, -d)
    s = 0.05 + 0.45 * depth + 0.55 * glow + 0.15 * np.clip(yn, 0, 1.2) + 0.15 * facing * (1 - depth)
    idx = np.clip(np.floor(s * 5 + (BAYER - 0.5) * DITHER), 0, 4).astype(int)

    hl = inner & (rw > 0.7) & (rw < 0.83) & (theta > 1.85) & (theta < 2.6)
    hl |= inner & (rw > 0.58) & (rw < 0.67) & (theta > 2.62) & (theta < 2.82)
    spec = inner & (rw > 0.735) & (rw < 0.795) & (theta > 2.02) & (theta < 2.3)

    for bu, byn, br in p.bubbles:
        bx, by = p.point(bu, byn)
        dist = np.hypot(X - bx, Y - by)
        body = inner & (depth > 0.25)
        ring = body & (dist <= br + 0.4) & (dist > br - 0.6)
        idx[ring] = np.maximum(idx[ring] - 2, 1)
        hl |= body & (np.hypot(X - (bx - br * 0.4), Y - (by + br * 0.4)) <= 0.75)
    for dp in p.drops:
        if dp.r >= 1.8:
            hl |= inner & (np.hypot(X - (dp.x - dp.r * 0.4), Y - (dp.y + dp.r * 0.4)) <= 0.8)

    edge_lit = facing > 0.45
    rgb = shade_rgb(p, pal, idx, edge, edge_lit, hl, spec)
    if pal_to is not None and p.mix > 0:
        rgb_to = shade_rgb(p, pal_to, idx, edge, edge_lit, hl, spec)
        rgb = np.where((BAYER < p.mix)[..., None], rgb_to, rgb)
    if p.flash > 0:
        rgb = rgb + (255 - rgb) * p.flash

    alpha = np.where(mask, BODY_ALPHA, 0)
    alpha[edge | hl | spec] = 255
    out = np.dstack([np.clip(rgb, 0, 255), alpha]).astype(np.uint8)
    return Image.fromarray(out, "RGBA")


ANIMS: dict[str, dict] = {}


def anim(name, frames, fps, loop=True, mix_from=None):
    def deco(fn):
        ANIMS[name] = dict(fn=fn, frames=frames, fps=fps, loop=loop, mix_from=mix_from)
        return fn
    return deco


def side_splash(i, x_edge, specs, g=1.0, y0=2.0, k0=5.0, kfall=2.5):
    drops = []
    for side in (-1, 1):
        for vx, vy, r, shrink in specs:
            dp = ballistic(side * x_edge, y0, side * vx, vy, g, r, shrink, i, k0, kfall)
            if dp:
                drops.append(dp)
    return drops


@anim("idle", 8, 10)
def idle(t, i):
    ph = TAU * t
    bubbles = []
    for u0, off, rmax in ((-0.3, 0.0, 2.4), (0.34, 0.4, 1.9), (0.04, 0.72, 1.5)):
        life = (t + off) % 1
        if life < 0.85:
            bubbles.append((u0 + 0.07 * math.sin(TAU * life * 1.5 + off * 9), 0.14 + 0.6 * life, rmax * (0.55 + 0.45 * life)))
    return squash(1 + 0.05 * math.sin(ph), waves=[(0.03, 2, ph), (0.018, 4, -2 * ph)], bubbles=bubbles)


@anim("move", 8, 12)
def move(t, i):
    ph = TAU * t
    p = squash(1 + 0.07 * math.sin(ph), lean=4 + 3 * math.sin(ph - 1.2), cx=1.5 * math.sin(ph + 0.5),
               waves=[(0.045, 3, -ph), (0.015, 5, 2 * ph)])
    if t < 0.75:
        life = t / 0.75
        p.drops.append(Drop(p.cx - p.w * 1.0 - 11 * life, 3.2 - 0.8 * life, 3.4 * (1 - 0.6 * life), max(0.0, 6 * (1 - 2.0 * life))))
    return p


@anim("jump", 5, 15, loop=False)
def jump(t, i):
    sy = (0.84, 0.66, 1.32, 1.38, 1.24)[i]
    if i < 2:
        return squash(sy, p_bot=3.6, waves=[(0.03, 5, i * 1.7)])
    j = i - 2
    p = squash(sy, base=(-SINK, 1.0, 2.0)[j], taper=(-0.3, -0.42, -0.32)[j], p_bot=(2.8, 2.1, 2.0)[j],
               waves=[(0.04, 3, i * 2.0)], wave_bottom=0.6)
    p.drops = side_splash(j, 13, [(3.4, 2.6, 3.0, 0.5)], g=0.9, y0=2.0, k0=5, kfall=4)
    return p


@anim("rise", 4, 12)
def rise(t, i):
    ph = TAU * t
    return squash(1.22 + 0.03 * math.sin(ph), base=1.0, taper=-0.3, p_bot=2.1, wave_bottom=0.6,
                  waves=[(0.03, 3, ph), (0.02, 5, -ph)], ground=False)


@anim("fall", 4, 12)
def fall(t, i):
    ph = TAU * t
    p = squash(1.12 + 0.03 * math.sin(ph), base=1.0, taper=0.25, p_bot=2.0, p_top=2.3, wave_bottom=0.15,
               waves=[(0.055, 4, 2 * ph), (0.025, 6, -ph)], ground=False)
    for u, off in ((-0.35, 0.0), (0.3, 0.5)):
        life = (t + off) % 1
        x, y = p.point(u, 1.0)
        p.drops.append(Drop(x, y + 3 + 9 * life, 2.4 * (1 - 0.55 * life), max(0.0, 4 * (1 - 3 * life)), 1.6, math.pi / 2))
    return p


@anim("land", 8, 15, loop=False)
def land(t, i):
    decay = math.exp(-0.42 * i)
    sy = 1 - 0.42 * decay * math.cos(1.05 * i)
    p = squash(sy, p_bot=3.2 + 2.4 * decay, p_top=2.0 + 0.5 * decay,
               waves=[(0.07 * decay, 3, i * 1.7), (0.04 * decay, 5, i * 2.3)])
    p.drops = side_splash(i, W0 * 0.58 ** -0.65 * 0.8, [(3.6, 3.4, 3.6, 0.3), (1.2, 6.4, 2.8, 0.25)])
    return p


@anim("stomp_windup", 4, 16, loop=False)
def stomp_windup(t, i):
    if i == 3:
        return squash(1.3, base=2.0, taper=0.45, p_top=1.7, p_bot=2.0, ground=False, waves=[(0.03, 4, 1.0)])
    return squash((1.08, 0.94, 0.98)[i], size=(0.92, 0.84, 0.8)[i], base=2.0, c=0.48, p_top=2.0, p_bot=2.0, ground=False,
                  wave_bottom=1.0, waves=[(0.03 + 0.012 * i, 6 + i % 2, i * 2.1), (0.02, 9, -i * 1.3)])


@anim("stomp_fall", 2, 16)
def stomp_fall(t, i):
    p = squash(1.45 + 0.05 * i, base=2.0, taper=0.5, p_top=1.6, p_bot=2.0, ground=False,
               waves=[(0.03, 4, i * math.pi)], wave_bottom=0.1)
    for xo, yo in ((-9, 6), (0, 10), (9, 4)):
        p.drops.append(Drop(p.cx + xo * 0.7, p.base + p.h + yo + (i * 5 + yo) % 8, 1.4, 0.0, 3.0, math.pi / 2))
    return p


@anim("stomp_impact", 9, 15, loop=False)
def stomp_impact(t, i):
    sy = (0.4, 0.5, 0.82, 1.24, 1.12, 0.92, 1.04, 0.98, 1.0)[i]
    decay = math.exp(-0.38 * i)
    p = squash(sy, vol=0.6, p_bot=3.2 + 3.0 * decay, p_top=2.0 + 0.7 * decay,
               waves=[(0.08 * decay, 3, i * 1.9), (0.05 * decay, 6, i * 2.7)])
    p.drops = side_splash(i, W0 * 0.4 ** -0.6 * 0.82, [(4.6, 5.4, 4.0, 0.32), (2.8, 7.6, 3.2, 0.28), (6.2, 3.4, 2.8, 0.25)],
                          g=1.1, y0=3.0, k0=6, kfall=3)
    return p


@anim("hurt", 5, 14, loop=False)
def hurt(t, i):
    p = squash((0.86, 1.12, 0.94, 1.04, 1.0)[i], lean=(-6, 4, -3, 1.5, 0)[i], flash=(0.85, 0, 0.6, 0, 0)[i],
               waves=[(0.09 - 0.02 * i, 5, i * 2.1), (0.04 - 0.008 * i, 3, -i * 1.4)])
    x0, y0 = p.point(-0.55, 0.8)
    for vx, vy, r in ((-3.0, 2.6, 2.4), (-1.6, 3.6, 1.8)):
        dp = ballistic(x0, y0, vx, vy, 0.8, r, 0.25, i, 4, 4)
        if dp:
            p.drops.append(dp)
    return p


@anim("absorb", 6, 12)
def absorb(t, i):
    ph = TAU * t
    p = squash(1 + 0.04 * math.sin(2 * ph), lean=3 + math.sin(ph), cx=1.0,
               waves=[(0.04, 6, -2 * ph), (0.02, 3, ph)])
    x, y = p.point(1.0, 0.45)
    p.drops.append(Drop(x + 10 - 11 * t, y, 4.2 - 1.4 * t, 7.0))
    p.bubbles = [(0.55 - 0.9 * ((t + off) % 1), 0.4 + 0.12 * off, 1.8) for off in (0.0, 0.5)]
    return p


@anim("powerup", 10, 14, loop=False, mix_from="calm")
def powerup(t, i):
    size = (1.0, 1.04, 1.08, 1.12, 1.16, 1.2, 0.92, 1.06, 0.98, 1.0)[i]
    sy = (1.0, 0.96, 1.04, 0.95, 1.06, 1.0, 0.82, 1.1, 0.97, 1.0)[i]
    waves = [(0.03 + 0.008 * i, 6, i * 2.4), (0.02, 9, -i * 1.7)] if i < 6 else [(0.05 * (10 - i) / 4, 3, i * 1.6)]
    p = squash(sy, size=size, waves=waves, mix=(0, 0, 0.1, 0.25, 0.45, 0.7, 0.9, 1, 1, 1)[i], flash=0.75 if i == 5 else 0.0)
    if i >= 5:
        for a in np.linspace(0.35, math.pi - 0.35, 5):
            x0, y0 = p.cx + math.cos(a) * p.w * 0.85, p.base + p.h * 0.45 + math.sin(a) * p.h * 0.5
            dp = ballistic(x0, y0, math.cos(a) * 4.5, math.sin(a) * 4.5, 0.8, 3.0, 0.35, i - 5, 5, 5)
            if dp:
                p.drops.append(dp)
    return p


@anim("death", 10, 12, loop=False)
def death(t, i):
    if i == 0:
        return squash(1.16, flash=0.7, waves=[(0.08, 5, 0.0)])
    m = ease((i - 1) / 8)
    p = Pose(w=W0 * (1 + 0.5 * m), h=H0 * (1 - 0.8 * m), p_top=2 + 1.6 * m, p_bot=3.2 + 2 * m,
             waves=[(0.05 * (1 - m) + 0.02, 3, i * 1.9), (0.035 * m, 7, i * 1.3)])
    for side, lag in ((-1, 0.0), (1, 0.25)):
        k = ease((i - 1) / 8 + lag)
        if k < 0.95:
            x, y = p.point(side * 0.97, 0.75 * (1 - k))
            p.drops.append(Drop(x, max(y, 2.0), 2.6 - k, 3.0))
    if i >= 6:
        p.bubbles = [(-0.3 + 0.25 * (i - 6), 0.5, 1.8 - 0.4 * (i - 6))]
    return p


def frames_for(name: str) -> list[Pose]:
    a = ANIMS[name]
    return [a["fn"](i / a["frames"], i) for i in range(a["frames"])]


def preview_bg(size: int) -> Image.Image:
    bg = Image.new("RGBA", (size, size), (38, 42, 54, 255))
    ImageDraw.Draw(bg).rectangle([0, GROUND_ROW, size, size], fill=(28, 30, 40, 255))
    return bg


def write_gif(frames: list[Image.Image], fps: int, loop: bool, path: Path, scale: int = 4):
    bg = preview_bg(FRAME)
    imgs = [Image.alpha_composite(bg, f).resize((FRAME * scale,) * 2, Image.NEAREST).convert("P", palette=Image.ADAPTIVE)
            for f in frames]
    durations = [round(1000 / fps)] * len(imgs)
    if not loop:
        durations[-1] = 500
    imgs[0].save(path, save_all=True, append_images=imgs[1:], duration=durations, loop=0, disposal=2)


def write_contact(sheet: dict[str, list[Image.Image]], path: Path, scale: int = 3):
    boxes = [f.getbbox() for fs in sheet.values() for f in fs if f.getbbox()]
    x0, y0 = max(0, min(b[0] for b in boxes) - 2), max(0, min(b[1] for b in boxes) - 2)
    x1, y1 = min(FRAME, max(b[2] for b in boxes) + 2), min(FRAME, max(b[3] for b in boxes) + 2)
    cw, ch = (x1 - x0) * scale, (y1 - y0) * scale
    label_w, cols = 110, max(len(fs) for fs in sheet.values())
    out = Image.new("RGBA", (label_w + cw * cols, ch * len(sheet)), (24, 26, 34, 255))
    draw = ImageDraw.Draw(out)
    cell_bg = preview_bg(FRAME).crop((x0, y0, x1, y1))
    for r, (name, fs) in enumerate(sheet.items()):
        draw.text((8, r * ch + ch // 2 - 6), name, fill=(220, 220, 230, 255))
        for c, f in enumerate(fs):
            cell = Image.alpha_composite(cell_bg, f.crop((x0, y0, x1, y1))).resize((cw, ch), Image.NEAREST)
            out.paste(cell, (label_w + c * cw, r * ch))
    out.save(path)


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--palettes", nargs="+", default=list(PALETTES), choices=list(PALETTES))
    ap.add_argument("--only", nargs="+", choices=list(ANIMS), help="render just these animations")
    ap.add_argument("--out", type=Path, default=ROOT / "Assets" / "Art" / "Slime")
    ap.add_argument("--preview", type=Path, default=Path(__file__).resolve().parent / "preview")
    args = ap.parse_args()

    names = args.only or list(ANIMS)
    args.preview.mkdir(parents=True, exist_ok=True)
    manifest = {"frameSize": FRAME, "pixelsPerUnit": 64, "pivot": "center", "animations": []}

    for pal_name in args.palettes:
        folder = args.out / pal_name.capitalize()
        folder.mkdir(parents=True, exist_ok=True)
        written, sheet = set(), {}
        for name in names:
            a = ANIMS[name]
            if a["mix_from"] == pal_name:
                continue
            pal_from = PALETTES[a["mix_from"]] if a["mix_from"] else PALETTES[pal_name]
            pal_to = PALETTES[pal_name] if a["mix_from"] else None
            imgs = [render(p, pal_from, pal_to) for p in frames_for(name)]
            for i, img in enumerate(imgs):
                path = folder / f"slime_{pal_name}_{name}_{i:02d}.png"
                img.save(path)
                written.add(path.name)
            write_gif(imgs, a["fps"], a["loop"], args.preview / f"{pal_name}_{name}.gif")
            sheet[name] = imgs
            if all(m["name"] != name for m in manifest["animations"]):
                manifest["animations"].append({"name": name, "fps": a["fps"], "loop": a["loop"], "frames": a["frames"]})
        if not args.only:
            for stale in folder.glob(f"slime_{pal_name}_*.png"):
                if stale.name not in written:
                    stale.unlink()
                    stale.with_name(stale.name + ".meta").unlink(missing_ok=True)
            write_contact(sheet, args.preview / f"contact_{pal_name}.png")
        print(f"{pal_name}: {len(written)} frames -> {folder.relative_to(ROOT) if folder.is_relative_to(ROOT) else folder}")

    if not args.only:
        (args.out / "slime_animations.json").write_text(json.dumps(manifest, indent=2))
    print(f"previews -> {args.preview}")


if __name__ == "__main__":
    main()
