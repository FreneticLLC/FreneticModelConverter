FreneticModelConverter
======================

The Frenetic Model Converter: A system that converts any given model format to `.fmd` files (and produces reference `.fmi` files). This repository also contains the specification docs for the file format (in this readme, below).

### This Codebase

This codebase is an executable file that can be operated via command line to convert files (loaded using [AssImp](https://github.com/assimp/assimp)) to `.fmd`.

The program should be called like `FreneticModelConverter.exe <filename> ['pretrans']['texture']`
- For example: `FreneticModelConverter.exe modelname.dae`
- For example (2): `FreneticModelConverter.exe myfile.dae texture`
- For example (3): `FreneticModelConverter.exe somepath/somemodel.dae pretransmodel`

The "pretrans" option means to pre-transform the model to match the start of the animation node. This is needed with some animated models if intended to be used without applying the animation separately.

The "texture" option means to assume the texture paths in the source file are proper, and therefore include them in the `.fmi` file output (otherwise, the `.fmi` file will simply include `unknown` in each texture path, to be filled properly later).

### Status

This project is fairly stable, but has some trouble with AssImp reading files weirdly.

## The Format

This format exists primarily for the sake of having a model format that [the Frenetic Game Engine](https://github.com/FreneticLLC/FreneticGameEngine) can rely on.

The format has the following properties:
- **Unambigious.** This means there are no variants or oddities allowed. This is a problem with many common file formats and makes it difficulty to reliably load model files produced by different sources. As an example of the lack of ambiguity: all faces are triangles, no quads or anything else.
- **Space-efficient.** This means the data is stored as raw small data (and then automatically compressed as well). This is a problem with many FOSS file formats that use very long ways of writing simple data (eg `.obj` and `.dae` are based on text, which is the opposite of space-efficient).
- **Stores all model data.** This means that all data required for a 3D model is included - vertex positions, face vertex IDs, normal vectors, texture coordinates, bones and their vertex weights.
- **Does not store non-model data.** This means that data related to 3D projects, but not specific to a 3D model, is excluded - cameras, animations, etc. are left out (animations can be tracked separately).
- **Simple.** The format is very straight forward. This reference implementation codebase can output a model file in only a few hundred lines of code. The reference reader in FGE similarly needs a very small amount of code to read the files.

### Format Specification

All data is little-endian, and stored without any padding.

The following formats are used:
- Integer: A 32-bit integer. Encoded as 4 bytes, little-endian.
- Float: A 32-bit floating point number. Encoded as 4 bytes, little-endian, per IEEE-754.
- Vector3: A 3-value floating-point vector. Stored as X, then Y, then Z, each as a float, for a total of 12 bytes.
- Matrix4: A 4 by 4 matrix of floats. Matrix uses right-handed convention, and orders data as 4 rows, each containing 4 floats. This uses a total of 64 bytes of data.
    - To be clear, the data is ordered as (Row, Column): (1,1), (1,2), (1,3), (1,4), (2,1), (2,2), (2,3), (2,4), (3,1), (3,2), (3,3), (3,4), (4,1), (4,2), (4,3), (4,4)
- String: Every string is prefixed by an integer (4 bytes) that identifies the number of bytes the string occupies (NOT number of characters, due to some characters needing multiple bytes). The string then follows, encoded in UTF-8.

The file format is as follows:
- 6 bytes: "FMD001" as direct ASCII encoding.
    - Note: This should be treated as an identifier constant. "FMD" labels the format and "001" is the version. Reader implementations are allowed to either require the "001" version or ignore it to their own choice (it is advised to warn users if an invalid version is loaded, though). Writer implementations should only output "001", until the official format is modified (and has a new version for the modified format).
- Matrix4: The model root transformation node. In many cases, will be an identity matrix.
- Integer: The number of meshes within the model (will often be just one).
- List of meshes, each encoded directly one after the other in line without padding. A mesh is defined as follows:
    - String: Name of the mesh.
    - Integer: Number of vertices.
    - List of vertices, each vertex is a Vector3, each encoded directly one after the other in line without padding.
    - Integer: Number of faces.
        - Note: this will in many models be exactly 1/3rd the number of vertices, but not always.
    - List of faces, each face is a set of 3 integers, indicating the 3 indices in the vertex list that are the corners of the triangle face, each encoded directly one after the other in line without padding.
    - Integer: Number of texture coordinates. Should match the number of vertices in most cases (reader implementations may error if this is not the case).
    - List of texture coordinates, each texture-coordinate is a set of 2 floats (X, then Y), each encoded directly one after the other in line without padding.
    - Integer: Number of normals. Should match the number of vertices in most cases (reader implementations may error if this is not the case).
    - List of normals, each normal is a Vector3, each encoded directly one after the other in line without padding.
    - Integer: Number of bones.
    - List of bones, each encoded directly one after the other in line without padding. A bone is defined as follows:
        - String: Name of the bone. Usually corresponds to a node that is named the same.
        - Integer: Number of vertex weights to encode.
        - List of vertex weights, each encoded directly one after the other in line without padding. A vertex weight is defined as follows:
            - Integer: The vertex ID the weight applies to.
            - Float: The weight of the bone on that vertex. All weight values from all bones on any one given vertex ID should add up to exactly 1.
        - Matrix4: The bone's offset from the bone it's attached to.
- Tree of nodes, starting with the root node, each encoded directly one after the other in line without padding. A node is defined as follows:
    - String: name of the node. Name usually corresponds to a mesh or bone by the same name.
    - Matrix4: The transformation of the node (relative to its parent node).
    - Integer: The number of child nodes. Root and branch nodes often have one or more child nodes. Leaf nodes have zero for this value, and no data included for the following this.
    - List of child-nodes, each encoded directly one after the other in line without padding. Each contained node is defined the same as the definition of node that contains this line. That is, nodes recursively contain more nodes (a tree structure).

### Licensing pre-note:

This is an open source project, provided entirely freely, for everyone to use and contribute to.

If you make any changes that could benefit the community as a whole, please contribute upstream.

### The short of the license is:

You can do basically whatever you want, except you may not hold any developer liable for what you do with the software.

### The long version of the license follows:

The MIT License (MIT)

Copyright (c) 2015-2020 Frenetic LLC, All Rights Reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
