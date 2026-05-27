using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

// Aplica Perfect DOS VGA 437 Win SDF a todos los TMP_Text de escenas y prefabs.
// Procesa un asset por tick — no bloquea el editor.
public static class SetAllFonts
{
    const string TMP_PATH = "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset";
    const string TTF_PATH = "Assets/UI/Fonts/Perfect DOS VGA 437 Win.ttf";

    static Queue<string> _queue;
    static TMP_FontAsset _tmpFont;
    static Font          _ttfFont;
    static int           _tmpCount, _ttfCount, _scenesChanged, _prefabsChanged, _total;
    static bool          _running;

    [MenuItem("Eldoria/Set All Fonts")]
    static void Run()
    {
        if (_running) { Debug.LogWarning("[SetAllFonts] Ya está en proceso."); return; }

        _tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TMP_PATH);
        _ttfFont = AssetDatabase.LoadAssetAtPath<Font>(TTF_PATH);
        if (_tmpFont == null) { Debug.LogError("[SetAllFonts] No encontró: " + TMP_PATH); return; }

        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        _queue = new Queue<string>();
        _tmpCount = _ttfCount = _scenesChanged = _prefabsChanged = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            _queue.Enqueue(AssetDatabase.GUIDToAssetPath(guid));

        foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            _queue.Enqueue(AssetDatabase.GUIDToAssetPath(guid));

        _total   = _queue.Count;
        _running = true;
        EditorApplication.update += Tick;
        Debug.Log($"[SetAllFonts] Iniciando — {_total} assets en cola.");
    }

    static void Tick()
    {
        if (_queue == null || _queue.Count == 0)
        {
            EditorApplication.update -= Tick;
            _running = false;
            Finish();
            return;
        }

        string path = _queue.Dequeue();
        int done = _total - _queue.Count;

        if (path.EndsWith(".unity"))
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool dirty = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var t in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.font == _tmpFont) continue;
                    t.font = _tmpFont; EditorUtility.SetDirty(t); _tmpCount++; dirty = true;
                }
                if (_ttfFont != null)
                    foreach (var t in root.GetComponentsInChildren<Text>(true))
                    {
                        if (t.font == _ttfFont) continue;
                        t.font = _ttfFont; EditorUtility.SetDirty(t); _ttfCount++; dirty = true;
                    }
            }
            if (dirty) { EditorSceneManager.SaveScene(scene); _scenesChanged++; }
            Debug.Log($"[SetAllFonts] [{done}/{_total}] {System.IO.Path.GetFileName(path)}");
        }
        else
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return;
            bool dirty = false;
            foreach (var t in prefab.GetComponentsInChildren<TMP_Text>(true))
            {
                if (t.font == _tmpFont) continue;
                t.font = _tmpFont; EditorUtility.SetDirty(t); _tmpCount++; dirty = true;
            }
            if (_ttfFont != null)
                foreach (var t in prefab.GetComponentsInChildren<Text>(true))
                {
                    if (t.font == _ttfFont) continue;
                    t.font = _ttfFont; EditorUtility.SetDirty(t); _ttfCount++; dirty = true;
                }
            if (dirty) { PrefabUtility.SavePrefabAsset(prefab); _prefabsChanged++; }
        }
    }

    static void Finish()
    {
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        string msg = $"Completado.\n\nTMP_Text actualizados : {_tmpCount}\n" +
                     $"Text legacy           : {_ttfCount}\n\n" +
                     $"Escenas modificadas   : {_scenesChanged}\n" +
                     $"Prefabs modificados   : {_prefabsChanged}";
        Debug.Log("[SetAllFonts] " + msg);
        EditorUtility.DisplayDialog("Set All Fonts — Eldoria", msg, "OK");
    }
}
