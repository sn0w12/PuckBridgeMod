using System;
using System.Collections.Generic;

namespace PuckBridgeMod.Commands
{
    public class CommandHandler
    {
        private readonly Dictionary<string, ICommand> _commands = new Dictionary<string, ICommand>();

        public CommandHandler()
        {
            RegisterDefaultCommands();
        }

        private void RegisterDefaultCommands()
        {
            RegisterCommand(new SystemMessageCommand());
            RegisterCommand(new RestartGameCommand());
            RegisterCommand(new KickPlayerCommand());
        }

        public void RegisterCommand(ICommand command)
        {
            _commands[command.CommandName] = command;
            Util.Logger.Info($"Registered command: {command.CommandName}");
        }

        public void UnregisterCommand(string commandName)
        {
            if (_commands.Remove(commandName))
            {
                Util.Logger.Info($"Unregistered command: {commandName}");
            }
        }

        public bool HandleCommand(string commandName, object payload)
        {
            if (_commands.TryGetValue(commandName, out ICommand command))
            {
                try
                {
                    command.Execute(payload);
                    return true;
                }
                catch (Exception ex)
                {
                    Util.Logger.Error($"Error executing command '{commandName}'", ex);
                    return false;
                }
            }

            Util.Logger.Warning($"Unknown command: {commandName}");
            return false;
        }

        public string[] GetAvailableCommands()
        {
            var commands = new string[_commands.Count];
            _commands.Keys.CopyTo(commands, 0);
            return commands;
        }
    }
}