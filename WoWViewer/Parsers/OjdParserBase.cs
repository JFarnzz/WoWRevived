using System;
using System.Text;

namespace WoWViewer.Parsers
{
    /// <summary>
    /// Base class for OJD file parsers with common functionality.
    /// </summary>
    public abstract class OjdParserBase
    {
  protected const byte ENTRY_MARKER = 0xFF;
        protected const int HEADER_SIZE = 7;
        
        /// <summary>
/// Validates if a byte represents a printable ASCII character.
        /// </summary>
        protected static bool IsAsciiChar(byte b) => b >= 0x20 && b <= 0x7E;

     /// <summary>
        /// Safely reads a UInt16 from byte array at specified offset.
/// </summary>
  protected static bool TryReadUInt16(ReadOnlySpan<byte> data, int offset, out ushort value)
        {
  value = 0;
     if (offset + 1 >= data.Length)
  return false;

            value = BitConverter.ToUInt16(data.Slice(offset, 2));
            return true;
        }

        /// <summary>
        /// Safely extracts an ASCII string from byte array.
        /// </summary>
        protected static string SafeGetString(ReadOnlySpan<byte> data, int offset, int length)
        {
            if (offset < 0 || length <= 0 || offset + length > data.Length)
          return string.Empty;

   return Encoding.ASCII.GetString(data.Slice(offset, length));
    }

        /// <summary>
        /// Validates that the file exists and is accessible.
    /// </summary>
   protected static void ValidateFile(string filePath)
        {
 if (string.IsNullOrWhiteSpace(filePath))
             throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

          if (!File.Exists(filePath))
            throw new FileNotFoundException($"OJD file not found: {filePath}", filePath);
        }
    }
}
