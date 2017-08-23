using System;
using System.Drawing;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.util;

namespace LDraw
{
	public class ObjectManagerUI :ToolForm
	{
		#region UI Elements
		private SplitContainer m_split0;
		private TableLayoutPanel m_table0;
		private Panel m_panel0;
		private pr.gui.ComboBox m_cb_filter;
		private Button m_btn_collapse_all;
		private ImageList m_il_buttons;
		private Button m_btn_expand_all;
		private TreeGridView m_tree;
		private pr.gui.DataGridView m_grid;
		#endregion

		public ObjectManagerUI(SceneUI scene)
			:base(scene.TopLevelControl, EPin.Centre)
		{
			InitializeComponent();
			Text = $"Scene Manager - {scene.SceneName}";

			Scene = scene;
			Objects = new BindingSource<View3d.Object>();
			Siblings = new BindingSource<View3d.Object>();

			SetupUI();

			PopulateObjects();
		}
		protected override void Dispose(bool disposing)
		{
			Objects = null;
			Scene = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnShown(EventArgs e)
		{
			CenterToParent();
			base.OnShown(e);
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return Scene.Model; }
		}

		/// <summary>View 3D</summary>
		public View3d View3d
		{
			get { return Scene.View3d; }
		}

		/// <summary>The scene that owns this object manager</summary>
		public SceneUI Scene
		{
			get { return m_scene; }
			private set
			{
				if (m_scene == value) return;
				if (m_scene != null)
				{
					m_scene.Window.OnSceneChanged -= HandleSceneChanged;
				}
				m_scene = value;
				if (m_scene != null)
				{
					m_scene.Window.OnSceneChanged += HandleSceneChanged;
				}
			}
		}
		private SceneUI m_scene;
		private void HandleSceneChanged(object sender, EventArgs e)
		{
			PopulateObjects();
		}

		/// <summary>The collection of objects in this scene</summary>
		public BindingSource<View3d.Object> Objects
		{
			get { return m_objects; }
			private set
			{
				if (m_objects == value) return;
				if (m_objects != null)
				{
				}
				m_objects = value;
				if (m_objects != null)
				{
				}
			}
		}
		private BindingSource<View3d.Object> m_objects;

		/// <summary>The collection of sibling objects of the currently selected object</summary>
		public BindingSource<View3d.Object> Siblings { get; private set; }

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Filter
			m_btn_expand_all.Click += (s,a) =>
			{
				m_tree.ExpandAll();
			};
			m_btn_collapse_all.Click += (s,a) =>
			{
				m_tree.CollapseAll();
			};
			#endregion

			#region Tree
			m_tree.ShowLines = true;
			m_tree.VirtualNodes = false;
			m_tree.AutoGenerateColumns = false;
			m_tree.DataBinder = i => new TreeItem((View3d.Object)i);
			m_tree.Columns.Add(new TreeGridColumn
			{
				HeaderText = "Name",
				Name = nameof(View3d.Object.Name),
				DataPropertyName = nameof(View3d.Object.Name),
			});
			m_tree.CurrentCellChanged += (s,a) =>
			{
				// When the current cell changes, update the 'Siblings' data source
				var row = (TreeGridNode)m_tree.CurrentCell?.OwningRow;
				var obj = (View3d.Object)row?.DataBoundItem;
				var siblings = obj?.Parent?.Children ?? Objects;
				Siblings.DataSource = siblings;
				Siblings.Position = row?.NodeIndex ?? -1;
			};
			m_tree.DataSource = Objects;
			#endregion

			#region Grid
			m_grid.AutoGenerateColumns = false;
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Name",
				Name = nameof(View3d.Object.Name),
				DataPropertyName = nameof(View3d.Object.Name),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Visible",
				Name = nameof(View3d.Object.Visible),
				DataPropertyName = nameof(View3d.Object.Visible),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Colour",
				Name = nameof(View3d.Object.Colour),
				DataPropertyName = nameof(View3d.Object.Colour),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Flags",
				Name = nameof(View3d.Object.Flags),
				DataPropertyName = nameof(View3d.Object.Flags),
			});
			m_grid.CellFormatting += (s,a) =>
			{
				if (!m_grid.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell)) return;
				var obj = Siblings[a.RowIndex];
				switch (col.DataPropertyName)
				{
				case nameof(View3d.Object.Colour):
					{
						a.CellStyle.BackColor = obj.Colour.Alpha(0xff);
						a.CellStyle.ForeColor = HSV.FromColor(obj.Colour).V > 0.5f ? Color.Black : Color.White;
						a.Value = string.Empty;
						a.FormattingApplied = true;
						break;
					}
				case nameof(View3d.Object.Flags):
					{
						a.Value = obj.Flags.ToString();
						a.FormattingApplied = true;
						break;
					}
				}

				a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
				a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
			};
			m_grid.MouseDown += DataGridView_.ColumnVisibility;
			m_grid.VisibleChanged += DataGridView_.FitColumnsToDisplayWidth;
			m_grid.ColumnWidthChanged += DataGridView_.FitColumnsToDisplayWidth;
			m_grid.SizeChanged += DataGridView_.FitColumnsToDisplayWidth;
			m_grid.ContextMenuStrip = CreateListCMenu();
			m_grid.DataSource = Siblings;
			#endregion
		}

		/// <summary>Refresh 'Objects' with the objects currently in the scene</summary>
		private void PopulateObjects()
		{
			Siblings.DataSource = null;
			Objects.DataSource = Scene.Objects;
		}

		/// <summary>Create the context menu for the list view</summary>
		private ContextMenuStrip CreateListCMenu()
		{
			var cmenu = new ContextMenuStrip();

			return cmenu;
		}

		/// <summary>Data binding helper</summary>
		private class TreeItem :ITreeItem
		{
			private readonly View3d.Object m_obj;
			public TreeItem(View3d.Object obj)
			{
				m_obj = obj;
			}
			public object Parent
			{
				get { return m_obj.Parent; }
			}
			public int ChildCount
			{
				get { return m_obj.ChildCount; }
			}
			public object Child(int index)
			{
				return m_obj.GetChild(index);
			}
		}
		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ObjectManagerUI));
			this.m_split0 = new System.Windows.Forms.SplitContainer();
			this.m_grid = new pr.gui.DataGridView();
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_panel0 = new System.Windows.Forms.Panel();
			this.m_cb_filter = new pr.gui.ComboBox();
			this.m_btn_collapse_all = new System.Windows.Forms.Button();
			this.m_il_buttons = new System.Windows.Forms.ImageList(this.components);
			this.m_btn_expand_all = new System.Windows.Forms.Button();
			this.m_tree = new pr.gui.TreeGridView();
			((System.ComponentModel.ISupportInitialize)(this.m_split0)).BeginInit();
			this.m_split0.Panel1.SuspendLayout();
			this.m_split0.Panel2.SuspendLayout();
			this.m_split0.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.m_table0.SuspendLayout();
			this.m_panel0.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tree)).BeginInit();
			this.SuspendLayout();
			// 
			// m_split0
			// 
			this.m_split0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split0.Location = new System.Drawing.Point(3, 36);
			this.m_split0.Name = "m_split0";
			// 
			// m_split0.Panel1
			// 
			this.m_split0.Panel1.Controls.Add(this.m_tree);
			// 
			// m_split0.Panel2
			// 
			this.m_split0.Panel2.Controls.Add(this.m_grid);
			this.m_split0.Size = new System.Drawing.Size(506, 311);
			this.m_split0.SplitterDistance = 175;
			this.m_split0.TabIndex = 0;
			// 
			// m_grid
			// 
			this.m_grid.AllowUserToAddRows = false;
			this.m_grid.AllowUserToDeleteRows = false;
			this.m_grid.AllowUserToOrderColumns = true;
			this.m_grid.AllowUserToResizeRows = false;
			this.m_grid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_grid.Location = new System.Drawing.Point(0, 0);
			this.m_grid.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid.Name = "m_grid";
			this.m_grid.ReadOnly = true;
			this.m_grid.RowHeadersVisible = false;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.Size = new System.Drawing.Size(327, 311);
			this.m_grid.TabIndex = 0;
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 1;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Controls.Add(this.m_split0, 0, 1);
			this.m_table0.Controls.Add(this.m_panel0, 0, 0);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Margin = new System.Windows.Forms.Padding(0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 2;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Size = new System.Drawing.Size(512, 350);
			this.m_table0.TabIndex = 1;
			// 
			// m_panel0
			// 
			this.m_panel0.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel0.Controls.Add(this.m_cb_filter);
			this.m_panel0.Controls.Add(this.m_btn_collapse_all);
			this.m_panel0.Controls.Add(this.m_btn_expand_all);
			this.m_panel0.Location = new System.Drawing.Point(0, 0);
			this.m_panel0.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel0.Name = "m_panel0";
			this.m_panel0.Size = new System.Drawing.Size(512, 33);
			this.m_panel0.TabIndex = 1;
			// 
			// m_cb_filter
			// 
			this.m_cb_filter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_filter.BackColor = System.Drawing.Color.White;
			this.m_cb_filter.BackColorInvalid = System.Drawing.Color.White;
			this.m_cb_filter.BackColorValid = System.Drawing.Color.White;
			this.m_cb_filter.CommitValueOnFocusLost = true;
			this.m_cb_filter.DisplayProperty = null;
			this.m_cb_filter.ForeColor = System.Drawing.Color.Gray;
			this.m_cb_filter.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_cb_filter.ForeColorValid = System.Drawing.Color.Black;
			this.m_cb_filter.FormattingEnabled = true;
			this.m_cb_filter.Location = new System.Drawing.Point(61, 6);
			this.m_cb_filter.Name = "m_cb_filter";
			this.m_cb_filter.PreserveSelectionThruFocusChange = false;
			this.m_cb_filter.Size = new System.Drawing.Size(444, 21);
			this.m_cb_filter.TabIndex = 2;
			this.m_cb_filter.UseValidityColours = true;
			this.m_cb_filter.Value = null;
			// 
			// m_btn_collapse_all
			// 
			this.m_btn_collapse_all.AutoSize = true;
			this.m_btn_collapse_all.ImageIndex = 1;
			this.m_btn_collapse_all.ImageList = this.m_il_buttons;
			this.m_btn_collapse_all.Location = new System.Drawing.Point(31, 3);
			this.m_btn_collapse_all.Name = "m_btn_collapse_all";
			this.m_btn_collapse_all.Size = new System.Drawing.Size(28, 28);
			this.m_btn_collapse_all.TabIndex = 1;
			this.m_btn_collapse_all.UseVisualStyleBackColor = true;
			// 
			// m_il_buttons
			// 
			this.m_il_buttons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_il_buttons.ImageStream")));
			this.m_il_buttons.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il_buttons.Images.SetKeyName(0, "plus1_16x16.png");
			this.m_il_buttons.Images.SetKeyName(1, "minus1_16x16.png");
			// 
			// m_btn_expand_all
			// 
			this.m_btn_expand_all.AutoSize = true;
			this.m_btn_expand_all.ImageIndex = 0;
			this.m_btn_expand_all.ImageList = this.m_il_buttons;
			this.m_btn_expand_all.Location = new System.Drawing.Point(3, 3);
			this.m_btn_expand_all.Name = "m_btn_expand_all";
			this.m_btn_expand_all.Size = new System.Drawing.Size(28, 28);
			this.m_btn_expand_all.TabIndex = 0;
			this.m_btn_expand_all.UseVisualStyleBackColor = true;
			// 
			// m_tree
			// 
			this.m_tree.AllowUserToAddRows = false;
			this.m_tree.AllowUserToDeleteRows = false;
			this.m_tree.AllowUserToResizeColumns = false;
			this.m_tree.AllowUserToResizeRows = false;
			this.m_tree.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_tree.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_tree.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_tree.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_tree.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_tree.ColumnHeadersVisible = false;
			this.m_tree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tree.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_tree.ImageList = null;
			this.m_tree.Location = new System.Drawing.Point(0, 0);
			this.m_tree.Margin = new System.Windows.Forms.Padding(0);
			this.m_tree.Name = "m_tree";
			this.m_tree.NodeCount = 0;
			this.m_tree.ReadOnly = true;
			this.m_tree.RowHeadersVisible = false;
			this.m_tree.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_tree.ShowLines = true;
			this.m_tree.Size = new System.Drawing.Size(175, 311);
			this.m_tree.TabIndex = 0;
			this.m_tree.VirtualNodes = false;
			// 
			// ObjectManagerUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(512, 350);
			this.Controls.Add(this.m_table0);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ObjectManagerUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Scene Manager";
			this.m_split0.Panel1.ResumeLayout(false);
			this.m_split0.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split0)).EndInit();
			this.m_split0.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.m_table0.ResumeLayout(false);
			this.m_panel0.ResumeLayout(false);
			this.m_panel0.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tree)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
