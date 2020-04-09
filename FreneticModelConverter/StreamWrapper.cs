//
// This file is part of the Frenetic Converter Engine, created by Frenetic LLC.
// This code is Copyright (C) Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the FreneticModelConverter source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Assimp;

namespace FreneticModelConverter
{
    public class StreamWrapper
    {
        public static readonly Encoding UTF8 = new UTF8Encoding(false);
        public Stream BaseStream;

        public StreamWrapper(Stream stream)
        {
            BaseStream = stream;
        }

        public void WriteInt(int i)
        {
            byte[] bits = BitConverter.GetBytes(i);
            BaseStream.Write(bits, 0, bits.Length);
        }

        public void WriteFloat(float f)
        {
            byte[] bits = BitConverter.GetBytes(f);
            BaseStream.Write(bits, 0, bits.Length);
        }

        public void WriteVector3D(Vector3D vector)
        {
            WriteFloat(vector.X);
            WriteFloat(vector.Y);
            WriteFloat(vector.Z);
        }

        public void WriteMatrix4x4(Matrix4x4 matrix)
        {
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    WriteFloat(matrix[i + 1, j + 1]);
                }
            }
        }

        public void WriteStringProper(string input)
        {
            byte[] inputBytes = UTF8.GetBytes(input);
            WriteInt(inputBytes.Length);
            BaseStream.Write(inputBytes, 0, inputBytes.Length);
        }
    }
}
