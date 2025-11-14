using System.Text;
using WoWViewer.Parsers;

namespace WoWViewer
{
    public partial class MapEditorForm : Form
    {
        private NsbMapData? currentMapData;
        private int currentSectorIndex = 1;
        private const int MAP_SCALE = 2;  // Scale factor for visualization
        private float zoomLevel = 1.0f;
        private bool showGrid = false;
        private bool showLabels = false;
        private Dictionary<ushort, string> objectNames = new Dictionary<ushort, string>();

        // Editing state
        private NsbMapObject? selectedObject = null;
        private List<NsbMapObject> selectedObjects = new List<NsbMapObject>();
        private bool isDragging = false;
        private Point lastMousePos;
        private Point panOffset = new Point(0, 0);
        private bool isModified = false;
        
        // Undo/Redo system
        private Stack<NsbMapData> undoStack = new Stack<NsbMapData>();
        private Stack<NsbMapData> redoStack = new Stack<NsbMapData>();
        
        // Edit modes
        private EditMode currentEditMode = EditMode.Select;
        private ushort selectedObjectTypeToPlace = 0;
        
        private enum EditMode
        {
            Select,     // Select and move objects
            Place,      // Place new objects
            Delete      // Delete objects
        }

        public MapEditorForm()
        {
            InitializeComponent();
            LoadSectorNames();
            LoadObjectNames();
            
            // Set up mouse events for editing
            pictureBox1.MouseDown += PictureBox1_MouseDown;
            pictureBox1.MouseMove += PictureBox1_MouseMove;
            pictureBox1.MouseUp += PictureBox1_MouseUp;
            pictureBox1.MouseWheel += PictureBox1_MouseWheel;
        }

        private void LoadObjectNames()
        {
            try
            {
                // Try to find OBJ.ojd in multiple locations
                string[] possiblePaths = new[]
                {
                    "OBJ.ojd",
                    "..\\OBJ.ojd",
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OBJ.ojd")
                };

                string? objPath = null;
                foreach (var path in possiblePaths)
                {
                    if (File.Exists(path))
                    {
                        objPath = path;
                        break;
                    }
                }

                if (objPath != null)
                {
                    var entries = ObjOjdParser.Parse(objPath);
                    foreach (var entry in entries)
                    {
                        if (!objectNames.ContainsKey(entry.Id))
                        {
                            objectNames[entry.Id] = entry.Name;
                        }
                    }
                    label1.Text = $"Loaded {objectNames.Count} object definitions from OBJ.ojd";
                }
                else
                {
                    label1.Text = "OBJ.ojd not found - object IDs will be shown instead of names";
                }
            }
            catch (Exception ex)
            {
                label1.Text = $"Warning: Could not load OBJ.ojd - {ex.Message}";
            }
        }

        private void LoadSectorNames()
        {
            // Populate combo box with sector numbers
            for (int i = 1; i <= 30; i++)
            {
                comboBox1.Items.Add($"Sector {i}");
            }
            comboBox1.SelectedIndex = 0;
        }

        // Load Selected Map Button
        private void button1_Click(object sender, EventArgs e)
        {
            LoadMap(currentSectorIndex);
        }

        // Export All Maps Button
        private void button2_Click(object sender, EventArgs e)
        {
            ExportAllMaps();
        }

        // Sector selection changed
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            currentSectorIndex = comboBox1.SelectedIndex + 1;
            label2.Text = $"Sector {currentSectorIndex} selected";
        }

        /// <summary>
        /// Find NSB file in multiple possible locations.
        /// </summary>
        private string? FindNsbFile(int sectorNumber)
        {
            string[] possiblePaths = new[]
            {
                $"DAT\\{sectorNumber}.nsb",           // Subdirectory (installed game)
                $"{sectorNumber}.nsb",                 // Current directory
                $"..\\DAT\\{sectorNumber}.nsb",       // Parent's DAT folder
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DAT", $"{sectorNumber}.nsb"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{sectorNumber}.nsb")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        /// <summary>
        /// Load and display a single NSB map file.
        /// </summary>
        private void LoadMap(int sectorNumber)
        {
            try
            {
                string? filePath = FindNsbFile(sectorNumber);
                
                if (filePath == null)
                {
                    MessageBox.Show($"Map file not found: {sectorNumber}.nsb\n\nSearched in:\n" +
                        $"- DAT\\ subdirectory\n" +
                        $"- Current directory\n" +
                        $"- Application directory\n\n" +
                        $"Please ensure the .nsb files are in one of these locations.",
                        "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                currentMapData = NsbParser.Parse(filePath);
                
                label1.Text = $"Loaded: Sector {sectorNumber} ({currentMapData.EntryCount} objects)";
                
                // Visualize the map
                RenderMap();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading map: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Render the map objects to the picture box.
        /// </summary>
        private void RenderMap()
        {
            if (currentMapData == null || pictureBox1 == null)
                return;

            // Create bitmap for visualization
            int width = pictureBox1.Width;
            int height = pictureBox1.Height;
            var bitmap = new Bitmap(width, height);

            using (var g = Graphics.FromImage(bitmap))
            {
                g.Clear(Color.Black);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Find bounds for scaling
                ushort minX = ushort.MaxValue, maxX = 0;
                ushort minY = ushort.MaxValue, maxY = 0;

                foreach (var obj in currentMapData.Objects)
                {
                    if (obj.Field0 < minX) minX = obj.Field0;
                    if (obj.Field0 > maxX) maxX = obj.Field0;
                    if (obj.Field2 < minY) minY = obj.Field2;
                    if (obj.Field2 > maxY) maxY = obj.Field2;
                }

                float scaleX = (maxX - minX) > 0 ? (width - 40) / (float)(maxX - minX) : 1;
                float scaleY = (maxY - minY) > 0 ? (height - 40) / (float)(maxY - minY) : 1;
                float scale = Math.Min(scaleX, scaleY) * zoomLevel;

                // Draw grid if enabled
                if (showGrid)
                {
                    using (var gridPen = new Pen(Color.FromArgb(50, 100, 100, 100), 1))
                    {
                        int gridSize = 50;
                        for (int x = 0; x < width; x += gridSize)
                            g.DrawLine(gridPen, x, 0, x, height);
                        for (int y = 0; y < height; y += gridSize)
                            g.DrawLine(gridPen, 0, y, width, y);
                    }
                }

                // Draw objects
                int objectCount = 0;
                foreach (var obj in currentMapData.Objects)
                {
                    float x = (obj.Field0 - minX) * scale + 20 + panOffset.X;
                    float y = (obj.Field2 - minY) * scale + 20 + panOffset.Y;

                    // Check if object is selected
                    bool isSelected = selectedObjects.Contains(obj);

                    // Color by object type (Field4)
                    Color objColor = GetObjectColor(obj.Field4);
                    float dotSize = 3 * zoomLevel;
                    
                    // Draw selection highlight
                    if (isSelected)
                    {
                        float highlightSize = dotSize + 4;
                        g.FillEllipse(new SolidBrush(Color.Yellow), x - highlightSize/2, y - highlightSize/2, highlightSize, highlightSize);
                    }
                    
                    g.FillEllipse(new SolidBrush(objColor), x - dotSize/2, y - dotSize/2, dotSize, dotSize);

                    // Draw labels if enabled (limit to prevent clutter)
                    if ((showLabels || isSelected) && objectCount < 100)
                    {
                        string label = GetObjectName(obj.Field4);
                        using (var font = new Font("Arial", Math.Max(6, 6 * zoomLevel)))
                        {
                            // Draw text with background for readability
                            var textSize = g.MeasureString(label, font);
                            g.FillRectangle(new SolidBrush(Color.FromArgb(200, 0, 0, 0)), 
                                x + dotSize, y, textSize.Width, textSize.Height);
                            g.DrawString(label, font, Brushes.White, x + dotSize, y);
                        }
                    }
                    objectCount++;
                }

                // Draw info overlay
                using (var bgBrush = new SolidBrush(Color.FromArgb(180, 0, 0, 0)))
                {
                    g.FillRectangle(bgBrush, 5, 5, 300, 45);
                }
                g.DrawString($"Sector {currentSectorIndex}: {currentMapData.EntryCount} objects", 
                    new Font("Arial", 10, FontStyle.Bold), Brushes.White, 10, 10);
                g.DrawString($"Zoom: {zoomLevel * 100:F0}% | Grid: {(showGrid ? "On" : "Off")} | Labels: {(showLabels ? "On" : "Off")}", 
                    SystemFonts.DefaultFont, Brushes.LightGray, 10, 30);
            }

            pictureBox1.Image = bitmap;
        }

        /// <summary>
        /// Get object name by ID from OBJ.ojd.
        /// </summary>
        private string GetObjectName(ushort objectId)
        {
            if (objectNames.TryGetValue(objectId, out string? name))
            {
                // Clean up the path for display
                string cleanName = name.Replace("\\", " / ");
                return Path.GetFileNameWithoutExtension(cleanName);
            }
            return $"ID:{objectId}";
        }

        /// <summary>
        /// Get color based on object type ID.
        /// </summary>
        private Color GetObjectColor(ushort objectType)
        {
            // Color mapping - this is speculative and needs game data analysis
            return objectType switch
            {
                < 1000 => Color.Green,      // Terrain
                < 5000 => Color.Yellow,     // Buildings
                < 10000 => Color.Red,       // Units
                _ => Color.Cyan             // Unknown
            };
        }

        /// <summary>
        /// Export all 30 NSB maps to text files for analysis.
        /// </summary>
        private void ExportAllMaps()
        {
            try
            {
                // Find where the NSB files are located
                string? firstFile = FindNsbFile(1);
                if (firstFile == null)
                {
                    MessageBox.Show("Could not find any .nsb files. Please ensure they are in the DAT folder or current directory.",
                        "Files Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string directory = Path.GetDirectoryName(firstFile) ?? ".";
                int exportCount = 0;

                for (int i = 1; i <= 30; i++)
                {
                    string? nsbFile = FindNsbFile(i);
                    if (nsbFile != null)
                    {
                        NsbParser.ExportToLog(nsbFile);
                        exportCount++;
                    }
                }

                MessageBox.Show($"Successfully exported {exportCount} map files to:\n{directory}", 
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting maps: {ex.Message}", 
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Toolbar Navigation Events
        private void btnPrevious_Click(object sender, EventArgs e)
        {
            if (currentSectorIndex > 1)
            {
                currentSectorIndex--;
                comboBox1.SelectedIndex = currentSectorIndex - 1;
                LoadMap(currentSectorIndex);
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (currentSectorIndex < 30)
            {
                currentSectorIndex++;
                comboBox1.SelectedIndex = currentSectorIndex - 1;
                LoadMap(currentSectorIndex);
            }
        }

        // Zoom Controls
        private void btnZoomIn_Click(object sender, EventArgs e)
        {
            zoomLevel = Math.Min(zoomLevel * 1.25f, 5.0f);
            UpdateZoomLabel();
            RenderMap();
        }

        private void btnZoomOut_Click(object sender, EventArgs e)
        {
            zoomLevel = Math.Max(zoomLevel / 1.25f, 0.25f);
            UpdateZoomLabel();
            RenderMap();
        }

        private void btnZoomReset_Click(object sender, EventArgs e)
        {
            zoomLevel = 1.0f;
            UpdateZoomLabel();
            RenderMap();
        }

        private void UpdateZoomLabel()
        {
            lblZoomLevel.Text = $"{zoomLevel * 100:F0}%";
        }

        // Toggle Features
        private void btnToggleGrid_Click(object sender, EventArgs e)
        {
            showGrid = btnToggleGrid.Checked;
            RenderMap();
        }

        private void btnToggleLabels_Click(object sender, EventArgs e)
        {
            showLabels = btnToggleLabels.Checked;
            RenderMap();
        }

        // Edit Mode Buttons
        private void btnSelect_Click(object sender, EventArgs e)
        {
            currentEditMode = EditMode.Select;
            btnSelect.Checked = true;
            btnPlace.Checked = false;
            btnDelete.Checked = false;
            pictureBox1.Cursor = Cursors.Default;
            label1.Text = "Select Mode - Click and drag to move objects";
        }

        private void btnPlace_Click(object sender, EventArgs e)
        {
            currentEditMode = EditMode.Place;
            btnSelect.Checked = false;
            btnPlace.Checked = true;
            btnDelete.Checked = false;
            pictureBox1.Cursor = Cursors.Cross;
            label1.Text = "Place Mode - Click to place new objects (Object Type: " + selectedObjectTypeToPlace + ")";
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            currentEditMode = EditMode.Delete;
            btnSelect.Checked = false;
            btnPlace.Checked = false;
            btnDelete.Checked = true;
            pictureBox1.Cursor = Cursors.No;
            label1.Text = "Delete Mode - Click objects to delete them";
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveCurrentMap();
        }

        // Keyboard Shortcuts
        private void MapEditorForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control)
            {
                switch (e.KeyCode)
                {
                    case Keys.Left:
                        btnPrevious_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.Right:
                        btnNext_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.Add:
                    case Keys.Oemplus:
                        btnZoomIn_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.Subtract:
                    case Keys.OemMinus:
                        btnZoomOut_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.D0:
                    case Keys.NumPad0:
                        btnZoomReset_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.S:
                        SaveCurrentMap();
                        e.Handled = true;
                        break;
                    case Keys.Z:
                        Undo();
                        e.Handled = true;
                        break;
                    case Keys.Y:
                        Redo();
                        e.Handled = true;
                        break;
                }
            }
            else
            {
                switch (e.KeyCode)
                {
                    case Keys.G:
                        btnToggleGrid.Checked = !btnToggleGrid.Checked;
                        btnToggleGrid_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.L:
                        btnToggleLabels.Checked = !btnToggleLabels.Checked;
                        btnToggleLabels_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.S:
                        btnSelect_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.P:
                        btnPlace_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.D:
                        btnDelete_Click(sender, e);
                        e.Handled = true;
                        break;
                    case Keys.Delete:
                        if (selectedObject != null)
                        {
                            DeleteObject(selectedObject);
                            e.Handled = true;
                        }
                        break;
                }
            }
        }

        #region Mouse Interaction Handlers

        private void PictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (currentMapData == null) return;

            if (e.Button == MouseButtons.Left)
            {
                var clickedObject = FindObjectAtPoint(e.Location);
                
                switch (currentEditMode)
                {
                    case EditMode.Select:
                        if (clickedObject != null)
                        {
                            // Multi-select with Ctrl
                            if (ModifierKeys.HasFlag(Keys.Control))
                            {
                                if (selectedObjects.Contains(clickedObject))
                                    selectedObjects.Remove(clickedObject);
                                else
                                    selectedObjects.Add(clickedObject);
                            }
                            else
                            {
                                selectedObjects.Clear();
                                selectedObjects.Add(clickedObject);
                            }
                            selectedObject = clickedObject;
                            isDragging = true;
                            lastMousePos = e.Location;
                            UpdatePropertyDisplay();
                        }
                        else
                        {
                            // Clicked empty space - clear selection
                            selectedObjects.Clear();
                            selectedObject = null;
                            UpdatePropertyDisplay();
                        }
                        RenderMap();
                        break;
                        
                    case EditMode.Place:
                        PlaceNewObject(e.Location);
                        break;
                        
                    case EditMode.Delete:
                        if (clickedObject != null)
                        {
                            DeleteObject(clickedObject);
                        }
                        break;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Right-click to pan
                lastMousePos = e.Location;
            }
        }

        private void PictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (currentMapData == null) return;

            if (e.Button == MouseButtons.Left && isDragging && selectedObject != null)
            {
                // Calculate movement delta
                int deltaX = (int)((e.X - lastMousePos.X) / zoomLevel / MAP_SCALE);
                int deltaY = (int)((e.Y - lastMousePos.Y) / zoomLevel / MAP_SCALE);

                // Move all selected objects
                foreach (var obj in selectedObjects)
                {
                    obj.Field0 = (ushort)Math.Clamp(obj.Field0 + deltaX, 0, ushort.MaxValue);
                    obj.Field2 = (ushort)Math.Clamp(obj.Field2 + deltaY, 0, ushort.MaxValue);
                }

                lastMousePos = e.Location;
                isModified = true;
                UpdatePropertyDisplay();
                RenderMap();
            }
            else if (e.Button == MouseButtons.Right)
            {
                // Pan the view
                panOffset.X += e.X - lastMousePos.X;
                panOffset.Y += e.Y - lastMousePos.Y;
                lastMousePos = e.Location;
                RenderMap();
            }
            else
            {
                // Update cursor based on hover
                var hoverObject = FindObjectAtPoint(e.Location);
                pictureBox1.Cursor = hoverObject != null ? Cursors.Hand : Cursors.Default;
            }
        }

        private void PictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                SaveUndoState();
            }
        }

        private void PictureBox1_MouseWheel(object sender, MouseEventArgs e)
        {
            // Zoom with mouse wheel
            if (e.Delta > 0)
                btnZoomIn_Click(sender, e);
            else if (e.Delta < 0)
                btnZoomOut_Click(sender, e);
        }

        #endregion

        #region Object Management

        private NsbMapObject? FindObjectAtPoint(Point screenPoint)
        {
            if (currentMapData == null) return null;

            // Convert screen coordinates to map coordinates
            float mapX = (screenPoint.X - panOffset.X) / (zoomLevel * MAP_SCALE);
            float mapY = (screenPoint.Y - panOffset.Y) / (zoomLevel * MAP_SCALE);

            // Find closest object within selection radius
            const int SELECTION_RADIUS = 10;
            NsbMapObject? closest = null;
            float closestDist = SELECTION_RADIUS / zoomLevel;

            foreach (var obj in currentMapData.Objects)
            {
                float dx = obj.Field0 - mapX;
                float dy = obj.Field2 - mapY;
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = obj;
                }
            }

            return closest;
        }

        private void PlaceNewObject(Point screenPoint)
        {
            if (currentMapData == null) return;

            // Convert screen to map coordinates
            ushort mapX = (ushort)Math.Clamp((screenPoint.X - panOffset.X) / (zoomLevel * MAP_SCALE), 0, ushort.MaxValue);
            ushort mapY = (ushort)Math.Clamp((screenPoint.Y - panOffset.Y) / (zoomLevel * MAP_SCALE), 0, ushort.MaxValue);

            var newObject = new NsbMapObject
            {
                Index = currentMapData.Objects.Count,
                Field0 = mapX,
                Field1 = 0,
                Field2 = mapY,
                Field3 = 0,
                Field4 = selectedObjectTypeToPlace,
                Field5 = 0
            };

            SaveUndoState();
            currentMapData.Objects.Add(newObject);
            currentMapData.EntryCount = currentMapData.Objects.Count;
            isModified = true;
            
            label1.Text = $"Placed object at ({mapX}, {mapY}) - Type: {selectedObjectTypeToPlace}";
            RenderMap();
        }

        private void DeleteObject(NsbMapObject obj)
        {
            if (currentMapData == null) return;

            SaveUndoState();
            currentMapData.Objects.Remove(obj);
            currentMapData.EntryCount = currentMapData.Objects.Count;
            
            // Re-index remaining objects
            for (int i = 0; i < currentMapData.Objects.Count; i++)
            {
                currentMapData.Objects[i].Index = i;
            }
            
            if (selectedObjects.Contains(obj))
                selectedObjects.Remove(obj);
            if (selectedObject == obj)
                selectedObject = null;
            
            isModified = true;
            label1.Text = $"Deleted object - {currentMapData.Objects.Count} objects remaining";
            UpdatePropertyDisplay();
            RenderMap();
        }

        private void UpdatePropertyDisplay()
        {
            if (selectedObject != null)
            {
                string objName = GetObjectName(selectedObject.Field4);
                label2.Text = $"Selected: {objName} | Pos: ({selectedObject.Field0}, {selectedObject.Field2}) | " +
                             $"F1:{selectedObject.Field1} F3:{selectedObject.Field3} F4:{selectedObject.Field4} F5:{selectedObject.Field5}";
            }
            else if (selectedObjects.Count > 1)
            {
                label2.Text = $"{selectedObjects.Count} objects selected";
            }
            else
            {
                label2.Text = $"Sector {currentSectorIndex}";
            }
        }

        #endregion

        #region Undo/Redo System

        private void SaveUndoState()
        {
            if (currentMapData == null) return;

            // Deep clone current state
            var clone = CloneMapData(currentMapData);
            undoStack.Push(clone);
            redoStack.Clear();
            
            // Limit undo stack size
            if (undoStack.Count > 50)
            {
                var temp = undoStack.ToArray();
                undoStack.Clear();
                for (int i = 0; i < 50; i++)
                    undoStack.Push(temp[i]);
            }
        }

        private void Undo()
        {
            if (undoStack.Count > 0 && currentMapData != null)
            {
                redoStack.Push(CloneMapData(currentMapData));
                currentMapData = undoStack.Pop();
                isModified = true;
                RenderMap();
                label1.Text = "Undo performed";
            }
        }

        private void Redo()
        {
            if (redoStack.Count > 0 && currentMapData != null)
            {
                undoStack.Push(CloneMapData(currentMapData));
                currentMapData = redoStack.Pop();
                isModified = true;
                RenderMap();
                label1.Text = "Redo performed";
            }
        }

        private NsbMapData CloneMapData(NsbMapData source)
        {
            var clone = new NsbMapData
            {
                EntryCount = source.EntryCount
            };

            foreach (var obj in source.Objects)
            {
                clone.Objects.Add(new NsbMapObject
                {
                    Index = obj.Index,
                    Field0 = obj.Field0,
                    Field1 = obj.Field1,
                    Field2 = obj.Field2,
                    Field3 = obj.Field3,
                    Field4 = obj.Field4,
                    Field5 = obj.Field5
                });
            }

            return clone;
        }

        #endregion

        #region File Operations

        private void SaveCurrentMap()
        {
            if (currentMapData == null || !isModified)
            {
                MessageBox.Show("No changes to save.", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                string? nsbPath = FindNsbFile(currentSectorIndex);
                if (nsbPath == null)
                {
                    MessageBox.Show("Could not find original NSB file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Backup original file
                string backupPath = nsbPath + ".backup";
                if (!File.Exists(backupPath))
                {
                    File.Copy(nsbPath, backupPath, false);
                }

                // Write modified NSB file
                WriteNsbFile(nsbPath, currentMapData);
                isModified = false;
                
                MessageBox.Show($"Saved sector {currentSectorIndex} successfully.\nBackup created: {Path.GetFileName(backupPath)}",
                    "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WriteNsbFile(string filePath, NsbMapData mapData)
        {
            using var writer = new BinaryWriter(File.Open(filePath, FileMode.Create));
            
            // Write header
            writer.Write((int)0);  // Reserved
            writer.Write((uint)0x4C4F4D42);  // "BMOL" magic
            writer.Write(mapData.Objects.Count);  // Entry count
            
            // Write objects
            foreach (var obj in mapData.Objects)
            {
                writer.Write(obj.Field0);
                writer.Write(obj.Field1);
                writer.Write(obj.Field2);
                writer.Write(obj.Field3);
                writer.Write(obj.Field4);
                writer.Write(obj.Field5);
            }
        }

        #endregion
    }
}
