// SRDebugger Options — Spout controls
// Changing Source Name reconnects the SpoutReceiver live (no restart needed).

using System.ComponentModel;

public partial class SROptions
{
    private const string SpoutCat = "Spout";

    [Category(SpoutCat), Sort(0)]
    [DisplayName("Source Name")]
    public string SpoutSourceName
    {
        get => BlobPreviz.ConfigManager.Instance?.SpoutSourceName ?? "";
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.SpoutSourceName = value; }
    }
}
