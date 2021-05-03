using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Maths;

namespace Rylogic.Gui.WPF.ChartDiagram
{
	/// <summary>An invisible 'node-like' object used for detaching/attaching connectors.</summary>
	public class NodeProxy :Node
	{
		// Notes:
		// - Public so that sub-classed controls can detect and exclude it

		public NodeProxy()
			: base(new v4(20,20,0,0), Guid.NewGuid(), "ProxyNode", m4x4.Identity, new NodeStyle(Guid.Empty))
		{
			Anchor = new AnchorPoint(this, v4.Origin, v4.Zero);
		}

		/// <summary>Get the single anchor point of the proxy node</summary>
		public AnchorPoint Anchor { get; }

		/// <summary>The normal of the sole anchor point on this node. v4.Zero is valid</summary>
		public v4 AnchorNormal
		{
			get => Anchor.Normal;
			set
			{
				if (Anchor.Normal == value) return;
				Anchor.Normal = value;
				if (Connectors.Count != 0 && Connectors[0].Anc(this) is AnchorPoint ap)
					ap.Normal = value;
			}
		}

		/// <summary>Signal that the diagram needs laying out</summary>
		protected override void NotifyChartChanged()
		{
			// Don't raise diagram changed for the proxy node.
		}

		/// <summary>Return the preferred node size given the current text and upper size bounds</summary>
		public override v4 PreferredSize(v4 layout)
		{
			return new v4(20, 20, 0, 0);
		}

		/// <summary>Perform a hit test on this object. Returns null for no hit. 'point' is in diagram space</summary>
		public override ChartControl.HitTestResult.Hit? HitTest(Point chart_point, Point client_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
		{
			//var pt = point - Centre;
			//if (pt.LengthSq > Math_.Sqr(Bounds.Diametre / 2f))
			//	return null;
			//
			//var hit = new ChartControl.HitTestResult.Hit(this, pt);
			//return hit;
			return null;
		}

		/// <summary>Return all the locations that connectors can attach to on this element</summary>
		public override IEnumerable<AnchorPoint> AnchorPoints()
		{
			yield return Anchor;
		}

		/// <summary>Update the graphics and object transforms associated with this element</summary>
		protected override void UpdateGfxCore()
		{ }

		/// <summary>Add the graphics associated with this element to the window</summary>
		//protected override void AddToSceneCore(View3d.Window window)
		//{ }
	}
}