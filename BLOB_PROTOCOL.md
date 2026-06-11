# Blob V1.1 — OSC Protocol Reference

## Transport

Messages are sent as **OSC bundles**. When multiple blobs are active simultaneously, all messages for that frame are packed into a single bundle.

---

## Message Types

There are three message types covering the full lifecycle of a tracked object.

| Address | Purpose |
|---|---|
| `/blob/join` | A new blob has appeared |
| `/blob/move` | A previously tracked blob has moved |
| `/blob/exit` | A previously tracked blob is no longer tracked |

---

## `/blob/join` and `/blob/move`

Both messages share the same 8 arguments.

**Type tag:** `,siiffffff`

| # | Type | Description |
|---|---|---|
| 1 | `string` | GUID identifying the host tracker. Fixed for the lifetime of the tracking session. |
| 2 | `int` | Purpose unknown. Observed value: always `0`. |
| 3 | `int` | Blob ID. Increments by 1 for each new blob and is never reused. |
| 4 | `float` | Time (seconds) the blob has been continuously tracked. |
| 5 | `float` | Bounding box X min. |
| 6 | `float` | Bounding box Y min. |
| 7 | `float` | Bounding box X max. |
| 8 | `float` | Bounding box Y max. |

> Bounding box values are in a **normalised but unbounded** coordinate space — values outside 0–1 have been observed (e.g. a ymin of `-0.04`) when a blob is partially outside the tracked area. Origin and axis direction to be confirmed against live data.

> **Blob ID start value:** Live captures show IDs of `1`, `2`, etc. It is unclear whether the sequence begins at `0` (with blob 0 having exited before the capture) or at `1`. To be confirmed.

---

## `/blob/exit`

**Type tag:** `,sii`

| # | Type | Description |
|---|---|---|
| 1 | `string` | GUID of the host tracker. |
| 2 | `int` | Purpose unknown. Observed value: always `0`. |
| 3 | `int` | ID of the blob that has exited. |

---

## Notes

- Blob IDs are **integers**, not UUIDs. They index upward from `0` and are **never reused** — IDs increase monotonically across the lifetime of the tracking session, even after a blob exits.
- The tracker GUID (argument 1) is a property of the **tracking host**, not the individual blob.
- The purpose of argument 2 (always `0`) is currently unknown. It may be a flags field, a session counter, or reserved for future use.
- Unlike some protocols, exits are **explicitly signalled** via `/blob/exit` rather than relying on a client-side timeout.
