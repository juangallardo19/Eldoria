using UnityEngine;
using UnityEngine.UI;

// Adjuntado a los botones de tab del mapa. Se autocablea al WorldMapController en Start.
[RequireComponent(typeof(Button))]
public class WorldMapTabButton : MonoBehaviour
{
    public bool isHubTab;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            if (isHubTab) WorldMapController.Instance?.ShowHubTab();
            else          WorldMapController.Instance?.ShowMtnTab();
        });
    }
}
