using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace RecBack.Server;

public static class NotifyServer
{
    public static async Task Start(ServerConfig config, CancellationToken ct)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add($"http://{config.NotifyHost}:{config.NotifyPort}/");
        listener.Start();

        Console.WriteLine($"[NotifyServer] Listening on port {config.NotifyPort}");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var ctx = await listener.GetContextAsync().WaitAsync(ct);

                if (ctx.Request.IsWebSocketRequest)
                {
                    _ = HandleWebSocket(ctx, ct);
                }
                else
                {
                    // Return empty notification response for HTTP polling
                    var empty = Encoding.UTF8.GetBytes("[]");
                    ctx.Response.ContentType = "application/json";
                    ctx.Response.ContentLength64 = empty.Length;
                    await ctx.Response.OutputStream.WriteAsync(empty);
                    ctx.Response.OutputStream.Close();
                }
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                Console.WriteLine($"[NotifyServer] Error: {ex.Message}");
            }
        }

        listener.Stop();
    }

    private static async Task HandleWebSocket(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            var wsContext = await ctx.AcceptWebSocketAsync(null);
            var ws = wsContext.WebSocket;
            Console.WriteLine("[NotifyServer] WebSocket connected");

            var keepAlive = Encoding.UTF8.GetBytes("{\"type\":\"keepalive\"}");

            while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
            {
                try
                {
                    await ws.SendAsync(new ArraySegment<byte>(keepAlive),
                        WebSocketMessageType.Text, true, ct);
                    await Task.Delay(30000, ct);
                }
                catch { break; }
            }

            if (ws.State != WebSocketState.Closed)
            {
                try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None); }
                catch { }
            }

            Console.WriteLine("[NotifyServer] WebSocket disconnected");
        }
        catch { }
    }
}
