using System;
using HarmonyLib;
using UnityEngine;
using PuckBridgeMod.Commands;
using PuckBridgeMod.Core;

namespace PuckBridgeMod {
    public class PuckBridgeMod : IPuckMod
    {
        private BridgeClient _client;
        internal static PuckBridgeMod _instance;
        static readonly Harmony harmony = new Harmony("sn0w12.puckbridgemod");
        private bool _bridgeStarted = false;
        private MessageProcessor _messageProcessor;
        private CommandHandler _commandHandler;

        internal BridgeClient Client => _client;
        internal CommandHandler CommandHandler => _commandHandler;

        internal void QueueSystemMessage(string message) => _messageProcessor.QueueSystemMessage(message);
        internal void QueueGameAction(System.Action action) => _messageProcessor.QueueGameAction(action);

        private ushort GetBridgePortFromArgs()
        {
            string[] args = System.Environment.GetCommandLineArgs();

            // Look for -bridgeport argument
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].ToLower() == "-bridgeport")
                {
                    if (ushort.TryParse(args[i + 1], out ushort port))
                    {
                        Util.Logger.Info($"Bridge port set from command line: {port}");
                        return port;
                    }
                    else
                    {
                        Util.Logger.Warning($"Invalid bridge port argument: {args[i + 1]}");
                    }
                }
            }

            // Default port
            return 9000;
        }

        private void StartBridge()
        {
            if (_bridgeStarted) return;

            ushort bridgePort = GetBridgePortFromArgs();
            Util.Logger.Info($"Starting bridge on port {bridgePort}...");

            _client = new BridgeClient("127.0.0.1", bridgePort);
            _client.Start();
            _bridgeStarted = true;
        }

        private void ApplyPatchesSafely()
        {
            try
            {
                // Try to patch each class individually to identify problematic patches
                Util.Logger.Info("Applying GameState patches...");
                harmony.PatchAll(typeof(Patches.GameStatePatch));
                harmony.PatchAll(typeof(Patches.GoalScoredPatch));
                Util.Logger.Info("GameState patches applied successfully");

                Util.Logger.Info("Applying Player patches...");
                harmony.PatchAll(typeof(Patches.PlayerSpawnPatch));
                harmony.PatchAll(typeof(Patches.PlayerDespawnPatch));
                harmony.PatchAll(typeof(Patches.PlayerStateChangedPatch));
                Util.Logger.Info("Player patches applied successfully");

                Util.Logger.Info("Applying ServerManager patches...");
                harmony.PatchAll(typeof(Patches.IMetricsPatch));
                Util.Logger.Info("ServerManager patches applied successfully");
            }
            catch (Exception e)
            {
                Util.Logger.Error("Error applying patches", e);
                throw;
            }
        }

        public bool OnEnable()
        {
            _instance = this;
            _commandHandler = new CommandHandler();
            _messageProcessor = new MessageProcessor();

            string[] args = System.Environment.GetCommandLineArgs();
            Util.Logger.Info($"Command line args: {string.Join(" ", args)}");

            try
            {
                ApplyPatchesSafely();
            }
            catch (Exception e)
            {
                Util.Logger.Error("Harmony patch failed", e);
                return false;
            }

            try
            {
                StartBridge();
            }
            catch (Exception e)
            {
                Util.Logger.Error("Failed to start bridge in OnEnable", e);
                return false;
            }

            if (UnityEngine.Object.FindFirstObjectByType<MonoBehaviour>() is MonoBehaviour mono)
            {
                mono.StartCoroutine(_messageProcessor.ItemProcessingCoroutine());
            }

            Util.Logger.Info("Mod enabled successfully");
            return true;
        }

        public bool OnDisable()
        {
            _messageProcessor?.Stop();

            try
            {
                harmony.UnpatchSelf();
            }
            catch (Exception e)
            {
                Util.Logger.Error("Harmony unpatch failed", e);
                return false;
            }

            _client?.Stop();
            Util.Logger.Info("Mod disabled");
            return true;
        }
    }
}