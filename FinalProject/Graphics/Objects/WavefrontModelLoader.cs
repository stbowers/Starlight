using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FinalProject.Graphics.Math;

namespace FinalProject.Graphics.Objects
{
    public class WavefrontObject
    {
        public const int VERTEX_POSITION_SIZE = 3 * 4;
        public const int VERTEX_POSITION_OFFSET = 0;
        public const int VERTEX_TEXTURE_COORDINATE_SIZE = 2 * 4;
        public const int VERTEX_TEXTURE_COORDINATE_OFFSET = VERTEX_POSITION_OFFSET + VERTEX_POSITION_SIZE;
        public const int VERTEX_NORMAL_SIZE = 3 * 4;
        public const int VERTEX_NORMAL_OFFSET = VERTEX_TEXTURE_COORDINATE_OFFSET + VERTEX_TEXTURE_COORDINATE_SIZE;
        public const int VERTEX_SIZE = VERTEX_POSITION_SIZE + VERTEX_TEXTURE_COORDINATE_SIZE + VERTEX_NORMAL_SIZE;

        public byte[] VertexData;
        public int[] Indices;

        public Vec3 GetVertexPosition(int index)
        {
            float[] posData = new float[3];
            Buffer.BlockCopy(VertexData, (index * VERTEX_SIZE) + VERTEX_POSITION_OFFSET, posData, 0, VERTEX_POSITION_SIZE);
            return new Vec3(posData);
        }

        public Vec2 GetVertexTextureCoordinate(int index)
        {
            float[] texCoordData = new float[2];
            Buffer.BlockCopy(VertexData, (index * VERTEX_SIZE) + VERTEX_TEXTURE_COORDINATE_OFFSET, texCoordData, 0, VERTEX_TEXTURE_COORDINATE_SIZE);
            return new Vec2(texCoordData);
        }

        public Vec3 GetVertexNormal(int index)
        {
            float[] normalData = new float[3];
            Buffer.BlockCopy(VertexData, (index * VERTEX_SIZE) + VERTEX_NORMAL_OFFSET, normalData, 0, VERTEX_NORMAL_SIZE);
            return new Vec3(normalData);
        }
    }

    static class WavefrontModelLoader
    {
        public static WavefrontObject LoadFile(string file)
        {
            StreamReader reader = File.OpenText(file);
            string line;

            // lists of the vectors defined by v, vt, and vn in the obj file
            List<Vec3> obj_v = new List<Vec3>();
            List<Vec2> obj_vt = new List<Vec2>();
            List<Vec3> obj_vn = new List<Vec3>();

            // set of indices into obj_v, obj_vt, and obj_vn which make a unique vertex in the mesh
            List<IVec3> vertexData = new List<IVec3>();
            // list of indices into vertexData which make the faces of the model (every group of 3 make one face from the vertices)
            List<int> vertexIndices = new List<int>();

            // parse each line of the file
            while ((line = reader.ReadLine()) != null)
            {
                string[] words = line.Split(' ');
                switch (words[0])
                {
                    case "v":
                        // vertex: v x y z
                        obj_v.Add(new Vec3(float.Parse(words[1]), float.Parse(words[2]), float.Parse(words[3])));
                        break;
                    case "vt":
                        // texture coordinate: vt x y
                        // flip y component of texture coordinates for Vulkan
                        obj_vt.Add(new Vec2(float.Parse(words[1]), -float.Parse(words[2])));
                        break;
                    case "vn":
                        // normal: vn x y z
                        obj_vn.Add(new Vec3(float.Parse(words[1]), float.Parse(words[2]), float.Parse(words[3])));
                        break;
                    case "f":
                        // face: v/vt/vn v/vt/vn v/vt/vn
                        for (int i = 1; i < 4; i++)
                        {
                            string[] components = words[i].Split('/');
                            IVec3 vertex = new IVec3(int.Parse(components[0]), int.Parse(components[1]), int.Parse(components[2]));
                            Predicate<IVec3> sameVertex = x => x[0] == vertex[0] && x[1] == vertex[1] && x[2] == vertex[2];
                            if (!vertexData.Exists(sameVertex))
                            {
                                // only add the vertex if it doesn't already exist in the list
                                vertexData.Add(vertex);
                            }
                            vertexIndices.Add(vertexData.FindIndex(sameVertex));
                        }
                        break;
                }
            }

            // Create a new object from the data
            WavefrontObject newObject = new WavefrontObject();
            int numVertices = vertexIndices.Count;
            newObject.VertexData = new byte[vertexData.Count * WavefrontObject.VERTEX_SIZE];
            newObject.Indices = vertexIndices.ToArray();
            for (int i = 0; i < vertexData.Count; i++)
            {
                Vec3 vertPos = obj_v[vertexData[i][0] - 1];
                Vec2 vertTex = obj_vt[vertexData[i][1] - 1];
                Vec3 vertNorm = obj_vn[vertexData[i][2] - 1];

                vertPos.Bytes.CopyTo(newObject.VertexData, (i * WavefrontObject.VERTEX_SIZE) + WavefrontObject.VERTEX_POSITION_OFFSET);
                vertTex.Bytes.CopyTo(newObject.VertexData, (i * WavefrontObject.VERTEX_SIZE) + WavefrontObject.VERTEX_TEXTURE_COORDINATE_OFFSET);
                vertNorm.Bytes.CopyTo(newObject.VertexData, (i * WavefrontObject.VERTEX_SIZE) + WavefrontObject.VERTEX_NORMAL_OFFSET);
            }

            return newObject;
        }
    }
}
