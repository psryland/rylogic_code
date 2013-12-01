using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Forms;
using pr.extn;
using pr.maths;
using pr.util;

namespace Rylogic.VSExtension
{
	//[DesignerCategory("Code")]
	//[ComplexBindingProperties]
	//[Docking(DockingBehavior.Ask)]
	//internal sealed class GroupList :ListView
	//{
	//	private enum EGroupColumns
	//	{
	//		Name,
	//		LeadingSpace,
	//	}

	//	/// <summary>Can't do this in the constructor because the designer screws it up</summary>
	//	public void Init()
	//	{
	//		VirtualMode = true;
	//		Columns.Add(new ColumnHeader{Tag = EGroupColumns.Name        , Text = EGroupColumns.Name        .ToStringFast()});
	//		Columns.Add(new ColumnHeader{Tag = EGroupColumns.LeadingSpace, Text = EGroupColumns.LeadingSpace.ToStringFast()});
	//	}

	//	/// <summary>The data source</summary>
	//	[Browsable(false)]
	//	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	//	[EditorBrowsable(EditorBrowsableState.Never)]
	//	public BindingSource Data
	//	{
	//		get { return m_data; }
	//		set
	//		{
	//			if (ReferenceEquals(m_data, value)) return;
	//			ListChangedEventHandler OnDataChanged = (s,a) => Refresh();
	//			m_data.ListChanged -= OnDataChanged;
	//			m_data = value ?? new BindingSource();
	//			m_data.ListChanged += OnDataChanged;
	//			Refresh();
	//		}
	//	}
	//	private BindingSource m_data = new BindingSource();

	//	/// <summary>Forces the control to invalidate its client area and immediately redraw itself and any child controls.</summary>
	//	public override void Refresh()
	//	{
	//		RowCount = Data.Count + (AllowUserToAddRows ? 1 : 0);
	//		base.Refresh();
	//	}

	//	/// <summary>Called when the current cell is changed</summary>
	//	protected override void OnCurrentCellChanged(EventArgs e)
	//	{
	//		base.OnCurrentCellChanged(e);
	//		var row = CurrentRow;
	//		if (row != null && Data.Count != 0)
	//			Data.Position = Maths.Clamp(row.Index, 0, Data.Count -  1);
	//	}

	//	/// <summary>Supply cell values</summary>
	//	protected override void OnCellValueNeeded(DataGridViewCellValueEventArgs e)
	//	{
	//		base.OnCellValueNeeded(e);
	//		if (e.RowIndex    < 0 || e.RowIndex    >= Data.Count) return;
	//		if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;
	//		// Note Data.Count != RowCount, because of the 'insert new row' row

	//		var grp = (AlignGroup)Data[e.RowIndex];
	//		switch ((EGroupColumns)Columns[e.ColumnIndex].Tag)
	//		{
	//		default:
	//			e.Value = string.Empty;
	//			break;
	//		case EGroupColumns.Name:
	//			e.Value = grp.Name;
	//			break;
	//		case EGroupColumns.LeadingSpace:
	//			e.Value = grp.LeadingSpace.ToString(CultureInfo.InvariantCulture);
	//			break;
	//		}
	//	}

	//	/// <summary>Validate user data</summary>
	//	protected override void OnCellValidating(DataGridViewCellValidatingEventArgs e)
	//	{
	//		base.OnCellValidating(e);
	//		if (e.RowIndex    < 0 || e.RowIndex    >= Data.Count) return;
	//		if (e.RowIndex    < 0 || e.RowIndex    >= RowCount) return;
	//		if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;

	//		switch ((EGroupColumns)Columns[e.ColumnIndex].Tag)
	//		{
	//		default:
	//			Rows[e.RowIndex].ErrorText = string.Empty;
	//			break;
	//		case EGroupColumns.Name:
	//			break;
	//		case EGroupColumns.LeadingSpace:
	//			int ws;
	//			if (!int.TryParse(e.FormattedValue.ToString(), out ws) || ws < 0)
	//			{
	//				e.Cancel = true;
	//				Rows[e.RowIndex].ErrorText = "Leading white space must be greater or equal to zero";
	//			}
	//			break;
	//		}
	//	}

	//	/// <summary>Update after cell editing</summary>
	//	protected override void OnCellValuePushed(DataGridViewCellValueEventArgs e)
	//	{
	//		base.OnCellValuePushed(e);
	//		if (e.RowIndex    < 0 || e.RowIndex    >= Data.Count) return;
	//		if (e.ColumnIndex < 0 || e.ColumnIndex >= ColumnCount) return;

	//		var grp = (AlignGroup)Data[e.RowIndex];
	//		switch ((EGroupColumns)Columns[e.ColumnIndex].Tag)
	//		{
	//		case EGroupColumns.Name:
	//			grp.Name = ((string)e.Value).HasValue() ? e.Value.ToString() : "AlignGroup";
	//			break;
	//		case EGroupColumns.LeadingSpace:
	//			grp.LeadingSpace = int.Parse(e.Value.ToString());
	//			break;
	//		}
	//	}

	//	protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)
	//	{
	//		base.OnEditingControlShowing(e);
	//		var tb = e.Control as TextBox;
	//		if (tb != null)
	//		{
	//			tb.AcceptsReturn = true;
	//			tb.PreviewKeyDown += (s,a) =>
	//				{
	//					if (a.KeyCode == Keys.Return)
	//						EndEdit();
	//				};
	//		}
	//	}
	//}

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
		public void Init()
		{
			VirtualMode = true;
			AutoGenerateColumns = false;
			Columns.Add(new DataGridViewTextBoxColumn{Tag = EGroupColumns.Name        , HeaderText = EGroupColumns.Name        .ToStringFast(), FillWeight = 100});
			Columns.Add(new DataGridViewTextBoxColumn{Tag = EGroupColumns.LeadingSpace, HeaderText = EGroupColumns.LeadingSpace.ToStringFast(), FillWeight = 30});
		}

		/// <summary>The data source</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		[EditorBrowsable(EditorBrowsableState.Never)]
		public BindingSource Data
		{
			get { return m_data; }
			set
			{
				if (ReferenceEquals(m_data, value)) return;
				ListChangedEventHandler OnDataChanged = (s,a) => Refresh();
				m_data.ListChanged -= OnDataChanged;
				m_data = value ?? new BindingSource();
				m_data.ListChanged += OnDataChanged;
				Refresh();
			}
		}
		private BindingSource m_data = new BindingSource();

		/// <summary>Forces the control to invalidate its client area and immediately redraw itself and any child controls.</summary>
		public override void Refresh()
		{
			RowCount = Data.Count + (AllowUserToAddRows ? 1 : 0);
			base.Refresh();
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

		protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)
		{
			base.OnEditingControlShowing(e);
			var tb = e.Control as TextBox;
			if (tb != null)
			{
				tb.AcceptsReturn = true;
				tb.PreviewKeyDown += (s,a) =>
					{
						if (a.KeyCode == Keys.Enter)
						{
							a.IsInputKey = false;
						}
					};
			}
		}
	}
}
