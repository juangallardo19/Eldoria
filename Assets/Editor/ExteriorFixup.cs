#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Corrige tres problemas de HV01_Exterior de un solo golpe:
//   1. Plataformas elevadas → EdgeCollider2D (sin geometría lateral, pasa desde cualquier dirección)
//   2. Cámara → modo FollowBounded con límites correctos para la escena 4x
//   3. DayCycleController → distancias mucho más largas (ciclo completo ~30 min a walkSpeed)
// Menú: Eldoria → Fix Exterior Scene
public static class ExteriorFixup
{
    [MenuItem("Eldoria/Fix Exterior Scene")]
    static void Fix()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "HV01_Exterior")
        {
            Debug.LogWarning("[ExteriorFixup] Abre HV01_Exterior primero.");
            return;
        }

        FixPlatforms();
        FixCamera();
        FixDayCycle();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[ExteriorFixup] ✓ Plataformas=Edge, Cámara=FollowBounded, DayCycle=20000 units.");
    }

    // ── 1. Plataformas ────────────────────────────────────────────────────────
    // BoxCollider2D tiene lados sólidos aunque PlatformEffector2D esté activo.
    // EdgeCollider2D es solo una línea → sin geometría lateral ni inferior.
    static void FixPlatforms()
    {
        var group = GameObject.Find("ElevatedPlatforms");
        if (group == null) { Debug.LogWarning("[ExteriorFixup] ElevatedPlatforms no encontrado."); return; }

        int count = 0;
        foreach (Transform child in group.transform)
        {
            // Leer el ancho del BoxCollider2D antes de destruirlo
            float width = 10f;
            var bc = child.GetComponent<BoxCollider2D>();
            if (bc != null) { width = bc.size.x; Object.DestroyImmediate(bc); }

            // Quitar EdgeCollider2D previo si ya se ejecutó el script antes
            var old = child.GetComponent<EdgeCollider2D>();
            if (old != null) Object.DestroyImmediate(old);

            var ec = child.gameObject.AddComponent<EdgeCollider2D>();
            float hw = width * 0.5f;
            ec.SetPoints(new List<Vector2> { new Vector2(-hw, 0f), new Vector2(hw, 0f) });
            ec.usedByEffector = true;
            ec.edgeRadius = 0f;

            // PlatformEffector2D: surfaceArc=180 es suficiente para EdgeCollider
            // (solo tiene superficie superior — no hay lados que bloquear)
            var pe = child.GetComponent<PlatformEffector2D>();
            if (pe == null) pe = child.gameObject.AddComponent<PlatformEffector2D>();
            pe.useOneWay         = true;
            pe.useOneWayGrouping = true;
            pe.surfaceArc        = 180f;
            pe.rotationalOffset  = 0f;

            EditorUtility.SetDirty(child.gameObject);
            count++;
        }
        Debug.Log($"[ExteriorFixup] {count} plataformas → EdgeCollider2D.");
    }

    // ── 2. Cámara ─────────────────────────────────────────────────────────────
    // Escena 4x: suelo en world y≈-28.8, extensión ±84 unidades.
    // boundsMin.y=-35 → centro cámara mín = -30 (cubre suelo + margen).
    // boundsMax.y=10  → centro cámara máx =  5 (muestra cielo).
    static void FixCamera()
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[ExteriorFixup] Camera.main no encontrado."); return; }

        var cf = cam.GetComponent<CameraFollow>();
        if (cf == null) { Debug.LogWarning("[ExteriorFixup] CameraFollow no encontrado."); return; }

        cf.mode      = CameraFollow.CameraMode.FollowBounded;
        cf.boundsMin = new Vector2(-84f, -35f);
        cf.boundsMax = new Vector2( 84f,  10f);
        EditorUtility.SetDirty(cf);

        // Alejar la cámara aumentando orthographicSize (5 se sentía muy cerca)
        cam.orthographicSize = 8f;
        EditorUtility.SetDirty(cam);

        Debug.Log("[ExteriorFixup] Cámara → FollowBounded, bounds (-84,-35)→(84,10), orthoSize=8.");
    }

    // ── 3. DayCycleController ─────────────────────────────────────────────────
    // A walkSpeed=8: cycleEnd=20000 → ciclo completo en ~41 minutos.
    // A runSpeed=16: ciclo en ~21 minutos.
    static void FixDayCycle()
    {
        var go = GameObject.Find("DayCycle");
        if (go == null) { Debug.LogWarning("[ExteriorFixup] DayCycle no encontrado."); return; }

        var dc = go.GetComponent<DayCycleController>();
        if (dc == null) return;

        dc.dawnAt   = 3000f;
        dc.dayAt    = 7500f;
        dc.duskAt   = 14000f;
        dc.cycleEnd = 20000f;
        EditorUtility.SetDirty(dc);
        Debug.Log("[ExteriorFixup] DayCycle → 20000 units/ciclo.");
    }
}
#endif
