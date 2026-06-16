using HarmonyLib;
using BepInEx.Logging;

namespace RecBack.Patcher;

public static class NetworkPatcher
{
    private static Configuration? _config;
    private static ManualLogSource? _log;

    public static void Patch(Harmony harmony, Configuration config, ManualLogSource log)
    {
        _config = config;
        _log = log;

        var bestHTTP = System.AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "BestHTTP");

        if (bestHTTP == null)
        {
            _log.LogWarning("BestHTTP assembly not found");
            return;
        }

        var httpRequest = bestHTTP.GetType("BestHTTP.HTTPRequest");
        if (httpRequest == null)
        {
            _log.LogWarning("BestHTTP.HTTPRequest type not found");
            return;
        }

        var targetMethods = new[] { "Send", "SendAndProcessRequest" };
        foreach (var method in AccessTools.GetDeclaredMethods(httpRequest))
        {
            if (targetMethods.Contains(method.Name))
            {
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(NetworkPatcher).GetMethod(nameof(Prefix_HTTPRequest))));
                _log.LogInfo($"Patched BestHTTP.HTTPRequest.{method.Name}");
            }
        }

        _log.LogInfo("Network patches applied");
    }

    public static bool Prefix_HTTPRequest(object __instance)
    {
        try
        {
            var uriProp = AccessTools.Property(__instance.GetType(), "Uri");
            if (uriProp == null) return true;

            var uri = uriProp.GetValue(__instance);
            if (uri == null) return true;

            var toString = AccessTools.Method(uri.GetType(), "ToString");
            if (toString == null) return true;

            var url = toString.Invoke(uri, null) as string;
            if (url == null) return true;

            var redirected = RedirectUrl(url);
            if (redirected == url) return true;

            var uriCtor = AccessTools.Constructor(uri.GetType(), new[] { typeof(string) });
            if (uriCtor == null) return true;

            var newUri = uriCtor.Invoke(new object[] { redirected });
            uriProp.SetValue(__instance, newUri);
            _log.LogInfo($"Redirected: {url} -> {redirected}");
        }
        catch { }
        return true;
    }

    private static string RedirectUrl(string url)
    {
        if (_config == null) return url;
        var target = _config.NameserverTarget;

        if (url.Contains("ns.rec.net") || url.Contains("nameserver.rec.net"))
        {
            return $"http://{target}/";
        }

        if (url.Contains("api.rec.net"))
            return url.Replace("api.rec.net", target.Contains(":") ? target.Split(':')[0] : target);

        if (url.Contains("img.rec.net"))
            return url.Replace("img.rec.net", target.Contains(":") ? target.Split(':')[0] : target);

        return url;
    }
}
