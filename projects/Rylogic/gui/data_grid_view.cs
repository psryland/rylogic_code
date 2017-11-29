using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using pr.extn;
using pr.maths;
using pr.util;

namespace pr.gui
{
	public class DataGridView :System.Windows.Forms.DataGridView
	{
		public DataGridView()
		{
			m_fi_ptAnchorCell           = typeof(System.Windows.Forms.DataGridView).GetField("ptAnchorCell"             , BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
			m_mi_SelectRowRange         = typeof(System.Windows.Forms.DataGridView).GetMethod("SelectRowRange"          , BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
			m_pi_NoSelectionChangeCount = typeof(System.Windows.Forms.DataGridView).GetProperty("NoSelectionChangeCount", BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
			m_fi_ptMouseDownCell        = typeof(System.Windows.Forms.DataGridView).GetField("ptMouseDownCell"          , BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
			m_fi_trackColumn            = typeof(System.Windows.Forms.DataGridView).GetField("trackColumn"              , BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
			m_fi_trackColumnEdge        = typeof(System.Windows.Forms.DataGridView).GetField("trackColumnEdge"          , BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
			m_fi_trackColAnchor         = typeof(System.Windows.Forms.DataGridView).GetField("trackColAnchor"           , BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
			m_fi_trackRow               = typeof(System.Windows.Forms.DataGridView).GetField("trackRow"                 , BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
			m_fi_trackRowEdge           = typeof(System.Windows.Forms.DataGridView).GetField("trackRowEdge"             , BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
			m_fi_trackRowAnchor         = typeof(System.Windows.Forms.DataGridView).GetField("trackRowAnchor"           , BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly);
		}
		protected override void OnDataError(bool displayErrorDialogIfNoHandler, DataGridViewDataErrorEventArgs e)
		{
			base.OnDataError(false, e);
		}
		protected override void OnSelectionChanged(EventArgs e)
		{
			// Whenever the selection changes, and is a single cell selection, update the anchor location
			// This is a fix for: "click a cell, page up, hold down shift, arrow key to change selection"
			// Note: "SelectedRows" and "SelectedColumns" are only non-zero when the whole row or column
			// is selected, regardless of the selection mode.
			if (this.SelectedRowCount(max_count: 2) == 1)
			{
				SelectionAnchorCell = new Point(0, this.SelectedRows().First().Index);
			}
			else if (this.SelectedColumnCount(max_count: 2) == 1)
			{
				SelectionAnchorCell = new Point(this.SelectedColumns().First().Index, 0);
			}
			else
			{
				var cells = SelectedCells;
				if (cells.Count == 1)
					SelectionAnchorCell = new Point(cells[0].ColumnIndex, cells[0].RowIndex);
			}

			base.OnSelectionChanged(e);
		}
		protected override bool SetCurrentCellAddressCore(int columnIndex, int rowIndex, bool setAnchorCellAddress, bool validateCurrentCell, bool throughMouseClick)
		{
			// Set current cell is often reentrant. 'InSetCurrentCell' is for derived classes to
			// prevent setting the current cell during a set current cell handler.
			// e.g. 'OnCurrentCellChanged' can use 'InSetCurrentCell'
			using (Scope.Create(() => InSetCurrentCell = true, () => InSetCurrentCell = false))
			{
				if (BindingContext != null && DataSource != null)
				{
					// If the currency manager has no current value, set the current cell address to -1,-1
					// This prevents an IndexOutOfRange exception when the data source has Count != 0 but Position == -1.
					if (BindingContext[DataSource] is CurrencyManager cm && cm.Position == -1)
					{
						columnIndex = -1;
						rowIndex = -1;
					}
				}
				return base.SetCurrentCellAddressCore(columnIndex, rowIndex, setAnchorCellAddress, validateCurrentCell, throughMouseClick);
			}
		}
		public void Invalidate(object sender, EventArgs args)
		{
			Invalidate();
		}

		/// <summary>True while 'SetCurrentCellAddressCore' is on the call stack</summary>
		public bool InSetCurrentCell { get; private set; }

		/// <summary>Add rows to the selection </summary>
		public void SelectRowRange(int index, int count, bool select)
		{
			Debug.Assert(index >= 0 && index < RowCount);
			Debug.Assert(count >= 0 && count <= RowCount - index);
			m_mi_SelectRowRange.Invoke(this, new object[] { index, index + count - 1, select });
		}
		private MethodInfo m_mi_SelectRowRange;

		/// <summary>Get/Set the cell anchor, used for Shift+Selection</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Point SelectionAnchorCell
		{
			get { return (Point)m_fi_ptAnchorCell.GetValue(this); }
			set { m_fi_ptAnchorCell.SetValue(this, value); }
		}
		private FieldInfo m_fi_ptAnchorCell;

		/// <summary>Temporarily suspend selection changed events</summary>
		public Scope SuspendSelectionChanged()
		{
			return Scope.Create(
				() => ++NoSelectionChangeCount,
				() => --NoSelectionChangeCount);
		}
		private int NoSelectionChangeCount
		{
			get { return (int)m_pi_NoSelectionChangeCount.GetMethod.Invoke(this, null); }
			set { m_pi_NoSelectionChangeCount.SetMethod.Invoke(this, new object[] { value }); }
		}
		private PropertyInfo m_pi_NoSelectionChangeCount;

		/// <summary>Get/Set the reference cell used in mouse down</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Point MouseDownCell
		{
			get { return (Point)m_fi_ptMouseDownCell.GetValue(this); }
			set { m_fi_ptMouseDownCell.SetValue(this, value); }
		}
		private FieldInfo m_fi_ptMouseDownCell;

		/// <summary>Get/Set the reference column used in mouse selection and column resizes</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int TrackColumn
		{
			get { return (int)m_fi_trackColumn.GetValue(this); }
			set { m_fi_trackColumn.SetValue(this, value); }
		}
		private FieldInfo m_fi_trackColumn;

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int TrackColumnEdge
		{
			get { return (int)m_fi_trackColumnEdge.GetValue(this); }
			set { m_fi_trackColumnEdge.SetValue(this, value); }
		}
		private FieldInfo m_fi_trackColumnEdge;

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int TrackColumnAnchor
		{
			get { return (int)m_fi_trackColAnchor.GetValue(this); }
			set { m_fi_trackColAnchor.SetValue(this, value); }
		}
		private FieldInfo m_fi_trackColAnchor;

		/// <summary>Get/Set the reference row used in mouse selection and row resizes</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int TrackRow
		{
			get { return (int)m_fi_trackRow.GetValue(this); }
			set { m_fi_trackRow.SetValue(this, value); }
		}
		private FieldInfo m_fi_trackRow;

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int TrackRowEdge
		{
			get { return (int)m_fi_trackRowEdge.GetValue(this); }
			set { m_fi_trackRowEdge.SetValue(this, value); }
		}
		private FieldInfo m_fi_trackRowEdge;

		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public int TrackRowAnchor
		{
			get { return (int)m_fi_trackRowAnchor.GetValue(this); }
			set { m_fi_trackRowAnchor.SetValue(this, value); }
		}
		private FieldInfo m_fi_trackRowAnchor;

		///// <summary>The anchor cell data during a selection</summary>
		//[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		//public SelectionAnchorData SelectionAnchor
		//{
		//	get { return new SelectionAnchorData(this); }
		//	set
		//	{
		//		SelectionAnchorCell = value.SelectionAnchorCell;
		//		MouseDownCell       = value.MouseDownCell      ;
		//		TrackColumn         = value.TrackColumn        ;
		//		TrackColumnEdge     = value.TrackColumnEdge    ;
		//		TrackColumnAnchor   = value.TrackColumnAnchor  ;
		//		TrackRow            = value.TrackRow           ;
		//		TrackRowEdge        = value.TrackRowEdge       ;
		//		TrackRowAnchor      = value.TrackRowAnchor     ;
		//	}
		//}

		///// <summary>Anchor cell data during a selection</summary>
		//public class SelectionAnchorData
		//{
		//	public SelectionAnchorData(DataGridView dgv)
		//	{
		//		SelectionAnchorCell = dgv.SelectionAnchorCell;
		//		MouseDownCell       = dgv.MouseDownCell      ;
		//		TrackColumn         = dgv.TrackColumn        ;
		//		TrackColumnEdge     = dgv.TrackColumnEdge    ;
		//		TrackColumnAnchor   = dgv.TrackColumnAnchor  ;
		//		TrackRow            = dgv.TrackRow           ;
		//		TrackRowEdge        = dgv.TrackRowEdge       ;
		//		TrackRowAnchor      = dgv.TrackRowAnchor     ;
		//	}
		//	public Point SelectionAnchorCell { get; set; }
		//	public Point MouseDownCell       { get; set; }
		//	public int TrackColumn           { get; set; }
		//	public int TrackColumnEdge       { get; set; }
		//	public int TrackColumnAnchor     { get; set; }
		//	public int TrackRow              { get; set; }
		//	public int TrackRowEdge          { get; set; }
		//	public int TrackRowAnchor        { get; set; }

		//	public void Shift(Point delta, int col_count, int row_count)
		//	{
		//		SelectionAnchorCell = new Point(
		//			Maths.Clamp(SelectionAnchorCell.X + delta.X, 0, col_count-1),
		//			Maths.Clamp(SelectionAnchorCell.Y + delta.Y, 0, row_count-1));

		//		MouseDownCell = new Point(
		//			Maths.Clamp(MouseDownCell.X + delta.X, 0, col_count-1),
		//			Maths.Clamp(MouseDownCell.Y + delta.Y, 0, row_count-1));

		//		TrackColumn         = Maths.Clamp(TrackColumn       + delta.X, 0, col_count-1);
		//		TrackColumnEdge     = Maths.Clamp(TrackColumnEdge   + delta.X, 0, col_count-1);
		//		TrackColumnAnchor   = Maths.Clamp(TrackColumnAnchor + delta.X, 0, col_count-1);
		//		TrackRow            = Maths.Clamp(TrackRow          + delta.Y, 0, row_count-1);
		//		TrackRowEdge        = Maths.Clamp(TrackRowEdge      + delta.Y, 0, row_count-1);
		//		TrackRowAnchor      = Maths.Clamp(TrackRowAnchor    + delta.Y, 0, row_count-1);
		//	}
		//}
	}
}
