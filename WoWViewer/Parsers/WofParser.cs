using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace WoWViewer.Parsers
{
    /// <summary>
    /// Parser for .WOF (unit) 3D model files from War of the Worlds.
    /// These files contain 3D geometry data for vehicles, units, and moving objects.
    /// </summary>
    public class WofParser
    {
        /// <summary>
        /// Analyzes a WOF file's binary structure and dumps detailed information.
        /// </summary>
        public static void AnalyzeStructure(string filePath)
        {
            var data = File.ReadAllBytes(filePath);
            var filename = Path.GetFileName(filePath);
            var outputPath = Path.ChangeExtension(filePath, ".wof.analysis.txt");

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
            
            writer.WriteLine($"=== WOF File Analysis: {filename} ===");
            writer.WriteLine($"File Size: {data.Length} bytes ({data.Length:X}h)");
            writer.WriteLine();

            // Dump header
            writer.WriteLine("=== HEADER (First 256 bytes) ===");
            IobParser_DumpHex(writer, data, 0, Math.Min(256, data.Length));
            
            // Pattern analysis
            writer.WriteLine();
            writer.WriteLine("=== PATTERN ANALYSIS ===");
            
            if (data.Length >= 4)
            {
                uint magic = BitConverter.ToUInt32(data, 0);
                writer.WriteLine($"First 4 bytes (uint32): {magic} (0x{magic:X8})");
                writer.WriteLine($"First 4 bytes (int32): {BitConverter.ToInt32(data, 0)}");
            }

            // WOF files might have animation data or multiple frames
            writer.WriteLine();
            writer.WriteLine("=== POTENTIAL ANIMATION DATA ===");
            writer.WriteLine("(WOF files may contain multiple poses/frames for unit animation)");

            writer.WriteLine();
            writer.WriteLine("=== END OF ANALYSIS ===");
        }

        public static WofModel? Parse(string filePath)
        {
            try
            {
                var data = File.ReadAllBytes(filePath);
                return Parse(data);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing WOF file: {ex.Message}");
                return null;
            }
        }

        public static WofModel? Parse(byte[] data)
        {
            if (data.Length < 16)
                return null;

            var model = new WofModel
            {
                RawData = data,
                FileSize = data.Length
            };

            using var ms = new MemoryStream(data);
            using var br = new BinaryReader(ms);

            model.Header1 = br.ReadInt32();
            model.Header2 = br.ReadInt32();
            model.Header3 = br.ReadInt32();
            model.Header4 = br.ReadInt32();

            return model;
        }

        // Helper to share hex dump functionality
        private static void IobParser_DumpHex(StreamWriter writer, byte[] data, int offset, int length)
        {
            for (int i = offset; i < offset + length; i += 16)
            {
                writer.Write($"{i:X8}  ");

                for (int j = 0; j < 16; j++)
                {
                    if (i + j < offset + length)
                        writer.Write($"{data[i + j]:X2} ");
                    else
                        writer.Write("   ");
                    if (j == 7) writer.Write(" ");
                }

                writer.Write(" |");

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
    }

    public class WofModel
    {
        public byte[] RawData { get; set; } = Array.Empty<byte>();
        public int FileSize { get; set; }

        public int Header1 { get; set; }
        public int Header2 { get; set; }
        public int Header3 { get; set; }
        public int Header4 { get; set; }

        // Animation frames
        public List<WofFrame> Frames { get; set; } = new List<WofFrame>();
    }

    public class WofFrame
    {
        public List<Vector3> Vertices { get; set; } = new List<Vector3>();
        public List<IobFace> Faces { get; set; } = new List<IobFace>();
    }
}
