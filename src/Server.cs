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

var (method, path, httpVersion) = (lineFirstPart[0], lineFirstPart[1], lineFirstPart[2]);

var response = path == "/" ? $"{httpVersion} 200 OK\r\n\r\n" : $"{httpVersion} 404 Not Found \r\n\r\n";

socket.Send(Encoding.UTF8.GetBytes(response));

