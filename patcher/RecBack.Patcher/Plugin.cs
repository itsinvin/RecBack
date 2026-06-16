using BepInEx;
using BepInEx.Unity.IL2CPP;
using BepInEx.Logging;
using HarmonyLib;

namespace RecBack.Patcher;

[BepInPlugin("recback.patcher", "RecBack Patcher", "1.0.0")]
public class Plugin : BasePlugin
{
    public override void Load()
    {
        Log.LogInfo("RecBack Patcher v1.0.0 loading...");

        var config = Configuration.Load();
        var harmony = new Harmony("recback.patcher");

        NetworkPatcher.Patch(harmony, config, Log);
        AntiCheatPatcher.Patch(harmony, Log);
        UserInterfacePatcher.Patch(harmony, Log);

        Log.LogInfo($"RecBack initialized. Nameserver: {config.NameserverTarget}");
    }
}
