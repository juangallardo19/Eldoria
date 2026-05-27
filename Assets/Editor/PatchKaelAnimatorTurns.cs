using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

// Menu: Eldoria/Kael/Remove Animator Turns
// Removes states IdleTurn, RunTurn, WalkTurn, RunToIdle and their parameters
// from KaelAnimator.controller, leaving only the base animations.
public static class PatchKaelAnimatorTurns
{
    const string CtrlPath = "Assets/Animations/Kael/KaelAnimator.controller";

    static readonly string[] TurnNames =
        { "IdleTurn", "RunTurn", "WalkTurn", "RunToIdle" };

    [MenuItem("Eldoria/Kael/Remove Animator Turns")]
    static void Run()
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlPath);
        if (ctrl == null)
        {
            EditorUtility.DisplayDialog("Eldoria",
                "KaelAnimator.controller not found at " + CtrlPath, "OK");
            return;
        }

        var sm = ctrl.layers[0].stateMachine;

        foreach (var name in TurnNames)
        {
            // ── Remove AnyState → state transitions ───────────────────────
            foreach (var t in sm.anyStateTransitions
                .Where(t => t.destinationState?.name == name).ToArray())
            {
                sm.RemoveAnyStateTransition(t);
                Debug.Log($"[UnpatchKael] AnyState → '{name}' transition removed.");
            }

            // ── Remove state ──────────────────────────────────────────────
            var stateMatch = sm.states.FirstOrDefault(s => s.state.name == name);
            if (stateMatch.state != null)
            {
                sm.RemoveState(stateMatch.state);
                Debug.Log($"[UnpatchKael] State '{name}' removed.");
            }

            // ── Remove parameter ──────────────────────────────────────────
            var param = ctrl.parameters.FirstOrDefault(p => p.name == name);
            if (param != null)
            {
                ctrl.RemoveParameter(param);
                Debug.Log($"[UnpatchKael] Parameter '{name}' removed.");
            }
        }

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("[UnpatchKael] ✓ KaelAnimator cleaned — base animations only.");
    }
}
