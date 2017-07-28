using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	public class GridBase :pr.gui.DataGridView, IDockable
	{
		public GridBase(Model model, string title, string name)
		{
			using (((ISupportInitialize)this).InitialiseScope())
			{
				AutoGenerateColumns         = false;
				AllowUserToAddRows          = false;
				AllowUserToDeleteRows       = false;
				AllowUserToResizeRows       = false;
				AllowUserToResizeColumns    = true;
				AllowUserToOrderColumns     = true;
				AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
				AutoSizeRowsMode            = DataGridViewAutoSizeRowsMode.AllCells;
				BackgroundColor             = SystemColors.Window;
				CellBorderStyle             = DataGridViewCellBorderStyle.None;
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
				Dock                        = DockStyle.None;
				DoubleBuffered              = true;
				Location                    = Point.Empty;
				Margin                      = Padding.Empty;
				Name                        = name;
				RowHeadersVisible           = false;
				SelectionMode               = DataGridViewSelectionMode.FullRowSelect;
				Size                        = new Size(135, 55);
				DefaultCellStyle            = new DataGridViewCellStyle
				{
					Alignment          = DataGridViewContentAlignment.MiddleLeft,
					BackColor          = SystemColors.Window,
					Font               = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0),
					ForeColor          = SystemColors.ControlText,
					SelectionBackColor = SystemColors.Highlight,
					SelectionForeColor = SystemColors.HighlightText,
					WrapMode           = DataGridViewTriState.False,
				};
			}

			// Support for dock container controls
			DockControl = new DockControl(this, name) { TabText = title };

			Model = model;
		}
		protected override void Dispose(bool disposing)
		{
			DockControl = null;
			Model = null;
			base.Dispose(disposing);
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			DataGridView_.RightMouseSelect(this, e);
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		{
			[DebuggerStepThrough] get { return m_impl_dock_control; }
			private set
			{
				if (m_impl_dock_control == value) return;
				Util.Dispose(ref m_impl_dock_control);
				m_impl_dock_control = value;
			}
		}
		private DockControl m_impl_dock_control;

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
				}
				m_model = value;
				if (m_model != null)
				{
				}
			}
		}
		private Model m_model;
	}

	public class TreeBase :pr.gui.TreeGridView, IDockable
	{
		public TreeBase(Model model, string title, string name)
		{
			using (((ISupportInitialize)this).InitialiseScope())
			{
				AutoGenerateColumns         = false;
				AllowUserToAddRows          = false;
				AllowUserToDeleteRows       = false;
				AllowUserToResizeRows       = false;
				AllowUserToResizeColumns    = true;
				AllowUserToOrderColumns     = true;
				AutoSizeColumnsMode         = DataGridViewAutoSizeColumnsMode.Fill;
				AutoSizeRowsMode            = DataGridViewAutoSizeRowsMode.AllCells;
				BackgroundColor             = SystemColors.Window;
				CellBorderStyle             = DataGridViewCellBorderStyle.None;
				ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
				Dock                        = DockStyle.None;
				DoubleBuffered              = true;
				Location                    = Point.Empty;
				Margin                      = Padding.Empty;
				Name                        = name;
				RowHeadersVisible           = false;
				SelectionMode               = DataGridViewSelectionMode.FullRowSelect;
				Size                        = new Size(135, 55);
				DefaultCellStyle            = new DataGridViewCellStyle
				{
					Alignment          = DataGridViewContentAlignment.MiddleLeft,
					BackColor          = SystemColors.Window,
					Font               = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point, 0),
					ForeColor          = SystemColors.ControlText,
					SelectionBackColor = SystemColors.Highlight,
					SelectionForeColor = SystemColors.HighlightText,
					WrapMode           = DataGridViewTriState.False,
				};
			}

			// Support for dock container controls
			DockControl = new DockControl(this, name) { TabText = title };

			Model = model;
		}
		protected override void Dispose(bool disposing)
		{
			DockControl = null;
			Model = null;
			base.Dispose(disposing);
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			DataGridView_.RightMouseSelect(this, e);
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		{
			[DebuggerStepThrough] get { return m_impl_dock_control; }
			private set
			{
				if (m_impl_dock_control == value) return;
				Util.Dispose(ref m_impl_dock_control);
				m_impl_dock_control = value;
			}
		}
		private DockControl m_impl_dock_control;

		/// <summary>App logic</summary>
		public Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
				}
				m_model = value;
				if (m_model != null)
				{
				}
			}
		}
		private Model m_model;
	}
}
