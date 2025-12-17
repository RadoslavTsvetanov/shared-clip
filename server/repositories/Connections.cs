using System.Net;

public record ClientInfo(string Name, IPEndPoint EndPoint, DateTime ConnectedAt);

public class ConnectionsRepository
{
    private readonly Dictionary<string, ClientInfo> _clients = new();

    public void AddClient(ClientInfo client)
    {
        _clients[client.Name] = client;
    }

    public ClientInfo? GetClient(string name)
    {
        _clients.TryGetValue(name, out var client);
        return client;
    }

    public IReadOnlyCollection<ClientInfo> GetClients()
    {
        return _clients.Values;
    }
}
