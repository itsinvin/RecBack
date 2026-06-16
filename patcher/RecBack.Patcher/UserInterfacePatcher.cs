using HarmonyLib;
using BepInEx.Logging;

namespace RecBack.Patcher;

public static class UserInterfacePatcher
{
    private static ManualLogSource? _log;

    public static void Patch(Harmony harmony, ManualLogSource log)
    {
        _log = log;

        PatchScreenTypes(harmony, new[] { "HomeScreen", "EventsScreen", "NotificationsScreen", "PeopleScreen" });
        PatchGradient(harmony);
        PatchPhotonAppSettings(harmony);

        _log.LogInfo("UI patches applied");
    }

    private static void PatchScreenTypes(Harmony harmony, string[] screenNames)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in asm.GetTypes())
            {
                if (type.FullName == null) continue;
                if (!type.FullName.Contains("RRUI")) continue;

                foreach (var screen in screenNames)
                {
                    if (!type.Name.Contains(screen)) continue;

                    foreach (var method in AccessTools.GetDeclaredMethods(type))
                    {
                        if (method.Name == "Show" || method.Name == "OnEnable" || method.Name == "Start")
                        {
                            try
                            {
                                harmony.Patch(method, prefix: new HarmonyMethod(typeof(UserInterfacePatcher).GetMethod(nameof(EnableScreen))));
                                _log.LogInfo($"Patched UI: {type.Name}.{method.Name}");
                            }
                            catch { }
                        }
                    }
                }
            }
        }
    }

    private static void PatchGradient(Harmony harmony)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var gradientType = asm.GetType("RRUI.UIGradient");
            if (gradientType == null) continue;

            var gradientProp = AccessTools.Property(gradientType, "Gradient");
            var setGradient = gradientProp?.SetMethod;
            if (setGradient != null)
            {
                harmony.Patch(setGradient, prefix: new HarmonyMethod(typeof(UserInterfacePatcher).GetMethod(nameof(Prefix_Gradient))));
                _log.LogInfo("Patched RRUI.UIGradient.set_Gradient");
            }
        }
    }

    private static void PatchPhotonAppSettings(Harmony harmony)
    {
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                var appSettingsType = asm.GetType("Photon.Realtime.AppSettings");
                if (appSettingsType == null) continue;

                foreach (var method in AccessTools.GetDeclaredMethods(appSettingsType))
                {
                    if (method.Name == "ToString" || method.Name == "get_UseNameServer")
                    {
                        harmony.Patch(method, postfix: new HarmonyMethod(typeof(UserInterfacePatcher).GetMethod(nameof(Postfix_AppSettings))));
                        _log.LogInfo($"Patched Photon: AppSettings.{method.Name}");
                    }
                }
            }
            catch { }
        }
    }

    public static bool EnableScreen()
    {
        return false;
    }

    public static bool Prefix_Gradient()
    {
        return false;
    }

    public static void Postfix_AppSettings(ref bool __result)
    {
        __result = false;
    }
}
