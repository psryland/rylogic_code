using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RyLogViewer
{
	/// <summary>A helper class for loading transform substitutions</summary>
	public class TxfmSubLoader
	{
		// ReSharper disable UnassignedField.Global,FieldCanBeMadeReadOnly.Global
		[ImportMany(typeof(ITxfmSub))] public List<ITxfmSub> TxfmSubs; // populated by 'ComposeParts'
		// ReSharper restore UnassignedField.Global,FieldCanBeMadeReadOnly.Global
		
		/// <summary>Find all type derived from ITxfmSub</summary>
		public TxfmSubLoader()
		{
			TxfmSubs = null;
			var cat       = new AssemblyCatalog(typeof(Transform).Assembly);
			var comp_cont = new CompositionContainer(cat);
			comp_cont.ComposeParts(this);
			Debug.Assert(TxfmSubs != null, "TxfmSubs != null");
			TxfmSubs.Sort((lhs,rhs) => string.CompareOrdinal(lhs.Name, rhs.Name));
		}

		/// <summary>Factory method for creating substitution objects by name</summary>
		public ITxfmSub Create(string id, string name)
		{
			foreach (ITxfmSub s in TxfmSubs)
			{
				if (s.Name != name) continue;
				ITxfmSub sub = s.Clone();
				sub.Id = id;
				return sub;
			}
			throw new ArgumentException("Unknown transform substitution type");
		}
	}

	/// <summary>An interface for substitution types used in Transform</summary>
	public interface ITxfmSub
	{
		/// <summary>The tag id for the substitution</summary>
		string Id { get; set; }

		/// <summary>The friendly name for the substitution</summary>
		string Name { get; }

		/// <summary>True if this substitution can be configured</summary>
		bool Configurable { get; }

		/// <summary>A summary of the configuration for this transform substitution</summary>
		string ConfigSummary { get; }

		/// <summary>A method to setup the transform substitution's specific data</summary>
		void Config(IWin32Window owner);

		/// <summary>Returns 'elem' transformed</summary>
		string Result(string elem);

		/// <summary>Serialise data for the substitution to an xml node</summary>
		XElement ToXml(XElement node);

		/// <summary>Deserialise data for the substitution from an xml node</summary>
		void FromXml(XElement node);

		/// <summary>Create a copy of this instance</summary>
		ITxfmSub Clone();
	}

	/// <summary>Common functionality for simple transform substitutions</summary>
	public class TxfmSubBase :ITxfmSub
	{
		protected TxfmSubBase(string type) { Id = ""; Name = type; }

		/// <summary>The tag id if this substitution. Should be something wrapped in '{','}'. E.g {boobs}</summary>
		public string Id { get; set; }

		/// <summary>A human readable name for the substitution</summary>
		public string Name { get; private set; }

		/// <summary>True if this substitution can be configured</summary>
		public virtual bool Configurable { get { return false; } }

		/// <summary>A summary of the configuration for this transform substitution</summary>
		public virtual string ConfigSummary { get { return ""; } }

		/// <summary>A method to setup the transform substitution's specific data</summary>
		public virtual void Config(IWin32Window owner) { } // Nothing to configure

		/// <summary>Returns 'elem' transformed</summary>
		public virtual string Result(string elem) { return elem; }

		/// <summary>Serialise data for the substitution to an xml node</summary>
		public virtual XElement ToXml(XElement node) { return node; }

		/// <summary>Deserialise data for the substitution from an xml node</summary>
		public virtual void FromXml(XElement node) { }

		/// <summary>Create a copy of this instance</summary>
		public ITxfmSub Clone() { return (ITxfmSub)MemberwiseClone(); }

		public override string ToString() { return string.Format("{0} ({1})" ,Id ,Name); }
	}

	/// <summary>A default substitution object which does not transform the input element</summary>
	[Export(typeof(ITxfmSub))]
	public class SubNoChange :TxfmSubBase
	{
		public SubNoChange() :base("No Change") {}
	}

	/// <summary>A substitution type that converts elements to lower case</summary>
	[Export(typeof(ITxfmSub))]
	public class SubToLower :TxfmSubBase
	{
		public SubToLower() :base("Lower Case") {}
		public override string Result(string elem)
		{
			return elem.ToLower();
		}
	}

	/// <summary>A substitution type that converts elements to lower case</summary>
	[Export(typeof(ITxfmSub))]
	public class SubToUpper :TxfmSubBase
	{
		public SubToUpper() :base("Upper Case") {}
		public override string Result(string elem)
		{
			return elem.ToUpper();
		}
	}
}
