using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace WoWViewer.Parsers
{
    /// <summary>
    /// Parsed 3D geometry data from IOB/WOF files
    /// </summary>
    public class Geometry3D
    {
        public Vector3[] Vertices { get; set; } = Array.Empty<Vector3>();
        public Vector3[] Normals { get; set; } = Array.Empty<Vector3>();
        public ushort[] Indices { get; set; } = Array.Empty<ushort>();
        public int TriangleCount => Indices.Length / 3;
    }

    /// <summary>
    /// Parses decompressed IOB/WOF geometry data into 3D mesh structure
    /// </summary>
    public class GeometryParser
    {
        /// <summary>
        /// Parse decompressed geometry data
        /// </summary>
        /// <param name="decompressedData">Raw decompressed bytes from Huffman decoder</param>
        /// <returns>Parsed 3D geometry</returns>
        public static Geometry3D Parse(byte[] decompressedData)
        {
            if (decompressedData == null || decompressedData.Length < 32)
                throw new ArgumentException("Data too small to contain geometry");

            var geometry = new Geometry3D();

            // Read header - vertex count at offset 0x1E (observed pattern)
            ushort vertexCount = BitConverter.ToUInt16(decompressedData, 0x1E);
            Console.WriteLine($"Header vertex count: {vertexCount}");

            // Vertex data starts around offset 0x20-0x30
            int vertexDataStart = FindVertexDataStart(decompressedData);
            Console.WriteLine($"Vertex data starts at offset: 0x{vertexDataStart:X}");

            // Parse exactly vertexCount vertices (3 floats per vertex = 12 bytes)
            var vertices = new List<Vector3>();
            int offset = vertexDataStart;
            
            for (int i = 0; i < vertexCount && offset + 12 <= decompressedData.Length; i++)
            {
                float x = BitConverter.ToSingle(decompressedData, offset);
                float y = BitConverter.ToSingle(decompressedData, offset + 4);
                float z = BitConverter.ToSingle(decompressedData, offset + 8);

                // Validate coordinates
                if (IsValidCoordinate(x) && IsValidCoordinate(y) && IsValidCoordinate(z))
                {
                    vertices.Add(new Vector3(x, y, z));
                    offset += 12;
                }
                else
                {
                    Console.WriteLine($"Warning: Invalid coordinate at vertex {i}, offset 0x{offset:X}");
                    break;
                }
            }

            geometry.Vertices = vertices.ToArray();
            Console.WriteLine($"Parsed {geometry.Vertices.Length}/{vertexCount} vertices");

            // Parse face indices (after vertex data)
            int faceDataStart = offset;
            Console.WriteLine($"Face data should start at offset: 0x{faceDataStart:X} (file size: 0x{decompressedData.Length:X})");
            
            int remainingBytes = decompressedData.Length - faceDataStart;
            Console.WriteLine($"Remaining bytes for faces: {remainingBytes}");
            
            // Try parsing as uint16 triangle indices
            var indices = new List<ushort>();
            for (int i = faceDataStart; i + 2 <= decompressedData.Length; i += 2)
            {
                ushort index = BitConverter.ToUInt16(decompressedData, i);
                
                // Valid indices should be less than vertex count
                if (index < vertexCount)
                {
                    indices.Add(index);
                }
                else if (index == 0xFFFF)
                {
                    // Potential primitive restart or end marker
                    continue;
                }
                else
                {
                    // Invalid index, might not be face data
                    break;
                }
            }

            if (indices.Count >= 3 && indices.Count % 3 == 0)
            {
                geometry.Indices = indices.ToArray();
                Console.WriteLine($"Parsed {geometry.TriangleCount} triangles ({indices.Count} indices)");
            }
            else
            {
                Console.WriteLine($"Could not parse valid face data ({indices.Count} indices found)");
            }

            return geometry;
        }

        private static int FindVertexDataStart(byte[] data)
        {
            // Look for the first occurrence of valid float triplets
            for (int i = 0; i < Math.Min(128, data.Length - 12); i++)
            {
                float x = BitConverter.ToSingle(data, i);
                float y = BitConverter.ToSingle(data, i + 4);
                float z = BitConverter.ToSingle(data, i + 8);

                if (IsValidCoordinate(x) && IsValidCoordinate(y) && IsValidCoordinate(z))
                {
                    // Check if this is the start of a sequence
                    float x2 = BitConverter.ToSingle(data, i + 12);
                    float y2 = BitConverter.ToSingle(data, i + 16);
                    float z2 = BitConverter.ToSingle(data, i + 20);

                    if (IsValidCoordinate(x2) && IsValidCoordinate(y2) && IsValidCoordinate(z2))
                    {
                        return i;
                    }
                }
            }

            return 0; // Default to start if not found
        }

        private static bool IsValidCoordinate(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) &&
                   Math.Abs(value) < 100.0f;
        }

        /// <summary>
        /// Export geometry to Wavefront OBJ format
        /// </summary>
        public static string ExportToOBJ(Geometry3D geometry, string modelName)
        {
            var obj = new System.Text.StringBuilder();
            obj.AppendLine($"# {modelName}");
            obj.AppendLine($"# Vertices: {geometry.Vertices.Length}");
            obj.AppendLine($"# Triangles: {geometry.TriangleCount}");
            obj.AppendLine();

            // Write vertices
            foreach (var v in geometry.Vertices)
            {
                obj.AppendLine($"v {v.X:F6} {v.Y:F6} {v.Z:F6}");
            }

            obj.AppendLine();

            // Write normals if present
            if (geometry.Normals.Length > 0)
            {
                foreach (var n in geometry.Normals)
                {
                    obj.AppendLine($"vn {n.X:F6} {n.Y:F6} {n.Z:F6}");
                }
                obj.AppendLine();
            }

            // Write faces
            if (geometry.Indices.Length > 0)
            {
                for (int i = 0; i < geometry.Indices.Length; i += 3)
                {
                    // OBJ indices are 1-based
                    int i1 = geometry.Indices[i] + 1;
                    int i2 = geometry.Indices[i + 1] + 1;
                    int i3 = geometry.Indices[i + 2] + 1;

                    obj.AppendLine($"f {i1} {i2} {i3}");
                }
            }

            return obj.ToString();
        }
    }
}
