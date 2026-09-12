using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BackpackSurvivor.EditorTools
{
    public static class QuestS12TelemetryAudit
    {
        [MenuItem("Tools/Backpack Survivor/Quest/S12 Export Telemetry Summary")]
        public static void Export()
        {
            var source = Path.Combine(Application.persistentDataPath, "quest_telemetry.jsonl");
            var projectRoot = Directory.GetParent(Directory.GetParent(Application.dataPath).FullName).FullName;
            var target = Path.Combine(projectRoot, "Docs/Evidence/S12/telemetry-summary.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (!File.Exists(source)) { File.WriteAllText(target, "NO_DATA telemetry file missing: " + source + "\n"); Debug.Log("[S12] no telemetry data"); return; }
            var lines = File.ReadAllLines(source);
            var completed = 0; var victory = 0; var died = 0;
            foreach (var line in lines) { if (line.IndexOf("\"completed\":true", StringComparison.OrdinalIgnoreCase) >= 0) completed++; if (line.IndexOf("\"outcome\":0", StringComparison.OrdinalIgnoreCase) >= 0) victory++; if (line.IndexOf("\"outcome\":1", StringComparison.OrdinalIgnoreCase) >= 0) died++; }
            File.WriteAllText(target, "rows=" + lines.Length + "\nvictoryRows=" + victory + "\ndiedRows=" + died + "\ncompletedRows=" + completed + "\nsource=" + source + "\n");
            Debug.Log("[S12] telemetry summary exported rows=" + lines.Length);
        }
    }
}
