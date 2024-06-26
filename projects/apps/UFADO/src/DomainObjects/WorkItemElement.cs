﻿using System;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Rylogic.Gfx;
using Rylogic.Gui.WPF.ChartDiagram;
using Rylogic.Maths;
using Rylogic.Utility;

namespace UFADO.DomainObjects;

public abstract class WorkItemElement : Node
{
	public WorkItemElement(Guid id, WorkItem wi)
		: base(v4.One, id, wi.Title())
	{
		Item = wi;
	}
	protected override void Dispose(bool disposing)
	{
		Gfx = null!;
		//Surf = null!;
		Item = null!;
		base.Dispose(disposing);
	}

	/// <summary>ADO Item Id</summary>
	public int ItemId => Item.Id ?? -1;

	/// <summary>Item parent</summary>
	public long? ParentItem => Item.Fields.TryGetValue("System.Parent", out var v) ? (long)v : null;

	/// <summary>The work item source data</summary>
	public WorkItem Item
	{
		get => m_src;
		set
		{
			if (m_src == value) return;
			m_src = value;
			// Generate?
		}
	}
	private WorkItem m_src = null!;

	/// <summary>Graphics for the node</summary>
	public View3d.Object Gfx
	{
		get => m_gfx;
		protected set
		{
			if (m_gfx == value) return;
			Util.Dispose(ref m_gfx!);
			m_gfx = value;
		}
	}
	private View3d.Object m_gfx = null!;

	///// <summary>The style for all work streams</summary>
	//public static NodeStyle DefaultStyle { get; } = new NodeStyle
	//{
	//	//Fill = new Colour32(0xFFB0B1EU),
	//	CornerRadius = 0,
	//	TexelDensity = 4,
	//};

	///// <summary>Texture surface</summary>
	//protected Surface Surf
	//{
	//	get => m_surf;
	//	private set
	//	{
	//		if (m_surf == value) return;
	//		Util.Dispose(ref m_surf!);
	//		m_surf = value;
	//	}
	//}
	//private Surface m_surf = null!;

	///// <summary>The current node text colour given the hovering/selection state</summary>
	//public Colour32 TextColour
	//{
	//	get
	//	{
	//		var text_colour = Style.Text;
	//		if (!Enabled) text_colour = Style.TextDisabled;
	//		return text_colour;
	//	}
	//}

	///// <summary>The current node background colour given the hovering/selection state</summary>
	//public Colour32 BackgroundColour
	//{
	//	get
	//	{
	//		// Background colour
	//		var fill_colour = Style.Fill;
	//		if (Hovered) fill_colour = Style.Hovered;
	//		if (Selected) fill_colour = Style.Selected;
	//		if (!Enabled) fill_colour = Style.Disabled;
	//		return fill_colour;
	//	}
	//}

	///// <inheritdoc/>
	//public override BBox Bounds => new BBox(O2W.pos, 0.5f * Size);

	///// <summary>Render the background colour to the node</summary>
	//protected void RenderBkgd(View3d.Texture.Lock tex)
	//{
	//	using var fill_brush = new SolidBrush(BackgroundColour);
	//	tex.Gfx.FillRectangle(fill_brush, -1, -1, tex.Size.Width + 2, tex.Size.Height + 2);
	//}

	///// <summary>Render the text to the node texture</summary>
	//protected void RenderText(View3d.Texture.Lock tex)
	//{
	//	using var text_brush = new SolidBrush(TextColour);
	//	var loc = TextLocation(tex.Gfx, new RectangleF(PointF.Empty, tex.Size));
	//	tex.Gfx.DrawString(Text, Style.Font, text_brush, loc, TextFormat);
	//}

	///// <inheritdoc/>
	//protected override void UpdateGfxCore()
	//{
	//	base.UpdateGfxCore();

	//	// Refresh the texture content
	//	{
	//		using var tex = Surf.LockSurface(discard: true);
	//		RenderBkgd(tex);
	//		RenderText(tex);
	//	}

	//	// Refresh the model geometry when the size changes
	//	if (Size.xy != Surf.Size)
	//	{
	//		var ldr = new LdrBuilder();
	//		ldr.Rect("node", Colour32.White, EAxisId.PosZ, Size.x, Size.y, true, (float)Style.CornerRadius, v4.Origin);
	//		Gfx.UpdateModel(ldr, View3d.EUpdateObject.Model);
	//		Gfx.SetTexture(Surf.Surf);
	//	}
	//}

	/// <inheritdoc/>
	protected override void UpdateSceneCore(View3d.Window window, View3d.Camera camera)
	{
		base.UpdateSceneCore(window, camera);
		if (Gfx == null)
			return;

		// Set the node to world transform
		Gfx.O2P = O2W;
		window.AddObject(Gfx);
	}

	/// <inheritdoc/>
	protected override void RemoveFromSceneCore(View3d.Window window)
	{
		base.RemoveFromSceneCore(window);
		if (Gfx == null)
			return;

		window.RemoveObject(Gfx);
	}
}

