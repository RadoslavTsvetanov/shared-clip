using System;
using System.Collections.Generic;
using System.Linq;

enum Values
{
    STRING,
    NUMBER,
    BOOLEAN
}

record CommandArg(string Name, Values? Value);
record CommandMetadata(string Name);
record CliCommand(CommandMetadata Metadata, CommandArg[] Args, Action<CommandArg[]> Handler);

class CreateCLIMapping
{
    public List<CliCommand> Commands { get; } = new();

    public CreateCLIMapping Add(CliCommand c)
    {
        Commands.Add(c);
        return this;
    }

    public void Execute(string command)
    {
        var cmd = Commands.FirstOrDefault(c => c.Metadata.Name == command);
        if (cmd == null)
        {
            Console.WriteLine($"Command {command} not found");
            return;
        }
        cmd.Handler(cmd.Args);
    }

    CreateCLIMapping(Command[] commands)
    {
        Commands = commands.ToList();
    }

    static CreateCLIMapping empty()
    {
        return new CreateCLIMapping(Array.Empty<CliCommand>());
    }
}

// Example usage
// class Program
// {
//     static void Main()
//     {
//         var cli = new CreateCLIMapping();

//         var cmd = new CliCommand(
//             new CommandMetadata("greet"),
//             new[] { new CommandArg("name", Values.STRING) },
//             args => Console.WriteLine($"Hello, {args[0]}!") // ar
//         );

//         cli.Add(cmd);

//         cli.Execute("greet");
//     }
// }