using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Text;

namespace Zoltu.IO
{
    public class BigEndianBinaryWriter : BinaryWriter
    {
        public BigEndianBinaryWriter(Stream stream) : base(stream, Encoding.UTF8)
        {
            Contract.Requires(stream != null);
        }
        public BigEndianBinaryWriter(Stream stream, bool leaveOpen) : base(stream, Encoding.UTF8, leaveOpen)
        {
            Contract.Requires(stream != null);
        }
        public override void Write(decimal value)
        {
            int[] ints = decimal.GetBits(value);
            Contract.Assume(ints != null);
            Contract.Assume(ints.Length == 4);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(ints);
            for (int i = 0; i < 4; ++i)
            {
                byte[] bytes = BitConverter.GetBytes(ints[i]);
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(bytes);
                Write(bytes);
            }
        }
        public override void Write(float value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }
        public override void Write(double value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }
        public override void Write(short value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }
        public override void Write(int value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }
        public override void Write(long value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }
        public override void Write(ushort value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }
        public override void Write(uint value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }
        public override void Write(ulong value)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }
        private void WriteBigEndian(byte[] bytes)
        {
            Contract.Requires(bytes != null);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            Write(bytes);
        }
    }
}