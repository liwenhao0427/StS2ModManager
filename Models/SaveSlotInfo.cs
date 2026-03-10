namespace StS2ModManager.Models;

public class SaveSlotInfo
{
    public int SlotId { get; set; }
    public bool HasNormalSave { get; set; }
    public bool HasModdedSave { get; set; }
    public DateTime? NormalSaveTime { get; set; }
    public DateTime? ModdedSaveTime { get; set; }

    public string DisplayStatus
    {
        get
        {
            if (HasNormalSave && HasModdedSave) return "有Mod存档 | 有非Mod存档";
            if (HasNormalSave) return "仅有非Mod存档";
            if (HasModdedSave) return "仅有Mod存档";
            return "无存档";
        }
    }
}
