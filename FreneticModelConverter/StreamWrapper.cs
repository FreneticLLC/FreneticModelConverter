using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FreneticModelConverter
{
    public class StreamWrapper
    {
        public Stream BaseStream;

        public StreamWrapper(Stream stream)
        {
            BaseStream = stream;
        }

        public void WriteInt(int i)
        {
            byte[] bits = BitConverter.GetBytes(i);
            if (!BitConverter.IsLittleEndian)
            {
                bits = bits.Reverse().ToArray();
            }
            BaseStream.Write(bits, 0, bits.Length);
        }

        public void WriteFloat(float f)
        {
            byte[] bits = BitConverter.GetBytes(f);
            if (!BitConverter.IsLittleEndian)
            {
                bits = bits.Reverse().ToArray();
            }
            BaseStream.Write(bits, 0, bits.Length);
        }
    }
}
