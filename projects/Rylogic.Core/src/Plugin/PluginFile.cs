﻿using System;
using System.IO;
using System.Reflection;
using Rylogic.Extn;

namespace Rylogic.Plugin
{
	/// <summary></summary>
	public sealed class PluginFile
	{
		public PluginFile(string name, Type type, Assembly ass, FileSystemInfo filedata, bool unique)
		{
			Name = name;
			Type = type;
			Ass = ass;
			FileData = filedata;
			Unique = unique;
		}

		/// <summary>A name for the plugin</summary>
		public string Name { get; private set; }

		/// <summary>The type that is flagged with the plugin attribute and implements 'TInterface'</summary>
		public Type Type { get; private set; }

		/// <summary>The assembly that contains 'Type'</summary>
		public Assembly Ass { get; private set; }

		/// <summary>The filepath of the assembly containing 'Type'</summary>
		public FileSystemInfo FileData { get; private set; }

		/// <summary>True if only one instance of the plugin at a time is allowed</summary>
		public bool Unique { get; private set; }

		/// <summary>Create an instance of this plugin</summary>
		public object Create()
		{
			return Type.New();
		}
		public object Create<A1>(A1 a1)
		{
			return Type.New(a1);
		}
		public object Create<A1, A2>(A1 a1, A2 a2)
		{
			return Type.New(a1, a2);
		}
		public object Create<A1, A2, A3>(A1 a1, A2 a2, A3 a3)
		{
			return Type.New(a1, a2, a3);
		}
		public object Create<A1, A2, A3, A4>(A1 a1, A2 a2, A3 a3, A4 a4)
		{
			return Type.New(a1, a2, a3, a4);
		}

		/// <summary></summary>
		public override string ToString()
		{
			return Name ?? Type.Name;
		}
	}
}
