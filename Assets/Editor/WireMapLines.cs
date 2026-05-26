#if UNITY_EDITOR
// Menú: Eldoria/Wire Map Lines
// Asigna WorldMapLine (zoneIdA/zoneIdB) a cada objeto Line del canvas existente
// usando el NOMBRE del objeto como fuente de verdad:
//   Formato: "Line{ZoneA}-{NumB}"  →  ej. "LineMTN05-06", "LineHUB01-07"
//   Sufijos de segmento L-shape ignorados: "LineMTN11-12(2)" → MTN11/MTN12 igual que "LineMTN11-12"
// Seguro de ejecutar sobre el canvas editado a mano sin tocar posiciones.
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class WireMapLines
{
    [MenuItem("Eldoria/Wire Map Lines")]
    static void Execute()
    {
        var panel = GameObject.Find("[WorldMapPanel]");
        if (panel == null) { Debug.LogError("[Eldoria] No se encontró [WorldMapPanel]."); return; }

        var mapCanvas = panel.transform.Find("MapCanvas");
        if (mapCanvas == null) { Debug.LogError("[Eldoria] No se encontró MapCanvas."); return; }

        int hubWired = WireContainer(mapCanvas.Find("HubContainer"));
        int mtnWired = WireContainer(mapCanvas.Find("MtnContainer"));

        EditorUtility.SetDirty(panel);
        EditorSceneManager.MarkSceneDirty(panel.scene);
        Debug.Log($"[Eldoria] Wire Map Lines: {hubWired} HUB + {mtnWired} MTN cableadas.");
    }

    // Parsea el nombre "Line{ZoneA}-{NumB}" para extraer los zone IDs.
    // "LineMTN11-12(2)" → zoneA="MTN11", zoneB="MTN12"
    static bool TryParseLineName(string name, out string zoneA, out string zoneB)
    {
        zoneA = zoneB = "";
        if (!name.StartsWith("Line")) return false;

        var raw = name.Substring(4); // "MTN05-06" o "MTN11-12(2)"
        // Quitar sufijo de segmento "(n)" o "(n("
        int parenIdx = raw.IndexOf('(');
        if (parenIdx >= 0) raw = raw.Substring(0, parenIdx).Trim();

        int dash = raw.IndexOf('-');
        if (dash < 2) return false;

        zoneA = raw.Substring(0, dash);          // "MTN05"
        var numB = raw.Substring(dash + 1);      // "06"
        if (zoneA.Length < 3) return false;

        var prefix = zoneA.Substring(0, 3);      // "MTN" o "HUB"
        zoneB = prefix + numB;                   // "MTN06"
        return true;
    }

    static int WireContainer(Transform container)
    {
        if (container == null) return 0;
        int wired = 0;

        for (int i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            if (child.GetComponent<WorldMapSection>() != null) continue;
            if (!TryParseLineName(child.name, out var zA, out var zB))
            {
                Debug.LogWarning($"[Eldoria] WireMapLines: '{child.name}' no sigue el formato LineMTN01-02. Ignorada.");
                continue;
            }

            Undo.RecordObject(child.gameObject, "Wire Map Lines");
            var wml = child.GetComponent<WorldMapLine>();
            if (wml == null) wml = Undo.AddComponent<WorldMapLine>(child.gameObject);

            wml.zoneIdA = zA;
            wml.zoneIdB = zB;
            wired++;
        }
        return wired;
    }
}
#endif
