using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace ModelAnalysisTool
{
    /// <summary>
    /// Analyzes if "additional vertices" are actually encoded face indices
    /// </summary>
    public class IndexEncodingAnalyzer
    {
        public static void AnalyzeIndexEncoding(string decompressedFilePath)
        {
            if (!File.Exists(decompressedFilePath))
            {
                Console.WriteLine($"File not found: {decompressedFilePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(decompressedFilePath);
            Console.WriteLine($"=== Index Encoding Analysis: {Path.GetFileName(decompressedFilePath)} ===\n");

            ushort headerVertexCount = BitConverter.ToUInt16(data, 0x1E);
            Console.WriteLine($"Header vertex count: {headerVertexCount}");

            // Parse first 336 "unique" vertices
            var uniqueVertices = new List<Vector3>();
            int offset = 0x16;
            for (int i = 0; i < headerVertexCount && offset + 12 <= data.Length; i++)
            {
                float x = BitConverter.ToSingle(data, offset);
                float y = BitConverter.ToSingle(data, offset + 4);
                float z = BitConverter.ToSingle(data, offset + 8);
                uniqueVertices.Add(new Vector3(x, y, z));
                offset += 12;
            }

            int faceDataStart = offset;
            Console.WriteLine($"Unique vertices parsed: {uniqueVertices.Count}");
            Console.WriteLine($"Face data starts at: 0x{faceDataStart:X}\n");

            // Parse "additional coordinates" as potential face indices
            var additionalCoords = new List<Vector3>();
            while (offset + 12 <= data.Length)
            {
                float x = BitConverter.ToSingle(data, offset);
                float y = BitConverter.ToSingle(data, offset + 4);
                float z = BitConverter.ToSingle(data, offset + 8);

                if (IsValidCoordinate(x) && IsValidCoordinate(y) && IsValidCoordinate(z))
                {
                    additionalCoords.Add(new Vector3(x, y, z));
                    offset += 12;
                }
                else break;
            }

            Console.WriteLine($"Additional coordinates: {additionalCoords.Count}\n");

            // HYPOTHESIS: Coordinates encode indices
            // Method 1: Coordinates match existing vertices (lookup by position)
            Console.WriteLine("=== HYPOTHESIS 1: Coordinates are References to Unique Vertices ===");
            TestCoordinateLookup(uniqueVertices, additionalCoords);

            // Method 2: Float components encode small integers
            Console.WriteLine("\n=== HYPOTHESIS 2: Float Bits Encode Integer Indices ===");
            TestFloatAsIntegerEncoding(data, faceDataStart, headerVertexCount);

            // Method 3: Check if coordinates are quantized to specific values
            Console.WriteLine("\n=== HYPOTHESIS 3: Coordinates Use Quantized Values ===");
            TestQuantization(additionalCoords);
        }

        private static void TestCoordinateLookup(List<Vector3> uniqueVerts, List<Vector3> faceCoords)
        {
            // Build lookup dictionary for unique vertices
            var vertexLookup = new Dictionary<string, int>();
            for (int i = 0; i < uniqueVerts.Count; i++)
            {
                string key = MakeKey(uniqueVerts[i]);
                if (!vertexLookup.ContainsKey(key))
                    vertexLookup[key] = i;
            }

            Console.WriteLine($"Unique vertex positions: {vertexLookup.Count}");

            // Try to match face coordinates to unique vertices
            var matchedIndices = new List<int>();
            int matches = 0;
            int misses = 0;

            for (int i = 0; i < faceCoords.Count; i++)
            {
                string key = MakeKey(faceCoords[i]);
                if (vertexLookup.TryGetValue(key, out int index))
                {
                    matchedIndices.Add(index);
                    matches++;
                }
                else
                {
                    // Try approximate match (floating point tolerance)
                    int closestIndex = FindClosestVertex(faceCoords[i], uniqueVerts);
                    if (closestIndex >= 0)
                    {
                        matchedIndices.Add(closestIndex);
                        matches++;
                    }
                    else
                    {
                        misses++;
                    }
                }
            }

            double matchPercent = 100.0 * matches / faceCoords.Count;
            Console.WriteLine($"Matches: {matches}/{faceCoords.Count} ({matchPercent:F1}%)");
            Console.WriteLine($"Misses: {misses}");

            if (matchPercent > 80.0)
            {
                Console.WriteLine("\n*** STRONG MATCH - Face coordinates reference unique vertices! ***");
                Console.WriteLine($"This creates {matchedIndices.Count / 3} triangles using indexed geometry");
                
                // Export this interpretation
                ExportIndexedGeometry(uniqueVerts, matchedIndices, "D:\\WoWRevived\\ExportedOBJ\\FARM.IOB.indexed.obj");
            }
            else
            {
                Console.WriteLine("\n*** Poor match - not direct coordinate lookup ***");
            }
        }

        private static void TestFloatAsIntegerEncoding(byte[] data, int start, int maxIndex)
        {
            Console.WriteLine($"Trying to interpret floats as integer indices (max: {maxIndex})...");
            
            var indices = new List<int>();
            int validCount = 0;

            for (int i = start; i + 4 <= data.Length; i += 4)
            {
                float f = BitConverter.ToSingle(data, i);
                int asInt = (int)f;

                if (asInt >= 0 && asInt < maxIndex && Math.Abs(f - asInt) < 0.001f)
                {
                    indices.Add(asInt);
                    validCount++;
                }
                else
                {
                    break;
                }

                if (validCount >= 20) break; // Sample first 20
            }

            if (validCount >= 3)
            {
                Console.WriteLine($"Found {validCount} values that look like integer indices:");
                for (int i = 0; i < Math.Min(20, indices.Count); i++)
                {
                    Console.Write($"{indices[i]} ");
                    if ((i + 1) % 3 == 0) Console.Write("| ");
                }
                Console.WriteLine();
            }
            else
            {
                Console.WriteLine("No clear integer pattern found");
            }
        }

        private static void TestQuantization(List<Vector3> coords)
        {
            // Check if coordinates use a limited set of values (quantized)
            var xValues = new HashSet<float>();
            var yValues = new HashSet<float>();
            var zValues = new HashSet<float>();

            foreach (var c in coords)
            {
                xValues.Add(c.X);
                yValues.Add(c.Y);
                zValues.Add(c.Z);
            }

            Console.WriteLine($"Unique X values: {xValues.Count}");
            Console.WriteLine($"Unique Y values: {yValues.Count}");
            Console.WriteLine($"Unique Z values: {zValues.Count}");

            // Check if values cluster around integers or specific fractions
            var allValues = xValues.Concat(yValues).Concat(zValues).OrderBy(v => v).ToList();
            Console.WriteLine($"\nFirst 20 unique values:");
            for (int i = 0; i < Math.Min(20, allValues.Count); i++)
            {
                Console.Write($"{allValues[i]:F3} ");
            }
            Console.WriteLine();
        }

        private static string MakeKey(Vector3 v, float tolerance = 0.001f)
        {
            // Round to tolerance for floating point comparison
            int x = (int)(v.X / tolerance);
            int y = (int)(v.Y / tolerance);
            int z = (int)(v.Z / tolerance);
            return $"{x},{y},{z}";
        }

        private static int FindClosestVertex(Vector3 coord, List<Vector3> vertices, float maxDist = 0.1f)
        {
            int closestIndex = -1;
            float closestDist = float.MaxValue;

            for (int i = 0; i < vertices.Count; i++)
            {
                float dist = Vector3.Distance(coord, vertices[i]);
                if (dist < closestDist && dist < maxDist)
                {
                    closestDist = dist;
                    closestIndex = i;
                }
            }

            return closestIndex;
        }

        private static void ExportIndexedGeometry(List<Vector3> vertices, List<int> indices, string outputPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                using var writer = new StreamWriter(outputPath, false, Encoding.ASCII);
                writer.WriteLine($"# Indexed geometry interpretation");
                writer.WriteLine($"# Unique vertices: {vertices.Count}");
                writer.WriteLine($"# Triangle indices: {indices.Count / 3}");
                writer.WriteLine();

                // Write unique vertices
                foreach (var v in vertices)
                {
                    writer.WriteLine($"v {v.X} {v.Y} {v.Z}");
                }

                writer.WriteLine();

                // Write triangle faces (groups of 3 indices)
                for (int i = 0; i + 2 < indices.Count; i += 3)
                {
                    int idx1 = indices[i] + 1;     // OBJ is 1-based
                    int idx2 = indices[i + 1] + 1;
                    int idx3 = indices[i + 2] + 1;
                    writer.WriteLine($"f {idx1} {idx2} {idx3}");
                }

                Console.WriteLine($"\n√ Exported indexed geometry to: {outputPath}");
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
