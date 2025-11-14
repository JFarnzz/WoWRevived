using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Text;

namespace ModelAnalysisTool
{
    /// <summary>
    /// Tests if data after vertices is byte-encoded face indices
    /// </summary>
    public class ByteIndexTester
    {
        public static void TestByteIndices(string decompressedFilePath)
        {
            if (!File.Exists(decompressedFilePath))
            {
                Console.WriteLine($"File not found: {decompressedFilePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(decompressedFilePath);
            Console.WriteLine($"=== Byte Index Hypothesis Test: {Path.GetFileName(decompressedFilePath)} ===\n");

            ushort headerVertexCount = BitConverter.ToUInt16(data, 0x1E);
            Console.WriteLine($"Header vertex count: {headerVertexCount}");

            // Parse unique vertices (first 336)
            var vertices = new List<Vector3>();
            int offset = 0x16;
            for (int i = 0; i < headerVertexCount && offset + 12 <= data.Length; i++)
            {
                float x = BitConverter.ToSingle(data, offset);
                float y = BitConverter.ToSingle(data, offset + 4);
                float z = BitConverter.ToSingle(data, offset + 8);
                vertices.Add(new Vector3(x, y, z));
                offset += 12;
            }

            int indexDataStart = offset;
            Console.WriteLine($"Vertices parsed: {vertices.Count}");
            Console.WriteLine($"Index data starts at: 0x{indexDataStart:X}\n");

            // Parse remaining data as byte indices
            var indices = new List<int>();
            int validIndices = 0;
            int invalidIndices = 0;

            for (int i = indexDataStart; i < data.Length; i++)
            {
                byte idx = data[i];
                
                if (idx < headerVertexCount)
                {
                    indices.Add(idx);
                    validIndices++;
                }
                else
                {
                    invalidIndices++;
                }
            }

            Console.WriteLine($"Byte indices analysis:");
            Console.WriteLine($"  Valid indices (< {headerVertexCount}): {validIndices}");
            Console.WriteLine($"  Invalid indices: {invalidIndices}");
            Console.WriteLine($"  Validity rate: {100.0 * validIndices / (validIndices + invalidIndices):F1}%");
            Console.WriteLine($"  Total triangles if valid: {validIndices / 3}");

            // Show first 60 indices
            Console.WriteLine($"\nFirst 60 byte values (groups of 3 for triangles):");
            for (int i = 0; i < Math.Min(60, indices.Count); i++)
            {
                Console.Write($"{indices[i],3}");
                if ((i + 1) % 3 == 0) Console.Write(" | ");
                if ((i + 1) % 15 == 0) Console.WriteLine();
            }
            Console.WriteLine();

            // Check for degenerate triangles
            int degenerates = 0;
            for (int i = 0; i + 2 < indices.Count; i += 3)
            {
                if (indices[i] == indices[i + 1] || 
                    indices[i + 1] == indices[i + 2] || 
                    indices[i] == indices[i + 2])
                {
                    degenerates++;
                }
            }

            Console.WriteLine($"\nDegenerate triangles (repeated indices): {degenerates} / {indices.Count / 3} ({100.0 * degenerates / (indices.Count / 3):F1}%)");

            // Calculate triangle quality
            CalculateTriangleQuality(vertices, indices);

            if (validIndices > invalidIndices * 10) // 90%+ valid
            {
                Console.WriteLine($"\n*** HIGH validity rate - byte index hypothesis LIKELY CORRECT! ***");
                ExportByteIndexed(vertices, indices, 
                    Path.Combine(Path.GetDirectoryName(decompressedFilePath), "..", "ExportedOBJ", 
                        Path.GetFileNameWithoutExtension(decompressedFilePath) + ".byteindexed.obj"));
            }
            else
            {
                Console.WriteLine($"\n*** LOW validity rate - byte index hypothesis incorrect ***");
            }
        }

        private static void CalculateTriangleQuality(List<Vector3> vertices, List<int> indices)
        {
            var areas = new List<double>();
            
            for (int i = 0; i + 2 < indices.Count; i += 3)
            {
                int idx0 = indices[i];
                int idx1 = indices[i + 1];
                int idx2 = indices[i + 2];

                if (idx0 >= vertices.Count || idx1 >= vertices.Count || idx2 >= vertices.Count)
                    continue;

                Vector3 v0 = vertices[idx0];
                Vector3 v1 = vertices[idx1];
                Vector3 v2 = vertices[idx2];

                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                double area = Vector3.Cross(edge1, edge2).Length() / 2.0;
                areas.Add(area);
            }

            if (areas.Count > 0)
            {
                areas.Sort();
                double avgArea = areas.Sum() / areas.Count;
                double medianArea = areas[areas.Count / 2];
                int nearZero = areas.Count(a => a < 0.001);

                Console.WriteLine($"\nTriangle quality:");
                Console.WriteLine($"  Average area: {avgArea:F3}");
                Console.WriteLine($"  Median area: {medianArea:F3}");
                Console.WriteLine($"  Near-zero area: {nearZero} ({100.0 * nearZero / areas.Count:F1}%)");
            }
        }

        private static void ExportByteIndexed(List<Vector3> vertices, List<int> indices, string outputPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                using var writer = new StreamWriter(outputPath, false, Encoding.ASCII);
                writer.WriteLine($"# Byte-indexed geometry interpretation");
                writer.WriteLine($"# Vertices: {vertices.Count}");
                writer.WriteLine($"# Triangles: {indices.Count / 3}");
                writer.WriteLine();

                // Write vertices
                foreach (var v in vertices)
                {
                    writer.WriteLine($"v {v.X} {v.Y} {v.Z}");
                }

                writer.WriteLine();

                // Write faces (OBJ uses 1-based indices)
                for (int i = 0; i + 2 < indices.Count; i += 3)
                {
                    int idx0 = indices[i] + 1;
                    int idx1 = indices[i + 1] + 1;
                    int idx2 = indices[i + 2] + 1;
                    writer.WriteLine($"f {idx0} {idx1} {idx2}");
                }

                Console.WriteLine($"\n√ Exported byte-indexed geometry to: {outputPath}");
                Console.WriteLine($"  OPEN IN BLENDER TO VALIDATE!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting: {ex.Message}");
            }
        }
    }
}
