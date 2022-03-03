using System;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Xml.Linq;

namespace RyLogViewer
{
	/// <summary>Base class for common functionality for simple transform substitutions</summary>
	public abstract class TransformSubstitutionBase :ITransformSubstitution
	{
		/// <summary>
		/// A unique id for this text transform, used to associate
		/// saved configuration data with this transformation.</summary>
		public abstract Guid Guid { get; }

		/// <summary>
		/// The name that appears in the transform column drop-down
		/// for this text transformation.</summary>
		public abstract string DropDownName { get; }

		/// <summary>
		/// Return true if this text transform requires configuration.
		/// Returning true will cause the pencil icon to be displayed
		/// and allow users to configure the text transform.</summary>
		public virtual bool Configurable { get { return false; } }

		/// <summary>
		/// Called when a user selects to edit the configuration for this transform.
		/// Implementers should display a modal dialog that collects any necessary data
		/// for the text transform.</summary>
		public virtual void ShowConfigUI(Form main_window) {} // Nothing to configure

		/// <summary>
		/// Returns a summary of the configuration for this transform in a form suitable
		/// for the tooltip that is displayed when the user hovers their mouse over the
		/// selected transform. Return null to not display a tooltip</summary>
		public virtual string ConfigSummary { get { return null; } }

		/// <summary>
		/// Returns the result of applying this text transform to 'captured_text'.
		/// This method provides the functionality of the text transform and should
		/// be efficiently implemented.</summary>
		public abstract string Result(string captured_text);

		/// <summary>
		/// Save data for the transform to the provided xml node.
		/// This is used to persist per-instance settings for this text
		/// transform within the main RyLogViewer settings xml file.
		/// Implementers should add xml nodes to 'data_root'</summary>
		public virtual XElement ToXml(XElement data_root) { return data_root; } // No persisted per-instance data

		/// <summary>
		/// Load instance data for this transform from 'data_root'.
		/// This method should be the symmetric opposite of 'ToXml()'</summary>
		public virtual void FromXml(XElement data_root) {} // No persisted per-instance data

		/// <summary></summary>
		public override string ToString() { return DropDownName; }
	}

	// Type converter so that ITransformSubstitution can be displayed in a DGV combo box
	public class TransformSubstitutionTypeConverter :TypeConverter
	{
		public override string ToString() { return typeof(ITransformSubstitution).Name; }
		public override bool GetPropertiesSupported    (ITypeDescriptorContext context) { return true; }
		public override bool GetCreateInstanceSupported(ITypeDescriptorContext context) { return false; }
		public override PropertyDescriptorCollection GetProperties(ITypeDescriptorContext context, object value, Attribute[] attributes)
		{
			return TypeDescriptor.GetProperties(value ,attributes);
		}
		public override object CreateInstance(ITypeDescriptorContext context, IDictionary values)
		{
			throw new NotSupportedException();
		}
	}
}
