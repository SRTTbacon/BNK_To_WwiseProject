using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace Zoltu.IO
{
    public class BigEndianBinaryReader : BinaryReader
    {
        public BigEndianBinaryReader(Stream input) : base(input)
        {
            Contract.Requires(input != null);
        }
        public BigEndianBinaryReader(Stream input, bool leaveOpen) : base(input, Encoding.UTF8, leaveOpen)
        {
            Contract.Requires(input != null);
        }
        public override decimal ReadDecimal()
        {
            byte[] bytes = GetNextBytesNativeEndian(16);
            int[] ints = new int[4];
            ints[0] = (bytes[0] << 0)
                | (bytes[1] << 8)
                | (bytes[2] << 16)
                | (bytes[3] << 24);
            ints[1] = (bytes[4] << 0)
                | (bytes[5] << 8)
                | (bytes[6] << 16)
                | (bytes[7] << 24);
            ints[2] = (bytes[8] << 0)
                | (bytes[9] << 8)
                | (bytes[10] << 16)
                | (bytes[11] << 24);
            ints[3] = (bytes[12] << 0)
                | (bytes[13] << 8)
                | (bytes[14] << 16)
                | (bytes[15] << 24);
            return new decimal(ints);
        }
        public override float ReadSingle()
        {
            return Read(4, BitConverter.ToSingle);
        }
        public override double ReadDouble()
        {
            return Read(8, BitConverter.ToDouble);
        }
        public override short ReadInt16()
        {
            return Read(2, BitConverter.ToInt16);
        }
        public override int ReadInt32()
        {
            return Read(4, BitConverter.ToInt32);
        }
        public override long ReadInt64()
        {
            return Read(8, BitConverter.ToInt64);
        }
        public override ushort ReadUInt16()
        {
            return Read(2, BitConverter.ToUInt16);
        }
        public override uint ReadUInt32()
        {
            return Read(4, BitConverter.ToUInt32);
        }
        public override ulong ReadUInt64()
        {
            return Read(8, BitConverter.ToUInt64);
        }
        private T Read<T>(int size, Func<byte[], int, T> converter) where T : struct
        {
            Contract.Requires(size >= 0);
            Contract.Requires(converter != null);
            byte[] bytes = GetNextBytesNativeEndian(size);
            return converter(bytes, 0);
        }
        private byte[] GetNextBytesNativeEndian(int count)
        {
            Contract.Requires(count >= 0);
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == count);
            byte[] bytes = GetNextBytes(count);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }
        private byte[] GetNextBytes(int count)
        {
            Contract.Requires(count >= 0);
            Contract.Ensures(Contract.Result<byte[]>() != null);
            Contract.Ensures(Contract.Result<byte[]>().Length == count);
            byte[] buffer = new byte[count];
            int bytesRead = BaseStream.Read(buffer, 0, count);
            return bytesRead != count ? throw new EndOfStreamException() : buffer;
        }
    }
}