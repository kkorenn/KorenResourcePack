#!/usr/bin/env python3
"""
Dump every codepoint contained in each TTF/OTF under Assets/Font/ to a matching
.txt file in Assets/Font/_charsets/. Use the produced .txt with Unity TextMeshPro
Font Asset Creator > Character Set: "Characters from File".

Drops Hangul ranges from Maplestory Bold so its atlas stays small (CJK Hangul is
~11k glyphs and would otherwise force an 8K atlas).

Usage:
    python3 tools/dump_charsets.py            # process every font
    python3 tools/dump_charsets.py FontA.ttf  # process one font
"""

import os
import sys

try:
    from fontTools.ttLib import TTFont
except ImportError:
    sys.exit("fonttools missing. Install: pip3 install fonttools")

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
FONTS_DIR = os.path.join(REPO, "KorenResourcePack-Unity", "Assets", "Font")
OUT_DIR = os.path.join(FONTS_DIR, "_charsets")

# Ranges to drop per font name (case-insensitive substring match against filename).
# Hangul = ~11k Korean syllables; keeping symbols + Latin is plenty for the keyviewer.
DROP_RANGES = {
    "maplestory": [
        (0x1100, 0x11FF),  # Hangul Jamo
        (0x3130, 0x318F),  # Hangul Compatibility Jamo
        (0xA960, 0xA97F),  # Hangul Jamo Extended-A
        (0xAC00, 0xD7A3),  # Hangul Syllables
        (0xD7B0, 0xD7FF),  # Hangul Jamo Extended-B
    ],
}


def in_drop_ranges(cp, drop):
    return any(lo <= cp <= hi for lo, hi in drop)


def dump(path):
    base = os.path.basename(path)
    stem = os.path.splitext(base)[0]
    drop = []
    for key, ranges in DROP_RANGES.items():
        if key in base.lower():
            drop = ranges
            break

    font = TTFont(path)
    cmap = font.getBestCmap() or {}
    cps = sorted(
        cp for cp in cmap.keys()
        # 0x20-0xFFFF is the BMP printable range; skip DEL (0x7F) and any dropped ranges.
        # Going past 0xFFFF would require UTF-16 surrogate pairs which TMP handles poorly.
        if 0x20 <= cp <= 0xFFFF and cp != 0x7F and not in_drop_ranges(cp, drop)
    )

    os.makedirs(OUT_DIR, exist_ok=True)
    out_path = os.path.join(OUT_DIR, stem + ".txt")
    with open(out_path, "w", encoding="utf-8") as f:
        f.write("".join(chr(cp) for cp in cps))

    note = f" (dropped {sum(hi - lo + 1 for lo, hi in drop)} codepoints)" if drop else ""
    print(f"{base}: {len(cps)} glyphs{note} -> {out_path}")


def main(argv):
    if len(argv) > 1:
        for name in argv[1:]:
            path = name if os.path.isabs(name) else os.path.join(FONTS_DIR, name)
            dump(path)
        return

    for f in sorted(os.listdir(FONTS_DIR)):
        if f.lower().endswith((".ttf", ".otf")):
            dump(os.path.join(FONTS_DIR, f))


if __name__ == "__main__":
    main(sys.argv)
