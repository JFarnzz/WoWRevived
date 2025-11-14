using System;
using System.Collections.Generic;
using System.IO;

namespace WoWViewer.Parsers
{
    /// <summary>
    /// Decoder for FFUH (Huffman-compressed) 3D model files used in the RAGE engine.
    /// IOB files contain building geometry, WOF files contain unit animation data.
    /// </summary>
    public class HuffmanDecoder
    {
        private const uint FFUH_MAGIC = 0x48554646; // "FFUH" in little-endian (HUFF reversed)
        
        /// <summary>
        /// Result of Huffman decompression operation
        /// </summary>
        public class DecompressionResult
        {
            /// <summary>Decompressed data bytes</summary>
            public byte[]? Data { get; set; }
            
            /// <summary>Number of bytes in compressed input</summary>
            public int CompressedSize { get; set; }
            
            /// <summary>Number of bytes in decompressed output</summary>
            public int DecompressedSize { get; set; }
            
            /// <summary>Whether decompression completed successfully</summary>
            public bool Success { get; set; }
            
            /// <summary>Error message if decompression failed</summary>
            public string? ErrorMessage { get; set; }
        }

        /// <summary>
        /// Huffman tree node used during decoding
        /// </summary>
        private class HuffmanNode
        {
            public byte? Value { get; set; }  // Leaf nodes have values
            public HuffmanNode? Left { get; set; }   // 0 bit
            public HuffmanNode? Right { get; set; }  // 1 bit
            public bool IsLeaf => Value.HasValue;
        }

        /// <summary>
        /// Analyzes FFUH header structure to understand compression format
        /// </summary>
        /// <param name="filePath">Path to IOB or WOF file</param>
        /// <returns>Header analysis information</returns>
        public static Dictionary<string, object> AnalyzeHeader(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            byte[] data = File.ReadAllBytes(filePath);
            if (data.Length < 16)
                throw new InvalidDataException($"File too small: {data.Length} bytes");

            uint magic = BitConverter.ToUInt32(data, 0);
            if (magic != FFUH_MAGIC)
                throw new InvalidDataException($"Invalid magic: 0x{magic:X8}, expected 0x{FFUH_MAGIC:X8}");

            var result = new Dictionary<string, object>
            {
                { "Magic", "FFUH" },
                { "FileSize", data.Length },
                { "Offset_0x04", BitConverter.ToUInt32(data, 0x04) },  // Compressed size candidate
                { "Offset_0x08", BitConverter.ToUInt32(data, 0x08) },  // Decompressed size candidate
                { "Offset_0x0C", BitConverter.ToUInt32(data, 0x0C) },  // Data offset candidate
                { "Offset_0x10", BitConverter.ToUInt32(data, 0x10) },  // Tree size or additional info
            };

            // Analyze patterns in header
            List<uint> headerValues = new List<uint>();
            for (int i = 4; i < Math.Min(64, data.Length); i += 4)
            {
                headerValues.Add(BitConverter.ToUInt32(data, i));
            }
            result["HeaderPattern"] = headerValues;

            return result;
        }

        /// <summary>
        /// Decompress FFUH-compressed file data
        /// </summary>
        /// <param name="filePath">Path to IOB or WOF file</param>
        /// <returns>Decompression result with raw geometry data</returns>
        public static DecompressionResult Decompress(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new DecompressionResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"File not found: {filePath}" 
                    };

                byte[] data = File.ReadAllBytes(filePath);
                return Decompress(data);
            }
            catch (Exception ex)
            {
                return new DecompressionResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Decompression failed: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// Decompress FFUH-compressed byte array
        /// </summary>
        /// <param name="compressedData">Compressed data starting with FFUH magic</param>
        /// <returns>Decompression result</returns>
        public static DecompressionResult Decompress(byte[] compressedData)
        {
            try
            {
                // Validate magic bytes
                if (compressedData.Length < 16)
                    return new DecompressionResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Data too small: {compressedData.Length} bytes" 
                    };

                uint magic = BitConverter.ToUInt32(compressedData, 0);
                if (magic != FFUH_MAGIC)
                    return new DecompressionResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Invalid magic: 0x{magic:X8}" 
                    };

                // Parse header (structure to be determined through testing)
                // Common Huffman formats:
                // - Offset 0x04: Compressed size OR tree size
                // - Offset 0x08: Decompressed size
                // - Offset 0x0C: Data start offset OR tree start offset
                
                uint value1 = BitConverter.ToUInt32(compressedData, 0x04);
                uint value2 = BitConverter.ToUInt32(compressedData, 0x08);
                uint value3 = BitConverter.ToUInt32(compressedData, 0x0C);
                
                // Based on analysis:
                // value1 (0x04) = decompressed size
                // value2 (0x08) = compressed data size
                // value3 (0x0C) = unknown (checksum?)
                // Offset 0x10 = tree size
                
                int decompressedSize = (int)value1;  // Fixed: was value2
                int compressedSize = (int)value2;    // Fixed: was value1
                int treeSize = (int)BitConverter.ToUInt32(compressedData, 0x10);
                
                // Sanity checks
                if (decompressedSize <= 0 || decompressedSize > 10 * 1024 * 1024)
                    return new DecompressionResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Suspicious decompressed size: {decompressedSize}" 
                    };
                
                if (compressedSize <= 0 || compressedSize > compressedData.Length)
                    return new DecompressionResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Invalid compressed size: {compressedSize}" 
                    };

                if (treeSize < 256 || treeSize > compressedData.Length / 2)
                    return new DecompressionResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Suspicious tree size: {treeSize}" 
                    };

                // Build Huffman tree from frequency table at offset 0x14
                const int TREE_START = 0x14;
                if (TREE_START + treeSize > compressedData.Length)
                    return new DecompressionResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"Tree data exceeds file bounds" 
                    };

                HuffmanNode root = BuildTree(compressedData.AsSpan(TREE_START, treeSize));
                
                // Compressed data starts after tree
                int dataStart = TREE_START + treeSize;
                if (dataStart >= compressedData.Length)
                    return new DecompressionResult 
                    { 
                        Success = false, 
                        ErrorMessage = $"No compressed data after tree" 
                    };

                // Decompress bit stream
                byte[] decompressed = DecompressBitStream(
                    root, 
                    compressedData.AsSpan(dataStart), 
                    decompressedSize
                );
                
                return new DecompressionResult
                {
                    Success = true,
                    CompressedSize = compressedData.Length,
                    DecompressedSize = decompressed.Length,
                    Data = decompressed
                };
            }
            catch (Exception ex)
            {
                return new DecompressionResult 
                { 
                    Success = false, 
                    ErrorMessage = $"Exception: {ex.Message}" 
                };
            }
        }

        /// <summary>
        /// Build Huffman tree from frequency table
        /// </summary>
        /// <param name="treeData">Frequency table data (variable length)</param>
        /// <returns>Root node of Huffman tree</returns>
        private static HuffmanNode BuildTreeFromFrequencies(ReadOnlySpan<byte> treeData)
        {
            // RAGE engine uses VARIABLE-LENGTH frequency table
            // Format appears to be: [uint32 count] followed by [count] uint32 frequency values
            // Corresponding to bytes 0..count-1
            
            if (treeData.Length < 4)
                throw new InvalidDataException($"Tree data too small: {treeData.Length} bytes");

            // Try interpreting as contiguous uint32 values
            int numEntries = treeData.Length / 4;
            var frequencies = new Dictionary<byte, uint>();
            
            for (int i = 0; i < numEntries; i++)
            {
                uint freq = BitConverter.ToUInt32(treeData.Slice(i * 4, 4));
                if (freq > 0 && i < 256)  // Only valid byte values
                {
                    frequencies[(byte)i] = freq;
                }
            }

            if (frequencies.Count == 0)
                throw new InvalidDataException($"No symbols in frequency table (parsed {numEntries} entries)");

            // Build Huffman tree using priority queue
            var priorityQueue = new PriorityQueue<HuffmanNode, uint>();
            
            foreach (var kvp in frequencies)
            {
                var node = new HuffmanNode { Value = kvp.Key };
                priorityQueue.Enqueue(node, kvp.Value);
            }

            // Build tree bottom-up - standard Huffman algorithm
            var nodeFrequencies = new Dictionary<HuffmanNode, uint>();
            foreach (var kvp in frequencies)
            {
                var node = new HuffmanNode { Value = kvp.Key };
                nodeFrequencies[node] = kvp.Value;
            }

            while (priorityQueue.Count > 1)
            {
                var left = priorityQueue.Dequeue();
                var right = priorityQueue.Dequeue();

                var parent = new HuffmanNode
                {
                    Left = left,
                    Right = right
                };

                // Combined frequency for parent
                uint leftFreq = nodeFrequencies.ContainsKey(left) ? nodeFrequencies[left] : 1;
                uint rightFreq = nodeFrequencies.ContainsKey(right) ? nodeFrequencies[right] : 1;
                uint combinedFreq = leftFreq + rightFreq;
                
                nodeFrequencies[parent] = combinedFreq;
                priorityQueue.Enqueue(parent, combinedFreq);
            }

            return priorityQueue.Dequeue();
        }

        /// <summary>
        /// Build Huffman tree from frequency table or encoded tree data
        /// </summary>
        /// <param name="treeData">Encoded tree structure from file header</param>
        /// <returns>Root node of Huffman tree</returns>
        private static HuffmanNode BuildTree(ReadOnlySpan<byte> treeData)
        {
            // Try frequency table format first (most common)
            try
            {
                return BuildTreeFromFrequencies(treeData);
            }
            catch (Exception ex)
            {
                // TODO: Try other formats if frequency table fails
                throw new InvalidDataException($"Tree building failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decompress bit stream using Huffman tree
        /// </summary>
        /// <param name="root">Root of Huffman tree</param>
        /// <param name="compressedBits">Compressed bit stream</param>
        /// <param name="outputSize">Expected size of decompressed data</param>
        /// <returns>Decompressed bytes</returns>
        private static byte[] DecompressBitStream(HuffmanNode root, ReadOnlySpan<byte> compressedBits, int outputSize)
        {
            byte[] output = new byte[outputSize];
            int outputIndex = 0;
            HuffmanNode? current = root;
            
            // Process each bit - try LSB first (bit 0 = rightmost bit)
            for (int byteIndex = 0; byteIndex < compressedBits.Length && outputIndex < outputSize; byteIndex++)
            {
                byte currentByte = compressedBits[byteIndex];
                
                for (int bitIndex = 0; bitIndex < 8 && outputIndex < outputSize; bitIndex++)
                {
                    // Read bit LSB first (bit 0 to bit 7)
                    bool bit = (currentByte & (1 << bitIndex)) != 0;
                    
                    // Traverse tree: 0 = left, 1 = right
                    current = bit ? current?.Right : current?.Left;
                    
                    if (current == null)
                        throw new InvalidDataException($"Invalid Huffman code at byte {byteIndex}, bit {bitIndex}");
                    
                    // Found leaf node - emit symbol
                    if (current.IsLeaf && current.Value.HasValue)
                    {
                        output[outputIndex++] = current.Value.Value;
                        current = root; // Reset to root for next symbol
                    }
                }
            }
            
            if (outputIndex != outputSize)
            {
                // Allow partial decompression for debugging
                Array.Resize(ref output, outputIndex);
                return output;
            }
            
            return output;
        }
    }
}
