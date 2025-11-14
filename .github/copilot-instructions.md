# WoW Revived - AI Coding Agent Instructions

## Project Overview
A toolkit for modding/preserving Jeff Wayne's "The War of the Worlds" (RAGE engine game). Two main C# WinForms applications: **WoWLauncher** (game launcher with registry management) and **WoWViewer** (toolkit for extracting/editing game files).

## Architecture

### Two-App Structure
- **WoWLauncher/**: Game launcher with registry settings management, custom keyboard shortcuts, and admin elevation
- **WoWViewer/**: Multi-tool application (File Extractor, Save Editor, Text Editor, Map Editor)
  - Uses Form-based UI pattern where main form (`Form1.cs`/`WoWViewer`) launches specialized editors as separate forms
  - Single instance enforcement in both apps (checks `Process.GetProcessesByName`)

### Key File Formats
- **`.wow`** - Archive files containing compressed game assets:
  - `DAT/Dat.wow` (25.3 MB) - Game data, sprites, palettes, fonts
  - `MAPS/MAPS.WoW` (116.2 MB) - Map terrain and object data
  - `SFX/sfx.wow` (35.9 MB) - Sound effects
  - `VOX/human.wow` (30.8 MB) / `VOX/martian.wow` (39.3 MB) - Voice acting audio
- **`.ojd`** - Structured binary data files (found in game root):
  - `TEXT.ojd` (63,833 bytes) - 1396 game strings starting at offset `0x289`, format: `FF [LookupID:2] [FactionID:2] [PurposeID:2] [Length:2] [String+Null]`
  - `OBJ.ojd` (129,839 bytes) - Game object definitions (buildings, units, structures)
  - `SFX.ojd` (16,900 bytes) - Sound effect reference IDs
  - `AI.ojd` (53,844 bytes) - AI behavior definitions
- **`.nsb`** - Map files (30 files in `DAT/` folder, numbered 1-30 for each sector):
  - Format: 12-byte header (reserved int32, "BMOL" magic, entry count int32)
  - Entries: 12 bytes each (6x uint16 fields - likely coordinates, object IDs, properties)
  - Parsed by `NsbParser` in `Parsers/` directory
- **`.spr`** - Run-length encoded grayscale sprite images (stored in .wow archives)
- **`.smk`** - Smacker video files in `FMV/` folder (intro, outro, rage logo, title screens)
- **Save files** - Binary format with name/date/time at known offsets (see `SaveGame/SaveFileStructure.cs`)
- **Game executables**: `WoW.exe` (1.07 MB - main game), `WOWStart.exe` (304 KB - launcher/CD check)

### Parser Architecture (`WoWViewer/Parsers/`)
Factory pattern with specialized parsers:
- `OjdParserBase` - Shared utilities (span-based parsing, validation)
- `OjdParserFactory` - Auto-detects file type by name and returns appropriate parser
- `TextOjdParser`, `ObjOjdParser`, `SfxOjdParser` - Type-specific implementations
- `NsbParser` - Map file parser ("BMOL" magic, 12-byte header + 12-byte entries)

**Parser Pattern:**
```csharp
ReadOnlySpan<byte> data = File.ReadAllBytes(filePath);  // Zero-allocation parsing
if (!TryParseEntry(data, ref offset, out var entry)) { continue; }
```

### Data Models (`WoWViewer/Class1.cs`)
- `WowFileEntry` - Archive file entries (Name, Length, Offset, Data)
- `WowTextEntry` - TEXT.ojd entries with Faction/Index tracking
- `OjdEntry` - Generic OJD entry (Id, Type, Length, Name)

## Critical Implementation Patterns

### Registry Management (WoWLauncher)
Game requires registry keys under `HKLM\SOFTWARE\WOW6432Node\Rage\Jeff Wayne's 'The War Of The Worlds'\1.00.000`:
- Keys: `Screen`, `BattleMap`, `Research`, `Tweak`
- **Always** use `RegistryView.Registry32` for 32-bit game compatibility
- Launcher auto-updates `CD Path` and `Install Path` to current directory
- Settings pattern: compare before write using `registryCompare()` helper

### Binary File Editing
Use `BinaryUtility.ReplaceByte()` for patching game executable:
```csharp
var patches = new List<(long Offset, byte Value)> { (0x1234, 0xAB), ... };
BinaryUtility.ReplaceByte(patches, "game.exe");
```

### Text Encoding
TEXT.ojd supports **UTF-8 AND ISO-8859-1 (Latin-1)**. Always:
```csharp
Encoding Latin1 = Encoding.GetEncoding("ISO-8859-1");
```
Convert `\n` → `\r\n` when loading, reverse when saving (Windows newline handling).

### File Handler Pattern (WoWViewer)
Dictionary-based dispatch in `Form1.cs`:
```csharp
handlers = new Dictionary<string, Action<WowFileEntry>> {
    { "SPR", HandleSPR }, { "PAL", HandlePalette }, ...
};
```
Add new handlers for file types here.

## Development Workflows

### Building
- **Framework:** .NET 8 Windows Forms
- **Solution files:** `WoWLauncher/WoWLauncher.sln`, `WoWViewer/WoWViewer.sln`
- Build: `dotnet build` in respective directories
- Publish: `dotnet publish -c Release` (profiles in `Properties/PublishProfiles/`)

### Testing Game Interaction
Game expects this directory structure in install folder:
```
WoW.exe, WOWStart.exe          # Main executables
TEXT.ojd, OBJ.ojd, SFX.ojd, AI.ojd  # Core data files (root)
Human.000, Martian.000         # Faction-specific data
DAT/
  ├── Dat.wow                  # Main asset archive
  ├── 1.nsb through 30.nsb     # 30 sector map files
SFX/sfx.wow                    # Sound effects archive
VOX/
  ├── human.wow                # Human faction voice
  └── martian.wow              # Martian faction voice
FMV/                           # Video files (on CD, accessed at runtime)
MAPS/MAPS.WoW                  # Map data (on CD, accessed at runtime)
```
- Custom language packs named like `TEXT-German.ojd` go in root directory
- Game uses CD check via `ENGLISH.CD` / `MARTIAN.CD` marker files

### Adding New OJD Parser
1. Create parser class in `Parsers/` inheriting `OjdParserBase`
2. Add to `OjdParserFactory` switch cases
3. Use `ReadOnlySpan<byte>` for performance
4. Implement `const` markers for magic numbers/offsets
5. Add XML docs for public methods

## Project-Specific Conventions

### Constants Over Magic Numbers
```csharp
private const byte ENTRY_MARKER = 0xFF;
private const int TEXT_ENTRY_START_OFFSET = 0x289;
```

### Error Handling
- **UI Code:** Always show `MessageBox` with context (never silent failures)
- **Parsers:** Validate file existence, return empty/null on malformed data (don't throw)
- **File Operations:** Wrap in `try-catch`, provide user-friendly messages

### Naming
- PascalCase for public methods: `ParseTextOjd()` not `parseTEXTOJD()`
- Descriptive variable names: `strStart`, `strEnd`, not `x`, `y`
- File extension checks: `.ToLowerInvariant()` for case-insensitive comparison

## Integration Points

### Decomp Work (WoWDecomp/ida-notes-thor/)
IDA Pro analysis notes for future executable patching:
- `ida-map.txt` - Virtual key address mappings
- `text-ojd.txt`, `obj-ojd.txt`, `sfx-ojd.txt` - File format documentation
- Reference when implementing keyboard shortcut patching or game hooks

### Language Pack Detection
Launcher scans for `*.OJD` files, ignores `TEXT/AI/OBJ/SFX.ojd`, adds rest as language options.

## Current Work Areas

### High Priority
- **Save Editor** (10% → 80%): Resource editing, building/unit inventory, sector control
  - See `SaveGame/IMPLEMENTATION_PLAN.md` for detailed roadmap
  - Structure docs in `SaveFileStructure.cs`
  - Note: Game has 30 sectors (not 31 as initially estimated), one .nsb file per sector
- **Map Editor** (1% → 40%): NSB file parsing complete, basic visualization implemented
  - `NsbParser` fully parses "BMOL" format with 12-byte entries
  - `MapEditorForm` displays objects as colored dots scaled to fit viewport
  - Next: Identify field meanings (coordinates, object types, properties)
  - Next: Add terrain rendering, building/unit identification from OBJ.ojd

### Future Goals
- No-CD music fix (mini-ISO mounting)
- Video intercept (Smacker → Bink upgrade)
- Full executable decomp/recompilation

## Reference Files
- `README.md` - Feature completion status, installation guide
- `ROADMAP_IMPLEMENTATION.md` - Detailed implementation priorities
- `Parsers/IMPROVEMENTS.md` - Parser architecture rationale
- `WoWDecomp/ida-notes-thor/` - Reverse engineering documentation
