using System.Text.Json;
using RecBack.Server;

var config = new ServerConfig();
if (args.Length > 0 && args[0] == "--init")
{
    config.Save();
    Console.WriteLine("Created default config.json");
    return;
}

if (File.Exists("config.json"))
{
    try
    {
        config = JsonSerializer.Deserialize<ServerConfig>(File.ReadAllText("config.json")) ?? config;
    }
    catch { }
}

// Docker environment overrides
var envIp = Environment.GetEnvironmentVariable("EXTERNAL_IP");
if (!string.IsNullOrEmpty(envIp))
{
    config.ExternalIp = envIp;
}

Console.WriteLine("RecBack Server v1.0.0");
Console.WriteLine($"NameServer:   http://{config.NameserverHost}:{config.NameserverPort}");
Console.WriteLine($"API Server:   http://{config.ApiHost}:{config.ApiPort}");
Console.WriteLine($"Image Server: http://{config.ImageHost}:{config.ImagePort}");
Console.WriteLine($"Notify Server: http://{config.NotifyHost}:{config.NotifyPort}");
Console.WriteLine();

var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

var tasks = new List<Task>
{
    NameServer.Start(config, cts.Token),
    ApiServer.Start(config, cts.Token),
    ImageServer.Start(config, cts.Token),
    NotifyServer.Start(config, cts.Token),
};

Console.WriteLine("All servers started. Press Ctrl+C to stop.");
try
{
    await Task.WhenAll(tasks);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Shutting down...");
}
