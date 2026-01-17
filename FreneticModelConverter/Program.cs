//
// This file is part of the Frenetic Converter Engine, created by Frenetic LLC.
// This code is Copyright (C) Frenetic LLC under the terms of a strict license.
// See README.md or LICENSE.txt in the FreneticModelConverter source root for the contents of the license.
// If neither of these are available, assume that neither you nor anyone other than the copyright holder
// hold any right or permission to use this software until such time as the official license is identified.
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using System.IO.Compression;
using Assimp;
using System.Security.Cryptography.X509Certificates;

namespace FreneticModelConverter
{
    /// <summary>
    /// Entry point and primary class.
    /// </summary>
    class Program
    {
        /// <summary>
        /// The name of the executable file (useful for error message output).
        /// </summary>
        public static readonly string EXENAME = Path.GetFileName(Assembly.GetExecutingAssembly().Location);

        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="args">Parameters passed to execution.</param>
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine($"{EXENAME} <filename> ['pretrans']['texture']");
                Console.WriteLine($"For example: {EXENAME} modelname.dae");
                Console.WriteLine($"For example (2): {EXENAME} myfile.dae texture");
                Console.WriteLine($"For example (3): {EXENAME} somepath/somemodel.dae pretranstexture");
                return;
            }
            string filename = args[0];
            if (!File.Exists(filename))
            {
                Console.WriteLine("Invalid filename (does not exist).");
                Console.WriteLine($"{EXENAME} <filename>");
                return;
            }
            AssimpContext context = new AssimpContext();
            PreTransformNode = args.Length > 1 && args[1].ToLower().Contains("pretrans");
            UseModelTexture = args.Length > 1 && args[1].ToLower().Contains("texture");
            Console.WriteLine($"Pre-transform model to animation node = {PreTransformNode}");
            Console.WriteLine($"Use file texture names = {UseModelTexture}");
            Scene filedata = context.ImportFile(filename, PostProcessSteps.Triangulate | PostProcessSteps.FlipWindingOrder);
            int dot = filename.LastIndexOf('.');
            string cleanName = dot == 1 ? filename : filename.Substring(0, dot);
            if (File.Exists($"{cleanName}.fmd"))
            {
                File.Delete($"{cleanName}.fmd");
            }
            FileStream fileOutputStream = File.OpenWrite($"{cleanName}.fmd");
            File.WriteAllText($"{cleanName}.fmi", ExportModelData(Path.GetFileNameWithoutExtension(filename), filedata, fileOutputStream));
            fileOutputStream.Flush();
            fileOutputStream.Close();
        }

        /// <summary>
        /// Whether to pre-transform the model to its animation node data.
        /// </summary>
        public static bool PreTransformNode = false;

        /// <summary>
        /// Whether to assume the file has proper texture file paths pre-included
        /// (if not, the .fmi file will just show texture path as "unknown").
        /// </summary>
        public static bool UseModelTexture = false;

        /// <summary>
        /// Exports the given model to the given output stream.
        /// </summary>
        /// <param name="filename">Name of the model.</param>
        /// <param name="scene">The model to output.</param>
        /// <param name="baseoutstream">The stream to output to.</param>
        /// <returns>The texture output data.</returns>
        static string ExportModelData(string filename, Scene scene, Stream baseoutstream)
        {
            float minX = 0, minY = 0, minZ = 0, maxX = 0, maxY = 0, maxZ = 0;
            baseoutstream.WriteByte((byte)'F');
            baseoutstream.WriteByte((byte)'M');
            baseoutstream.WriteByte((byte)'D');
            baseoutstream.WriteByte((byte)'0');
            baseoutstream.WriteByte((byte)'0');
            baseoutstream.WriteByte((byte)'1');
            MemoryStream outputStreamInternal = new MemoryStream();
            StreamWrapper outstream = new StreamWrapper(outputStreamInternal);
            outstream.WriteMatrix4x4(scene.RootNode.Transform);
            outstream.WriteInt(scene.MeshCount);
            Console.WriteLine($"Writing {scene.MeshCount} meshes...");
            StringBuilder textureFileBuilder = new StringBuilder();
            textureFileBuilder.Append($"model={filename}\n");
            for (int meshId = 0; meshId < scene.MeshCount; meshId++)
            {
                Mesh mesh = scene.Meshes[meshId];
                Console.WriteLine($"Writing mesh: {mesh.Name}");
                string nodeName = mesh.Name.ToLower().Replace('#', '_').Replace('.', '_');
                if (PreTransformNode && GetNode(scene.RootNode, nodeName) is null)
                {
                    Console.WriteLine($"NO NODE FOR: {nodeName}");
                    continue;
                }
                Matrix4x4 transformMatrix = PreTransformNode ? GetNode(scene.RootNode, nodeName).Transform * scene.RootNode.Transform : Matrix4x4.Identity;
                if (!(mesh.Name.StartsWith("marker_") || mesh.Name.StartsWith("collisionconvex_") || mesh.Name.StartsWith("collisioncomplex_")))
                {
                    if (UseModelTexture)
                    {
                        Material mater = scene.Materials[mesh.MaterialIndex];
                        if (mater.HasTextureDiffuse)
                        {
                            textureFileBuilder.Append($"{mesh.Name}={mater.TextureDiffuse.FilePath}\n");
                        }
                        if (mater.HasTextureSpecular)
                        {
                            textureFileBuilder.Append($"{mesh.Name}:::specular={scene.Materials[mesh.MaterialIndex].TextureSpecular.FilePath}\n");
                        }
                        if (mater.HasTextureReflection)
                        {
                            textureFileBuilder.Append($"{mesh.Name}:::reflectivity={scene.Materials[mesh.MaterialIndex].TextureReflection.FilePath}\n");
                        }
                        if (mater.HasTextureNormal)
                        {
                            textureFileBuilder.Append($"{mesh.Name}:::normal={scene.Materials[mesh.MaterialIndex].TextureNormal.FilePath}\n");
                        }
                    }
                    else
                    {
                        textureFileBuilder.Append($"{mesh.Name}=UNKNOWN\n");
                    }
                }
                outstream.WriteStringProper(mesh.Name);
                outstream.WriteInt(mesh.VertexCount);
                for (int v = 0; v < mesh.VertexCount; v++)
                {
                    Vector3D vert = transformMatrix * mesh.Vertices[v];
                    outstream.WriteVector3D(vert);
                    minX = Math.Min(minX, vert.X);
                    minY = Math.Min(minY, vert.Y);
                    minZ = Math.Min(minZ, vert.Z);
                    maxX = Math.Max(maxX, vert.X);
                    maxY = Math.Max(maxY, vert.Y);
                    maxZ = Math.Max(maxZ, vert.Z);
                }
                outstream.WriteInt(mesh.FaceCount);
                for (int f = 0; f < mesh.FaceCount; f++)
                {
                    Face face = mesh.Faces[f];
                    outstream.WriteInt(face.Indices[0]);
                    outstream.WriteInt(face.Indices[face.IndexCount > 1 ? 1 : 0]);
                    outstream.WriteInt(face.Indices[face.IndexCount > 2 ? 2 : 0]);
                }
                outstream.WriteInt(mesh.TextureCoordinateChannels[0].Count);
                for (int t = 0; t < mesh.TextureCoordinateChannels[0].Count; t++)
                {
                    outstream.WriteFloat(mesh.TextureCoordinateChannels[0][t].X);
                    outstream.WriteFloat(mesh.TextureCoordinateChannels[0][t].Y);
                }
                outstream.WriteInt(mesh.Normals.Count);
                Matrix4x4 normalMatrixRaw = transformMatrix;
                normalMatrixRaw.Inverse();
                normalMatrixRaw.Transpose();
                Matrix3x3 normalMatrix3 = new Matrix3x3(normalMatrixRaw);
                for (int n = 0; n < mesh.Normals.Count; n++)
                {
                    outstream.WriteVector3D(normalMatrix3 * mesh.Normals[n]);
                }
                outstream.WriteInt(mesh.BoneCount);
                for (int b = 0; b < mesh.BoneCount; b++)
                {
                    Bone bone = mesh.Bones[b];
                    outstream.WriteStringProper(bone.Name);
                    outstream.WriteInt(bone.VertexWeightCount);
                    for (int v = 0; v < bone.VertexWeightCount; v++)
                    {
                        outstream.WriteInt(bone.VertexWeights[v].VertexID);
                        outstream.WriteFloat(bone.VertexWeights[v].Weight);
                    }
                    outstream.WriteMatrix4x4(bone.OffsetMatrix);
                }
            }
            Console.WriteLine($"Model bounds: Min({minX}, {minY}, {minZ}) Max({maxX}, {maxY}, {maxZ}), size is ({maxX - minX}, {maxY - minY}, {maxZ - minZ})");
            OutputNode(scene.RootNode, outstream);
            byte[] outputBytesRaw = outputStreamInternal.ToArray();
            outputBytesRaw = GZip(outputBytesRaw);
            baseoutstream.Write(outputBytesRaw, 0, outputBytesRaw.Length);
            return textureFileBuilder.ToString();
        }

        /// <summary>
        /// Gets the animation node by name from a node tree.
        /// </summary>
        /// <param name="root">The root of the node tree.</param>
        /// <param name="namelow">The (pre-lowercased) name to look for.</param>
        /// <returns>The node named, or null.</returns>
        static Node GetNode(Node root, string namelow)
        {
            if (root.Name.ToLower() == namelow)
            {
                return root;
            }
            foreach (Node child in root.Children)
            {
                Node res = GetNode(child, namelow);
                if (res != null)
                {
                    return res;
                }
            }
            return null;
        }

        /// <summary>
        /// Outputs an animation node to the given output stream.
        /// </summary>
        /// <param name="node">The node to output.</param>
        /// <param name="outstream">The stream to output to.</param>
        static void OutputNode(Node node, StreamWrapper outstream)
        {
            outstream.WriteStringProper(node.Name);
            Console.WriteLine($"Output node: {node.Name}");
            outstream.WriteMatrix4x4(node.Transform);
            outstream.WriteInt(node.ChildCount);
            for (int i = 0; i < node.ChildCount; i++)
            {
                OutputNode(node.Children[i], outstream);
            }
        }

        /// <summary>
        /// Compresses a data stream using the GZip algorithm.
        /// </summary>
        /// <param name="input">The data to compress.</param>
        /// <returns>The compressed data.</returns>
        public static byte[] GZip(byte[] input)
        {
            MemoryStream memstream = new MemoryStream();
            GZipStream GZStream = new GZipStream(memstream, CompressionMode.Compress);
            GZStream.Write(input, 0, input.Length);
            GZStream.Close();
            byte[] finaldata = memstream.ToArray();
            memstream.Close();
            return finaldata;
        }
    }
}
