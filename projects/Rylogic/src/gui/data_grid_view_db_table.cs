using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui
{
	/// <summary>Base class for DGV's based on BindingSourceDbTable<></summary>
	public class DataGridViewDbTable<Type> :DataGridView where Type:new()
	{
		// Notes:
		//  Set 'DataPropertyName' to link grid columns with columns in the DB table (required for sorting to work).
		//  'ColumnValue' should return the actual type of column value, i.e. *not* converted to a string.
		//  'ColumnFormat' should be used to convert types to strings (set FormattingApplied = true)

		private EventBatcher m_eb_update;

		public DataGridViewDbTable(BindingSourceDbTable<Type> table)
		{
			VirtualMode                   = true;
			AllowDrop                     = false;
			AllowUserToAddRows            = false;
			AllowUserToDeleteRows         = false;
			AllowUserToResizeRows         = false;
			AllowUserToOrderColumns       = true;
			AutoGenerateColumns           = false;
			ColumnHeadersVisible          = true;
			AutoSizeColumnsMode           = DataGridViewAutoSizeColumnsMode.None;
			ColumnHeadersHeightSizeMode   = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			EditMode                      = DataGridViewEditMode.EditProgrammatically;
			RowHeadersBorderStyle         = DataGridViewHeaderBorderStyle.Single;
			RowHeadersVisible             = false;
			RowHeadersWidth               = 28;
			RowHeadersWidthSizeMode       = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			SelectionMode                 = DataGridViewSelectionMode.FullRowSelect;
			RowsDefaultCellStyle          = new DataGridViewCellStyle{Alignment = DataGridViewContentAlignment.TopLeft, WrapMode = DataGridViewTriState.True,};
			ReadOnly                      = true;

			m_eb_update = new EventBatcher(() =>
			{
				if (DataSource == null) return;
				DataSource.UpdateIfNecessary();
				Refresh();
			}, TimeSpan.FromMilliseconds(10));

			DataSource = table;
		}
		protected override void Dispose(bool disposing)
		{
			DataSource = null;
			Util.Dispose(ref m_eb_update);
			base.Dispose(disposing);
		}

		/// <summary>The data source for the grid</summary>
		public new BindingSourceDbTable<Type> DataSource
		{
			get { return m_impl_datasource; }
			set
			{
				if (m_impl_datasource == value) return;
				if (m_impl_datasource != null)
				{
					RowCount = 0;
					m_impl_datasource.Invalidated -= m_eb_update.Signal;
					m_impl_datasource.ListChanging -= HandleDataChanged;
					m_impl_datasource.PositionChanged -= HandlePositionChanged;
				}
				m_impl_datasource = value;
				if (m_impl_datasource != null)
				{
					RowCount = m_impl_datasource.Count;
					m_impl_datasource.Invalidated += m_eb_update.Signal;
					m_impl_datasource.ListChanging += HandleDataChanged;
					m_impl_datasource.PositionChanged += HandlePositionChanged;
					m_impl_datasource.ResetBindings();
				}
			}
		}
		private BindingSourceDbTable<Type> m_impl_datasource;

		/// <summary>Handle the current position in the data source changing</summary>
		private void HandlePositionChanged(object sender, PositionChgEventArgs e)
		{
			if (m_in_position_changed != 0) return;
			using (Scope.Create(() => ++m_in_position_changed, () => --m_in_position_changed))
			{
				if (e.NewIndex != -1)
				{
					// Do nothing if the new index is already selected
					var selected_rows = this.GetRowsWithState(DataGridViewElementStates.Selected|DataGridViewElementStates.Visible);
					if (selected_rows.Any(x => x.Index == e.NewIndex))
						return;

					ClearSelection();
					if (e.NewIndex < 0 || e.NewIndex >= RowCount)
						throw new Exception($"Current data source position {e.NewIndex} is not a valid row in the grid ({RowCount} rows)");
					Rows[e.NewIndex].Selected = true;
				}
				else
				{
					ClearSelection();
				}
			}
		}
		private int m_in_position_changed;

		/// <summary>Handle the data collection changing</summary>
		private void HandleDataChanged(object sender, ListChgEventArgs<Type> args)
		{
			switch (args.ChangeType)
			{
			case ListChg.PreReset:
				m_scroll_pos = this.ScrollScope();
				RowCount = 0;
				break;
			case ListChg.Reset:
				RowCount = DataSource?.Count ?? 0;
				Util.Dispose(ref m_scroll_pos);
				m_eb_update.Signal();
				break;
			case ListChg.ItemReset:
				m_eb_update.Signal();
				break;
			}
		}
		private Scope m_scroll_pos;

		/// <summary>Return the item associated with row 'index' in the grid</summary>
		public Type GetItem(int index)
		{
			return (uint)index < (uint)DataSource.Count ? DataSource[index] : new Type();
		}

		/// <summary>Return the value for the given column and item state</summary>
		public virtual object ColumnValue(DataGridViewColumn column, Type item)
		{
			var pi = item.GetType().GetProperty(column.DataPropertyName);
			if (pi == null) throw new Exception($"Unknown column {item.GetType().Name}.{column.DataPropertyName ?? string.Empty}");
			return pi.GetValue(item, null);
		}

		/// <summary>Return the value for the given column and item state</summary>
		protected virtual void ColumnFormat(DataGridViewColumn column, Type item, DataGridViewCellFormattingEventArgs e)
		{}

		/// <summary>Return the string to use in an SQL filter to match 'item' in 'column'</summary>
		public virtual string FilterValue(DataGridViewColumn column, Type item)
		{
			var val = ColumnValue(column, item);
			return
				val is int           ? ((int   )val).ToString() :
				val is uint          ? ((uint  )val).ToString() :
				val is long          ? ((long  )val).ToString() :
				val is ulong         ? ((ulong )val).ToString() :
				val is short         ? ((short )val).ToString() :
				val is ushort        ? ((ushort)val).ToString() :
				val is sbyte         ? ((sbyte )val).ToString() :
				val is byte          ? ((byte  )val).ToString() :
				val is float         ? ((float )val).ToString() :
				val is double        ? ((double)val).ToString() :
				val is bool          ? (((bool )val)?"1":"0") :
				val is Color         ? ((Color )val).ToArgb().ToString() :
				val is double        ? ((double)val).ToString() :
				val.GetType().IsEnum ? ((int   )val).ToString() :
				$"'{val}'";
		}

		/// <summary>Respond to a left click on the grid</summary>
		protected virtual void LeftClick(DataGridViewColumn column, HitTestInfo hit)
		{}

		/// <summary>Add context menu items</summary>
		protected virtual void AddContextMenuItems(DataGridViewColumn column, List<ToolStripItem> opts, HitTestInfo hit)
		{}

		/// <summary>Respond to a double clicked cell</summary>
		protected virtual void CellDoubleClicked(DataGridViewColumn column, DataGridViewCellEventArgs args)
		{}

		#region DGV overrides

		/// <summary>Handle selection changed</summary>
		protected override void OnSelectionChanged(EventArgs e)
		{
			DataSource.Position = CurrentRow?.Index ?? -1;
			base.OnSelectionChanged(e);
		}

		/// <summary>Handle virtual mode cell data access</summary>
		protected sealed override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			DataGridViewColumn col;
			base.OnCellValueNeeded(e);
			if (!this.Within(e.ColumnIndex, e.RowIndex, out col)) return;
			e.Value = ColumnValue(col, GetItem(e.RowIndex));
		}

		/// <summary>Handle virtual mode cell formatting</summary>
		protected sealed override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			DataGridViewColumn col;
			base.OnCellFormatting(e);
			if (!this.Within(e.ColumnIndex, e.RowIndex, out col)) return;
			ColumnFormat(col, GetItem(e.RowIndex), e);
		}

		/// <summary>Handle mouse clicks on the grid</summary>
		protected sealed override void OnMouseClick(MouseEventArgs args)
		{
			base.OnMouseClick(args);

			DataGridViewColumn col;
			var hit = HitTest(args.X, args.Y);
			if (!this.Within(hit.ColumnIndex, hit.RowIndex, out col))
				return;

			switch (args.Button)
			{
			case MouseButtons.Left:
				{
					LeftClick(col, hit);
					break;
				}
			case MouseButtons.Right:
				{
					// Select the clicked item
					DataSource.Position = hit.RowIndex;

					var opts = new List<ToolStripItem>();
					AddContextMenuItems(col, opts, hit);
					if (opts.Count != 0)
					{
						var menu = new ContextMenuStrip();
						menu.Items.AddRange(opts.ToArray());
						menu.Show(MousePosition);
					}
					break;
				}
			}
		}

		/// <summary>Handle cell double clicks</summary>
		protected sealed override void OnCellDoubleClick(DataGridViewCellEventArgs args)
		{
			DataGridViewColumn col;
			base.OnCellDoubleClick(args);
			if (!this.Within(args.ColumnIndex, args.RowIndex, out col)) return;
			CellDoubleClicked(col, args);
		}

		/// <summary>Handle sorting by column header</summary>
		protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
		{
			base.OnColumnHeaderMouseClick(e);
			if (ModifierKeys == Keys.None)
			{
				// Reset all of the columns to no sorting, except for 'e.ColumnIndex'.
				foreach (var c in Columns.Cast<DataGridViewColumn>())
					if (c.Index != e.ColumnIndex)
						c.HeaderCell.SortGlyphDirection = SortOrder.None;

				// Set the sort direction of the selected column
				var col = Columns[e.ColumnIndex];
				var hdr = col.HeaderCell;
				hdr.SortGlyphDirection = Enum<SortOrder>.Cycle(hdr.SortGlyphDirection);

				// Apply the sorting to the data source.
				DataSource.SortColumn    = hdr.SortGlyphDirection != SortOrder.None ? col.DataPropertyName : null;
				DataSource.SortAscending = hdr.SortGlyphDirection == SortOrder.Ascending;
				DataSource.Update();
			}
		}

		/// <summary>Handle resizing columns</summary>
		protected override void OnColumnDividerDoubleClick(DataGridViewColumnDividerDoubleClickEventArgs e)
		{
			var col = Columns[e.ColumnIndex];
			col.Width = col.GetPreferredWidth(DataGridViewAutoSizeColumnMode.DisplayedCells, true);
		}

		/// <summary>Update the FillWeights after a column width changes and rescale the columns to fit</summary>
		protected override void OnColumnWidthChanged(DataGridViewColumnEventArgs e)
		{
			base.OnColumnWidthChanged(e);

			if (m_block_column_width_events) return;
			using (Scope.Create(() => m_block_column_width_events = true, () => m_block_column_width_events = false))
			{
				this.SetFillWeightsOnColumnWidthChanged(e.Column.Index);
				this.SetGridColumnSizes(DataGridView_.EColumnSizeOptions.GrowToDisplayWidth);
			}
		}
		private bool m_block_column_width_events;

		/// <summary>Resize columns on layout</summary>
		protected override void OnLayout(LayoutEventArgs e)
		{
			this.SetGridColumnSizes(DataGridView_.EColumnSizeOptions.GrowToDisplayWidth);
			base.OnLayout(e);
		}

		/// <summary>Support resizeable rows</summary>
		protected override void OnRowHeightInfoNeeded(DataGridViewRowHeightInfoNeededEventArgs args)
		{
			args.Height = RowTemplate.Height;
		}
		protected override void OnRowHeightInfoPushed(DataGridViewRowHeightInfoPushedEventArgs args)
		{
			RowTemplate.Height = args.Height;
			Refresh();
		}

		#endregion
	}
}
