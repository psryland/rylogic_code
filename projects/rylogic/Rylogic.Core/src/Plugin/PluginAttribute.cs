using System;

namespace Rylogic.Plugin
{
	/// <summary>An attribute for marking classes intended as plugins</summary>
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public sealed class PluginAttribute :Attribute
	{
		public PluginAttribute(Type interface_, bool unique = false)
		{
			Interface = interface_;
			Unique = unique;
		}

		/// <summary>The interface that this plugin implements</summary>
		public Type Interface { get; private set; }

		/// <summary>Set to true if multiple instances of this plugin can be loaded at once</summary>
		public bool Unique { get; set; }
	}
}
