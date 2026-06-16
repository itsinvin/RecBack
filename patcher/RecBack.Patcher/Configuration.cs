using System.IO;
using BepInEx;

namespace RecBack.Patcher;

public class Configuration
{
    public string NameserverTarget { get; set; } = "localhost:9999";

    public static Configuration Load()
    {
        var config = new Configuration();
        var configPath = Path.Combine(Paths.ConfigPath, "recback.patches.cfg");

        if (File.Exists(configPath))
        {
            var lines = File.ReadAllLines(configPath);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("//") || trimmed.StartsWith("#") || string.IsNullOrEmpty(trimmed))
                    continue;

                var parts = trimmed.Split('=', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    if (key.Equals("Target", System.StringComparison.OrdinalIgnoreCase))
                        config.NameserverTarget = value;
                }
            }
        }

        return config;
    }
}
