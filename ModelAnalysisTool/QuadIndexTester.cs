using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ModelAnalysisTool
{
    /// <summary>
    /// Tests quad-based geometry (4 vertices per face)
    /// </summary>
    public class QuadIndexTester
    {
        public static void TestQuadIndices(string decompressedFilePath)
        {
            if (!File.Exists(decompressedFilePath))
            {
                Console.WriteLine($"File not found: {decompressedFilePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(decompressedFilePath);
            Console.WriteLine($"=== Quad Index Hypothesis Test: {Path.GetFileName(decompressedFilePath)} ===\n");

            ushort headerVertexCount = BitConverter.ToUInt16(data, 0x1E);
            Console.WriteLine($"Header vertex count: {headerVertexCount}");

            // Parse vertices
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
            int indexCount = data.Length - indexDataStart;
            Console.WriteLine($"Vertices: {vertices.Count}");
            Console.WriteLine($"Index bytes: {indexCount}");
            Console.WriteLine($"As quads (÷4): {indexCount / 4} quads\n");

            // Parse as byte indices
            var indices = new List<int>();
            for (int i = indexDataStart; i < data.Length; i++)
            {
                indices.Add(data[i]);
            }

            // Analyze as quads
            Console.WriteLine("=== QUAD INTERPRETATION ===");
            AnalyzeAsQuads(vertices, indices);

            // Export both triangle and quad versions
            ExportAsQuads(vertices, indices, 
                Path.Combine(Path.GetDirectoryName(decompressedFilePath), "..", "ExportedOBJ", 
                    Path.GetFileNameWithoutExtension(decompressedFilePath) + ".quads.obj"));

            ExportQuadsAsTriangles(vertices, indices,
                Path.Combine(Path.GetDirectoryName(decompressedFilePath), "..", "ExportedOBJ",
                    Path.GetFileNameWithoutExtension(decompressedFilePath) + ".quads_triangulated.obj"));
        }

        private static void AnalyzeAsQuads(List<Vector3> vertices, List<int> indices)
        {
            int quadCount = indices.Count / 4;
            int degenerateQuads = 0;
            int validQuads = 0;
            var areas = new List<double>();

            for (int i = 0; i + 3 < indices.Count; i += 4)
            {
                int i0 = indices[i];
                int i1 = indices[i + 1];
                int i2 = indices[i + 2];
                int i3 = indices[i + 3];

                // Check for degenerate (repeated indices)
                var uniqueIndices = new HashSet<int> { i0, i1, i2, i3 };
                if (uniqueIndices.Count < 4)
                {
                    degenerateQuads++;
                    continue;
                }

                // Check bounds
                if (i0 >= vertices.Count || i1 >= vertices.Count || 
                    i2 >= vertices.Count || i3 >= vertices.Count)
                {
                    degenerateQuads++;
                    continue;
                }

                // Calculate quad area (approximate as two triangles)
                Vector3 v0 = vertices[i0];
                Vector3 v1 = vertices[i1];
                Vector3 v2 = vertices[i2];
                Vector3 v3 = vertices[i3];

                double area1 = Vector3.Cross(v1 - v0, v2 - v0).Length() / 2.0;
                double area2 = Vector3.Cross(v2 - v0, v3 - v0).Length() / 2.0;
                double totalArea = area1 + area2;

                if (totalArea > 0.001)
                {
                    areas.Add(totalArea);
                    validQuads++;
                }
                else
                {
                    degenerateQuads++;
                }
            }

            Console.WriteLine($"Total quads: {quadCount}");
            Console.WriteLine($"Valid quads: {validQuads}");
            Console.WriteLine($"Degenerate quads: {degenerateQuads}");
            Console.WriteLine($"Degenerate rate: {100.0 * degenerateQuads / quadCount:F1}%");

            if (areas.Count > 0)
            {
                areas.Sort();
                Console.WriteLine($"\nQuad quality:");
                Console.WriteLine($"  Average area: {areas.Average():F3}");
                Console.WriteLine($"  Median area: {areas[areas.Count / 2]:F3}");
                Console.WriteLine($"  Min area: {areas[0]:F3}");
                Console.WriteLine($"  Max area: {areas[areas.Count - 1]:F3}");
            }

            // Show first 20 quads
            Console.WriteLine($"\nFirst 20 quads (indices):");
            for (int i = 0; i < Math.Min(20 * 4, indices.Count); i += 4)
            {
                Console.WriteLine($"  Quad {i / 4}: [{indices[i]}, {indices[i + 1]}, {indices[i + 2]}, {indices[i + 3]}]");
            }
        }

        private static void ExportAsQuads(List<Vector3> vertices, List<int> indices, string outputPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                using var writer = new StreamWriter(outputPath, false, Encoding.ASCII);
                writer.WriteLine($"# Quad-based geometry");
                writer.WriteLine($"# Vertices: {vertices.Count}");
                writer.WriteLine($"# Quads: {indices.Count / 4}");
                writer.WriteLine();

                foreach (var v in vertices)
                {
                    writer.WriteLine($"v {v.X} {v.Y} {v.Z}");
                }

                writer.WriteLine();

                // OBJ supports quads natively
                for (int i = 0; i + 3 < indices.Count; i += 4)
                {
                    int idx0 = indices[i] + 1;
                    int idx1 = indices[i + 1] + 1;
                    int idx2 = indices[i + 2] + 1;
                    int idx3 = indices[i + 3] + 1;
                    writer.WriteLine($"f {idx0} {idx1} {idx2} {idx3}");
                }

                Console.WriteLine($"\n√ Exported quads to: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static void ExportQuadsAsTriangles(List<Vector3> vertices, List<int> indices, string outputPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                using var writer = new StreamWriter(outputPath, false, Encoding.ASCII);
                writer.WriteLine($"# Quads triangulated (2 triangles per quad)");
                writer.WriteLine($"# Vertices: {vertices.Count}");
                writer.WriteLine($"# Quads: {indices.Count / 4}");
                writer.WriteLine($"# Triangles: {indices.Count / 4 * 2}");
                writer.WriteLine();

                foreach (var v in vertices)
                {
                    writer.WriteLine($"v {v.X} {v.Y} {v.Z}");
                }

                writer.WriteLine();

                // Split each quad into 2 triangles
                for (int i = 0; i + 3 < indices.Count; i += 4)
                {
                    int idx0 = indices[i] + 1;
                    int idx1 = indices[i + 1] + 1;
                    int idx2 = indices[i + 2] + 1;
                    int idx3 = indices[i + 3] + 1;

                    // Triangle 1: 0-1-2
                    writer.WriteLine($"f {idx0} {idx1} {idx2}");
                    // Triangle 2: 0-2-3
                    writer.WriteLine($"f {idx0} {idx2} {idx3}");
                }

                Console.WriteLine($"√ Exported triangulated quads to: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
