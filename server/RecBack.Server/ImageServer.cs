using System.Net;

namespace RecBack.Server;

public static class ImageServer
{
    private static ServerConfig? _cfg;
    private static byte[]? _defaultPng;

    public static async Task Start(ServerConfig config, CancellationToken ct)
    {
        _cfg = config;

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://{config.ImageHost}:{config.ImagePort}/");
        listener.Start();

        _defaultPng = System.Text.Encoding.UTF8.GetBytes(
            "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"256\" height=\"256\">" +
            "<rect width=\"100%\" height=\"100%\" fill=\"#2a2a3e\"/>" +
            "<text x=\"50%\" y=\"50%\" font-family=\"monospace\" font-size=\"24\" fill=\"#888\" text-anchor=\"middle\" dy=\".3em\">RecBack</text></svg>"
        );

        Console.WriteLine($"[ImageServer] Listening on port {config.ImagePort}");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ctx = await listener.GetContextAsync().WaitAsync(ct);
                _ = HandleRequest(ctx);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageServer] Error: {ex.Message}");
            }
        }

        listener.Stop();
    }

    private static async Task HandleRequest(HttpListenerContext ctx)
    {
        try
        {
            var req = ctx.Request;
            var res = ctx.Response;
            var path = req.Url?.AbsolutePath?.TrimStart('/') ?? "";

            var imageDir = Path.Combine(_cfg?.DataDir ?? "data", "images");
            Directory.CreateDirectory(imageDir);

            var filePath = Path.Combine(imageDir, path.Replace('/', Path.DirectorySeparatorChar)
                .Replace("..", ""));

            if (File.Exists(filePath))
            {
                var ext = Path.GetExtension(filePath).ToLower();
                res.ContentType = ext switch
                {
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".gif" => "image/gif",
                    _ => "image/png"
                };
                var bytes = await File.ReadAllBytesAsync(filePath);
                res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes);
            }
            else
            {
                res.ContentType = "image/svg+xml";
                res.ContentLength64 = _defaultPng?.Length ?? 0;
                if (_defaultPng != null)
                    await res.OutputStream.WriteAsync(_defaultPng);
            }

            res.OutputStream.Close();
        }
        catch { }
    }
}
