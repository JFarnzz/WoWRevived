using System;
using System.Collections.Generic;
using System.IO;
using WoWViewer.Parsers;
using ModelAnalysisTool;

namespace WoWViewer
{
    /// <summary>
    /// Command-line utility for analyzing and extracting 3D model files.
    /// Run this to reverse engineer IOB and WOF file formats.
    /// </summary>
    public class ModelAnalysisTool
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== War of the Worlds - 3D Model Analysis Tool ===\n");

            if (args.Length == 0)
            {
                ShowHelp();
                return;
            }

            string command = args[0].ToLower();

            switch (command)
            {
                case "analyze-headers":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Usage: analyze-headers <extracted-models-dir> <output-dir>");
                        return;
                    }
                    HuffmanAnalysisTool.AnalyzeHeaders(args[1], args[2]);
                    break;

                case "test-decompress":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: test-decompress <extracted-models-dir>");
                        return;
                    }
                    HuffmanAnalysisTool.TestDecompression(args[1]);
                    break;

                case "analyze-geometry":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: analyze-geometry <decompressed-file>");
                        return;
                    }
                    GeometryStructureAnalyzer.AnalyzeStructure(args[1]);
                    break;

                case "parse-geometry":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: parse-geometry <decompressed-file>");
                        return;
                    }
                    ParseAndExportGeometry(args[1]);
                    break;

                case "analyze-faces":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: analyze-faces <decompressed-file>");
                        return;
                    }
                    FaceFormatAnalyzer.AnalyzeFaceData(args[1]);
                    break;

                case "analyze-all-coords":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: analyze-all-coords <decompressed-file>");
                        return;
                    }
                    CompleteCoordinateAnalyzer.AnalyzeAllCoordinates(args[1]);
                    break;

                case "test-implicit-faces":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: test-implicit-faces <decompressed-file>");
                        return;
                    }
                    ImplicitTopologyTester.TestImplicitTopology(args[1]);
                    break;

                case "test-index-encoding":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: test-index-encoding <decompressed-file>");
                        return;
                    }
                    IndexEncodingAnalyzer.AnalyzeIndexEncoding(args[1]);
                    break;

                case "test-byte-indices":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: test-byte-indices <decompressed-file>");
                        return;
                    }
                    ByteIndexTester.TestByteIndices(args[1]);
                    break;

                case "test-quad-indices":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: test-quad-indices <decompressed-file>");
                        return;
                    }
                    QuadIndexTester.TestQuadIndices(args[1]);
                    break;

                case "analyze-iob":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: analyze-iob <path-to-iob-file>");
                        return;
                    }
                    AnalyzeIob(args[1]);
                    break;

                case "analyze-wof":
                    if (args.Length < 2)
                    {
                        Console.WriteLine("Usage: analyze-wof <path-to-wof-file>");
                        return;
                    }
                    AnalyzeWof(args[1]);
                    break;

                case "batch-analyze":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Usage: batch-analyze <input-directory> <output-directory>");
                        return;
                    }
                    BatchAnalyze(args[1], args[2]);
                    break;

                case "extract-models":
                    if (args.Length < 3)
                    {
                        Console.WriteLine("Usage: extract-models <dat.wow-path> <output-directory>");
                        return;
                    }
                    ExtractModels(args[1], args[2]);
                    break;

                default:
                    Console.WriteLine($"Unknown command: {command}");
                    ShowHelp();
                    break;
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  analyze-headers <in> <out>  - Analyze FFUH header structure");
            Console.WriteLine("  test-decompress <in>        - Test Huffman decompression");
            Console.WriteLine("  analyze-geometry <file>     - Analyze decompressed geometry structure");
            Console.WriteLine("  analyze-faces <file>        - Deep analysis of face/index data");
            Console.WriteLine("  analyze-all-coords <file>   - Parse ALL coordinates and analyze patterns");
            Console.WriteLine("  test-implicit-faces <file>  - Test if faces are implicit (vertex order)");
            Console.WriteLine("  test-index-encoding <file>  - Test if extra coords encode face indices");
            Console.WriteLine("  test-byte-indices <file>    - Test if post-vertex data is byte indices");
            Console.WriteLine("  test-quad-indices <file>    - Test quad geometry (4 vertices per face)");
            Console.WriteLine("  parse-geometry <file>       - Parse geometry and export to OBJ");
            Console.WriteLine("  analyze-iob <file>          - Analyze single IOB building model");
            Console.WriteLine("  analyze-wof <file>          - Analyze single WOF unit model");
            Console.WriteLine("  batch-analyze <in> <out>    - Analyze all models in directory");
            Console.WriteLine("  extract-models <wow> <out>  - Extract all IOB/WOF from Dat.wow");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  analyze-headers ExtractedModels\\ ModelAnalysis\\");
            Console.WriteLine("  test-decompress ExtractedModels\\");
            Console.WriteLine("  analyze-geometry DecompressedModels\\FARM.IOB.decompressed");
            Console.WriteLine("  parse-geometry DecompressedModels\\FARM.IOB.decompressed");
            Console.WriteLine("  analyze-iob FARM.IOB");
            Console.WriteLine("  extract-models \"D:\\Game\\DAT\\Dat.wow\" \"D:\\Models\"");
            Console.WriteLine();
        }

        private static void AnalyzeIob(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"ERROR: File not found: {filePath}");
                return;
            }

            Console.WriteLine($"Analyzing IOB file: {Path.GetFileName(filePath)}...\n");
            
            try
            {
                IobParser.AnalyzeStructure(filePath);
                var outputPath = Path.ChangeExtension(filePath, ".iob.analysis.txt");
                Console.WriteLine($"\n✓ Analysis complete!");
                Console.WriteLine($"  Output: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ERROR: {ex.Message}");
            }
        }

        private static void AnalyzeWof(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"ERROR: File not found: {filePath}");
                return;
            }

            Console.WriteLine($"Analyzing WOF file: {Path.GetFileName(filePath)}...\n");
            
            try
            {
                WofParser.AnalyzeStructure(filePath);
                var outputPath = Path.ChangeExtension(filePath, ".wof.analysis.txt");
                Console.WriteLine($"\n✓ Analysis complete!");
                Console.WriteLine($"  Output: {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ERROR: {ex.Message}");
            }
        }

        private static void BatchAnalyze(string inputDir, string outputDir)
        {
            if (!Directory.Exists(inputDir))
            {
                Console.WriteLine($"ERROR: Input directory not found: {inputDir}");
                return;
            }

            Directory.CreateDirectory(outputDir);

            Console.WriteLine($"Batch analyzing models from: {inputDir}");
            Console.WriteLine($"Output directory: {outputDir}\n");

            // Analyze IOB files
            var iobFiles = Directory.GetFiles(inputDir, "*.iob", SearchOption.AllDirectories);
            Console.WriteLine($"Found {iobFiles.Length} IOB files\n");

            foreach (var file in iobFiles)
            {
                try
                {
                    Console.Write($"  {Path.GetFileName(file)}... ");
                    var outputPath = Path.Combine(outputDir, Path.GetFileName(file) + ".analysis.txt");
                    IobParser.AnalyzeStructure(file);
                    File.Move(Path.ChangeExtension(file, ".iob.analysis.txt"), outputPath, true);
                    Console.WriteLine("✓");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ {ex.Message}");
                }
            }

            // Analyze WOF files
            var wofFiles = Directory.GetFiles(inputDir, "*.wof", SearchOption.AllDirectories);
            Console.WriteLine($"\nFound {wofFiles.Length} WOF files\n");

            foreach (var file in wofFiles)
            {
                try
                {
                    Console.Write($"  {Path.GetFileName(file)}... ");
                    var outputPath = Path.Combine(outputDir, Path.GetFileName(file) + ".analysis.txt");
                    WofParser.AnalyzeStructure(file);
                    File.Move(Path.ChangeExtension(file, ".wof.analysis.txt"), outputPath, true);
                    Console.WriteLine("✓");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ {ex.Message}");
                }
            }

            Console.WriteLine($"\n✓ Batch analysis complete! Results in: {outputDir}");
        }

        private static void ExtractModels(string wowArchivePath, string outputDir)
        {
            if (!File.Exists(wowArchivePath))
            {
                Console.WriteLine($"ERROR: Archive not found: {wowArchivePath}");
                return;
            }

            Directory.CreateDirectory(outputDir);

            Console.WriteLine($"Extracting 3D models from: {Path.GetFileName(wowArchivePath)}");
            Console.WriteLine($"Output directory: {outputDir}\n");

            try
            {
                // Use existing WOW archive parser
                var entries = ParseWowArchive(wowArchivePath);
                
                int iobCount = 0;
                int wofCount = 0;

                foreach (var entry in entries)
                {
                    var ext = Path.GetExtension(entry.Name).ToLower();
                    
                    if (ext == ".iob" || ext == ".wof")
                    {
                        if (entry.Data == null) continue; // Skip entries without data
                        
                        var outputPath = Path.Combine(outputDir, entry.Name);
                        File.WriteAllBytes(outputPath, entry.Data);
                        
                        if (ext == ".iob")
                        {
                            iobCount++;
                            Console.WriteLine($"  [IOB] {entry.Name}");
                        }
                        else
                        {
                            wofCount++;
                            Console.WriteLine($"  [WOF] {entry.Name}");
                        }
                    }
                }

                Console.WriteLine($"\n✓ Extraction complete!");
                Console.WriteLine($"  IOB files: {iobCount}");
                Console.WriteLine($"  WOF files: {wofCount}");
                Console.WriteLine($"  Total: {iobCount + wofCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ERROR: {ex.Message}");
            }
        }

        // WOW archive parser for KAT! archives (Dat.wow and MAPS.wow)
        private static List<WowFileEntry> ParseWowArchive(string filePath)
        {
            var entries = new List<WowFileEntry>();
            
            using var br = new BinaryReader(File.OpenRead(filePath));
            
            // Read magic ("KAT!" for Dat.wow/MAPS.wow archives)
            string magic = new string(br.ReadChars(4));
            if (magic != "KAT!")
            {
                throw new InvalidDataException($"Invalid archive format. Expected 'KAT!', got '{magic}'");
            }

            int fileCount = br.ReadInt32();

            // Read file table (KAT! format: 4-byte skip, offset, length, 12-byte name, 20-byte skip)
            for (int i = 0; i < fileCount; i++)
            {
                br.ReadInt32();                         // Skip 4 bytes
                int offset = br.ReadInt32();            // File offset
                int length = br.ReadInt32();            // File size
                byte[] nameBytes = br.ReadBytes(12);    // Filename (ASCII, null-padded)
                br.BaseStream.Seek(20, SeekOrigin.Current); // Skip 20 bytes
                
                // Extract null-terminated string
                int zeroIndex = Array.IndexOf(nameBytes, (byte)0);
                string name = System.Text.Encoding.ASCII.GetString(
                    nameBytes, 
                    0, 
                    zeroIndex >= 0 ? zeroIndex : nameBytes.Length
                );

                entries.Add(new WowFileEntry 
                { 
                    Name = name, 
                    Length = length, 
                    Offset = offset 
                });
            }

            // Read file data for each entry
            foreach (var entry in entries)
            {
                br.BaseStream.Seek(entry.Offset, SeekOrigin.Begin);
                entry.Data = br.ReadBytes(entry.Length);
            }

            return entries;
        }

        private static void ParseAndExportGeometry(string decompressedFilePath)
        {
            if (!File.Exists(decompressedFilePath))
            {
                Console.WriteLine($"✗ File not found: {decompressedFilePath}");
                return;
            }

            Console.WriteLine($"\n=== Parsing Geometry: {Path.GetFileName(decompressedFilePath)} ===\n");

            try
            {
                byte[] data = File.ReadAllBytes(decompressedFilePath);
                Console.WriteLine($"Decompressed size: {data.Length} bytes");

                var geometry = WoWViewer.Parsers.GeometryParser.Parse(data);

                Console.WriteLine($"\nParsed geometry:");
                Console.WriteLine($"  Vertices: {geometry.Vertices.Length}");
                Console.WriteLine($"  Normals: {geometry.Normals.Length}");
                Console.WriteLine($"  Triangles: {geometry.TriangleCount}");

                // Show first 10 vertices
                Console.WriteLine($"\nFirst 10 vertices:");
                for (int i = 0; i < Math.Min(10, geometry.Vertices.Length); i++)
                {
                    var v = geometry.Vertices[i];
                    Console.WriteLine($"  [{i}]: ({v.X:F3}, {v.Y:F3}, {v.Z:F3})");
                }

                // Export to OBJ
                string modelName = Path.GetFileNameWithoutExtension(decompressedFilePath);
                string objContent = WoWViewer.Parsers.GeometryParser.ExportToOBJ(geometry, modelName);

                string outputDir = Path.Combine(Path.GetDirectoryName(decompressedFilePath) ?? ".", "..", "ExportedOBJ");
                Directory.CreateDirectory(outputDir);
                string objPath = Path.Combine(outputDir, modelName + ".obj");
                File.WriteAllText(objPath, objContent);

                Console.WriteLine($"\n√ Exported to: {objPath}");
                Console.WriteLine($"  Open in Blender to validate geometry!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }
    }
}
