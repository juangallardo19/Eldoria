using UnityEngine;

// Define los límites de cámara con perfil escalonado para salas de techo irregular.
// Patrón: Value Object — declara la forma del área válida como una curva lineal por tramos en X.
// Ventaja sobre CameraBoundsZone múltiple: no hay saltos de zona — los límites de Y
// se interpolan suavemente a medida que el jugador avanza horizontalmente.
//
// USO:
//   Añadir a cualquier GameObject de la escena. CameraFollow lo detecta en Start automáticamente.
//   Editar el array "steps" en Inspector: cada punto define (x, yMin, yMax) en espacio mundo.
//   Los puntos deben estar ordenados de menor X a mayor X.
//   El gizmo verde en Scene View muestra el perfil exacto del área válida.
public class SteppedCameraBounds : MonoBehaviour
{
    [Tooltip("Puntos de control del área válida. Ordenar de izquierda (X menor) a derecha (X mayor).")]
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

    // Devuelve (x: yMin, y: yMax) interpolados según la posición X del jugador.
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

        // Perfil de techo y suelo
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

        // Relleno semitransparente por tramo
        Gizmos.color = new Color(0.1f, 0.95f, 0.25f, 0.06f);
        for (int i = 0; i < steps.Length - 1; i++)
        {
            float cx   = (steps[i].x    + steps[i+1].x)    / 2f;
            float cyMn = (steps[i].yMin + steps[i+1].yMin) / 2f;
            float cyMx = (steps[i].yMax + steps[i+1].yMax) / 2f;
            Gizmos.DrawCube(new Vector3(cx, (cyMn + cyMx) / 2f, 0),
                            new Vector3(steps[i+1].x - steps[i].x, cyMx - cyMn, 0.1f));
        }

        // Etiquetas de Y en cada punto de control
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
    [Tooltip("Posición X de este punto de control (espacio mundo)")]
    public float x;
    [Tooltip("Límite inferior del área de cámara en esta X")]
    public float yMin;
    [Tooltip("Límite superior del área de cámara en esta X")]
    public float yMax;
}
