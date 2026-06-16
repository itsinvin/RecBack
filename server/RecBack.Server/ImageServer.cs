namespace RecBack.Server;

public static class ImageServer
{
    private static readonly byte[] DefaultSvg = System.Text.Encoding.UTF8.GetBytes(
        "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"256\" height=\"256\">" +
        "<rect width=\"100%\" height=\"100%\" fill=\"#2a2a3e\"/>" +
        "<text x=\"50%\" y=\"50%\" font-family=\"monospace\" font-size=\"24\" fill=\"#888\" text-anchor=\"middle\" dy=\".3em\">RecBack</text></svg>");

    public static async Task Start(ServerConfig config, CancellationToken ct)
    {
        var srv = new SimpleHttpServer(config.ImagePort, (req, _) =>
        {
            var resp = new HttpResponse
            {
                ContentType = "image/svg+xml",
                Body = DefaultSvg
            };
            return Task.FromResult(resp);
        });

        Console.WriteLine($"[ImageServer] Listening on port {config.ImagePort}");
        await srv.Start(ct);
    }
}
