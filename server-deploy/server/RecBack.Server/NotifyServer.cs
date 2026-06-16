using System.Net.WebSockets;
using System.Text;

namespace RecBack.Server;

public static class NotifyServer
{
    public static async Task Start(ServerConfig config, CancellationToken ct)
    {
        var srv = new SimpleHttpServer(config.NotifyPort, async (req, token) =>
        {
            if (req.Headers.GetValueOrDefault("Upgrade")?.ToLower() == "websocket")
                return await HandleWebSocket(req, token);
            return HttpResponse.Json(new { });
        });

        Console.WriteLine($"[NotifyServer] Listening on port {config.NotifyPort}");
        await srv.Start(ct);
    }

    private static Task<HttpResponse> HandleWebSocket(HttpRequest req, CancellationToken ct)
    {
        return Task.FromResult(HttpResponse.Json(new { type = "connected", message = "RecBack notification server" }));
    }
}
