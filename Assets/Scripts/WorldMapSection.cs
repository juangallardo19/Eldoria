using UnityEngine;
using UnityEngine.UI;

// Data for a map section. Attached to each zone Image.
// Pattern: Value Object
public class WorldMapSection : MonoBehaviour
{
    public string   zoneId;
    public string[] sceneNames;
    public Sprite   normalSprite;
    public Sprite   activeSprite;

    [HideInInspector] public Image sectionImage;

    void Awake() => sectionImage = GetComponent<Image>();
}
