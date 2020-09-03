using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Rylogic.Gui.WPF.TextEditor
{
	/// <summary>A lookup table from style bits to styles</summary>
	public class StyleMap :Dictionary<ushort, Style> { }

	/// <summary>Base class for a style</summary>
	public abstract class Style :IDisposable
	{
		public Style()
		{

		}
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
		protected virtual void Dispose(bool disposing)
		{
		}
	}
}