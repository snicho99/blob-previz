using System;
using System.Net;
using System.Net.Sockets;

namespace BlobPreviz
{
    /// <summary>
    /// Minimal OSC bundle encoder targeting the Blob V1.1 protocol.
    /// Builds one bundle per frame in a pre-allocated buffer and sends it as a
    /// single UDP datagram. Not thread-safe — call from the main thread only.
    ///
    /// OSC bundle wire format:
    ///   "#bundle\0"          8 bytes  (string, already 4-byte aligned)
    ///   timetag              8 bytes  (uint64 big-endian; 1 = immediate)
    ///   [ int32 msgSize      4 bytes
    ///     message content    msgSize bytes ] × N
    ///
    /// OSC message wire format:
    ///   address string       N bytes, null-terminated, padded to 4-byte boundary
    ///   type tag string      N bytes, null-terminated, padded to 4-byte boundary
    ///   arguments            4 bytes each (int32 / float32 big-endian; strings padded)
    /// </summary>
    internal sealed class OscBundleClient : IDisposable
    {
        // 8 KB is plenty for any realistic blob count.
        readonly byte[] _buf = new byte[8192];
        int _pos;
        Socket _socket;

        public OscBundleClient(string host, int port)
            => Connect(host, port);

        public void Reconnect(string host, int port)
        {
            _socket?.Close();
            Connect(host, port);
        }

        void Connect(string host, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.Connect(new IPEndPoint(IPAddress.Parse(host), port));
        }

        // ── Bundle lifecycle ────────────────────────────────────────────────

        /// <summary>Call once at the start of each frame before adding messages.</summary>
        public void BeginBundle()
        {
            _pos = 0;
            AppendString("#bundle");      // 8 bytes (7 chars + null, already aligned)
            AppendInt32(0);               // timetag high word (immediate = 0x00000000_00000001)
            AppendInt32(1);               // timetag low word
        }

        /// <summary>True if at least one message has been added since BeginBundle.</summary>
        public bool HasMessages => _pos > 16;

        /// <summary>Send the bundle. No-op if no messages were added.</summary>
        public void Send()
        {
            if (HasMessages)
                _socket.Send(_buf, _pos, SocketFlags.None);
        }

        // ── Message builders ────────────────────────────────────────────────

        /// <summary>
        /// Adds a /blob/join or /blob/move message.
        /// Type tag: ,siiffffff
        /// </summary>
        public void AddJoinMove(string address, string trackerGuid, int blobId,
            float trackedTime, float xMin, float yMin, float xMax, float yMax)
        {
            int sizeSlot = ReserveSizeSlot();

            AppendString(address);
            AppendString(",siifffff");
            AppendString(trackerGuid);
            AppendInt32(0);           // arg 2: unknown, always 0
            AppendInt32(blobId);
            AppendFloat(trackedTime);
            AppendFloat(xMin);
            AppendFloat(yMin);
            AppendFloat(xMax);
            AppendFloat(yMax);

            WriteSize(sizeSlot);
        }

        /// <summary>
        /// Adds a /blob/exit message.
        /// Type tag: ,sii
        /// </summary>
        public void AddExit(string trackerGuid, int blobId)
        {
            int sizeSlot = ReserveSizeSlot();

            AppendString("/blob/exit");
            AppendString(",sii");
            AppendString(trackerGuid);
            AppendInt32(0);           // arg 2: unknown, always 0
            AppendInt32(blobId);

            WriteSize(sizeSlot);
        }

        // ── Encoding helpers ────────────────────────────────────────────────

        // Reserves 4 bytes for a message-size field and returns its position.
        int ReserveSizeSlot()
        {
            int slot = _pos;
            _pos += 4;
            return slot;
        }

        // Writes the number of bytes written since the size slot into that slot.
        void WriteSize(int slot)
        {
            int size = _pos - slot - 4;
            _buf[slot    ] = (byte)(size >> 24);
            _buf[slot + 1] = (byte)(size >> 16);
            _buf[slot + 2] = (byte)(size >>  8);
            _buf[slot + 3] = (byte) size;
        }

        void AppendString(string s)
        {
            int len = s.Length;
            for (int i = 0; i < len; i++)
                _buf[_pos++] = (byte)s[i];
            int padded = AlignTo4(len + 1);   // +1 for null terminator
            for (int i = len; i < padded; i++)
                _buf[_pos++] = 0;
        }

        void AppendInt32(int v)
        {
            _buf[_pos++] = (byte)(v >> 24);
            _buf[_pos++] = (byte)(v >> 16);
            _buf[_pos++] = (byte)(v >>  8);
            _buf[_pos++] = (byte) v;
        }

        void AppendFloat(float v)
        {
            // Reinterpret float bits as int, then write big-endian.
            AppendInt32(BitConverter.SingleToInt32Bits(v));
        }

        static int AlignTo4(int n) => (n + 3) & ~3;

        // ── IDisposable ──────────────────────────────────────────────────────

        public void Dispose()
        {
            _socket?.Close();
            _socket = null;
        }
    }
}
