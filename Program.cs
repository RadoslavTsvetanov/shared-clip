using System.Net;
using System.Net.WebSockets;
using System.Text;

if (args.Length > 0 && args[0] == "server")
{
    var commandsRepo = new CommandsRepository();
    var httpServer = HttpServer.def();
    httpServer.startWebSocketServer();
}

ServerConfig config = new ServerConfig("localhost", 8080);
IClient client = new HttpClientConnectingToServer(config);



CreateCLIMapping cli = CreateCLIMapping.empty();

cli
    .Add(new CliCommand(
        new CommandMetadata("send"),
        new[] { new CommandArg("message", Values.STRING) },
        args => client.SendMessage(args[0])
    ))
    .Add(new CliCommand(
        new CommandMetadata("getLatest"),
        Array.Empty<CommandArg>(),
        args => Console.WriteLine(client.getLast())
    ))
    .Add(new CliCommand(
        new CommandMetadata("syncClipboard"),
        Array.Empty<CommandArg>(),
        args => client.syncMessagesToClipboard()
    ));