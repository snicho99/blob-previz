// SRDebugger Options — Spout controls
// Source Name can be typed directly or cycled through live senders with Prev/Next.

using System;
using System.ComponentModel;
using Klak.Spout;

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

    [Category(SpoutCat), Sort(1)]
    [DisplayName("< Prev Source")]
    public void SpoutPrevSource() => CycleSpoutSource(-1);

    [Category(SpoutCat), Sort(2)]
    [DisplayName("Next Source >")]
    public void SpoutNextSource() => CycleSpoutSource(+1);

    void CycleSpoutSource(int delta)
    {
        if (BlobPreviz.ConfigManager.Instance == null) return;
        var sources = SpoutManager.GetSourceNames();
        if (sources == null || sources.Length == 0) return;

        var current = BlobPreviz.ConfigManager.Instance.SpoutSourceName;
        int idx = Array.IndexOf(sources, current);
        // If current name not found, delta +1 starts at 0, delta -1 starts at last.
        idx = idx < 0 ? (delta > 0 ? 0 : sources.Length - 1)
                      : (idx + delta + sources.Length) % sources.Length;

        BlobPreviz.ConfigManager.Instance.SpoutSourceName = sources[idx];
    }
}
