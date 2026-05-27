#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using TMPro;

// Runs before BossBoomerangBuildPreProcess (order 0).
// Forces the embedded Material back onto the TMP font asset so TMP_FontAsset.ReadFontAssetDefinition()
// never hits a null material reference during build preprocessing.
public class TMPFontMaterialBuildFix : IPreprocessBuildWithReport
{
    public int callbackOrder => -10;

    const string FONT_PATH = "Assets/UI/Fonts/Perfect DOS VGA 437 Win SDF.asset";

    public void OnPreprocessBuild(BuildReport report)
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FONT_PATH);
        if (font == null)
        {
            Debug.LogWarning("[TMPFontMaterialBuildFix] Font asset not found at: " + FONT_PATH);
            return;
        }

        // Find the embedded Material in the same .asset file
        Material embeddedMat = null;
        foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(FONT_PATH))
        {
            if (obj is Material m) { embeddedMat = m; break; }
        }

        if (embeddedMat == null)
        {
            Debug.LogWarning("[TMPFontMaterialBuildFix] No embedded Material found in font asset.");
            return;
        }

        // Re-assign via SerializedObject so Unity serializes it properly before the build
        var so   = new SerializedObject(font);
        var prop = so.FindProperty("material");
        if (prop != null)
        {
            prop.objectReferenceValue = embeddedMat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Also set directly on the object in case SO path isn't enough
        font.material = embeddedMat;
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TMPFontMaterialBuildFix] ✓ Material '{embeddedMat.name}' assigned to '{font.name}'.");
    }
}
#endif
