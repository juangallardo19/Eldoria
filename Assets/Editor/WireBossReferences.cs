using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

// Menu: Eldoria/Boss/3 - Wire Boss References
// Wires private references of BossObsesion in the active scene.
// Run AFTER scripts compile and Boss_Obsesion exists in the scene.
public static class WireBossReferences
{
    const string PrefabDir = "Assets/Prefabs/Boss";

    [MenuItem("Eldoria/Boss/3 - Wire Boss References")]
    static void Run()
    {
        var bossGO = GameObject.Find("Boss_Obsesion");
        if (bossGO == null)
        {
            EditorUtility.DisplayDialog("Eldoria", "Boss_Obsesion not found in the active scene.", "OK");
            return;
        }

        var boss = bossGO.GetComponent<BossObsesion>();
        if (boss == null)
        {
            EditorUtility.DisplayDialog("Eldoria", "Boss_Obsesion is missing the BossObsesion component.", "OK");
            return;
        }

        var so = new SerializedObject(boss);

        // ── Hitboxes ──────────────────────────────────────────────────────────
        var meleeGO = bossGO.transform.Find("MeleeHitbox");
        var spinGO  = bossGO.transform.Find("SpinHitbox");
        var rspGO   = bossGO.transform.Find("RangeSpawnPoint");

        if (meleeGO != null)
        {
            var hb = meleeGO.GetComponent<BossAttackHitbox>();
            so.FindProperty("meleeHitbox").objectReferenceValue = hb;
            Debug.Log("[WireBoss] meleeHitbox → " + (hb != null ? "OK" : "NULL (missing BossAttackHitbox)"));
        }
        else Debug.LogWarning("[WireBoss] MeleeHitbox child not found.");

        if (spinGO != null)
        {
            var hb = spinGO.GetComponent<BossAttackHitbox>();
            so.FindProperty("spinHitbox").objectReferenceValue = hb;
            Debug.Log("[WireBoss] spinHitbox → " + (hb != null ? "OK" : "NULL (missing BossAttackHitbox)"));
        }
        else Debug.LogWarning("[WireBoss] SpinHitbox child not found.");

        if (rspGO != null)
        {
            so.FindProperty("rangeSpawnPoint").objectReferenceValue = rspGO;
            Debug.Log("[WireBoss] rangeSpawnPoint → OK");
        }
        else Debug.LogWarning("[WireBoss] RangeSpawnPoint child not found.");

        // ── Prefabs ───────────────────────────────────────────────────────────
        var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/BossProjectile.prefab");
        var boomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/BossBoomerang.prefab");

        so.FindProperty("projectilePrefab").objectReferenceValue = projPrefab;
        so.FindProperty("boomerangPrefab") .objectReferenceValue = boomPrefab;

        Debug.Log("[WireBoss] projectilePrefab → " + (projPrefab != null ? "OK" : "not found — run Setup Boss1 first"));
        Debug.Log("[WireBoss] boomerangPrefab  → " + (boomPrefab  != null ? "OK" : "not found — run Setup Boss1 first"));

        // ── Boomerang frames (boomarang arms.png sub-sprites) ────────────────
        var boomerangAssets = AssetDatabase.LoadAllAssetsAtPath(
            "Assets/Sprites/Boss1Obsesion/Sprite Sheets/boomarang arms.png");
        var boomerangSprites = new System.Collections.Generic.List<Sprite>();
        foreach (var a in boomerangAssets)
            if (a is Sprite s) boomerangSprites.Add(s);
        boomerangSprites.Sort((a, b) =>
            System.StringComparer.OrdinalIgnoreCase.Compare(a.name, b.name));

        var framesProp = so.FindProperty("boomerangFrames");
        framesProp.arraySize = boomerangSprites.Count;
        for (int i = 0; i < boomerangSprites.Count; i++)
            framesProp.GetArrayElementAtIndex(i).objectReferenceValue = boomerangSprites[i];
        Debug.Log($"[WireBoss] boomerangFrames → {boomerangSprites.Count} frames assigned");

        // ── AnimatorController ────────────────────────────────────────────────
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animations/Boss1Obsesion/Boss1Obsesion.controller");
        if (ctrl != null)
        {
            var anim = bossGO.GetComponent<Animator>();
            if (anim != null) anim.runtimeAnimatorController = ctrl;
            Debug.Log("[WireBoss] AnimatorController → OK");
        }
        else
            Debug.LogWarning("[WireBoss] Controller not found — run 'Eldoria/Boss/2 - Setup Boss1' first.");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(bossGO);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("[WireBoss] ✓ Boss_Obsesion references wired and scene saved.");
    }
}
