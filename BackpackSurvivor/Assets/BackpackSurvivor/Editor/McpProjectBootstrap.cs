using System;
using System.IO;
using MCPForUnity.Editor.Helpers;
using MCPForUnity.Editor.Services;
using MCPForUnity.Editor.Services.Transport;
using UnityEditor;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    /// <summary>Connects this editor to the existing local MCP server without changing global preferences.</summary>
    public static class McpProjectBootstrap
    {
        private const string ExpectedEndpoint = "http://127.0.0.1:8095";

        [Serializable]
        private sealed class ConnectionReport
        {
            public bool connected;
            public string endpoint;
            public string projectPath;
            public string unityVersion;
            public string error;
        }

        [MenuItem("Tools/Backpack Survivor/Connect Local MCP")]
        public static async void Connect()
        {
            var report = new ConnectionReport
            {
                endpoint = ExpectedEndpoint,
                projectPath = Directory.GetParent(Application.dataPath).FullName,
                unityVersion = Application.unityVersion
            };
            try
            {
                // Existing local settings are shared by Unity editors. Validate them; do not overwrite them.
                if (HttpEndpointUtility.IsRemoteScope() ||
                    !string.Equals(HttpEndpointUtility.GetBaseUrl().TrimEnd('/'), ExpectedEndpoint, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Existing Unity MCP endpoint must be local http://127.0.0.1:8095. No settings were changed.");
                }
                report.connected = await MCPServiceLocator.TransportManager.StartAsync(TransportMode.Http);
                if (!report.connected)
                    report.error = "The existing local MCP server did not accept the editor connection.";
            }
            catch (Exception exception)
            {
                report.error = exception.Message;
                Debug.LogException(exception);
            }

            string workspace = Directory.GetParent(report.projectPath).FullName;
            string logPath = Path.Combine(workspace, "Tools", "UnityMCP", "Logs", "unity-connection.json");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.WriteAllText(logPath, JsonUtility.ToJson(report, true));
            Debug.Log("BACKPACK_MCP_CONNECTED=" + report.connected);
        }
    }
}
