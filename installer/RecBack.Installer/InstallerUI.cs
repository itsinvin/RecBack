namespace RecBack.Installer;

public static class InstallerUI
{
    public static string LOGO => $@"
{Color("96")}██████╗ ███████╗ ██████╗ ██████╗  █████╗  ██████╗██╗  ██╗
{Color("94")}██╔══██╗██╔════╝██╔════╝ ██╔══██╗██╔══██╗██╔════╝██║ ██╔╝
{Color("96")}██████╔╝█████╗  ██║      ██████╔╝███████║██║     █████╔╝
{Color("94")}██╔══██╗██╔══╝  ██║      ██╔══██╗██╔══██║██║     ██╔═██╗
{Color("96")}██║  ██║███████╗╚██████╗ ██████╔╝██║  ██║╚██████╗██║  ██╗
{Color("90")}╚═╝  ╚═╝╚══════╝ ╚═════╝ ╚═════╝ ╚═╝  ╚═╝ ╚═════╝╚═╝  ╚═╝
{Color("2;3")}━━━ Rec Room is Back ─━━━{Reset()}
";

    private const string _spinnerFrames = "⠋⠙⠹⠸⠼⠴⠦⠧⠇⠏";
    private static readonly string[] _colorCycle = ["36", "34", "96", "94"];

    public static string Color(string code) => $"\x1b[{code}m";
    public static string Reset() => "\x1b[0m";
    public static string Bold() => "\x1b[1m";
    public static string Dim() => "\x1b[2m";
    public static string HideCursor() => "\x1b[?25l";
    public static string ShowCursor() => "\x1b[?25h";
    public static string ClearLine() => "\x1b[2K";
    public static string CursorUp(int n = 1) => $"\x1b[{n}A";

    public static async Task AnimateLogo()
    {
        var lines = LOGO.TrimStart().Split('\n');
        foreach (var line in lines)
        {
            Console.WriteLine(line);
            await Task.Delay(40);
        }
        await Task.Delay(300);
    }

    public static async Task AnimateText(string text, int delayMs = 15)
    {
        foreach (var ch in text)
        {
            Console.Write(ch);
            await Task.Delay(delayMs);
        }
        Console.WriteLine();
    }

    public static async Task Spinner(int durationMs, string message = "Loading")
    {
        var end = Environment.TickCount + durationMs;
        int i = 0;
        while (Environment.TickCount < end)
        {
            Console.Write($"\r{Color("96")}{_spinnerFrames[i % _spinnerFrames.Length]} {message}{Reset()}");
            await Task.Delay(80);
            i++;
        }
        Console.Write($"\r{new string(' ', 40)}\r");
    }

    public static void DrawProgressBar(long current, long total, string prefix = "")
    {
        if (total <= 0) return;
        var pct = (double)current / total;
        var filled = (int)(40 * pct);
        var bar = $"{Color("92")}{new string('█', filled)}{Color("90")}{new string('░', 40 - filled)}{Reset()}";
        Console.Write($"\r{prefix} {bar} {pct,6:0.0%}");
    }

    public static void ClearScreen()
    {
        Console.Write("\x1b[2J\x1b[H");
    }

    public static void PrintHeader()
    {
        ClearScreen();
        Console.WriteLine(LOGO);
    }

    public static void Step(int num, string text, bool completed = false)
    {
        if (completed)
            Console.WriteLine($"  {Color("92")}\u2713{Reset()} Step {num}: {text}");
        else
            Console.WriteLine($"  {Color("96")}\u2192{Reset()} Step {num}: {text}");
    }

    public static void Info(string text) => Console.WriteLine($"  {Color("94")}i{Reset()} {text}");
    public static void Success(string text) => Console.WriteLine($"  {Color("92")}\u2713{Reset()} {text}");
    public static void Warn(string text) => Console.WriteLine($"  {Color("93")}\u26a0{Reset()} {text}");
    public static void Error(string text) => Console.WriteLine($"  {Color("91")}\u2717{Reset()} {text}");

    public static string Prompt(string text, string defaultValue = "")
    {
        if (!string.IsNullOrEmpty(defaultValue))
        {
            var val = PromptRaw($"  {Color("96")}?{Reset()} {text} [{Dim()}{defaultValue}{Reset()}]: ");
            return string.IsNullOrEmpty(val) ? defaultValue : val;
        }
        return PromptRaw($"  {Color("96")}?{Reset()} {text}: ");
    }

    public static bool PromptYesNo(string text, bool defaultYes = true)
    {
        var hint = defaultYes ? "Y/n" : "y/N";
        var val = PromptRaw($"  {Color("96")}?{Reset()} {text} [{hint}]: ").ToLowerInvariant();
        if (string.IsNullOrEmpty(val)) return defaultYes;
        return val.StartsWith('y');
    }

    public static string PromptPassword(string text)
    {
        Console.Write($"  {Color("96")}?{Reset()} {text}: ");
        var pwd = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && pwd.Length > 0)
            {
                pwd.Length--;
                Console.Write("\b \b");
            }
            else if (key.Key != ConsoleKey.Backspace)
            {
                pwd.Append(key.KeyChar);
                Console.Write('*');
            }
        }
        Console.WriteLine();
        return pwd.ToString();
    }

    private static string PromptRaw(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine()?.Trim() ?? "";
    }

    public static async Task DownloadWithProgress(string url, string destPath, string label = "Downloading")
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("RecBack-Installer/1.0");

        using var resp = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        var total = resp.Content.Headers.ContentLength ?? -1;

        await using var fs = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var stream = await resp.Content.ReadAsStreamAsync();

        var buffer = new byte[81920];
        long read = 0;
        int chunk;
        while ((chunk = await stream.ReadAsync(buffer)) > 0)
        {
            await fs.WriteAsync(buffer.AsMemory(0, chunk));
            read += chunk;
            if (total > 0) DrawProgressBar(read, total, $"  {label}");
        }
        Console.WriteLine();
    }
}
