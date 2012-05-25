using pr.gui;

namespace test.test.ui
{
	partial class MainForm
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			pr.gui.ViewImageControl.QualityModes qualityModes1 = new pr.gui.ViewImageControl.QualityModes();
			pr.gui.VT100Settings vT100Settings1 = new pr.gui.VT100Settings();
			pr.gui.DockContainer.StyleData styleData1 = new pr.gui.DockContainer.StyleData();
			this.m_view3d = new pr.gui.View3D();
			this.m_view_image_control = new pr.gui.ViewImageControl();
			this.m_graph = new pr.gui.GraphControl();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.m_vt100 = new pr.gui.VT100();
			this.m_btn_chore = new System.Windows.Forms.Button();
			this.m_view_video_control = new pr.gui.ViewVideoControl();
			this.m_tree_grid = new pr.gui.TreeGridView();
			this.m_dock = new pr.gui.DockContainer();
			this.Column1 = new pr.gui.TreeGridColumn();
			this.Column2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Column3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			this.Tree = new pr.gui.TreeGridColumn();
			this.Data1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Data2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_open = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_recent_files = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.graphControl1 = new pr.gui.GraphControl();
			((System.ComponentModel.ISupportInitialize)(this.m_graph)).BeginInit();
			this.tableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_vt100)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_tree_grid)).BeginInit();
			this.m_menu.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.graphControl1)).BeginInit();
			this.SuspendLayout();
			// 
			// m_view3d
			// 
			this.m_view3d.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_view3d.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_view3d.ClickTimeMS = 180;
			this.m_view3d.Location = new System.Drawing.Point(3, 3);
			this.m_view3d.Name = "m_view3d";
			this.m_view3d.RenderMode = pr.gui.View3D.ERenderMode.Solid;
			this.m_view3d.Size = new System.Drawing.Size(324, 251);
			this.m_view3d.TabIndex = 2;
			// 
			// m_view_image_control
			// 
			this.m_view_image_control.AdditionalInputKeys = null;
			this.m_view_image_control.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_view_image_control.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_view_image_control.Centre = new System.Drawing.Point(75, 60);
			this.m_view_image_control.DblBuffered = true;
			this.m_view_image_control.EnableDragging = true;
			this.m_view_image_control.ErrorImage = ((System.Drawing.Image)(resources.GetObject("m_view_image_control.ErrorImage")));
			this.m_view_image_control.Image = ((System.Drawing.Image)(resources.GetObject("m_view_image_control.Image")));
			this.m_view_image_control.ImageVisible = true;
			this.m_view_image_control.Location = new System.Drawing.Point(333, 260);
			this.m_view_image_control.Name = "m_view_image_control";
			qualityModes1.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
			qualityModes1.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			qualityModes1.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
			qualityModes1.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
			this.m_view_image_control.Quality = qualityModes1;
			this.m_view_image_control.Rotation = System.Drawing.RotateFlipType.RotateNoneFlipNone;
			this.m_view_image_control.Size = new System.Drawing.Size(324, 251);
			this.m_view_image_control.TabIndex = 0;
			this.m_view_image_control.Zoom = 1F;
			// 
			// m_graph
			// 
			this.m_graph.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_graph.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_graph.Centre = ((System.Drawing.PointF)(resources.GetObject("m_graph.Centre")));
			this.m_graph.Location = new System.Drawing.Point(3, 260);
			this.m_graph.Name = "m_graph";
			this.m_graph.Size = new System.Drawing.Size(324, 251);
			this.m_graph.TabIndex = 5;
			this.m_graph.Title = "";
			this.m_graph.Zoom = 1F;
			this.m_graph.ZoomMax = 3.402823E+38F;
			this.m_graph.ZoomMin = 1.401298E-45F;
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 3;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.tableLayoutPanel1.Controls.Add(this.m_graph, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_vt100, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.m_btn_chore, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.m_view_image_control, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.m_view3d, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.m_view_video_control, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.m_tree_grid, 2, 0);
			this.tableLayoutPanel1.Controls.Add(this.m_dock, 2, 1);
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 24);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(991, 771);
			this.tableLayoutPanel1.TabIndex = 6;
			// 
			// m_vt100
			// 
			this.m_vt100.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			this.m_vt100.CurrentLine = 0;
			this.m_vt100.CursorLocation = new System.Drawing.Point(0, 0);
			this.m_vt100.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_vt100.Font = new System.Drawing.Font("Courier New", 10F);
			this.m_vt100.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			this.m_vt100.HexOutput = false;
			this.m_vt100.HideSelection = false;
			this.m_vt100.LocalEcho = true;
			this.m_vt100.Location = new System.Drawing.Point(3, 517);
			this.m_vt100.Name = "m_vt100";
			this.m_vt100.NewlineRecv = pr.gui.VT100Settings.ENewLineMode.LF;
			this.m_vt100.NewlineSend = pr.gui.VT100Settings.ENewLineMode.CR;
			this.m_vt100.OutputCursorLocation = new System.Drawing.Point(0, 0);
			vT100Settings1.BackColour = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
			vT100Settings1.ForeColour = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
			vT100Settings1.HexOutput = false;
			vT100Settings1.LocalEcho = true;
			vT100Settings1.NewlineRecv = pr.gui.VT100Settings.ENewLineMode.LF;
			vT100Settings1.NewlineSend = pr.gui.VT100Settings.ENewLineMode.CR;
			vT100Settings1.TabSize = 8;
			vT100Settings1.TerminalWidth = 100;
			vT100Settings1.XML = resources.GetString("vT100Settings1.XML");
			this.m_vt100.Settings = vT100Settings1;
			this.m_vt100.Size = new System.Drawing.Size(324, 251);
			this.m_vt100.TabIndex = 7;
			this.m_vt100.TabSize = 8;
			this.m_vt100.TerminalWidth = 100;
			this.m_vt100.Text = "";
			this.m_vt100.WordWrap = false;
			// 
			// m_btn_chore
			// 
			this.m_btn_chore.Location = new System.Drawing.Point(333, 517);
			this.m_btn_chore.Name = "m_btn_chore";
			this.m_btn_chore.Size = new System.Drawing.Size(75, 23);
			this.m_btn_chore.TabIndex = 6;
			this.m_btn_chore.Text = "Chore";
			this.m_btn_chore.UseVisualStyleBackColor = true;
			// 
			// m_view_video_control
			// 
			this.m_view_video_control.AdditionalInputKeys = null;
			this.m_view_video_control.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_view_video_control.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_view_video_control.FitToWindow = true;
			this.m_view_video_control.Location = new System.Drawing.Point(333, 3);
			this.m_view_video_control.Name = "m_view_video_control";
			this.m_view_video_control.RemoteAutoHidePeriod = 1500;
			this.m_view_video_control.RemoteAutoHideSpeed = 400;
			this.m_view_video_control.RemoteVisible = false;
			this.m_view_video_control.Size = new System.Drawing.Size(324, 251);
			this.m_view_video_control.TabIndex = 8;
			this.m_view_video_control.Video = null;
			// 
			// m_tree_grid
			// 
			this.m_tree_grid.AllowUserToAddRows = false;
			this.m_tree_grid.AllowUserToDeleteRows = false;
			this.m_tree_grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_tree_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tree_grid.ImageList = null;
			this.m_tree_grid.Location = new System.Drawing.Point(663, 3);
			this.m_tree_grid.Name = "m_tree_grid";
			this.m_tree_grid.NodeCount = 0;
			this.m_tree_grid.RowHeadersVisible = false;
			this.m_tree_grid.ShowLines = true;
			this.m_tree_grid.Size = new System.Drawing.Size(325, 251);
			this.m_tree_grid.TabIndex = 9;
			this.m_tree_grid.VirtualNodes = false;
			// 
			// m_dock
			// 
			this.m_dock.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_dock.BackColor = System.Drawing.SystemColors.ControlDark;
			this.m_dock.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_dock.Location = new System.Drawing.Point(663, 260);
			this.m_dock.Name = "m_dock";
			this.m_dock.Size = new System.Drawing.Size(325, 251);
			styleData1.MarginSize = 80;
			this.m_dock.Style = styleData1;
			this.m_dock.TabIndex = 10;
			// 
			// Column1
			// 
			this.Column1.DefaultNodeImage = null;
			this.Column1.HeaderText = "Column1";
			this.Column1.Name = "Column1";
			this.Column1.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.Column1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// Column2
			// 
			this.Column2.HeaderText = "Column2";
			this.Column2.Name = "Column2";
			this.Column2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// Column3
			// 
			this.Column3.HeaderText = "Column3";
			this.Column3.Name = "Column3";
			this.Column3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "clicket.ico");
			this.m_image_list.Images.SetKeyName(1, "Smiling gekko 150x121.jpg");
			// 
			// Tree
			// 
			this.Tree.DefaultNodeImage = null;
			this.Tree.HeaderText = "Tree";
			this.Tree.Name = "Tree";
			this.Tree.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// Data1
			// 
			this.Data1.HeaderText = "Data1";
			this.Data1.Name = "Data1";
			this.Data1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// Data2
			// 
			this.Data2.HeaderText = "Data2";
			this.Data2.Name = "Data2";
			this.Data2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
			// 
			// m_menu
			// 
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(1003, 24);
			this.m_menu.TabIndex = 6;
			this.m_menu.Text = "menuStrip1";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_open,
            this.toolStripSeparator1,
            this.m_menu_file_recent_files,
            this.toolStripSeparator2,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_open
			// 
			this.m_menu_file_open.Name = "m_menu_file_open";
			this.m_menu_file_open.Size = new System.Drawing.Size(136, 22);
			this.m_menu_file_open.Text = "&Open";
			this.m_menu_file_open.Click += new System.EventHandler(this.OnFileOpen);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(133, 6);
			// 
			// m_menu_file_recent_files
			// 
			this.m_menu_file_recent_files.Name = "m_menu_file_recent_files";
			this.m_menu_file_recent_files.Size = new System.Drawing.Size(136, 22);
			this.m_menu_file_recent_files.Text = "Recent Files";
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(133, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(136, 22);
			this.m_menu_file_exit.Text = "E&xit";
			this.m_menu_file_exit.Click += new System.EventHandler(this.OnExit);
			// 
			// graphControl1
			// 
			this.graphControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.graphControl1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.graphControl1.Centre = ((System.Drawing.PointF)(resources.GetObject("graphControl1.Centre")));
			this.graphControl1.Location = new System.Drawing.Point(3, 199);
			this.graphControl1.Name = "graphControl1";
			this.graphControl1.Size = new System.Drawing.Size(227, 190);
			this.graphControl1.TabIndex = 5;
			this.graphControl1.Title = "";
			this.graphControl1.Zoom = 1F;
			this.graphControl1.ZoomMax = 3.402823E+38F;
			this.graphControl1.ZoomMin = 1.401298E-45F;
			// 
			// MainForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1003, 807);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.m_menu);
			this.MainMenuStrip = this.m_menu;
			this.Name = "MainForm";
			this.Text = "MainForm";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
			((System.ComponentModel.ISupportInitialize)(this.m_graph)).EndInit();
			this.tableLayoutPanel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_vt100)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_tree_grid)).EndInit();
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.graphControl1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private ViewImageControl m_view_image_control;
		private View3D m_view3d;
		private GraphControl m_graph;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.MenuStrip m_menu;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_recent_files;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_open;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem m_menu_file_exit;
		private GraphControl graphControl1;
		private System.Windows.Forms.Button m_btn_chore;
		private VT100 m_vt100;
		private ViewVideoControl m_view_video_control;
		private TreeGridColumn Tree;
		private System.Windows.Forms.DataGridViewTextBoxColumn Data1;
		private System.Windows.Forms.DataGridViewTextBoxColumn Data2;
		private System.Windows.Forms.ImageList m_image_list;
		private TreeGridColumn Column1;
		private System.Windows.Forms.DataGridViewTextBoxColumn Column2;
		private System.Windows.Forms.DataGridViewTextBoxColumn Column3;
		private TreeGridView m_tree_grid;
		private DockContainer m_dock;
	}
}