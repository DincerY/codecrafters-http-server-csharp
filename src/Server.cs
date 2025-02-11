using System.ComponentModel;
using System.IO.Compression;
using System.Net;
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

    byte[] byteResponse = null;

    if (request.Path == "/")
    {
        byteResponse = new Response(request.HttpVersion, StatusCode.Ok).ToBytes();
    }
    else if (request.Path.StartsWith("/echo/"))
    {
        var parameter = request.GetUrlParameter("/echo/");
        var encodingTypes = request.GetHeader("Accept-Encoding");

        if (encodingTypes != null && encodingTypes.Contains("gzip"))
        {
            //encodedData = Response.Compress(System.Text.Encoding.ASCII.GetBytes(parameter), encodingTypes);
            //response = $"HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\nContent-Length: {encodedData.Length}\r\nContent-Encoding: gzip\r\n\r\n";

            //var bytes = Encoding.ASCII.GetBytes(response);
            //byteResponse = new byte[bytes.Length + encodedData.Length];

            byteResponse = new Response(request.HttpVersion, StatusCode.Ok, parameter, "text/plain", encodingTypes).ToBytes(true);
        }
        else
        {
            byteResponse = new Response(request.HttpVersion, StatusCode.Ok, parameter, "text/plain").ToBytes(true);
        }


    }
    else if (request.Path.StartsWith("/user-agent"))
    {
        request.Headers.TryGetValue("User-Agent", out var headerVal);
        byteResponse = new Response(request.HttpVersion, StatusCode.Ok, headerVal, "text/plain").ToBytes(true);
    }
    else if (request.Path.StartsWith("/files/"))
    {
        var fileName = request.GetUrlParameter("/files/");
        var directory = Environment.GetCommandLineArgs()[2];
        string filePath = Path.Combine(directory, fileName);
        if (request.HttpMethod == "GET")
        {
            if (File.Exists(filePath))
            {
                var fileText = File.ReadAllText(filePath);
                byteResponse = new Response(request.HttpVersion, StatusCode.Ok, fileText,
                    "application/octet-stream").ToBytes(true);
            }
            else
            {
                byteResponse = new Response(request.HttpVersion, StatusCode.NotFound).ToBytes();
            }
        }
        else if (request.HttpMethod == "POST")
        {
            using FileStream stream = File.Create("filePath");
            stream.Write(Encoding.ASCII.GetBytes(request.Body));

            byteResponse = new Response(request.HttpVersion, StatusCode.Created, request.Body, "application/octet-stream").ToBytes(true);
        }
    }
    else
    {
        byteResponse = new Response(request.HttpVersion, StatusCode.NotFound).ToBytes();
    }

    
    socket.Send(byteResponse);
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
    public Response(string version, StatusCode status, string body, string contentType) : this(version, status)
    {
        Body = body;
        ContentType = contentType;
        BodyEncoded = Compress(System.Text.Encoding.ASCII.GetBytes(Body), "");
    }
    public Response(string version, StatusCode status, string body, string contentType, string encoding) : this(version, status, body, contentType)
    {
        Encoding = encoding;
        BodyEncoded = Compress(System.Text.Encoding.ASCII.GetBytes(Body), Encoding);
    }
    public StatusCode Status { get; }
    private string Body { get; } = "";
    public string ContentType { get; } = "";
    public string Version { get; }
    public string Encoding { get; set; } = "";
    private byte[] BodyEncoded { get; }

    public string NoHeaderResponse()
    {
        return $"{Version} {(int)Status} {Status.GetDescription()}\r\n\r\n";
    }

    public override string ToString()
    {
        StringBuilder builder = new StringBuilder();
        builder.Append($"{Version} {(int)Status} {Status.GetDescription()}\r\n");
        GetHeaders(builder);
        builder.Append("\r\n");
        //if (BodyEncoded != null)
        //{
        //    builder.Append($"\r\n{System.Text.Encoding.ASCII.GetString(BodyEncoded)}");
        //}
        return builder.ToString();
    }

    public byte[] ToBytes(bool withHeader = false)
    {
        byte[] byteArrayResult = null;
        string stringRes = NoHeaderResponse();

        if (withHeader)
        {
            stringRes = this.ToString();
        }

        var byteArr = System.Text.Encoding.ASCII.GetBytes(stringRes);
        
        
        byteArrayResult = new byte[byteArr.Length + BodyEncoded.Length];
        

        for (int i = 0; i < byteArr.Length; i++)
        {
            byteArrayResult[i] = (byte)byteArr[i];
        }

        for (int i = byteArr.Length; i < BodyEncoded.Length + byteArr.Length; i++)
        {
            byteArrayResult[i] = BodyEncoded[i - byteArr.Length];
        }

        return byteArrayResult;
    }


    private void GetHeaders(StringBuilder builder)
    {
        builder.Append($"Content-Type: {ContentType}\r\nContent-Length: {BodyEncoded.Length}\r\n");
        if (Encoding.Contains("gzip"))
        {
            builder.Append("Content-Encoding: gzip\r\n");
        }
    }
    public static byte[] Compress(byte[] body, string encoding)
    {
        string[] encodings = encoding.Split(", ");
        foreach (var e in encodings)
        {
            switch (e)
            {
                case "gzip":
                    using (var stream = new MemoryStream())
                    {
                        using (var gzip = new GZipStream(stream, CompressionMode.Compress))
                        {
                            gzip.Write(body, 0, body.Length);
                        }
                        return stream.ToArray();
                    }
            }
        }
        return body;
    }
}

class Request
{
    public Request(byte[] buffer)
    {
        Lines = Encoding.ASCII.GetString(buffer).Split("\r\n");
        HttpMethod = Lines[0].Split(" ")[0];
        Path = Lines[0].Split(" ")[1];
        HttpVersion = Lines[0].Split(" ")[2];
        Body = Lines[^1].TrimEnd('\0');
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
    private string[] Lines { get; }
    public string HttpMethod { get; }
    public string Path { get; }
    public string HttpVersion { get; }
    public string Body { get; }

    public string? GetHeader(string key)
    {
        Headers.TryGetValue(key, out var value);
        return value;
    }

    public string GetUrlParameter(string preUrl)
    {
        return Path.Substring(preUrl.Length);
    }

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




