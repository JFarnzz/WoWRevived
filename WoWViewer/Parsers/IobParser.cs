using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace WoWViewer.Parsers
{
    /// <summary>
    /// Parser for .IOB (building) 3D model files from War of the Worlds.
    /// These files contain 3D geometry data for buildings and structures.
    /// </summary>
    public class IobParser
    {
        /// <summary>
        /// Analyzes an IOB file's binary structure and dumps detailed information.
        /// Used for reverse engineering the format.
        /// </summary>
        public static void AnalyzeStructure(string filePath)
        {
            var data = File.ReadAllBytes(filePath);
            var filename = Path.GetFileName(filePath);
            var outputPath = Path.ChangeExtension(filePath, ".iob.analysis.txt");

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
            
            writer.WriteLine($"=== IOB File Analysis: {filename} ===");
            writer.WriteLine($"File Size: {data.Length} bytes ({data.Length:X}h)");
            writer.WriteLine();

            // Dump header (first 256 bytes or less)
            writer.WriteLine("=== HEADER (First 256 bytes) ===");
            DumpHexAndAscii(writer, data, 0, Math.Min(256, data.Length));
            
            // Look for common patterns
            writer.WriteLine();
            writer.WriteLine("=== PATTERN ANALYSIS ===");
            
            // Check for magic numbers
            if (data.Length >= 4)
            {
                uint magic = BitConverter.ToUInt32(data, 0);
                writer.WriteLine($"First 4 bytes (uint32): {magic} (0x{magic:X8})");
                writer.WriteLine($"First 4 bytes (int32): {BitConverter.ToInt32(data, 0)}");
                writer.WriteLine($"First 4 bytes (ASCII): \"{GetAsciiString(data, 0, 4)}\"");
            }
            
            if (data.Length >= 8)
            {
                writer.WriteLine($"Bytes 4-7 (uint32): {BitConverter.ToUInt32(data, 4)} (0x{BitConverter.ToUInt32(data, 4):X8})");
            }

            // Check for potential vertex data (floats are common in 3D)
            writer.WriteLine();
            writer.WriteLine("=== POTENTIAL FLOAT VALUES (First 64 bytes) ===");
            for (int i = 0; i < Math.Min(64, data.Length - 4); i += 4)
            {
                float value = BitConverter.ToSingle(data, i);
                if (!float.IsNaN(value) && !float.IsInfinity(value))
                {
                    writer.WriteLine($"Offset 0x{i:X4}: {value:F6}");
                }
            }

            // Look for repeated structures (common in vertex/face arrays)
            writer.WriteLine();
            writer.WriteLine("=== STRUCTURE DETECTION ===");
            DetectRepeatedStructures(writer, data);

            // Check for null-terminated strings
            writer.WriteLine();
            writer.WriteLine("=== POTENTIAL STRINGS ===");
            FindPotentialStrings(writer, data);

            writer.WriteLine();
            writer.WriteLine("=== END OF ANALYSIS ===");
        }

        /// <summary>
        /// Attempts to parse an IOB file based on discovered structure.
        /// This is a work-in-progress parser that will be refined as we learn more.
        /// </summary>
        public static IobModel? Parse(string filePath)
        {
            try
            {
                var data = File.ReadAllBytes(filePath);
                return Parse(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing IOB file: {ex.Message}");
                return null;
            }
        }

        public static IobModel? Parse(byte[] data)
        {
            if (data.Length < 16)
                return null;

            var model = new IobModel
            {
                RawData = data,
                FileSize = data.Length
            };

            // TODO: Reverse engineer the structure
            // Common 3D model components to look for:
            // - Header with counts (vertices, faces, materials)
            // - Vertex array (position, normal, UV)
            // - Face/triangle indices
            // - Material definitions
            // - Bounding box
            // - LOD levels?

            // Placeholder: Read first values as potential header
            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);

            // Hypothesis: First values might be counts or sizes
            model.Header1 = br.ReadInt32();
            model.Header2 = br.ReadInt32();
            model.Header3 = br.ReadInt32();
            model.Header4 = br.ReadInt32();

            return model;
        }

        /// <summary>
        /// Exports IOB model data to a human-readable format for analysis.
        /// </summary>
        public static void ExportToAnalysis(string iobPath, string outputPath)
        {
            var model = Parse(iobPath);
            if (model == null)
            {
                Console.WriteLine("Failed to parse IOB file.");
                return;
            }

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
            writer.WriteLine($"IOB Model: {Path.GetFileName(iobPath)}");
            writer.WriteLine($"File Size: {model.FileSize} bytes");
            writer.WriteLine();
            writer.WriteLine($"Header Values:");
            writer.WriteLine($"  Header1: {model.Header1} (0x{model.Header1:X8})");
            writer.WriteLine($"  Header2: {model.Header2} (0x{model.Header2:X8})");
            writer.WriteLine($"  Header3: {model.Header3} (0x{model.Header3:X8})");
            writer.WriteLine($"  Header4: {model.Header4} (0x{model.Header4:X8})");
        }

        // Helper methods for analysis

        private static void DumpHexAndAscii(StreamWriter writer, byte[] data, int offset, int length)
        {
            for (int i = offset; i < offset + length; i += 16)
            {
                // Hex address
                writer.Write($"{i:X8}  ");

                // Hex bytes
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < offset + length)
                        writer.Write($"{data[i + j]:X2} ");
                    else
                        writer.Write("   ");

                    if (j == 7) writer.Write(" ");
                }

                writer.Write(" |");

                // ASCII representation
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < offset + length)
                    {
                        byte b = data[i + j];
                        writer.Write(b >= 32 && b < 127 ? (char)b : '.');
                    }
                }

                writer.WriteLine("|");
            }
        }

        private static string GetAsciiString(byte[] data, int offset, int length)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < length && offset + i < data.Length; i++)
            {
                byte b = data[offset + i];
                sb.Append(b >= 32 && b < 127 ? (char)b : '?');
            }
            return sb.ToString();
        }

        private static void DetectRepeatedStructures(StreamWriter writer, byte[] data)
        {
            // Try common structure sizes for 3D data
            int[] structSizes = { 12, 16, 20, 24, 32, 36, 40 }; // floats: 3, 4, 5, 6, 8, 9, 10

            foreach (var size in structSizes)
            {
                if (data.Length < size * 3) continue;

                // Check if data is consistently divided by this structure size
                int possibleCount = data.Length / size;
                bool looksGood = true;

                // Simple heuristic: check if treating as floats produces reasonable values
                int validFloats = 0;
                for (int i = 0; i < Math.Min(possibleCount, 10); i++)
                {
                    int offset = i * size;
                    for (int j = 0; j < size / 4; j++)
                    {
                        if (offset + j * 4 + 4 > data.Length) break;
                        float f = BitConverter.ToSingle(data, offset + j * 4);
                        if (!float.IsNaN(f) && !float.IsInfinity(f) && Math.Abs(f) < 10000)
                            validFloats++;
                    }
                }

                if (validFloats > possibleCount * size / 4 * 0.7) // 70% valid floats
                {
                    writer.WriteLine($"Structure size {size} bytes: {possibleCount} possible entries (looks promising - {validFloats} valid floats)");
                }
            }
        }

        private static void FindPotentialStrings(StreamWriter writer, byte[] data)
        {
            for (int i = 0; i < data.Length - 4; i++)
            {
                // Look for sequences of printable ASCII
                if (IsPrintableAscii(data[i]))
                {
                    int length = 0;
                    while (i + length < data.Length && 
                           IsPrintableAscii(data[i + length]) && 
                           length < 256)
                    {
                        length++;
                    }

                    if (length >= 4) // Minimum string length
                    {
                        string str = Encoding.ASCII.GetString(data, i, length);
                        writer.WriteLine($"Offset 0x{i:X4}: \"{str}\" (length: {length})");
                        i += length; // Skip past this string
                    }
                }
            }
        }

        private static bool IsPrintableAscii(byte b)
        {
            return b >= 32 && b < 127;
        }

        /// <summary>
        /// Batch analyze all IOB files in a directory.
        /// </summary>
        public static void AnalyzeAllInDirectory(string directory, string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            var iobFiles = Directory.GetFiles(directory, "*.iob", SearchOption.AllDirectories);

            Console.WriteLine($"Found {iobFiles.Length} IOB files to analyze.");

            foreach (var file in iobFiles)
            {
                try
                {
                    Console.WriteLine($"Analyzing: {Path.GetFileName(file)}...");
                    var outputPath = Path.Combine(outputDirectory, Path.GetFileName(file) + ".analysis.txt");
                    AnalyzeStructure(file);
                    Console.WriteLine($"  -> {Path.GetFileName(outputPath)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ERROR: {ex.Message}");
                }
            }

            Console.WriteLine($"\nAnalysis complete! Results in: {outputDirectory}");
        }
    }

    /// <summary>
    /// Represents a parsed IOB 3D model.
    /// Structure will be refined as we reverse engineer the format.
    /// </summary>
    public class IobModel
    {
        public byte[] RawData { get; set; } = Array.Empty<byte>();
        public int FileSize { get; set; }

        // Header (unknown structure yet)
        public int Header1 { get; set; }
        public int Header2 { get; set; }
        public int Header3 { get; set; }
        public int Header4 { get; set; }

        // Geometry data (to be populated once format is understood)
        public List<Vector3> Vertices { get; set; } = new List<Vector3>();
        public List<Vector3> Normals { get; set; } = new List<Vector3>();
        public List<Vector2> UVs { get; set; } = new List<Vector2>();
        public List<IobFace> Faces { get; set; } = new List<IobFace>();
        public List<IobMaterial> Materials { get; set; } = new List<IobMaterial>();

        // Metadata
        public string Name { get; set; } = string.Empty;
        public Vector3 BoundingBoxMin { get; set; }
        public Vector3 BoundingBoxMax { get; set; }
    }

    public class IobFace
    {
        public int[] VertexIndices { get; set; } = new int[3]; // Triangle
        public int MaterialIndex { get; set; }
    }

    public class IobMaterial
    {
        public string Name { get; set; } = string.Empty;
        public Vector4 DiffuseColor { get; set; }
        public string TextureName { get; set; } = string.Empty;
    }
}
