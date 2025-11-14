using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WoWViewer.Parsers
{
    /// <summary>
    /// Represents the result of parsing an SFX.ojd entry.
    /// </summary>
    public class SfxOjdEntry
    {
      public int Index { get; set; }
     public int Offset { get; set; }
        public string HeaderId { get; set; } = "??";
        public int Length { get; set; }
        public SfxEntryType Type { get; set; }
      public string Text { get; set; } = string.Empty;
}

    /// <summary>
    /// Type classification for SFX entries.
    /// </summary>
    public enum SfxEntryType
    {
 Unverified,
     StringEntry,
  MismatchedLength
    }

    /// <summary>
    /// Parser for SFX.ojd files containing sound effect definitions.
    /// </summary>
    public class SfxOjdParser : OjdParserBase
    {
        private const int MIN_STRING_LENGTH = 2;

        /// <summary>
        /// Parses an SFX.ojd file and returns a list of entries.
/// </summary>
        /// <param name="filePath">Path to the SFX.ojd file.</param>
        /// <returns>List of parsed SfxOjdEntry objects.</returns>
        public static List<SfxOjdEntry> Parse(string filePath)
        {
     ValidateFile(filePath);

            ReadOnlySpan<byte> data = File.ReadAllBytes(filePath);
     var entries = new List<SfxOjdEntry>();
   int count = 0;

        for (int i = 0; i < data.Length - 1; i++)
    {
                if (!IsAsciiChar(data[i]) || data[i + 1] == 0x00)
            continue;

                // Find end of potential string
  int start = i;
     int length = 0;

           while (i < data.Length && IsAsciiChar(data[i]))
             {
   i++;
              length++;
    }

         // Validate string candidate
  if (length < MIN_STRING_LENGTH || i >= data.Length || data[i] != 0x00)
      continue;

      // Extract string
       string text = SafeGetString(data, start, length);

      // Try to backtrack to find header
             var entry = AnalyzeEntry(data, start, length, text, count);
    entries.Add(entry);
      count++;
       }

            return entries;
        }

        /// <summary>
        /// Parses SFX file and exports results to CSV.
    /// </summary>
     public static void ParseToCSV(string filePath, string? outputPath = null)
        {
   var entries = Parse(filePath);
     
       outputPath ??= Path.ChangeExtension(filePath, "-dump.csv");

            using var writer = new StreamWriter(outputPath, false, Encoding.UTF8);
    writer.WriteLine("Index,Offset,HeaderID,Length,Type,Text");

       foreach (var entry in entries)
  {
     writer.WriteLine($"{entry.Index},{entry.Offset:X},{entry.HeaderId},{entry.Length},{entry.Type},\"{EscapeCsv(entry.Text)}\"");
   }
        }

        private static SfxOjdEntry AnalyzeEntry(ReadOnlySpan<byte> data, int stringOffset, int length, string text, int index)
  {
            var entry = new SfxOjdEntry
         {
      Index = index,
              Offset = stringOffset,
         Length = length,
                Text = text,
         Type = SfxEntryType.Unverified,
        HeaderId = "??"
            };

         // Try to backtrack to find header
int headerOffset = stringOffset - HEADER_SIZE;
         if (headerOffset < 0 || data[headerOffset] != ENTRY_MARKER)
         return entry;

            if (!TryReadUInt16(data, headerOffset + 1, out ushort id))
           return entry;

    if (!TryReadUInt16(data, headerOffset + 5, out ushort maybeLength))
       return entry;

         entry.HeaderId = id.ToString("X4");

            // Check if length matches (including null terminator)
            if (maybeLength == length + 1)
       {
                entry.Type = SfxEntryType.StringEntry;
 }
            else
            {
  entry.Type = SfxEntryType.MismatchedLength;
            }

            return entry;
    }

        private static string EscapeCsv(string value)
        {
    if (string.IsNullOrEmpty(value))
                return value;

   // Escape quotes
    return value.Replace("\"", "\"\"");
        }
    }
}
