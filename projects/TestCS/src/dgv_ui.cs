using System;
using System.ComponentModel;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.gui;
using pr.util;
using ListBox = pr.gui.ListBox;

namespace TestCS
{
	public class DgvUI :Form
	{
		private class DGV :DataGridView
		{
			public ListBox List { get; set; }
			public void Log(string method)
			{
				if (List == null) return;
				var rows_visible = List.ClientSize.Height / List.ItemHeight;
				var at_bottom = List.TopIndex >= (List.Items.Count - rows_visible + 1);
				List.Items.Add(method);
				List.TopIndex = List.Items.Count - 1;
			}

			#region Overrides
			protected override void   OnAllowUserToAddRowsChanged(EventArgs e)                                         { Log("OnAllowUserToAddRowsChanged             "); base.OnAllowUserToAddRowsChanged             (e);}
			protected override void   OnAllowUserToDeleteRowsChanged(EventArgs e)                                      { Log("OnAllowUserToDeleteRowsChanged          "); base.OnAllowUserToDeleteRowsChanged          (e);}
			protected override void   OnAllowUserToOrderColumnsChanged(EventArgs e)                                    { Log("OnAllowUserToOrderColumnsChanged        "); base.OnAllowUserToOrderColumnsChanged        (e);}
			protected override void   OnAllowUserToResizeColumnsChanged(EventArgs e)                                   { Log("OnAllowUserToResizeColumnsChanged       "); base.OnAllowUserToResizeColumnsChanged       (e);}
			protected override void   OnAllowUserToResizeRowsChanged(EventArgs e)                                      { Log("OnAllowUserToResizeRowsChanged          "); base.OnAllowUserToResizeRowsChanged          (e);}
			protected override void   OnAlternatingRowsDefaultCellStyleChanged(EventArgs e)                            { Log("OnAlternatingRowsDefaultCellStyleChanged"); base.OnAlternatingRowsDefaultCellStyleChanged(e);}
			protected override void   OnAutoGenerateColumnsChanged(EventArgs e)                                        { Log("OnAutoGenerateColumnsChanged            "); base.OnAutoGenerateColumnsChanged            (e);}
			protected override void   OnAutoSizeColumnModeChanged(DataGridViewAutoSizeColumnModeEventArgs e)           { Log("OnAutoSizeColumnModeChanged             "); base.OnAutoSizeColumnModeChanged             (e);}
			protected override void   OnAutoSizeColumnsModeChanged(DataGridViewAutoSizeColumnsModeEventArgs e)         { Log("OnAutoSizeColumnsModeChanged            "); base.OnAutoSizeColumnsModeChanged            (e);}
			protected override void   OnAutoSizeRowsModeChanged(DataGridViewAutoSizeModeEventArgs e)                   { Log("OnAutoSizeRowsModeChanged               "); base.OnAutoSizeRowsModeChanged               (e);}
			protected override void   OnBackgroundColorChanged(EventArgs e)                                            { Log("OnBackgroundColorChanged                "); base.OnBackgroundColorChanged                (e);}
			protected override void   OnBindingContextChanged(EventArgs e)                                             { Log("OnBindingContextChanged                 "); base.OnBindingContextChanged                 (e);}
			protected override void   OnBorderStyleChanged(EventArgs e)                                                { Log("OnBorderStyleChanged                    "); base.OnBorderStyleChanged                    (e);}
			protected override void   OnCancelRowEdit(QuestionEventArgs e)                                             { Log("OnCancelRowEdit                         "); base.OnCancelRowEdit                         (e);}
			protected override void   OnCellBeginEdit(DataGridViewCellCancelEventArgs e)                               { Log("OnCellBeginEdit                         "); base.OnCellBeginEdit                         (e);}
			protected override void   OnCellBorderStyleChanged(EventArgs e)                                            { Log("OnCellBorderStyleChanged                "); base.OnCellBorderStyleChanged                (e);}
			protected override void   OnCellClick(DataGridViewCellEventArgs e)                                         { Log("OnCellClick                             "); base.OnCellClick                             (e);}
			protected override void   OnCellContentClick(DataGridViewCellEventArgs e)                                  { Log("OnCellContentClick                      "); base.OnCellContentClick                      (e);}
			protected override void   OnCellContentDoubleClick(DataGridViewCellEventArgs e)                            { Log("OnCellContentDoubleClick                "); base.OnCellContentDoubleClick                (e);}
			protected override void   OnCellContextMenuStripChanged(DataGridViewCellEventArgs e)                       { Log("OnCellContextMenuStripChanged           "); base.OnCellContextMenuStripChanged           (e);}
			protected override void   OnCellContextMenuStripNeeded(DataGridViewCellContextMenuStripNeededEventArgs e)  { Log("OnCellContextMenuStripNeeded            "); base.OnCellContextMenuStripNeeded            (e);}
			protected override void   OnCellDoubleClick(DataGridViewCellEventArgs e)                                   { Log("OnCellDoubleClick                       "); base.OnCellDoubleClick                       (e);}
			protected override void   OnCellEndEdit(DataGridViewCellEventArgs e)                                       { Log("OnCellEndEdit                           "); base.OnCellEndEdit                           (e);}
			protected override void   OnCellEnter(DataGridViewCellEventArgs e)                                         { Log("OnCellEnter                             "); base.OnCellEnter                             (e);}
			protected override void   OnCellErrorTextChanged(DataGridViewCellEventArgs e)                              { Log("OnCellErrorTextChanged                  "); base.OnCellErrorTextChanged                  (e);}
			//protected override void   OnCellErrorTextNeeded(DataGridViewCellErrorTextNeededEventArgs e)                { Log("OnCellErrorTextNeeded                   "); base.OnCellErrorTextNeeded                   (e);}
			protected override void   OnCellFormatting(DataGridViewCellFormattingEventArgs e)                          { Log("OnCellFormatting                        "); base.OnCellFormatting                        (e);}
			protected override void   OnCellLeave(DataGridViewCellEventArgs e)                                         { Log("OnCellLeave                             "); base.OnCellLeave                             (e);}
			protected override void   OnCellMouseClick(DataGridViewCellMouseEventArgs e)                               { Log("OnCellMouseClick                        "); base.OnCellMouseClick                        (e);}
			protected override void   OnCellMouseDoubleClick(DataGridViewCellMouseEventArgs e)                         { Log("OnCellMouseDoubleClick                  "); base.OnCellMouseDoubleClick                  (e);}
			protected override void   OnCellMouseDown(DataGridViewCellMouseEventArgs e)                                { Log("OnCellMouseDown                         "); base.OnCellMouseDown                         (e);}
			protected override void   OnCellMouseEnter(DataGridViewCellEventArgs e)                                    { Log("OnCellMouseEnter                        "); base.OnCellMouseEnter                        (e);}
			protected override void   OnCellMouseLeave(DataGridViewCellEventArgs e)                                    { Log("OnCellMouseLeave                        "); base.OnCellMouseLeave                        (e);}
			//protected override void   OnCellMouseMove(DataGridViewCellMouseEventArgs e)                                { Log("OnCellMouseMove                         "); base.OnCellMouseMove                         (e);}
			protected override void   OnCellMouseUp(DataGridViewCellMouseEventArgs e)                                  { Log("OnCellMouseUp                           "); base.OnCellMouseUp                           (e);}
			protected override void   OnCellPainting(DataGridViewCellPaintingEventArgs e)                              { Log("OnCellPainting                          "); base.OnCellPainting                          (e);}
			protected override void   OnCellParsing(DataGridViewCellParsingEventArgs e)                                { Log("OnCellParsing                           "); base.OnCellParsing                           (e);}
			protected override void   OnCellStateChanged(DataGridViewCellStateChangedEventArgs e)                      { Log("OnCellStateChanged                      "); base.OnCellStateChanged                      (e);}
			protected override void   OnCellStyleChanged(DataGridViewCellEventArgs e)                                  { Log("OnCellStyleChanged                      "); base.OnCellStyleChanged                      (e);}
			protected override void   OnCellStyleContentChanged(DataGridViewCellStyleContentChangedEventArgs e)        { Log("OnCellStyleContentChanged               "); base.OnCellStyleContentChanged               (e);}
			protected override void   OnCellToolTipTextChanged(DataGridViewCellEventArgs e)                            { Log("OnCellToolTipTextChanged                "); base.OnCellToolTipTextChanged                (e);}
			protected override void   OnCellToolTipTextNeeded(DataGridViewCellToolTipTextNeededEventArgs e)            { Log("OnCellToolTipTextNeeded                 "); base.OnCellToolTipTextNeeded                 (e);}
			protected override void   OnCellValidated(DataGridViewCellEventArgs e)                                     { Log("OnCellValidated                         "); base.OnCellValidated                         (e);}
			protected override void   OnCellValidating(DataGridViewCellValidatingEventArgs e)                          { Log("OnCellValidating                        "); base.OnCellValidating                        (e);}
			protected override void   OnCellValueChanged(DataGridViewCellEventArgs e)                                  { Log("OnCellValueChanged                      "); base.OnCellValueChanged                      (e);}
			protected override void   OnCellValueNeeded(DataGridViewCellValueEventArgs e)                              { Log("OnCellValueNeeded                       "); base.OnCellValueNeeded                       (e);}
			protected override void   OnCellValuePushed(DataGridViewCellValueEventArgs e)                              { Log("OnCellValuePushed                       "); base.OnCellValuePushed                       (e);}
			protected override void   OnColumnAdded(DataGridViewColumnEventArgs e)                                     { Log("OnColumnAdded                           "); base.OnColumnAdded                           (e);}
			protected override void   OnColumnContextMenuStripChanged(DataGridViewColumnEventArgs e)                   { Log("OnColumnContextMenuStripChanged         "); base.OnColumnContextMenuStripChanged         (e);}
			protected override void   OnColumnDataPropertyNameChanged(DataGridViewColumnEventArgs e)                   { Log("OnColumnDataPropertyNameChanged         "); base.OnColumnDataPropertyNameChanged         (e);}
			protected override void   OnColumnDefaultCellStyleChanged(DataGridViewColumnEventArgs e)                   { Log("OnColumnDefaultCellStyleChanged         "); base.OnColumnDefaultCellStyleChanged         (e);}
			protected override void   OnColumnDisplayIndexChanged(DataGridViewColumnEventArgs e)                       { Log("OnColumnDisplayIndexChanged             "); base.OnColumnDisplayIndexChanged             (e);}
			protected override void   OnColumnDividerDoubleClick(DataGridViewColumnDividerDoubleClickEventArgs e)      { Log("OnColumnDividerDoubleClick              "); base.OnColumnDividerDoubleClick              (e);}
			protected override void   OnColumnDividerWidthChanged(DataGridViewColumnEventArgs e)                       { Log("OnColumnDividerWidthChanged             "); base.OnColumnDividerWidthChanged             (e);}
			protected override void   OnColumnHeaderCellChanged(DataGridViewColumnEventArgs e)                         { Log("OnColumnHeaderCellChanged               "); base.OnColumnHeaderCellChanged               (e);}
			protected override void   OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)                       { Log("OnColumnHeaderMouseClick                "); base.OnColumnHeaderMouseClick                (e);}
			protected override void   OnColumnHeaderMouseDoubleClick(DataGridViewCellMouseEventArgs e)                 { Log("OnColumnHeaderMouseDoubleClick          "); base.OnColumnHeaderMouseDoubleClick          (e);}
			protected override void   OnColumnHeadersBorderStyleChanged(EventArgs e)                                   { Log("OnColumnHeadersBorderStyleChanged       "); base.OnColumnHeadersBorderStyleChanged       (e);}
			protected override void   OnColumnHeadersDefaultCellStyleChanged(EventArgs e)                              { Log("OnColumnHeadersDefaultCellStyleChanged  "); base.OnColumnHeadersDefaultCellStyleChanged  (e);}
			protected override void   OnColumnHeadersHeightChanged(EventArgs e)                                        { Log("OnColumnHeadersHeightChanged            "); base.OnColumnHeadersHeightChanged            (e);}
			protected override void   OnColumnHeadersHeightSizeModeChanged(DataGridViewAutoSizeModeEventArgs e)        { Log("OnColumnHeadersHeightSizeModeChanged    "); base.OnColumnHeadersHeightSizeModeChanged    (e);}
			protected override void   OnColumnMinimumWidthChanged(DataGridViewColumnEventArgs e)                       { Log("OnColumnMinimumWidthChanged             "); base.OnColumnMinimumWidthChanged             (e);}
			protected override void   OnColumnNameChanged(DataGridViewColumnEventArgs e)                               { Log("OnColumnNameChanged                     "); base.OnColumnNameChanged                     (e);}
			protected override void   OnColumnRemoved(DataGridViewColumnEventArgs e)                                   { Log("OnColumnRemoved                         "); base.OnColumnRemoved                         (e);}
			protected override void   OnColumnSortModeChanged(DataGridViewColumnEventArgs e)                           { Log("OnColumnSortModeChanged                 "); base.OnColumnSortModeChanged                 (e);}
			protected override void   OnColumnStateChanged(DataGridViewColumnStateChangedEventArgs e)                  { Log("OnColumnStateChanged                    "); base.OnColumnStateChanged                    (e);}
			protected override void   OnColumnToolTipTextChanged(DataGridViewColumnEventArgs e)                        { Log("OnColumnToolTipTextChanged              "); base.OnColumnToolTipTextChanged              (e);}
			protected override void   OnColumnWidthChanged(DataGridViewColumnEventArgs e)                              { Log("OnColumnWidthChanged                    "); base.OnColumnWidthChanged                    (e);}
			protected override void   OnCurrentCellChanged(EventArgs e)                                                { Log("OnCurrentCellChanged                    "); base.OnCurrentCellChanged                    (e);}
			protected override void   OnCurrentCellDirtyStateChanged(EventArgs e)                                      { Log("OnCurrentCellDirtyStateChanged          "); base.OnCurrentCellDirtyStateChanged          (e);}
			protected override void   OnCursorChanged(EventArgs e)                                                     { Log("OnCursorChanged                         "); base.OnCursorChanged                         (e);}
			protected override void   OnDataBindingComplete(DataGridViewBindingCompleteEventArgs e)                    { Log("OnDataBindingComplete                   "); base.OnDataBindingComplete                   (e);}
			protected override void   OnDataMemberChanged(EventArgs e)                                                 { Log("OnDataMemberChanged                     "); base.OnDataMemberChanged                     (e);}
			protected override void   OnDataSourceChanged(EventArgs e)                                                 { Log("OnDataSourceChanged                     "); base.OnDataSourceChanged                     (e);}
			protected override void   OnDefaultCellStyleChanged(EventArgs e)                                           { Log("OnDefaultCellStyleChanged               "); base.OnDefaultCellStyleChanged               (e);}
			protected override void   OnDefaultValuesNeeded(DataGridViewRowEventArgs e)                                { Log("OnDefaultValuesNeeded                   "); base.OnDefaultValuesNeeded                   (e);}
			protected override void   OnDoubleClick(EventArgs e)                                                       { Log("OnDoubleClick                           "); base.OnDoubleClick                           (e);}
			protected override void   OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)            { Log("OnEditingControlShowing                 "); base.OnEditingControlShowing                 (e);}
			protected override void   OnEditModeChanged(EventArgs e)                                                   { Log("OnEditModeChanged                       "); base.OnEditModeChanged                       (e);}
			protected override void   OnEnabledChanged(EventArgs e)                                                    { Log("OnEnabledChanged                        "); base.OnEnabledChanged                        (e);}
			protected override void   OnEnter(EventArgs e)                                                             { Log("OnEnter                                 "); base.OnEnter                                 (e);}
			protected override void   OnFontChanged(EventArgs e)                                                       { Log("OnFontChanged                           "); base.OnFontChanged                           (e);}
			protected override void   OnForeColorChanged(EventArgs e)                                                  { Log("OnForeColorChanged                      "); base.OnForeColorChanged                      (e);}
			protected override void   OnGotFocus(EventArgs e)                                                          { Log("OnGotFocus                              "); base.OnGotFocus                              (e);}
			protected override void   OnGridColorChanged(EventArgs e)                                                  { Log("OnGridColorChanged                      "); base.OnGridColorChanged                      (e);}
			protected override void   OnHandleCreated(EventArgs e)                                                     { Log("OnHandleCreated                         "); base.OnHandleCreated                         (e);}
			protected override void   OnHandleDestroyed(EventArgs e)                                                   { Log("OnHandleDestroyed                       "); base.OnHandleDestroyed                       (e);}
			protected override void   OnKeyDown(KeyEventArgs e)                                                        { Log("OnKeyDown                               "); base.OnKeyDown                               (e);}
			protected override void   OnKeyPress(KeyPressEventArgs e)                                                  { Log("OnKeyPress                              "); base.OnKeyPress                              (e);}
			protected override void   OnKeyUp(KeyEventArgs e)                                                          { Log("OnKeyUp                                 "); base.OnKeyUp                                 (e);}
			protected override void   OnLayout(LayoutEventArgs e)                                                      { Log("OnLayout                                "); base.OnLayout                                (e);}
			protected override void   OnLeave(EventArgs e)                                                             { Log("OnLeave                                 "); base.OnLeave                                 (e);}
			protected override void   OnLostFocus(EventArgs e)                                                         { Log("OnLostFocus                             "); base.OnLostFocus                             (e);}
			protected override void   OnMouseClick(MouseEventArgs e)                                                   { Log("OnMouseClick                            "); base.OnMouseClick                            (e);}
			protected override void   OnMouseDoubleClick(MouseEventArgs e)                                             { Log("OnMouseDoubleClick                      "); base.OnMouseDoubleClick                      (e);}
			protected override void   OnMouseDown(MouseEventArgs e)                                                    { Log("OnMouseDown                             "); base.OnMouseDown                             (e);}
			//protected override void   OnMouseEnter(EventArgs e)                                                        { Log("OnMouseEnter                            "); base.OnMouseEnter                            (e);}
			//protected override void   OnMouseLeave(EventArgs e)                                                        { Log("OnMouseLeave                            "); base.OnMouseLeave                            (e);}
			//protected override void OnMouseMove(MouseEventArgs e)                                                    { Log("OnMouseMove                             "); base.OnMouseMove                             (e);}
			protected override void   OnMouseUp(MouseEventArgs e)                                                      { Log("OnMouseUp                               "); base.OnMouseUp                               (e);}
			protected override void   OnMouseWheel(MouseEventArgs e)                                                   { Log("OnMouseWheel                            "); base.OnMouseWheel                            (e);}
			protected override void   OnMultiSelectChanged(EventArgs e)                                                { Log("OnMultiSelectChanged                    "); base.OnMultiSelectChanged                    (e);}
			protected override void   OnNewRowNeeded(DataGridViewRowEventArgs e)                                       { Log("OnNewRowNeeded                          "); base.OnNewRowNeeded                          (e);}
			protected override void   OnPaint(PaintEventArgs e)                                                        { Log("OnPaint                                 "); base.OnPaint                                 (e);}
			protected override void   OnReadOnlyChanged(EventArgs e)                                                   { Log("OnReadOnlyChanged                       "); base.OnReadOnlyChanged                       (e);}
			protected override void   OnResize(EventArgs e)                                                            { Log("OnResize                                "); base.OnResize                                (e);}
			protected override void   OnRightToLeftChanged(EventArgs e)                                                { Log("OnRightToLeftChanged                    "); base.OnRightToLeftChanged                    (e);}
			protected override void   OnRowContextMenuStripChanged(DataGridViewRowEventArgs e)                         { Log("OnRowContextMenuStripChanged            "); base.OnRowContextMenuStripChanged            (e);}
			protected override void   OnRowContextMenuStripNeeded(DataGridViewRowContextMenuStripNeededEventArgs e)    { Log("OnRowContextMenuStripNeeded             "); base.OnRowContextMenuStripNeeded             (e);}
			protected override void   OnRowDefaultCellStyleChanged(DataGridViewRowEventArgs e)                         { Log("OnRowDefaultCellStyleChanged            "); base.OnRowDefaultCellStyleChanged            (e);}
			protected override void   OnRowDirtyStateNeeded(QuestionEventArgs e)                                       { Log("OnRowDirtyStateNeeded                   "); base.OnRowDirtyStateNeeded                   (e);}
			protected override void   OnRowDividerDoubleClick(DataGridViewRowDividerDoubleClickEventArgs e)            { Log("OnRowDividerDoubleClick                 "); base.OnRowDividerDoubleClick                 (e);}
			protected override void   OnRowDividerHeightChanged(DataGridViewRowEventArgs e)                            { Log("OnRowDividerHeightChanged               "); base.OnRowDividerHeightChanged               (e);}
			protected override void   OnRowEnter(DataGridViewCellEventArgs e)                                          { Log("OnRowEnter                              "); base.OnRowEnter                              (e);}
			protected override void   OnRowErrorTextChanged(DataGridViewRowEventArgs e)                                { Log("OnRowErrorTextChanged                   "); base.OnRowErrorTextChanged                   (e);}
			//protected override void   OnRowErrorTextNeeded(DataGridViewRowErrorTextNeededEventArgs e)                  { Log("OnRowErrorTextNeeded                    "); base.OnRowErrorTextNeeded                    (e);}
			protected override void   OnRowHeaderCellChanged(DataGridViewRowEventArgs e)                               { Log("OnRowHeaderCellChanged                  "); base.OnRowHeaderCellChanged                  (e);}
			protected override void   OnRowHeaderMouseClick(DataGridViewCellMouseEventArgs e)                          { Log("OnRowHeaderMouseClick                   "); base.OnRowHeaderMouseClick                   (e);}
			protected override void   OnRowHeaderMouseDoubleClick(DataGridViewCellMouseEventArgs e)                    { Log("OnRowHeaderMouseDoubleClick             "); base.OnRowHeaderMouseDoubleClick             (e);}
			protected override void   OnRowHeadersBorderStyleChanged(EventArgs e)                                      { Log("OnRowHeadersBorderStyleChanged          "); base.OnRowHeadersBorderStyleChanged          (e);}
			protected override void   OnRowHeadersDefaultCellStyleChanged(EventArgs e)                                 { Log("OnRowHeadersDefaultCellStyleChanged     "); base.OnRowHeadersDefaultCellStyleChanged     (e);}
			protected override void   OnRowHeadersWidthChanged(EventArgs e)                                            { Log("OnRowHeadersWidthChanged                "); base.OnRowHeadersWidthChanged                (e);}
			protected override void   OnRowHeadersWidthSizeModeChanged(DataGridViewAutoSizeModeEventArgs e)            { Log("OnRowHeadersWidthSizeModeChanged        "); base.OnRowHeadersWidthSizeModeChanged        (e);}
			protected override void   OnRowHeightChanged(DataGridViewRowEventArgs e)                                   { Log("OnRowHeightChanged                      "); base.OnRowHeightChanged                      (e);}
			//protected override void   OnRowHeightInfoNeeded(DataGridViewRowHeightInfoNeededEventArgs e)                { Log("OnRowHeightInfoNeeded                   "); base.OnRowHeightInfoNeeded                   (e);}
			protected override void   OnRowHeightInfoPushed(DataGridViewRowHeightInfoPushedEventArgs e)                { Log("OnRowHeightInfoPushed                   "); base.OnRowHeightInfoPushed                   (e);}
			protected override void   OnRowLeave(DataGridViewCellEventArgs e)                                          { Log("OnRowLeave                              "); base.OnRowLeave                              (e);}
			protected override void   OnRowMinimumHeightChanged(DataGridViewRowEventArgs e)                            { Log("OnRowMinimumHeightChanged               "); base.OnRowMinimumHeightChanged               (e);}
			protected override void   OnRowPostPaint(DataGridViewRowPostPaintEventArgs e)                              { Log("OnRowPostPaint                          "); base.OnRowPostPaint                          (e);}
			protected override void   OnRowPrePaint(DataGridViewRowPrePaintEventArgs e)                                { Log("OnRowPrePaint                           "); base.OnRowPrePaint                           (e);}
			protected override void   OnRowsAdded(DataGridViewRowsAddedEventArgs e)                                    { Log("OnRowsAdded                             "); base.OnRowsAdded                             (e);}
			protected override void   OnRowsDefaultCellStyleChanged(EventArgs e)                                       { Log("OnRowsDefaultCellStyleChanged           "); base.OnRowsDefaultCellStyleChanged           (e);}
			protected override void   OnRowsRemoved(DataGridViewRowsRemovedEventArgs e)                                { Log("OnRowsRemoved                           "); base.OnRowsRemoved                           (e);}
			protected override void   OnRowUnshared(DataGridViewRowEventArgs e)                                        { Log("OnRowUnshared                           "); base.OnRowUnshared                           (e);}
			protected override void   OnRowValidated(DataGridViewCellEventArgs e)                                      { Log("OnRowValidated                          "); base.OnRowValidated                          (e);}
			protected override void   OnRowValidating(DataGridViewCellCancelEventArgs e)                               { Log("OnRowValidating                         "); base.OnRowValidating                         (e);}
			protected override void   OnScroll(ScrollEventArgs e)                                                      { Log("OnScroll                                "); base.OnScroll                                (e);}
			protected override void   OnSelectionChanged(EventArgs e)                                                  { Log("OnSelectionChanged                      "); base.OnSelectionChanged                      (e);}
			protected override void   OnSortCompare(DataGridViewSortCompareEventArgs e)                                { Log("OnSortCompare                           "); base.OnSortCompare                           (e);}
			protected override void   OnSorted(EventArgs e)                                                            { Log("OnSorted                                "); base.OnSorted                                (e);}
			protected override void   OnUserAddedRow(DataGridViewRowEventArgs e)                                       { Log("OnUserAddedRow                          "); base.OnUserAddedRow                          (e);}
			protected override void   OnUserDeletedRow(DataGridViewRowEventArgs e)                                     { Log("OnUserDeletedRow                        "); base.OnUserDeletedRow                        (e);}
			protected override void   OnUserDeletingRow(DataGridViewRowCancelEventArgs e)                              { Log("OnUserDeletingRow                       "); base.OnUserDeletingRow                       (e);}
			protected override void   OnValidating(CancelEventArgs e)                                                  { Log("OnValidating                            "); base.OnValidating                            (e);}
			protected override void   OnVisibleChanged(EventArgs e)                                                    { Log("OnVisibleChanged                        "); base.OnVisibleChanged                        (e);}
			protected override void   OnDataError(bool displayErrorDialogIfNoHandler, DataGridViewDataErrorEventArgs e){ Log("OnDataError                             "); base.OnDataError                             (displayErrorDialogIfNoHandler, e);}
			protected override void   OnRowStateChanged(int rowIndex, DataGridViewRowStateChangedEventArgs e)          { Log("OnRowStateChanged                       "); base.OnRowStateChanged                       (rowIndex, e);}
			#endregion
		}
		private enum EOptions
		{
			One,
			Two,
			Three,
		}
		private class Record
		{
			public string Name     { get; set; }
			public int Value       { get; set; }
			public EOptions Option { get; set; }
			public float FValue    { get; set; }
			public bool Checked    { get; set; }

			public Record() { }
			public Record(string name, int val, EOptions opt)
			{
				Name    = name;
				Value   = val;
				Option  = opt;
				FValue  = val * 6.28f;
				Checked = (val % 2) == 1;
			}
			public override string ToString()
			{
				return "{0} {1} {2} {3} {4}".Fmt(Name, Value, Option, FValue, Checked);
			}
		}

		private BindingSource<Record> m_bs;
		private DragDrop m_dd;
		private SplitContainer m_split;
		private ListBox m_events;
		private DGV m_grid;

		public DgvUI()
		{
			InitializeComponent();

			// Grid data
			m_bs = new BindingSource<Record>{DataSource = new BindingListEx<Record>()};
			for (int i = 0; i != 10; ++i)
				m_bs.Add(new Record("Name"+i, i, (EOptions)(i % 3)));

			// Drag drop
			m_dd = new DragDrop(m_grid);
			m_dd.DoDrop += DataGridViewEx.DragDrop_DoDropMoveRow;
			m_grid.MouseDown += DataGridViewEx.DragDrop_DragRow;

			// log list
			m_events.ContextMenuStrip = new ContextMenuStrip();
			m_events.ContextMenuStrip.Items.Add2("Log DGV events", null, (s,a) =>
			{
				m_grid.List = m_grid.List == null ? m_events : null;
				s.As<ToolStripMenuItem>().Checked = m_grid.List != null;
			});

			m_grid.AllowDrop = true;
			m_grid.AutoGenerateColumns = false;
			m_grid.AllowUserToAddRows = true;
			m_grid.AllowUserToDeleteRows = true;
			m_grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Name",
				DataPropertyName = nameof(Record.Name),
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "Value",
				DataPropertyName = nameof(Record.Value),
			});
			m_grid.Columns.Add(new DataGridViewComboBoxColumn
			{
				Name = "Option",
				DataPropertyName = nameof(Record.Option),
				FlatStyle = System.Windows.Forms.FlatStyle.Flat,
				DataSource = Enum<EOptions>.Values,
			});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = "FValue",
				DataPropertyName = nameof(Record.FValue),
			});
			m_grid.Columns.Add(new DataGridViewCheckMarkColumn
			{
				Name = "CheckMark",
				DataPropertyName = nameof(Record.Checked),
			});
			m_grid.Columns.Add(new DataGridViewTrackBarColumn
			{
				Name = "TrackBar",
				DataPropertyName = nameof(Record.Value),
				MinValue = 0,
				MaxValue = 100,
			});

			// Bind to the data source
			//m_grid.DataSource = new Record[0];
			m_grid.DataSource = m_bs;

			// Automatic column sizing
			m_grid.SizeChanged += DataGridViewEx.FitToDisplayWidth;
			m_grid.ColumnWidthChanged += DataGridViewEx.FitToDisplayWidth;
			m_grid.RowHeadersWidthChanged += DataGridViewEx.FitToDisplayWidth;
			m_grid.SetGridColumnSizes(DataGridViewEx.EColumnSizeOptions.FitToDisplayWidth);

			// Column filtering
			m_grid.KeyDown += DataGridViewEx.ColumnFilters;
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_split = new System.Windows.Forms.SplitContainer();
			this.m_events = new pr.gui.ListBox();
			this.m_grid = new TestCS.DgvUI.DGV();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).BeginInit();
			this.m_split.Panel1.SuspendLayout();
			this.m_split.Panel2.SuspendLayout();
			this.m_split.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			// 
			// m_split
			// 
			this.m_split.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split.Location = new System.Drawing.Point(0, 0);
			this.m_split.Name = "m_split";
			// 
			// m_split.Panel1
			// 
			this.m_split.Panel1.Controls.Add(this.m_events);
			// 
			// m_split.Panel2
			// 
			this.m_split.Panel2.Controls.Add(this.m_grid);
			this.m_split.Size = new System.Drawing.Size(695, 560);
			this.m_split.SplitterDistance = 192;
			this.m_split.TabIndex = 0;
			// 
			// m_events
			// 
			this.m_events.DisplayProperty = null;
			this.m_events.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_events.FormattingEnabled = true;
			this.m_events.Location = new System.Drawing.Point(0, 0);
			this.m_events.Margin = new System.Windows.Forms.Padding(0);
			this.m_events.Name = "m_events";
			this.m_events.Size = new System.Drawing.Size(192, 560);
			this.m_events.TabIndex = 0;
			// 
			// m_grid
			// 
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid.List = null;
			this.m_grid.Location = new System.Drawing.Point(0, 0);
			this.m_grid.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid.Name = "m_grid";
			this.m_grid.Size = new System.Drawing.Size(499, 560);
			this.m_grid.TabIndex = 2;
			// 
			// DgvUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(695, 560);
			this.Controls.Add(this.m_split);
			this.Name = "DgvUI";
			this.Text = "DGV Test";
			this.m_split.Panel1.ResumeLayout(false);
			this.m_split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split)).EndInit();
			this.m_split.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
	}
}
