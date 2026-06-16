using System.Net;
using System.Text.Json;

namespace RecBack.Server;

public static class NameServer
{
    public static async Task Start(ServerConfig config, CancellationToken ct)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://{config.NameserverHost}:{config.NameserverPort}/");
        listener.Start();

        Console.WriteLine($"[NameServer] Listening on port {config.NameserverPort}");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ctx = await listener.GetContextAsync().WaitAsync(ct);
                _ = HandleRequest(ctx, config);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[NameServer] Error: {ex.Message}");
            }
        }

        listener.Stop();
    }

    private static async Task HandleRequest(HttpListenerContext ctx, ServerConfig config)
    {
        try
        {
            var response = new
            {
                API = $"http://{config.ExternalIp}:{config.ApiPort}",
                Images = $"http://{config.ExternalIp}:{config.ImagePort}",
                Notifications = $"http://{config.ExternalIp}:{config.NotifyPort}",
                Identity = $"http://{config.ExternalIp}:{config.ApiPort}",
                Rooms = $"http://{config.ExternalIp}:{config.ApiPort}",
                Subscriptions = $"http://{config.ExternalIp}:{config.ApiPort}",
                Pipeline = $"http://{config.ExternalIp}:{config.ApiPort}"
            };

            var json = JsonSerializer.Serialize(response);
            var buffer = System.Text.Encoding.UTF8.GetBytes(json);

            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentLength64 = buffer.Length;
            ctx.Response.StatusCode = 200;
            await ctx.Response.OutputStream.WriteAsync(buffer);
            ctx.Response.OutputStream.Close();
        }
        catch { }
    }
}
