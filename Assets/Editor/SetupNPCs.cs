using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// Pattern: Command — editor menus for creating NPC idle animations and placing them in the scene.
// Each NPC has its own menu; all use the same generic CreateNPC() method.
public static class SetupNPCs
{
    // ── Durgan in HV01_Exterior ───────────────────────────────────────────
    [MenuItem("Eldoria/NPCs/Setup Durgan (HV01_Exterior)")]
    public static void SetupDurgan()
    {
        var spritePrefix = "Assets/Sprites/DurganGOOD/durganEstandoTranqui";
        CreateNPC("NPC_Durgan", "Durgan", spritePrefix, 6,
                  "HV01_Exterior", new Vector3(-40f, -26f, 0f));
    }

    // ── Lyara in HV05 ────────────────────────────────────────────────────
    [MenuItem("Eldoria/NPCs/Setup Lyara (HV05)")]
    public static void SetupLyara()
    {
        var spritePrefix = "Assets/Sprites/Lyara/LyaraEstandoTranqui";
        CreateNPC("NPC_Lyara", "Lyara", spritePrefix, 6,
                  "HV05", new Vector3(0f, -20f, 0f));
    }

    // ── Generic method ────────────────────────────────────────────────────
    private static void CreateNPC(string goName, string animName,
                                  string spritePrefix, int frameCount,
                                  string requiredScene, Vector3 position)
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.name != requiredScene)
        {
            EditorUtility.DisplayDialog("Eldoria",
                $"Open the '{requiredScene}' scene before running this menu.", "OK");
            return;
        }

        // ── 1. Cargar frames ──────────────────────────────────────────────
        Sprite[] frames = new Sprite[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            string path = $"{spritePrefix}{i + 1}.png";
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (frames[i] == null)
            {
                Debug.LogError($"[SetupNPCs] Sprite not found: {path}");
                return;
            }
        }

        // ── 2. Carpeta de animaciones ─────────────────────────────────────
        string animDir = $"Assets/Animations/{animName}";
        if (!AssetDatabase.IsValidFolder(animDir))
            AssetDatabase.CreateFolder("Assets/Animations", animName);

        // ── 3. AnimationClip idle (8 fps, loop) ───────────────────────────
        string clipPath = $"{animDir}/{animName}Idle.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath)
                   ?? new AnimationClip();

        clip.frameRate = 8f;
        clip.wrapMode  = WrapMode.Loop;

        var keys = new ObjectReferenceKeyframe[frameCount];
        for (int i = 0; i < frameCount; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / 8f, value = frames[i] };

        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);

        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) == null)
            AssetDatabase.CreateAsset(clip, clipPath);
        else
            EditorUtility.SetDirty(clip);

        // ── 4. AnimatorController ─────────────────────────────────────────
        string ctrlPath = $"{animDir}/{animName}Animator.controller";
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        if (ctrl == null)
        {
            ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
            var sm   = ctrl.layers[0].stateMachine;
            var idle = sm.AddState("Idle");
            idle.motion     = clip;
            sm.defaultState = idle;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // ── 5. Colocar NPC en escena ──────────────────────────────────────
        var existing = GameObject.Find(goName);
        if (existing != null)
        {
            GameObject.DestroyImmediate(existing);
            Debug.Log($"[SetupNPCs] Existing {goName} removed and recreated.");
        }

        var npc = new GameObject(goName);
        npc.transform.position = position;

        var sr       = npc.AddComponent<SpriteRenderer>();
        sr.sprite              = frames[0];
        sr.sortingLayerName    = "Characters";

        var animator = npc.AddComponent<Animator>();
        animator.runtimeAnimatorController = ctrl;

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[SetupNPCs] {goName} placed in '{scene.name}' at {position}. " +
                  "Adjust position in Scene View if needed.");
    }
}
