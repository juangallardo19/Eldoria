using UnityEngine;

// ScriptableObject que centraliza los retratos de diálogo del tutorial.
// Crear/actualizar: Eldoria/Setup Tutorial Config
[CreateAssetMenu(menuName = "Eldoria/Tutorial Config")]
public class TutorialConfig : ScriptableObject
{
    [Header("Retratos de Diálogo")]
    public Sprite kaelDialoguePortrait;
    public Sprite araDialoguePortrait;
    public Sprite durganDialoguePortrait;
    public Sprite liaraDialoguePortrait;

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Eldoria/Setup Tutorial Config")]
    static void SetupConfig()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources");
        const string ASSET_PATH = "Assets/Resources/TutorialConfig.asset";

        var cfg = UnityEditor.AssetDatabase.LoadAssetAtPath<TutorialConfig>(ASSET_PATH);
        if (cfg == null)
        {
            cfg = CreateInstance<TutorialConfig>();
            UnityEditor.AssetDatabase.CreateAsset(cfg, ASSET_PATH);
        }

        var so = new UnityEditor.SerializedObject(cfg);
        Assign(so, "kaelDialoguePortrait",   "Assets/Sprites/Talk/Kael/Normal/DialogoKael1.png");
        Assign(so, "araDialoguePortrait",    "Assets/Sprites/Talk/Ara/Normal/DialogoAra1.png");
        Assign(so, "durganDialoguePortrait", "Assets/Sprites/Talk/Durgan/Idle.png");
        Assign(so, "liaraDialoguePortrait",  "Assets/Sprites/Talk/Lyara/Idle.png");
        so.ApplyModifiedProperties();
        UnityEditor.EditorUtility.SetDirty(cfg);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.Selection.activeObject = cfg;
        Debug.Log("[TutorialConfig] Setup completo — retratos asignados.");
    }

    static void Assign(UnityEditor.SerializedObject so, string prop, string path)
    {
        var p   = so.FindProperty(prop);
        var spr = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (p != null && spr != null) p.objectReferenceValue = spr;
        else Debug.LogWarning($"[TutorialConfig] No encontrado: {path}");
    }
#endif
}
