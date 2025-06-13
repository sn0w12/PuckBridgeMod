using System;
using HarmonyLib;

namespace PuckBridgeMod.Patches
{
    [HarmonyPatch(typeof(Player), "OnNetworkSpawn")]
    public class PlayerSpawnPatch
    {
        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            try
            {
                if (PuckBridgeMod._instance?.Client != null)
                {
                    var playerData = PlayerPatchHelpers.CreatePlayerData(__instance, "player_spawned");
                    PuckBridgeMod._instance.Client.SendPlayerData(playerData);
                }
            }
            catch (Exception e)
            {
                Util.Logger.Error("Failed to send player spawn data", e);
            }
        }
    }

    [HarmonyPatch(typeof(Player), "OnNetworkDespawn")]
    public class PlayerDespawnPatch
    {
        [HarmonyPrefix]
        static void Prefix(Player __instance)
        {
            try
            {
                if (PuckBridgeMod._instance?.Client != null)
                {
                    var playerData = PlayerPatchHelpers.CreatePlayerData(__instance, "player_despawned");
                    PuckBridgeMod._instance.Client.SendPlayerData(playerData);
                }
            }
            catch (Exception e)
            {
                Util.Logger.Error("Failed to send player despawn data", e);
            }
        }
    }

    [HarmonyPatch(typeof(Player), "OnPlayerStateChanged")]
    public class PlayerStateChangedPatch
    {
        [HarmonyPostfix]
        static void Postfix(PlayerState oldState, PlayerState newState, Player __instance)
        {
            try
            {
                if (PuckBridgeMod._instance?.Client != null)
                {
                    var stateChangeData = new
                    {
                        category = "player_state_changed",
                        clientId = __instance.OwnerClientId,
                        username = __instance.Username.Value.ToString(),
                        oldState = oldState.ToString(),
                        newState = newState.ToString(),
                        player = PlayerPatchHelpers.CreatePlayerSummary(__instance)
                    };

                    PuckBridgeMod._instance.Client.SendPlayerData(stateChangeData);
                }
            }
            catch (Exception e)
            {
                Util.Logger.Error("Failed to send player state change data", e);
            }
        }
    }

    // Helper methods shared across patches
    public static class PlayerPatchHelpers
    {
        public static object CreatePlayerData(Player player, string category)
        {
            return new
            {
                category = category,
                player = CreateFullPlayerData(player)
            };
        }

        public static object CreateFullPlayerData(Player player)
        {
            return new
            {
                clientId = player.OwnerClientId,
                state = player.State.Value.ToString(),
                username = player.Username.Value.ToString(),
                number = player.Number.Value,
                team = player.Team.Value.ToString(),
                role = player.Role.Value.ToString(),
                handedness = player.Handedness.Value.ToString(),
                stats = new
                {
                    goals = player.Goals.Value,
                    assists = player.Assists.Value,
                    ping = player.Ping.Value
                },
                profile = new
                {
                    country = player.Country.Value.ToString(),
                    steamId = player.SteamId.Value.ToString(),
                    patreonLevel = player.PatreonLevel.Value,
                    adminLevel = player.AdminLevel.Value
                }
            };
        }

        public static object CreatePlayerSummary(Player player)
        {
            return new
            {
                clientId = player.OwnerClientId,
                username = player.Username.Value.ToString(),
                state = player.State.Value.ToString(),
                team = player.Team.Value.ToString(),
                role = player.Role.Value.ToString(),
                number = player.Number.Value,
                goals = player.Goals.Value,
                assists = player.Assists.Value,
                ping = player.Ping.Value
            };
        }
    }
}