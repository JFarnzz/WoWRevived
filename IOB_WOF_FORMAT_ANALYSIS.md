# IOB/WOF 3D Model Format Analysis

## Overview
This document contains reverse engineering findings for the **IOB** (building models) and **WOF** (unit models) 3D formats used in Jeff Wayne's "The War of the Worlds" (RAGE engine).

**Status:** ✅ **494 models extracted** (349 IOB buildings + 145 WOF units)  
**Analysis:** ✅ **All files analyzed** - see `ModelAnalysis/` for individual hex dumps  
**Magic Bytes:** IOB files use **"FFUH"** (little endian for "HUFF") indicating Huffman compression

---

## File Inventory

### IOB Buildings (349 files)
- **Civilian structures:** FARM, HOUSE1-2, COTTAGE, MANOR, BARN, PUB1-2, CHURCH, CHAPEL, CATH, ABBEY, CASTLE, PARLMENT, SCHOOL, MONUMENT
- **Industrial:** CM_1 (coal mine), OR_MAIN (oil refinery), CF_STOR (coal factory storage), DY_WHOUS (dockyard warehouse), CRANE, WINDMILL, WATERMIL
- **London landmarks:** LONDNOST, LONDST (London Station), LIGHTHSE (lighthouse), PIER, STONECIR (Stonehenge)
- **Military:** CON_* (construction sites), CP_* (checkpoints), EWP_* (electric weapon platforms), SF_* (support facilities)
- **Trees:** TREE1-17, RED1-17 (red weed?)
- **Alien structures:** MT_* (Martian Tripod parts), HE_* (Heat-Ray emitters), PP_* (Pit Platforms), MB_* (Mother Builders), F_* (Fighting Machines)
- **Level geometry:** LH1_00F through LH23_90F (112 files - likely terrain/building angles)

### WOF Units (145 files)
- **Human vehicles:** IRONCL_1-3 (ironclad), BALLOON, AIRSHIP1-3, ZEPPLIN1-3, BIKE2, BRIDGE_1-4
- **Weapons:** MORTAR1-3, BOMBAR_1-3 (bombardier), ELECTR_1-3, R_RAY_1-3 (ray weapon), HEAT_R_1-3 (heat ray)
- **Units:** FIGHT_1-3, HEV_GU_1-4 (heavy guns), MED_GU_1-4 (medium guns), AA_GU_1-3 (anti-air guns), SAPPER_1-2
- **Construction:** DIGGER_1-2, CONSTR_1-2, CONS_M_1-3 (construction machine)
- **Alien units:** DRONE_1-3, FLYING_1-3, HANDLE_1-2, TELEPA_1-3 (telepathic), TEMPES_1-3 (tempest), PROJEC_1-3 (projector), SCANIN_1-3 (scanning)
- **Alien vehicles:** AR_LOR_1-3 (armored lorry?), AR_TRA_1-3 (armored transport?), TUNNEL_1-3, SUBMER_1-3 (submersible)
- **Support:** MOB_RE_1-2 (mobile repair?), SEL_PR_1-3 (self-propelled?), SCOU_M_1-3 (scout machine)
- **Shadows:** SH_* prefix (26 files - shadow versions of units)

---

## Binary Structure

### IOB Format (Initial Findings)

```
Offset   Type      Description
------   ----      -----------
0x0000   char[4]   Magic: "FFUH" (0x48554646) - Huffman compression marker
0x0004   uint32    Unknown value (typically 0x2212 = 8722)
0x0008   uint32    Unknown value (typically 0x17E8 = 6120)
0x000C   uint32    Unknown value (typically 0xBF1C = 48924)
0x0010   uint32    Possible vertex count or data size
...      varies    Compressed data follows
```

**Key Observations:**
1. All IOB files start with "FFUH" magic bytes (reversed "HUFF")
2. Files range from ~1KB (simple trees) to ~70KB (complex buildings)
3. Data appears to be Huffman compressed after header
4. No obvious float patterns in early bytes (compressed)
5. Likely contains: vertices, faces, normals, UVs, texture names

### WOF Format (Initial Findings)
- Similar structure to IOB
- Additional data for animation frames (vehicles have moving parts)
- Turret rotation data (guns)
- Possible damage states (numbered variants like HEAT_R_1, HEAT_R_2, HEAT_R_3)

---

## Sample Analysis: FARM.IOB

**File Size:** 7160 bytes (0x1BF8)

### Header Analysis
```
Offset   Hex                Value (uint32)   Notes
------   ---                --------------   -----
0x0000   46 46 55 48        0x48554646       Magic: "FFUH"
0x0004   12 22 00 00        8722             Unknown
0x0008   E8 17 00 00        6120             Unknown
0x000C   1C BF 00 00        48924            Unknown
0x0010   B1 03 00 00        945              Possible count
0x0014   6B 00 00 00        107              Possible count
0x0018   6F 01 00 00        367              Possible count
0x001C   9E 00 00 00        158              Possible count
```

### Pattern Detection
- No obvious repeated 12/16/20/24-byte structures (data is compressed)
- Few ASCII strings found (mostly binary data)
- Likely need to decompress Huffman encoding first before parsing geometry

---

## Next Steps

### Immediate (Decompression Required)
1. **Reverse engineer Huffman decompression algorithm**
   - Analyze game executable (WoW.exe) for IOB loading code
   - Check IDA notes in `WoWDecomp/ida-notes-thor/` for clues
   - Look for Huffman tree construction in game code

2. **Test decompression on sample files**
   - Start with simplest models (TREE1.IOB, POD.IOB)
   - Verify decompressed size estimates
   - Check for known 3D format markers after decompression

### Once Decompressed
3. **Parse geometry data**
   - Identify vertex format (position, normal, UV)
   - Find face index arrays
   - Locate material/texture references
   - Determine coordinate system (Y-up vs Z-up)

4. **Build IobToGltfConverter**
   - Convert vertices/faces to GLTF format
   - Map texture names to material slots
   - Handle LOD levels if present
   - Export to `ExtractedModels/GLTF/`

5. **Implement WOF animation support**
   - Parse animation frame data
   - Export skeletal hierarchy
   - Convert to GLTF with animations

---

## Tools

### ModelAnalysisTool (CLI)
Located: `ModelAnalysisTool/bin/Release/net8.0/ModelAnalysisTool.exe`

**Commands:**
```bash
# Extract models from archive
ModelAnalysisTool.exe extract-models "path\to\Dat.wow" "output\dir"

# Analyze single file
ModelAnalysisTool.exe analyze-iob "FARM.IOB"
ModelAnalysisTool.exe analyze-wof "AIRSHIP.WOF"

# Batch analysis
ModelAnalysisTool.exe batch-analyze "input\dir" "output\dir"
```

### Analysis Output
- Hex dump with ASCII representation (16 bytes per line)
- Header pattern analysis (magic bytes, counts)
- Potential float value detection
- Structure size pattern detection (12/16/20/24/32-byte repeats)
- ASCII string location finder

---

## Related Files
- `WoWViewer/Parsers/IobParser.cs` - IOB analysis framework
- `WoWViewer/Parsers/WofParser.cs` - WOF analysis framework
- `ModelAnalysisTool/Program.cs` - CLI extraction tool
- `ExtractedModels/` - 494 extracted model files
- `ModelAnalysis/` - 494 hex dump analysis reports
- `WoWDecomp/ida-notes-thor/` - Game executable reverse engineering notes

---

## Research Notes

### Huffman Compression
The "FFUH" magic strongly suggests custom Huffman encoding:
- Tree likely embedded in each file or shared globally
- May use fixed-length bit codes or variable-length
- Decompressor probably in WoW.exe around file I/O routines
- Check for "WOW::LoadModel" or similar functions in IDA

### Model Complexity
Based on file sizes, rough estimates of polygon counts:
- Simple models (trees, poles): 50-200 polygons (~1-3 KB)
- Medium models (houses, vehicles): 500-2000 polygons (~5-15 KB)
- Complex models (cathedrals, tripods): 2000-8000 polygons (~20-70 KB)

### Texture References
No embedded texture data found - textures likely in separate files:
- Look for .PAL (palette), .SPR (sprite), or texture archives
- Texture names probably stored as fixed-length strings in model data
- May reference by ID rather than filename

---

**Last Updated:** 2025-01-23  
**Status:** Phase 1 - Format Analysis Complete ✅  
**Next Phase:** Phase 2 - Huffman Decompression Research 🔄
