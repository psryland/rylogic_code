using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Graphix;
using Rylogic.Gui;
using Rylogic.Utility;
using DataGridView = Rylogic.Gui.DataGridView;
using ToolStripContainer = Rylogic.Gui.ToolStripContainer;

namespace LDraw
{
	//TODO move this to Rylogic.Graphix
	public class ObjectManagerUI :ToolForm
	{
		// Notes:
		//  - The data source 'Objects' is the collection of top level objects in the scene.
		//  - The Tree view displays the hierarchy of the scene. It is in alphabetic order
		//    because the scene objects are not stored in an ordered container.
		//  - The Grid view has two modes; virtual mode on/off. In virtual mode, it displays a flattened view
		//    of the tree and uses the tree view rows as the virtual data source. In non-virtual mode, a copy
		//    of the tree view rows is taken to create a data source which then allows sorting/filter.
		//  - As nodes are expanded/collapsed, child nodes are inserted/removed from the tree. This forces
		//    the grid back into virtual mode.
		//  - The number of rows in tree should always be the same as the number of rows in the grid.
		//  - Managing scene changed is an issue. The grids update, get cell values, and change current
		//    position while out of sync with the data source, causing exceptions.

		#region UI Elements
		private SplitContainer m_split0;
		private ImageList m_il_buttons;
		private TreeGridView m_tree;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;
		private ToolStripContainer m_tsc;
		private ToolStrip m_ts;
		private ToolStripButton m_btn_expand_all;
		private ToolStripButton m_btn_collapse_all;
		private ToolStripPatternFilter m_ts_filter;
		private Button m_btn_bong;
		private DataGridView m_grid;
		#endregion

		public ObjectManagerUI(SceneUI scene)
			:base(scene.TopLevelControl, EPin.Centre)
		{
			InitializeComponent();
			Text = $"Scene Manager - {scene.SceneName}";

			Objects = new BindingSource<View3d.Object> { DataSource = new BindingListEx<View3d.Object>() };
			Exclude = new HashSet<Guid>(new[]{ ChartControl.ChartTools.Id });
			Scene = scene;

			SetupUI();

			CenterToParent();
			SyncObjectsWithScene();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
			Scene = null;
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

		/// <summary>The collection of objects in this scene (top level tree nodes)</summary>
		public BindingSource<View3d.Object> Objects { [DebuggerStepThrough] get; private set; }

		/// <summary>Context Ids of objects not to show in the object manager</summary>
		public HashSet<Guid> Exclude { get; private set; }

		/// <summary>The scene that owns this object manager</summary>
		public SceneUI Scene
		{
			get { return m_scene; }
			private set
			{
				if (m_scene == value) return;
				if (m_scene != null)
				{
					m_scene.SceneChanged -= HandleSceneChanged;
					Objects.Clear();
				}
				m_scene = value;
				if (m_scene != null)
				{
					m_scene.SceneChanged += HandleSceneChanged;
				}

				// Handlers
				void HandleSceneChanged(object sender = null, View3d.SceneChangedEventArgs args = null)
				{
					// Ignore if only excluded context ids have changed
					if (args.ContextIds.All(x => Exclude.Contains(x)))
						return;

					SyncObjectsWithScene();
				}
			}
		}
		private SceneUI m_scene;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Tool bar
			m_btn_expand_all.Click += (s,a) =>
			{
				m_tree.ExpandAll();
			};
			m_btn_collapse_all.Click += (s,a) =>
			{
				m_tree.CollapseAll();
			};
			m_ts_filter.AutoSize = false;
			m_ts_filter.History = Model.Settings.FilterHistory;
			m_ts_filter.PatternChanged += (s,a) =>
			{
				// Cancel any previous filter
				VirtualMode = true;

				var has_value = m_ts_filter.Pattern.Expr.HasValue();
				if (has_value)
				{
					// Apply the new filter
					VirtualMode = false;
					Model.Settings.FilterHistory = m_ts_filter.History;
				}
			};
			m_ts_filter.StretchToFit(250);
			m_ts.Stretch = true;
			m_ts.Layout += (s,a) =>
			{
				m_ts_filter.StretchToFit(250);
			};
			#endregion

			#region Grid
			m_grid.VirtualMode = true;
			m_grid.AutoGenerateColumns = false;
			var column_filter_data = (DataGridView_.ColumnFiltersData)null;
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Name",
				Name = nameof(View3d.Object.Name),
				DataPropertyName = nameof(View3d.Object.Name),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Type",
				Name = nameof(View3d.Object.Type),
				DataPropertyName = nameof(View3d.Object.Type),
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
				if (!m_grid.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col) || SrcUpdating)
					return;

				var obj = ObjectFromGridRow(a.RowIndex);
				switch (col.DataPropertyName)
				{
				case nameof(View3d.Object.Colour):
					{
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
			m_grid.CellPainting += (s,a) =>
			{
				if (!m_grid.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col) || SrcUpdating)
					return;
				
				var obj = ObjectFromGridRow(a.RowIndex);
				switch (col.DataPropertyName)
				{
				case nameof(View3d.Object.Colour):
					{
						a.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
						using (var b0 = Gfx_.CheckerBrush(8, 8, 0xFFFFFFFF, 0xFFA0A0A0U))
						using (var b1 = new SolidBrush(obj.Colour))
						{
							a.Graphics.FillRectangle(b0, a.CellBounds);
							a.Graphics.FillRectangle(b1, a.CellBounds);
						}
						a.Handled = true;
						break;
					}
				}
			};
			m_grid.KeyDown += (s,a) =>
			{
				switch (a.KeyCode)
				{
				case Keys.Space:
					{
						var rec = ModifierKeys.HasFlag(Keys.Control) == false;
						if (a.Modifiers == Keys.Shift)
							ShowOrHideUnselected(Tri.Toggle, rec);
						else
							ShowOrHideSelected(Tri.Toggle, rec);
						break;
					}
				}
			};
			m_grid.CellValueNeeded += (s,a) =>
			{
				if (!m_grid.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col) || SrcUpdating)
					return;

				var obj = ObjectFromGridRow(a.RowIndex);
				switch (col.DataPropertyName)
				{
				case nameof(View3d.Object.Name   ): a.Value = obj.Name; break;
				case nameof(View3d.Object.Type   ): a.Value = obj.Type; break;
				case nameof(View3d.Object.Visible): a.Value = obj.Visible; break;
				case nameof(View3d.Object.Colour ): a.Value = obj.Colour; break;
				case nameof(View3d.Object.Flags  ): a.Value = obj.Flags; break;
				}
			};
			m_grid.MouseDown += (s,a) =>
			{
				var hit = m_grid.HitTestEx(a.X, a.Y);
				if (hit.Row != null && !hit.Row.Selected)
				{
					m_grid.ClearSelection();
					hit.Row.Selected = true;
				}
				switch (a.Button)
				{
				case MouseButtons.Left:
					{
						if (hit.Column != null && hit.Type == DataGridView_.HitTestInfo.EType.ColumnHeader)
						{
							VirtualMode = false;
							m_grid.Sort(hit.Column, hit.Column.HeaderCell.SortGlyphDirection.NextDirection());
						}
						break;
					}
				case MouseButtons.Right:
					{
						// Right mouse on a column header displays a context menu for hiding/showing columns
						if (hit.Column != null && hit.Type == DataGridView_.HitTestInfo.EType.ColumnHeader)
							m_grid.ColumnVisibilityContextMenu(hit.GridPoint);
						break;
					}
				}
			};
			m_grid.RowStateChanged += (s,a) =>
			{
				if (SrcUpdating)
					return;

				// Update the object selection flag when the row selection changes
				if (a.StateChanged.HasFlag(DataGridViewElementStates.Selected))
				{
					//TODO Apply the selected state recursively if the child objects are not expanded in the tree view
					var obj = ObjectFromGridRow(a.Row.Index);
					obj.FlagsSet(View3d.EFlags.Selected, a.Row.Selected, null);

					// Cause a refresh
					Owner.Invalidate();
				}
			};
			m_grid.DataSourceChanged += (s,a) =>
			{
				var virtual_mode = m_grid.DataSource == null;
				if (virtual_mode)
				{
					m_grid.VirtualMode = true;
					column_filter_data?.Dispose();
					m_grid.RowCount = m_tree.RowCount;
				}
				else
				{
					m_grid.VirtualMode = false;
					column_filter_data?.Dispose();
					column_filter_data = m_grid.ColumnFilters(create_if_necessary:true);
				}
			};
			m_grid.CurrentCellChanged += (s,a) =>
			{
				if (m_grid.CurrentCell == null || m_tree.InSetCurrentCell) return;
				var addr = m_grid.CurrentCellAddress;
				if (VirtualMode)
				{
					if (!SrcUpdating)
						m_tree.CurrentCell = m_tree[0, addr.Y];
				}
				else
				{
					var obj = (View3d.Object)m_grid.CurrentRow?.DataBoundItem;
					if (obj == null) return;
					m_tree.CurrentCell = m_tree[0, m_tree.FindNodeForItem(obj, displayed:true).RowIndex];
				}
			};
			m_grid.VisibleChanged += DataGridView_.FitColumnsToDisplayWidth;
			m_grid.ColumnWidthChanged += DataGridView_.FitColumnsToDisplayWidth;
			m_grid.SizeChanged += DataGridView_.FitColumnsToDisplayWidth;
			m_grid.KeyDown += DataGridView_.ColumnFilters;
			m_grid.ContextMenuStrip = CreateListCMenu();
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
			m_tree.NodeExpanded += (s,a) =>
			{
				UpdateRowCount();
			};
			m_tree.NodeCollapsed += (s,a) =>
			{
				UpdateRowCount();
			};
			m_tree.CurrentCellChanged += (s,a) =>
			{
				if (m_tree.CurrentCell == null || m_grid.InSetCurrentCell) return;
				var addr = m_tree.CurrentCellAddress;
				if (VirtualMode)
				{
					if (!SrcUpdating)
						m_grid.CurrentCell = m_grid[addr.X, addr.Y];
				}
				else if (m_grid.DataSource is IList<View3d.Object> src)
				{
					var obj = (View3d.Object)m_tree.CurrentNode?.DataBoundItem;
					if (obj == null) return;

					// The grid may be filtered, so 'obj' may not be in the grid data source
					var idx = src.IndexOf(obj);
					if (idx != -1)
						m_grid.CurrentCell = m_grid[addr.X, idx];
				}
			};
			m_tree.DataSource = Objects;
			#endregion
		}

		/// <summary>Create the context menu for the list view</summary>
		private ContextMenuStrip CreateListCMenu()
		{
			var cmenu = new ContextMenuStrip();
			#region Hide
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Hide"));
				opt.ToolTipText = "Hide selected objects.\r\nHold Control to exclude child objects\r\nToggle visibility shortcut: [Space]";
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = m_grid.SelectedRowCount(max_count:1) != 0;
				};
				opt.Click += (s,a) =>
				{
					var rec = ModifierKeys.HasFlag(Keys.Control) == false;
					ShowOrHideSelected(Tri.Clear, rec);
				};
			}
			#endregion
			#region Show
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Show"));
				opt.ToolTipText = "Show selected objects.\r\nHold Control to exclude child objects\r\nToggle visibility shortcut: [Space]";
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = m_grid.SelectedRowCount(max_count:1) != 0;
				};
				opt.Click += (s,a) =>
				{
					var rec = ModifierKeys.HasFlag(Keys.Control) == false;
					ShowOrHideSelected(Tri.Set, rec);
				};
			}
			#endregion
			cmenu.Items.AddSeparator();
			#region Hide Others
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Hide Others"));
				opt.ToolTipText = "Hide unselected objects.\r\nHold Control to exclude child objects\r\nShortcut: [Shift+Space]";
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = m_grid.SelectedRowCount(max_count:1) != 0;
				};
				opt.Click += (s,a) =>
				{
					var rec = ModifierKeys.HasFlag(Keys.Control) == false;
					ShowOrHideUnselected(Tri.Clear, rec);
				};
			}
			#endregion
			#region Show Others
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Show Others"));
				opt.ToolTipText = "Show unselected objects.\r\nHold Control to exclude child objects\r\nShortcut: [Shift+Space]";
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = m_grid.SelectedRowCount(max_count:1) != 0;
				};
				opt.Click += (s,a) =>
				{
					var rec = ModifierKeys.HasFlag(Keys.Control) == false;
					ShowOrHideUnselected(Tri.Set, rec);
				};
			}
			#endregion
			cmenu.Items.AddSeparator();
			#region Wireframe
			{
				const string Solid = "Solid";
				const string Wireframe = "Wireframe";

				var opt = cmenu.Items.Add2(new ToolStripMenuItem(Wireframe));
				opt.ToolTipText = "Switch the selected objects between solid and wireframe mode.\r\nShortcut: [Ctrl+W]";
				cmenu.Opening += (s,a) =>
				{
					var sel = m_grid.GetRowsWithState(DataGridViewElementStates.Selected).FirstOrDefault();
					opt.Enabled = sel != null;
					opt.Text = (sel != null && ObjectFromGridRow(sel.Index).Wireframe) ? Solid : Wireframe;
				};
				opt.Click += (s,a) =>
				{
					var wire = opt.Text == Wireframe;
					foreach (var row in m_grid.GetRowsWithState(DataGridViewElementStates.Selected))
						ObjectFromGridRow(row.Index).Wireframe = wire;
				};
			}
			#endregion
			cmenu.Items.AddSeparator();
			#region Invert Selection
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Invert Selection"));
				opt.ToolTipText = "Invert the selection";
				opt.Click += (s,a) =>
				{
					using (m_grid.SuspendRedraw(true))
						foreach (var row in m_grid.Rows.Cast<DataGridViewRow>())
							row.Selected = !row.Selected;
				};
			}
			#endregion
			return cmenu;
		}

		/// <summary>Get the view 3d object for the given tree row index</summary>
		private View3d.Object ObjectFromGridRow(int row_index)
		{
			// In virtual mode, the tree rows are the data source for the grid
			if (VirtualMode)
			{
				Debug.Assert(m_grid.RowCount == m_tree.RowCount);
				var node = (TreeGridNode)m_tree.Rows[row_index];
				var obj = (View3d.Object)node.DataBoundItem;
				return obj;
			}
			else
			{
				var src = (IList<View3d.Object>)m_grid.DataSource;
				Debug.Assert(row_index.Within(0, src.Count));
				return src[row_index];
			}
		}

		/// <summary>Get/Set whether the grid displays a copy of the flattened tree view or</summary>
		private bool VirtualMode
		{
			get { return m_grid.DataSource == null; }
			set
			{
				if (VirtualMode == value) return;
				if (value)
				{
					// Switch the grid back to virtual mode and reset the row count
					m_grid.DataSource = null;
				}
				else
				{
					// Switch to data bound mode so we can filter and sort
					var rows = m_tree.Rows.Cast<TreeGridNode>().Select(x => (View3d.Object)x.DataBoundItem);
					if (Filter.Expr.HasValue()) rows = rows.Where(x => Filter.IsMatch(x.Name));
					m_grid.DataSource = new BindingListEx<View3d.Object>(rows);
				}
			}
		}

		/// <summary>True while the tree and grid are out of sync</summary>
		private bool SrcUpdating
		{
			get { return m_src_updating; }
			set
			{
				if (m_src_updating == value) return;
				m_src_updating = value;
				Debug.Assert(m_src_updating || m_tree.RowCount == m_grid.RowCount);
			}
		}
		private bool m_src_updating;

		/// <summary>Update the 'Objects' collection to match the objects in the scene</summary>
		private void SyncObjectsWithScene()
		{
			// Flag the source data as possibly out of sync between the tree and grid
			SrcUpdating = true;

			// Suspend tree and grid updates until the changes have been made to the 'Objects' collection
			//using (m_tree.SuspendRedraw(true))
			//using (m_grid.SuspendRedraw(true))
			using (Objects.SuspendEvents(reset_bindings_on_resume:true, preserve_position:true))
			{
				m_tree.CurrentCell = null;
				m_grid.CurrentCell = null;

				// Read the objects from the scene
				var objects = new HashSet<View3d.Object>();
				Scene.Window.EnumObjects(obj => objects.Add(obj), Exclude.ToArray(), 0, 1);

				// Remove objects that are no longer in the scene
				Objects.RemoveIf(x => !objects.Contains(x));

				// Remove objects from 'objects' that are still in the scene
				Objects.ForEach(x => objects.Remove(x));

				// Add whatever is left
				objects.ForEach(x => Objects.Add(x));

				// Sort the objects alphabetically
				Objects.Sort(Cmp<View3d.Object>.From((l, r) => l.Name.CompareTo(r.Name)));
			}
			Debug.Assert(m_tree.RowCount == Objects.Count);

			// After the objects collection has been changed, the tree should
			// update due to it's data binding. We need to update the grid as well.
			UpdateRowCount();
		}

		/// <summary>Set the grid back to virtual mode and update the row count</summary>
		private void UpdateRowCount()
		{
			VirtualMode = true;
			if (m_grid.RowCount == m_tree.RowCount)
				return;

			m_grid.RowCount = m_tree.RowCount;
			UpdateSelectionState();
			m_grid.Invalidate();
			SrcUpdating = false;
		}

		/// <summary>Update the 'selected' flag on each object</summary>
		private void UpdateSelectionState()
		{
			// Clear the selection flag on all objects
			foreach (var obj in Objects)
				obj.FlagsSet(View3d.EFlags.Selected, false, string.Empty);

			// Set it on the selected objects
			foreach (var obj in SelectedObjects)
				obj.FlagsSet(View3d.EFlags.Selected, true, null);
		}

		/// <summary>Grid view filter</summary>
		private Pattern Filter
		{
			get { return m_ts_filter.Pattern; }
		}

		/// <summary>Enumerate all objects in the grid</summary>
		private IEnumerable<View3d.Object> ObjectsInGrid
		{
			get { return int_.Range(m_grid.RowCount).Select(i => ObjectFromGridRow(i)); }
		}

		/// <summary>Enumerate all selected objects in the grid</summary>
		private IEnumerable<View3d.Object> SelectedObjects
		{
			get { return m_grid.GetRowsWithState(DataGridViewElementStates.Selected).Select(x => ObjectFromGridRow(x.Index)); }
		}

		/// <summary>Enumerate all un-selected objects in the grid</summary>
		private IEnumerable<View3d.Object> UnselectedObjects
		{
			get { return m_grid.Rows.Cast<DataGridViewRow>().Where(x => !x.Selected).Select(x => ObjectFromGridRow(x.Index)); }
		}

		/// <summary>Show or hide all currently selected objects</summary>
		private void ShowOrHideSelected(Tri show, bool recursive)
		{
			foreach (var obj in SelectedObjects)
				obj.VisibleSet(
					show == Tri.Clear  ? false :
					show == Tri.Set    ? true :
					show == Tri.Toggle ? !obj.Visible :
					throw new Exception($"Unknown visibility state: {show}"),
					recursive ? string.Empty : null);

			m_grid.Invalidate();
			m_scene.Invalidate();
		}

		/// <summary>Show or hide all currently un-selected objects</summary>
		private void ShowOrHideUnselected(Tri show, bool recursive)
		{
			foreach (var obj in UnselectedObjects)
				obj.VisibleSet(
					show == Tri.Clear  ? false :
					show == Tri.Set    ? true :
					show == Tri.Toggle ? !obj.Visible :
					throw new Exception($"Unknown visibility state: {show}"),
					recursive ? string.Empty : null);

			m_grid.Invalidate();
			m_scene.Invalidate();
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
				Debug.Assert(index.Within(0,ChildCount), "Child index out of range");
				return m_obj.Child(index);
			}
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ObjectManagerUI));
			this.m_split0 = new System.Windows.Forms.SplitContainer();
			this.m_tree = new Rylogic.Gui.TreeGridView();
			this.m_btn_bong = new System.Windows.Forms.Button();
			this.m_grid = new Rylogic.Gui.DataGridView();
			this.m_il_buttons = new System.Windows.Forms.ImageList(this.components);
			this.m_ss = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_tsc = new Rylogic.Gui.ToolStripContainer();
			this.m_ts = new System.Windows.Forms.ToolStrip();
			this.m_btn_expand_all = new System.Windows.Forms.ToolStripButton();
			this.m_btn_collapse_all = new System.Windows.Forms.ToolStripButton();
			this.m_ts_filter = new Rylogic.Gui.ToolStripPatternFilter();
			((System.ComponentModel.ISupportInitialize)(this.m_split0)).BeginInit();
			this.m_split0.Panel1.SuspendLayout();
			this.m_split0.Panel2.SuspendLayout();
			this.m_split0.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_tree)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.m_ss.SuspendLayout();
			this.m_tsc.BottomToolStripPanel.SuspendLayout();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_ts.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_split0
			// 
			this.m_split0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split0.Location = new System.Drawing.Point(0, 0);
			this.m_split0.Name = "m_split0";
			// 
			// m_split0.Panel1
			// 
			this.m_split0.Panel1.Controls.Add(this.m_tree);
			// 
			// m_split0.Panel2
			// 
			this.m_split0.Panel2.Controls.Add(this.m_btn_bong);
			this.m_split0.Panel2.Controls.Add(this.m_grid);
			this.m_split0.Size = new System.Drawing.Size(512, 302);
			this.m_split0.SplitterDistance = 177;
			this.m_split0.TabIndex = 0;
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
			this.m_tree.Size = new System.Drawing.Size(177, 302);
			this.m_tree.TabIndex = 0;
			this.m_tree.VirtualNodes = false;
			// 
			// m_btn_bong
			// 
			this.m_btn_bong.Location = new System.Drawing.Point(244, 12);
			this.m_btn_bong.Name = "m_btn_bong";
			this.m_btn_bong.Size = new System.Drawing.Size(75, 23);
			this.m_btn_bong.TabIndex = 1;
			this.m_btn_bong.Text = "Bong!";
			this.m_btn_bong.UseVisualStyleBackColor = true;
			this.m_btn_bong.Visible = false;
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
			this.m_grid.Size = new System.Drawing.Size(331, 302);
			this.m_grid.TabIndex = 0;
			// 
			// m_il_buttons
			// 
			this.m_il_buttons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_il_buttons.ImageStream")));
			this.m_il_buttons.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il_buttons.Images.SetKeyName(0, "plus1_16x16.png");
			this.m_il_buttons.Images.SetKeyName(1, "minus1_16x16.png");
			// 
			// m_ss
			// 
			this.m_ss.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ss.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status});
			this.m_ss.Location = new System.Drawing.Point(0, 0);
			this.m_ss.Name = "m_ss";
			this.m_ss.Size = new System.Drawing.Size(512, 22);
			this.m_ss.TabIndex = 2;
			this.m_ss.Text = "statusStrip1";
			// 
			// m_status
			// 
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(26, 17);
			this.m_status.Text = "Idle";
			// 
			// m_tsc
			// 
			// 
			// m_tsc.BottomToolStripPanel
			// 
			this.m_tsc.BottomToolStripPanel.Controls.Add(this.m_ss);
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Controls.Add(this.m_split0);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(512, 302);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(512, 350);
			this.m_tsc.TabIndex = 2;
			this.m_tsc.Text = "toolStripContainer1";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_ts);
			// 
			// m_ts
			// 
			this.m_ts.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ts.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_btn_expand_all,
            this.m_btn_collapse_all,
            this.m_ts_filter});
			this.m_ts.Location = new System.Drawing.Point(0, 0);
			this.m_ts.Name = "m_ts";
			this.m_ts.Size = new System.Drawing.Size(512, 26);
			this.m_ts.Stretch = true;
			this.m_ts.TabIndex = 0;
			// 
			// m_btn_expand_all
			// 
			this.m_btn_expand_all.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_expand_all.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_expand_all.Image")));
			this.m_btn_expand_all.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_expand_all.Name = "m_btn_expand_all";
			this.m_btn_expand_all.Size = new System.Drawing.Size(23, 23);
			this.m_btn_expand_all.Text = "toolStripButton1";
			// 
			// m_btn_collapse_all
			// 
			this.m_btn_collapse_all.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_btn_collapse_all.Image = ((System.Drawing.Image)(resources.GetObject("m_btn_collapse_all.Image")));
			this.m_btn_collapse_all.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_btn_collapse_all.Name = "m_btn_collapse_all";
			this.m_btn_collapse_all.Size = new System.Drawing.Size(23, 23);
			this.m_btn_collapse_all.Text = "toolStripButton1";
			// 
			// m_filter
			// 
			this.m_ts_filter.Name = "m_filter";
			this.m_ts_filter.Size = new System.Drawing.Size(180, 23);
			// 
			// ObjectManagerUI
			// 
			this.AcceptButton = this.m_btn_bong;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(512, 350);
			this.Controls.Add(this.m_tsc);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ObjectManagerUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Scene Manager";
			this.m_split0.Panel1.ResumeLayout(false);
			this.m_split0.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split0)).EndInit();
			this.m_split0.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_tree)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.m_ss.ResumeLayout(false);
			this.m_ss.PerformLayout();
			this.m_tsc.BottomToolStripPanel.ResumeLayout(false);
			this.m_tsc.BottomToolStripPanel.PerformLayout();
			this.m_tsc.ContentPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.m_ts.ResumeLayout(false);
			this.m_ts.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
