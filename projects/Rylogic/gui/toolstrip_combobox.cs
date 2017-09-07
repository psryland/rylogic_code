using System;
using System.Windows.Forms;

namespace pr.gui
{
	/// <summary>
	/// A replacement for ToolStripComboBox that preserves the text selection across focus lost/gained
	/// and also doesn't throw a first chance exception when then the combo box data source is set to null</summary>
	public class ToolStripComboBox :ToolStripControlHost
	{
		public ToolStripComboBox()
			:this(string.Empty)
		{ }
		public ToolStripComboBox(string name)
			:base(new ComboBox { Name = name }, name)
		{}

		/// <summary>Apply the control name to the combo box as well</summary>
		public new string Name
		{
			get { return base.Name; }
			set { base.Name = ComboBox.Name = value; }
		}

		/// <summary>The hosted combo box</summary>
		public ComboBox ComboBox
		{
			get { return (ComboBox)Control; }
		}

		/// <summary>The items displayed in the combo box</summary>
		public ComboBox.ObjectCollection Items
		{
			get { return ComboBox.Items; }
		}

		/// <summary>Get/Set the selected item</summary>
		public int SelectedIndex
		{
			get { return ComboBox.SelectedIndex; }
			set { ComboBox.SelectedIndex = value; }
		}

		/// <summary>Raised when the selected index changes</summary>
		public event EventHandler SelectedIndexChanged
		{
			add { ComboBox.SelectedIndexChanged += value; }
			remove { ComboBox.SelectedIndexChanged -= value; }
		}

		/// <summary>The selected item</summary>
		public object SelectedItem
		{
			get { return ComboBox.SelectedItem; }
			set { ComboBox.SelectedItem = value; }
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public string SelectedText
		{
			get { return ComboBox.SelectedText; }
			set { ComboBox.SelectedText = value; }
		}

		/// <summary>Gets/Sets the style of the combo box</summary>
		public ComboBoxStyle DropDownStyle
		{
			get { return ComboBox.DropDownStyle; }
			set { ComboBox.DropDownStyle = value; }
		}

		/// <summary>Get/Set the appearance of the combo box</summary>
		public FlatStyle FlatStyle
		{
			get { return ComboBox.FlatStyle; }
			set { ComboBox.FlatStyle = value; }
		}
	}
}
