using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace RecBack.Server;

public static class ApiServer
{
    private static ServerConfig? _config;

    public static async Task Start(ServerConfig config, CancellationToken ct)
    {
        _config = config;
        Directory.CreateDirectory(config.DataDir);

        var listener = new HttpListener();
        listener.Prefixes.Add($"http://{config.ApiHost}:{config.ApiPort}/");
        listener.Start();

        Console.WriteLine($"[ApiServer] Listening on port {config.ApiPort}");

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
                Console.WriteLine($"[ApiServer] Error: {ex.Message}");
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
            var path = req.Url?.AbsolutePath?.TrimEnd('/') ?? "/";
            var method = req.HttpMethod;

            Console.WriteLine($"[ApiServer] {method} {path}");

            if (path.Contains("/account") || path.Contains("/Account"))
            {
                await HandleAccount(req, res);
            }
            else if (path.Contains("/profile") || path.Contains("/Profile"))
            {
                await HandleProfile(req, res);
            }
            else if (path.Contains("/config") || path.Contains("/Config"))
            {
                await HandleConfig(req, res);
            }
            else if (path.Contains("/room") || path.Contains("/Room") || path.Contains("/rooms"))
            {
                await HandleRooms(req, res);
            }
            else if (path.Contains("/version") || path.Contains("/Version"))
            {
                await RespondJson(res, new { ValidVersion = true, Version = "2023.1.0" });
            }
            else if (path.Contains("/health") || path == "/" || string.IsNullOrEmpty(path))
            {
                await RespondJson(res, new { status = "ok", service = "RecBack" });
            }
            else
            {
                await RespondJson(res, new { }, 200);
            }
        }
        catch { }
    }

    private static async Task HandleAccount(HttpListenerRequest req, HttpListenerResponse res)
    {
        var path = req.Url?.AbsolutePath?.TrimEnd('/') ?? "";
        var method = req.HttpMethod;

        if (path.EndsWith("/login") || path.EndsWith("/Login"))
        {
            var loginResponse = new
            {
                userId = "10000000",
                username = "RecBackPlayer",
                displayName = "RecBack Player",
                token = "recback-token-2023",
                ticket = "recback-ticket-2023"
            };
            await RespondJson(res, loginResponse);
        }
        else if (path.EndsWith("/create") || path.EndsWith("/Create") || method == "POST")
        {
            var createResponse = new
            {
                userId = Guid.NewGuid().ToString("N")[..8],
                username = "NewPlayer",
                displayName = "New Player",
                token = "recback-token-new",
                success = true
            };
            await RespondJson(res, createResponse);
        }
        else if (path.Contains("/search") || path.Contains("/Search"))
        {
            await RespondJson(res, new object[] { new
            {
                userId = "10000000",
                username = "RecBackPlayer",
                displayName = "RecBack Player",
                profileImage = "default.png",
                level = 30
            }});
        }
        else
        {
            await RespondJson(res, new
            {
                userId = "10000000",
                username = "RecBackPlayer",
                displayName = "RecBack Player",
                token = "recback-token",
                profileImage = "default.png",
                level = 30,
                XP = 5000,
                registrationDate = "2023-01-01"
            });
        }
    }

    private static async Task HandleProfile(HttpListenerRequest req, HttpListenerResponse res)
    {
        var profile = new
        {
            userId = "10000000",
            username = "RecBackPlayer",
            displayName = "RecBack Player",
            profileImage = "default.png",
            level = 30,
            XP = 5000,
            registrationDate = "2023-01-01",
            bio = "Playing Rec Room again with RecBack!",
            subLevel = 0,
            isJunior = false,
            isDeveloper = false
        };
        await RespondJson(res, profile);
    }

    private static async Task HandleConfig(HttpListenerRequest req, HttpListenerResponse res)
    {
        var config = new
        {
            maxPlayers = 16,
            maxRooms = 50,
            allowCustomRooms = true,
            allowPrivateRooms = true,
            allowMatchmaking = true,
            photonRegion = "us",
            photonServer = "127.0.0.1",
            photonPort = 5056
        };
        await RespondJson(res, config);
    }

    private static async Task HandleRooms(HttpListenerRequest req, HttpListenerResponse res)
    {
        var rooms = new[]
        {
            new { roomId = "dorm", name = "Dorm Room", type = "dorm", maxPlayers = 8, isPrivate = true, scene = "DormRoom" },
            new { roomId = "rec_center", name = "Rec Center", type = "public", maxPlayers = 16, isPrivate = false, scene = "RecCenter" },
            new { roomId = "paintball", name = "Paintball", type = "game", maxPlayers = 8, isPrivate = false, scene = "Paintball" },
            new { roomId = "quest", name = "Quest", type = "game", maxPlayers = 4, isPrivate = false, scene = "Quest" }
        };
        await RespondJson(res, rooms);
    }

    private static async Task RespondJson(HttpListenerResponse res, object data, int statusCode = 200)
    {
        var json = JsonSerializer.Serialize(data);
        var buffer = System.Text.Encoding.UTF8.GetBytes(json);
        res.ContentType = "application/json";
        res.ContentLength64 = buffer.Length;
        res.StatusCode = statusCode;
        await res.OutputStream.WriteAsync(buffer);
        res.OutputStream.Close();
    }
}
