using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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

			// List of indicies into obj_v, obj_vt, and obj_vn describing a vertex in a face
			List<IVec3> obj_f = new List<IVec3>();

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
							obj_f.Add(new IVec3(int.Parse(components[0]), int.Parse(components[1]), int.Parse(components[2])));
                        }
                        break;
                }
            }

			// Create a new object from the data
            WavefrontObject newObject = new WavefrontObject();
			// allocate enough space for all verticies, although we may shrink this array later to match how much space we're actually using after removing duplicates
            newObject.VertexData = new byte[obj_f.Count * WavefrontObject.VERTEX_SIZE];
			newObject.Indices = new int[obj_f.Count];

			// map of IVec3 to indicies - once a vertex is written to newObject.VertexData its index is added to this map, if another vertex with the same data appears it will use the same index
			Dictionary<IVec3, int> writtenIndices = new Dictionary<IVec3, int>();
			int vertexIndex = 0;
			for (int i = 0; i < obj_f.Count; i++)
			{
				IVec3 vertexIndicies = obj_f[i];
				int index;

				// if vertex is not written, write it
				if (!writtenIndices.ContainsKey(vertexIndicies))
				{
					// write vertex info to newObject.VertexData
					Vec3 vertPos = obj_v[vertexIndicies[0] - 1];
					Vec2 vertTex = obj_vt[vertexIndicies[1] - 1];
					Vec3 vertNorm = obj_vn[vertexIndicies[2] - 1];

					vertPos.Bytes.CopyTo(newObject.VertexData, (vertexIndex * WavefrontObject.VERTEX_SIZE) + WavefrontObject.VERTEX_POSITION_OFFSET);
					vertTex.Bytes.CopyTo(newObject.VertexData, (vertexIndex * WavefrontObject.VERTEX_SIZE) + WavefrontObject.VERTEX_TEXTURE_COORDINATE_OFFSET);
					vertNorm.Bytes.CopyTo(newObject.VertexData, (vertexIndex * WavefrontObject.VERTEX_SIZE) + WavefrontObject.VERTEX_NORMAL_OFFSET);

					// write index to wirttenIndices
					writtenIndices[vertexIndicies] = vertexIndex;
					index = vertexIndex;
					vertexIndex++;
				}
				else
				{
					index = writtenIndices[vertexIndicies];
				}

				// write index to indicies
				newObject.Indices[i] = index;
			}

			// resize newObject.VertexData to actual ammount of data used
			Array.Resize(ref newObject.VertexData, vertexIndex * WavefrontObject.VERTEX_SIZE);

			Console.WriteLine("Loaded object with {0} verticies ({4} unique) made of {1} positions, {2} texture coordinates, and {3} normals", obj_f.Count, obj_v.Count, obj_vt.Count, obj_vn.Count, vertexIndex);

			return newObject;
        }
    }
}
