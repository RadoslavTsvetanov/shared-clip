using System.Net.WebSockets;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class HttpServer
{
    public readonly string host;
    public readonly int port;
    private readonly CommandsRepository commandsRepository;
    private readonly ConcurrentDictionary<string, WebSocket> _sockets = new();

    public void startHttpServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://{host}:{port}");
        var app = builder.Build();

        app.MapGet(
            "/Messages",
            (int fromPosition, int toPosition) =>
            commandsRepository.GetMessages(fromPosition, toPosition)
        );

        app.MapGet(
            "/Messages/Last",
            () =>
            commandsRepository.GetLastMessage()
        );

        app.MapGet(
            "/Messages/matching={pattern}",
            (string pattern, int numberOf) =>
            commandsRepository.GetMatchingMessages(pattern, numberOf));

        app.Run();

    }

    public void startWebSocketServer()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls($"http://{host}:{port}");
        var app = builder.Build();

        app.UseWebSockets();

        var handler = async (HttpContext context) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                var socketId = Guid.NewGuid().ToString();
                _sockets.TryAdd(socketId, webSocket);
                Console.WriteLine($"[WS] Client connected: {socketId}");

                await Echo(webSocket, socketId);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        };

        app.Map("/ws", handler);
        app.Map("/", handler);

        app.Run();
    }

    private async Task Echo(WebSocket webSocket, string socketId)
    {
        var buffer = new byte[1024 * 4];
        try
        {
            while (webSocket.State == WebSocketState.Open)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
                else
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"[WS] Received: {message}");

                    try
                    {
                        var doc = JsonDocument.Parse(message);
                        if (doc.RootElement.TryGetProperty("type", out var typeElement) && typeElement.GetString() == "newMessage")
                        {
                            if (doc.RootElement.TryGetProperty("content", out var contentElement))
                            {
                                var content = contentElement.GetString();
                                if (!string.IsNullOrEmpty(content))
                                {
                                    commandsRepository.SaveCommand(new Command(content, socketId, DateTime.UtcNow));
                                    Console.WriteLine($"[WS] Saved command: {content}");
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Ignore non-JSON
                    }

                    // Broadcast
                    foreach (var (id, socket) in _sockets)
                    {
                        if (socket.State == WebSocketState.Open)
                        {
                            var bytes = Encoding.UTF8.GetBytes(message);
                            await socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WS] Error: {ex.Message}");
        }
        finally
        {
            _sockets.TryRemove(socketId, out _);
            Console.WriteLine($"[WS] Client disconnected: {socketId}");
        }
    }

    public HttpServer(string host, int port, CommandsRepository commandsRepository)
    {
        this.host = host;
        this.port = port;
        this.commandsRepository = commandsRepository;
    }

    static HttpServer def()
    {

        Console.WriteLine("Starting ASP.NET Core WebSocket Server on http://localhost:8080");
        return new HttpServer("localhost", 8080, new CommandsRepository());
    }
}