# War of the Worlds Revival - Development Roadmap Implementation

## Executive Summary

Based on the README.md analysis, here's a comprehensive implementation plan for the remaining features of the **WoW Revived** project.

---

## **Current Project Status**

### ? **Completed (95-100%)**
1. **Custom Launcher** - 95%
   - Dynamic language detection
   - Registry settings
   - Keyboard shortcuts (WIP)

2. **File Extractor** - 95%
   - .WoW archive extraction
   - Waveform preview/replace
   - OJD parsing (just massively improved!)

3. **Text Editor** - 100%
   - All 1396 strings editable
   - UTF-8 + ISO-8859-1 support
   - File recompilation
   - Import/Export
   - Rich text with newlines

### ?? **In Progress (1-10%)**
4. **Save Editor** - 10%
   - ? Name/Date/Time editing
   - ? Swap sides
   - ? Delete saves
   - ? Resource editing
   - ? Building/Unit management

5. **Map Editor** - 1%
   - Basic .nsb parsing only

6. **Decomp/Recomp** - 1%
   - Virtual key mapping started

### ? **Not Started (0%)**
7. **No-CD Music Fix** - 0%
8. **Video Playback Intercept** - 0%
9. **Remake** - 0%

---

## **Implementation Priority Queue**

### **?? Priority 1: Complete Save Editor (10% ? 80%)**
**Timeline:** 2-4 weeks  
**Effort:** Medium  
**Impact:** High  

**Why First:**
- Foundation already exists
- High community demand
- Uses existing OJD parsers
- Requires save file reverse engineering (benefits other tools)

**Deliverables:**
1. ? Save file structure documentation (`SaveFileStructure.cs`)
2. ? Save file parser (`SaveFileParser.cs`)
3. ? Resource editing UI
4. ? Building inventory display/editing
5. ? Unit inventory display/editing
6. ? Sector control editor
7. ? Research progress editor

**Files Created:**
- `WoWViewer/SaveGame/SaveFileStructure.cs`
- `WoWViewer/SaveGame/SaveFileParser.cs`
- `WoWViewer/SaveGame/IMPLEMENTATION_PLAN.md`

---

### **?? Priority 2: Enhance Map Editor (1% ? 40%)**
**Timeline:** 3-5 weeks  
**Effort:** High  
**Impact:** Medium-High  

**Why Second:**
- Enables map modding
- Requires NSB format documentation
- Visual representation needed
- Complex data structures

**Deliverables:**
1. Document .nsb file structure
2. Create `NsbFileParser.cs`
3. Identify terrain data
4. Identify unit placement data
5. Identify building placement data
6. Create visual map viewer (2D grid)
7. Basic map editing capabilities
8. Export modified maps

**Approach:**
```csharp
// Proposed structure
public class NsbMapData
{
    public int MapWidth { get; set; }
    public int MapHeight { get; set; }
    public TerrainTile[,] Terrain { get; set; }
    public List<MapBuilding> Buildings { get; set; }
    public List<MapUnit> Units { get; set; }
}

public class TerrainTile
{
    public byte TerrainType { get; set; }
    public byte HeightLevel { get; set; }
    public bool IsWalkable { get; set; }
}
```

---

### **?? Priority 3: No-CD Music Fix (0% ? 60%)**
**Timeline:** 2-3 weeks  
**Effort:** Medium  
**Impact:** Very High  

**Why Third:**
- High community demand
- Improves game experience
- Relatively self-contained
- Multiple solution approaches

**Proposed Solutions:**

**Option A: Mini-ISO Creation**
```
1. Extract audio tracks from CDs
2. Create CUE/BIN files
3. Mount via WinCDEmu API
4. Redirect game CD calls
```

**Option B: Audio File Replacement**
```
1. Extract CD audio to WAV/MP3
2. Hook DirectSound/WinMM
3. Redirect audio playback
4. No CD required
```

**Option C: DLL Injection**
```
1. Create wrapper DLL for CD audio calls
2. Inject into game process
3. Intercept MCI commands
4. Play files instead of CD
```

**Recommended:** Option B (most compatible)

**Deliverables:**
1. CD audio extraction tool
2. Audio format converter (CDA ? WAV/OGG)
3. Audio hooking DLL
4. Installer/patcher
5. Configuration UI

---

### **?? Priority 4: Video Playback Intercept (0% ? 50%)**
**Timeline:** 3-4 weeks  
**Effort:** High  
**Impact:** Medium  

**Why Fourth:**
- Enables HD video mods
- Complex DLL hooking required
- Video upscaling needed

**Proposed Approach:**
```
1. Intercept smackw32.dll calls
2. Redirect to binkw32.dll (modern codec)
3. Upscale original videos with AI (Topaz Video Enhance AI)
4. Replace video files
5. Maintain compatibility
```

**Technical Challenges:**
- Bink vs Smacker format differences
- Maintain audio sync
- Resolution scaling
- Performance considerations

**Deliverables:**
1. Video replacement system
2. Smacker to Bink converter
3. Video upscaling guide
4. HD video pack (community contribution)

---

### **?? Priority 5: Expand Decomp/Recomp (1% ? 30%)**
**Timeline:** Ongoing (6+ months)  
**Effort:** Very High  
**Impact:** High (Long-term)  

**Why Fifth:**
- Long-term project
- Enables deep modding
- Foundation for remake
- Community collaboration needed

**Current Status:**
- Basic virtual key mapping done
- `ida-map.txt` started

**Next Steps:**
1. Map all keyboard shortcuts
2. Document function addresses
3. Identify game logic functions
4. Create API documentation
5. Build modding SDK

**Files to Create:**
```
WoWDecomp/
??? FunctionMap.md         # All identified functions
??? DataStructures.md      # Game data structures
??? NetworkProtocol.md     # Multiplayer protocol
??? FileFormats.md         # Complete file format docs
??? ModdingSDK/  # Tools for modders
    ??? WoWModAPI.dll
    ??? Documentation/
```

---

### **?? Priority 6: Remake (0% ? Planning)**
**Timeline:** 1-2 years  
**Effort:** Extreme  
**Impact:** Ultimate Goal  

**Why Last:**
- Most complex undertaking
- Requires all above completed
- Legal considerations
- Team effort needed

**Proposed Tech Stack:**
- **Engine:** Unreal Engine 5 or Unity
- **Graphics:** Upscaled textures + modern rendering
- **Audio:** Re-recorded orchestra + original voice acting
- **Networking:** Modern protocols (Steam, Epic)
- **Modding:** Built-in mod support

**Milestones:**
1. Proof of concept (single battle)
2. Campaign map recreation
3. All units/buildings implemented
4. Multiplayer functional
5. Campaign missions complete
6. Polish & release

---

## **Development Resources Needed**

### **Tools:**
- **Hex Editors:** HxD, 010 Editor
- **Disassemblers:** IDA Pro, Ghidra
- **Graphics:** Photoshop, GIMP, Blender
- **Audio:** Audacity, FL Studio
- **Video:** Topaz Video Enhance AI, Adobe After Effects

### **Team Roles:**
- **Lead Developer:** Architecture & coordination
- **Reverse Engineers:** Save files, map files, exe analysis
- **UI/UX Developer:** WinForms improvements
- **Graphics Artist:** Texture upscaling, icon design
- **Audio Engineer:** Music extraction, enhancement
- **QA Testers:** Bug hunting, validation
- **Documentation Writer:** Wikis, tutorials, API docs

### **Community Support:**
- **Discord:** https://discord.gg/bwG6Z3RK8b
- **GitHub:** Issues, PRs, discussions
- **Save File Samples:** Different game states
- **Testing:** Cross-platform validation

---

## **Recommended Development Order**

### **Phase 1 (Month 1-2):**
1. ? Complete OJD parser improvements (DONE!)
2. ? Complete Save Editor (resource editing)
3. ? Begin save file structure documentation

### **Phase 2 (Month 3-4):**
1. Complete Save Editor (buildings/units)
2. Begin Map Editor enhancement
3. Start No-CD Music Fix

### **Phase 3 (Month 5-6):**
1. Complete No-CD Music Fix
2. Advance Map Editor to 40%
3. Begin Video Playback Intercept

### **Phase 4 (Month 7-12):**
1. Complete Video Playback Intercept
2. Advance Decomp work to 30%
3. Begin Remake planning

---

## **Success Metrics**

| Feature | Current | Target | Priority |
|---------|---------|--------|----------|
| Custom Launcher | 95% | 100% | Low |
| File Extractor | 95% | 100% | Low |
| Text Editor | 100% | 100% | ? |
| Save Editor | 10% | 80% | **HIGH** |
| Map Editor | 1% | 40% | Medium |
| No-CD Music | 0% | 60% | High |
| Video Intercept | 0% | 50% | Medium |
| Decomp/Recomp | 1% | 30% | Low |
| Remake | 0% | Planning | Low |

---

## **Risk Assessment**

### **High Risk:**
- **Save File Structure Unknown** - Requires extensive RE
- **NSB Map Format Undocumented** - May be complex
- **Legal Issues (Remake)** - Need clearance from rights holders

### **Medium Risk:**
- **Video Upscaling Quality** - AI results vary
- **Multiplayer Compatibility** - Network code fragile
- **Community Adoption** - Need active user base

### **Low Risk:**
- **OJD Parsers** - Already working well
- **Text Editing** - Fully functional
- **No-CD Fix** - Standard techniques

---

## **Conclusion**

The **Save Editor** is the clear first priority. With the OJD parsers now fully functional and modular, we have a solid foundation for:

1. Mapping building/unit IDs to names
2. Loading TEXT.ojd descriptions
3. Parsing save file data structures

**Next immediate action:** Obtain sample save files and begin hex analysis to document resource offsets and sector data structures.

The comprehensive framework is now in place in:
- `WoWViewer/SaveGame/SaveFileStructure.cs`
- `WoWViewer/SaveGame/SaveFileParser.cs`
- `WoWViewer/SaveGame/IMPLEMENTATION_PLAN.md`

Once save file offsets are documented, implementation of resource editing is straightforward (1-2 days). Building/unit management will follow (1-2 weeks).

**Let's get this game fully preserved and moddable! ??**
