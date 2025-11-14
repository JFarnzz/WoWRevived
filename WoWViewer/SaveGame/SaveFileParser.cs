using System;
using System.IO;
using System.Text;

namespace WoWViewer.SaveGame
{
    /// <summary>
    /// Parser for War of the Worlds save game files.
    /// </summary>
    public static class SaveFileParser
    {
  /// <summary>
   /// Parse a complete save file.
   /// </summary>
        public static SaveFileData ParseSaveFile(string filePath)
        {
   if (!File.Exists(filePath))
        throw new FileNotFoundException($"Save file not found: {filePath}");

         var saveData = new SaveFileData();
         
      using (var br = new BinaryReader(File.OpenRead(filePath)))
{
         // Parse basic information
    ParseBasicInfo(br, saveData);
    
   // Parse resources (if structure is known)
   ParseResources(br, saveData);
     
        // Parse faction
 ParseFaction(br, saveData);
       
    // Parse sector data (requires more reverse engineering)
   // ParseSectorData(br, saveData);
        }
            
    return saveData;
     }

    /// <summary>
        /// Parse basic save information (name, date, time).
 /// </summary>
        private static void ParseBasicInfo(BinaryReader br, SaveFileData saveData)
{
            // Read save name
     br.BaseStream.Seek(SaveFileStructure.NAME_OFFSET, SeekOrigin.Begin);
       byte[] nameBytes = br.ReadBytes(SaveFileStructure.NAME_LENGTH);
          saveData.SaveName = Encoding.ASCII.GetString(nameBytes).Split('\0')[0];

     // Read time
            br.BaseStream.Seek(SaveFileStructure.TIME_OFFSET, SeekOrigin.Begin);
     float tickFloat = br.ReadSingle();
            float totalHours = tickFloat / SaveFileStructure.TIME_TICK_DIVISOR;
            int hours = (int)totalHours;
   float fractionalHour = totalHours - hours;
   int minutes = (int)(fractionalHour * 60);
            int seconds = (int)((fractionalHour * 60 - minutes) * 60);

  // Read date
        br.BaseStream.Seek(SaveFileStructure.DATE_OFFSET, SeekOrigin.Begin);
            ushort day = (ushort)(br.ReadUInt16() + 1);    // Zero-based to one-based
  ushort month = (ushort)(br.ReadUInt16() + 1);  // Zero-based to one-based
            ushort year = br.ReadUInt16();

 // Handle years below 1753 (DateTime limitation)
   if (year < 1753)
       {
         saveData.ActualYear = year;
             year = 1753;
     }

          saveData.SaveDateTime = new DateTime(year, month, day, hours, minutes, seconds);
        }

        /// <summary>
 /// Parse faction type.
   /// </summary>
        private static void ParseFaction(BinaryReader br, SaveFileData saveData)
        {
 br.BaseStream.Seek(SaveFileStructure.FACTION_OFFSET, SeekOrigin.Begin);
            byte factionByte = br.ReadByte();
        
    saveData.Faction = factionByte switch
            {
             SaveFileStructure.FACTION_HUMAN => FactionType.Human,
             SaveFileStructure.FACTION_MARTIAN => FactionType.Martian,
    _ => FactionType.Human // Default
         };
    }

     /// <summary>
        /// Parse resource values.
        /// NOTE: Offsets are tentative and need verification with actual save files.
        /// </summary>
        private static void ParseResources(BinaryReader br, SaveFileData saveData)
        {
          try
       {
          br.BaseStream.Seek(SaveFileStructure.RESOURCE1_OFFSET, SeekOrigin.Begin);
        saveData.Resource1 = br.ReadInt32();
  
     br.BaseStream.Seek(SaveFileStructure.RESOURCE2_OFFSET, SeekOrigin.Begin);
                saveData.Resource2 = br.ReadInt32();
          
     br.BaseStream.Seek(SaveFileStructure.RESOURCE3_OFFSET, SeekOrigin.Begin);
     saveData.Resource3 = br.ReadInt32();
        }
   catch
            {
   // If resource parsing fails, set to zero
       saveData.Resource1 = 0;
         saveData.Resource2 = 0;
   saveData.Resource3 = 0;
    }
        }

        /// <summary>
     /// Parse sector data with buildings and units.
 /// NOTE: This requires extensive reverse engineering of the save file format.
        /// </summary>
        private static void ParseSectorData(BinaryReader br, SaveFileData saveData)
        {
    // TODO: Implement when save file structure is fully documented
         // This will require:
            // 1. Identifying sector data block locations
      // 2. Parsing building arrays per sector
   // 3. Parsing unit arrays per sector
            // 4. Mapping building/unit IDs to names using OBJ.ojd and TEXT.ojd
 }

 /// <summary>
        /// Write save file data back to disk.
        /// </summary>
  public static void WriteSaveFile(string filePath, SaveFileData saveData)
{
   if (!File.Exists(filePath))
                throw new FileNotFoundException($"Save file not found: {filePath}");

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
    using (var bw = new BinaryWriter(fs))
      {
         WriteBasicInfo(bw, saveData);
     WriteResources(bw, saveData);
}
        }

private static void WriteBasicInfo(BinaryWriter bw, SaveFileData saveData)
{
  // Write name
            bw.BaseStream.Seek(SaveFileStructure.NAME_OFFSET, SeekOrigin.Begin);
   byte[] nameBytes = new byte[SaveFileStructure.NAME_LENGTH];
            byte[] newNameBytes = Encoding.ASCII.GetBytes(saveData.SaveName);
 Array.Copy(newNameBytes, nameBytes, Math.Min(SaveFileStructure.NAME_LENGTH, newNameBytes.Length));
    bw.Write(nameBytes);

            // Write time
       DateTime dt = saveData.SaveDateTime;
      float totalHours = dt.Hour + (dt.Minute / 60f) + (dt.Second / 3600f);
       float tickFloat = totalHours * SaveFileStructure.TIME_TICK_DIVISOR;
   bw.BaseStream.Seek(SaveFileStructure.TIME_OFFSET, SeekOrigin.Begin);
   bw.Write(tickFloat);

            // Write date
            ushort day = (ushort)(dt.Day - 1);    // One-based to zero-based
ushort month = (ushort)(dt.Month - 1); // One-based to zero-based
            ushort year = saveData.ActualYear > 0 ? saveData.ActualYear : (ushort)dt.Year;

            bw.BaseStream.Seek(SaveFileStructure.DATE_OFFSET, SeekOrigin.Begin);
            bw.Write(day);
        bw.Write(month);
     bw.Write(year);
        }

        private static void WriteResources(BinaryWriter bw, SaveFileData saveData)
        {
    try
    {
    bw.BaseStream.Seek(SaveFileStructure.RESOURCE1_OFFSET, SeekOrigin.Begin);
            bw.Write(saveData.Resource1);
                
         bw.BaseStream.Seek(SaveFileStructure.RESOURCE2_OFFSET, SeekOrigin.Begin);
  bw.Write(saveData.Resource2);
    
     bw.BaseStream.Seek(SaveFileStructure.RESOURCE3_OFFSET, SeekOrigin.Begin);
         bw.Write(saveData.Resource3);
         }
       catch
{
     // Silently fail if resources can't be written
         }
        }
    }
}
