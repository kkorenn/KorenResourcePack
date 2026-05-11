#!/bin/bash
set -e
cd /Users/koren/Documents/KorenResourcePack/src

# ============================================================
# 1. Settings.cs — unnest Settings class from partial class Main
# ============================================================
# Remove the `public static partial class Main` wrapper + its closing `}`
sed -i '' '/^    public static partial class Main$/d' Settings.cs
# Remove the opening brace of partial class Main (line after the removed declaration)
sed -i '' '/^    {$/{ N; /^    {\n        public class Settings/{ s/^    {\n/    /; }; }' Settings.cs
# Simpler approach: just remove the standalone `    {` line right before Settings class
# Actually let me just do it line by line

# Let me use a different approach - use awk to transform the files
# For Settings.cs: remove the "public static partial class Main" + "{" lines and the matching "}"

# ============================================================
# Process each file with a Python script for reliability
# ============================================================
python3 << 'PYEOF'
import re, os

def transform_file(filepath, new_class_line, extra_replacements=None):
    """Remove partial class Main wrapper, add new class line, fix references."""
    with open(filepath, 'r') as f:
        lines = f.readlines()
    
    # Find the "public static partial class Main" line
    partial_idx = None
    partial_brace_idx = None
    for i, line in enumerate(lines):
        if 'public static partial class Main' in line:
            partial_idx = i
            # Find the opening brace (next line or same line)
            for j in range(i+1, min(i+3, len(lines))):
                if lines[j].strip() == '{':
                    partial_brace_idx = j
                    break
            break
    
    if partial_idx is None:
        print(f"  No partial class Main found in {filepath}")
        return
    
    # Find the matching closing brace
    # It's the second-to-last '}' in the file (last one closes namespace)
    brace_positions = [i for i, line in enumerate(lines) if line.strip() == '}']
    if len(brace_positions) < 2:
        print(f"  Can't find closing brace in {filepath}")
        return
    closing_brace_idx = brace_positions[-2]
    
    # Build new file
    new_lines = []
    for i, line in enumerate(lines):
        if i == partial_idx:
            if new_class_line:
                new_lines.append(new_class_line + '\n')
            continue
        if i == partial_brace_idx:
            if new_class_line:
                new_lines.append('    {\n')
            continue
        if i == closing_brace_idx:
            if new_class_line:
                new_lines.append('    }\n')
            continue
        new_lines.append(line)
    
    # Apply extra replacements
    if extra_replacements:
        content = ''.join(new_lines)
        for pattern, replacement in extra_replacements:
            content = re.sub(pattern, replacement, content)
        new_lines = content.splitlines(True)
    
    with open(filepath, 'w') as f:
        f.writelines(new_lines)
    print(f"  Transformed {filepath}")

# Helper: create replacements for settings/mod/etc references
# We need word-boundary-safe replacements

def make_replacements(*fields):
    """Create regex replacements for unqualified field access -> Main.field"""
    repls = []
    for field in fields:
        # Match 'field' that is NOT preceded by 'Main.' or another letter/underscore
        # and IS followed by typical access patterns
        repls.append(
            (r'(?<![A-Za-z_.])(' + re.escape(field) + r')(?=[.\s;,\)\]?!])', r'Main.\1')
        )
    return repls

# --- Settings.cs: just remove the wrapper, no class line needed (Settings stays as-is) ---
print("Settings.cs:")
transform_file('Settings.cs', None)  # None = remove wrapper entirely

# --- Font.cs ---
print("Font.cs:")
font_repls = make_replacements('settings', 'preferredHudFont')
# mod references: mod?.Logger, mod.Path — but NOT modEnabled, modEntry
font_repls.append((r'(?<![A-Za-z_.])mod(?=[\?.])', r'Main.mod'))
transform_file('Font.cs', '    internal static class FontLoader', font_repls)

# --- Status.cs ---
print("Status.cs:")
status_repls = make_replacements('settings')
status_repls.append((r'(?<![A-Za-z_.])mod(?=[\?.])', r'Main.mod'))
transform_file('Status.cs', '    internal static class Status', status_repls)

# --- PlayCount.cs ---
print("PlayCount.cs:")
pc_repls = make_replacements('settings')
pc_repls.append((r'(?<![A-Za-z_.])mod(?=[\?.])', r'Main.mod'))
# modEnabled reference
pc_repls.append((r'(?<![A-Za-z_.])modEnabled(?![A-Za-z_])', r'Main.modEnabled'))
transform_file('PlayCount.cs', '    internal static class PlayCount', pc_repls)

# --- Overlay.cs ---
print("Overlay.cs:")
ov_repls = make_replacements('settings', 'perfectCombo', 'runVisible')
ov_repls.append((r'(?<![A-Za-z_.])mod(?=[\?.])', r'Main.mod'))
# playDatas, lastMapHash, startProgress, GetPlayData, GetCurrentMultiplier are 
# currently in partial class Main (PlayCount.cs). After extraction they'll be in PlayCount.
ov_repls.append((r'(?<![A-Za-z_.])playDatas(?![A-Za-z_])', r'PlayCount.playDatas'))
ov_repls.append((r'(?<![A-Za-z_.])lastMapHash(?![A-Za-z_])', r'PlayCount.lastMapHash'))
ov_repls.append((r'(?<![A-Za-z_.])startProgress(?![A-Za-z_])', r'PlayCount.startProgress'))
ov_repls.append((r'(?<![A-Za-z_.])GetPlayData(?=\()', r'PlayCount.GetPlayData'))
ov_repls.append((r'(?<![A-Za-z_.])GetCurrentMultiplier(?=\()', r'PlayCount.GetCurrentMultiplier'))
ov_repls.append((r'(?<![A-Za-z_.])FormatProgressRange(?=\()', r'Status.FormatProgressRange'))
ov_repls.append((r'(?<![A-Za-z_.])FormatPercent(?=\()', r'Status.FormatPercent'))
# overlayBuilt stays in Overlay (it's defined there)
transform_file('Overlay.cs', '    internal static class Overlay', ov_repls)

# --- KeyViewer.cs ---
print("KeyViewer.cs:")
kv_repls = make_replacements('settings')
kv_repls.append((r'(?<![A-Za-z_.])mod(?=[\?.])', r'Main.mod'))
transform_file('KeyViewer.cs', '    internal static class KeyViewer', kv_repls)

# --- SettingsGui.cs ---
print("SettingsGui.cs:")
sg_repls = make_replacements('settings')
sg_repls.append((r'(?<![A-Za-z_.])mod(?=[\?.])', r'Main.mod'))
# keyViewerKeys is in KeyViewer now
sg_repls.append((r'(?<![A-Za-z_.])keyViewerKeys(?![A-Za-z_])', r'KeyViewer.keyViewerKeys'))
# InvalidatePercentCaches moves to Status
sg_repls.append((r'(?<![A-Za-z_.])InvalidatePercentCaches(?=\()', r'Status.InvalidatePercentCaches'))
transform_file('SettingsGui.cs', '    internal static class SettingsGui', sg_repls)

# --- Main.cs: remove 'partial' keyword ---
print("Main.cs:")
with open('Main.cs', 'r') as f:
    content = f.read()
content = content.replace('public static partial class Main', 'public static class Main')
with open('Main.cs', 'w') as f:
    f.write(content)
print("  Removed 'partial' from Main.cs")

print("\nDone with structural transforms!")
PYEOF
