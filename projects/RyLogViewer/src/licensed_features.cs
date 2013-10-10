using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.extn;

namespace RyLogViewer
{
	/// <summary>Interface for controlling time limited features</summary>
	public interface ILicensedFeature
	{
		/// <summary>An html description of the licensed feature</summary>
		string FeatureDescription { get; }

		/// <summary>True if the licensed feature is still currently in use</summary>
		bool FeatureInUse { get; }

		/// <summary>Called to stop the use of the feature</summary>
		void CloseFeature();
	}

	public sealed partial class Main
	{
		/// <summary>The collection of licensed features in use</summary>
		private readonly Dictionary<string, ILicensedFeature> m_licensed_features = new Dictionary<string, ILicensedFeature>();

		/// <summary>Call to begin or update a time limited feature</summary>
		public void UseLicensedFeature(string key, ILicensedFeature use)
		{
			if (m_license.Valid)
				return;

			// See if the feature is already monitored, if so, just update the interface
			if (m_licensed_features.ContainsKey(key))
			{
				m_licensed_features[key] = use;
				return;
			}

			// If the feature isn't in use, ignore the request (this method is called recursively)
			if (!use.FeatureInUse)
				return;

			// Otherwise, prompt about the feature
			var dlg = new CrippleUI(use.FeatureDescription);
			if (dlg.ShowDialog(this) != DialogResult.OK)
			{
				// Not interested, close and go home
				use.CloseFeature();
				return;
			}

			// Otherwise, the user wants to use the feature
			m_licensed_features[key] = use;
			this.BeginInvokeDelayed(
				(int)TimeSpan.FromMinutes(FreeEditionLimits.FeatureEnableTimeInMinutes).TotalMilliseconds,
				() =>
				{
					ILicensedFeature feat;
					if (!m_licensed_features.TryGetValue(key, out feat)) return;
					m_licensed_features.Remove(key);
					UseLicensedFeature(key, feat);
				});
		}
	}

	/// <summary>Limits the number of highlighting patterns in use</summary>
	public class HighlightingCountLimiter :ILicensedFeature
	{
		private readonly Main m_main;
		private readonly Settings m_settings;
		public HighlightingCountLimiter(Main main, Settings settings)
		{
			m_main = main;
			m_settings = settings;
		}

		/// <summary>An html description of the licensed feature</summary>
		public string FeatureDescription
		{
			get { return Resources.cripple_highlighting; }
		}

		/// <summary>True if the licensed feature is still currently in use</summary>
		public virtual bool FeatureInUse
		{
			get { return m_settings.HighlightPatterns.Length > FreeEditionLimits.MaxHighlights; }
		}

		/// <summary>Called to stop the use of the feature</summary>
		public virtual void CloseFeature()
		{
			// Read the patterns from the settings to see if more than the max allowed are in use
			var pats = m_settings.HighlightPatterns.ToList();
			pats.RemoveToEnd(FreeEditionLimits.MaxHighlights);
			m_settings.HighlightPatterns = pats.ToArray();
			m_main.ApplySettings();
		}
	}

	/// <summary>Limits the number of highlighting patterns in use</summary>
	public class FilteringCountLimiter :ILicensedFeature
	{
		private readonly Main m_main;
		private readonly Settings m_settings;
		public FilteringCountLimiter(Main main, Settings settings)
		{
			m_main = main;
			m_settings = settings;
		}

		/// <summary>An html description of the licensed feature</summary>
		public string FeatureDescription
		{
			get { return Resources.cripple_filtering; }
		}

		/// <summary>True if the licensed feature is still currently in use</summary>
		public virtual bool FeatureInUse
		{
			get { return m_settings.FilterPatterns.Length> FreeEditionLimits.MaxFilters; }
		}

		/// <summary>Called to stop the use of the feature</summary>
		public virtual void CloseFeature()
		{
			// Read the patterns from the settings to see if more than the max allowed are in use
			var pats = m_settings.FilterPatterns.ToList();
			pats.RemoveToEnd(FreeEditionLimits.MaxFilters);
			m_settings.FilterPatterns = pats.ToArray();
			m_main.ApplySettings();
		}
	}

	/// <summary>Limits the use of aggregate files</summary>
	public class AggregateFileLimiter :ILicensedFeature
	{
		private readonly Main m_main;
		private readonly AggregateFilesUI m_ui;
		public AggregateFileLimiter(Main main, AggregateFilesUI ui)
		{
			m_main = main;
			m_ui = ui;
		}

		/// <summary>An html description of the licensed feature</summary>
		public string FeatureDescription
		{
			get { return Resources.cripple_aggregate_files; }
		}

		/// <summary>True if the licensed feature is still currently in use</summary>
		public bool FeatureInUse
		{
			get
			{
				return
					m_main != null && m_main.FileSource is AggregateFile ||
					m_ui != null && m_ui.Visible;
			}
		}

		/// <summary>Called to stop the use of the feature</summary>
		public void CloseFeature()
		{
			if (m_main != null && m_main.FileSource is AggregateFile)
				m_main.CloseLogFile();
			if (m_ui != null && m_ui.Visible)
				m_ui.Close();
		}
	}
}
