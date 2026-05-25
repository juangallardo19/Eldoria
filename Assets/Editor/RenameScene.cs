using UnityEditor;

public static class RenameScene
{
    [MenuItem("Eldoria/Rename MTN10 to PreMTN10")]
    static void Run()
    {
        string error = AssetDatabase.RenameAsset(
            "Assets/Scenes/Montanas/MTN10.unity", "PreMTN10");

        if (string.IsNullOrEmpty(error))
            UnityEngine.Debug.Log("[RenameScene] MTN10.unity renombrado a PreMTN10.unity correctamente.");
        else
            UnityEngine.Debug.LogError("[RenameScene] Error: " + error);

        AssetDatabase.Refresh();
    }
}
