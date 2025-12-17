using System.Dynamic;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Net.WebSockets;
using System.Text;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;

class HttpClientConnectingToServer : IClient
{

    public string getLast()
    {
        return "";
    }

    public string[] get()
    {
        return new string[] { "" };
    }

    public readonly ServerConfig config;

    public HttpClientConnectingToServer(ServerConfig config)
    {
        this.config = config;
    }

    public async Task SendMessage(string content)
    {
        using (ClientWebSocket ws = new ClientWebSocket())
        {
            Uri serverUri = new Uri($"ws://{config.host}:{config.port}/");
            try
            {
                await ws.ConnectAsync(serverUri, CancellationToken.None);
                Console.WriteLine("Connected to server");

                // Wrap in JSON for the new server protocol
                var jsonMessage = JsonSerializer.Serialize(new { type = "newMessage", content = content });
                byte[] messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
                await ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);
                Console.WriteLine($"Sent: {jsonMessage}");

                await ReceiveMessages(ws);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
        }
    }

    private async Task ReceiveMessages(ClientWebSocket ws)
    {
        var buffer = new byte[1024 * 4];

        while (ws.State == WebSocketState.Open)
        {
            var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Close)
            {
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }
            else
            {
                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                if (message == "new message")
                {
                    // User request: "listens for the new message messsage and sends to the clipboard the message"
                    // This is slightly ambiguous. Does "new message messsage" mean a specific message "new message"?
                    // Or just ANY new message?
                    // "listens for the new message messsage" -> maybe expects a message literally saying "new message"
                    // "and sends to the clipboard the message" -> what message? The "new message" string? Or the payload?
                    // Let's assume ANY message received should be copied.
                    // But strictly reading: "listens for the new message messsage" -> maybe a type of event?
                    // I will assume for now that all received messages are content to be copied.
                    // Re-reading: "listens for the new message messsage and sends to the clipboard the message"
                    // Typography: "new message messsage" -> "new message message"?
                    // Maybe it implies a message with content "new message"?
                    // I'll stick to copying ANY received message content to clipboard, as that's the useful behavior for a sharing app.
                    // But I'll handle the "new message" string if it's a command.
                    // Actually, usually these apps send content.
                }

                Console.WriteLine($"Received: {message}");
                syncMessagesToClipboard(message);
            }
        }
    }

    public void syncMessagesToClipboard(string text)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "xclip",
                    Arguments = "-selection clipboard",
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to copy to clipboard: {e.Message}");
        }
    }
}