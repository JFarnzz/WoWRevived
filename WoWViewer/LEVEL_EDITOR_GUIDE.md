# War of the Worlds - Level Editor Guide

## Overview

The Map Editor has been enhanced into a **full-featured Level Editor** allowing you to view, edit, create, and delete objects in all 30 sector maps of Jeff Wayne's "The War of the Worlds" game.

## Features Implemented

### ✅ Core Editing

- **Object Selection** - Click objects to select them (yellow highlight)
- **Drag & Drop** - Move selected objects by dragging with mouse
- **Multi-Select** - Hold **Ctrl** and click to select multiple objects
- **Object Creation** - Place new objects on the map
- **Object Deletion** - Remove objects from the map
- **Undo/Redo** - Full undo/redo system (up to 50 steps)

### ✅ Navigation & View

- **Zoom Controls** - Zoom in/out with toolbar buttons or mouse wheel
- **Pan View** - Right-click and drag to pan the camera
- **Grid Display** - Toggle grid overlay for alignment
- **Object Labels** - Show object names from OBJ.ojd
- **Sector Navigation** - Previous/Next buttons or dropdown selector

### ✅ File Operations

- **Auto-Backup** - Original files backed up as `.nsb.backup` before saving
- **Modified Indicator** - Tracks unsaved changes
- **Safe Writes** - Binary format preserved correctly

## Toolbar Controls

```
[◀ Previous] [Next ▶] | [➕] [➖] [🔍 Reset] | [Grid] [Labels] | 100% | [Select] [Place] [Delete] | [💾 Save]
```

### Navigation

- **Previous** - Load previous sector (Ctrl+Left Arrow)
- **Next** - Load next sector (Ctrl+Right Arrow)

### Zoom

- **➕** - Zoom in (Ctrl+Plus or Mouse Wheel Up)
- **➖** - Zoom out (Ctrl+Minus or Mouse Wheel Down)
- **Reset** - Reset zoom to 100% (Ctrl+0)

### Display

- **Grid** - Toggle grid overlay (G key)
- **Labels** - Toggle object name labels (L key)

### Edit Modes

- **Select** - Click and drag to move objects (S key) - *Default*
- **Place** - Click to place new objects (P key)
- **Delete** - Click objects to delete them (D key)

### File

- **Save** - Save changes to NSB file with auto-backup (Ctrl+S)

## Keyboard Shortcuts

### Navigation

- **Ctrl+Left Arrow** - Previous sector
- **Ctrl+Right Arrow** - Next sector

### Zoom

- **Ctrl+Plus** or **Mouse Wheel Up** - Zoom in
- **Ctrl+Minus** or **Mouse Wheel Down** - Zoom out
- **Ctrl+0** - Reset zoom

### Edit Modes

- **S** - Select mode
- **P** - Place mode
- **D** - Delete mode

### Editing

- **Ctrl+Z** - Undo last action
- **Ctrl+Y** - Redo last undone action
- **Delete** - Delete selected object
- **Ctrl+S** - Save current map

### Display

- **G** - Toggle grid
- **L** - Toggle labels

## Mouse Controls

### Select Mode (Default)

- **Left Click** - Select object (yellow highlight appears)
- **Ctrl+Left Click** - Add/remove from selection
- **Left Drag** - Move selected object(s)
- **Right Drag** - Pan view
- **Mouse Wheel** - Zoom in/out

### Place Mode

- **Left Click** - Place new object at cursor position

### Delete Mode

- **Left Click** - Delete object under cursor

## NSB File Format

Each map object has **6 fields** (12 bytes total):

```csharp
Field0 - X Coordinate (0-65535)
Field1 - Unknown (flags/layer?)
Field2 - Y Coordinate (0-65535)
Field3 - Unknown (rotation/state?)
Field4 - Object Type ID (links to OBJ.ojd)
Field5 - Unknown (properties?)
```

### Known Mappings

- **Field0** = X position on map
- **Field2** = Y position on map
- **Field4** = Object ID (references buildings/units in OBJ.ojd)

## Object Types (Field4 Values)

Object IDs are linked to `OBJ.ojd` definitions:

- **< 1000** - Terrain objects (trees, rocks, etc.)
- **1000-4999** - Buildings (houses, factories, monuments)
- **5000-9999** - Units (vehicles, machines)
- **10000+** - Special objects

## Workflow Examples

### Moving a Building

1. Load sector using dropdown or Previous/Next
2. Ensure **Select** mode is active (default)
3. Click the building to select it (yellow highlight)
4. Drag to new location
5. Click **Save** (or Ctrl+S)

### Placing New Objects

1. Click **Place** button (or press P)
2. Click on map where you want the object
3. Object appears with ID 0 (needs proper ID assignment)
4. Switch to **Select** mode and adjust if needed
5. **Save** when done

### Deleting Objects

1. Click **Delete** button (or press D)
2. Click objects to remove them
3. Each click deletes the object under cursor
4. **Save** to commit changes

### Multi-Object Editing

1. Hold **Ctrl** and click multiple objects
2. Drag any selected object - all move together
3. Useful for repositioning groups of objects

## File Locations

NSB files are searched in order:

1. `DAT\` subdirectory (game installation layout)
2. Current working directory
3. Application directory
4. `..\..\DAT\` (relative paths)
5. Adjacent to executable

## Backup System

**Automatic backups** are created when saving:

- Original: `1.nsb`
- Backup: `1.nsb.backup`
- Backups are **never overwritten** - your first save is always preserved

## Safety Features

### ✅ Data Integrity

- Binary format strictly follows original specification
- Magic number validation (`BMOL` / `0x4C4F4D42`)
- Entry count auto-updated
- Object indices re-calculated on deletion

### ✅ User Protection

- Modified indicator prevents accidental data loss
- Automatic backup before first save
- Undo/Redo for experimentation
- Clone-based state management (no pointer issues)

## Limitations & Future Work

### Current Limitations

- **Object Type Selection** - Currently defaults to ID 0 when placing
  - *Workaround*: Place object, save, edit in hex editor, or use external tool
- **Field Meanings** - Fields 1, 3, 5 meanings unknown
- **No Terrain Editor** - Can't modify underlying terrain tiles
- **No Property Panel** - Can't edit object properties in UI (position only)

### Planned Features

- **Object Palette** - Browse and select from all OBJ.ojd entries
- **Property Panel** - Edit all 6 fields with validation
- **Copy/Paste** - Duplicate objects
- **Search/Filter** - Find objects by type or ID
- **Batch Operations** - Delete/move multiple types at once
- **Terrain Rendering** - Show actual terrain tiles from .SHM/.SHL files
- **Sprite Preview** - Show actual building/unit sprites
- **Snap-to-Grid** - Align objects to grid lines
- **Coordinate Display** - Show X/Y coords in real-time

## Troubleshooting

### "Map file not found"

- Ensure .nsb files (1.nsb through 30.nsb) are in `DAT\` folder
- Or copy them to the application directory

### "Could not load OBJ.ojd"

- Objects will show as "ID:####" instead of names
- Copy `OBJ.ojd` to application directory for name lookup

### Objects disappear after saving

- Check backup file (`*.nsb.backup`) - original preserved
- Verify Field4 values are valid object IDs from OBJ.ojd

### Undo not working

- Undo only tracks changes after first action in session
- Maximum 50 undo steps

## Technical Details

### Parser

- Uses `NsbParser` with `ReadOnlySpan<byte>` for performance
- Zero-allocation parsing
- Strict format validation

### Writer

- `BinaryWriter` with explicit field order
- 12-byte header: Reserved(4) + Magic(4) + EntryCount(4)
- 12-byte entries: 6× ushort fields

### State Management

- Deep cloning for undo/redo (no shared references)
- Stack-based undo (LIFO)
- Modified flag for save prompt

## Map Editor Progress

**Current Status: ~60% Complete**

### Completed (60%)

- ✅ NSB parsing and display
- ✅ Object selection and highlighting
- ✅ Drag-and-drop object movement
- ✅ Multi-select with Ctrl
- ✅ Zoom and pan controls
- ✅ Grid and labels
- ✅ Undo/Redo system (50 steps)
- ✅ Save with auto-backup
- ✅ Object placement and deletion
- ✅ Keyboard shortcuts
- ✅ OBJ.ojd name lookups

### In Progress (30%)

- 🔄 Object type selection for placement
- 🔄 Property editing panel
- 🔄 Field meaning identification

### Planned (10%)

- ⏳ Terrain rendering
- ⏳ Sprite preview
- ⏳ Copy/paste
- ⏳ Search and filter
- ⏳ Batch operations

## Contributing

When modifying the editor, maintain these patterns:

1. **Undo-able Actions** - Call `SaveUndoState()` before modifications
2. **Modified Flag** - Set `isModified = true` after changes
3. **Re-indexing** - Update `Index` fields after add/delete
4. **Entry Count** - Keep `EntryCount` in sync with `Objects.Count`
5. **Validation** - Check `currentMapData != null` in all handlers

## References

- **Architecture**: `.github/copilot-instructions.md`
- **NSB Format**: `Parsers/IMPROVEMENTS.md`
- **Object IDs**: `OBJ.ojd` (binary), `OBJ.-dump.csv` (readable)
- **Reverse Engineering**: `WoWDecomp/ida-notes-thor/obj-ojd.txt`

---

**Status**: Functional Level Editor
**Last Updated**: November 13, 2025
**Progress**: Map Viewer (40%) → **Level Editor (60%)**
