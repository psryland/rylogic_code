using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Rylogic.VSExtension.Properties;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylogic.VSExtension
{
	[DesignerCategory("Code")]
	[ComplexBindingProperties]
	[Docking(DockingBehavior.Ask)]
	internal sealed class PatternGrid :DataGridView
	{
		private enum EPatternColumns
		{
			Active,
			Pattern,
			Offset,
			MinWidth,
			Edit
		}

		public PatternGrid()
		{
			if (Util.DesignTime) return;

			// The patterns grid is virtual mode so that we can draw images and handle edits
			AutoGenerateColumns = false;
			AllowUserToAddRows = true;
			Columns.Add(new DataGridViewImageColumn   {Tag = EPatternColumns.Active   ,HeaderText = EPatternColumns.Active   .ToStringFast() ,FillWeight = 25  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});
			Columns.Add(new DataGridViewTextBoxColumn {Tag = EPatternColumns.Pattern  ,HeaderText = EPatternColumns.Pattern  .ToStringFast() ,FillWeight = 100 ,ReadOnly = true });
			Columns.Add(new DataGridViewTextBoxColumn {Tag = EPatternColumns.Offset   ,HeaderText = EPatternColumns.Offset   .ToStringFast() ,FillWeight = 30 });
			Columns.Add(new DataGridViewTextBoxColumn {Tag = EPatternColumns.MinWidth ,HeaderText = EPatternColumns.MinWidth .ToStringFast() ,FillWeight = 30 });
			Columns.Add(new DataGridViewImageColumn   {Tag = EPatternColumns.Edit     ,HeaderText = EPatternColumns.Edit     .ToStringFast() ,FillWeight = 15  ,ReadOnly = true ,AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader ,ImageLayout = DataGridViewImageCellLayout.Zoom});

			// Clear the 'X' null image
			foreach (var col in Columns.OfType<DataGridViewImageColumn>())
				col.DefaultCellStyle.NullValue = null;
		}

		/// <summary>A method to call when the edit pattern dialog is needed</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public Action<AlignPattern> ShowEditPatternDlg { get; set; }

		/// <summary>A method that closes an edit pattern dialog because the pattern is being deleted</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public Action<AlignPattern> CloseEditPatternDlg { get; set; }

		/// <summary>The data source</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public BindingSource Data
		{
			get { return (BindingSource)DataSource; }
			set { DataSource = value; }
		}

		/// <summary>Called when the current cell is changed</summary>
		protected override void OnCurrentCellChanged(EventArgs e)
		{
			base.OnCurrentCellChanged(e);
			var row = CurrentRow;
			if (row != null && Data.Count != 0)
				Data.Position = Maths.Clamp(row.Index, 0, Data.Count -  1);
		}

		/// <summary>Supply cell values</summary>
		protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			base.OnCellValueNeeded(e);

			if (e.RowIndex    < 0 || e.RowIndex    >= Data.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;
			// Note Data.Count != RowCount, because of the 'insert new row' row

			var pat = (AlignPattern)Data[e.RowIndex];
			switch ((EPatternColumns)Columns[e.ColumnIndex].Tag)
			{
			default:
				e.Value = string.Empty;
				break;
			case EPatternColumns.Pattern:
				var val = pat.ToString();
				if (string.IsNullOrEmpty(val)) val = "<blank>";
				if (string.IsNullOrWhiteSpace(val)) val = "'"+val+"'";
				if (pat.Invert) val = "not " + val;
				e.Value = val;
				break;
			case EPatternColumns.Offset:
				e.Value = pat.Offset;
				break;
			case EPatternColumns.MinWidth:
				e.Value = pat.MinWidth;
				break;
			}
		}

		/// <summary>Validate user data</summary>
		protected override void OnCellValidating(DataGridViewCellValidatingEventArgs e)
		{
			base.OnCellValidating(e);
			if (e.RowIndex    < 0 || e.RowIndex    >= Data.Count) return;
			if (e.RowIndex    < 0 || e.RowIndex    >= RowCount) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;

			switch ((EPatternColumns)Columns[e.ColumnIndex].Tag)
			{
			default:
				Rows[e.RowIndex].ErrorText = string.Empty;
				break;
			case EPatternColumns.Offset:
				int ofs;
				if (!int.TryParse(e.FormattedValue.ToString(), out ofs))
				{
					e.Cancel = true;
					Rows[e.RowIndex].ErrorText = "Offset must be a positive or negative integer";
				}
				break;
			case EPatternColumns.MinWidth:
				int width;
				if (!int.TryParse(e.FormattedValue.ToString(), out width) || width < 0)
				{
					e.Cancel = true;
					Rows[e.RowIndex].ErrorText = "Min Width must be a positive integer";
				}
				break;
			}
		}

		/// <summary>Update a cell value after editing</summary>
		protected override void OnCellValuePushed(DataGridViewCellValueEventArgs e)
		{
			base.OnCellValuePushed(e);
			if (e.RowIndex    < 0 || e.RowIndex    >= Data.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;

			var pat = (AlignPattern)Data[e.RowIndex];
			switch ((EPatternColumns)Columns[e.ColumnIndex].Tag)
			{
			case EPatternColumns.Offset:
				pat.Offset = int.Parse(e.Value.ToString());
				break;
			case EPatternColumns.MinWidth:
				pat.MinWidth = int.Parse(e.Value.ToString());
				break;
			}
		}

		/// <summary>Cell formatting</summary>
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			base.OnCellFormatting(e);
			if (e.RowIndex    < 0 || e.RowIndex    >= RowCount) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;

			if (e.RowIndex >= Data.Count)
			{
				e.Value = null;
			}
			else
			{
				var pat = (AlignPattern)Data[e.RowIndex];
				switch ((EPatternColumns)Columns[e.ColumnIndex].Tag)
				{
				default: return;
				case EPatternColumns.Active:
					e.Value = pat.Active ? Resources.green_tick : Resources.gray_cross;
					break;
				case EPatternColumns.Edit:
					e.Value = Resources.pencil;
					break;
				}
			}
		}

		/// <summary>Cell tool tips</summary>
		protected override void OnCellToolTipTextNeeded(DataGridViewCellToolTipTextNeededEventArgs e)
		{
			base.OnCellToolTipTextNeeded(e);
			if (e.RowIndex    < 0 || e.RowIndex    >= Data.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;

			var pat = (AlignPattern)Data[e.RowIndex];
			switch ((EPatternColumns)Columns[e.ColumnIndex].Tag)
			{
			default: return;
			case EPatternColumns.Pattern:
				e.ToolTipText= pat.Comment;
				break;
			}
		}

		/// <summary>Handle cell clicks</summary>
		protected override void OnCellClick(DataGridViewCellEventArgs e)
		{
			base.OnCellClick(e);
			if (e.RowIndex    < 0 || e.RowIndex    >= Data.Count) return;
			if (e.RowIndex    < 0 || e.RowIndex    >= RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;

			var pat = (AlignPattern)Data[e.RowIndex];
			switch ((EPatternColumns)Columns[e.ColumnIndex].Tag)
			{
			default: return;
			case EPatternColumns.Active:
				pat.Active = !pat.Active;
				break;
			case EPatternColumns.Edit:
				ShowEditPatternDlg(pat);
				break;
			}
		}

		/// <summary>Double click edits the pattern</summary>
		protected override void OnCellDoubleClick(DataGridViewCellEventArgs e)
		{
			base.OnCellDoubleClick(e);
			if (e.RowIndex    < 0 || e.RowIndex    >= RowCount   ) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;

			if (e.RowIndex >= Data.Count)
			{
				ShowEditPatternDlg(null);
			}
			else
			{
				var pat = (AlignPattern)Data[e.RowIndex];
				switch ((EPatternColumns)Columns[e.ColumnIndex].Tag)
				{
				case EPatternColumns.Pattern:
					ShowEditPatternDlg(pat);
					break;
				}
			}
		}

		/// <summary>Delete a pattern</summary>
		protected override void OnUserDeletingRow(DataGridViewRowCancelEventArgs e)
		{
			base.OnUserDeletingRow(e);
			if (e.Row.Index < 0 || e.Row.Index >= Data.Count)
				return;

			// Record the row index because it becomes -1 when 'e.Row' is deleted
			var row_index = e.Row.Index;

			// Note, don't update the grid here or it causes an ArgumentOutOfRange exception.
			// Other stuff must be using the grid row that will be deleted.
			var pat = (AlignPattern)Data[row_index];
			CloseEditPatternDlg(pat);
		}
	}
}
