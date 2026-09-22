#!/usr/bin/env bats
# Tests for install.sh. Run with: bats install.bats
#
# install.sh is sourced (not executed) in every test, which is safe because
# all of its logic lives inside functions guarded by a `[[ "${BASH_SOURCE[0]}"
# == "${0}" ]]` check at the bottom of the file - sourcing it only defines
# functions and variables, nothing runs automatically.

setup() {
  SCRIPT="$BATS_TEST_DIRNAME/install.sh"
  export TEST_HOME="$BATS_TEST_TMPDIR/home"
  mkdir -p "$TEST_HOME"
  export HOME="$TEST_HOME"
  source "$SCRIPT"
}

# --- find_steam_root -------------------------------------------------------

@test "find_steam_root: finds the default Linux Steam location" {
  mkdir -p "$HOME/.local/share/Steam/steamapps"
  run find_steam_root
  [ "$status" -eq 0 ]
  [ "$output" = "$HOME/.local/share/Steam" ]
}

@test "find_steam_root: falls back to ~/.steam/steam" {
  mkdir -p "$HOME/.steam/steam/steamapps"
  run find_steam_root
  [ "$status" -eq 0 ]
  [ "$output" = "$HOME/.steam/steam" ]
}

@test "find_steam_root: fails when no Steam install exists" {
  run find_steam_root
  [ "$status" -ne 0 ]
}

# --- list_steam_libraries ---------------------------------------------------

@test "list_steam_libraries: always includes the Steam root itself" {
  mkdir -p "$HOME/.local/share/Steam/steamapps"
  run list_steam_libraries "$HOME/.local/share/Steam"
  [ "$status" -eq 0 ]
  [[ "$output" == *"$HOME/.local/share/Steam"* ]]
}

@test "list_steam_libraries: parses extra library paths from libraryfolders.vdf" {
  local steam_root="$HOME/.local/share/Steam"
  mkdir -p "$steam_root/steamapps"
  cat > "$steam_root/steamapps/libraryfolders.vdf" <<EOF
"libraryfolders"
{
	"0"
	{
		"path"		"$steam_root"
	}
	"1"
	{
		"path"		"/mnt/sdcard/SteamLibrary"
	}
}
EOF
  run list_steam_libraries "$steam_root"
  [ "$status" -eq 0 ]
  [[ "$output" == *"/mnt/sdcard/SteamLibrary"* ]]
}

# --- find_game_dir -----------------------------------------------------------

@test "find_game_dir: finds the game in the default library" {
  mkdir -p "$HOME/.local/share/Steam/steamapps/common/Graveyard Keeper 2"
  run find_game_dir
  [ "$status" -eq 0 ]
  [ "$output" = "$HOME/.local/share/Steam/steamapps/common/Graveyard Keeper 2" ]
}

@test "find_game_dir: finds the game in a secondary library (e.g. SD card)" {
  local steam_root="$HOME/.local/share/Steam"
  local second_lib="$BATS_TEST_TMPDIR/sdcard/SteamLibrary"
  mkdir -p "$steam_root/steamapps"
  mkdir -p "$second_lib/steamapps/common/Graveyard Keeper 2"
  cat > "$steam_root/steamapps/libraryfolders.vdf" <<EOF
"libraryfolders"
{
	"0"
	{
		"path"		"$steam_root"
	}
	"1"
	{
		"path"		"$second_lib"
	}
}
EOF
  run find_game_dir
  [ "$status" -eq 0 ]
  [ "$output" = "$second_lib/steamapps/common/Graveyard Keeper 2" ]
}

@test "find_game_dir: fails when the game isn't installed anywhere" {
  mkdir -p "$HOME/.local/share/Steam/steamapps"
  run find_game_dir
  [ "$status" -ne 0 ]
}

# --- is_steam_deck -----------------------------------------------------------

@test "is_steam_deck: true when os-release identifies as SteamOS" {
  export OS_RELEASE_FILE="$BATS_TEST_TMPDIR/os-release"
  printf 'ID=steamos\nVARIANT_ID=steamdeck\n' > "$OS_RELEASE_FILE"
  run is_steam_deck
  [ "$status" -eq 0 ]
}

@test "is_steam_deck: false on a generic distro" {
  export OS_RELEASE_FILE="$BATS_TEST_TMPDIR/os-release"
  printf 'ID=arch\n' > "$OS_RELEASE_FILE"
  run is_steam_deck
  [ "$status" -ne 0 ]
}

@test "is_steam_deck: false when os-release doesn't exist" {
  export OS_RELEASE_FILE="$BATS_TEST_TMPDIR/does-not-exist"
  run is_steam_deck
  [ "$status" -ne 0 ]
}

# --- patch_vdf_launch_options -------------------------------------------------

vdf_fixture() {
  cat > "$1" <<'EOF'
"UserLocalConfigStore"
{
	"Software"
	{
		"Valve"
		{
			"Steam"
			{
				"apps"
				{
					"12345"
					{
						"LastPlayed"		"111"
					}
					"4358690"
					{
						"LastPlayed"		"456"
					}
					"999999"
					{
						"LastPlayed"		"789"
						"LaunchOptions"		"-someflag"
					}
				}
			}
		}
	}
}
EOF
}

@test "patch_vdf_launch_options: inserts LaunchOptions when absent" {
  local cfg="$BATS_TEST_TMPDIR/localconfig.vdf"
  vdf_fixture "$cfg"
  patch_vdf_launch_options "$cfg" "4358690" 'WINEDLLOVERRIDES="winhttp=n,b" %command%'
  grep -A3 '"4358690"' "$cfg" | grep -q '"LaunchOptions"'
}

@test "patch_vdf_launch_options: escapes embedded quotes so the VDF stays valid" {
  local cfg="$BATS_TEST_TMPDIR/localconfig.vdf"
  vdf_fixture "$cfg"
  patch_vdf_launch_options "$cfg" "4358690" 'WINEDLLOVERRIDES="winhttp=n,b" %command%'
  # The value must contain escaped quotes (\"), never a bare unescaped " before %command%.
  run grep -F '\"winhttp=n,b\"' "$cfg"
  [ "$status" -eq 0 ]
  run grep -F '"WINEDLLOVERRIDES="winhttp' "$cfg"
  [ "$status" -ne 0 ]
}

@test "patch_vdf_launch_options: replaces an existing LaunchOptions value" {
  local cfg="$BATS_TEST_TMPDIR/localconfig.vdf"
  vdf_fixture "$cfg"
  patch_vdf_launch_options "$cfg" "999999" 'NEWVALUE'
  run grep -A4 '"999999"' "$cfg"
  [[ "$output" == *'"LaunchOptions"'*'"NEWVALUE"'* ]]
  [[ "$output" != *'-someflag'* ]]
}

@test "patch_vdf_launch_options: only touches the target appid's block" {
  local cfg="$BATS_TEST_TMPDIR/localconfig.vdf"
  vdf_fixture "$cfg"
  patch_vdf_launch_options "$cfg" "4358690" 'SOMEVALUE'
  # Untouched apps keep exactly their original content.
  run grep -A1 '"12345"' "$cfg"
  [[ "$output" != *'LaunchOptions'* ]]
  run grep -A4 '"999999"' "$cfg"
  [[ "$output" == *'-someflag'* ]]
}

@test "patch_vdf_launch_options: fails cleanly when the appid isn't in the file" {
  local cfg="$BATS_TEST_TMPDIR/localconfig.vdf"
  vdf_fixture "$cfg"
  run patch_vdf_launch_options "$cfg" "424242" 'SOMEVALUE'
  [ "$status" -ne 0 ]
  # File must be left byte-for-byte untouched on failure.
  run grep -c 'LaunchOptions' "$cfg"
  [ "$output" -eq 1 ]
}

# --- main() sanity checks ---------------------------------------------------

@test "main: dies with a clear message when GraveZoom.dll is missing" {
  cd "$BATS_TEST_TMPDIR"
  cp "$SCRIPT" ./install.sh
  run bash ./install.sh
  [ "$status" -ne 0 ]
  [[ "$output" == *"GraveZoom.dll not found"* ]]
}

@test "main: dies with a clear message when the given path isn't a game install" {
  cd "$BATS_TEST_TMPDIR"
  mkdir -p BepInEx/plugins/GraveZoom
  touch BepInEx/plugins/GraveZoom/GraveZoom.dll
  cp "$SCRIPT" ./install.sh
  mkdir -p not_the_game
  run bash ./install.sh ./not_the_game
  [ "$status" -ne 0 ]
  [[ "$output" == *"doesn't look like a Graveyard Keeper 2 install"* ]]
}

@test "main: accepts an explicit game path and deploys GraveZoom.dll" {
  cd "$BATS_TEST_TMPDIR"
  mkdir -p BepInEx/plugins/GraveZoom
  echo "fake dll contents" > BepInEx/plugins/GraveZoom/GraveZoom.dll
  cp "$SCRIPT" ./install.sh

  mkdir -p game/BepInEx/core game/BepInEx/plugins/ConfigurationManager
  touch game/GraveyardKeeper2.exe
  touch game/BepInEx/core/BepInEx.dll
  touch game/BepInEx/plugins/ConfigurationManager/ConfigurationManager.dll

  run bash ./install.sh ./game
  [ "$status" -eq 0 ]
  [ -f game/BepInEx/plugins/GraveZoom/GraveZoom.dll ]
  diff BepInEx/plugins/GraveZoom/GraveZoom.dll game/BepInEx/plugins/GraveZoom/GraveZoom.dll
  [[ "$output" == *"BepInEx already installed, skipping."* ]]
  [[ "$output" == *"Configuration Manager already installed, skipping."* ]]
  [[ "$output" == *"Installing Grave Zoom..."* ]]
}
