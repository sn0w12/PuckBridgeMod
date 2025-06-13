using Newtonsoft.Json.Linq;

namespace PuckBridgeMod.Commands
{
    public class SystemMessageCommand : ICommand
    {
        public string CommandName => "system_message";

        public void Execute(object payload)
        {
            var jPayload = payload as JObject;
            string message = jPayload?["message"]?.ToString();

            if (!string.IsNullOrEmpty(message))
            {
                PuckBridgeMod._instance?.QueueSystemMessage(message);
            }
            else
            {
                Util.Logger.Warning("System message command missing 'message' parameter");
            }
        }
    }
}