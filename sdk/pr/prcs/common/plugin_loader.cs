﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace pr.common
{
	/// <summary>Scans a directory for assemblies that contain public types with the 'TAttr' attribute.</summary>
	public class PluginLoader<TAttr, TInterface> where TAttr:Attribute where TInterface:class
	{
		// Notes:
		//  Be careful with instances here, this class will contain a collection of instances,
		//  if you dispose any, remember to remove them from the Plugins list.
		//  You can always clone the instances using something like:
		//   (TInterface)Activator.CreateInstance(Plugins[0].GetType())

		/// <summary>Initialises the plugin loader with instances of the found plugins</summary>
		public PluginLoader(string directory, bool recursive, string regex_pattern = @".*\.dll", Action<string> progress_cb = null)
		{
			Plugins  = new List<TInterface>();
			Failures = new List<Tuple<string, Exception>>();

			Log.Debug(this, "Loading plugins for interface: {0}".Fmt(typeof(TInterface).Name));
			foreach (var dll in PathEx.EnumerateFiles(directory, regex_pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
			{
				try
				{
					if (progress_cb != null) progress_cb(dll.FullPath);

					var ass = Assembly.LoadFile(dll.FullPath);
					foreach (var type in ass.GetExportedTypes())
					{
						if (!type.GetCustomAttributes(typeof(TAttr), false).Any())
							continue;

						Plugins.Add((TInterface)Activator.CreateInstance(type));
						Log.Debug(this, "   Found implementation: {0} from {1}".Fmt(type.Name, dll.FullPath));
					}
				}
				catch (Exception ex)
				{
					Failures.Add(new Tuple<string, Exception>(dll.FullPath, ex));
					Log.Debug(this, "   Error: {0}".Fmt(ex.Message));
				}
			}
		}

		/// <summary>The loaded plugin instances</summary>
		public List<TInterface> Plugins { get; private set; }

		/// <summary>A list of files that failed to load and their reasons why</summary>
		public List<Tuple<string,Exception>> Failures { get; private set; }

		/// <summary>Load plugins showing a UI if it takes too long</summary>
		public static PluginLoader<TAttr, TInterface> LoadWithUI(Form parent, string directory, bool recursive, string regex_pattern = @".*\.dll", int delay = 500, string title = null, string desc = null, Icon icon = null)
		{
			if (title == null) title = "Loading Plugins";
			if (desc == null) desc = "Scanning for implementations of {0}".Fmt(typeof(TInterface).Name);

			PluginLoader<TAttr, TInterface> loader = null;
			var progress = new ProgressForm(title, desc, icon, ProgressBarStyle.Marquee, (s,a,cb) =>
				{
					loader = new PluginLoader<TAttr, TInterface>(directory, recursive, regex_pattern, file =>
						{
							cb(new ProgressForm.UserState{Description = "{0}\r\n{1}".Fmt(desc, file)});
							if (s.CancelPending) throw new OperationCanceledException();
						});
				});
			progress.ShowDialog(parent, delay);

			// Report any plugins that failed to load
			if (loader.Failures.Count != 0)
			{
				var msg = "The following plugins failed to load:\r\n{0}".Fmt(string.Join("\r\n", loader.Failures));
				MsgBox.Show(parent, msg, "Plugin Load Failures", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

			return loader;
		}
	}
}