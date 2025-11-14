using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Text;

namespace ModelAnalysisTool
{
    /// <summary>
    /// Tests hypothesis that IOB files use implicit face topology
    /// where consecutive vertices form triangles
    /// </summary>
    public class ImplicitTopologyTester
    {
        public static void TestImplicitTopology(string decompressedFilePath)
        {
            if (!File.Exists(decompressedFilePath))
            {
                Console.WriteLine($"File not found: {decompressedFilePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(decompressedFilePath);
            Console.WriteLine($"=== Testing Implicit Face Topology: {Path.GetFileName(decompressedFilePath)} ===\n");

            // Parse ALL coordinates
            int coordStart = 0x16; // Known start offset
            var vertices = new List<Vector3>();
            int offset = coordStart;

            while (offset + 12 <= data.Length)
            {
                float x = BitConverter.ToSingle(data, offset);
                float y = BitConverter.ToSingle(data, offset + 4);
                float z = BitConverter.ToSingle(data, offset + 8);

                if (IsValidCoordinate(x) && IsValidCoordinate(y) && IsValidCoordinate(z))
                {
                    vertices.Add(new Vector3(x, y, z));
                    offset += 12;
                }
                else
                {
                    break;
                }
            }

            Console.WriteLine($"Parsed {vertices.Count} total vertices\n");

            // Test Hypothesis 1: Every 3 consecutive vertices = 1 triangle
            Console.WriteLine("=== HYPOTHESIS 1: Triangle List (v0,v1,v2), (v3,v4,v5), ... ===");
            TestTriangleList(vertices);

            // Test Hypothesis 2: Triangle strip (v0,v1,v2), (v1,v2,v3), (v2,v3,v4), ...
            Console.WriteLine("\n=== HYPOTHESIS 2: Triangle Strip (shared edges) ===");
            TestTriangleStrip(vertices);

            // Test Hypothesis 3: First 336 are unique, rest index into them
            Console.WriteLine("\n=== HYPOTHESIS 3: First 336 unique, next 389 are face structure ===");
            TestIndexedGeometry(vertices);

            // Export as triangle list for visual validation in Blender
            Console.WriteLine("\n=== Exporting Triangle List to OBJ for Visual Validation ===");
            ExportAsTriangleList(vertices, 
                Path.Combine(Path.GetDirectoryName(decompressedFilePath), "..", "ExportedOBJ", 
                    Path.GetFileNameWithoutExtension(decompressedFilePath) + ".implicit.obj"));
        }

        private static void TestTriangleList(List<Vector3> vertices)
        {
            int triangleCount = vertices.Count / 3;
            Console.WriteLine($"Would create {triangleCount} triangles from {vertices.Count} vertices");
            
            if (vertices.Count % 3 != 0)
            {
                Console.WriteLine($"WARNING: {vertices.Count % 3} vertices left over (not divisible by 3)");
            }

            // Calculate some triangle statistics
            var areas = new List<double>();
            for (int i = 0; i + 2 < vertices.Count; i += 3)
            {
                Vector3 v0 = vertices[i];
                Vector3 v1 = vertices[i + 1];
                Vector3 v2 = vertices[i + 2];

                // Calculate triangle area using cross product
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
                double minArea = areas[0];
                double maxArea = areas[areas.Count - 1];

                Console.WriteLine($"\nTriangle Statistics:");
                Console.WriteLine($"  Average area: {avgArea:F3}");
                Console.WriteLine($"  Median area: {medianArea:F3}");
                Console.WriteLine($"  Min area: {minArea:F3}");
                Console.WriteLine($"  Max area: {maxArea:F3}");

                // Check for degenerate triangles (area near 0)
                int degenerateCount = areas.Count(a => a < 0.001);
                Console.WriteLine($"  Degenerate triangles (area < 0.001): {degenerateCount} ({100.0 * degenerateCount / areas.Count:F1}%)");

                if (degenerateCount > areas.Count * 0.5)
                {
                    Console.WriteLine($"\n*** HIGH degenerate triangle count - triangle list hypothesis UNLIKELY ***");
                }
                else
                {
                    Console.WriteLine($"\n*** Low degenerate count - triangle list hypothesis PLAUSIBLE ***");
                }
            }
        }

        private static void TestTriangleStrip(List<Vector3> vertices)
        {
            int triangleCount = vertices.Count - 2;
            Console.WriteLine($"Would create {triangleCount} triangles from {vertices.Count} vertices (strip)");

            // Calculate area statistics for strip interpretation
            var areas = new List<double>();
            for (int i = 0; i + 2 < vertices.Count; i++)
            {
                Vector3 v0 = vertices[i];
                Vector3 v1 = vertices[i + 1];
                Vector3 v2 = vertices[i + 2];

                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                double area = Vector3.Cross(edge1, edge2).Length() / 2.0;
                areas.Add(area);
            }

            if (areas.Count > 0)
            {
                double avgArea = areas.Sum() / areas.Count;
                int degenerateCount = areas.Count(a => a < 0.001);
                Console.WriteLine($"  Average area: {avgArea:F3}");
                Console.WriteLine($"  Degenerate triangles: {degenerateCount} ({100.0 * degenerateCount / areas.Count:F1}%)");

                if (degenerateCount > areas.Count * 0.5)
                {
                    Console.WriteLine($"\n*** HIGH degenerate count - triangle strip hypothesis UNLIKELY ***");
                }
            }
        }

        private static void TestIndexedGeometry(List<Vector3> vertices)
        {
            if (vertices.Count < 336)
            {
                Console.WriteLine("Not enough vertices for indexed geometry test");
                return;
            }

            Console.WriteLine($"First 336 vertices would be the unique vertex pool");
            Console.WriteLine($"Remaining {vertices.Count - 336} vertices could encode face structure");
            Console.WriteLine($"\nPossible interpretations:");
            Console.WriteLine($"  1. Each triplet after 336 references indices (but we only have coordinates)");
            Console.WriteLine($"  2. The structure is more complex (quads, mixed primitives)");
            Console.WriteLine($"  3. This hypothesis doesn't match the data (all coordinates, no indices)");
        }

        private static void ExportAsTriangleList(List<Vector3> vertices, string outputPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                using var writer = new StreamWriter(outputPath, false, Encoding.ASCII);
                writer.WriteLine($"# Implicit topology test - triangle list interpretation");
                writer.WriteLine($"# Generated from {vertices.Count} vertices");
                writer.WriteLine($"# WARNING: This assumes every 3 vertices = 1 triangle (may be wrong!)");
                writer.WriteLine();

                // Write all vertices
                foreach (var v in vertices)
                {
                    writer.WriteLine($"v {v.X} {v.Y} {v.Z}");
                }

                writer.WriteLine();

                // Write triangle faces (every 3 vertices)
                int faceCount = vertices.Count / 3;
                for (int i = 0; i < faceCount; i++)
                {
                    int idx1 = i * 3 + 1; // OBJ indices are 1-based
                    int idx2 = i * 3 + 2;
                    int idx3 = i * 3 + 3;
                    writer.WriteLine($"f {idx1} {idx2} {idx3}");
                }

                Console.WriteLine($"√ Exported implicit topology OBJ to: {outputPath}");
                Console.WriteLine($"  Vertices: {vertices.Count}");
                Console.WriteLine($"  Faces: {faceCount}");
                Console.WriteLine($"\nOPEN THIS IN BLENDER to validate if topology is correct!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting: {ex.Message}");
            }
        }

        private static bool IsValidCoordinate(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) &&
                   value >= -1000.0f && value <= 1000.0f;
        }
    }
}
