using System;
using System.ComponentModel;
using System.Net;
using System.Net.Mime;
using System.Net.Sockets;
using System.Reflection;
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
        //response = $"{request.HttpVersion} 200 OK\r\n\r\n";
        response = new Response(request.HttpVersion, StatusCode.Ok).ToString();
    }
    else if (request.Path.StartsWith("/echo/"))
    {
        var message = request.Path.Substring(6);
        if (request.Headers.ContainsValue("gzip"))
        {
            response = new Response(request.HttpVersion, StatusCode.Ok, message, "text/plain", "gzip").ToString();
        }
        else
        {
            //response = $"{request.HttpVersion} 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {message.Length}\r\n\r\n{message}";
            response = new Response(request.HttpVersion, StatusCode.Ok, message, "text/plain").ToString();
        }
        
    }
    else if (request.Path.StartsWith("/user-agent"))
    {
        var userAgent = request.Lines.SingleOrDefault(a => a.Contains("User-Agent:"));
        var headerVal = userAgent.Split(": ")[1];
        //response = $"{request.HttpVersion} 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {headerVal.Length}\r\n\r\n{headerVal}";
        response = new Response(request.HttpVersion, StatusCode.Ok,headerVal,"text/plain").ToString();
    }
    else if (request.Path.StartsWith("/files/"))
    {
        var fileName = request.Path.Substring(7);
        string fileText = "";
        var directory = Environment.GetCommandLineArgs()[2];
        string filePath = Path.Combine(directory,fileName);
        if (request.HttpMethod == "GET")
        {
            if (File.Exists(filePath))
            {
                fileText = File.ReadAllText(filePath);
                //response = $"{request.HttpVersion} 200 OK\r\nContent-Type: application/octet-stream\r\nContent-Length: {fileText.Length}\r\n\r\n{fileText}";
                response = new Response(request.HttpVersion, StatusCode.Ok, fileText,
                    "application/octet-stream").ToString();
            }
            else
            {
                response = $"{request.HttpVersion} 404 Not Found\r\n\r\n";
            }
        }
        else if (request.HttpMethod == "POST")
        {
            using FileStream stream = File.Create(filePath);
            stream.Write(Encoding.UTF8.GetBytes(request.Body));
            //response = $"{request.HttpVersion} 201 Created\r\n\r\n";
            response = new Response(request.HttpVersion, StatusCode.Created).ToString();
        }

    }
    else
    {
        //response = $"{request.HttpVersion} 404 Not Found\r\n\r\n";
        response = new Response(request.HttpVersion, StatusCode.NotFound).ToString();
    }

    socket.Send(Encoding.UTF8.GetBytes(response));
    socket.Close();
    return Task.CompletedTask;
}

class Response
{
    public Response(string version, StatusCode status)
    {
        Version = version;
        Status = status;
    }
    public Response(string version, StatusCode status, string body, string contentType) : this(version,status)
    {
        Body = body;
        ContentType = contentType;
    }
    public Response(string version, StatusCode status, string body, string contentType, string encoding) : this(version, status,body, contentType)
    {
        Encoding = encoding;
    }
    public StatusCode Status { get; }
    public string Body { get; }
    public string? ContentType { get; }
    public string? Version { get; }
    public string Encoding { get; set; }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append($"{Version} {(int)Status} {Status.GetDescription()}\r\n");
        if (ContentType != null)
        {
            builder.Append($"Content-Type: {ContentType}\r\nContent-Length: {Body.Length}\r\n");
        }

        if (Encoding == "gzip")
        {
            builder.Append($"Context-Encoding: {Encoding}\r\n");
        }

        if (Body != null)
        {
            builder.Append($"\r\n\r\n{Body}");
        }
        return builder.ToString();
    }
}

class Request
{
    public Request(byte[] buffer)
    {
        Lines = Encoding.UTF8.GetString(buffer).Split("\r\n");
        HttpMethod = Lines[0].Split(" ")[0];
        Path = Lines[0].Split(" ")[1];
        HttpVersion = Lines[0].Split(" ")[2];
        Body = Lines[^1].TrimEnd();
        Headers = new Dictionary<string, string>();

        for (int i = 1; i < Lines.Length - 2; i++)
        {
            string[] content = Lines[i].Trim().Split(": ");
            string key = content[0];
            string value = content[1];
            Headers.Add(key, value);
        }
    }
    public Dictionary<string, string> Headers { get; private set; }
    public string[] Lines { get; }
    public string HttpMethod { get; }
    public string Path { get; }
    public string HttpVersion { get; }
    public string Body { get; set; }
}

enum StatusCode
{
    [Description("OK")]
    Ok = 200,

    [Description("Created")]
    Created = 201,

    [Description("Not Found")]
    NotFound = 404,

    [Description("Internal Server Error")]
    InternalServerError = 500,
}

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        return value.GetType().GetMember(value.ToString()).First().GetCustomAttribute<DescriptionAttribute>() is { } attribute
            ? attribute.Description
            : value.ToString();
    }
}





