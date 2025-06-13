using System;
using UnityEngine;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PuckBridgeMod.Patches
{
    [HarmonyPatch]
    public class IMetricsPatch
    {
        // Target the IMetrics method - you may need to adjust this based on the exact class name
        [HarmonyPatch(typeof(ServerManager), "IMetrics")]
        [HarmonyPrefix]
        static bool Prefix(ServerManager __instance, float delay, ref IEnumerator __result)
        {
            __result = PatchedIMetrics(__instance, delay);
            return false; // Skip original method
        }

        private static IEnumerator PatchedIMetrics(ServerManager instance, float delay)
        {
            yield return new WaitForSeconds(delay);

            // Access the deltaTimeBuffer field using reflection or Harmony AccessTools
            var deltaTimeBufferField = AccessTools.Field(typeof(ServerManager), "deltaTimeBuffer");
            var deltaTimeBuffer = (List<float>)deltaTimeBufferField.GetValue(instance);

            if (deltaTimeBuffer.Count <= 0)
            {
                deltaTimeBuffer.Clear();
                yield return null;
            }

            // Calculate performance metrics
            float currentFPS = 1f / Time.deltaTime;
            float minFPS = 1f / deltaTimeBuffer.Max();
            float avgFPS = 1f / deltaTimeBuffer.Average();
            float maxFPS = 1f / deltaTimeBuffer.Min();

            // Send performance data through websocket
            if (PuckBridgeMod._instance?.Client != null)
            {
                var performanceData = new
                {
                    type = "performance",
                    fps = new
                    {
                        current = currentFPS,
                        min = minFPS,
                        average = avgFPS,
                        max = maxFPS
                    }
                };

                try
                {
                    PuckBridgeMod._instance.Client.SendPerformanceData(performanceData);
                }
                catch (Exception e)
                {
                    Util.Logger.Error("Failed to send performance data", e);
                }
            }

            // Keep original debug log
            Debug.Log(string.Format("[ServerManager] FPS: {0} (min: {1}, average: {2}, max: {3})", new object[]
            {
                currentFPS,
                minFPS,
                avgFPS,
                maxFPS
            }));

            deltaTimeBuffer.Clear();

            // Call the original Server_StartMetricsCoroutine method
            var startMetricsMethod = AccessTools.Method(typeof(ServerManager), "Server_StartMetricsCoroutine");
            startMetricsMethod.Invoke(instance, null);

            yield break;
        }
    }
}