using System;
using System.Collections.Generic;
using System.Linq;

namespace NameTable
{
    public struct Bytes2Longs
    {
     
        public readonly ulong[] Longs;
        public readonly uint? Int;
        public readonly ushort? Short;
        public readonly byte? Byte;

        public Bytes2Longs(byte[] bytes)
        {
            if (bytes.Length > 7)
            {
                Longs=new ulong[bytes.Length/8];
                for (int i = 0; i < Longs.Length; i++)
                    Longs[i]=BitConverter.ToUInt64(bytes, i*8);
            }
            else Longs = null;     
            int sizeReaded = Longs == null ? 0 : Longs.Length*8;
            int lastLength = bytes.Length%8;
            if (lastLength > 3)
            {
                Int = BitConverter.ToUInt32(bytes, sizeReaded);
                sizeReaded += 4;
                lastLength -= 4;
            }
            else Int = null;
            if (lastLength > 1)
            {
                Short = BitConverter.ToUInt16(bytes, sizeReaded);
                {
                    sizeReaded += 2;
                    lastLength -= 2;
                }
            }
            else Short = null;
            if (lastLength > 0)
                Byte = bytes[sizeReaded];
            else Byte = null;
        }

        public byte[] ToBytes()
        {
            byte[] bytes=new byte[(Longs==null ?0 : Longs.Length*8)+ (Int==null ?0:4)+(Short==null ?0:2)+(Byte==null ?0:1)];
            int bytesWrited = 0;
            if(Longs!=null)
            for (int i = 0; i < Longs.Length; i++)
            {
                byte[] bytes1 = BitConverter.GetBytes(Longs[i]);
                bytes1.CopyTo(bytes,bytesWrited);
                bytesWrited += 8;
            }
            if (Int != null)
            {
                byte[] bytes1 = BitConverter.GetBytes(Int.Value);
                bytes1.CopyTo(bytes, bytesWrited);
                bytesWrited += 4;
            }
            if (Short != null)
            {
                byte[] bytes1 = BitConverter.GetBytes(Short.Value);
                bytes1.CopyTo(bytes, bytesWrited);
                bytesWrited += 2;
            }
            if (Byte != null)
                bytes[bytesWrited] = Byte.Value;
            return bytes;
        }

        private bool Equals(Bytes2Longs other)
        {
            bool rest = Int == other.Int && Short == other.Short && Byte == other.Byte;
            if(!rest) return false;
            if (Longs == null && other.Longs == null)
                return true;
            if(Longs.Length!=other.Longs.Length)
                return false;

            ulong[] longs = Longs;
            return Enumerable.Range(0, longs.Length).All(i => longs[i] == other.Longs[i]);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                ulong longs=Longs == null ? 1 : Longs.Length==1 ? Longs.First() : Longs.Skip(1).Aggregate(Longs.First(),(l, l1) => l ^ l1 );
                int hashCode = (int)((ulong)((2 ^ (Int ?? 0))*(3 ^ (Short ?? 0))*(5 ^ (Byte ?? 0))*7) ^longs);
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Bytes2Longs && Equals(((Bytes2Longs) obj));
        }
    }
}