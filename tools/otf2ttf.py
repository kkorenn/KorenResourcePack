#!/usr/bin/env python3
"""Convert OTF (CFF) to TTF (TrueType) using fontTools + cu2qu.

Usage:
    python3 tools/otf2ttf.py [input.otf ...] [--out-dir DIR] [--keep]

Defaults: scans Fonts/ for *.otf, writes Fonts/<name>.ttf, deletes original
unless --keep is passed.
"""

import argparse
import os
import sys
from pathlib import Path

from cu2qu.pens import Cu2QuPen
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont, newTable
from fontTools.ttLib.tables._g_l_y_f import Glyph


MAX_ERR = 1.0  # in font units, conversion tolerance


def glyphs_to_quadratic(font: TTFont, max_err: float) -> dict:
    glyph_set = font.getGlyphSet()
    glyphs = {}
    for gname in font.getGlyphOrder():
        ttpen = TTGlyphPen(glyph_set)
        cu2qupen = Cu2QuPen(ttpen, max_err, reverse_direction=True)
        glyph_set[gname].draw(cu2qupen)
        glyphs[gname] = ttpen.glyph()
    return glyphs


def convert(in_path: Path, out_path: Path) -> None:
    font = TTFont(str(in_path))
    if "CFF " not in font and "CFF2" not in font:
        raise SystemExit(f"{in_path}: not a CFF/CFF2-based OTF")

    glyphs = glyphs_to_quadratic(font, MAX_ERR)

    glyf = newTable("glyf")
    glyf.glyphOrder = font.getGlyphOrder()
    glyf.glyphs = glyphs
    font["glyf"] = glyf

    loca = newTable("loca")
    font["loca"] = loca

    maxp = font["maxp"]
    maxp.tableVersion = 0x00010000
    maxp.maxZones = 1
    maxp.maxTwilightPoints = 0
    maxp.maxStorage = 0
    maxp.maxFunctionDefs = 0
    maxp.maxInstructionDefs = 0
    maxp.maxStackElements = 0
    maxp.maxSizeOfInstructions = 0
    maxp.maxComponentElements = max(
        (len(g.components) for g in glyphs.values() if hasattr(g, "components")),
        default=0,
    )

    post = font["post"]
    post.formatType = 3.0
    post.extraNames = []
    post.mapping = {}
    post.glyphOrder = None

    for tag in ("CFF ", "CFF2", "VORG"):
        if tag in font:
            del font[tag]

    head = font["head"]
    head.indexToLocFormat = 0
    head.glyphDataFormat = 0

    sfnt_version = font.sfntVersion
    font.sfntVersion = "\x00\x01\x00\x00"  # TrueType
    try:
        font.save(str(out_path))
    except Exception:
        font.sfntVersion = sfnt_version
        raise


def main() -> int:
    p = argparse.ArgumentParser(description="OTF to TTF converter")
    p.add_argument("inputs", nargs="*", help="OTF files; default: scan Fonts/")
    p.add_argument("--out-dir", default=None, help="Output directory")
    p.add_argument("--keep", action="store_true", help="Keep original OTF files")
    args = p.parse_args()

    if args.inputs:
        inputs = [Path(x) for x in args.inputs]
    else:
        inputs = sorted(Path("Fonts").glob("*.otf"))

    if not inputs:
        print("No OTF files to convert.")
        return 0

    for in_path in inputs:
        out_dir = Path(args.out_dir) if args.out_dir else in_path.parent
        out_dir.mkdir(parents=True, exist_ok=True)
        out_path = out_dir / (in_path.stem + ".ttf")

        print(f"Converting {in_path} -> {out_path}")
        try:
            convert(in_path, out_path)
        except Exception as e:
            print(f"  Failed: {e}", file=sys.stderr)
            continue

        if not args.keep:
            try:
                in_path.unlink()
                print(f"  Deleted {in_path}")
            except OSError as e:
                print(f"  Could not delete {in_path}: {e}", file=sys.stderr)

    return 0


if __name__ == "__main__":
    sys.exit(main())
