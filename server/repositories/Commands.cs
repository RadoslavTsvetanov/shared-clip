public record Command(string Value, string FromClient, DateTime Timestamp);

public class CommandsRepository
{
    private readonly List<Command> _commands = new();

    public void SaveCommand(Command command)
    {
        _commands.Add(command);
    }

    public Command? GetLast()
    {
        return _commands.LastOrDefault();
    }

    public IEnumerable<Command> GetFromPositionToPosition(int from, int to)
    {
        return _commands.Skip(from).Take(to - from + 1);
    }

    public IEnumerable<Command> GetMatchingFirstX(int x)
    {
        return _commands.Take(x);
    }
}
