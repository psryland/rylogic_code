using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Rylogic.Common;
using Rylogic.Extn;

namespace Rylogic.Plugin
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

		public Plugins()
		{
			Instances = new List<TInterface>();
			Failures = new List<FailedPlugin>();
		}

		/// <summary>The loaded plugin instances</summary>
		public List<TInterface> Instances { get; private set; }

		/// <summary>A list of files that failed to load and their reasons why</summary>
		public List<FailedPlugin> Failures { get; private set; }

		/// <summary>Add an explicit instance</summary>
		public Plugins<TInterface> Add(TInterface inst)
		{
			Instances.Add(inst);
			return this;
		}

		/// <summary>Add explicit instances</summary>
		public Plugins<TInterface> Add(IEnumerable<TInterface> inst)
		{
			Instances.AddRange(inst);
			return this;
		}

		/// <summary>Create an instance of 'ty' named 'name'</summary>
		public Plugins<TInterface> Load(string name, Type ty, object[] args = null, Func<Type, object[], TInterface> factory = null)
		{
			// Create all plugins using the factory callback
			// This allows plugins to be created on a dispatcher thread using Dispatcher.Invoke
			factory = factory ?? ((t, a) => (TInterface)Activator.CreateInstance(t, a));

			try
			{
				// An exception here means the constructor for the type being created has thrown.
				Instances.Add(factory(ty, args));
			}
			catch (Exception ex)
			{
				Failures.Add(new FailedPlugin(name, ex));
			}
			return this;
		}

		/// <summary>Add instances from 'plugin_file'</summary>
		public Plugins<TInterface> Load(PluginFile file, object[] args = null, Func<Type, object[], TInterface> factory = null)
		{
			return Load(file.Name, file.Type, args, factory);
		}

		/// <summary>Add instances from 'plugin_files'</summary>
		public Plugins<TInterface> Load(IEnumerable<PluginFile> plugin_files, object[] args = null, Func<Type, object[], TInterface> factory = null)
		{
			foreach (var file in plugin_files)
				Load(file.Name, file.Type, args, factory);

			return this;
		}

		/// <summary>Load an instance of each type that implements the interface 'TInterface' in 'ass'</summary>
		public Plugins<TInterface> Load(Assembly ass, object[] args = null, Func<Type, object[], TInterface> factory = null)
		{
			foreach (var type in ass.GetExportedTypes())
			{
				var attr = type.FindAttribute<PluginAttribute>(false);
				if (attr == null || attr.Interface != typeof(TInterface))
					continue;

				// Return the type that implements the interface
				Load(Path_.FileTitle(ass.Location), type, args, factory);
			}
			return this;
		}

		/// <summary>Loads an instance of each type found in 'directory' that is flagged with the plugin attribute and implements 'TInterface'</summary>
		public Plugins<TInterface> Load(string directory, object[] args = null, SearchOption search = SearchOption.TopDirectoryOnly, string regex_filter = DefaultRegexPattern, Func<Type, object[], TInterface> factory = null, Func<string, float, bool> progress = null)
		{
			// Default callbacks
			progress = progress ?? ((s, p) => true);

			// Find all the types that implement 'TInterface'
			var types = Enumerate(directory, search, regex_filter).ToList();
			int i = 0, imax = types.Count;

			// Create an instance of each type
			foreach (var file in types)
			{
				// Report progress
				if (!progress(file.FileData.Name, (i + 1f) / imax))
					break;

				Load(file, args, factory);
			}

			return this;
		}

		/// <summary>Enumerate the types that implement the interface 'TInterface'</summary>
		public static IEnumerable<PluginFile> Enumerate(Assembly ass)
		{
			foreach (var type in ass.GetExportedTypes())
			{
				var attr = type.FindAttribute<PluginAttribute>(false);
				if (attr == null || attr.Interface != typeof(TInterface))
					continue;

				// Return the type that implements the interface
				var fd = new FileInfo(ass.Location);
				yield return new PluginFile(fd.Name, type, ass, fd, attr.Unique);
			}
		}

		/// <summary>Enumerate the types that implement the interface 'TInterface'</summary>
		public static IEnumerable<PluginFile> Enumerate(string directory, SearchOption search = SearchOption.TopDirectoryOnly, string regex_filter = DefaultRegexPattern)
		{
			// Build a list of assemblies to check
			var filedata = Path_.EnumFileSystem(directory, search, regex_filter, exclude:FileAttributes.Directory).ToList();

			// Load each assembly and look for types that are flagged with the Plugin attribute
			foreach (var fd in filedata)
			{
				var ass = Assembly.LoadFile(fd.FullName);
				foreach (var plugin in Enumerate(ass))
					yield return plugin;
			}
		}

		/// <summary>Tuple for plugins that fail to load</summary>
		public class FailedPlugin
		{
			public FailedPlugin(string name, Exception error)
			{
				Name = name;
				Error = error;
			}
			public string Name { get; private set; }
			public Exception Error { get; private set; }
		}
	}
}
