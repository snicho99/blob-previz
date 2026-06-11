// SRDebugger Options — OSC controls
// Changing IP or Port reconnects the OscEmitter live (no restart needed).

using System.ComponentModel;

public partial class SROptions
{
    private const string OscCat = "OSC";

    [Category(OscCat), Sort(0)]
    [DisplayName("Target IP")]
    public string OscTargetIp
    {
        get => BlobPreviz.ConfigManager.Instance?.OscTargetIp ?? "127.0.0.1";
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.OscTargetIp = value; }
    }

    [Category(OscCat), Sort(1)]
    [DisplayName("Port")]
    [NumberRange(1024, 65535)]
    public int OscPort
    {
        get => BlobPreviz.ConfigManager.Instance?.OscPort ?? 9000;
        set { if (BlobPreviz.ConfigManager.Instance != null) BlobPreviz.ConfigManager.Instance.OscPort = value; }
    }
}
