using System;
using System.Diagnostics;
using System.Windows.Controls;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Rylogic.Gfx;
using Rylogic.Gui.WPF;
using Rylogic.Gui.WPF.ChartDiagram;
using Rylogic.Maths;
using Rylogic.Utility;
using UFADO.Utility;

namespace UFADO.DomainObjects;

public abstract class WorkItemNode :Node
{
	// Notes:
	//  - Work items don't include the ADO WorkItem by default so that they can be
	//    stored and loaded locally. The ADO WorkItem is set later when the data is
	//    loaded from the server.

	public WorkItemNode(ItemId item_id, string title, ItemId? parent_item_id)
		: base(v4.One, Guid.NewGuid(), title)
	{
		ItemId = item_id;
		ParentItemId = parent_item_id;

		// All objects have a text label
		TitleGfx = new()
		{
			Text = Text,
			Background = Colour32.Transparent.ToMediaBrush(),
			Foreground = Colour32.White.ToMediaBrush(),
			IsHitTestVisible = false,
		};
	}
	protected override void Dispose(bool disposing)
	{
		Item = null!;
		Gfx = null!;
		TitleGfx.Detach();
		base.Dispose(disposing);
	}

	/// <summary>ADO Item Id</summary>
	public ItemId ItemId { get; }

	/// <summary>ADO Parent Item Id</summary>
	public ItemId? ParentItemId { get; private set; }

	/// <summary>The item title</summary>
	public string Title
	{
		get => Text;
		set
		{
			if (Title == value) return;
			Text = value;
			TitleGfx.Text = Text;
		}
	}

	/// <summary>The work item source data</summary>
	public WorkItem? Item
	{
		get => m_src;
		set
		{
			if (m_src == value)
				return;
			m_src = value;
			if (m_src != null)
			{
				Debug.Assert(m_src.Id == ItemId);

				// Update fields
				Title = m_src.Title();
				ParentItemId = m_src.ParentItemId();
			}
		}
	}
	private WorkItem? m_src;

	/// <summary>The text name for the item</summary>
	private TextBlock TitleGfx { get; }

	/// <summary>Graphics for the item</summary>
	public View3d.Object Gfx
	{
		get => m_gfx;
		protected set
		{
			if (m_gfx == value)
				return;
			Util.Dispose(ref m_gfx!);
			m_gfx = value;
		}
	}
	private View3d.Object m_gfx = null!;

	/// <inheritdoc/>
	protected override void UpdateSceneCore(View3d.Window window, View3d.Camera camera)
	{
		base.UpdateSceneCore(window, camera);
		if (Chart == null)
			return;
		if (Gfx == null)
			return;

		// Position the node in the chart
		Gfx.O2P = O2W;
		window.AddObject(Gfx);

		// Position the title label
		//TODO: Need to make the title look like it's moving in 3d (same as billboard 3d)
		var pt_cs = Chart.Camera.W2O * O2W.pos;
		var distance = Chart.Camera.NormalisedDistance(-pt_cs.z);
		if (distance > 0 && distance < 1)
		{
			Chart.Overlay.Adopt(TitleGfx);
			var title_pos = Chart.ChartToScene(pt_cs);
			Canvas.SetLeft(TitleGfx, title_pos.x);
			Canvas.SetTop(TitleGfx, title_pos.y);
		}
		else
		{
			TitleGfx.Detach();
		}
	}

	/// <inheritdoc/>
	protected override void RemoveFromSceneCore(View3d.Window window)
	{
		base.RemoveFromSceneCore(window);
		if (Gfx == null)
			return;

		window.RemoveObject(Gfx);
		TitleGfx.Detach();
	}
}


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