# FFUH Header Structure Analysis

## Raw Data Analysis

### FARM.IOB (7160 bytes)
```
Offset 0x04: 0x00002212 = 8722 dec
Offset 0x08: 0x000017E8 = 6120 dec  
Offset 0x0C: 0x0000BF1C = 48924 dec
Offset 0x10: 0x000003B1 = 945 dec
```

### CASTLE.IOB (7458 bytes)
```
Offset 0x04: 0x00002218 = 8728 dec
Offset 0x08: 0x00001912 = 6418 dec
Offset 0x0C: 0x0000C86F = 51311 dec
Offset 0x10: 0x00000443 = 1091 dec
```

### AIRSHIP.WOF (19812 bytes)
```
Offset 0x04: 0x00007136 = 28982 dec
Offset 0x08: 0x00004954 = 18772 dec
Offset 0x0C: 0x00024A7F = 150143 dec
Offset 0x10: 0x000013B1 = 5041 dec
```

### FIGHTING.WOF (49297 bytes)
```
Offset 0x04: 0x00010ECE = 69326 dec
Offset 0x08: 0x0000BC81 = 48257 dec
Offset 0x0C: 0x0005E3E7 = 386023 dec
Offset 0x10: 0x00005041 = 20545 dec
```

## Pattern Analysis

### Observation 1: Offset 0x04 vs File Size
| File | File Size | Value @ 0x04 | Ratio |
|------|-----------|--------------|-------|
| FARM.IOB | 7160 | 8722 | 1.22x |
| CASTLE.IOB | 7458 | 8728 | 1.17x |
| AIRSHIP.WOF | 19812 | 28982 | 1.46x |
| FIGHTING.WOF | 49297 | 69326 | 1.41x |

**Hypothesis**: 0x04 contains **DECOMPRESSED SIZE** (raw geometry data size).  
Compression ratio averages 1.2-1.5x (file is 70-85% of decompressed size).

### Observation 2: Offset 0x08 vs File Size
| File | File Size | Value @ 0x08 | Ratio |
|------|-----------|--------------|-------|
| FARM.IOB | 7160 | 6120 | 0.85x |
| CASTLE.IOB | 7458 | 6418 | 0.86x |
| AIRSHIP.WOF | 19812 | 18772 | 0.95x |
| FIGHTING.WOF | 49297 | 48257 | 0.98x |

**Hypothesis**: 0x08 contains **COMPRESSED DATA SIZE** (size of actual Huffman-encoded bit stream).  
Typically 85-98% of file size. Remaining bytes are header/tree data.

### Observation 3: Offset 0x0C - The Mystery Value
| File | Value @ 0x0C | vs Decompressed | Pattern |
|------|--------------|-----------------|---------|
| FARM.IOB | 48924 | 5.6x larger | 0x0000BF1C |
| CASTLE.IOB | 51311 | 5.9x larger | 0x0000C86F |
| AIRSHIP.WOF | 150143 | 5.2x larger | 0x00024A7F |
| FIGHTING.WOF | 386023 | 5.6x larger | 0x0005E3E7 |

**This is NOT a file offset or size!** Values are way too large.

**Possible interpretations:**
1. **CRC32 checksum** of decompressed data
2. **Encoded metadata** (multiple values packed into uint32)
3. **Bit offset** for Huffman tree start (in bits, not bytes)
4. **Magic number** specific to RAGE engine's Huffman variant

### Observation 4: Offset 0x10 - Smaller Values
| File | Value @ 0x10 | vs Decompressed | vs Compressed |
|------|--------------|-----------------|---------------|
| FARM.IOB | 945 | 10.8% | 15.4% |
| CASTLE.IOB | 1091 | 12.5% | 17.0% |
| AIRSHIP.WOF | 5041 | 17.4% | 26.9% |
| FIGHTING.WOF | 20545 | 29.6% | 42.6% |

**Hypothesis**: 0x10 might be **HUFFMAN TREE SIZE** or **HEADER SIZE**.  
Larger models have proportionally larger trees (more unique byte values/patterns).

### Observation 5: Values at 0x14+ Look Like Frequencies
```
FARM.IOB:
  [10]: 107, [14]: 367, [18]: 158, [1C]: 154, [20]: 264, [24]: 82, [28]: 85...

FIGHTING.WOF:
  [10]: 493, [14]: 896, [18]: 400, [1C]: 250, [20]: 155, [24]: 660, [28]: 206...
```

These are **byte-sized values** (107, 367, 158, etc.) that could represent:
- **Symbol frequencies** for building Huffman tree (frequency table format)
- **Tree node data** (parent/child relationships)

## Header Structure Hypothesis

```
Offset  Size  Field                Description
------  ----  -------------------  ----------------------------------------
0x00    4     Magic                "FFUH" (0x48554646)
0x04    4     DecompressedSize     Size of uncompressed geometry data
0x08    4     CompressedSize       Size of Huffman-encoded bit stream
0x0C    4     Unknown/Checksum     Mystery value (~5.6x decompressed size)
0x10    4     TreeSize/HeaderSize  Size of Huffman tree or header in bytes
0x14    ???   TreeData             Huffman tree encoding (frequency table?)
???     ???   CompressedData       Huffman-encoded bit stream
```

## Next Steps

### Step 2A: Validate Hypothesis
1. **Decompress based on current understanding**:
   - Read DecompressedSize (0x04)
   - Read CompressedSize (0x08)
   - Read TreeSize (0x10)
   - Skip to offset `0x14 + TreeSize` = start of compressed data
   - Build Huffman tree from bytes at 0x14 to 0x14+TreeSize
   - Decode bit stream

2. **Test tree format**:
   - Try interpreting 0x14+ as 256 uint32 frequency table
   - Try canonical Huffman with code lengths
   - Try pre-order tree traversal format

### Step 2B: Alternative - Reverse Engineer Original Decoder
Look in WoW.exe at decompression routine:
- IDA Pro disassembly in `WoWDecomp/ida-notes-thor/`
- Search for FFUH or 0x48554646 references
- Find function that reads these files
- Understand exact tree format

### Step 2C: Implement Test Decompressor
Create minimal decompressor to validate header understanding:
```csharp
byte[] data = File.ReadAllBytes("FARM.IOB");
int decompSize = BitConverter.ToInt32(data, 0x04);  // 8722
int compSize = BitConverter.ToInt32(data, 0x08);     // 6120
int treeSize = BitConverter.ToInt32(data, 0x10);     // 945

// Try to build tree from data[0x14..0x14+treeSize]
HuffmanNode root = BuildTreeFromFrequencyTable(data, 0x14, treeSize);

// Try to decompress
int dataStart = 0x14 + treeSize;  // Should be around offset 0x3C9
byte[] decompressed = DecompressBitStream(root, data, dataStart, compSize, decompSize);

// Validate: Look for patterns in decompressed data
// - Float patterns (3D coordinates)
// - uint16 patterns (face indices)
// - Repeating structures
```

## Confidence Levels

✅ **High Confidence (90%+)**:
- 0x00: "FFUH" magic bytes
- 0x04: Decompressed size
- 0x08: Compressed data size

⚠️ **Medium Confidence (70%)**:
- 0x10: Tree or header size
- 0x14+: Tree encoding starts here

❓ **Low Confidence (40%)**:
- 0x0C: Purpose unknown (not a simple offset/size)
- Exact tree encoding format
- Bit order (MSB first vs LSB first)

## References

- **Standard Huffman**: https://en.wikipedia.org/wiki/Huffman_coding
- **Canonical Huffman**: More efficient encoding, sorted by code length
- **zlib format**: Uses Huffman for DEFLATE algorithm (similar concepts)
- **RAGE engine**: Proprietary format, may have custom tree encoding
