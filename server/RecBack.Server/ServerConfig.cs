namespace RecBack.Server;

public class ServerConfig
{
    public string NameserverHost { get; set; } = "0.0.0.0";
    public int NameserverPort { get; set; } = 9999;
    public string ApiHost { get; set; } = "0.0.0.0";
    public int ApiPort { get; set; } = 2018;
    public string ImageHost { get; set; } = "0.0.0.0";
    public int ImagePort { get; set; } = 20182;
    public string NotifyHost { get; set; } = "0.0.0.0";
    public int NotifyPort { get; set; } = 20161;
    public string ExternalIp { get; set; } = "localhost";
    public string DataDir { get; set; } = "data";

    public void Save()
    {
        var dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var path = Path.Combine(dir ?? ".", "config.json");
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
    }
}
