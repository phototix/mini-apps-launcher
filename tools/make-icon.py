#!/usr/bin/env python3
"""Generate assets/launcher.ico. Requires Pillow (pip install pillow)."""

from pathlib import Path

from PIL import Image, ImageDraw

MASTER = 1024
OUT = Path(__file__).resolve().parent.parent / "assets" / "launcher.ico"
SIZES = [(16, 16), (24, 24), (32, 32), (48, 48), (64, 64), (128, 128)]


def mix(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def build():
    top = (37, 99, 235)
    bottom = (15, 23, 42)

    image = Image.new("RGBA", (MASTER, MASTER), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    for y in range(MASTER):
        color = mix(top, bottom, y / (MASTER - 1))
        draw.line([(0, y), (MASTER, y)], fill=color + (255,))

    pad = int(MASTER * 0.055)
    radius = int(MASTER * 0.235)
    mask = Image.new("L", (MASTER, MASTER), 0)
    ImageDraw.Draw(mask).rounded_rectangle(
        [pad, pad, MASTER - pad, MASTER - pad], radius=radius, fill=255
    )
    image.putalpha(mask)

    draw = ImageDraw.Draw(image)
    inset = int(MASTER * 0.245)
    gap = int(MASTER * 0.052)
    tile = (MASTER - inset * 2 - gap) // 2
    tile_radius = int(tile * 0.28)

    colors = [
        (248, 250, 252),
        (125, 211, 252),
        (125, 211, 252),
        (248, 250, 252),
    ]
    positions = [
        (inset, inset),
        (inset + tile + gap, inset),
        (inset, inset + tile + gap),
        (inset + tile + gap, inset + tile + gap),
    ]

    for (x, y), color in zip(positions, colors):
        draw.rounded_rectangle(
            [x, y, x + tile, y + tile], radius=tile_radius, fill=color + (255,)
        )

    return image


def main():
    OUT.parent.mkdir(parents=True, exist_ok=True)
    build().save(OUT, format="ICO", sizes=SIZES)
    print(f"Wrote {OUT}")


if __name__ == "__main__":
    main()
