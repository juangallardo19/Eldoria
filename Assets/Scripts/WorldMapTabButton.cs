using UnityEngine;
using UnityEngine.UI;

// Attached to map tab buttons. Self-wires to WorldMapController in Start.
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
