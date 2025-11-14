namespace WoWViewer
{
    partial class MapEditorForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MapEditorForm));
            toolStrip1 = new ToolStrip();
            btnPrevious = new ToolStripButton();
            btnNext = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            btnZoomIn = new ToolStripButton();
            btnZoomOut = new ToolStripButton();
            btnZoomReset = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            btnToggleGrid = new ToolStripButton();
            btnToggleLabels = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            lblZoomLevel = new ToolStripLabel();
            toolStripSeparator4 = new ToolStripSeparator();
            btnSelect = new ToolStripButton();
            btnPlace = new ToolStripButton();
            btnDelete = new ToolStripButton();
            toolStripSeparator5 = new ToolStripSeparator();
            btnSave = new ToolStripButton();
            button1 = new Button();
            button2 = new Button();
            comboBox1 = new ComboBox();
            pictureBox1 = new PictureBox();
            label1 = new Label();
            label2 = new Label();
            toolStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // toolStrip1
            // 
            toolStrip1.Items.AddRange(new ToolStripItem[] { btnPrevious, btnNext, toolStripSeparator1, btnZoomIn, btnZoomOut, btnZoomReset, toolStripSeparator2, btnToggleGrid, btnToggleLabels, toolStripSeparator3, lblZoomLevel, toolStripSeparator4, btnSelect, btnPlace, btnDelete, toolStripSeparator5, btnSave });
            toolStrip1.Location = new Point(0, 0);
            toolStrip1.Name = "toolStrip1";
            toolStrip1.Size = new Size(800, 25);
            toolStrip1.TabIndex = 6;
            toolStrip1.Text = "toolStrip1";
            // 
            // btnPrevious
            // 
            btnPrevious.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnPrevious.Name = "btnPrevious";
            btnPrevious.Size = new Size(70, 22);
            btnPrevious.Text = "◀ Previous";
            btnPrevious.ToolTipText = "Previous Sector (Ctrl+Left)";
            btnPrevious.Click += btnPrevious_Click;
            // 
            // btnNext
            // 
            btnNext.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnNext.Name = "btnNext";
            btnNext.Size = new Size(56, 22);
            btnNext.Text = "Next ▶";
            btnNext.ToolTipText = "Next Sector (Ctrl+Right)";
            btnNext.Click += btnNext_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 25);
            // 
            // btnZoomIn
            // 
            btnZoomIn.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnZoomIn.Name = "btnZoomIn";
            btnZoomIn.Size = new Size(23, 22);
            btnZoomIn.Text = "➕";
            btnZoomIn.ToolTipText = "Zoom In (Ctrl++)";
            btnZoomIn.Click += btnZoomIn_Click;
            // 
            // btnZoomOut
            // 
            btnZoomOut.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnZoomOut.Name = "btnZoomOut";
            btnZoomOut.Size = new Size(23, 22);
            btnZoomOut.Text = "➖";
            btnZoomOut.ToolTipText = "Zoom Out (Ctrl+-)";
            btnZoomOut.Click += btnZoomOut_Click;
            // 
            // btnZoomReset
            // 
            btnZoomReset.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnZoomReset.Name = "btnZoomReset";
            btnZoomReset.Size = new Size(60, 22);
            btnZoomReset.Text = "🔍 Reset";
            btnZoomReset.ToolTipText = "Reset Zoom (Ctrl+0)";
            btnZoomReset.Click += btnZoomReset_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 25);
            // 
            // btnToggleGrid
            // 
            btnToggleGrid.CheckOnClick = true;
            btnToggleGrid.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnToggleGrid.Name = "btnToggleGrid";
            btnToggleGrid.Size = new Size(32, 22);
            btnToggleGrid.Text = "Grid";
            btnToggleGrid.ToolTipText = "Toggle Grid (G)";
            btnToggleGrid.Click += btnToggleGrid_Click;
            // 
            // btnToggleLabels
            // 
            btnToggleLabels.CheckOnClick = true;
            btnToggleLabels.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnToggleLabels.Name = "btnToggleLabels";
            btnToggleLabels.Size = new Size(44, 22);
            btnToggleLabels.Text = "Labels";
            btnToggleLabels.ToolTipText = "Toggle Labels (L)";
            btnToggleLabels.Click += btnToggleLabels_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 25);
            // 
            // lblZoomLevel
            // 
            lblZoomLevel.Name = "lblZoomLevel";
            lblZoomLevel.Size = new Size(43, 22);
            lblZoomLevel.Text = "100%";
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 25);
            // 
            // btnSelect
            // 
            btnSelect.Checked = true;
            btnSelect.CheckOnClick = true;
            btnSelect.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnSelect.Name = "btnSelect";
            btnSelect.Size = new Size(42, 22);
            btnSelect.Text = "Select";
            btnSelect.ToolTipText = "Select Mode - Click to select and drag objects (S)";
            btnSelect.Click += btnSelect_Click;
            // 
            // btnPlace
            // 
            btnPlace.CheckOnClick = true;
            btnPlace.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnPlace.Name = "btnPlace";
            btnPlace.Size = new Size(38, 22);
            btnPlace.Text = "Place";
            btnPlace.ToolTipText = "Place Mode - Click to place new objects (P)";
            btnPlace.Click += btnPlace_Click;
            // 
            // btnDelete
            // 
            btnDelete.CheckOnClick = true;
            btnDelete.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(44, 22);
            btnDelete.Text = "Delete";
            btnDelete.ToolTipText = "Delete Mode - Click objects to delete them (D)";
            btnDelete.Click += btnDelete_Click;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            toolStripSeparator5.Size = new Size(6, 25);
            // 
            // btnSave
            // 
            btnSave.DisplayStyle = ToolStripItemDisplayStyle.Text;
            btnSave.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(35, 22);
            btnSave.Text = "💾 Save";
            btnSave.ToolTipText = "Save changes to NSB file (Ctrl+S)";
            btnSave.Click += btnSave_Click;
            // 
            // button1
            // 
            button1.Location = new Point(12, 66);
            button1.Name = "button1";
            button1.Size = new Size(120, 30);
            button1.TabIndex = 0;
            button1.Text = "Load Selected Map";
            button1.UseVisualStyleBackColor = true;
            button1.Click += button1_Click;
            // 
            // button2
            // 
            button2.Location = new Point(138, 66);
            button2.Name = "button2";
            button2.Size = new Size(120, 30);
            button2.TabIndex = 1;
            button2.Text = "Export All Maps";
            button2.UseVisualStyleBackColor = true;
            button2.Click += button2_Click;
            // 
            // comboBox1
            // 
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox1.FormattingEnabled = true;
            comboBox1.Location = new Point(12, 37);
            comboBox1.Name = "comboBox1";
            comboBox1.Size = new Size(246, 23);
            comboBox1.TabIndex = 2;
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            // 
            // pictureBox1
            // 
            pictureBox1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pictureBox1.BackColor = Color.Black;
            pictureBox1.BorderStyle = BorderStyle.FixedSingle;
            pictureBox1.Location = new Point(12, 132);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(776, 306);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 109);
            label1.Name = "label1";
            label1.Size = new Size(143, 15);
            label1.TabIndex = 4;
            label1.Text = "Select a sector to load...";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(264, 40);
            label2.Name = "label2";
            label2.Size = new Size(88, 15);
            label2.TabIndex = 5;
            label2.Text = "Sector 1 selected";
            // 
            // MapEditorForm
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(pictureBox1);
            Controls.Add(comboBox1);
            Controls.Add(button2);
            Controls.Add(button1);
            Controls.Add(toolStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            KeyPreview = true;
            Name = "MapEditorForm";
            Text = "Map Editor - War of the Worlds";
            KeyDown += MapEditorForm_KeyDown;
            toolStrip1.ResumeLayout(false);
            toolStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip1;
        private ToolStripButton btnPrevious;
        private ToolStripButton btnNext;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton btnZoomIn;
        private ToolStripButton btnZoomOut;
        private ToolStripButton btnZoomReset;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton btnToggleGrid;
        private ToolStripButton btnToggleLabels;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripLabel lblZoomLevel;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton btnSelect;
        private ToolStripButton btnPlace;
        private ToolStripButton btnDelete;
        private ToolStripSeparator toolStripSeparator5;
        private ToolStripButton btnSave;
        private Button button1;
        private Button button2;
        private ComboBox comboBox1;
        private PictureBox pictureBox1;
        private Label label1;
        private Label label2;
    }
}