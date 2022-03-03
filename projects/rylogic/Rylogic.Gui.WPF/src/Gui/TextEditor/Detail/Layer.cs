using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Rylogic.Gui.WPF.TextEditor;

namespace Rylogic.Gui.WPF
{
	public class Layer :UIElement
	{
		// Notes:
		//  - Base class for layers in the TextView control
		//  - Visuals are a lightweight form of UI elements

		/// <summary>Z-Ordered layer types</summary>
		public enum EType
		{
			Background,
			Text,
			Caret,
		}

		public Layer(EType type, TextView text_view)
		{
			Type = type;
			TextView = text_view;
			Visuals = new VisualCollection(this);
			Elements = new UIElementCollection(this, text_view);
			Focusable = false;
		}

		/// <summary>The type of layer this is</summary>
		public EType Type { get; }

		/// <summary>The text view this layer belongs to</summary>
		public TextView TextView { get; }

		/// <summary>The child visuals in this layer</summary>
		public VisualCollection Visuals { get; }

		/// <summary>The child visuals in this layer</summary>
		public UIElementCollection Elements { get; }

		/// <inheritdoc/>
		protected override int VisualChildrenCount => Visuals.Count + Elements.Count;

		/// <inheritdoc/>
		protected override Visual GetVisualChild(int index) => index < Visuals.Count ? Visuals[index] : Elements[index - Visuals.Count];

		/// <inheritdoc/>
		protected override GeometryHitTestResult? HitTestCore(GeometryHitTestParameters hitTestParameters) => null;

		/// <inheritdoc/>
		protected override HitTestResult? HitTestCore(PointHitTestParameters hitTestParameters) => null;

		/// <inheritdoc/>
		protected override void OnRender(DrawingContext dc)
		{
			base.OnRender(dc);
			TextView.RenderBackground(dc, Type);
		}
	}
}