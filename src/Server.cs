using System.Net;
using System.Net.Sockets;
using System.Text;

// You can use print statements as follows for debugging, they'll be visible when running tests.
Console.WriteLine("Logs from your program will appear here!");


TcpListener server = new TcpListener(IPAddress.Any, 4221);
server.Start();
var socket = server.AcceptSocket(); // wait for client

var responseBuffer = new byte[1024];
int receivedBytes = socket.Receive(responseBuffer);

var val = Encoding.UTF8.GetString(responseBuffer).Split("\r\n");


var lineFirstPart = val[0].Split(" ");

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
else
{
    response = $"{httpVersion} 404 Not Found\r\n\r\n";
}

socket.Send(Encoding.UTF8.GetBytes(response));

socket.Close();

