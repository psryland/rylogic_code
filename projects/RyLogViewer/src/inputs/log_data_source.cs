﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	/// <summary>Parts of the Main form related to buffering non-file streams into an output file</summary>
	public partial class Main :Form
	{
		private BufferedCustomDataSource m_buffered_custom_source;

		/// <summary>Custom data sources loaded from plugins</summary>
		internal List<ICustomLogDataSource> CustomDataSources
		{
			get
			{
				if (m_custom_data_sources == null)
				{
					// Add built in sources (before the plugins so that built in sources can be replaced)
					m_custom_data_sources = new List<ICustomLogDataSource>();

					// Loads dlls from the plugins directory looking for transform substitutions
					var loader = PluginLoader<CustomDataSourceAttribute, ICustomLogDataSource>.LoadWithUI(this, Misc.ResolveAppPath("plugins"), true);
					foreach (var sub in loader.Plugins)
						m_custom_data_sources.Add(sub);
				}
				return m_custom_data_sources;
			}
		}
		private List<ICustomLogDataSource> m_custom_data_sources;

		/// <summary>Add menu options for the custom data sources</summary>
		private void InitCustomDataSources()
		{
			if (CustomDataSources.Count != 0)
				m_menu_file_data_sources.DropDownItems.Add(new ToolStripSeparator());

			foreach (var src in CustomDataSources)
			{
				var src_type = src.GetType();
				m_menu_file_data_sources.DropDownItems.Add(src.MenuText, null, (s,a) =>
					{
						// Create a new instance of the plugin, since it will be disposed once complete
						var inst = (ICustomLogDataSource)Activator.CreateInstance(src_type);
						var config = new LogDataSourceConfig(this, m_settings.OutputFilepathHistory);
						var launch = inst.ShowConfigUI(config);
						if (!launch.DoLaunch) return;
						LogCustomDataSource(inst, launch);
					});
			}
		}

		/// <summary>Setup the UI to receive streamed log data</summary>
		private void PrepareForStreamedData()
		{
			EnableTail(true);
			EnableWatch(true);
			EnableAdditive(true);
		}

		/// <summary>Launch the custom data source</summary>
		private void LogCustomDataSource(ICustomLogDataSource src, LogDataSourceRunData launch)
		{
			BufferedCustomDataSource buffered_src = null;
			try
			{
				// Close any currently open file
				// Strictly, we don't have to close because OpenLogFile closes before opening
				// however if the user reopens the same process the existing process will hold
				// a lock to the capture file preventing the new process being created.
				CloseLogFile();

				// Set options so that data always shows
				PrepareForStreamedData();

				// Launch the process with standard output/error redirected to the temporary file
				buffered_src = new BufferedCustomDataSource(src, launch);

				// Give some UI feedback when the data source ends
				buffered_src.ConnectionDropped += (s,a)=>
					{
						this.BeginInvoke(() => SetStaticStatusMessage(string.Format("{0} stopped", src.ShortName), Color.Black, Color.LightSalmon));
					};

				// Open the capture file created by buffered_src
				OpenSingleLogFile(buffered_src.Filepath, !buffered_src.TmpFile);
				buffered_src.Start();
				SetStaticStatusMessage("Connected", Color.Black, Color.LightGreen);

				// Pass over the ref
				if (m_buffered_custom_source != null) m_buffered_custom_source.Dispose();
				m_buffered_custom_source = buffered_src;
				buffered_src = null;
			}
			catch (Exception ex)
			{
				Log.Exception(this, ex, "Custom data source failed: {0} -> {1}".Fmt(src.ShortName, launch.OutputFilepath));
				Misc.ShowMessage(this, "Failed to launch {0}.".Fmt(src.ShortName), "Data Source Failed", MessageBoxIcon.Error, ex);
			}
			finally
			{
				if (buffered_src != null)
					buffered_src.Dispose();
			}
		}
	}

	/// <summary>Manages collected data from a custom data source</summary>
	public class BufferedCustomDataSource :BufferedStream
	{
		/// <summary>The source providing the log data</summary>
		private readonly ICustomLogDataSource m_src;
		private readonly byte[] m_buf;

		public BufferedCustomDataSource(ICustomLogDataSource src, LogDataSourceRunData launch)
			:base(launch.OutputFilepath, launch.AppendOutputFile)
		{
			m_src = src;
			m_buf = new byte[BufBlockSize];
		}
		public override void Dispose()
		{
			base.Dispose();
			m_src.Dispose();
		}

		/// <summary>Should return true to continue reading data</summary>
		protected override bool IsConnected
		{
			get { return m_src.IsConnected; }
		}

		/// <summary>Start collecting log data asynchronously</summary>
		public void Start()
		{
			Log.Info(this, "Data Source {0} started".Fmt(m_src.ShortName));

			m_src.Start();
			m_src.BeginRead(m_buf, 0, m_buf.Length, DataRecv, new AsyncData(m_src, m_buf));
		}
	}
}