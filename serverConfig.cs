using System.Reflection.Emit;

public record ServerConfig(string host, int port)
{
    public string getUrl()
    {
        return $"http://{host}:{port}";
    }
}
