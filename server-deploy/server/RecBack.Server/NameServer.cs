namespace RecBack.Server;

public static class NameServer
{
    public static async Task Start(ServerConfig config, CancellationToken ct)
    {
        var srv = new SimpleHttpServer(config.NameserverPort, (req, _) =>
        {
            var resp = new
            {
                API = $"http://{config.ExternalIp}:{config.ApiPort}",
                Images = $"http://{config.ExternalIp}:{config.ImagePort}",
                Notifications = $"http://{config.ExternalIp}:{config.NotifyPort}",
                Identity = $"http://{config.ExternalIp}:{config.ApiPort}",
                Rooms = $"http://{config.ExternalIp}:{config.ApiPort}",
                Subscriptions = $"http://{config.ExternalIp}:{config.ApiPort}",
                Pipeline = $"http://{config.ExternalIp}:{config.ApiPort}"
            };
            return Task.FromResult(HttpResponse.Json(resp));
        });

        Console.WriteLine($"[NameServer] Listening on port {config.NameserverPort}");
        await srv.Start(ct);
    }
}
