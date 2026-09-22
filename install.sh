#!/usr/bin/env bash
# Grave Zoom installer for Linux / Steam Deck.
#
# Run this script from inside the extracted Grave Zoom package (it expects
# a sibling "BepInEx/plugins/GraveZoom/GraveZoom.dll" next to itself). It will:
#   1. Find your Graveyard Keeper 2 install (any Steam library, incl. SD card).
#   2. Install BepInEx (Mono, x64) if it's not already present.
#   3. Install BepInEx Configuration Manager (the in-game F1 settings menu) if missing.
#   4. Copy Grave Zoom into place.
#   5. Set up the Steam launch option needed for BepInEx to load under Proton
#      - automatically on Steam Deck, or printed as instructions elsewhere.
#
# Nothing outside your Graveyard Keeper 2 folder is touched, except the one
# Steam launch-option edit on Steam Deck, which backs up the file it edits first.

set -euo pipefail

APPID=4358690
GAME_DIRNAME="Graveyard Keeper 2"
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" &>/dev/null && pwd)"
BUNDLED_MOD_DIR="$SCRIPT_DIR/BepInEx/plugins/GraveZoom"

log()  { printf '==> %s\n' "$*"; }
warn() { printf 'WARNING: %s\n' "$*" >&2; }
die()  { printf 'ERROR: %s\n' "$*" >&2; exit 1; }

# ---------------------------------------------------------------------------
# 0. Sanity checks
# ---------------------------------------------------------------------------

[ -f "$BUNDLED_MOD_DIR/GraveZoom.dll" ] || die \
  "GraveZoom.dll not found next to this script (expected: $BUNDLED_MOD_DIR/GraveZoom.dll).
Keep install.sh inside the extracted Grave Zoom package and run it from there."

for tool in curl unzip python3; do
  command -v "$tool" >/dev/null 2>&1 || die "'$tool' is required but not found. Please install it and re-run."
done

# ---------------------------------------------------------------------------
# 1. Locate the Graveyard Keeper 2 install across every configured Steam library
# ---------------------------------------------------------------------------

find_steam_root() {
  for candidate in "$HOME/.local/share/Steam" "$HOME/.steam/steam" "$HOME/.steam/root"; do
    [ -d "$candidate/steamapps" ] && { printf '%s' "$candidate"; return 0; }
  done
  return 1
}

# Reads libraryfolders.vdf and prints one library path per line.
list_steam_libraries() {
  local steam_root="$1"
  local vdf="$steam_root/steamapps/libraryfolders.vdf"
  printf '%s\n' "$steam_root"
  [ -f "$vdf" ] || return 0
  grep -oE '"path"\s+"[^"]+"' "$vdf" | sed -E 's/"path"\s+"([^"]+)"/\1/' | sed 's/\\\\/\//g'
}

find_game_dir() {
  local steam_root game_dir
  steam_root="$(find_steam_root)" || return 1
  while IFS= read -r lib; do
    game_dir="$lib/steamapps/common/$GAME_DIRNAME"
    [ -d "$game_dir" ] && { printf '%s' "$game_dir"; return 0; }
  done < <(list_steam_libraries "$steam_root")
  return 1
}

if [ "${1:-}" != "" ]; then
  GAME_DIR="$1"
else
  GAME_DIR="$(find_game_dir)" || die \
    "Could not find a '$GAME_DIRNAME' install in any Steam library.
Run this script again with the install path as an argument, e.g.:
  ./install.sh \"/path/to/Graveyard Keeper 2\""
fi

[ -f "$GAME_DIR/GraveyardKeeper2.exe" ] || die "'$GAME_DIR' doesn't look like a Graveyard Keeper 2 install (GraveyardKeeper2.exe not found)."
log "Game found: $GAME_DIR"

# ---------------------------------------------------------------------------
# 2. Install BepInEx if missing
# ---------------------------------------------------------------------------

install_bepinex() {
  log "Installing BepInEx (Mono, x64)..."
  local api="https://api.github.com/repos/BepInEx/BepInEx/releases"
  local asset_url
  asset_url="$(curl -fsSL "$api" | python3 -c '
import json, sys
releases = json.load(sys.stdin)
for rel in releases:
    if rel.get("tag_name", "").startswith("v5."):
        for asset in rel.get("assets", []):
            name = asset["name"]
            if "win_x64" in name and name.endswith(".zip"):
                print(asset["browser_download_url"])
                sys.exit(0)
sys.exit(1)
')" || die "Could not find a BepInEx 5 (win_x64) release asset."

  local tmp; tmp="$(mktemp -d)"
  curl -fsSL "$asset_url" -o "$tmp/bepinex.zip"
  unzip -q -o "$tmp/bepinex.zip" -d "$GAME_DIR"
  rm -rf "$tmp"
  log "BepInEx installed."
}

if [ -f "$GAME_DIR/BepInEx/core/BepInEx.dll" ]; then
  log "BepInEx already installed, skipping."
else
  install_bepinex
fi

# ---------------------------------------------------------------------------
# 3. Install BepInEx Configuration Manager if missing
# ---------------------------------------------------------------------------

install_config_manager() {
  log "Installing BepInEx Configuration Manager..."
  local api="https://api.github.com/repos/BepInEx/BepInEx.ConfigurationManager/releases/latest"
  local asset_url
  asset_url="$(curl -fsSL "$api" | python3 -c '
import json, sys
rel = json.load(sys.stdin)
for asset in rel.get("assets", []):
    name = asset["name"]
    if "BepInEx5" in name and name.endswith(".zip"):
        print(asset["browser_download_url"])
        sys.exit(0)
sys.exit(1)
')" || { warn "Could not find a BepInEx5 Configuration Manager release asset; skipping (optional)."; return 0; }

  local tmp; tmp="$(mktemp -d)"
  curl -fsSL "$asset_url" -o "$tmp/configmanager.zip"
  unzip -q -o "$tmp/configmanager.zip" -d "$GAME_DIR"
  rm -rf "$tmp"
  log "Configuration Manager installed (open with F1 in-game)."
}

if [ -f "$GAME_DIR/BepInEx/plugins/ConfigurationManager/ConfigurationManager.dll" ]; then
  log "Configuration Manager already installed, skipping."
else
  install_config_manager
fi

# ---------------------------------------------------------------------------
# 4. Deploy Grave Zoom
# ---------------------------------------------------------------------------

log "Installing Grave Zoom..."
mkdir -p "$GAME_DIR/BepInEx/plugins/GraveZoom"
cp "$BUNDLED_MOD_DIR/GraveZoom.dll" "$GAME_DIR/BepInEx/plugins/GraveZoom/"

# ---------------------------------------------------------------------------
# 5. Steam launch option (WINEDLLOVERRIDES) so BepInEx loads under Proton
# ---------------------------------------------------------------------------

LAUNCH_OPTION='WINEDLLOVERRIDES="winhttp=n,b" %command%'

is_steam_deck() {
  [ -f /etc/os-release ] && grep -qiE 'steamdeck|steamos' /etc/os-release
}

set_launch_option_steamdeck() {
  local steam_root userdata_dir cfg
  steam_root="$(find_steam_root)" || { warn "Could not locate Steam's data folder; skipping automatic launch option."; return 1; }
  userdata_dir="$steam_root/userdata"
  [ -d "$userdata_dir" ] || { warn "No Steam userdata folder found; skipping automatic launch option."; return 1; }

  if pgrep -x steam >/dev/null 2>&1 || pgrep -x steamwebhelper >/dev/null 2>&1; then
    warn "Steam is currently running. Close Steam fully, then re-run this script to set the launch option automatically."
    return 1
  fi

  local found=0
  for cfg in "$userdata_dir"/*/config/localconfig.vdf; do
    [ -f "$cfg" ] || continue
    found=1
    cp "$cfg" "$cfg.gravezoom-backup-$(date +%s)"
    python3 - "$cfg" "$APPID" "$LAUNCH_OPTION" <<'PYEOF'
import re, sys

path, appid, launch_option = sys.argv[1], sys.argv[2], sys.argv[3]
with open(path, "r", encoding="utf-8", errors="surrogateescape") as f:
    lines = f.readlines()

def find_block(lines, start):
    depth = 0
    for i in range(start, len(lines)):
        depth += lines[i].count("{") - lines[i].count("}")
        if depth <= 0 and "{" in "".join(lines[start:i + 1]):
            return i
    return len(lines) - 1

app_line = None
for i, line in enumerate(lines):
    if re.match(rf'^\s*"{re.escape(appid)}"\s*$', line):
        app_line = i
        break

if app_line is None:
    print(f"App {appid} not found in {path}; leaving file untouched.")
    sys.exit(1)

brace_line = app_line + 1
while brace_line < len(lines) and lines[brace_line].strip() != "{":
    brace_line += 1
end_line = find_block(lines, brace_line)

replaced = False
for i in range(brace_line + 1, end_line):
    if re.match(r'^\s*"LaunchOptions"\s*"', lines[i]):
        indent = lines[i][:len(lines[i]) - len(lines[i].lstrip())]
        lines[i] = f'{indent}"LaunchOptions"\t\t"{launch_option}"\n'
        replaced = True
        break

if not replaced:
    indent = lines[brace_line + 1][:len(lines[brace_line + 1]) - len(lines[brace_line + 1].lstrip())] if brace_line + 1 < end_line else "\t\t\t"
    lines.insert(brace_line + 1, f'{indent}"LaunchOptions"\t\t"{launch_option}"\n')

with open(path, "w", encoding="utf-8", errors="surrogateescape") as f:
    f.writelines(lines)

print(f"Launch option set for app {appid} in {path}")
PYEOF
  done

  [ "$found" -eq 1 ] || { warn "No localconfig.vdf found under $userdata_dir; skipping automatic launch option."; return 1; }
  log "Launch option set automatically. Restart Steam for it to take effect."
  return 0
}

if is_steam_deck; then
  log "Steam Deck detected, attempting to set the launch option automatically..."
  if ! set_launch_option_steamdeck; then
    warn "Automatic setup failed. Set it manually instead:"
    printf '  Steam > Graveyard Keeper 2 > Properties > General > Launch Options:\n  %s\n' "$LAUNCH_OPTION"
  fi
else
  log "Non-Deck Linux system detected. Set this manually:"
  printf '  Steam > Graveyard Keeper 2 > Properties > General > Launch Options:\n  %s\n' "$LAUNCH_OPTION"
fi

log "Done. Launch the game once so BepInEx generates its config, then Page Up/Down/Home to zoom."
