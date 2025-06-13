using System.Runtime.CompilerServices;

namespace PuckBridgeMod.Core
{
    public static class ServerConfigurationExtensions
    {
        private static readonly ConditionalWeakTable<ServerConfiguration, BridgePortHolder> bridgePortStorage =
            new ConditionalWeakTable<ServerConfiguration, BridgePortHolder>();

        private class BridgePortHolder
        {
            public ushort bridgePort { get; set; } = 9000;
        }

        public static ushort GetBridgePort(this ServerConfiguration config)
        {
            return bridgePortStorage.GetOrCreateValue(config).bridgePort;
        }

        public static void SetBridgePort(this ServerConfiguration config, ushort value)
        {
            bridgePortStorage.GetOrCreateValue(config).bridgePort = value;
        }
    }
}
