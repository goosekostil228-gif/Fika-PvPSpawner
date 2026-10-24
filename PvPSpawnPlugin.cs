using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using EFT;
using EFT.Game.Spawning;
using HarmonyLib;
using UnityEngine;

namespace FikaPvPSpawner
{
    [BepInPlugin("com.custom.fikapvpspawner", "Fika PvP Spawner", "1.1.0")]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.SoftDependency)]
    public class PvPSpawnPlugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        internal static PvPSpawnPlugin Instance;

        internal static ConfigEntry<bool> EnablePvPSpawns;
        internal static ConfigEntry<float> MinDistanceBetweenPlayers;
        internal static ConfigEntry<float> SpawnDelaySeconds;
        internal static ConfigEntry<KeyCode> ManualRespawnKey;

        private void Awake()
        {
            Log = Logger;
            Instance = this;

            EnablePvPSpawns = Config.Bind(
                "1. General",
                "Enable PvP Spawns",
                true,
                "Automatically relocate squad members to separate PMC spawn points at the start of the raid."
            );

            MinDistanceBetweenPlayers = Config.Bind(
                "1. General",
                "Min Distance (Meters)",
                150f,
                new ConfigDescription(
                    "Minimum distance (in meters) from other human players when selecting a random PMC spawn point.",
                    new AcceptableValueRange<float>(20f, 1000f)
                )
            );

            SpawnDelaySeconds = Config.Bind(
                "1. General",
                "Auto-Teleport Delay (Sec)",
                1.5f,
                new ConfigDescription(
                    "Delay after raid start before relocating the player (prevents Fika network sync from snapping players back).",
                    new AcceptableValueRange<float>(0.5f, 10f)
                )
            );

            ManualRespawnKey = Config.Bind(
                "2. Controls",
                "Randomize Spawn Key",
                KeyCode.F9,
                "Hotkey to manually teleport to a random distant PMC spawn point."
            );

            new Harmony("com.custom.fikapvpspawner").PatchAll();
            Log.LogInfo("Fika PvP Spawner v1.1.0 loaded successfully!");
        }

        private void Update()
        {
            if (EnablePvPSpawns.Value && Input.GetKeyDown(ManualRespawnKey.Value))
            {
                RelocateLocalPlayer();
            }
        }

        public void TriggerDelayedRelocation()
        {
            StartCoroutine(DelayedRelocateCoroutine());
        }

        private IEnumerator DelayedRelocateCoroutine()
        {
            yield return new WaitForSeconds(SpawnDelaySeconds.Value);
            RelocateLocalPlayer();
        }

        public static void RelocateLocalPlayer()
        {
            if (!Singleton<GameWorld>.Instantiated) return;

            var gameWorld = Singleton<GameWorld>.Instance;
            var localPlayer = gameWorld.MainPlayer;
            if (localPlayer == null || !localPlayer.IsYourPlayer) return;

            // Find all official spawn markers on the current map
            var allSpawnMarkers = UnityEngine.Object.FindObjectsOfType<SpawnPointMarker>();
            if (allSpawnMarkers == null || allSpawnMarkers.Length == 0)
            {
                Log.LogWarning("[FikaPvPSpawner] No SpawnPointMarkers found on the map!");
                return;
            }

            // Filter only valid PMC (Player) spawn positions using bitwise flag check
            var pmcSpawns = allSpawnMarkers
                .Where(m => m.SpawnPoint != null && (m.SpawnPoint.Categories & ESpawnCategoryMask.Player) != 0)
                .Select(m => m.SpawnPoint.Position)
                .ToList();

            // Fallback in case custom map markers don't have standard Player masks
            if (pmcSpawns.Count == 0)
            {
                pmcSpawns = allSpawnMarkers.Select(m => m.transform.position).ToList();
            }

            // Collect positions of all other human players currently in the raid
            var otherPlayersPositions = gameWorld.AllAlivePlayersList
                .Where(p => p != null && p.Id != localPlayer.Id && !p.IsAI)
                .Select(p => p.Position)
                .ToList();

            Vector3 chosenSpawn;

            if (otherPlayersPositions.Count > 0)
            {
                // Filter spawns that meet the minimum distance requirement from all other human players
                var validSpawns = pmcSpawns
                    .Where(pos => otherPlayersPositions.All(otherPos => Vector3.Distance(pos, otherPos) >= MinDistanceBetweenPlayers.Value))
                    .ToList();

                if (validSpawns.Count > 0)
                {
                    chosenSpawn = validSpawns[UnityEngine.Random.Range(0, validSpawns.Count)];
                }
                else
                {
                    // Fallback for small maps (e.g., Factory): pick the furthest available spawn point
                    chosenSpawn = pmcSpawns
                        .OrderByDescending(pos => otherPlayersPositions.Min(otherPos => Vector3.Distance(pos, otherPos)))
                        .First();
                }
            }
            else
            {
                // If local player initialized first, pick any random PMC spawn away from default group spawn
                chosenSpawn = pmcSpawns[UnityEngine.Random.Range(0, pmcSpawns.Count)];
            }

            // Slight Y-axis offset to prevent clipping through terrain colliders
            Vector3 safePosition = chosenSpawn + new Vector3(0f, 0.25f, 0f);

            localPlayer.Teleport(safePosition, true);
            Log.LogInfo($"[FikaPvPSpawner] Player relocated to PMC spawn: {safePosition}");
        }
    }

    [HarmonyPatch(typeof(GameWorld), nameof(GameWorld.OnGameStarted))]
    public static class GameStartedPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (PvPSpawnPlugin.EnablePvPSpawns.Value && PvPSpawnPlugin.Instance != null)
            {
                PvPSpawnPlugin.Instance.TriggerDelayedRelocation();
            }
        }
    }
}