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

    var lines = Encoding.UTF8.GetString(responseBuffer).Split("\r\n");


    var lineFirstPart = lines[0].Split(" ");



    var method = lineFirstPart[0];
    var path = lineFirstPart[1];
    var httpVersion = lineFirstPart[2];

    string response = "";

    if (path == "/")
    {
        response = $"{httpVersion} 200 OK\r\n\r\n";
    }
    else if (path.StartsWith("/echo/"))
    {
        var message = path.Substring(6);
        response = $"{httpVersion} 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {message.Length}\r\n\r\n{message}";
    }
    else if (path.StartsWith("/user-agent"))
    {
        var userAgent = lines.SingleOrDefault(a => a.Contains("User-Agent:"));
        var headerVal = userAgent.Split(": ")[1];
        response = $"{httpVersion} 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {headerVal.Length}\r\n\r\n{headerVal}";
    }
    else
    {
        response = $"{httpVersion} 404 Not Found\r\n\r\n";
    }

    socket.Send(Encoding.UTF8.GetBytes(response));
    socket.Close();
    return Task.CompletedTask;
}







