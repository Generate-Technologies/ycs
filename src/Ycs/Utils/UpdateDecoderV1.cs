// ------------------------------------------------------------------------------
// V1 update decoder — reads the Yjs V1 wire format (encodeStateAsUpdate).
// Added locally; not part of upstream yjs/ycs.
// ------------------------------------------------------------------------------

using System;
using System.IO;

namespace Ycs
{
    /// <summary>
    /// V1 delete-set decoder. Unlike <see cref="DSDecoderV2"/>, V1 reads clock
    /// and length directly as varints — no delta encoding.
    /// </summary>
    internal class DSDecoderV1 : IDSDecoder
    {
        private readonly bool _leaveOpen;

        public DSDecoderV1(Stream input, bool leaveOpen = false)
        {
            _leaveOpen = leaveOpen;
            Reader = input;
        }

        public Stream Reader { get; private set; }
        protected bool Disposed { get; private set; }

        /// <summary>No-op in V1 — no delta state to reset.</summary>
        public void ResetDsCurVal()
        {
        }

        public long ReadDsClock()
        {
            return Reader.ReadVarUint();
        }

        public long ReadDsLength()
        {
            return Reader.ReadVarUint();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            System.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing && !_leaveOpen)
                {
                    Reader?.Dispose();
                }

                Reader = null;
                Disposed = true;
            }
        }
    }

    /// <summary>
    /// V1 update decoder. Every field is read inline from the single byte stream —
    /// no sub-channels, no RLE decoders, no feature-flag byte.
    /// Implements the same <see cref="IUpdateDecoder"/> contract as
    /// <see cref="UpdateDecoderV2"/> so it plugs directly into
    /// <see cref="EncodingUtils.ReadStructs"/> and
    /// <see cref="StructStore.ReadAndApplyDeleteSet"/>.
    /// </summary>
    internal sealed class UpdateDecoderV1 : DSDecoderV1, IUpdateDecoder
    {
        public UpdateDecoderV1(Stream input, bool leaveOpen = false)
            : base(input, leaveOpen)
        {
        }

        public ID ReadLeftId()
        {
            return new ID(Reader.ReadVarUint(), Reader.ReadVarUint());
        }

        public ID ReadRightId()
        {
            return new ID(Reader.ReadVarUint(), Reader.ReadVarUint());
        }

        public long ReadClient()
        {
            return Reader.ReadVarUint();
        }

        public byte ReadInfo()
        {
            return Reader._ReadByte();
        }

        public string ReadString()
        {
            return Reader.ReadVarString();
        }

        public bool ReadParentInfo()
        {
            return Reader.ReadVarUint() == 1;
        }

        public uint ReadTypeRef()
        {
            return Reader.ReadVarUint();
        }

        public int ReadLength()
        {
            return (int)Reader.ReadVarUint();
        }

        public object ReadAny()
        {
            return Reader.ReadAny();
        }

        public byte[] ReadBuffer()
        {
            return Reader.ReadVarUint8Array();
        }

        public string ReadKey()
        {
            return Reader.ReadVarString();
        }

        public object ReadJson()
        {
            var jsonString = Reader.ReadVarString();
            return Newtonsoft.Json.JsonConvert.DeserializeObject(jsonString);
        }
    }
}
