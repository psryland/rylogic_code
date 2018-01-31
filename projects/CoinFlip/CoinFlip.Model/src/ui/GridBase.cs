using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;

namespace CoinFlip
{
	public class GridBase :Rylogic.Gui.DataGridView, IDockable
	{
		public GridBase(Model model, string title, string name)
		{
			m_model = model;
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
				ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
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

			// Even though the model has been set, set it again to give
			// sub classes a chance to hook up event handlers
			CreateHandle();
			this.BeginInvoke(() => SetModelCore(model));
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
		protected override void OnDataSourceChanged(EventArgs e)
		{
			base.OnDataSourceChanged(e);
			if (m_data_source is IBatchChanges bc0)
			{
				bc0.BatchChanges -= HandleBatchChanges;
			}
			m_data_source = DataSource;
			if (m_data_source is IBatchChanges bc1)
			{
				bc1.BatchChanges += HandleBatchChanges;
			}

			// Handlers
			void HandleBatchChanges(object sender, PrePostEventArgs args)
			{
				if (args.Before)
					SuspendLayout();
				else
					ResumeLayout();
			}
		}
		private object m_data_source;

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
				SetModelCore(value);
			}
		}
		private Model m_model;
		protected virtual void SetModelCore(Model model)
		{
			m_model = model;
		}
	}
}
