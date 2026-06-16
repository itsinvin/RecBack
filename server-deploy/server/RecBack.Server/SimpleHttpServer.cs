using System.Net;
using System.Net.Sockets;
using System.Text;

namespace RecBack.Server;

public class SimpleHttpServer : IDisposable
{
    private readonly TcpListener _listener;
    private readonly Func<HttpRequest, CancellationToken, Task<HttpResponse>> _handler;
    private readonly CancellationTokenSource _cts = new();

    public SimpleHttpServer(int port, Func<HttpRequest, CancellationToken, Task<HttpResponse>> handler)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _handler = handler;
    }

    public async Task Start(CancellationToken ct)
    {
        _listener.Start();
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, _cts.Token);
        while (!linked.IsCancellationRequested)
        {
            try
            {
                var client = await _listener.AcceptTcpClientAsync(linked.Token);
                _ = HandleClient(client, linked.Token);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var req = await HttpRequest.ParseAsync(stream, ct);
                var resp = await _handler(req, ct);
                await resp.WriteAsync(stream, ct);
            }
            catch { }
        }
    }

    public void Dispose() { _cts.Cancel(); _listener.Stop(); }
}

public class HttpRequest
{
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public Dictionary<string, string> Headers { get; set; } = new();
    public byte[] Body { get; set; } = [];

    public string? Query(string key)
    {
        var idx = Path.IndexOf('?');
        if (idx < 0) return null;
        var qs = Path[(idx + 1)..];
        return System.Web.HttpUtility.ParseQueryString(qs)[key];
    }

    public static async Task<HttpRequest> ParseAsync(NetworkStream stream, CancellationToken ct)
    {
        var req = new HttpRequest();
        var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        var requestLine = await reader.ReadLineAsync(ct) ?? "";
        var parts = requestLine.Split(' ');
        if (parts.Length >= 2)
        {
            req.Method = parts[0];
            req.Path = parts[1];
        }

        int contentLength = 0;
        string? line;
        while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync(ct)))
        {
            var colon = line.IndexOf(':');
            if (colon > 0)
            {
                var key = line[..colon].Trim();
                var val = line[(colon + 1)..].Trim();
                req.Headers[key] = val;
                if (key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
                    contentLength = int.Parse(val);
            }
        }

        if (contentLength > 0)
        {
            req.Body = new byte[contentLength];
            int read = 0;
            while (read < contentLength)
                read += await stream.ReadAsync(req.Body.AsMemory(read, contentLength - read), ct);
        }
        return req;
    }
}

public class HttpResponse
{
    public int StatusCode { get; set; } = 200;
    public string ContentType { get; set; } = "text/plain";
    public byte[] Body { get; set; } = [];

    public static HttpResponse Json(object obj) => new()
    {
        ContentType = "application/json",
        Body = Encoding.UTF8.GetBytes(System.Text.Json.JsonSerializer.Serialize(obj))
    };

    public static HttpResponse Text(string text) => new()
    {
        ContentType = "text/plain",
        Body = Encoding.UTF8.GetBytes(text)
    };

    public static HttpResponse Html(string html) => new()
    {
        ContentType = "text/html",
        Body = Encoding.UTF8.GetBytes(html)
    };

    public async Task WriteAsync(NetworkStream stream, CancellationToken ct)
    {
        var header = $"HTTP/1.1 {StatusCode} OK\r\nContent-Type: {ContentType}\r\nContent-Length: {Body.Length}\r\nConnection: close\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(header);
        await stream.WriteAsync(headerBytes, ct);
        if (Body.Length > 0)
            await stream.WriteAsync(Body, ct);
    }
}
