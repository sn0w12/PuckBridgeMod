using System;
using System.Collections.Generic;
using HarmonyLib;

namespace PuckBridgeMod.Patches
{
    [HarmonyPatch]
    public class GameStatePatch
    {
        [HarmonyPatch(typeof(ScoreboardController), "Event_OnGameStateChanged")]
        [HarmonyPostfix]
        static void Postfix(Dictionary<string, object> message)
        {
            try
            {
                if (message.ContainsKey("newGameState") && PuckBridgeMod._instance?.Client != null)
                {
                    GameState gameState = (GameState)message["newGameState"];

                    var gameStateData = new
                    {
                        category = "game_state",
                        phase = gameState.Phase.ToString(),
                        time = gameState.Time,
                        period = gameState.Period,
                        scores = new
                        {
                            blue = gameState.BlueScore,
                            red = gameState.RedScore
                        },
                    };

                    PuckBridgeMod._instance.Client.SendGameStateData(gameStateData);
                }
            }
            catch (Exception e)
            {
                Util.Logger.Error("Failed to send game state data", e);
            }
        }
    }

    [HarmonyPatch]
    public class GoalScoredPatch
    {
        [HarmonyPatch(typeof(GameManager), "Server_GoalScored")]
        [HarmonyPostfix]
        static void Postfix(PlayerTeam team, Player lastPlayer, Player goalPlayer, Player assistPlayer, Player secondAssistPlayer, Puck puck, GameManager __instance)
        {
            try
            {
                if (PuckBridgeMod._instance?.Client != null)
                {
                    var goalData = new
                    {
                        category = "goal_scored",
                        team = team.ToString(),
                        scores = new
                        {
                            blue = __instance.GameState.Value.BlueScore,
                            red = __instance.GameState.Value.RedScore
                        },
                        players = new
                        {
                            goal = goalPlayer != null ? new
                            {
                                username = goalPlayer.Username.Value.ToString(),
                                clientId = goalPlayer.OwnerClientId
                            } : null,
                            assist = assistPlayer != null ? new
                            {
                                username = assistPlayer.Username.Value.ToString(),
                                clientId = assistPlayer.OwnerClientId
                            } : null,
                            secondAssist = secondAssistPlayer != null ? new
                            {
                                username = secondAssistPlayer.Username.Value.ToString(),
                                clientId = secondAssistPlayer.OwnerClientId
                            } : null,
                            last = lastPlayer != null ? new
                            {
                                username = lastPlayer.Username.Value.ToString(),
                                clientId = lastPlayer.OwnerClientId
                            } : null
                        },
                        puck = new
                        {
                            speed = puck.Speed,
                            shotSpeed = puck.ShotSpeed
                        },
                        gameState = new
                        {
                            phase = __instance.GameState.Value.Phase.ToString(),
                            time = __instance.GameState.Value.Time,
                            period = __instance.GameState.Value.Period
                        }
                    };

                    PuckBridgeMod._instance.Client.SendGoalData(goalData);
                }
            }
            catch (Exception e)
            {
                Util.Logger.Error("Failed to send goal data", e);
            }
        }
    }
}