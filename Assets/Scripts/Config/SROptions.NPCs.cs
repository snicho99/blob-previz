// SRDebugger Options — NPC controls
// [Category] is System.ComponentModel.CategoryAttribute.
// [NumberRange], [Sort], [DisplayName] are SROptions inner classes — no qualifier needed.

using System.ComponentModel;

public partial class SROptions
{
    private const string NpcCat = "NPCs";

    [Category(NpcCat), Sort(0)]
    [DisplayName("Wandering Enabled")]
    public bool WanderingEnabled
    {
        get => BlobPreviz.ConfigManager.Instance != null && BlobPreviz.ConfigManager.Instance.WanderingEnabled;
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.WanderingEnabled = value; }
    }

    [Category(NpcCat), Sort(1)]
    [DisplayName("NPC Count")]
    [NumberRange(0, 20)]
    public int NPCCount
    {
        get => BlobPreviz.ConfigManager.Instance?.NpcCount ?? 2;
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.NpcCount = value; }
    }

    [Category(NpcCat), Sort(2)]
    [DisplayName("Wandering Speed")]
    [NumberRange(0.3, 6.0)]
    public float WanderingSpeed
    {
        get => BlobPreviz.ConfigManager.Instance?.WanderingSpeed ?? 1.4f;
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.WanderingSpeed = value; }
    }

    [Category(NpcCat), Sort(3)]
    [DisplayName("Speed Variability")]
    [NumberRange(0.0, 3.0)]
    public float WanderingSpeedVariability
    {
        get => BlobPreviz.ConfigManager.Instance?.WanderingSpeedVariability ?? 0f;
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.WanderingSpeedVariability = value; }
    }

    [Category(NpcCat), Sort(4)]
    [DisplayName("Visible")]
    public bool NpcVisible
    {
        get => BlobPreviz.ConfigManager.Instance == null || BlobPreviz.ConfigManager.Instance.NpcVisible;
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.NpcVisible = value; }
    }
}
