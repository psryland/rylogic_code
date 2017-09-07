using System;
using System.Windows.Forms;
using pr.common;

namespace pr.gui
{
	public class ToolStripPatternFilter :ToolStripControlHost
	{
		public ToolStripPatternFilter()
			:this(string.Empty)
		{ }
		public ToolStripPatternFilter(string name)
			:base(new PatternFilter { Name = name }, name)
		{}

		/// <summary>Apply the control name to the combo box as well</summary>
		public new string Name
		{
			get { return base.Name; }
			set { base.Name = PatternFilter.Name = value; }
		}

		/// <summary>The hosted control</summary>
		public PatternFilter PatternFilter
		{
			get { return (PatternFilter)Control; }
		}

		/// <summary>The current pattern</summary>
		public Pattern Pattern
		{
			get { return PatternFilter.Pattern; }
			set { PatternFilter.Pattern = value; }
		}

		/// <summary>The current pattern</summary>
		public Pattern[] History
		{
			get { return PatternFilter.History; }
			set { PatternFilter.History = value; }
		}

		/// <summary>Raised when the pattern is changed</summary>
		public event EventHandler PatternChanged
		{
			add { PatternFilter.PatternChanged += value; }
			remove { PatternFilter.PatternChanged -= value; }
		}

	}
}
