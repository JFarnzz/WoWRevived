using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WoWViewer.Parsers
{
    /// <summary>
    /// Parser for .nsb (sector map) files.
    /// Format: Header (12 bytes) + Array of MapObject entries (12 bytes each)
    /// </summary>
    public class NsbParser : OjdParserBase
    {
        private const uint NSB_MAGIC = 0x4C4F4D42; // "BMOL" in little-endian
        private new const int HEADER_SIZE = 12;
        private const int ENTRY_SIZE = 12;

        /// <summary>
        /// Parses an NSB map file.
        /// </summary>
        /// <param name="filePath">Path to the .nsb file.</param>
        /// <returns>Parsed map data.</returns>
        public static NsbMapData Parse(string filePath)
        {
            ValidateFile(filePath);

            ReadOnlySpan<byte> data = File.ReadAllBytes(filePath);
            
            if (data.Length < HEADER_SIZE)
                throw new InvalidDataException($"NSB file too small: {data.Length} bytes");

            var mapData = new NsbMapData();
            
            // Parse header
            int reserved = BitConverter.ToInt32(data.Slice(0, 4));
            uint magic = BitConverter.ToUInt32(data.Slice(4, 4));
            int entryCount = BitConverter.ToInt32(data.Slice(8, 4));

            if (magic != NSB_MAGIC)
                throw new InvalidDataException($"Invalid NSB magic: 0x{magic:X8} (expected 0x{NSB_MAGIC:X8})");

            mapData.EntryCount = entryCount;

            // Parse entries
            int offset = HEADER_SIZE;
            for (int i = 0; i < entryCount && offset + ENTRY_SIZE <= data.Length; i++)
            {
                var entry = new NsbMapObject
                {
                    Index = i,
                    Field0 = BitConverter.ToUInt16(data.Slice(offset + 0, 2)),
                    Field1 = BitConverter.ToUInt16(data.Slice(offset + 2, 2)),
                    Field2 = BitConverter.ToUInt16(data.Slice(offset + 4, 2)),
                    Field3 = BitConverter.ToUInt16(data.Slice(offset + 6, 2)),
                    Field4 = BitConverter.ToUInt16(data.Slice(offset + 8, 2)),
                    Field5 = BitConverter.ToUInt16(data.Slice(offset + 10, 2))
                };

                mapData.Objects.Add(entry);
                offset += ENTRY_SIZE;
            }

            return mapData;
        }

        /// <summary>
        /// Exports NSB data to a readable log file.
        /// </summary>
        public static void ExportToLog(string inputPath, string? outputPath = null)
        {
            var mapData = Parse(inputPath);
            outputPath ??= Path.ChangeExtension(inputPath, ".nsb.txt");

            // Try to load OBJ.ojd for object names
            var objNames = new Dictionary<ushort, string>();
            try
            {
                string[] objPaths = { "OBJ.ojd", "..\\OBJ.ojd" };
                foreach (var path in objPaths)
                {
                    if (File.Exists(path))
                    {
                        var entries = ObjOjdParser.Parse(path);
                        foreach (var entry in entries)
                        {
                            if (!objNames.ContainsKey(entry.Id))
                                objNames[entry.Id] = entry.Name;
                        }
                        break;
                    }
                }
            }
            catch { /* Ignore if OBJ.ojd not found */ }

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
            
            writer.WriteLine($"NSB Map File: {Path.GetFileName(inputPath)}");
            writer.WriteLine($"Total Objects: {mapData.EntryCount}");
            writer.WriteLine();
            writer.WriteLine("Index | Field0 | Field1 | Field2 | Field3 | Field4 | Field5 | Object Name");
            writer.WriteLine("------|--------|--------|--------|--------|--------|--------|-------------");

            foreach (var obj in mapData.Objects)
            {
                string objName = objNames.TryGetValue(obj.Field4, out string? name) ? name : "Unknown";
                writer.WriteLine($"{obj.Index,5} | {obj.Field0,6} | {obj.Field1,6} | {obj.Field2,6} | {obj.Field3,6} | {obj.Field4,6} | {obj.Field5,6} | {objName}");
            }
        }

        /// <summary>
        /// Batch export all NSB files in a directory.
        /// </summary>
        public static void ExportAllInDirectory(string directory)
        {
            var nsbFiles = Directory.GetFiles(directory, "*.nsb");
            
            foreach (var file in nsbFiles)
            {
                try
                {
                    ExportToLog(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to parse {Path.GetFileName(file)}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Represents a complete NSB map file.
    /// </summary>
    public class NsbMapData
    {
        public int EntryCount { get; set; }
        public List<NsbMapObject> Objects { get; set; } = new List<NsbMapObject>();
    }

    /// <summary>
    /// Represents a single map object entry (12 bytes).
    /// Field meanings are currently unknown and require reverse engineering.
    /// Likely candidates: X/Y coordinates, object type ID, rotation, scale, etc.
    /// </summary>
    public class NsbMapObject
    {
        public int Index { get; set; }
        public ushort Field0 { get; set; }  // Possibly X coordinate or object type
        public ushort Field1 { get; set; }  // Possibly flags or layer
        public ushort Field2 { get; set; }  // Possibly Y coordinate
        public ushort Field3 { get; set; }  // Possibly rotation or state
        public ushort Field4 { get; set; }  // Possibly object ID or variant
        public ushort Field5 { get; set; }  // Possibly Z-order or properties

        public override string ToString()
        {
            return $"[{Index,4}] ({Field0}, {Field1}) -> ({Field2}, {Field3}) | ID: {Field4} | Flags: {Field5}";
        }
    }
}
