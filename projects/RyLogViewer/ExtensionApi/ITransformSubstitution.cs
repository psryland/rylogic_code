using System;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RyLogViewer
{
	/// <summary>Interface to a transform substitution plugin</summary>
	public interface ITransformSubstitution
	{
		// The name of the substitutor is used as the unique identifier
		// since the user will not be able to distinguish between them
		// if two had the same name. Also, there's a bug in DGVComboBoxColumn
		// that means the combo can't be bound to have 'Value' as a complex object.
		// This means the string in the combo box needs to uniquely identify the substitutor

		/// <summary>The name of the substitution (must be unique)</summary>
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
		ITransformSubstitution Clone();
	}

	/// <summary>Base class for common functionality for simple transform substitutions</summary>
	public abstract class TransformSubstitutionBase :ITransformSubstitution
	{
		/// <summary>The name of the substitution (must be unique)</summary>
		public abstract string Name { get; }

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
		public ITransformSubstitution Clone() { return (ITransformSubstitution)MemberwiseClone(); }

		public override string ToString() { return string.Format("{0}",Name); }
	}

	/// <summary>An attribute for marking classes intended as ITransformSubstitution implementations</summary>
	[AttributeUsage(AttributeTargets.Class)]
	public sealed class TransformSubstitutionAttribute :Attribute
	{}
}
