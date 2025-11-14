using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WoWViewer.Parsers
{
    /// <summary>
    /// Faction type for TEXT.ojd entries.
    /// </summary>
    public enum FactionType : byte
    {
Martian = 0x00,
    Human = 0x01,
        UI = 0x02,
        Unknown = 0xFF
    }

    /// <summary>
    /// Represents a parsed TEXT.ojd entry.
    /// </summary>
    public class TextOjdEntry
    {
 public int Offset { get; set; }
        public ushort LookupId { get; set; }
        public FactionType Faction { get; set; }
        public ushort PurposeId { get; set; }
        public ushort Length { get; set; }
        public string Text { get; set; } = string.Empty;

        public override string ToString()
  {
         return $"Offset: [{Offset:X}] LookupID: [{LookupId:D}] Faction: [{Faction}] PurposeID: {PurposeId:D} Length: [{Length:D}] Text: {Text}";
        }
    }

  /// <summary>
    /// Parser for TEXT.ojd files containing game text strings.
    /// Based on format: FF [LookupID:2] [FactionID:2] [PurposeID:2] [Length:2] [String+Null]
    /// </summary>
    public class TextOjdParser : OjdParserBase
    {
        private const int TEXT_ENTRY_START_OFFSET = 0x289;
        private const int KNOWN_ENTRY_COUNT = 1396;
private const int TEXT_HEADER_SIZE = 9; // FF + 2+2+2+2 bytes

        /// <summary>
   /// Parses a TEXT.ojd file with known structure (1396 entries starting at 0x289).
        /// </summary>
 /// <param name="filePath">Path to TEXT.ojd file.</param>
     /// <returns>List of parsed text entries.</returns>
        public static List<TextOjdEntry> Parse(string filePath)
        {
   ValidateFile(filePath);

      ReadOnlySpan<byte> data = File.ReadAllBytes(filePath);
   var entries = new List<TextOjdEntry>(KNOWN_ENTRY_COUNT);
            int offset = TEXT_ENTRY_START_OFFSET;

          for (int i = 0; i < KNOWN_ENTRY_COUNT && offset < data.Length; i++)
  {
                if (!TryParseTextEntry(data, ref offset, out var entry))
        {
             // Log warning but continue - malformed entry
          continue;
  }

      entries.Add(entry);
   }

            return entries;
        }

        /// <summary>
        /// Parses TEXT.ojd and exports to a formatted log file.
        /// </summary>
        public static void ParseToLog(string filePath, string? outputPath = null)
        {
     var entries = Parse(filePath);
      outputPath ??= Path.ChangeExtension(filePath, "-log.txt");

  using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
       
 foreach (var entry in entries)
            {
           writer.WriteLine(entry.ToString());
            }

         writer.WriteLine();
        writer.WriteLine($"Total valid entries: {entries.Count}");
        }

        private static bool TryParseTextEntry(ReadOnlySpan<byte> data, ref int offset, out TextOjdEntry entry)
        {
     entry = new TextOjdEntry { Offset = offset };

     // Check for entry marker (though TEXT.ojd might not always have it)
   // Based on format: FF is at offset, then +2 lookupID, +4 faction, +6 purposeID, +8 length
  int baseOffset = offset;
if (data[baseOffset] == ENTRY_MARKER)
 baseOffset++; // Skip marker if present

            // Ensure enough data for header
            if (baseOffset + 8 >= data.Length)
           return false;

         // Read header fields
     if (!TryReadUInt16(data, baseOffset, out ushort lookupId))
  return false;

      byte factionByte = data[baseOffset + 2];
            FactionType faction = factionByte switch
      {
0x00 => FactionType.Martian,
           0x01 => FactionType.Human,
0x02 => FactionType.UI,
   _ => FactionType.Unknown
            };

      if (!TryReadUInt16(data, baseOffset + 4, out ushort purposeId))
                return false;

        if (!TryReadUInt16(data, baseOffset + 6, out ushort length))
     return false;

    // Validate length
            int stringOffset = baseOffset + 8;
  if (length == 0 || stringOffset + length > data.Length)
         return false;

        // Extract string (length includes null terminator)
            string text = SafeGetString(data, stringOffset, length - 1);

     // Populate entry
     entry.LookupId = lookupId;
    entry.Faction = faction;
 entry.PurposeId = purposeId;
          entry.Length = length;
            entry.Text = text;

        // Move offset to next entry
         offset += length + TEXT_HEADER_SIZE - 1; // -1 because length includes null

       return true;
        }

        /// <summary>
        /// Gets faction type as a friendly string.
    /// </summary>
        public static string GetFactionName(FactionType faction)
        {
         return faction switch
            {
FactionType.Martian => "Martian",
         FactionType.Human => "Human",
         FactionType.UI => "UI",
                _ => "Unknown"
          };
        }
    }
}
