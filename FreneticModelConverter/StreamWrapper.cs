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
    /// <summary>
    /// Helper for managing data output streams.
    /// </summary>
    public class StreamWrapper
    {
        /// <summary>
        /// A reference UTF8 encoding object.
        /// </summary>
        public static readonly Encoding UTF8 = new UTF8Encoding(false);

        /// <summary>
        /// The underlying C# <see cref="Stream"/> object.
        /// </summary>
        public Stream BaseStream;

        /// <summary>
        /// Constructs the <see cref="StreamWrapper"/> instance from any existing output <see cref="Stream"/> object.
        /// </summary>
        public StreamWrapper(Stream stream)
        {
            BaseStream = stream;
        }

        /// <summary>
        /// Writes a 32-bit <see cref="int"/> to the output stream.
        /// </summary>
        /// <param name="i">The integer value.</param>
        public void WriteInt(int i)
        {
            byte[] bits = BitConverter.GetBytes(i);
            BaseStream.Write(bits, 0, bits.Length);
        }

        /// <summary>
        /// Writes a 32-bit <see cref="float"/> to the output stream.
        /// </summary>
        /// <param name="f">The floating-point value.</param>
        public void WriteFloat(float f)
        {
            byte[] bits = BitConverter.GetBytes(f);
            BaseStream.Write(bits, 0, bits.Length);
        }

        /// <summary>
        /// Writes a <see cref="Vector3D"/> to the output stream.
        /// </summary>
        /// <param name="vector">The 3D vector value.</param>
        public void WriteVector3D(Vector3D vector)
        {
            WriteFloat(vector.X);
            WriteFloat(vector.Y);
            WriteFloat(vector.Z);
        }

        /// <summary>
        /// Writes a <see cref="Matrix4x4"/> to the output stream.
        /// </summary>
        /// <param name="matrix">The 4x4 matrix value.</param>
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

        /// <summary>
        /// Writes a <see cref="string"/> to the output stream as UTF-8. Prefixes it with a 32-bit integer indicating the length.
        /// </summary>
        /// <param name="input">The string to write.</param>
        public void WriteStringProper(string input)
        {
            byte[] inputBytes = UTF8.GetBytes(input);
            WriteInt(inputBytes.Length);
            BaseStream.Write(inputBytes, 0, inputBytes.Length);
        }
    }
}
