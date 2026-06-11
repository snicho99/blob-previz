# BlobPreviz — Camera OSC Protocol

Messages emitted by the simulator describing the virtual camera and its output streams. These are **simulator-specific** — they are not part of the Blob V1.1 tracker protocol and will not be present when connecting to real hardware.

All messages are sent as OSC bundles on the same IP/port as the blob tracker messages.

---

## Message Types

| Address | Purpose |
|---|---|
| `/camera/depth/config` | Depth texture stream parameters — sent on start, on change, and as a 1 Hz heartbeat |

---

## `/camera/depth/config`

Advertises the depth texture stream and the decode parameters receivers need to interpret it. Sent immediately on start, immediately on any setting change, and as a **1 Hz heartbeat** so late-joining receivers always have current values.

**Type tag:** `,sffff`

| # | Type | Description |
|---|---|---|
| 1 | `string` | Spout source name of the depth texture output stream. |
| 2 | `float` | Depth range min (metres) — the real-world distance that maps to R=0. |
| 3 | `float` | Depth range max (metres) — the real-world distance that maps to R=255. |
| 4 | `float` | Camera near clip plane (metres). |
| 5 | `float` | Camera far clip plane (metres). |

---

## Depth texture encoding

The depth texture is a standard RGBA frame shared via Spout:

| Channel | Encoding |
|---|---|
| **R** | Normalised depth: `R/255 * (rangeMax - rangeMin) + rangeMin` → metres. Lossy (256 levels) but immediately usable for visual processing. |
| **G** | High byte of full 16-bit depth in mm: `floor(depth_mm / 256)` |
| **B** | Low byte of full 16-bit depth in mm: `depth_mm % 256` |
| **A** | Validity mask: `1.0` where depth is valid, `0.0` for pixels with no measurement (outside capture volume or beyond far clip). |

Reconstruct full precision depth: `depth_mm = G * 256 + B`, then `depth_m = depth_mm / 1000.0`.

Range-map decode for R channel: `depth_m = (R / 255.0) * (rangeMax - rangeMin) + rangeMin`.
