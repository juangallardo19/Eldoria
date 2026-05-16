using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public const int SlotCount = 4;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private string SlotPath(int slot) =>
        Path.Combine(Application.persistentDataPath, $"save_slot_{slot}.json");

    public SaveData Load(int slot)
    {
        string path = SlotPath(slot);
        if (!File.Exists(path)) return new SaveData();
        return JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
    }

    public void Save(int slot, SaveData data)
    {
        data.isEmpty = false;
        File.WriteAllText(SlotPath(slot), JsonUtility.ToJson(data, true));
    }

    public void Delete(int slot)
    {
        string path = SlotPath(slot);
        if (File.Exists(path)) File.Delete(path);
    }

    public static int ActiveSlot { get; private set; } = -1;

    public void SelectSlot(int slot) => ActiveSlot = slot;
}
