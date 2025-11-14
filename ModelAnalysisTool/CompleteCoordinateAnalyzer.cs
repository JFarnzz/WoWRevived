using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using System.Text;

namespace ModelAnalysisTool
{
    /// <summary>
    /// Analyzes the complete coordinate structure in IOB files
    /// to understand vertex/normal organization and implicit face topology
    /// </summary>
    public class CompleteCoordinateAnalyzer
    {
        public static void AnalyzeAllCoordinates(string decompressedFilePath)
        {
            if (!File.Exists(decompressedFilePath))
            {
                Console.WriteLine($"File not found: {decompressedFilePath}");
                return;
            }

            byte[] data = File.ReadAllBytes(decompressedFilePath);
            Console.WriteLine($"=== Complete Coordinate Analysis: {Path.GetFileName(decompressedFilePath)} ===");
            Console.WriteLine($"Total size: {data.Length} bytes (0x{data.Length:X})\n");

            // Read vertex count from header
            ushort headerVertexCount = BitConverter.ToUInt16(data, 0x1E);
            Console.WriteLine($"Header vertex count: {headerVertexCount}");

            // Find where coordinate data starts
            int coordStart = FindCoordinateDataStart(data);
            Console.WriteLine($"Coordinate data starts at offset: 0x{coordStart:X}\n");

            // Parse ALL coordinates until we hit non-coordinate data
            var coordinates = new List<Vector3>();
            int offset = coordStart;
            
            while (offset + 12 <= data.Length)
            {
                float x = BitConverter.ToSingle(data, offset);
                float y = BitConverter.ToSingle(data, offset + 4);
                float z = BitConverter.ToSingle(data, offset + 8);

                if (IsValidCoordinate(x) && IsValidCoordinate(y) && IsValidCoordinate(z))
                {
                    coordinates.Add(new Vector3(x, y, z));
                    offset += 12;
                }
                else
                {
                    break;
                }
            }

            int totalCoords = coordinates.Count;
            Console.WriteLine($"Total coordinates found: {totalCoords}");
            Console.WriteLine($"  - Header declares: {headerVertexCount}");
            Console.WriteLine($"  - Additional coordinates: {totalCoords - headerVertexCount}");
            Console.WriteLine($"  - Ratio: {(float)totalCoords / headerVertexCount:F2}x\n");

            // Analyze remaining bytes after coordinates
            int nonCoordStart = offset;
            int remainingBytes = data.Length - nonCoordStart;
            Console.WriteLine($"Non-coordinate data:");
            Console.WriteLine($"  - Starts at offset: 0x{nonCoordStart:X}");
            Console.WriteLine($"  - Remaining bytes: {remainingBytes}\n");

            if (remainingBytes == 0)
            {
                Console.WriteLine("*** CRITICAL FINDING ***");
                Console.WriteLine("File contains ONLY coordinate data!");
                Console.WriteLine("This means:");
                Console.WriteLine("  1. Face topology is NOT stored in this file");
                Console.WriteLine("  2. Faces might be in a separate file/structure");
                Console.WriteLine("  3. OR faces are implicitly defined by coordinate order");
                Console.WriteLine("  4. OR the format uses triangle strips/fans with no indices\n");
            }
            else
            {
                Console.WriteLine($"Analyzing remaining {remainingBytes} bytes...");
                AnalyzeRemainingData(data, nonCoordStart);
            }

            // Analyze coordinate patterns
            Console.WriteLine("\n=== Coordinate Pattern Analysis ===");
            AnalyzeCoordinatePatterns(coordinates, headerVertexCount);

            // Show sample coordinates
            Console.WriteLine("\n=== Sample Coordinates ===");
            ShowSampleCoordinates(coordinates, headerVertexCount);

            // Export for external analysis
            ExportCoordinatesToCSV(coordinates, headerVertexCount, 
                Path.ChangeExtension(decompressedFilePath, ".coordinates.csv"));
        }

        private static int FindCoordinateDataStart(byte[] data)
        {
            // Look for first sequence of valid float triplets
            for (int i = 0; i < Math.Min(128, data.Length - 24); i++)
            {
                bool valid = true;
                for (int j = 0; j < 2; j++) // Check two consecutive triplets
                {
                    int idx = i + (j * 12);
                    float x = BitConverter.ToSingle(data, idx);
                    float y = BitConverter.ToSingle(data, idx + 4);
                    float z = BitConverter.ToSingle(data, idx + 8);

                    if (!IsValidCoordinate(x) || !IsValidCoordinate(y) || !IsValidCoordinate(z))
                    {
                        valid = false;
                        break;
                    }
                }

                if (valid) return i;
            }

            return 0x16; // Default based on observations
        }

        private static bool IsValidCoordinate(float value)
        {
            // Buildings/units typically in range -100 to +100
            return !float.IsNaN(value) && !float.IsInfinity(value) &&
                   value >= -1000.0f && value <= 1000.0f;
        }

        private static void AnalyzeRemainingData(byte[] data, int startOffset)
        {
            int remaining = data.Length - startOffset;
            if (remaining == 0) return;

            Console.WriteLine($"\nRemaining data analysis:");
            
            // Show first 64 bytes as hex
            Console.WriteLine($"First 64 bytes (hex):");
            for (int i = 0; i < Math.Min(64, remaining); i += 16)
            {
                Console.Write($"  0x{(startOffset + i):X4}: ");
                for (int j = 0; j < 16 && (i + j) < remaining; j++)
                {
                    Console.Write($"{data[startOffset + i + j]:X2} ");
                }
                Console.WriteLine();
            }

            // Try interpreting as various formats
            Console.WriteLine($"\nTrying to interpret as uint16 array:");
            int uint16Count = 0;
            for (int i = startOffset; i + 2 <= data.Length && uint16Count < 20; i += 2)
            {
                ushort value = BitConverter.ToUInt16(data, i);
                Console.WriteLine($"  [{uint16Count}] = {value} (0x{value:X4})");
                uint16Count++;
            }
        }

        private static void AnalyzeCoordinatePatterns(List<Vector3> coordinates, int headerCount)
        {
            if (coordinates.Count == 0) return;

            // Check if coordinates after headerCount are duplicates
            Console.WriteLine($"Checking for duplicate vertices...");
            var uniqueCoords = new HashSet<string>();
            int duplicates = 0;

            for (int i = 0; i < Math.Min(headerCount, coordinates.Count); i++)
            {
                string key = $"{coordinates[i].X:F6},{coordinates[i].Y:F6},{coordinates[i].Z:F6}";
                uniqueCoords.Add(key);
            }

            for (int i = headerCount; i < coordinates.Count; i++)
            {
                string key = $"{coordinates[i].X:F6},{coordinates[i].Y:F6},{coordinates[i].Z:F6}";
                if (uniqueCoords.Contains(key))
                {
                    duplicates++;
                }
            }

            Console.WriteLine($"  - Unique vertices (first {headerCount}): {uniqueCoords.Count}");
            Console.WriteLine($"  - Duplicates in additional coords: {duplicates}");
            Console.WriteLine($"  - Unique additional coords: {(coordinates.Count - headerCount) - duplicates}");

            // Check for patterns in additional coordinates
            if (coordinates.Count > headerCount)
            {
                Console.WriteLine($"\nAnalyzing additional {coordinates.Count - headerCount} coordinates:");
                
                // Calculate average magnitude
                double avgMag = 0;
                for (int i = headerCount; i < coordinates.Count; i++)
                {
                    avgMag += coordinates[i].Length();
                }
                avgMag /= (coordinates.Count - headerCount);
                Console.WriteLine($"  - Average magnitude: {avgMag:F3}");

                // Check if they look like normals (unit vectors)
                int nearUnitLength = 0;
                for (int i = headerCount; i < coordinates.Count; i++)
                {
                    float len = coordinates[i].Length();
                    if (len >= 0.9f && len <= 1.1f)
                        nearUnitLength++;
                }

                double unitPercent = 100.0 * nearUnitLength / (coordinates.Count - headerCount);
                Console.WriteLine($"  - Near unit length (0.9-1.1): {nearUnitLength} ({unitPercent:F1}%)");

                if (unitPercent > 80.0)
                {
                    Console.WriteLine($"\n*** Additional coordinates appear to be NORMALS (unit vectors) ***");
                }
                else
                {
                    Console.WriteLine($"\n*** Additional coordinates appear to be VERTICES (not normals) ***");
                }
            }
        }

        private static void ShowSampleCoordinates(List<Vector3> coordinates, int headerCount)
        {
            int samplesToShow = 10;

            Console.WriteLine($"First {samplesToShow} coordinates (header vertices):");
            for (int i = 0; i < Math.Min(samplesToShow, coordinates.Count); i++)
            {
                var v = coordinates[i];
                Console.WriteLine($"  [{i}]: ({v.X:F3}, {v.Y:F3}, {v.Z:F3}) - Length: {v.Length():F3}");
            }

            if (coordinates.Count > headerCount)
            {
                Console.WriteLine($"\nFirst {samplesToShow} additional coordinates (offset {headerCount}):");
                for (int i = 0; i < Math.Min(samplesToShow, coordinates.Count - headerCount); i++)
                {
                    var v = coordinates[headerCount + i];
                    Console.WriteLine($"  [{headerCount + i}]: ({v.X:F3}, {v.Y:F3}, {v.Z:F3}) - Length: {v.Length():F3}");
                }
            }
        }

        private static void ExportCoordinatesToCSV(List<Vector3> coordinates, int headerCount, string outputPath)
        {
            try
            {
                using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
                writer.WriteLine("Index,X,Y,Z,Length,Type");

                for (int i = 0; i < coordinates.Count; i++)
                {
                    var v = coordinates[i];
                    string type = i < headerCount ? "VERTEX" : "ADDITIONAL";
                    writer.WriteLine($"{i},{v.X},{v.Y},{v.Z},{v.Length()},{type}");
                }

                Console.WriteLine($"\n√ Exported coordinates to: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting coordinates: {ex.Message}");
            }
        }
    }
}
