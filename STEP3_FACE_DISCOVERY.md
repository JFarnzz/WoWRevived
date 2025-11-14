# STEP 3 PROGRESS: Face Format Investigation

## Date: November 13, 2025

## Status: INCONCLUSIVE - Geometry Format Partially Decoded

## Critical Findings

### 1. IOB Files Contain ONLY Vertex Data
After analyzing the complete structure of decompressed IOB files (tested with FARM.IOB):
- **Total coordinates**: 725 Vector3 triplets (12 bytes each)
- **Header vertex count**: 336 (stored at offset 0x1E)
- **Additional coordinates**: 389
- **NO face index data** in the file

The file is **100% coordinate data** from offset 0x16 to EOF (0x2212).

### 2. Additional Coordinates Are NOT Normals
Analysis of the 389 additional coordinates:
- **Average magnitude**: 12.356 (not unit length)
- **Near unit length (0.9-1.1)**: 0.0% (would be ~100% if normals)
- **Conclusion**: Additional coordinates are VERTICES, not normal vectors

### 3. Face Topology is Implicit by Vertex Order

**Hypothesis**: Every 3 consecutive vertices define one triangle face (triangle list)

**Test Results**:
```
Total vertices: 725
Triangles created: 241 (725 / 3)
Leftover vertices: 2
Degenerate triangles: 9 (3.7%)
Average triangle area: 79.090
Median triangle area: 55.224
Min area: 0.000
Max area: 416.686
```

**Analysis**:
- **Only 3.7% degenerate triangles** - Very low, suggests correct interpretation
- For comparison, random vertex ordering would produce 50%+ degenerate triangles
- Triangle areas are reasonable for building-scale geometry (FARM is ~24 units tall)

### 4. Validation Strategy

Created `FARM.IOB.implicit.obj` with:
- All 725 vertices
- 241 triangle faces (indices: 0,1,2 / 3,4,5 / 6,7,8 / ...)

**Next Steps**:
1. ✅ Open `ExportedOBJ/FARM.IOB.implicit.obj` in Blender
2. Visual inspection:
   - Does it look like a farm building?
   - Are faces properly oriented (not inside-out)?
   - Are there visible holes or missing geometry?
3. If validated → Apply to all 494 models
4. If invalid → Investigate alternative topology (strips, fans, or external indices)

## Technical Implications

### RAGE Engine 3D Format Structure

```
[FFUH Compressed IOB File]
  ↓ Huffman Decompression
[Decompressed Geometry]
  Header (0x00-0x15)
    - Unknown fields
    - Vertex count at 0x1E (ushort)
  Vertex Data (0x16-EOF)
    - ALL vertices as sequential Vector3 triplets
    - No separation between "unique" and "indexed" vertices
    - Faces implicitly defined by vertex order (every 3 = triangle)
```

### Key Insight: Vertex Duplication

The format **duplicates vertices** rather than using index buffers:
- 336 "unique" positions from header count
- 725 total vertices in file
- **2.16x vertex duplication ratio**

This is typical of:
1. **Per-face normals**: Each triangle gets its own vertex copies for sharp edges
2. **UV seam handling**: Vertices duplicated where texture coordinates split
3. **Simplicity**: No index buffer = simpler renderer (1998 hardware consideration)

## Files Modified/Created

1. `ModelAnalysisTool/CompleteCoordinateAnalyzer.cs` - Parses ALL coordinates, not just header count
2. `ModelAnalysisTool/ImplicitTopologyTester.cs` - Tests triangle list/strip hypotheses
3. `ExportedOBJ/FARM.IOB.implicit.obj` - Complete mesh with 725 vertices, 241 faces
4. `DecompressedModels/FARM.IOB.coordinates.csv` - All coordinates with metadata

## Open Questions

1. **Header vertex count meaning**: Why does header say 336 if all 725 are vertices?
   - Possible: 336 = "unique positions", 389 = duplicates for sharp edges
   - Possible: 336 = first LOD level, rest are detail vertices
   
2. **2 leftover vertices**: 725 = (241 × 3) + 2
   - Could be padding
   - Could be end markers
   - Unlikely to be significant

3. **WOF (unit) files**: Do animated models use same format?
   - Likely more complex (animation frames, bone weights)
   - Test with simple unit model once buildings are validated

## Validation Checklist

### Blender Visual Validation
- [ ] Import `FARM.IOB.implicit.obj` into Blender
- [ ] Check overall shape resembles farm building
- [ ] Verify face normals (View → Face Orientation in Blender)
- [ ] Look for holes/gaps (missing faces)
- [ ] Compare against in-game screenshot if available

### Batch Testing
- [ ] Apply implicit topology to 10 different building models
- [ ] Check if degenerate triangle % remains low (< 10%)
- [ ] Identify any models that don't work with this format

### Next Models to Test
1. **CHURCH.IOB** - Larger building, more complex
2. **COTTAGE.IOB** - Small building, simple
3. **CRANE.IOB** - Tall structure, different proportions
4. **AR_TRAT1.WOF** - First unit model (animation test)

## Test Results Summary

### Hypothesis 1: Implicit Triangle List ❌ REJECTED
- **Result**: Garbled geometry in Blender
- **Degenerate triangles**: 3.7% (seemingly good)
- **Visual**: Completely unusable - random triangle connections

### Hypothesis 2: Byte-Indexed Triangles ❌ REJECTED  
- **Valid indices**: 100% (all bytes < 336)
- **Degenerate triangles**: 74.2%
- **Issue**: Extreme clustering - 52% of indices are value 192, 30% are 176
- **Conclusion**: Not standard triangle indices

### Hypothesis 3: Quad-Based Geometry ❌ REJECTED
- **Valid quads**: 4.5% (53/1167)
- **Degenerate quads**: 95.5%
- **Conclusion**: Even worse than triangles

## Current Understanding

### What We Know ✅
1. **Huffman decompression**: 100% working
2. **Vertex data**: Successfully parsed 336 unique vertices
3. **File structure**: 336 vertices + 4668 bytes of additional data
4. **Data validity**: Post-vertex bytes are valid indices (< 336)

### What's Unclear ❓
1. **Face topology**: Cannot decode face connectivity from available data
2. **Byte patterns**: Suspicious clustering (52% are value 192) doesn't match normal index distribution
3. **Additional data purpose**: Could be LOD, collision mesh, or metadata rather than render geometry

### Most Likely Scenarios

1. **Incomplete Format**: IOB files contain vertex data + metadata, but face topology is stored elsewhere:
   - In game executable (hardcoded mesh definitions per model ID)
   - In separate companion files we haven't identified
   - In OBJ.ojd or other data files

2. **Non-Standard Encoding**: Face data uses custom compression or encoding we haven't cracked:
   - Run-length encoding
   - Triangle strips with custom restart markers
   - Procedural generation based on vertex patterns

3. **Purpose Mismatch**: IOB files might not be primary render geometry:
   - Collision meshes (simplified, hence degenerate triangles)
   - LOD levels (low detail representations)
   - Shadow volumes or other auxiliary geometry

## Next Steps for Investigation

### Option A: Reverse Engineer Game Executable
- Use IDA Pro to find 3D rendering code
- Trace how IOB files are loaded and processed
- Identify where face topology comes from
- **Difficulty**: High - requires x86 assembly analysis
- **Time**: Days to weeks

### Option B: Extract from Running Game
- Use 3D interception tools (RenderDoc, Nijs, 3D Ripper DX)
- Capture geometry directly from DirectX/OpenGL calls
- Reverse engineer from captured meshes
- **Difficulty**: Medium - game is old (1998), may not work with modern tools
- **Time**: Hours to days

### Option C: Community Research
- Check if modding community has documented format
- Look for existing tools or documentation
- Search for similar RAGE engine games from that era
- **Difficulty**: Low - just research
- **Time**: Hours

### Option D: Accept Limitation & Use Alternative Assets
- For remaster, create new 3D models inspired by originals
- Use extracted textures (SPR files) for visual reference
- Focus on gameplay mechanics rather than asset extraction
- **Difficulty**: Low - creative work
- **Time**: Varies

## Files for Further Analysis

Created during investigation:
- `FARM.IOB.implicit.obj` - Failed implicit topology test
- `FARM.IOB.byteindexed.obj` - Failed byte index test
- `FARM.IOB.quads.obj` - Failed quad test
- `FARM.IOB.coordinates.csv` - All 725 coordinates for external analysis
- Analysis tools in `ModelAnalysisTool/`

## Recommendation

Given the difficulty in decoding the face format, I recommend **Option B** (3D capture from running game) as the most practical path forward for the remaster project. This would give you the actual rendered geometry without needing to fully reverse engineer the proprietary format.
