using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

class UdpServerRunner
{
    public static async Task Run()
    {
        var connectionsRepo = new ConnectionsRepository();
        var commandsRepo = new CommandsRepository();

        using var udp = new UdpClient(5000);

        Console.WriteLine("UDP server running on port 5000");

        while (true)
        {
            var result = await udp.ReceiveAsync();
            var message = Encoding.UTF8.GetString(result.Buffer);

            try
            {
                var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;
                var type = root.GetProperty("type").GetString();

                switch (type)
                {
                    case "connect":
                        {
                            var name = root.GetProperty("name").GetString()!;

                            var client = new ClientInfo(
                                name,
                                result.RemoteEndPoint,
                                DateTime.UtcNow
                            );

                            connectionsRepo.AddClient(client);

                            Console.WriteLine($"[CONNECT] {name}");
                            break;
                        }

                    case "send":
                        {
                            var name = root.GetProperty("name").GetString()!;
                            var value = root.GetProperty("command").GetString()!;

                            if (connectionsRepo.GetClient(name) == null)
                                break;

                            commandsRepo.SaveCommand(
                                new Command(value, name, DateTime.UtcNow)
                            );

                            Console.WriteLine($"[SEND] {name}: {value}");
                            break;
                        }

                    case "show":
                        {
                            var last = commandsRepo.GetLast();

                            var response = last == null
                                ? "{\"status\":\"empty\"}"
                                : JsonSerializer.Serialize(new
                                {
                                    status = "ok",
                                    command = last.Value,
                                    from = last.FromClient,
                                    timestamp = last.Timestamp
                                });

                            var data = Encoding.UTF8.GetBytes(response);
                            await udp.SendAsync(data, data.Length, result.RemoteEndPoint);
                            break;
                        }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");
            }
        }
    }
}
