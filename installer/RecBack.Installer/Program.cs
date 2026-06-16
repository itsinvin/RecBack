using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using RecBack.Installer;

Console.OutputEncoding = System.Text.Encoding.UTF8;

const string Version = "1.0.0";
const string ManifestId = "7490748483298966814";

var rand = new Random();
try { Console.Write(InstallerUI.HideCursor()); }
catch { }

await InstallerUI.AnimateLogo();
await InstallerUI.AnimateText("Welcome to RecBack - The Rec Room Revival Project!", 15);
await Task.Delay(300);

try { Console.Write(InstallerUI.ShowCursor()); }
catch { }

// ─── Step 1: Install Directory ───
Console.WriteLine();
InstallerUI.Step(1, "Choose where to install RecBack");

var defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RecBack");
var installDir = InstallerUI.Prompt("Installation directory", defaultDir);
installDir = Path.GetFullPath(installDir);
Directory.CreateDirectory(installDir);

var gameDir = Path.Combine(installDir, "game");

InstallerUI.Success($"Will install to: {installDir}");
await Task.Delay(500);

// ─── Step 2: Check for updates ───
Console.WriteLine();
InstallerUI.Step(2, "Checking for RecBack updates");
await CheckForUpdates();
await Task.Delay(300);

// ─── Step 3: Get the game build ───
Console.WriteLine();
InstallerUI.Step(3, "Get a Rec Room build");

var buildExe = FindBuildExe(gameDir);
if (buildExe != null)
{
    InstallerUI.Success($"Found existing build: {Path.GetFileName(buildExe)}");
    if (!InstallerUI.PromptYesNo("Re-download/install a different build?", false))
        goto step4;
    buildExe = null;
}

if (buildExe == null)
{
    Directory.CreateDirectory(gameDir);

    if (InstallerUI.PromptYesNo("Download a 2023 Rec Room build from Steam?"))
        await RunDepotDownloader(gameDir);

    buildExe = FindBuildExe(gameDir);
}

if (buildExe == null)
{
    InstallerUI.Warn("No build found. You can:");
    InstallerUI.Info($"1. Copy a 2023 build into: {gameDir}");
    InstallerUI.Info("2. Run the installer again after placing a build there");

    var existing = @"C:\Users\invin\Downloads\BLANKRENAMETHIS";
    if (Directory.Exists(existing) && InstallerUI.PromptYesNo("Copy your existing BLANKRENAMETHIS build?"))
    {
        CopyDirectory(existing, gameDir);
        buildExe = FindBuildExe(gameDir);
        if (buildExe != null) InstallerUI.Success("Build copied!");
    }

    if (buildExe == null)
    {
        InstallerUI.Error("No build available. Please add one manually and re-run.");
        Console.WriteLine($"\n  {InstallerUI.Dim()}Press Enter to exit...{InstallerUI.Reset()}");
        Console.ReadLine();
        return;
    }
}

// ─── Step 4: Configure Server IP ───
step4:
Console.WriteLine();
InstallerUI.Step(4, "Configure your RecBack server");

var local = InstallerUI.PromptYesNo("Running server on this machine?", true);
var serverIp = InstallerUI.Prompt("RecBack server IP address", local ? "localhost" : "192.168.1.100");
var serverPort = InstallerUI.Prompt("Server port (NameServer)", "9999");

InstallerUI.Success($"Server: {serverIp}:{serverPort}");
await Task.Delay(300);

// ─── Step 5: Install BepInEx + Doorstop ───
Console.WriteLine();
InstallerUI.Step(5, "Install BepInEx mod loader");

var bepinexDir = Path.Combine(installDir, "bepinex");
Directory.CreateDirectory(bepinexDir);

var bepInExCore = Path.Combine(bepinexDir, "BepInEx", "core", "BepInEx.Core.dll");
if (!File.Exists(bepInExCore))
{
    await InstallerUI.Spinner(500, "Preparing BepInEx download");
    if (!await DownloadBepInEx(bepinexDir))
    {
        var bundled = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "bepinex");
        if (Directory.Exists(bundled))
        {
            InstallerUI.Info("Using bundled BepInEx...");
            CopyDirectory(bundled, bepinexDir);
        }
        else
        {
            InstallerUI.Error("BepInEx not available. Download manually from github.com/BepInEx/BepInEx");
            Console.WriteLine($"\n  {InstallerUI.Dim()}Press Enter to exit...{InstallerUI.Reset()}");
            Console.ReadLine();
            return;
        }
    }
}
else
{
    InstallerUI.Success("BepInEx already installed");
}

await InstallerUI.Spinner(300, "Configuring Doorstop");
if (SetupDoorstop(gameDir, bepinexDir))
    InstallerUI.Success("Doorstop configured");
else
    InstallerUI.Error("Failed to configure Doorstop");

// ─── Step 6: Install RecBack Patcher ───
Console.WriteLine();
InstallerUI.Step(6, "Install RecBack patches");

var pluginsDir = Path.Combine(gameDir, "BepInEx", "plugins");
Directory.CreateDirectory(pluginsDir);

var patcherDll = FindPatcherDll();
if (patcherDll != null)
{
    await InstallerUI.Spinner(300, "Installing patcher plugin");
    InstallPatcherPlugin(gameDir, patcherDll, $"{serverIp}:{serverPort}");
    InstallerUI.Success("RecBack Patcher plugin installed");
}
else
{
    InstallerUI.Warn("Patcher DLL not bundled. Install manually from GitHub releases.");
    var configDir = Path.Combine(gameDir, "BepInEx", "config");
    Directory.CreateDirectory(configDir);
    await File.WriteAllTextAsync(Path.Combine(configDir, "recback.patches.cfg"),
        $"[Nameserver]\nTarget = {serverIp}:{serverPort}\n");
    InstallerUI.Info("Config file created. Place RecBack.Patcher.dll in BepInEx/plugins/");
}

// ─── Step 7: Create Launchers ───
Console.WriteLine();
InstallerUI.Step(7, "Create launchers");

buildExe = FindBuildExe(gameDir) ?? "";
CreateLaunchers(gameDir, buildExe);
CreateSteamAppId(gameDir);

await Task.Delay(300);

// ─── Summary ───
Console.WriteLine();
Console.WriteLine($"  {InstallerUI.Color("92")}{new string('\u2550', 50)}{InstallerUI.Reset()}");
Console.WriteLine($"  {InstallerUI.Color("92")}RecBack v{Version} Installation Complete!{InstallerUI.Reset()}");
Console.WriteLine($"  {InstallerUI.Color("92")}{new string('\u2550', 50)}{InstallerUI.Reset()}");
Console.WriteLine();
InstallerUI.Info("Quick Start:");
InstallerUI.Info("1. Start the server:  dotnet run --project server/RecBack.Server");
InstallerUI.Info("2. Or on NAS:         docker compose up -d");
InstallerUI.Info($"3. Launch the game:   {Path.Combine(gameDir, "RecBack_ScreenMode.bat")}");
Console.WriteLine();
InstallerUI.Warn("The first launch may take a while as BepInEx generates interop assemblies.");
Console.WriteLine();
Console.Write($"  {InstallerUI.Dim()}Press Enter to exit...{InstallerUI.Reset()}");
Console.ReadLine();

// ═══════════════════════════════════════════════════════════════════
// Local Functions
// ═══════════════════════════════════════════════════════════════════

async Task CheckForUpdates()
{
    InstallerUI.Info("Checking for updates...");
    await InstallerUI.Spinner(1000, "Checking GitHub");

    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("RecBack-Installer/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

        var resp = await client.GetAsync("https://api.github.com/repos/itsinvin/RecBack/releases/latest");
        if (!resp.IsSuccessStatusCode) return;

        var json = await resp.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var tag = doc.RootElement.GetProperty("tag_name").GetString() ?? "";

        if (!string.IsNullOrEmpty(tag) && tag.TrimStart('v') != Version)
        {
            InstallerUI.Warn($"RecBack {tag} is available! You have v{Version}");
            if (InstallerUI.PromptYesNo("Download the latest version from GitHub?"))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/itsinvin/RecBack/releases/latest",
                    UseShellExecute = true
                });
            }
        }
    }
    catch
    {
        InstallerUI.Info("Could not check for updates (offline or no release yet)");
    }
}

async Task RunDepotDownloader(string destDir)
{
    InstallerUI.Info("You need a 2023 Rec Room build from Steam.");
    InstallerUI.Info("RecBack can use DepotDownloader to get it.");

    var ddDir = Path.Combine(installDir, "depotdownloader");
    var ddExe = Path.Combine(ddDir, "DepotDownloader.exe");

    if (!File.Exists(ddExe))
    {
        InstallerUI.Info("Downloading DepotDownloader...");
        Directory.CreateDirectory(ddDir);

        var ddUrl = "https://github.com/SteamRE/DepotDownloader/releases/latest/download/DepotDownloader-win-x64.zip";
        var ddZip = Path.Combine(installDir, "DepotDownloader.zip");

        await InstallerUI.DownloadWithProgress(ddUrl, ddZip, "  Downloading DepotDownloader");

        InstallerUI.Info("Extracting DepotDownloader...");
        ZipFile.ExtractToDirectory(ddZip, ddDir, overwriteFiles: true);
        File.Delete(ddZip);
    }

    if (!File.Exists(ddExe))
    {
        InstallerUI.Error("DepotDownloader executable not found");
        return;
    }

    Console.WriteLine();
    InstallerUI.Info("Rec Room Steam App ID: 471710");
    InstallerUI.Info("Depot ID: 471711 (Rec Room main content)");
    InstallerUI.Warn("You need a Steam account that owns Rec Room.");

    var username = InstallerUI.Prompt("Steam username");
    if (string.IsNullOrEmpty(username))
    {
        InstallerUI.Error("Steam username required");
        return;
    }

    var password = InstallerUI.PromptPassword("Steam password");
    if (string.IsNullOrEmpty(password))
    {
        InstallerUI.Error("Steam password required");
        return;
    }

    var manifestId = InstallerUI.Prompt("2023 build manifest ID (from SteamDB)", ManifestId);

    Console.WriteLine();
    InstallerUI.Info("Downloading Rec Room 2023 build...");
    InstallerUI.Warn("This will download ~5-10 GB. It may take a while.");

    var psi = new ProcessStartInfo(ddExe)
    {
        Arguments = $"-app 471710 -depot 471711 -manifest {manifestId} -username \"{username}\" -password \"{password}\" -dir \"{destDir}\"",
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true
    };

    var proc = new Process { StartInfo = psi };
    proc.OutputDataReceived += (_, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            Console.WriteLine($"  {InstallerUI.Dim()}{e.Data}{InstallerUI.Reset()}");
    };
    proc.ErrorDataReceived += (_, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            Console.WriteLine($"  {InstallerUI.Color("91")}{e.Data}{InstallerUI.Reset()}");
    };

    proc.Start();
    proc.BeginOutputReadLine();
    proc.BeginErrorReadLine();
    await proc.WaitForExitAsync();

    if (proc.ExitCode == 0)
        InstallerUI.Success("Build downloaded!");
    else
        InstallerUI.Error($"DepotDownloader failed with exit code {proc.ExitCode}");
}

async Task<bool> DownloadBepInEx(string destDir)
{
    InstallerUI.Info("Downloading BepInEx (IL2CPP loader)...");

    try
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("RecBack-Installer/1.0");
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github.v3+json");

        var resp = await client.GetAsync("https://api.github.com/repos/BepInEx/BepInEx/releases");
        if (!resp.IsSuccessStatusCode) return false;

        var json = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        string? zipUrl = null;
        foreach (var release in doc.RootElement.EnumerateArray())
        {
            foreach (var asset in release.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.Contains("win", StringComparison.OrdinalIgnoreCase) &&
                    name.Contains("x64", StringComparison.OrdinalIgnoreCase) &&
                    name.Contains("il2cpp", StringComparison.OrdinalIgnoreCase))
                {
                    zipUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
            }
            if (zipUrl != null) break;
        }

        zipUrl ??= "https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.1/BepInEx_Unity_IL2CPP_x64_6.0.0-pre.1.zip";

        var zipPath = Path.Combine(installDir, "bepinex.zip");
        InstallerUI.Info($"Downloading BepInEx...");
        await InstallerUI.DownloadWithProgress(zipUrl, zipPath, "  Downloading BepInEx");

        InstallerUI.Info("Extracting BepInEx...");
        ZipFile.ExtractToDirectory(zipPath, destDir, overwriteFiles: true);
        File.Delete(zipPath);

        InstallerUI.Success("BepInEx installed");
        return true;
    }
    catch (Exception ex)
    {
        InstallerUI.Error($"BepInEx download failed: {ex.Message}");
        return false;
    }
}

bool SetupDoorstop(string gameDir, string bepinexDir)
{
    try
    {
        var configPath = Path.Combine(gameDir, "doorstop_config.ini");
        File.WriteAllText(configPath, @"# RecBack Doorstop Configuration
[General]
enabled = true
target_assembly = BepInEx\core\BepInEx.Unity.IL2CPP.dll
redirect_output_log = false
ignore_disable_switch = false

[Il2Cpp]
coreclr_path = dotnet\coreclr.dll
corlib_dir = dotnet
");

        var winhttpSrc = Path.Combine(bepinexDir, "winhttp.dll");
        var winhttpDst = Path.Combine(gameDir, "winhttp.dll");
        if (File.Exists(winhttpSrc) && !File.Exists(winhttpDst))
            File.Copy(winhttpSrc, winhttpDst);

        var dotnetSrc = Path.Combine(bepinexDir, "dotnet");
        var dotnetDst = Path.Combine(gameDir, "dotnet");
        if (Directory.Exists(dotnetSrc) && !Directory.Exists(dotnetDst))
            CopyDirectory(dotnetSrc, dotnetDst);

        var bepInExSrc = Path.Combine(bepinexDir, "BepInEx");
        var bepInExDst = Path.Combine(gameDir, "BepInEx");
        if (Directory.Exists(bepInExSrc) && !Directory.Exists(bepInExDst))
            CopyDirectory(bepInExSrc, bepInExDst);

        return true;
    }
    catch (Exception ex)
    {
        InstallerUI.Error($"Doorstop setup failed: {ex.Message}");
        return false;
    }
}

void InstallPatcherPlugin(string gameDir, string patcherDll, string serverAddr)
{
    var pluginsDir = Path.Combine(gameDir, "BepInEx", "plugins");
    Directory.CreateDirectory(pluginsDir);
    File.Copy(patcherDll, Path.Combine(pluginsDir, "RecBack.Patcher.dll"), overwrite: true);

    var configDir = Path.Combine(gameDir, "BepInEx", "config");
    Directory.CreateDirectory(configDir);
    File.WriteAllText(Path.Combine(configDir, "recback.patches.cfg"),
        $"## RecBack Patcher Configuration\n[Nameserver]\n## Set this to your RecBack server IP:port\nTarget = {serverAddr}\n");
}

void CreateLaunchers(string gameDir, string exePath)
{
    var exeName = Path.GetFileName(exePath);

    File.WriteAllText(Path.Combine(gameDir, "RecBack_ScreenMode.bat"),
        $"@echo off\ntitle RecBack - Screen Mode\necho Starting RecBack...\nstart \"\" \"{exeName}\" +mode:screen\nexit\n");

    File.WriteAllText(Path.Combine(gameDir, "RecBack_VR.bat"),
        $"@echo off\ntitle RecBack - VR Mode\necho Starting RecBack...\nstart \"\" \"{exeName}\" +mode:vr\nexit\n");

    InstallerUI.Success("Created launchers (Screen Mode + VR)");
}

void CreateSteamAppId(string gameDir)
{
    File.WriteAllText(Path.Combine(gameDir, "steam_appid.txt"), "471710");
}

string? FindBuildExe(string directory)
{
    if (!Directory.Exists(directory)) return null;
    var candidates = new[] { "RecRoom.exe", "Recroom_Release.exe", "RecRoom_Release.exe", "RecBack.exe" };
    foreach (var c in candidates)
    {
        var path = Path.Combine(directory, c);
        if (File.Exists(path)) return path;
    }
    foreach (var f in Directory.GetFiles(directory, "*.exe"))
    {
        var name = Path.GetFileNameWithoutExtension(f).ToLowerInvariant();
        if (name.Contains("rec") || name.Contains("room"))
            return f;
    }
    return null;
}

string? FindPatcherDll()
{
    var paths = new[]
    {
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "build", "patcher", "RecBack.Patcher.dll"),
        Path.Combine(AppContext.BaseDirectory, "RecBack.Patcher.dll"),
        Path.Combine(installDir, "RecBack.Patcher.dll"),
    };
    foreach (var p in paths)
    {
        var full = Path.GetFullPath(p);
        if (File.Exists(full)) return full;
    }
    return null;
}

static void CopyDirectory(string sourceDir, string destDir)
{
    Directory.CreateDirectory(destDir);
    foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
    {
        var relative = Path.GetRelativePath(sourceDir, file);
        var dest = Path.Combine(destDir, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
        File.Copy(file, dest, overwrite: true);
    }
}
