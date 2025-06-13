using System;
using Newtonsoft.Json;

namespace PuckBridgeMod.Core {
    public class BridgeMessage {
        [JsonProperty("role")]
        public string Role { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("payload")]
        public object Payload { get; set; }

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");

        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();
    }
}