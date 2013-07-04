namespace RyLogViewer
{
	/// <summary>Subclass the DataGridView to add missing features</summary>
	public sealed class DataGridView :System.Windows.Forms.DataGridView
	{
		public DataGridView()
		{
			DoubleBuffered = true;
		}
	}

	/// <summary>Workaround for first chance exception in stand combo box</summary>
	public sealed class ComboBox :System.Windows.Forms.ComboBox
	{
		public override int SelectedIndex
		{
			get { return Items.Count != 0 ? base.SelectedIndex : -1; }
			set { if (Items.Count != 0) base.SelectedIndex = value; }
		}
	}
}
