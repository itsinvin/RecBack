using HarmonyLib;
using BepInEx.Logging;

namespace RecBack.Patcher;

public static class AntiCheatPatcher
{
    private static ManualLogSource? _log;
    private static int _patchedCount;

    public static void Patch(Harmony harmony, ManualLogSource log)
    {
        _log = log;
        _patchedCount = 0;

        PatchByTypeNamePattern(harmony, "EACManager");
        PatchByTypeNamePattern(harmony, "Rranticheat");
        PatchByTypeNamePattern(harmony, "AntiCheat");
        PatchByTypeNamePattern(harmony, "VAC");
        PatchByTypeNamePattern(harmony, "AntiDebug");
        PatchByTypeNamePattern(harmony, "AntiTamper");

        _log.LogInfo($"EAC patches applied: {_patchedCount} methods patched");
    }

    private static void PatchByTypeNamePattern(Harmony harmony, string pattern)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.FullName == null) continue;
                if (!type.FullName.Contains(pattern, System.StringComparison.OrdinalIgnoreCase)) continue;

                foreach (var method in AccessTools.GetDeclaredMethods(type))
                {
                    if (method.Name.Contains("Check") ||
                        method.Name.Contains("Validate") ||
                        method.Name.Contains("Init") ||
                        method.Name.Contains("Initialize") ||
                        method.Name.Contains("Start") ||
                        method.Name.Contains("Bypass") ||
                        method.Name.Contains("Detect") ||
                        method.Name.Contains("Verify") ||
                        method.Name.Contains("Authenticate") ||
                        method.Name.Contains("Challenge") ||
                        method.Name.Contains("DUID") ||
                        method.Name.Contains("Process"))
                    {
                        try
                        {
                            harmony.Patch(method, prefix: new HarmonyMethod(typeof(AntiCheatPatcher).GetMethod(nameof(SkipMethod))));
                            _log.LogInfo($"Bypassed EAC: {type.Name}.{method.Name}");
                            _patchedCount++;
                        }
                        catch { }
                    }
                }
            }
        }
    }

    public static bool SkipMethod()
    {
        return false;
    }
}
