# IOB/WOF File Format Documentation

## Overview
IOB (buildings) and WOF (units) files are 3D model formats used by the RAGE engine in Jeff Wayne's "The War of the Worlds" (1998). Both use Huffman compression indicated by the **FFUH** magic bytes (HUFF reversed).

## File Structure

### Magic Header
```
Offset 0x00: "FFUH" (0x48554646) - Huffman compression marker
Offset 0x04: uint32 - Compressed data size or flags
Offset 0x08: uint32 - Uncompressed size or data pointer
Offset 0x0C: uint32 - Unknown (possibly vertex count or offset)
```

### Compression
Files use **Huffman encoding** (variable-length codes based on frequency). The RAGE engine likely has a built-in Huffman decoder that:
1. Reads the "FFUH" header
2. Parses compression dictionary/tree
3. Decompresses to raw 3D data (vertices, normals, faces, UV coords)

### Decompressed Structure (Estimated)
After decompression, files likely contain:
- **Vertex data** (x, y, z coordinates as floats or fixed-point)
- **Normal vectors** (for lighting calculations)
- **Face indices** (triangles, typically 3 indices per face)
- **UV coordinates** (texture mapping, 2 floats per vertex)
- **Material IDs** (references to textures/colors)
- **LOD levels** (Level of Detail for performance)

## Analysis Results

### IOB Files (Buildings)
- **Count**: 349 files
- **Size Range**: ~1KB to ~50KB
- **Examples**:
  - `FARM.IOB` - 7,160 bytes
  - `ABBEY.IOB` - 9,037 bytes
  - `CASTLE.IOB` - Larger structures
  
**Characteristics:**
- Static geometry (no animation)
- Generally larger polygon counts than WOF
- Often have multiple parts (chimneys, roofs, walls)
- Naming suggests building types: FARM, HOUSE1, CASTLE, etc.

### WOF Files (Units)
- **Count**: 145 files
- **Size Range**: ~1KB to ~20KB
- **Examples**:
  - `AIRSHIP.WOF` - 19,812 bytes
  - `FIGHTING.WOF` - Fighting machine
  - `HEAT_R_1.WOF` - Heat ray weapon

**Characteristics:**
- May contain animation data (multiple poses)
- Smaller polygon counts for performance (many units on screen)
- Suffix patterns suggest variants: _1, _2, _3 (different angles/states?)
- Human units (BALLOON, IRONCLAD) vs Martian (FIGHTING, HEAT_R)

## Reverse Engineering Strategy

### Phase 1: Decompression ✅ IN PROGRESS
1. **Analyze Huffman structure** - Examine first ~1KB of files
2. **Build Huffman decoder** - Implement or use existing library
3. **Extract raw geometry** - Decompress to binary blobs

### Phase 2: Geometry Parsing
1. **Identify vertex blocks** - Look for float patterns (typical 3D coords: -100 to +100)
2. **Find face indices** - Sequential integers referencing vertices
3. **Map UV coordinates** - Pairs of floats (0.0 to 1.0 range)
4. **Parse material data** - Color/texture references

### Phase 3: GLTF Conversion
1. **Build GLTF exporter** - Use SharpGLTF library
2. **Map vertices/normals** - Convert coordinate system (RAGE → Y-up)
3. **Generate faces** - Triangle indices
4. **Embed textures** - Link to .SPR texture atlases
5. **Export animations** (WOF only) - Multiple pose frames

## Tools Created

### ModelAnalysisTool ✅
Location: `ModelAnalysisTool/bin/Release/net8.0/ModelAnalysisTool.exe`

**Commands:**
```powershell
# Extract all models from archives
.\ModelAnalysisTool.exe extract-all "path/to/Dat.wow"

# Analyze single model
.\ModelAnalysisTool.exe analyze-iob "path/to/FARM.IOB"
.\ModelAnalysisTool.exe analyze-wof "path/to/AIRSHIP.WOF"

# Batch analyze all models
.\ModelAnalysisTool.exe batch-analyze "ExtractedModels/" "ModelAnalysis/"
```

**Output:**
- `ExtractedModels/` - 494 raw IOB/WOF files
- `ModelAnalysis/` - 494 analysis reports (hex dumps, patterns)

### Parsers Created ✅
- `IobParser.cs` - IOB building model parser
- `WofParser.cs` - WOF unit model parser
- Both detect "FFUH" magic and extract header data

## Next Steps

### Immediate (Step 2)
1. **Implement Huffman decoder** - Critical for decompression
2. **Test decompression** - Verify output makes sense
3. **Parse vertex data** - Identify 3D coordinate patterns

### Near-term
4. **Build GLTF exporter** - Convert to modern format
5. **Import to Blender** - Visual verification
6. **Import to Godot** - Engine integration

### Long-term
7. **Batch convert all 494 models** - Automate pipeline
8. **Create texture atlas** - Combine .SPR files
9. **Map materials** - Link textures to models

## Resources

### Libraries
- **SharpGLTF** - GLTF 2.0 export (NuGet package)
- **System.IO.Compression** - Built-in decompression utilities
- **K4os.Compression.LZ4** - If not Huffman (backup option)

### Documentation
- GLTF 2.0 Specification: https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html
- Huffman Coding: https://en.wikipedia.org/wiki/Huffman_coding
- RAGE Engine: Limited public documentation (reverse engineer required)

### Example Code Structure
```csharp
// Future implementation
public class IobDecoder
{
    public static byte[] DecompressHuffman(byte[] compressedData)
    {
        // 1. Parse Huffman tree from header
        // 2. Decode bit stream
        // 3. Return raw geometry
    }
    
    public static GltfModel ParseGeometry(byte[] rawData)
    {
        // 1. Extract vertices (x,y,z floats)
        // 2. Extract normals
        // 3. Build face indices
        // 4. Add UV coordinates
        // 5. Return GLTF structure
    }
}
```

## File Naming Patterns

### Buildings (IOB)
- `LH##_##F.IOB` - Lighthouse variants (rotation angles: 00, 30, 45, 60, 90)
- `RED##.IOB` - Generic buildings (17 variants)
- `HB_PARL#.IOB` - Parliament building (7 parts)
- `*_MAIN.IOB` - Main building component
- `*_CHIM.IOB` - Chimney components

### Units (WOF)
- `*_1.WOF`, `*_2.WOF`, `*_3.WOF` - Animation frames or damage states
- `HEAT_R_*.WOF` - Martian heat ray
- `FIGHT_*.WOF` - Martian fighting machine
- `IRONC*.WOF` - Human ironclad ship
- `*T1.WOF`, `*T2.WOF` - Turret/weapon variants

## Status

- ✅ **Extraction Complete** - All 494 models extracted
- ✅ **Analysis Complete** - Binary structure documented
- ⏳ **Decompression** - Huffman decoder needed (STEP 2)
- ⏳ **Parsing** - Geometry extraction (STEP 3)
- ⏳ **Conversion** - GLTF export (STEP 4)
- ⏳ **Validation** - Import to Blender/Godot (STEP 5)

---

**Last Updated**: November 13, 2025  
**Progress**: Step 1 Complete ✅ - Moving to Step 2 (Huffman Decompression)
