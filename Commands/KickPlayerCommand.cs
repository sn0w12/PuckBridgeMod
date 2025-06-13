using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PuckBridgeMod.Commands
{
    public class KickPlayerCommand : ICommand
    {
        public string CommandName => "kick_player";

        public void Execute(object payload)
        {
            var jPayload = payload as JObject;
            string steamId = jPayload?["steamid"]?.ToString();
            string reason = jPayload?["reason"]?.ToString() ?? "Kicked by administrator";
            bool applyTimeout = jPayload?["apply_timeout"]?.ToObject<bool>() ?? true;

            if (string.IsNullOrEmpty(steamId))
            {
                Util.Logger.Warning("Kick player command missing 'steamid' parameter");
                return;
            }

            PuckBridgeMod._instance?.QueueGameAction(() =>
            {
                try
                {
                    // Find the ServerManager instance
                    var serverManager = Object.FindFirstObjectByType<ServerManager>();
                    if (serverManager == null)
                    {
                        Util.Logger.Warning("ServerManager not found, cannot kick player");
                        return;
                    }

                    // Find player by Steam ID
                    var players = Object.FindObjectsByType<Player>(FindObjectsSortMode.None);
                    foreach (var player in players)
                    {
                        string playerSteamId = player.SteamId.Value.ToString();
                        if (playerSteamId.Equals(steamId, System.StringComparison.OrdinalIgnoreCase))
                        {
                            string identifier = $"{player.Username.Value} (Steam ID: {steamId})";
                            PuckBridgeMod._instance?.QueueSystemMessage($"{identifier} was kicked: {reason}");
                            serverManager.Server_KickPlayer(player, DisconnectionCode.Kicked, applyTimeout);

                            Util.Logger.Info($"Kicked player {identifier}: {reason}");
                            return;
                        }
                    }

                    Util.Logger.Warning($"Player with Steam ID '{steamId}' not found");
                }
                catch (System.Exception ex)
                {
                    Util.Logger.Error($"Failed to kick player with Steam ID '{steamId}'", ex);
                }
            });
        }
    }
}