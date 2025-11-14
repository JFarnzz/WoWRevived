using System;
using System.IO;
using System.Collections.Generic;

namespace ModelAnalysisTool
{
    /// <summary>
    /// Deep analysis of IOB geometry structure to find face data
    /// </summary>
    public class FaceFormatAnalyzer
    {
        public static void AnalyzeFaceData(string decompressedFilePath)
        {
            if (!File.Exists(decompressedFilePath))
            {
                Console.WriteLine($"File not found: {decompressedFilePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(decompressedFilePath);
            Console.WriteLine($"=== Face Format Analysis: {Path.GetFileName(decompressedFilePath)} ===");
            Console.WriteLine($"Total size: {data.Length} bytes (0x{data.Length:X})\n");

            // Read vertex count from header
            ushort vertexCount = BitConverter.ToUInt16(data, 0x1E);
            Console.WriteLine($"Vertex count from header: {vertexCount}");
            
            // Calculate vertex data region
            int vertexDataStart = 0x16; // Observed from parsing
            int vertexDataSize = vertexCount * 12; // 3 floats per vertex
            int vertexDataEnd = vertexDataStart + vertexDataSize;
            
            Console.WriteLine($"Vertex data: 0x{vertexDataStart:X} to 0x{vertexDataEnd:X} ({vertexDataSize} bytes)");
            Console.WriteLine($"Remaining data: {data.Length - vertexDataEnd} bytes\n");

            // Analyze what's after vertices
            int postVertexStart = vertexDataEnd;
            
            // Check if it's more vertices (normals, duplicates, etc.)
            Console.WriteLine("--- Checking for Additional Vertex Data ---");
            int additionalVertices = 0;
            int offset = postVertexStart;
            while (offset + 12 <= data.Length)
            {
                float x = BitConverter.ToSingle(data, offset);
                float y = BitConverter.ToSingle(data, offset + 4);
                float z = BitConverter.ToSingle(data, offset + 8);
                
                if (IsValidCoordinate(x) && IsValidCoordinate(y) && IsValidCoordinate(z))
                {
                    additionalVertices++;
                    offset += 12;
                }
                else
                {
                    break;
                }
            }
            
            Console.WriteLine($"Additional vertex-like data: {additionalVertices} sets of coordinates");
            Console.WriteLine($"Total coordinates in file: {vertexCount + additionalVertices}");
            Console.WriteLine($"Non-coordinate data starts at: 0x{offset:X}\n");

            // Analyze remaining bytes
            if (offset < data.Length)
            {
                Console.WriteLine("--- Analyzing Remaining Bytes ---");
                int remainingBytes = data.Length - offset;
                Console.WriteLine($"Remaining bytes: {remainingBytes} (0x{remainingBytes:X})");
                
                // Show first 128 bytes
                Console.WriteLine($"\nFirst 128 bytes at offset 0x{offset:X}:");
                for (int i = 0; i < Math.Min(128, remainingBytes); i += 16)
                {
                    Console.Write($"  {offset + i:X4}: ");
                    for (int j = 0; j < 16 && i + j < remainingBytes; j++)
                    {
                        Console.Write($"{data[offset + i + j]:X2} ");
                    }
                    Console.WriteLine();
                }
                
                // Byte frequency analysis
                Console.WriteLine("\n--- Byte Frequency Analysis ---");
                var freq = new Dictionary<byte, int>();
                for (int i = offset; i < data.Length; i++)
                {
                    if (!freq.ContainsKey(data[i]))
                        freq[data[i]] = 0;
                    freq[data[i]]++;
                }
                
                Console.WriteLine("Most common bytes:");
                var sorted = new List<KeyValuePair<byte, int>>(freq);
                sorted.Sort((a, b) => b.Value.CompareTo(a.Value));
                for (int i = 0; i < Math.Min(10, sorted.Count); i++)
                {
                    Console.WriteLine($"  0x{sorted[i].Key:X2}: {sorted[i].Value} times ({100.0 * sorted[i].Value / remainingBytes:F1}%)");
                }
                
                // Try parsing as uint8 indices
                Console.WriteLine("\n--- Hypothesis 1: uint8 Face Indices ---");
                int validUint8Indices = 0;
                for (int i = offset; i < data.Length; i++)
                {
                    if (data[i] < vertexCount)
                        validUint8Indices++;
                }
                Console.WriteLine($"Bytes that could be valid uint8 indices: {validUint8Indices}/{remainingBytes} ({100.0 * validUint8Indices / remainingBytes:F1}%)");
                
                if (validUint8Indices > remainingBytes * 0.8)
                {
                    Console.WriteLine("→ LIKELY: Data contains uint8 face indices!");
                    ShowUint8Indices(data, offset, Math.Min(30, remainingBytes));
                }
                
                // Try parsing as uint16 indices
                Console.WriteLine("\n--- Hypothesis 2: uint16 Face Indices ---");
                int validUint16Indices = 0;
                for (int i = offset; i + 1 < data.Length; i += 2)
                {
                    ushort idx = BitConverter.ToUInt16(data, i);
                    if (idx < vertexCount || idx == 0xFFFF) // Include restart marker
                        validUint16Indices++;
                }
                Console.WriteLine($"uint16 values that could be valid indices: {validUint16Indices}/{remainingBytes / 2} ({100.0 * validUint16Indices / (remainingBytes / 2):F1}%)");
                
                if (validUint16Indices > (remainingBytes / 2) * 0.8)
                {
                    Console.WriteLine("→ LIKELY: Data contains uint16 face indices!");
                    ShowUint16Indices(data, offset, Math.Min(30, remainingBytes));
                }
            }
        }

        private static void ShowUint8Indices(byte[] data, int offset, int count)
        {
            Console.WriteLine("\nFirst uint8 values (as indices):");
            Console.Write("  ");
            for (int i = 0; i < count && offset + i < data.Length; i++)
            {
                Console.Write($"{data[offset + i]} ");
                if ((i + 1) % 15 == 0)
                    Console.Write("\n  ");
            }
            Console.WriteLine();
            
            // Try to identify triangles
            Console.WriteLine("\nAs triangle indices (groups of 3):");
            for (int i = 0; i < Math.Min(30, count); i += 3)
            {
                if (offset + i + 2 < data.Length)
                {
                    Console.WriteLine($"  Triangle {i/3}: [{data[offset+i]}, {data[offset+i+1]}, {data[offset+i+2]}]");
                }
            }
        }

        private static void ShowUint16Indices(byte[] data, int offset, int count)
        {
            Console.WriteLine("\nFirst uint16 values (as indices):");
            Console.Write("  ");
            int shown = 0;
            for (int i = 0; i < count && offset + i + 1 < data.Length; i += 2)
            {
                ushort idx = BitConverter.ToUInt16(data, offset + i);
                Console.Write($"{idx} ");
                shown++;
                if (shown % 15 == 0)
                    Console.Write("\n  ");
            }
            Console.WriteLine();
        }

        private static bool IsValidCoordinate(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) < 100.0f;
        }
    }
}
