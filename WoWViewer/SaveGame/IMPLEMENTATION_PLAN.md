# Save Editor Enhancement - Implementation Plan

## Overview

This document outlines the implementation plan for expanding the Save Editor from 10% ? 80% completion.

## Current Status

? **Completed Features:**

- Save name editing
- Date & time editing  
- Year override for dates below 1753
- Swap sides functionality (Human ? Martian)
- Delete save functionality
- Sector name loading from TEXT.ojd

? **Missing Features:**

- Resource editing (Steel/Coal/Oil or Human Blood/Copper/Heavy Elements)
- Building inventory per sector
- Unit inventory per sector
- Sector control status
- Detailed sector information

---

## Implementation Phases

### **Phase 1: Save File Reverse Engineering** ?

**Goal:** Document complete save file structure

**Tasks:**

1. Analyze save file hex dumps
2. Identify resource value offsets
3. Map sector data block locations
4. Document building/unit array structures
5. Create comprehensive offset mapping

**Tools Needed:**

- Hex editor (HxD, 010 Editor)
- Multiple save files at different game states
- IDA Pro notes from decomp work

**Output:** `SaveFileStructure.cs` with all offsets documented

---

### **Phase 2: Resource Editing** ??

**Status:** Framework Created
**Completion:** 25%

**Implementation:**

```csharp
// Add to SaveEditorForm.cs
private void LoadResources()
{
    var saveData = SaveFileParser.ParseSaveFile(fileName);
    
    if (saveData.Faction == FactionType.Human)
    {
        numericUpDown2.Value = saveData.Resource1; // Steel
        numericUpDown3.Value = saveData.Resource2; // Coal
        numericUpDown4.Value = saveData.Resource3; // Oil
    }
    else
    {
        numericUpDown2.Value = saveData.Resource1; // Human Blood
        numericUpDown3.Value = saveData.Resource2; // Copper
        numericUpDown4.Value = saveData.Resource3; // Heavy Elements
    }
}

private void SaveResources()
{
    var saveData = SaveFileParser.ParseSaveFile(fileName);
    
    saveData.Resource1 = (int)numericUpDown2.Value;
    saveData.Resource2 = (int)numericUpDown3.Value;
    saveData.Resource3 = (int)numericUpDown4.Value;
    
    SaveFileParser.WriteSaveFile(fileName, saveData);
}
```

**UI Changes:**

- Add 3 NumericUpDown controls for resources
- Labels update based on faction (Human vs Martian)
- Min: 0, Max: 999999
- Enable/disable with save file selection

---

### **Phase 3: Sector Data Visualization** ??

**Status:** Sector names loaded
**Completion:** 30%

**Implementation:**

```csharp
private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
{
    int sectorIndex = listBox2.SelectedIndex;
var sectorData = currentSaveData.Sectors[sectorIndex];
    
    // Update UI with sector information
    label9.Text = $"Sector: {sectorData.SectorName}";
    label10.Text = $"Control: {GetControlString(sectorData.ControlledBy)}";
    
    // Populate buildings list
    listBox3.Items.Clear();
    foreach (var building in sectorData.Buildings)
    {
        listBox3.Items.Add($"{building.BuildingName} (Lvl {building.BuildingLevel})");
    }
    
    // Populate units list
    listBox4.Items.Clear();
    foreach (var unit in sectorData.Units)
    {
        listBox4.Items.Add($"{unit.UnitName} (Lvl {unit.UnitLevel})");
    }
}
```

**Requires:**

- Sector control byte location
- Building array parsing
- Unit array parsing

---

### **Phase 4: Building Management** ??

**Status:** List boxes exist but not functional
**Completion:** 5%

**Features to Implement:**

- Display all buildings in selected sector
- Show building health/status
- Edit building level/health
- Add new buildings (if space allows)
- Remove buildings

**Data Mapping:**

- Use `OBJ.ojd` parser to map building IDs ? names
- Use `TEXT.ojd` parser for building descriptions

---

### **Phase 5: Unit Management** ??

**Status:** List boxes exist but not functional
**Completion:** 5%

**Features to Implement:**

- Display all units in selected sector
- Show unit health/experience
- Edit unit level/health
- Add new units (if space allows)
- Remove units

**Data Mapping:**

- Use `OBJ.ojd` parser to map unit IDs ? names
- Use `TEXT.ojd` parser for unit descriptions

---

### **Phase 6: Advanced Features** ??

**Status:** Not Started
**Completion:** 0%

**Features:**

1. **Research Progress Editor**
   - Current research item
   - Research completion percentage
   - Unlock all research (cheat)

2. **Sector Ownership Editor**
   - Change sector control (Neutral/Human/Martian)
   - Mass sector assignment

3. **Import/Export**
   - Export save data to JSON
   - Import from JSON
   - Save templates

4. **Validation**
   - Check for corrupt data
   - Warn about impossible values
   - Auto-repair common issues

---

## Technical Challenges

### **Challenge 1: Unknown Save File Structure**

**Problem:** Most save file offsets beyond name/date are undocumented

**Solutions:**

1. Analyze multiple saves at different game states
2. Compare hex dumps to identify patterns
3. Use IDA Pro to trace save/load functions in WoW.exe
4. Community help - ask Discord for save file samples

### **Challenge 2: Dynamic Data Structures**

**Problem:** Buildings/units per sector likely use variable-length arrays

**Solutions:**

1. Identify array headers (count + pointer)
2. Parse array sequentially
3. Use sentinel values to detect array end
4. Validate data consistency

### **Challenge 3: ID to Name Mapping**

**Problem:** Save files store IDs, need human-readable names

**Solutions:**
? Already solved with OJD parsers!

- Use `ObjOjdParser.Parse("OBJ.ojd")` for building/unit names
- Use `TextOjdParser.Parse("TEXT.ojd")` for descriptions
- Create lookup dictionaries on form load

---

## Testing Strategy

### **Test Cases:**

1. Load save files from different game stages
2. Edit resources, verify in-game
3. Edit date/time, verify campaign progression
4. Swap sides, verify faction changes
5. Add/remove buildings, verify sector state
6. Modify unit health, verify battle outcomes

### **Safety Measures:**

1. Always work on save file copies
2. Validate all writes before committing
3. Add "Restore Backup" functionality
4. Checksum validation (if game uses it)

---

## UI Enhancements

### **Additional Controls Needed:**

```
GroupBox: "Resources"
- NumericUpDown: Steel/Human Blood
- NumericUpDown: Coal/Copper
- NumericUpDown: Oil/Heavy Elements

GroupBox: "Sector Details"
- Label: Control Status
- ProgressBar: Sector Development

GroupBox: "Buildings" (listBox3)
- Button: Add Building
- Button: Remove Building
- Button: Edit Building

GroupBox: "Units" (listBox4)
- Button: Add Unit
- Button: Remove Unit
- Button: Edit Unit
```

---

## File Structure Documentation Needed

### **Critical Offsets to Find:**

```
0x0000 - Header/Magic bytes
0x000C - Save name (? known)
0x004C - Time (? known)
0x005A - Date (? known)
0x0070 - Resource 1 (?? needs verification)
0x0074 - Resource 2 (?? needs verification)
0x0078 - Resource 3 (?? needs verification)
0x0100 - Sector data block start (? unknown)
  - Sector control bytes
  - Building array
  - Unit array
  - Research data
```

---

## Next Steps

### **Immediate Actions:**

1. ? Create `SaveFileStructure.cs` framework
2. ? Create `SaveFileParser.cs` with read/write methods
3. ? Obtain multiple save files for analysis
4. ? Hex dump analysis to find resource offsets
5. ? Implement resource editing UI
6. ? Test resource modifications in-game

### **Short Term (1-2 weeks):**

1. Document sector data structure
2. Implement building list population
3. Implement unit list population
4. Add basic editing for buildings/units

### **Long Term (1 month):**

1. Full sector management
2. Research editor
3. Import/export functionality
4. Advanced validation

---

## Community Involvement

### **How Community Can Help:**

1. **Share Save Files**
   - Different campaign stages
   - Different factions
   - Before/after battles

2. **Test Modifications**
   - Verify in-game effects
   - Report bugs/crashes
   - Suggest features

3. **Reverse Engineering**
   - IDA Pro analysis
   - WoW.exe save/load function mapping
   - Share findings on Discord

4. **Documentation**
   - Wiki contributions
   - Tutorial videos
   - Community guides

---

## Success Metrics

**Current:** 10% Complete
**Target:** 80% Complete

**Feature Completion:**

- [?] Name editing - 100%
- [?] Date/Time editing - 100%
- [?] Swap sides - 100%
- [?] Delete saves - 100%
- [?] Sector names - 100%
- [??] Resource editing - 25%
- [?] Building management - 5%
- [?] Unit management - 5%
- [?] Sector control - 0%
- [?] Research editor - 0%

**Overall Progress:** 10% ? **Target: 80%**

When resource editing, building, and unit management are complete, we'll hit 80% functionality!
