using System;
using System.IO;
using System.Text;

namespace ModelAnalysisTool
{
    /// <summary>
    /// Analyzes decompressed IOB/WOF geometry data to identify structure
    /// </summary>
    public class GeometryStructureAnalyzer
    {
        public static void AnalyzeStructure(string decompressedFilePath)
        {
            if (!File.Exists(decompressedFilePath))
            {
                Console.WriteLine($"File not found: {decompressedFilePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(decompressedFilePath);
            Console.WriteLine($"=== Geometry Structure Analysis: {Path.GetFileName(decompressedFilePath)} ===");
            Console.WriteLine($"Total size: {data.Length} bytes\n");

            // Look for header patterns
            AnalyzeHeader(data);
            
            // Look for repeating patterns (vertices, faces)
            FindRepeatingPatterns(data);
            
            // Analyze as float array
            AnalyzeAsFloats(data);
            
            // Analyze as uint16 array
            AnalyzeAsUInt16(data);
        }

        private static void AnalyzeHeader(byte[] data)
        {
            Console.WriteLine("--- Potential Header (First 64 bytes) ---");
            
            // Show as hex
            Console.WriteLine("Hex:");
            for (int i = 0; i < Math.Min(64, data.Length); i += 16)
            {
                Console.Write($"  {i:X4}: ");
                for (int j = 0; j < 16 && i + j < data.Length; j++)
                {
                    Console.Write($"{data[i + j]:X2} ");
                }
                Console.WriteLine();
            }
            
            // Show as uint32
            Console.WriteLine("\nAs uint32 values:");
            for (int i = 0; i < Math.Min(64, data.Length); i += 4)
            {
                uint value = BitConverter.ToUInt32(data, i);
                Console.WriteLine($"  [{i:X2}]: {value,10} (0x{value:X8})");
            }
            
            // Show as uint16
            Console.WriteLine("\nAs uint16 values:");
            for (int i = 0; i < Math.Min(32, data.Length); i += 2)
            {
                ushort value = BitConverter.ToUInt16(data, i);
                Console.WriteLine($"  [{i:X2}]: {value,6} (0x{value:X4})");
            }
            
            Console.WriteLine();
        }

        private static void FindRepeatingPatterns(byte[] data)
        {
            Console.WriteLine("--- Pattern Analysis ---");
            
            // Check for 12-byte patterns (3 floats = XYZ vertex)
            Console.WriteLine("\nChecking for 12-byte vertex patterns (float x, y, z):");
            int vertexCandidates = 0;
            for (int i = 0; i < data.Length - 12; i += 12)
            {
                float x = BitConverter.ToSingle(data, i);
                float y = BitConverter.ToSingle(data, i + 4);
                float z = BitConverter.ToSingle(data, i + 8);
                
                if (IsValidCoordinate(x) && IsValidCoordinate(y) && IsValidCoordinate(z))
                {
                    vertexCandidates++;
                    if (vertexCandidates <= 10)
                    {
                        Console.WriteLine($"  Offset {i:X4}: ({x:F3}, {y:F3}, {z:F3})");
                    }
                }
            }
            Console.WriteLine($"  Total 12-byte valid patterns: {vertexCandidates}");
            
            // Check for 24-byte patterns (vertex + normal)
            Console.WriteLine("\nChecking for 24-byte patterns (vertex + normal):");
            int vertexNormalCandidates = 0;
            for (int i = 0; i < data.Length - 24; i += 24)
            {
                float x = BitConverter.ToSingle(data, i);
                float y = BitConverter.ToSingle(data, i + 4);
                float z = BitConverter.ToSingle(data, i + 8);
                float nx = BitConverter.ToSingle(data, i + 12);
                float ny = BitConverter.ToSingle(data, i + 16);
                float nz = BitConverter.ToSingle(data, i + 20);
                
                if (IsValidCoordinate(x) && IsValidCoordinate(y) && IsValidCoordinate(z) &&
                    IsValidNormal(nx) && IsValidNormal(ny) && IsValidNormal(nz))
                {
                    vertexNormalCandidates++;
                    if (vertexNormalCandidates <= 10)
                    {
                        Console.WriteLine($"  Offset {i:X4}: V({x:F2}, {y:F2}, {z:F2}) N({nx:F2}, {ny:F2}, {nz:F2})");
                    }
                }
            }
            Console.WriteLine($"  Total 24-byte valid patterns: {vertexNormalCandidates}");
            
            Console.WriteLine();
        }

        private static void AnalyzeAsFloats(byte[] data)
        {
            Console.WriteLine("--- Float Analysis (First 256 bytes) ---");
            
            int validFloats = 0;
            int totalFloats = 0;
            
            for (int i = 0; i < Math.Min(256, data.Length - 4); i += 4)
            {
                float value = BitConverter.ToSingle(data, i);
                totalFloats++;
                
                if (!float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) < 1000)
                {
                    validFloats++;
                    if (validFloats <= 20)
                    {
                        Console.WriteLine($"  [{i:X4}]: {value:F6}");
                    }
                }
            }
            
            Console.WriteLine($"\nValid floats: {validFloats}/{totalFloats} ({100.0 * validFloats / totalFloats:F1}%)");
            Console.WriteLine();
        }

        private static void AnalyzeAsUInt16(byte[] data)
        {
            Console.WriteLine("--- UInt16 Analysis (Looking for face indices) ---");
            
            // Find sequences of small integers (face indices)
            Console.WriteLine("\nSequences of small uint16 values (< 1000):");
            int sequenceStart = -1;
            int sequenceLength = 0;
            
            for (int i = 0; i < data.Length - 2; i += 2)
            {
                ushort value = BitConverter.ToUInt16(data, i);
                
                if (value < 1000)
                {
                    if (sequenceStart == -1)
                    {
                        sequenceStart = i;
                        sequenceLength = 1;
                    }
                    else
                    {
                        sequenceLength++;
                    }
                }
                else
                {
                    if (sequenceLength >= 10)
                    {
                        Console.WriteLine($"  Offset {sequenceStart:X4}: {sequenceLength} uint16 values (potential face indices)");
                        
                        // Show first few values
                        Console.Write($"    First 10: ");
                        for (int j = 0; j < Math.Min(10, sequenceLength); j++)
                        {
                            ushort idx = BitConverter.ToUInt16(data, sequenceStart + j * 2);
                            Console.Write($"{idx} ");
                        }
                        Console.WriteLine();
                    }
                    
                    sequenceStart = -1;
                    sequenceLength = 0;
                }
            }
            
            Console.WriteLine();
        }

        private static bool IsValidCoordinate(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && 
                   Math.Abs(value) < 100.0f;  // Typical game coordinate range
        }

        private static bool IsValidNormal(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && 
                   Math.Abs(value) <= 1.01f;  // Normals should be -1 to 1
        }
    }
}
