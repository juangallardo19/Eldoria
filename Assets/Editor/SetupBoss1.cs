using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// Menú: Eldoria/Boss/2 - Setup Boss1 en MTN10
// Ejecutar DESPUÉS de "Importar Sprites Boss1". Requiere escena MTN10 activa.
// Crea: AnimatorController + AnimationClips + prefabs de proyectil/boomerang + Boss GO + HealthBar.
public static class SetupBoss1
{
    const string SpritesBase = "Assets/Sprites/Boss1Obsesion/Sprite Sheets/";
    const string AnimDir     = "Assets/Animations/Boss1Obsesion";
    const string PrefabDir   = "Assets/Prefabs/Boss";

    // (stateName, fileName, fps, loop)
    static readonly (string state, string file, float fps, bool loop)[] Anims =
    {
        ("Sleep",      "static sleep ", 4f,  true),
        ("Wake",       "wake",          8f,  false),
        ("Idle",       "idle",          8f,  true),
        ("Move",       "move",          12f, true),
        ("TurnRight",  "turnaound to right", 8f, false),
        ("TurnLeft",   "turnaround to left",  8f, false),
        ("Melee",      "Melee attack",  12f, false),
        ("Range",      "range attack",  10f, false),
        ("SpinCharge", "Spin charge",   10f, false),
        ("SpinEnd",    "Spin charge end", 8f, false),
        ("Boomerang",  "boomarang arms", 10f, false),
        ("Buff",       "buff",           8f, false),
        ("Super",      "super attack",  10f, false),
        ("Death",      "death",          8f, false),
    };

    [MenuItem("Eldoria/Boss/2 - Setup Boss1 en MTN10")]
    static void Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != "MTN10")
        {
            EditorUtility.DisplayDialog("Eldoria",
                "Abre la escena MTN10 antes de ejecutar este menú.", "OK");
            return;
        }

        EnsureFolders();

        // ── 1. AnimatorController ─────────────────────────────────────────
        string ctrlPath = $"{AnimDir}/Boss1Obsesion.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
            ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);

        var sm = ctrl.layers[0].stateMachine;

        // Limpiar estados previos
        foreach (var s in sm.states.ToArray())
            sm.RemoveState(s.state);

        // ── 2. AnimationClips ─────────────────────────────────────────────
        var clipMap = new Dictionary<string, AnimationClip>();

        foreach (var (stateName, fileName, fps, loop) in Anims)
        {
            string spritePath = SpritesBase + fileName + ".png";
            var sprites = AssetDatabase.LoadAllAssetsAtPath(spritePath)
                .OfType<Sprite>()
                .OrderBy(s => ParseIndex(s.name))
                .ToArray();

            if (sprites.Length == 0)
            {
                Debug.LogWarning($"[SetupBoss1] Sin sprites en: {spritePath} " +
                                 "— ejecuta primero 'Importar Sprites Boss1'.");
                continue;
            }

            string clipPath = $"{AnimDir}/Boss1_{stateName}.anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath)
                       ?? new AnimationClip();
            clip.frameRate = fps;
            clip.wrapMode  = loop ? WrapMode.Loop : WrapMode.ClampForever;

            var keys = new ObjectReferenceKeyframe[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
                keys[i] = new ObjectReferenceKeyframe
                {
                    time  = i / fps,
                    value = sprites[i]
                };

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) == null)
                AssetDatabase.CreateAsset(clip, clipPath);
            else
                EditorUtility.SetDirty(clip);

            clipMap[stateName] = clip;

            // Agregar estado al AnimatorController
            var state  = sm.AddState(stateName);
            state.motion = clip;
            if (stateName == "Idle") sm.defaultState = state;
        }

        AssetDatabase.SaveAssets();

        // ── 3. Prefab de proyectil ────────────────────────────────────────
        string projPath = $"{PrefabDir}/BossProjectile.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(projPath) == null)
        {
            var projGO = new GameObject("BossProjectile");
            projGO.AddComponent<SpriteRenderer>().sortingOrder = 5;
            projGO.AddComponent<BoxCollider2D>().isTrigger     = true;
            projGO.AddComponent<BossProjectile>();
            PrefabUtility.SaveAsPrefabAsset(projGO, projPath);
            Object.DestroyImmediate(projGO);
        }

        // ── 4. Prefab de boomerang ────────────────────────────────────────
        string boomPath = $"{PrefabDir}/BossBoomerang.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(boomPath) == null)
        {
            var boomGO = new GameObject("BossBoomerang");
            boomGO.AddComponent<SpriteRenderer>().sortingOrder = 5;
            boomGO.AddComponent<BoxCollider2D>().isTrigger     = true;
            boomGO.AddComponent<BossBoomerang>();
            PrefabUtility.SaveAsPrefabAsset(boomGO, boomPath);
            Object.DestroyImmediate(boomGO);
        }

        AssetDatabase.Refresh();

        // ── 5. Boss GameObject en escena ──────────────────────────────────
        var existing = GameObject.Find("Boss_Obsesion");
        if (existing != null) Object.DestroyImmediate(existing);

        EnsureTag("Enemy");
        var bossGO = new GameObject("Boss_Obsesion");
        bossGO.transform.position  = new Vector3(0f, -11f, 0f);
        bossGO.transform.localScale = new Vector3(3.5f, 3.5f, 1f);
        bossGO.tag = "Enemy";

        // SpriteRenderer
        var sr = bossGO.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 2;
        // Asignar primer frame del idle como sprite inicial
        var idleSprites = AssetDatabase.LoadAllAssetsAtPath(SpritesBase + "idle.png")
            .OfType<Sprite>().OrderBy(s => ParseIndex(s.name)).ToArray();
        if (idleSprites.Length > 0) sr.sprite = idleSprites[0];

        // Rigidbody2D
        var rb = bossGO.AddComponent<Rigidbody2D>();
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;
        rb.gravityScale = 1f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // BoxCollider2D (cuerpo — recibe daño de Kael)
        var bodyCol  = bossGO.AddComponent<BoxCollider2D>();
        bodyCol.size = new Vector2(1.8f, 1.5f);

        // Animator
        var anim = bossGO.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;

        // BossObsesionAnimator
        bossGO.AddComponent<BossObsesionAnimator>();

        // ── Hitbox melee (hijo) ───────────────────────────────────────────
        var meleeHbGO  = new GameObject("MeleeHitbox");
        meleeHbGO.transform.SetParent(bossGO.transform);
        meleeHbGO.transform.localPosition = new Vector3(1.5f, 0f, 0f);
        var meleeHbCol = meleeHbGO.AddComponent<BoxCollider2D>();
        meleeHbCol.size      = new Vector2(2.5f, 1.4f);
        meleeHbCol.isTrigger = true;
        meleeHbCol.enabled   = false;
        var meleeHb = meleeHbGO.AddComponent<BossAttackHitbox>();

        // ── Hitbox spin (hijo) ────────────────────────────────────────────
        var spinHbGO  = new GameObject("SpinHitbox");
        spinHbGO.transform.SetParent(bossGO.transform);
        spinHbGO.transform.localPosition = Vector3.zero;
        var spinHbCol = spinHbGO.AddComponent<BoxCollider2D>();
        spinHbCol.size      = new Vector2(3.5f, 1.6f);
        spinHbCol.isTrigger = true;
        spinHbCol.enabled   = false;
        var spinHb = spinHbGO.AddComponent<BossAttackHitbox>();

        // ── RangeSpawnPoint (hijo) ────────────────────────────────────────
        var rspGO = new GameObject("RangeSpawnPoint");
        rspGO.transform.SetParent(bossGO.transform);
        rspGO.transform.localPosition = new Vector3(2f, 0.5f, 0f);

        // ── Cablear BossObsesion ──────────────────────────────────────────
        var boss = bossGO.AddComponent<BossObsesion>();
        var so   = new SerializedObject(boss);

        so.FindProperty("meleeHitbox").objectReferenceValue = meleeHb;
        so.FindProperty("spinHitbox") .objectReferenceValue = spinHb;
        so.FindProperty("rangeSpawnPoint").objectReferenceValue = rspGO.transform;

        var projPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(projPath);
        var boomPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(boomPath);
        so.FindProperty("projectilePrefab").objectReferenceValue = projPrefab;
        so.FindProperty("boomerangPrefab") .objectReferenceValue = boomPrefab;

        so.ApplyModifiedPropertiesWithoutUndo();

        // ── 6. BossHealthBar ──────────────────────────────────────────────
        var existing2 = GameObject.Find("BossHealthBar");
        if (existing2 != null) Object.DestroyImmediate(existing2);

        var hbGO = new GameObject("BossHealthBar");
        hbGO.AddComponent<BossHealthBar>();

        // ── 7. Guardar ────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[SetupBoss1] ✓ Boss_Obsesion colocado en MTN10. " +
                  "Ajusta la escala del boss y posición en Scene View. " +
                  "Asigna un 'projectileSprite' en Inspector si quieres sprite en proyectiles.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(AnimDir))
        {
            AssetDatabase.CreateFolder("Assets/Animations", "Boss1Obsesion");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Boss");
    }

    static void EnsureTag(string tag)
    {
        var tm = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags = tm.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.arraySize++;
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tm.ApplyModifiedProperties();
    }

    static int ParseIndex(string spriteName)
    {
        var parts = spriteName.Split('_');
        return int.TryParse(parts[parts.Length - 1], out int idx) ? idx : 0;
    }
}
