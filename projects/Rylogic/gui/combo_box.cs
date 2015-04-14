using System;

namespace pr.gui
{
	/// <summary>Replacement for the forms combo box that doesn't throw a first chance exception when the data source is empty</summary>
	public class ComboBox :System.Windows.Forms.ComboBox
	{
		public override int SelectedIndex
		{
			get { return base.SelectedIndex; }
			set
			{
				if (value < 0 || value >= Items.Count) return;
				base.SelectedIndex = value;
			}
		}
	}
}
