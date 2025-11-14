using System;
using System.Collections.Generic;
using System.IO;
using WoWViewer.Parsers;

namespace ModelAnalysisTool
{
    /// <summary>
    /// Test harness for analyzing FFUH header structure and implementing decompression
    /// </summary>
    public class HuffmanAnalysisTool
    {
        public static void AnalyzeHeaders(string extractedModelsPath, string outputPath)
        {
            Console.WriteLine("=== FFUH Header Analysis Tool ===\n");
            
            // Analyze a sample of IOB and WOF files
            string[] testFiles = new string[]
            {
                Path.Combine(extractedModelsPath, "FARM.IOB"),
                Path.Combine(extractedModelsPath, "CASTLE.IOB"),
                Path.Combine(extractedModelsPath, "AIRSHIP.WOF"),
                Path.Combine(extractedModelsPath, "FIGHTING.WOF"),
            };

            var results = new List<string>();
            results.Add("=== FFUH Header Analysis Results ===");
            results.Add($"Date: {DateTime.Now}");
            results.Add("");
            
            foreach (string filePath in testFiles)
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"✗ File not found: {filePath}");
                    continue;
                }

                Console.WriteLine($"\n--- Analyzing: {Path.GetFileName(filePath)} ---");
                results.Add($"\n=== {Path.GetFileName(filePath)} ===");
                
                try
                {
                    var headerInfo = HuffmanDecoder.AnalyzeHeader(filePath);
                    
                    results.Add($"File Size: {headerInfo["FileSize"]} bytes");
                    results.Add($"Magic: {headerInfo["Magic"]}");
                    results.Add("");
                    results.Add("Header Values:");
                    results.Add($"  Offset 0x04: {headerInfo["Offset_0x04"]:X8}h ({headerInfo["Offset_0x04"]} dec)");
                    results.Add($"  Offset 0x08: {headerInfo["Offset_0x08"]:X8}h ({headerInfo["Offset_0x08"]} dec)");
                    results.Add($"  Offset 0x0C: {headerInfo["Offset_0x0C"]:X8}h ({headerInfo["Offset_0x0C"]} dec)");
                    results.Add($"  Offset 0x10: {headerInfo["Offset_0x10"]:X8}h ({headerInfo["Offset_0x10"]} dec)");
                    results.Add("");
                    
                    // Display to console
                    Console.WriteLine($"  File Size: {headerInfo["FileSize"]} bytes");
                    Console.WriteLine($"  Offset 0x04: 0x{headerInfo["Offset_0x04"]:X8} ({headerInfo["Offset_0x04"]} dec)");
                    Console.WriteLine($"  Offset 0x08: 0x{headerInfo["Offset_0x08"]:X8} ({headerInfo["Offset_0x08"]} dec)");
                    Console.WriteLine($"  Offset 0x0C: 0x{headerInfo["Offset_0x0C"]:X8} ({headerInfo["Offset_0x0C"]} dec)");
                    Console.WriteLine($"  Offset 0x10: 0x{headerInfo["Offset_0x10"]:X8} ({headerInfo["Offset_0x10"]} dec)");
                    
                    // Analyze pattern
                    var pattern = headerInfo["HeaderPattern"] as List<uint>;
                    if (pattern != null && pattern.Count > 0)
                    {
                        results.Add("First 16 uint32 values:");
                        Console.WriteLine("  First 16 uint32 values:");
                        
                        for (int i = 0; i < Math.Min(16, pattern.Count); i++)
                        {
                            string line = $"    [{i * 4:X2}]: 0x{pattern[i]:X8} ({pattern[i],8} dec)";
                            results.Add(line);
                            Console.WriteLine(line);
                        }
                    }
                    
                    // Hypothesis testing
                    results.Add("");
                    results.Add("Hypothesis:");
                    uint fileSize = (uint)(int)headerInfo["FileSize"];
                    uint val1 = (uint)headerInfo["Offset_0x04"];
                    uint val2 = (uint)headerInfo["Offset_0x08"];
                    uint val3 = (uint)headerInfo["Offset_0x0C"];
                    
                    if (val2 > fileSize && val2 < fileSize * 10)
                    {
                        results.Add($"  - Offset 0x08 ({val2}) likely DECOMPRESSED SIZE (larger than file)");
                        Console.WriteLine($"  ✓ Offset 0x08 likely decompressed size: {val2} bytes");
                    }
                    
                    if (val3 > 16 && val3 < fileSize)
                    {
                        results.Add($"  - Offset 0x0C ({val3}) likely DATA OFFSET (within file bounds)");
                        Console.WriteLine($"  ✓ Offset 0x0C likely data offset: 0x{val3:X}");
                    }
                    
                    if (val1 < fileSize && val1 > 1000)
                    {
                        results.Add($"  - Offset 0x04 ({val1}) likely COMPRESSED SIZE or TREE SIZE");
                        Console.WriteLine($"  ✓ Offset 0x04 likely compressed/tree size: {val1} bytes");
                    }
                    
                    Console.WriteLine("  ✓ Analysis complete");
                }
                catch (Exception ex)
                {
                    string error = $"✗ Error: {ex.Message}";
                    results.Add(error);
                    Console.WriteLine($"  {error}");
                }
            }
            
            // Save results
            string outputFile = Path.Combine(outputPath, "HUFFMAN_HEADER_ANALYSIS.txt");
            Directory.CreateDirectory(outputPath);
            File.WriteAllLines(outputFile, results);
            Console.WriteLine($"\n√ Analysis saved to: {outputFile}");
        }

        public static void TestDecompression(string extractedModelsPath)
        {
            Console.WriteLine("\n=== FFUH Decompression Test ===\n");
            
            string testFile = Path.Combine(extractedModelsPath, "FARM.IOB");
            if (!File.Exists(testFile))
            {
                Console.WriteLine($"✗ Test file not found: {testFile}");
                return;
            }

            Console.WriteLine($"Testing decompression on: {Path.GetFileName(testFile)}");
            
            var result = HuffmanDecoder.Decompress(testFile);
            
            Console.WriteLine($"Success: {result.Success}");
            Console.WriteLine($"Compressed Size: {result.CompressedSize} bytes");
            Console.WriteLine($"Decompressed Size: {result.DecompressedSize} bytes");
            
            if (!result.Success)
            {
                Console.WriteLine($"Error: {result.ErrorMessage}");
            }
            else
            {
                Console.WriteLine($"√ Decompression successful!");
                Console.WriteLine($"  Output size: {result.Data?.Length} bytes");
                
                if (result.Data != null)
                {
                    // Save decompressed data
                    string outputPath = Path.Combine(Path.GetDirectoryName(extractedModelsPath) ?? ".", "DecompressedModels");
                    Directory.CreateDirectory(outputPath);
                    string outputFile = Path.Combine(outputPath, "FARM.IOB.decompressed");
                    File.WriteAllBytes(outputFile, result.Data);
                    Console.WriteLine($"  Saved to: {outputFile}");
                    
                    // Analyze decompressed data
                    AnalyzeDecompressedData(result.Data);
                }
            }
        }

        private static void AnalyzeDecompressedData(byte[] data)
        {
            Console.WriteLine("\n--- Decompressed Data Analysis ---");
            
            // Check for float patterns (3D coordinates typically -100 to +100)
            int floatMatches = 0;
            for (int i = 0; i < Math.Min(data.Length - 4, 1000); i += 4)
            {
                float value = BitConverter.ToSingle(data, i);
                if (!float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) < 10000)
                {
                    floatMatches++;
                }
            }
            Console.WriteLine($"  Valid float values in first 1000 bytes: {floatMatches}/250");
            
            // Check for uint16 patterns (face indices)
            int smallInts = 0;
            for (int i = 0; i < Math.Min(data.Length - 2, 1000); i += 2)
            {
                ushort value = BitConverter.ToUInt16(data, i);
                if (value < 10000)
                {
                    smallInts++;
                }
            }
            Console.WriteLine($"  Small uint16 values in first 1000 bytes: {smallInts}/500");
            
            // Show first 128 bytes as hex
            Console.WriteLine("\n  First 128 bytes (hex):");
            for (int i = 0; i < Math.Min(128, data.Length); i += 16)
            {
                Console.Write($"    {i:X4}:  ");
                for (int j = 0; j < 16 && i + j < data.Length; j++)
                {
                    Console.Write($"{data[i + j]:X2} ");
                }
                Console.WriteLine();
            }
            
            // Look for potential structure
            Console.WriteLine("\n  Potential float values (first 64 floats):");
            for (int i = 0; i < Math.Min(data.Length - 4, 256); i += 4)
            {
                float value = BitConverter.ToSingle(data, i);
                if (!float.IsNaN(value) && !float.IsInfinity(value) && Math.Abs(value) < 1000)
                {
                    Console.WriteLine($"    Offset {i:X4}: {value:F6}");
                    if (i >= 192) break; // Show first 16 valid floats
                }
            }
        }
    }
}
