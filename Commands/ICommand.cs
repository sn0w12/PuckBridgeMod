namespace PuckBridgeMod.Commands
{
    public interface ICommand
    {
        string CommandName { get; }
        void Execute(object payload);
    }
}