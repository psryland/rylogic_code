using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Maths;
using Rylogic.Gui.WinForms;
using DataGridView = Rylogic.Gui.WinForms.DataGridView;

namespace Rylogic.TextAligner
{
	[DesignerCategory("Code")]
	[ComplexBindingProperties]
	[Docking(DockingBehavior.Ask)]
	internal sealed class GroupGrid :DataGridView
	{
		private enum EGroupColumns
		{
			Name,
			LeadingSpace,
		}

		/// <summary>Can't do this in the constructor because the designer screws it up</summary>
		public GroupGrid()
		{
			if (this.IsInDesignMode())
				return;

			VirtualMode = true;
			AutoGenerateColumns = false;
			//m_data = new BindingSource();

			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Name",
				Tag = EGroupColumns.Name,
				FillWeight = 100
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Leading Space",
				Tag = EGroupColumns.LeadingSpace,
				FillWeight = 30
			});
		}

		/// <summary>Alignment group data source</summary>
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
				Data.Position = Math_.Clamp(row.Index, 0, Data.Count -  1);
		}

		/// <summary>Supply cell values</summary>
		protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
		{
			base.OnCellValueNeeded(e);
			if (e.RowIndex    < 0 || e.RowIndex    >= Data.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;
			// Note Data.Count != RowCount, because of the 'insert new row' row

			var grp = (AlignGroup)Data[e.RowIndex];
			switch ((EGroupColumns)Columns[e.ColumnIndex].Tag)
			{
			default:
				e.Value = string.Empty;
				break;
			case EGroupColumns.Name:
				e.Value = grp.Name;
				break;
			case EGroupColumns.LeadingSpace:
				e.Value = grp.LeadingSpace.ToString(CultureInfo.InvariantCulture);
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

			switch ((EGroupColumns)Columns[e.ColumnIndex].Tag)
			{
			default:
				Rows[e.RowIndex].ErrorText = string.Empty;
				break;
			case EGroupColumns.Name:
				break;
			case EGroupColumns.LeadingSpace:
				int ws;
				if (!int.TryParse(e.FormattedValue.ToString(), out ws) || ws < 0)
				{
					e.Cancel = true;
					Rows[e.RowIndex].ErrorText = "Leading white space must be greater or equal to zero";
				}
				break;
			}
		}

		/// <summary>Update after cell editing</summary>
		protected override void OnCellValuePushed(DataGridViewCellValueEventArgs e)
		{
			base.OnCellValuePushed(e);
			if (e.RowIndex    < 0 || e.RowIndex    >= Data.Count) return;
			if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;

			var grp = (AlignGroup)Data[e.RowIndex];
			switch ((EGroupColumns)Columns[e.ColumnIndex].Tag)
			{
			case EGroupColumns.Name:
				grp.Name = ((string)e.Value).HasValue() ? e.Value.ToString() : "AlignGroup";
				break;
			case EGroupColumns.LeadingSpace:
				grp.LeadingSpace = int.Parse(e.Value.ToString());
				break;
			}
		}

		/// <summary>Cell tool tips</summary>
		protected override void OnCellToolTipTextNeeded(DataGridViewCellToolTipTextNeededEventArgs e)
		{
			base.OnCellToolTipTextNeeded(e);
			if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;
			if (e.RowIndex == -1)
			{
				switch ((EGroupColumns)e.ColumnIndex)
				{
				case EGroupColumns.Name:
					e.ToolTipText = "The name of the alignment group";
					break;
				case EGroupColumns.LeadingSpace:
					e.ToolTipText = "The number of white space characters added in front of the aligned text";
					break;
				}
			}
		}
	}
}
