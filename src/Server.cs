using System.Net;
using System.Net.Sockets;
using System.Text;


TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
while (true)
{
    var socket = server.AcceptSocket();
    await HandleConnection(socket);

}


Task HandleConnection(Socket socket)
{
    var responseBuffer = new byte[1024];
    socket.Receive(responseBuffer);


    Request request = new Request(responseBuffer);

    string response = "";

    if (request.Path == "/")
    {
        response = $"{request.HttpVersion} {HttpStatus.OK} OK\r\n\r\n";
    }
    else if (request.Path.StartsWith("/echo/"))
    {
        var message = request.Path.Substring(6);
        response = $"{request.HttpVersion} {HttpStatus.OK} OK\r\nContent-Type: text/plain\r\nContent-Length: {message.Length}\r\n\r\n{message}";
    }
    else if (request.Path.StartsWith("/user-agent"))
    {
        var userAgent = request.Lines.SingleOrDefault(a => a.Contains("User-Agent:"));
        var headerVal = userAgent.Split(": ")[1];
        response = $"{request.HttpVersion} {HttpStatus.OK} OK\r\nContent-Type: text/plain\r\nContent-Length: {headerVal.Length}\r\n\r\n{headerVal}";
    }
    else if (request.Path.StartsWith("/files/"))
    {
        var fileName = request.Path.Substring(7);
        string fileText = "";
        var directory = Environment.GetCommandLineArgs()[2];
        string filePath = $"{directory}/{fileName}";
        if (File.Exists(filePath))
        {
            fileText = File.ReadAllText(filePath);
            response = $"{request.HttpVersion} {HttpStatus.OK} OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileText.Length}\r\n\r\n{fileText}";
        }
        else
        {
            response = $"{request.HttpVersion} {HttpStatus.NotFound} Not Found\r\n\r\n";
        }

    }
    else
    {
        response = $"{request.HttpVersion} {HttpStatus.NotFound} Not Found\r\n\r\n";
    }

    socket.Send(Encoding.UTF8.GetBytes(response));
    socket.Close();
    return Task.CompletedTask;
}

class Request
{
    public Request(byte[] buffer)
    {
        Lines = Encoding.UTF8.GetString(buffer).Split("\r\n");
        Method = Lines[0].Split(" ")[0];
        Path = Lines[0].Split(" ")[1];
        HttpVersion = Lines[0].Split(" ")[2];
    }

    public string[] Lines { get; }
    public string Method { get; }
    public string Path { get; }
    public string HttpVersion { get; }
}

enum HttpStatus
{
    OK = 200,
    NotFound = 404
}






