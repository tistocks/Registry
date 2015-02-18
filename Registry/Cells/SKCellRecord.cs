﻿using System;
using System.Linq;
using System.Text;
using NFluent;
using Registry.Other;

// namespaces...

namespace Registry.Cells
{
    // public classes...
    public class SKCellRecord : ICellTemplate, IRecordBase
    {
        // private fields...
        private readonly int _size;
        // protected internal constructors...
        /// <summary>
        ///     Initializes a new instance of the <see cref="SKCellRecord" /> class.
        ///     <remarks>Represents a Key Security Record</remarks>
        /// </summary>
        protected internal SKCellRecord(byte[] rawBytes, long relativeOffset)
        {
            RelativeOffset = relativeOffset;
            RawBytes = rawBytes;

            _size = BitConverter.ToInt32(rawBytes, 0);

            Check.That(Signature).IsEqualTo("sk");


            //this has to be a multiple of 8, so check for it
            var paddingOffset = 0x18 + DescriptorLength;
            var paddingLength = rawBytes.Length - paddingOffset;

            if (paddingLength > 0)
            {
                var padding = rawBytes.Skip((int) paddingOffset).Take((int) paddingLength).ToArray();

                Check.That(Array.TrueForAll(padding, a => a == 0));
            }

            //Check that we have accounted for all bytes in this record. this ensures nothing is hidden in this record or there arent additional data structures we havent processed in the record.
            Check.That(0x18 + (int) DescriptorLength + paddingLength).IsEqualTo(rawBytes.Length);
        }

        // public properties...

        /// <summary>
        ///     A relative offset to the previous SK record
        /// </summary>
        public uint BLink
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x0c); }
        }

        public uint DescriptorLength
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x14); }
        }

        /// <summary>
        ///     A relative offset to the next SK record
        /// </summary>
        public uint FLink
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x08); }
        }

        /// <summary>
        ///     A count of how many keys this security record applies to
        /// </summary>
        public uint ReferenceCount
        {
            get { return BitConverter.ToUInt32(RawBytes, 0x10); }
        }

        public ushort Reserved
        {
            get { return BitConverter.ToUInt16(RawBytes, 0x6); }
        }

        /// <summary>
        ///     The security descriptor object for this record
        /// </summary>
        public SKSecurityDescriptor SecurityDescriptor
        {
            get
            {
                var rawDescriptor = RawBytes.Skip(0x18).Take((int) DescriptorLength).ToArray();

                if (rawDescriptor.Length > 0)
                {
                    // i have seen cases where there is no available security descriptor because the sk record doesnt contain the right data
                    return new SKSecurityDescriptor(rawDescriptor);
                }

                return null;
            }
        }

        // public properties...
        public long AbsoluteOffset
        {
            get { return RelativeOffset + 4096; }
            set { }
        }

        public bool IsFree
        {
            get { return _size > 0; }
            set { }
        }

        public bool IsReferenced { get; internal set; }
        public byte[] RawBytes { get;  private set;}
        public long RelativeOffset { get;  private set;}

        public string Signature
        {
            get { return Encoding.ASCII.GetString(RawBytes, 4, 2); }
            set { }
        }

        public int Size
        {
            get { return Math.Abs(_size); }
           private set { }
        }

        // public methods...
        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(string.Format("Size: 0x{0:X}", Math.Abs(_size)));
            sb.AppendLine(string.Format("Relative Offset: 0x{0:X}", RelativeOffset));
            sb.AppendLine(string.Format("Absolute Offset: 0x{0:X}", AbsoluteOffset));
            sb.AppendLine(string.Format("Signature: {0}", Signature));

            sb.AppendLine(string.Format("Is Free: {0}", IsFree));

            sb.AppendLine();
            sb.AppendLine(string.Format("Forward Link: 0x{0:X}", FLink));
            sb.AppendLine(string.Format("Backward Link: 0x{0:X}", BLink));
            sb.AppendLine();

            sb.AppendLine(string.Format("Reference Count: {0:N0}", ReferenceCount));

            sb.AppendLine();
            sb.AppendLine(string.Format("Security descriptor length: 0x{0:X}", DescriptorLength));

            sb.AppendLine();
            sb.AppendLine(string.Format("Security descriptor: {0}", SecurityDescriptor));

            return sb.ToString();
        }
    }
}