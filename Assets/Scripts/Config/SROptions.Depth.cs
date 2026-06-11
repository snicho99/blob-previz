// SRDebugger Options — Depth texture controls

using System.ComponentModel;

public partial class SROptions
{
    private const string DepthCat = "Depth";

    [Category(DepthCat), Sort(0)]
    [DisplayName("Spout Output Name")]
    public string DepthSpoutName
    {
        get => BlobPreviz.ConfigManager.Instance?.DepthSpoutName ?? "BlobPrevizDepth";
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.DepthSpoutName = value; }
    }

    [Category(DepthCat), Sort(1)]
    [DisplayName("Range Min (m)")]
    [NumberRange(0f, 10f)]
    public float DepthRangeMin
    {
        get => BlobPreviz.ConfigManager.Instance?.DepthRangeMin ?? 0.3f;
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.DepthRangeMin = value; }
    }

    [Category(DepthCat), Sort(2)]
    [DisplayName("Range Max (m)")]
    [NumberRange(0.1f, 20f)]
    public float DepthRangeMax
    {
        get => BlobPreviz.ConfigManager.Instance?.DepthRangeMax ?? 5.0f;
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.DepthRangeMax = value; }
    }
}
