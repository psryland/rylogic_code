using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using pr.extn;
using pr.util;

namespace pr.common
{
	/// <summary>
	/// Scans a directory for assemblies that contain public types when the attribute 'TAttr'.
	/// Creates an instance of each type and adds it to 'Plugins'
	/// </summary>
	public class PluginLoader<TAttr, TInterface> where TAttr:Attribute where TInterface:class
	{
		public PluginLoader(string directory, bool recursive, string regex_pattern = @".*\.dll")
		{
			Plugins = new List<TInterface>();
			Failures = new List<Tuple<string, Exception>>();

			foreach (var dll in PathEx.EnumerateFiles(directory, regex_pattern, recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
			{
				try
				{
					var ass = Assembly.LoadFile(dll.FullPath);
					foreach (var type in ass.GetExportedTypes())
					{
						if (!type.GetCustomAttributes(typeof(TAttr), false).Any())
							continue;

						Plugins.Add(Activator.CreateInstance(type) as TInterface);
						Log.Debug(this, "Loaded {0} implementation: {1} from {2}".Fmt(typeof(TInterface).Name, type.Name, dll.FullPath));
					}
				}
				catch (Exception ex)
				{
					Failures.Add(new Tuple<string, Exception>(dll.FullPath, ex));
				}
			}
		}

		/// <summary>The loaded plugin instances</summary>
		public List<TInterface> Plugins { get; private set; }

		/// <summary>A list of files that failed to load and their reasons why</summary>
		public List<Tuple<string,Exception>> Failures { get; private set; }
	}
}
