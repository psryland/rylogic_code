﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Input;
using Rylogic.Common;
using Rylogic.Gfx;
using Rylogic.LDraw;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.ChartDiagram
{
	public class QuadNode :Node
	{
		public QuadNode(string text, v4? size = null, m4x4? position = null, NodeStyle? style = null, Guid? id = null)
			:base(size ?? new v4(1,1,0,0), id ?? Guid.NewGuid(), text, position, style)
		{
			Size = Style.AutoSize ? PreferredSize(SizeMax) : Size;

			// Create a texture for drawing the node content into
			var tex_size = Size.xy * Style.TexelDensity;
			Surf = new Surface(tex_size.xi, tex_size.yi);

			// Create a quad model
			var ldr = new LdrBuilder();
			ldr.Rect("node", Colour32.White, EAxisId.PosZ, Size.x, Size.y, true, (float)Style.CornerRadius, v4.Origin);
			Gfx = new View3d.Object(ldr, false, null, null);
			Gfx.SetTexture(Surf.Surf);
		}
		protected override void Dispose(bool disposing)
		{
			Gfx = null!;
			Surf = null!;
			base.Dispose(disposing);
		}

		/// <summary>Graphics for the node</summary>
		protected View3d.Object Gfx
		{
			get => m_gfx;
			set
			{
				if (m_gfx == value) return;
				Util.Dispose(ref m_gfx!);
				m_gfx = value;
			}
		}
		private View3d.Object m_gfx = null!;

		/// <summary>Texture surface</summary>
		protected Surface Surf
		{
			get => m_surf;
			private set
			{
				if (m_surf == value) return;
				Util.Dispose(ref m_surf!);
				m_surf = value;
			}
		}
		private Surface m_surf = null!;

		/// <inheritdoc/>
		public override BBox Bounds => new BBox(O2W.pos, 0.5f * Size);

		/// <inheritdoc/>
		public override IEnumerable<AnchorPoint> AnchorPoints()
		{
			// Remember, returned points are in node space.
			var units_per_anchor = (float)Style.AnchorSpacing;

			// Get the dimensions and half dimensions
			var sz = Size;
			var hsx = Math.Max(0f, 0.5f * sz.x - (float)Style.CornerRadius);
			var hsy = Math.Max(0f, 0.5f * sz.y - (float)Style.CornerRadius);
			var z = 0f;

			// Quantise to the minimum anchor spacing
			hsx = (int)(hsx / units_per_anchor) * units_per_anchor;
			hsy = (int)(hsy / units_per_anchor) * units_per_anchor;
			
			int id = 0;

			// Left
			for (var y = -hsy; y <= +hsy; y += units_per_anchor)
				yield return new AnchorPoint(this, new v4(-0.5f * sz.x, y, z, 1), -v4.XAxis, id++);

			// Top
			for (var x = -hsx; x <= +hsx; x += units_per_anchor)
				yield return new AnchorPoint(this, new v4(x, +0.5f * sz.y, z, 1), +v4.YAxis, id++);

			// Right
			for (var y = +hsy; y >= -hsy; y -= units_per_anchor)
				yield return new AnchorPoint(this, new v4(+0.5f * sz.x, y, z, 1), +v4.XAxis, id++);

			// Bottom
			for (var x = +hsx; x >= -hsx; x -= units_per_anchor)
				yield return new AnchorPoint(this, new v4(x, -0.5f * sz.y, z, 1), -v4.YAxis, id++);
		}

		/// <inheritdoc/>
		protected override void UpdateGfxCore()
		{
			base.UpdateGfxCore();

			// Refresh the texture content
			{
				var size = Surf.Size;
				using var tex = Surf.LockSurface(discard: true);
				tex.Gfx.CompositingMode = CompositingMode.SourceOver;
				tex.Gfx.CompositingQuality = CompositingQuality.HighQuality;
				tex.Gfx.SmoothingMode = SmoothingMode.AntiAlias;

				var fill_colour = Style.Fill;
				if (Hovered) fill_colour = Style.Hovered;
				if (Selected) fill_colour = Style.Selected;
				if (!Enabled) fill_colour = Style.Disabled;
				using var fill_brush = new SolidBrush(fill_colour);
				tex.Gfx.FillRectangle(fill_brush, -1, -1, size.x+2, size.y+2);
				//tex.Gfx.Clear(fill_colour);

				var text_colour = Style.Text;
				if (!Enabled) text_colour = Style.TextDisabled;
				using var text_brush = new SolidBrush(text_colour);

				var loc = TextLocation(tex.Gfx, size.ToRectangleF());
				tex.Gfx.DrawString(Text, Style.Font, text_brush, loc, TextFormat);
			}

			// Refresh the model geometry
			{
				var ldr = new LdrBuilder();
				ldr.Rect("node", Colour32.White, EAxisId.PosZ, Size.x, Size.y, true, (float)Style.CornerRadius, v4.Origin);
				Gfx.UpdateModel(ldr, View3d.EUpdateObject.Model);
				Gfx.SetTexture(Surf.Surf);
			}
		}

		/// <inheritdoc/>
		protected override void UpdateSceneCore()
		{
			base.UpdateSceneCore();
			if (Chart is not ChartControl chart || Gfx == null)
				return;
			
			// Don't set 'O2W' here, leave it to derived classes

			// Set the node to world transform
			Gfx.O2P = O2W;
			if (Visible)
				chart.Scene.Window.AddObject(Gfx);
			else
				chart.Scene.Window.RemoveObject(Gfx);
		}

		/// <inheritdoc/>
		public override ChartControl.HitTestResult.Hit? HitTest(v4 chart_point, v2 scene_point, ModifierKeys modifier_keys, EMouseBtns mouse_btns, View3d.Camera cam)
		{
			if (Chart == null)
				return null;

			var ray = cam.RaySS(scene_point);
			var results = Chart.Scene.Window.HitTest(ray, 0f, View3d.EHitTestFlags.Faces, new[] { Gfx });
			if (!results.IsHit)
				return null;

			// Convert the hit point to node space
			var pt = Math_.InvertFast(O2W) * results.m_ws_intercept;
			var hit = new ChartControl.HitTestResult.Hit(this, pt, null);
			return hit;
		}
	}
}