using Newtonsoft.Json.Linq;
using UnityEngine;

namespace PuckBridgeMod.Commands
{
    public class RestartGameCommand : ICommand
    {
        public string CommandName => "restart_game";

        public void Execute(object payload)
        {
            var jPayload = payload as JObject;
            string reason = jPayload?["reason"]?.ToString() ?? "Game restarted by administrator";
            bool warmup = jPayload?["warmup"]?.ToObject<bool>() ?? true;
            int warmupTime = jPayload?["warmup_time"]?.ToObject<int>() ?? -1;

            PuckBridgeMod._instance?.QueueGameAction(() =>
            {
                try
                {
                    var gameManager = Object.FindFirstObjectByType<GameManager>();
                    if (gameManager != null)
                    {
                        PuckBridgeMod._instance?.QueueSystemMessage($"Game restarting: {reason}");
                        gameManager.Server_StartGame(warmup, warmupTime);

                        Util.Logger.Info($"Game restarted: {reason}, warmup: {warmup}, warmup_time: {warmupTime}");
                    }
                    else
                    {
                        Util.Logger.Warning("GameManager not found, cannot restart game");
                    }
                }
                catch (System.Exception ex)
                {
                    Util.Logger.Error("Failed to restart game", ex);
                }
            });
        }
    }
}