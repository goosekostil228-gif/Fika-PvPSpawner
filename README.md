# Fika PvP Spawner (SPT 4.1.x)

A lightweight client-side BepInEx plugin designed for **Fika Co-op** that splits squad members across different official PMC spawn points at the beginning of a raid, allowing you to play custom PvP matches with your friends.

### Features
* **Automatic Squad Splitting:** Intercepts `GameWorld.OnGameStarted` and relocates players to random official PMC spawn points (`ESpawnCategoryMask.Player`) across the map.
* **Distance Filtering:** Ensures you spawn at least `150m` (configurable up to `1000m`) away from other human players in the session. Automatically falls back to the furthest possible spawn point on smaller maps like Factory.
* **Network Sync Safe:** Includes a configurable post-start delay (`1.5s` by default) so Fika's initial spawn synchronization doesn't snap players back to the host.
* **Manual Respawn Hotkey:** Press `F9` (configurable) at any time to relocate to a random distant PMC spawn point (useful for late loaders).
* **Easy Toggle:** Switch between Co-op and PvP modes on the fly via the `F12` BepInEx Configuration Manager without removing the mod.

### Installation
1. Download the archive and extract the `BepInEx` folder into your root SPT directory (`BepInEx/plugins/FikaPvPSpawner.dll`).
2. **All players** in the PvP match should have the plugin installed.
3. *(Recommended for PvP)* Open `F12` -> **Fika.Core** settings and disable **Nameplates / Friendly ESP** so you cannot see each other through walls.