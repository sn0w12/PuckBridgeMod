using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;

namespace PuckBridgeMod.Core
{
    internal class MessageProcessor
    {
        private readonly ConcurrentQueue<string> _systemMessageQueue = new ConcurrentQueue<string>();
        private readonly ConcurrentQueue<System.Action> _gameActionQueue = new ConcurrentQueue<System.Action>();
        private bool _running = true;

        internal void QueueSystemMessage(string message)
        {
            _systemMessageQueue.Enqueue(message);
            Util.Logger.Info($"Queued system message: {message}");
        }

        internal void QueueGameAction(System.Action action)
        {
            _gameActionQueue.Enqueue(action);
            Util.Logger.Debug("Queued game action");
        }

        internal IEnumerator ItemProcessingCoroutine()
        {
            while (_running || _systemMessageQueue.Count > 0 || _gameActionQueue.Count > 0)
            {
                ProcessQueuedItems();
                yield return new WaitForSeconds(0.1f); // Check every 100ms
            }
        }

        internal void Stop()
        {
            _running = false;
        }

        private void ProcessQueuedItems()
        {
            // Process system messages
            while (_systemMessageQueue.TryDequeue(out string message))
            {
                try
                {
                    var uiChat = UnityEngine.Object.FindFirstObjectByType<UIChat>();
                    if (uiChat != null)
                    {
                        uiChat.Server_SendSystemChatMessage(message);
                        Util.Logger.Info($"Sent system message: {message}");
                    }
                    else
                    {
                        Util.Logger.Warning("UIChat instance not found, cannot send system message");
                    }
                }
                catch (Exception ex)
                {
                    Util.Logger.Error($"Error sending system message: {message}", ex);
                }
            }

            // Process game actions
            while (_gameActionQueue.TryDequeue(out System.Action action))
            {
                try
                {
                    action.Invoke();
                }
                catch (Exception ex)
                {
                    Util.Logger.Error("Error executing game action", ex);
                }
            }
        }
    }
}
