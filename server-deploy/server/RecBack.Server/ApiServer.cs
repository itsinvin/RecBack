using System.Text.RegularExpressions;

namespace RecBack.Server;

public static class ApiServer
{
    public static async Task Start(ServerConfig config, CancellationToken ct)
    {
        Directory.CreateDirectory(config.DataDir);

        var srv = new SimpleHttpServer(config.ApiPort, (req, _) =>
        {
            var path = req.Path;
            if (path.Contains('?')) path = path[..path.IndexOf('?')];

            return (req.Method, path) switch
            {
                ("POST", "/Account/Login") => Task.FromResult(Login(req, config)),
                ("POST", "/Account/Register") => Task.FromResult(Register(req, config)),
                ("GET", "/Account/Me") => Task.FromResult(GetMe(req, config)),
                ("GET", "/config") => Task.FromResult(GetConfig()),
                _ when path.StartsWith("/rooms/") || path.StartsWith("/Room/") => Task.FromResult(RoomResponse()),
                _ => Task.FromResult(HttpResponse.Json(new { }))
            };
        });

        Console.WriteLine($"[ApiServer] Listening on port {config.ApiPort}");
        await srv.Start(ct);
    }

    private static HttpResponse Login(HttpRequest req, ServerConfig config)
    {
        var body = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(req.Body);
        var username = body?.GetValueOrDefault("username") ?? "player";
        var ticket = Guid.NewGuid().ToString("N");
        var accountPath = Path.Combine(config.DataDir, $"{username}.json");

        if (!File.Exists(accountPath))
        {
            var newAccount = new { Username = username, DisplayName = username, Created = DateTime.UtcNow.ToString("o") };
            File.WriteAllText(accountPath, System.Text.Json.JsonSerializer.Serialize(newAccount));
        }

        return HttpResponse.Json(new
        {
            ticket,
            username,
            displayName = username,
            token = ticket,
            userId = username.GetHashCode()
        });
    }

    private static HttpResponse Register(HttpRequest req, ServerConfig config)
    {
        return Login(req, config);
    }

    private static HttpResponse GetMe(HttpRequest req, ServerConfig config)
    {
        return HttpResponse.Json(new
        {
            username = "Player",
            displayName = "Player",
            userId = 1,
            developer = true,
            moderator = true,
            subscribed = true
        });
    }

    private static HttpResponse GetConfig()
    {
        return HttpResponse.Json(new
        {
            allowOculus = false,
            allowPS4 = false,
            allowPS5 = false,
            allowIOS = false,
            allowAndroid = false,
            minVersion = "20230324",
            recRoomPlusCost = 0
        });
    }

    private static HttpResponse RoomResponse()
    {
        return HttpResponse.Json(new
        {
            roomId = "recback_lobby",
            name = "RecBack Lobby",
            description = "Welcome to RecBack!",
            playerCount = 0,
            maxPlayers = 16,
            isPublic = true
        });
    }
}
