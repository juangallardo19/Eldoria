using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.Linq;

// Menú: Eldoria/Kael/Remove Animator Turns
// Elimina los estados IdleTurn, RunTurn, WalkTurn, RunToIdle y sus parámetros
// del KaelAnimator.controller, dejando solo las animaciones base.
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
                "No se encontró KaelAnimator.controller en " + CtrlPath, "OK");
            return;
        }

        var sm = ctrl.layers[0].stateMachine;

        foreach (var name in TurnNames)
        {
            // ── Eliminar transiciones AnyState → estado ───────────────────
            foreach (var t in sm.anyStateTransitions
                .Where(t => t.destinationState?.name == name).ToArray())
            {
                sm.RemoveAnyStateTransition(t);
                Debug.Log($"[UnpatchKael] Transición AnyState → '{name}' eliminada.");
            }

            // ── Eliminar estado ───────────────────────────────────────────
            var stateMatch = sm.states.FirstOrDefault(s => s.state.name == name);
            if (stateMatch.state != null)
            {
                sm.RemoveState(stateMatch.state);
                Debug.Log($"[UnpatchKael] Estado '{name}' eliminado.");
            }

            // ── Eliminar parámetro ────────────────────────────────────────
            var param = ctrl.parameters.FirstOrDefault(p => p.name == name);
            if (param != null)
            {
                ctrl.RemoveParameter(param);
                Debug.Log($"[UnpatchKael] Parámetro '{name}' eliminado.");
            }
        }

        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssets();
        Debug.Log("[UnpatchKael] ✓ KaelAnimator limpiado — solo animaciones base.");
    }
}
