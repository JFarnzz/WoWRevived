using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace WoWViewer.Parsers
{
    /// <summary>
    /// Parser for OBJ.ojd files containing game object definitions.
    /// </summary>
    public class ObjOjdParser : OjdParserBase
    {
        private const int MIN_VALID_LENGTH = 8;
        private const int MAX_VALID_LENGTH = 32;

        /// <summary>
        /// Parses an OBJ.ojd file and returns a list of entries.
        /// </summary>
        /// <param name="filePath">Path to the OBJ.ojd file.</param>
        /// <returns>List of parsed OjdEntry objects.</returns>
        public static List<OjdEntry> Parse(string filePath)
     {
   ValidateFile(filePath);

        var entries = new List<OjdEntry>();
  ReadOnlySpan<byte> data = File.ReadAllBytes(filePath);
      int index = 0;

       while (index < data.Length)
       {
      if (data[index] != ENTRY_MARKER)
         {
    index++;
         continue;
           }

     if (!TryParseEntry(data, ref index, out var entry))
            {
           index++;
     continue;
         }

         entries.Add(entry);
   }

          return entries;
        }

   private static bool TryParseEntry(ReadOnlySpan<byte> data, ref int index, out OjdEntry entry)
        {
            entry = new OjdEntry();

       // Ensure we have enough data for header
  if (index + HEADER_SIZE >= data.Length)
       return false;

// Read header fields
            if (!TryReadUInt16(data, index + 1, out ushort id))
   return false;
            if (!TryReadUInt16(data, index + 3, out ushort type))
        return false;
        if (!TryReadUInt16(data, index + 5, out ushort length))
         return false;

   int strStart = index + HEADER_SIZE;

    // Validation checks
        if (strStart >= data.Length)
return false;

      // Skip if next byte is another marker
  if (data[strStart] == ENTRY_MARKER)
     return false;

 // Validate string start
            if (!IsAsciiChar(data[strStart]))
     return false;

   // Validate length
  if (length < MIN_VALID_LENGTH || length > MAX_VALID_LENGTH || strStart + length > data.Length)
        return false;

     // Find null terminator
     int strEnd = FindNullTerminator(data, strStart, strStart + length);
            if (strEnd == -1)
      return false;

      // Extract and clean string
          string name = SafeGetString(data, strStart, strEnd - strStart);
     name = CleanString(name);

     if (string.IsNullOrWhiteSpace(name))
       return false;

   // Create entry
  entry.Id = id;
entry.Type = type;
 entry.Length = length;
            entry.Name = name;

            index = strEnd;
            return true;
        }

        private static int FindNullTerminator(ReadOnlySpan<byte> data, int start, int maxEnd)
     {
    for (int i = start; i < maxEnd && i < data.Length; i++)
      {
    if (data[i] == 0x00)
           return i;
       }
  return -1;
        }

 private static string CleanString(string input)
{
          if (string.IsNullOrEmpty(input))
           return input;

     // Remove control characters
    var sb = new StringBuilder(input.Length);
 foreach (char c in input)
    {
      if (c >= 0x20 && c <= 0x7E) // Printable ASCII only
 sb.Append(c);
      }
         return sb.ToString().Trim();
    }

        /// <summary>
        /// Creates a cleaned OJD file with injected padding.
        /// </summary>
        /// <param name="inputPath">Source OBJ.ojd file path.</param>
        /// <param name="outputPath">Output file path.</param>
        /// <param name="preserveHeaderBytes">Number of header bytes to preserve.</param>
      public static void WriteCleanedWithPadding(string inputPath, string outputPath, int preserveHeaderBytes = 0x431)
  {
       ValidateFile(inputPath);

byte[] data = File.ReadAllBytes(inputPath);
List<byte> newData = new List<byte>(data.Length + 1000);

            // Preserve header
        newData.AddRange(data.AsSpan(0, Math.Min(preserveHeaderBytes, data.Length)).ToArray());

   int index = preserveHeaderBytes;
   while (index < data.Length)
            {
          if (data[index] != ENTRY_MARKER)
   {
          index++;
    continue;
         }

        if (index + HEADER_SIZE >= data.Length)
    break;

       ushort len = BitConverter.ToUInt16(data, index + 5);
    int strStart = index + HEADER_SIZE;

 if (len == 0 || strStart + len > data.Length)
  {
  index++;
      continue;
     }

         // Copy: FF + 6-byte header
    newData.AddRange(data.AsSpan(index, HEADER_SIZE).ToArray());

    // Copy: ASCII string + null
  newData.AddRange(data.AsSpan(strStart, len).ToArray());

      // Inject: FF 00 (padding)
       newData.Add(ENTRY_MARKER);
      newData.Add(0x00);

  index = strStart + len;
            }

            File.WriteAllBytes(outputPath, newData.ToArray());
        }
    }
}
