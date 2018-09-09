using System;
using System.Drawing;
using System.Windows.Forms;

namespace RyLogViewer
{
	/// <summary>Interface to the main UI</summary>
	public interface IMainUI
	{
		/// <summary>The main UI as a form for use as the parent of child dialogs</summary>
		Form MainWindow { get; }


		/// <summary>Turn on/off quick filter mode</summary>
		void EnableQuickFilter(bool enable);

		/// <summary>Turn on/off highlights</summary>
		void EnableHighlights(bool enable);

		/// <summary>Turn on/off filters</summary>
		void EnableFilters(bool enable);

		/// <summary>Turn on/off transforms</summary>
		void EnableTransforms(bool enable);

		/// <summary>Turn on/off actions</summary>
		void EnableActions(bool enabled);

		/// <summary>Turn on/off tail mode</summary>
		void EnableTail(bool enabled);

		/// <summary>Turn on/off tail mode</summary>
		void EnableWatch(bool enable);

		/// <summary>Turn on/off additive only mode</summary>
		void EnableAdditive(bool enable);

		/// <summary>Enable/Disable monitor mode</summary>
		void EnableMonitorMode(bool enable);

		/// <summary>Create a message that displays for a period then disappears. Use null or "" to hide the status</summary>
		void SetTransientStatusMessage(string text, Color frcol, Color bkcol, TimeSpan display_time_ms);
		void SetTransientStatusMessage(string text, Color frcol, Color bkcol);
		void SetTransientStatusMessage(string text);

		/// <summary>Create a status message that displays until cleared. Use null or "" to hide the status</summary>
		void SetStaticStatusMessage(string text, Color frcol, Color bkcol);
		void SetStaticStatusMessage(string text);
	}
}