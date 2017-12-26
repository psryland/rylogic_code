using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Windows.Threading;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;

namespace Rylogic.Common
{
	/// <summary>Scans a directory for assemblies that contain public types with the 'PluginAttribute' attribute that implement 'TInterface'.</summary>
	public class Plugins<TInterface> where TInterface:class
	{
		// Notes:
		//  Be careful with instances here, this class will contain a collection of instances,
		//  if you dispose any, remember to remove them from the Plugins list.
		//  You can always clone the instances using something like:
		//   (TInterface)Activator.CreateInstance(Plugins[0].GetType())
		public const string DefaultRegexPattern = @"(?<name>.*)\.dll"; 

		/// <summary></summary>
		public Plugins()
		{
			Instances = new List<TInterface>();
			Failures = new List<Tuple<string, Exception>>();
		}

		/// <summary>The loaded plugin instances</summary>
		public List<TInterface> Instances { get; private set; }

		/// <summary>A list of files that failed to load and their reasons why</summary>
		public List<Tuple<string,Exception>> Failures { get; private set; }

		/// <summary>Enumerate the types that implement the interface 'TInterface'</summary>
		public static IEnumerable<PluginFile> Enumerate(string directory, SearchOption search, string regex_filter = DefaultRegexPattern)
		{
			// Build a list of assemblies to check
			var filedata = Shell_.EnumFileSystem(directory, search, regex_filter)
				.Where(x => !x.IsDirectory)
				.ToList();

			// Load each assembly and look for types that are flagged with the Plugin attribute
			foreach (var fd in filedata)
			{
				var ass = Assembly.LoadFile(fd.FullPath);
				foreach (var type in ass.GetExportedTypes())
				{
					var attr = type.FindAttribute<PluginAttribute>(false);
					if (attr == null || attr.Interface != typeof(TInterface))
						continue;

					// Return the type that implements the interface
					yield return new PluginFile { Type = type, Ass = ass, FileData = fd, Unique = attr.Unique };
				}
			}
		}

		/// <summary>Loads an instance of each type found in 'directory' that is flagged with the plugin attribute and implements 'TInterface'</summary>
		public static Plugins<TInterface> Load(string directory, object[] args, SearchOption search, string regex_filter = DefaultRegexPattern, Dispatcher dispatcher = null, Action<string, float> progress_cb = null)
		{
			var plugins = new Plugins<TInterface>();
			dispatcher = dispatcher ?? Dispatcher.CurrentDispatcher;
			Log.Debug(null, $"Loading plugins for interface: {typeof(TInterface).Name}");

			// Find all the types that implement 'TInterface'
			var types = Enumerate(directory, search, regex_filter).ToArray();
			int i = 0, imax = types.Length;

			// Create an instance of each type
			foreach (var ty in types)
			{
				if (progress_cb != null)
					progress_cb(ty.FileData.FileName, (i + 1f) / imax);

				// Create all plugins on the dispatcher thread
				dispatcher.Invoke(() =>
				{
					try
					{
						// An exception here means the constructor for the type being created has thrown.
						var cmp = (TInterface)Activator.CreateInstance(ty.Type, args);
						Log.Debug(null, $"   Found implementation: {ty.Type.Name} from {ty.FileData.FileName}");
						plugins.Instances.Add(cmp);
					}
					catch (Exception ex)
					{
						plugins.Failures.Add(new Tuple<string, Exception>(ty.FileData.FileName, ex));
						Log.Debug(null, $"   Error: {ex.Message}");
					}
				});
			}

			return plugins;
		}

		/// <summary>Load plugins showing a UI if it takes too long</summary>
		public static Plugins<TInterface> LoadWithUI(Form parent, string directory, object[] args, SearchOption search, string regex_filter = DefaultRegexPattern, int delay = 500, string title = null, string desc = null, Icon icon = null)
		{
			if (title == null) title = "Loading Plugins";
			if (desc == null) desc = "Scanning for implementations of {0}".Fmt(typeof(TInterface).Name);

			var dis = Dispatcher.CurrentDispatcher;

			var plugins = (Plugins<TInterface>)null;
			var progress = new ProgressForm(title, desc, icon, ProgressBarStyle.Continuous, (s,a,cb) =>
			{
				plugins = Load(directory, args, search, regex_filter, dis, (file,frac) =>
				{
					cb(new ProgressForm.UserState{Description = $"{desc}\r\n{file}", FractionComplete = frac});
					if (s.CancelPending) throw new OperationCanceledException();
				});
			});
			using (progress)
				progress.ShowDialog(parent, delay);

			// Report any plugins that failed to load
			if (plugins.Failures.Count != 0)
			{
				var msg = new StringBuilder("The following plugins failed to load:\r\n");
				foreach (var x in plugins.Failures)
				{
					msg.AppendLine("{0}".Fmt(x.Item1));
					msg.AppendLine("Reason:");
					msg.AppendLine("   {0}".Fmt(x.Item2.Message));
					msg.AppendLine();
				}

				MsgBox.Show(parent, msg.ToString(), "Plugin Load Failures", MessageBoxButtons.OK, MessageBoxIcon.Information);
			}

			return plugins;
		}
	}

	/// <summary>An attribute for marking classes intended as plugins</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class PluginAttribute :Attribute
	{
		public PluginAttribute(Type interface_)
		{
			Interface = interface_;
		}

		/// <summary>The interface that this plugin implements</summary>
		public Type Interface { get; private set; }

		/// <summary>Set to true if multiple instances of this plugin can be loaded at once</summary>
		public bool Unique { get; set; }
	}

	/// <summary></summary>
	public sealed class PluginFile
	{
		/// <summary>The type that is flagged with the plugin attribute and implements 'TInterface'</summary>
		public Type Type;

		/// <summary>The assembly that contains 'Type'</summary>
		public Assembly Ass;

		/// <summary>The filepath of the assembly containing 'Type'</summary>
		public Shell_.FileData FileData;

		/// <summary>True if only one instance of the plugin at a time is allowed</summary>
		public bool Unique;

		/// <summary></summary>
		public override string ToString()
		{
			return
				FileData.RegexMatch?.Groups["name"]?.Value.ToString(word_sep:Str.ESeparate.Add) ??
				FileData?.FileName ??
				Type.Name;
		}
	}
}
