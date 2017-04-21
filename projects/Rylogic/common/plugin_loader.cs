using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;
using pr.extn;
using pr.gui;
using pr.util;

namespace pr.common
{
	/// <summary>Scans a directory for assemblies that contain public types with the 'PluginAttribute' attribute that implement 'TInterface'.</summary>
	public class PluginLoader<TInterface> where TInterface:class
	{
		/// <summary>The loaded plugin instances</summary>
		public List<TInterface> Plugins { get; private set; }

		/// <summary>A list of files that failed to load and their reasons why</summary>
		public List<Tuple<string,Exception>> Failures { get; private set; }

		// Notes:
		//  Be careful with instances here, this class will contain a collection of instances,
		//  if you dispose any, remember to remove them from the Plugins list.
		//  You can always clone the instances using something like:
		//   (TInterface)Activator.CreateInstance(Plugins[0].GetType())

		/// <summary>Initialises the plugin loader with instances of the found plugins</summary>
		public PluginLoader(string directory, object[] args, bool recursive, string regex_pattern = @".*\.dll", Action<string, float> progress_cb = null, Dispatcher dispatcher = null)
		{
			Plugins  = new List<TInterface>();
			Failures = new List<Tuple<string, Exception>>();
			dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;

			Log.Debug(this, "Loading plugins for interface: {0}".Fmt(typeof(TInterface).Name));

			// Build a list of assemblies to check
			var filepaths = Path_.EnumFileSystem(directory, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly, regex_pattern)
				.Where(x => !x.IsDirectory)
				.Select(x => x.FullPath)
				.ToList();

			// Load each assembly
			int i = 0, imax = filepaths.Count;
			foreach (var dll in filepaths)
			{
				if (progress_cb != null)
					progress_cb(dll, (i + 1f) / imax);

				var ass = Assembly.LoadFile(dll);
				foreach (var type in ass.GetExportedTypes())
				{
					var attr = type.FindAttribute<PluginAttribute>(false);
					if (attr == null || attr.Interface != typeof(TInterface))
						continue;

					// Create all plugins on the dispatcher thread
					var lib = dll;
					var ty = type;
					dispatcher.Invoke(() =>
					{
						try
						{
							// An exception here means the constructor for the type being created has thrown.
							var cmp = (TInterface)Activator.CreateInstance(ty, args);
							Log.Debug(this, "   Found implementation: {0} from {1}".Fmt(ty.Name, lib));

							Plugins.Add(cmp);
						}
						catch (Exception ex)
						{
							Failures.Add(new Tuple<string, Exception>(dll, ex));
							Log.Debug(this, "   Error: {0}".Fmt(ex.Message));
						}
					});
				}
			}
		}

		/// <summary>Load plugins showing a UI if it takes too long</summary>
		public static PluginLoader<TInterface> LoadWithUI(Form parent, string directory, object[] args, bool recursive, string regex_pattern = @".*\.dll", int delay = 500, string title = null, string desc = null, Icon icon = null)
		{
			if (title == null) title = "Loading Plugins";
			if (desc == null) desc = "Scanning for implementations of {0}".Fmt(typeof(TInterface).Name);

			var dis = Dispatcher.CurrentDispatcher;

			PluginLoader<TInterface> loader = null;
			var progress = new ProgressForm(title, desc, icon, ProgressBarStyle.Continuous, (s,a,cb) =>
			{
				loader = new PluginLoader<TInterface>(directory, args, recursive, regex_pattern, (file,frac) =>
				{
					cb(new ProgressForm.UserState{Description = "{0}\r\n{1}".Fmt(desc, file), FractionComplete = frac});
					if (s.CancelPending) throw new OperationCanceledException();
				}, dis);
			});
			using (progress)
				progress.ShowDialog(parent, delay);

			// Report any plugins that failed to load
			if (loader.Failures.Count != 0)
			{
				var msg = new StringBuilder("The following plugins failed to load:\r\n");
				foreach (var x in loader.Failures)
				{
					msg.AppendLine("{0}".Fmt(x.Item1));
					msg.AppendLine("Reason:");
					msg.AppendLine("   {0}".Fmt(x.Item2.Message));
					msg.AppendLine();
				}

				MsgBox.Show(parent, msg.ToString(), "Plugin Load Failures", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

			return loader;
		}
	}

	/// <summary>An attribute for marking classes intended as plugins</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class PluginAttribute :Attribute
	{
		/// <summary>The interface that this plugin implements</summary>
		public Type Interface { get; private set; }

		public PluginAttribute(Type interface_)
		{
			Interface = interface_;
		}
	}
}
