using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace WoWViewer.Parsers
{
    /// <summary>
  /// Factory for creating appropriate OJD parsers based on file type.
    /// </summary>
    public static class OjdParserFactory
    {
        /// <summary>
        /// Automatically detects and parses the appropriate OJD file type.
        /// </summary>
      /// <param name="filePath">Path to the OJD file.</param>
      /// <returns>Generic object containing parsed results (cast to appropriate type).</returns>
        public static object ParseAuto(string filePath)
 {
      string fileName = Path.GetFileName(filePath).ToUpperInvariant();

      return fileName switch
   {
         "TEXT.OJD" => TextOjdParser.Parse(filePath),
          "SFX.OJD" => SfxOjdParser.Parse(filePath),
       "OBJ.OJD" => ObjOjdParser.Parse(filePath),
         _ => throw new NotSupportedException($"Unknown OJD file type: {fileName}")
            };
        }

        /// <summary>
        /// Asynchronously parses an OJD file.
        /// </summary>
  public static async Task<object> ParseAutoAsync(string filePath)
        {
   return await Task.Run(() => ParseAuto(filePath));
        }

        /// <summary>
        /// Gets the appropriate parser type for a given file.
        /// </summary>
        public static Type GetParserType(string filePath)
   {
      string fileName = Path.GetFileName(filePath).ToUpperInvariant();

            return fileName switch
    {
     "TEXT.OJD" => typeof(TextOjdParser),
            "SFX.OJD" => typeof(SfxOjdParser),
      "OBJ.OJD" => typeof(ObjOjdParser),
         _ => throw new NotSupportedException($"Unknown OJD file type: {fileName}")
      };
     }
    }

    /// <summary>
    /// Extension methods for OJD parsing operations.
    /// </summary>
  public static class OjdParserExtensions
    {
        /// <summary>
        /// Exports entries to a log file with custom formatting.
        /// </summary>
        public static void ExportToLog<T>(this IEnumerable<T> entries, string outputPath)
     {
            using var writer = new StreamWriter(outputPath, false, System.Text.Encoding.UTF8);
     int count = 0;

            foreach (var entry in entries)
            {
                writer.WriteLine(entry?.ToString() ?? "(null)");
      count++;
         }

            writer.WriteLine();
            writer.WriteLine($"Total entries: {count}");
        }

   /// <summary>
        /// Filters OjdEntry list by ID range.
        /// </summary>
        public static IEnumerable<OjdEntry> FilterByIdRange(this IEnumerable<OjdEntry> entries, ushort minId, ushort maxId)
        {
            foreach (var entry in entries)
            {
   if (entry.Id >= minId && entry.Id <= maxId)
 yield return entry;
            }
        }

        /// <summary>
        /// Filters TextOjdEntry list by faction.
    /// </summary>
        public static IEnumerable<TextOjdEntry> FilterByFaction(this IEnumerable<TextOjdEntry> entries, FactionType faction)
 {
      foreach (var entry in entries)
   {
  if (entry.Faction == faction)
    yield return entry;
   }
      }

        /// <summary>
  /// Gets only verified string entries from SFX parsing results.
     /// </summary>
        public static IEnumerable<SfxOjdEntry> GetVerifiedEntries(this IEnumerable<SfxOjdEntry> entries)
        {
         foreach (var entry in entries)
            {
       if (entry.Type == SfxEntryType.StringEntry)
         yield return entry;
            }
      }
    }
}
