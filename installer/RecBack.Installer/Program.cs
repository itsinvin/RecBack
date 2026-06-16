using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using RecBack.Installer;

Console.OutputEncoding = System.Text.Encoding.UTF8;

const string Version = "1.0.0";

// Where to download the pre-packaged 2023 Rec Room build archive
// This gets the latest build archive from your GitHub releases
const string BuildArchiveBase = "https://github.com/itsinvin/RecBack/releases/latest/download/RecRoom_2023-03-21.7z";
const int BuildArchiveParts = 2;

try { Console.Write(InstallerUI.HideCursor()); } catch { }

await InstallerUI.AnimateLogo();
await InstallerUI.AnimateText("Welcome to RecBack - The Rec Room Revival Project!");
await Task.Delay(300);

try { Console.Write(InstallerUI.ShowCursor()); } catch { }

// ─── Step 1: Install Directory ───
Console.WriteLine();
InstallerUI.Step(1, "Choose where to install RecBack");

var defaultDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "RecBack");
var installDir = InstallerUI.Prompt("Installation directory", defaultDir);
installDir = Path.GetFullPath(installDir);
Directory.CreateDirectory(installDir);

var gameDir = Path.Combine(installDir, "game");

InstallerUI.Success($"Will install to: {installDir}");
await Task.Delay(300);

// ─── Step 2: Download the build ───
Console.WriteLine();
InstallerUI.Step(2, "Download Rec Room 2023 build");

Directory.CreateDirectory(gameDir);

var existingExe = FindBuildExe(gameDir);
if (existingExe != null)
{
    InstallerUI.Success($"Found existing build: {Path.GetFileName(existingExe)}");
    if (!InstallerUI.PromptYesNo("Re-download?", false))
        goto installBepInEx;
}

InstallerUI.Info("Downloading Rec Room 21 March 2023 build (~5-10 GB)...");
InstallerUI.Warn("This is a large download and may take a while.");

if (!await DownloadBuild(gameDir))
{
    InstallerUI.Error("Build download failed. Make sure the archive exists in GitHub Releases.");
    Console.WriteLine($"\n  {InstallerUI.Dim()}Press Enter to exit...{InstallerUI.Reset()}");
    Console.ReadLine();
    return;
}

existingExe = FindBuildExe(gameDir);
if (existingExe == null)
{
    InstallerUI.Error("Build downloaded but no executable found. The archive may be incorrect.");
    Console.WriteLine($"\n  {InstallerUI.Dim()}Press Enter to exit...{InstallerUI.Reset()}");
    Console.ReadLine();
    return;
}

InstallerUI.Success($"Build ready: {Path.GetFileName(existingExe)}");
await Task.Delay(300);

// ─── Step 3: Configure Server ───
installBepInEx:
Console.WriteLine();
InstallerUI.Step(3, "Configure your RecBack server");

var local = InstallerUI.PromptYesNo("Running server on this machine?", true);
var serverIp = InstallerUI.Prompt("RecBack server IP address", local ? "localhost" : "192.168.1.100");
var serverPort = InstallerUI.Prompt("Server port (NameServer)", "9999");

InstallerUI.Success($"Server: {serverIp}:{serverPort}");
await Task.Delay(300);

// ─── Step 4: Install BepInEx + Doorstop ───
Console.WriteLine();
InstallerUI.Step(4, "Install BepInEx mod loader");

var bepinexDir = Path.Combine(installDir, "bepinex");
Directory.CreateDirectory(bepinexDir);

if (!File.Exists(Path.Combine(bepinexDir, "BepInEx", "core", "BepInEx.Core.dll")))
{
    if (!await DownloadBepInEx(bepinexDir))
    {
        InstallerUI.Error("BepInEx download failed.");
        Console.WriteLine($"\n  {InstallerUI.Dim()}Press Enter to exit...{InstallerUI.Reset()}");
        Console.ReadLine();
        return;
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

// ─── Step 5: Install Patcher Plugin ───
Console.WriteLine();
InstallerUI.Step(5, "Install RecBack patches");

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
    InstallerUI.Info("Download from: https://github.com/itsinvin/RecBack/releases");
}

// ─── Step 6: Create Launchers ───
Console.WriteLine();
InstallerUI.Step(6, "Create launchers");

existingExe = FindBuildExe(gameDir) ?? "";
CreateLaunchers(gameDir, existingExe);
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
InstallerUI.Warn("First launch takes a while as BepInEx generates interop assemblies.");
Console.WriteLine();
Console.Write($"  {InstallerUI.Dim()}Press Enter to exit...{InstallerUI.Reset()}");
Console.ReadLine();

// ═══════════════════════════════════════════════════════════════════
// Local Functions
// ═══════════════════════════════════════════════════════════════════

async Task<bool> DownloadBuild(string destDir)
{
    try
    {
        var archiveDir = Path.Combine(installDir, ".archives");
        Directory.CreateDirectory(archiveDir);

        // Download 7z CLI (standalone 7za.exe) from our release
        var sevenzaPath = Path.Combine(archiveDir, "7za.exe");
        if (!File.Exists(sevenzaPath))
        {
            InstallerUI.Info("Downloading extraction tool...");
            await InstallerUI.DownloadWithProgress(
                "https://github.com/itsinvin/RecBack/releases/latest/download/7za.exe",
                sevenzaPath, "  Downloading 7z CLI");
        }

        // Download all archive parts
        for (int i = 1; i <= BuildArchiveParts; i++)
        {
            var partName = $"RecRoom_2023-03-21.7z.{i:D3}";
            var partPath = Path.Combine(archiveDir, partName);
            if (File.Exists(partPath)) continue;

            var partUrl = $"{BuildArchiveBase}.{i:D3}";
            await InstallerUI.DownloadWithProgress(partUrl, partPath,
                $"  Downloading part {i}/{BuildArchiveParts}");
        }

        // Extract using 7za with progress
        InstallerUI.Info("Extracting build...");
        var firstPart = Path.Combine(archiveDir, $"RecRoom_2023-03-21.7z.001");
        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = sevenzaPath,
            Arguments = $"x \"{firstPart}\" -o\"{destDir}\" -y -bsp1",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        })!;

        // Read progress from stdout
        while (!proc.HasExited)
        {
            var line = await proc.StandardOutput.ReadLineAsync();
            if (line != null && !string.IsNullOrWhiteSpace(line))
                Console.Write($"\r  {InstallerUI.Color("96")}{line.Trim(),-60}{InstallerUI.Reset()}");
        }
        Console.Write(new string(' ', 70) + "\r");
        await proc.WaitForExitAsync();

        if (proc.ExitCode != 0)
        {
            var err = await proc.StandardError.ReadToEndAsync();
            throw new Exception($"7z extraction failed (exit {proc.ExitCode}): {err}");
        }

        // Cleanup
        Directory.Delete(archiveDir, recursive: true);
        return true;
    }
    catch (Exception ex)
    {
        InstallerUI.Error($"Build download failed: {ex.Message}");
        return false;
    }
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
            foreach (var asset in release.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.Contains("win") && name.Contains("x64") && name.Contains("il2cpp"))
                {
                    zipUrl = asset.GetProperty("browser_download_url").GetString();
                    break;
                }
                if (zipUrl != null) break;
            }

        zipUrl ??= "https://github.com/BepInEx/BepInEx/releases/download/v6.0.0-pre.1/BepInEx_Unity_IL2CPP_x64_6.0.0-pre.1.zip";

        var zipPath = Path.Combine(installDir, "bepinex.zip");
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
        File.WriteAllText(Path.Combine(gameDir, "doorstop_config.ini"), @"# RecBack Doorstop Configuration
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
        $"## RecBack Patcher Configuration\n[Nameserver]\nTarget = {serverAddr}\n");
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
        Path.Combine(AppContext.BaseDirectory, "RecBack.Patcher.dll"),
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "build", "patcher", "RecBack.Patcher.dll"),
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
