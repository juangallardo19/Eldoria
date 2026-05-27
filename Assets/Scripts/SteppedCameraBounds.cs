using UnityEngine;

// Defines camera bounds with a stepped profile for rooms with irregular ceilings.
// Pattern: Value Object — declares the valid area shape as a piecewise linear curve along X.
// Advantage over multiple CameraBoundsZone: no zone-switch jumps — Y limits are
// smoothly interpolated as the player moves horizontally.
//
// USAGE:
//   Add to any scene GameObject. CameraFollow auto-detects it in Start.
//   Edit the "steps" array in the Inspector: each point defines (x, yMin, yMax) in world space.
//   Points must be ordered from smallest X to largest X.
//   The green gizmo in Scene View shows the exact valid-area profile.
public class SteppedCameraBounds : MonoBehaviour
{
    [Tooltip("Control points of the valid area. Order from left (smallest X) to right (largest X).")]
    public BoundsStep[] steps = new BoundsStep[]
    {
        new BoundsStep { x = -58f, yMin = -16f, yMax =  8f },
        new BoundsStep { x = -30f, yMin = -16f, yMax = 10f },
        new BoundsStep { x =   0f, yMin = -16f, yMax = 14f },
        new BoundsStep { x =  30f, yMin = -16f, yMax = 10f },
        new BoundsStep { x =  58f, yMin = -16f, yMax =  8f },
    };

    public float XMin => (steps != null && steps.Length > 0) ? steps[0].x : -30f;
    public float XMax => (steps != null && steps.Length > 0) ? steps[steps.Length - 1].x : 30f;

    // Returns (x: yMin, y: yMax) interpolated for the player's X position.
    public Vector2 GetYBoundsAtX(float x)
    {
        if (steps == null || steps.Length == 0)
            return new Vector2(-10f, 10f);

        if (x <= steps[0].x)
            return new Vector2(steps[0].yMin, steps[0].yMax);

        if (x >= steps[steps.Length - 1].x)
            return new Vector2(steps[steps.Length - 1].yMin, steps[steps.Length - 1].yMax);

        for (int i = 0; i < steps.Length - 1; i++)
        {
            if (x >= steps[i].x && x <= steps[i + 1].x)
            {
                float t = (x - steps[i].x) / (steps[i + 1].x - steps[i].x);
                return new Vector2(
                    Mathf.Lerp(steps[i].yMin, steps[i + 1].yMin, t),
                    Mathf.Lerp(steps[i].yMax, steps[i + 1].yMax, t));
            }
        }

        return new Vector2(steps[steps.Length - 1].yMin, steps[steps.Length - 1].yMax);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (steps == null || steps.Length < 2) return;

        // Ceiling and floor profile
        Gizmos.color = new Color(0.1f, 0.95f, 0.25f, 0.85f);
        for (int i = 0; i < steps.Length - 1; i++)
        {
            Gizmos.DrawLine(new Vector3(steps[i].x,     steps[i].yMax,     0),
                            new Vector3(steps[i+1].x,   steps[i+1].yMax,   0));
            Gizmos.DrawLine(new Vector3(steps[i].x,     steps[i].yMin,     0),
                            new Vector3(steps[i+1].x,   steps[i+1].yMin,   0));
            Gizmos.DrawLine(new Vector3(steps[i].x,     steps[i].yMin,     0),
                            new Vector3(steps[i].x,     steps[i].yMax,     0));
        }
        int last = steps.Length - 1;
        Gizmos.DrawLine(new Vector3(steps[last].x, steps[last].yMin, 0),
                        new Vector3(steps[last].x, steps[last].yMax, 0));

        // Semi-transparent fill per segment
        Gizmos.color = new Color(0.1f, 0.95f, 0.25f, 0.06f);
        for (int i = 0; i < steps.Length - 1; i++)
        {
            float cx   = (steps[i].x    + steps[i+1].x)    / 2f;
            float cyMn = (steps[i].yMin + steps[i+1].yMin) / 2f;
            float cyMx = (steps[i].yMax + steps[i+1].yMax) / 2f;
            Gizmos.DrawCube(new Vector3(cx, (cyMn + cyMx) / 2f, 0),
                            new Vector3(steps[i+1].x - steps[i].x, cyMx - cyMn, 0.1f));
        }

        // Y labels at each control point
        var style = new GUIStyle
        {
            fontSize = 9,
            normal   = { textColor = new Color(0.1f, 0.95f, 0.25f) }
        };
        for (int i = 0; i < steps.Length; i++)
        {
            UnityEditor.Handles.Label(
                new Vector3(steps[i].x, steps[i].yMax + 0.3f, 0),
                $"top {steps[i].yMax:F1}", style);
            UnityEditor.Handles.Label(
                new Vector3(steps[i].x, steps[i].yMin - 1.2f, 0),
                $"bot {steps[i].yMin:F1}", style);
        }
    }
#endif
}

[System.Serializable]
public struct BoundsStep
{
    [Tooltip("X position of this control point (world space)")]
    public float x;
    [Tooltip("Lower camera bound at this X")]
    public float yMin;
    [Tooltip("Upper camera bound at this X")]
    public float yMax;
}
