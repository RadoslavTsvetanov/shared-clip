
interface IClient
{

    public string getLast();

    public string[] get();

    public Task SendMessage(string content);
}
