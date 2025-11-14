using System;

namespace WoWViewer.SaveGame
{
    /// <summary>
    /// Comprehensive save file structure documentation for War of the Worlds save files.
    /// </summary>
    public static class SaveFileStructure
    {
        // Header offsets
        public const int NAME_OFFSET = 0x0C;
        public const int NAME_LENGTH = 36;
    
        public const int TIME_OFFSET = 0x4C;
     public const int DATE_OFFSET = 0x5A;
  
        // Resource offsets (tentative - requires actual save file analysis)
        public const int RESOURCES_BASE_OFFSET = 0x70;
        public const int RESOURCE1_OFFSET = RESOURCES_BASE_OFFSET;      // Steel/Human Blood
        public const int RESOURCE2_OFFSET = RESOURCES_BASE_OFFSET + 4;  // Coal/Copper
    public const int RESOURCE3_OFFSET = RESOURCES_BASE_OFFSET + 8;  // Oil/Heavy Elements
        
        // Faction indicator
    public const int FACTION_OFFSET = 0x08;
        public const byte FACTION_HUMAN = 0x01;
        public const byte FACTION_MARTIAN = 0x02;
        
        // Sector data
        public const int SECTOR_COUNT = 30; // Verified: 30 .nsb map files (1.nsb through 30.nsb)
        public const int SECTOR_DATA_BASE = 0x100; // Estimated
        public const int SECTOR_DATA_SIZE = 256;   // Estimated per sector
        
        // Building/Unit data (estimated structure)
        public const int MAX_BUILDINGS_PER_SECTOR = 20;
     public const int MAX_UNITS_PER_SECTOR = 50;
        
        // Time conversion constants
        public const float TIME_TICK_DIVISOR = 20.055f;
    }

    /// <summary>
    /// Resource types for each faction.
  /// </summary>
    public enum ResourceType
    {
      // Human resources
  Steel = 0,
        Coal = 1,
        Oil = 2,
        
        // Martian resources
        HumanBlood = 0,
      Copper = 1,
        HeavyElements = 2
 }

    /// <summary>
    /// Faction identifier.
    /// </summary>
    public enum FactionType : byte
    {
        Human = 0x01,
        Martian = 0x02
    }

    /// <summary>
    /// Complete save file data structure.
    /// </summary>
    public class SaveFileData
    {
        public string SaveName { get; set; } = string.Empty;
        public DateTime SaveDateTime { get; set; }
        public ushort ActualYear { get; set; }
        public FactionType Faction { get; set; }
        
// Resources
        public int Resource1 { get; set; } // Steel or Human Blood
        public int Resource2 { get; set; } // Coal or Copper
public int Resource3 { get; set; } // Oil or Heavy Elements
        
        // Sector data
        public SectorData[] Sectors { get; set; } = new SectorData[SaveFileStructure.SECTOR_COUNT];
        
  public SaveFileData()
  {
            for (int i = 0; i < SaveFileStructure.SECTOR_COUNT; i++)
       {
            Sectors[i] = new SectorData { SectorIndex = i };
      }
        }
    }

    /// <summary>
    /// Data for a single sector.
    /// </summary>
    public class SectorData
    {
  public int SectorIndex { get; set; }
        public string SectorName { get; set; } = string.Empty;
        public byte ControlledBy { get; set; } // 0 = Neutral, 1 = Human, 2 = Martian
        public List<BuildingData> Buildings { get; set; } = new List<BuildingData>();
        public List<UnitData> Units { get; set; } = new List<UnitData>();
    }

    /// <summary>
  /// Building data structure.
    /// </summary>
    public class BuildingData
    {
        public ushort BuildingTypeId { get; set; }
        public string BuildingName { get; set; } = string.Empty;
        public byte BuildingLevel { get; set; }
        public ushort Health { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Unit data structure.
    /// </summary>
    public class UnitData
  {
        public ushort UnitTypeId { get; set; }
        public string UnitName { get; set; } = string.Empty;
   public byte UnitLevel { get; set; }
 public ushort Health { get; set; }
        public byte ExperienceLevel { get; set; }
  public bool IsActive { get; set; }
    }
}
