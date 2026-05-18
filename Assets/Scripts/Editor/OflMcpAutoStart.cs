using System;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;

[InitializeOnLoad]
public static class OflMcpAutoStart
{
    static OflMcpAutoStart()
    {
        EditorApplication.delayCall += StartMcpBridge;
    }

    private static async void StartMcpBridge()
    {
        try
        {
            var ankleBreakerBridgeType = Type.GetType("UnityMCP.Editor.MCPBridgeServer, AnkleBreaker.UnityMCP.Editor");
            if (ankleBreakerBridgeType != null)
            {
                UnityEngine.Debug.Log("[OFL MCP] AnkleBreaker Unity MCP detected; package auto-start is active.");
                return;
            }

            EditorPrefs.SetBool("MCPForUnity.UseHttpTransport", true);
            EditorPrefs.SetString("MCPForUnity.HttpTransportScope", "local");
            EditorPrefs.SetString("MCPForUnity.HttpUrl", "http://127.0.0.1:8080");
            var uvxPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Python",
                "Python312",
                "Scripts",
                "uvx.exe");
            EditorPrefs.SetString("MCPForUnity.UvxPath", uvxPath);
            EditorPrefs.SetBool("MCPForUnity.AutoStartOnLoad", true);

            var configCacheType = Type.GetType("MCPForUnity.Editor.Services.EditorConfigurationCache, MCPForUnity.Editor");
            var configInstance = configCacheType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            configCacheType?.GetMethod("Refresh", BindingFlags.Public | BindingFlags.Instance)?.Invoke(configInstance, null);

            await Task.Delay(TimeSpan.FromSeconds(2));

            var serviceLocatorType = Type.GetType("MCPForUnity.Editor.Services.MCPServiceLocator, MCPForUnity.Editor");
            var bridge = serviceLocatorType?.GetProperty("Bridge", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            var startAsyncMethod = bridge?.GetType().GetMethod("StartAsync", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);

            if (startAsyncMethod == null)
            {
                UnityEngine.Debug.Log("[OFL MCP] MCPForUnity package not found or incompatible; auto-start skipped.");
                return;
            }

            var startTask = startAsyncMethod.Invoke(bridge, null) as Task<bool>;
            var started = startTask != null && await startTask;
            UnityEngine.Debug.Log(started
                ? "[OFL MCP] Unity MCP bridge connected to http://127.0.0.1:8080."
                : "[OFL MCP] Unity MCP bridge did not start. Open Window > MCP For Unity to inspect status.");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning($"[OFL MCP] Auto-start failed: {ex.Message}");
        }
    }
}
