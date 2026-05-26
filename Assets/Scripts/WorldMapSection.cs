using UnityEngine;
using UnityEngine.UI;

// Datos de una sección del mapa. Se adjunta a cada Image de zona.
// Patrón: Value Object
public class WorldMapSection : MonoBehaviour
{
    public string   zoneId;
    public string[] sceneNames;
    public Sprite   normalSprite;
    public Sprite   activeSprite;

    [HideInInspector] public Image sectionImage;

    void Awake() => sectionImage = GetComponent<Image>();
}
