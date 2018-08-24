import * as Math_ from "../maths/maths";
import * as M4x4_ from "../maths/m4x4";
import * as Vec4_ from "../maths/v4";
import * as Vec2_ from "../maths/v2";
import * as Size_ from "../maths/size";
import * as Rect_ from "../maths/rect";
import * as BBox_ from "../maths/bbox";
import * as Rdr_ from "../renderer/renderer";
import * as Alg_ from "../common/algorithm";
import * as MC_ from "../common/multicast";
import * as Util_ from "../utility/util";
import M4x4 = Math_.M4x4;
import Vec4 = Math_.Vec4;
import Vec2 = Math_.Vec2;
import BBox = Math_.BBox;
import Size = Math_.Size;
import Rect = Math_.Rect;

/** Areas on the chart */
export enum EZone
{
	None = 0,
	Chart = 1 << 0,
	XAxis = 1 << 1,
	YAxis = 1 << 2,
	Title = 1 << 3,
}

/** Chart movement types */
export enum EMove
{
	None = 0,
	XZoomed = 1 << 0,
	YZoomed = 1 << 1,
	Zoomed = 0x3 << 0,
	XScroll = 1 << 2,
	YScroll = 1 << 3,
	Scroll = 0x3 << 2,
}

/** Mouse button enumeration */
export enum EButton
{
	Left = 0,
	Middle = 1,
	Right = 2,
	Back = 3,
	Forward = 4,
}

/** Chart navigation modes */
export enum ENavMode
{
	Chart2D = 0,
	Scene3D = 1,
}

/** Area selection modes */
export enum EAreaSelectMode
{
	Disabled = 0,
	SelectElements = 1,
	Zoom = 2,
}

/** Interface for chart elements */
export interface IElement
{
	/** The owning chart */
	chart: Chart;

	/** The unique id */
	id: string;

	/** The element to chart transform */
	o2w: M4x4;
	pos: Vec4;
	posZ: number;

	/** The bounding box of the element */
	bounds: BBox;

	/** True when this element has been selected */
	selected: boolean;

	/** True when the mouse is hovering over this element */
	hovered: boolean;

	/** True if the element is active for interaction on the chart */
	enabled: boolean;

	/** Position recorded at the time dragging start */
	drag_start_position: M4x4;

	/** Perform a hit test on this object.
	 * @param chart_point The hit test point in chart space
	 * @param client_point The hit test point in client space
	 * @param modifier_keys The modifier keys pressed at the time of the hit test
	 * @param cam The state of the camera at the time of the hit test
	 * @returns Return null for no hit.
	 */
	HitTest: (chart_point: Vec2, client_point: Vec2, modifier_keys: IModifierKeys, cam: Rdr_.Camera.ICamera) => HitTestResult.Hit;

	/** Raised when this element is clicked */
	OnClicked: MC_.MulticastDelegate<IElement, IChartClickedArgs>;
}

/** The state of the modifier keys */
export interface IModifierKeys
{
	ctrl: boolean;
	shift: boolean;
	alt: boolean;
}

/** HTML chart control based on WebGL */
export class Chart
{
	/**
	 * Create a chart in the given canvas element.
	 * @param canvas The HTML element to turn into a chart
	 */
	constructor(canvas: HTMLCanvasElement)
	{
		if (!canvas || !canvas.parentNode)
			throw new Error("Canvas cannot be null or undefined, or have no parent");

		// Hold a reference to the element we're drawing on
		// Create a duplicate canvas to draw 2d stuff on, and position it
		// at the same location, and over the top of, 'canvas'.
		// 'canvas2d' is an overlay over 'canvas3d'.
		this.canvas3d = canvas;
		this.canvas2d = <HTMLCanvasElement>canvas.cloneNode(false);
		this.canvas2d.style.position = "absolute";
		this.canvas2d.style.border = "none";
		this.canvas2d.style.left = this.canvas3d.style.left;
		this.canvas2d.style.top = this.canvas3d.style.top;
		canvas.parentNode.insertBefore(this.canvas2d, this.canvas3d);
	
		// Get the 2D drawing interface
		this.gfx2d = <CanvasRenderingContext2D>this.canvas2d.getContext("2d");
	
		// Initialise WebGL
		this.gfx3d = new Rdr_.Renderer(this.canvas3d);
		this.gfx3d.back_colour = Vec4_.create(1,1,1,1);
		this._redraw_pending = false;
	
		// Create an area for storing cached data
		this._cache =
		{
			chart_frame: null,
			xaxis_hash: "",
			yaxis_hash: "",
			invalidate: function(){ this.chart_frame = null; },
		};

		// The manager of chart utility graphics
		this._tools = new Tools(this);

		// Chart moved args
		this._chart_moved_args = null;

		// The collection of instances to render on the chart
		this.instances = [];
	
		// Initialise the chart camera
		this.camera = Rdr_.Camera.Create(Math_.TauBy8, this.gfx3d.aspect);
		this.camera.orthographic = true;
		this.camera.LookAt(Vec4_.create(0, 0, 10, 1), Vec4_.Origin, Vec4_.YAxis);
		this.camera.KeyDown = function()
		{
			return false;
		};
	
		// The light source the illuminates the chart
		this.light = Rdr_.Light.Create(Rdr_.ELight.Directional, Vec4_.Origin, Vec4_.Neg(Vec4_.ZAxis), undefined, [0.5,0.5,0.5], undefined, undefined);
	
		// Navigation
		this.navigation = new Navigation(this);
	
		// Chart options
		this.options = new ChartOptions();

		// Events
		this.OnChartMoved = new MC_.MulticastDelegate();
		this.OnChartClicked = new MC_.MulticastDelegate();
		this.OnChartAreaSelect = new MC_.MulticastDelegate();
		this.OnRendering = new MC_.MulticastDelegate();
		this.OnAutoRange = new MC_.MulticastDelegate();

		// Chart title
		this.title = "";
	
		// The data series
		this.elements = [];

		// Initialise Axis data
		let This = this;
		this.xaxis = new Axis(this, this.options.xaxis, "");
		this.yaxis = new Axis(this, this.options.yaxis, "");
		this.xaxis.OnScroll.sub(() => This._RaiseChartMoved(EMove.XScroll));
		this.xaxis.OnZoomed.sub(() => This._RaiseChartMoved(EMove.XZoomed));
		this.yaxis.OnScroll.sub(() => This._RaiseChartMoved(EMove.YScroll));
		this.yaxis.OnZoomed.sub(() => This._RaiseChartMoved(EMove.YZoomed));

		// Layout data for the chart
		this.dimensions = this.ChartDimensions();

		// Create a loop to watch for pending renders
		window.requestAnimationFrame(function loop()
		{
			if (This._redraw_pending) This.Render();
			window.requestAnimationFrame(loop);
		});
	}

	/** The title for the chart */
	public title: string;

	/** Chart elements series */
	public elements: IElement[];

	/** The X axis data for the chart */
	public xaxis: Axis;

	/** The Y axis data for the chart */
	public yaxis: Axis;

	/** Chart options */
	public options: ChartOptions;

	/** The canvas used for 3d content */
	readonly canvas3d: HTMLCanvasElement;

	/** The canvas used for 2d content */
	readonly canvas2d: HTMLCanvasElement;

	/** The 2D context */
	readonly gfx2d: CanvasRenderingContext2D;

	/** The 3D context */
	readonly gfx3d: Rdr_.Renderer;

	/** Instances to render on the chart */
	public instances: Rdr_.Instance.IInstance[];

	/** The camera used to view the chart */
	public camera: Rdr_.Camera.ICamera;

	/** The light source for the chart */
	public light: Rdr_.Light.ILight;

	/** The navigation control for the chart */
	public navigation: Navigation;

	/** Layout data for the chart */
	public dimensions: ChartDimensions;

	/** Cache data */
	private _cache:
		{
			chart_frame: HTMLCanvasElement | null,
			xaxis_hash: string,
			yaxis_hash: string,
			invalidate: () => void,
		};

	/** Private tools/graphics for the chart */
	public _tools: Tools;

	/** Events */
	public OnChartMoved: MC_.MulticastDelegate<Chart, IChartMovedArgs>;
	public OnChartClicked: MC_.MulticastDelegate<Chart, IChartClickedArgs>;
	public OnChartAreaSelect: MC_.MulticastDelegate<Chart, IChartAreaSelectArgs>;
	public OnRendering: MC_.MulticastDelegate<Chart, IRenderingArgs>;
	public OnAutoRange: MC_.MulticastDelegate<Chart, IAutoRangeArgs>;

	/** Get/Set whether the aspect ratio is locked to the current value */
	get lock_aspect(): boolean
	{
		return this.options.lock_aspect != null;
	}
	set lock_aspect(value: boolean)
	{
		if (this.lock_aspect == value) return;
		if (value)
			this.options.lock_aspect = (this.xaxis.span * this.dimensions.chart_area.h) / (this.yaxis.span * this.dimensions.chart_area.w);
		else
			this.options.lock_aspect = null;
	}

	/** Selected chart elements */
	get selected(): IElement[]
	{
		return this.elements.filter(x => x.selected);
	}

	/** Hovered chart elements */
	get hovered(): IElement[]
	{
		return this.elements.filter(x => x.hovered);
	}

	/**
	 * Render the chart
	 * @param {Chart} chart The chart to be rendered
	 */
	Render(): void
	{
		// Clear the redraw pending flag
		this._redraw_pending = false;

		// Navigation moves the camera and axis ranges are derived from the camera position/view
		this.SetRangeFromCamera();

		// Calculate the chart dimensions
		this.dimensions = this.ChartDimensions();

		// Draw the chart frame
		this._RenderChartFrame(this.dimensions);

		// Draw the chart content
		this._RenderChartContent(this.dimensions);
	}
	private _redraw_pending: boolean;

	/**
	 * Draw the chart frame
	 * @param dims The chart size data returned from ChartDimensions
	 */
	_RenderChartFrame(dims: ChartDimensions): void
	{
		// Drawing the frame can take a while. Cache the frame image so if the chart
		// is not moved, we can just blit the cached copy to the screen.
		let repaint = true;
		for (;;)
		{
			// Control size changed?
			if (this._cache.chart_frame == null ||
				this._cache.chart_frame.width != dims.area.w ||
				this._cache.chart_frame.height != dims.area.h)
			{
				// Create a double buffer
				this._cache.chart_frame = <HTMLCanvasElement>this.canvas2d.cloneNode(false);
				break;
			}

			// Axes zoomed/scrolled?
			if (this.xaxis.hash != this._cache.xaxis_hash)
				break;
			if (this.yaxis.hash != this._cache.yaxis_hash)
				break;

			// Rendering options changed?
			//if (m_this.XAxis.Options.ChangeCounter != m_xaxis_options)
			//	break;
			//if (m_this.YAxis.Options.ChangeCounter != m_yaxis_options)
			//	break;

			// Cached copy is still good
			repaint = false;
			break;
		}

		// Repaint the frame if the cached version is out of date
		if (repaint)
		{
			let bmp = <CanvasRenderingContext2D>this._cache.chart_frame.getContext("2d");
			let buffer = this._cache.chart_frame;
			let opts = this.options;
			let xaxis = this.xaxis;
			let yaxis = this.yaxis;

			// This is not enforced in the axis.Min/Max accessors because it's useful
			// to be able to change the min/max independently of each other, set them
			// to float max etc. It's only invalid to render a chart with a negative range
			//assert(xaxis.span > 0, "Negative x range");
			//assert(yaxis.span > 0, "Negative y range");

			bmp.save();
			bmp.clearRect(0, 0, buffer.width, buffer.height);

			// Clear to the background colour
			bmp.fillStyle = opts.bk_colour;
			bmp.fillRect(dims.area.l, dims.area.t, dims.chart_area.l - dims.area.l + 1, dims.area.b - dims.area.t + 1); // left band
			bmp.fillRect(dims.chart_area.r, dims.area.t, dims.area.r - dims.chart_area.r + 1, dims.area.b - dims.area.t + 1); // right band
			bmp.fillRect(dims.chart_area.l, dims.area.t, dims.chart_area.r - dims.chart_area.l + 1, dims.chart_area.t - dims.area.t + 1); // top band
			bmp.fillRect(dims.chart_area.l, dims.chart_area.b, dims.chart_area.r - dims.chart_area.l + 1, dims.area.b - dims.chart_area.b + 1); // bottom band

			// Draw the chart title and labels
			if (this.title && this.title.length > 0)
			{
				bmp.save();
				bmp.font = opts.title_font;
				bmp.fillStyle = opts.title_colour;

				let r = dims.title_size;
				let x = dims.area.l + (dims.area.w - r[0]) * 0.5;
				let y = dims.area.t + (opts.margin.top + opts.title_padding + r[1]);
				bmp.transform(1, 0, 0, 1, x, y);
				bmp.transform(opts.title_transform.m00, opts.title_transform.m01, opts.title_transform.m10, opts.title_transform.m11, opts.title_transform.dx, opts.title_transform.dy);
				bmp.fillText(this.title, 0, 0);
				bmp.restore();
			}
			if (xaxis.label && xaxis.label.length > 0 && opts.show_axes)
			{
				bmp.save();
				bmp.font = opts.xaxis.label_font;
				bmp.fillStyle = opts.xaxis.label_colour;

				let r = dims.xlabel_size;
				let x = dims.area.l + (dims.area.w - r[0]) * 0.5;
				let y = dims.area.b - opts.margin.bottom;// - r.y;
				bmp.transform(1, 0, 0, 1, x, y);
				bmp.transform(opts.xaxis.label_transform.m00, opts.xaxis.label_transform.m01, opts.xaxis.label_transform.m10, opts.xaxis.label_transform.m11, opts.xaxis.label_transform.dx, opts.xaxis.label_transform.dy);
				bmp.fillText(xaxis.label, 0, 0);
				bmp.restore();
			}
			if (yaxis.label && yaxis.label.length > 0 && opts.show_axes)
			{
				bmp.save();
				bmp.font = opts.yaxis.label_font;
				bmp.fillStyle = opts.yaxis.label_colour;

				let r = dims.ylabel_size;
				let x = dims.area.l + opts.margin.left + r[1];
				let y = dims.area.t + (dims.area.h - r[0]) * 0.5;
				bmp.transform(0, -1, 1, 0, x, y);
				bmp.transform(opts.yaxis.label_transform.m00, opts.yaxis.label_transform.m01, opts.yaxis.label_transform.m10, opts.yaxis.label_transform.m11, opts.yaxis.label_transform.dx, opts.yaxis.label_transform.dy);
				bmp.fillText(yaxis.label, 0, 0);
				bmp.restore();
			}

			// Tick marks and labels
			if (opts.show_axes)
			{
				if (opts.xaxis.draw_tick_labels || opts.xaxis.draw_tick_marks)
				{
					bmp.save();
					bmp.font = opts.xaxis.tick_font;
					bmp.textBaseline = 'top';
					bmp.fillStyle = opts.xaxis.tick_colour;
					bmp.textAlign = "center";
					bmp.lineWidth = 0;
					bmp.beginPath();

					let fh = Rdr_.Canvas.FontHeight(bmp, bmp.font);
					let Y = (dims.chart_area.b + opts.xaxis.tick_length + 1);
					let gl = xaxis.GridLines(dims.chart_area.w, opts.xaxis.pixels_per_tick);
					for (let x = gl.min; x < gl.max; x += gl.step)
					{
						let X = dims.chart_area.l + x*dims.chart_area.w/xaxis.span;
						if (opts.xaxis.draw_tick_labels)
						{
							let s = xaxis.TickText(x + xaxis.min, gl.step).split('\n');
							for (let i = 0; i != s.length; ++i)
								bmp.fillText(s[i], X, Y+i*fh, dims.xtick_label_size[0]);
						}
						if (opts.xaxis.draw_tick_marks)
						{
							bmp.moveTo(X, dims.chart_area.b);
							bmp.lineTo(X, dims.chart_area.b + opts.xaxis.tick_length);
						}
					}

					bmp.imageSmoothingEnabled = false;
					bmp.stroke();
					bmp.restore();
				}
				if (opts.yaxis.draw_tick_labels || opts.yaxis.draw_tick_marks)
				{
					bmp.save();
					bmp.font = opts.yaxis.tick_font;
					bmp.fillStyle = opts.yaxis.tick_colour;
					bmp.textBaseline = 'top';
					bmp.textAlign = "right";
					bmp.lineWidth = 0;
					bmp.beginPath();

					let fh = Rdr_.Canvas.FontHeight(bmp, bmp.font);
					let X = (dims.chart_area.l - opts.yaxis.tick_length - 1);
					let gl = yaxis.GridLines(dims.chart_area.h, opts.yaxis.pixels_per_tick);
					for (let y = gl.min; y < gl.max; y += gl.step)
					{
						let Y = dims.chart_area.b - y*dims.chart_area.h/yaxis.span;
						if (opts.yaxis.draw_tick_labels)
						{
							let s = yaxis.TickText(y + yaxis.min, gl.step).split('\n');
							for (let i = 0; i != s.length; ++i)
								bmp.fillText(s[i], X, Y+i*fh - dims.ytick_label_size[1]*0.5, dims.ytick_label_size[0]);
						}
						if (opts.yaxis.draw_tick_marks)
						{
							bmp.moveTo(dims.chart_area.l - opts.yaxis.tick_length, Y);
							bmp.lineTo(dims.chart_area.l, Y);
						}
					}

					bmp.imageSmoothingEnabled = false;
					bmp.stroke();
					bmp.restore();
				}

				{// Axes
					bmp.save();
					bmp.fillStyle = opts.axis_colour;
					bmp.lineWidth = opts.axis_thickness;
					bmp.moveTo(dims.chart_area.l+1, dims.chart_area.t);
					bmp.lineTo(dims.chart_area.l+1, dims.chart_area.b);
					bmp.lineTo(dims.chart_area.r, dims.chart_area.b);
					bmp.stroke();
					bmp.restore();
				}

				// Record cache invalidating values
				this._cache.xaxis_hash = this.xaxis.hash;
				this._cache.yaxis_hash = this.yaxis.hash;
			}
			bmp.restore();
		}

		let gfx2d = this.gfx2d;

		gfx2d.save();

		// Update the clipping region
		gfx2d.rect(dims.area.l, dims.area.t, dims.area.w, dims.area.h);
		gfx2d.rect(dims.chart_area.l, dims.chart_area.t, dims.chart_area.w, dims.chart_area.h);
		gfx2d.clip();

		// Blit the cached image to the screen
		gfx2d.imageSmoothingEnabled = false;
		gfx2d.drawImage(this._cache.chart_frame, dims.area.l, dims.area.t);
		gfx2d.restore();
	}

	/**
	 * Draw the chart content
	 * @param dims The chart size data returned from ChartDimensions
	 */
	_RenderChartContent(dims: ChartDimensions): void
	{
		this.instances.length = 0;

		// Add axis graphics
		if (this.options.show_grid_lines)
		{
			// Position the grid lines so that they line up with the axis tick marks.
			// Grid lines are modelled from the bottom left corner
			let wh = this.camera.ViewArea();
			
			{// X axis
				let xlines = this.xaxis.GridLineGfx();
				let gl = this.xaxis.GridLines(dims.chart_area.w, this.options.xaxis.pixels_per_tick);

				let ofs = Vec4_.create(wh[0]/2 - gl.min, wh[1]/2, this.options.grid_z_offset, 0);
				M4x4_.MulMV(this.camera.c2w, ofs, ofs);
				Vec4_.Sub(this.camera.focus_point, ofs, ofs);

				M4x4_.clone(this.camera.c2w, xlines.o2w);
				M4x4_.SetW(xlines.o2w, ofs);
				this.instances.push(xlines);
			}
			{// Y axis
				let ylines = this.yaxis.GridLineGfx();
				let gl = this.yaxis.GridLines(dims.chart_area.h, this.options.yaxis.pixels_per_tick);

				let ofs = Vec4_.create(wh[0]/2, wh[1]/2 - gl.min, this.options.grid_z_offset, 0);
				M4x4_.MulMV(this.camera.c2w, ofs, ofs);
				Vec4_.Sub(this.camera.focus_point, ofs, ofs);

				M4x4_.clone(this.camera.c2w, ylines.o2w);
				M4x4_.SetW(ylines.o2w, ofs);
				this.instances.push(ylines);
			}
		}

		// Add user graphics
		this.OnRendering.invoke(this, {});

		// Ensure the viewport matches the chart context area
		// WebGL view ports have 0,0 in the lower left, but 'dims.chart_area' has 0,0 at top left.
		this.gfx3d.webgl.viewport(dims.chart_area.l - dims.area.l, dims.area.b - dims.chart_area.b, dims.chart_area.w, dims.chart_area.h);
		
		// Render the chart
		this.gfx3d.Render(this.instances, this.camera, this.light);
	}

	/**
	 * Invalidate cached data before a Render
	 */
	Invalidate(): void
	{
		this._cache.invalidate();
		if (this._redraw_pending)
			return;

		// Set a flag to indicate redraw pending
		this._redraw_pending = true;
	}

	/**
	 * Set the axis range based on the position of the camera and the field of view.
	 */
	SetRangeFromCamera(): void
	{
		// The grid is always parallel to the image plane of the camera.
		// The camera forward vector points at the centre of the grid.
		let camera = this.camera;

		// Project the camera to world vector into camera space to determine the centre of the X/Y axis. 
		let w2c = camera.w2c;
		let x = -w2c[12];
		let y = -w2c[13];

		// The span of the X/Y axis is determined by the FoV and the focus point distance.
		let wh = camera.ViewArea();

		// Set the axes range
		let xmin = x - wh[0] * 0.5;
		let xmax = x + wh[0] * 0.5;
		let ymin = y - wh[1] * 0.5;
		let ymax = y + wh[1] * 0.5;
		if (xmin < xmax) this.xaxis.Set(xmin, xmax);
		if (ymin < ymax) this.yaxis.Set(ymin, ymax);
	}

	/**
	 * Position the camera based on the axis range.
	 */
	SetCameraFromRange(): void
	{
		// Set the aspect ratio from the axis range and scene bounds
		this.camera.aspect = (this.xaxis.span / this.yaxis.span);

		// Find the world space position of the new focus point.
		// Move the focus point within the focus plane (parallel to the camera XY plane)
		// and adjust the focus distance so that the view has a width equal to XAxis.Span.
		// The camera aspect ratio should ensure the YAxis is correct.
		let c2w = this.camera.c2w;
		let focus = Vec4_.AddN(
			Vec4_.MulS(M4x4_.GetX(c2w), this.xaxis.centre),
			Vec4_.MulS(M4x4_.GetY(c2w), this.yaxis.centre),
			Vec4_.MulS(M4x4_.GetZ(c2w), Vec4_.Dot(Vec4_.Sub(this.camera.focus_point, Vec4_.Origin), M4x4_.GetZ(c2w)))
			);

		// Move the camera in the camera Z axis direction so that the width at the focus dist
		// matches the XAxis range. Tan(FovX/2) = (XAxis.Span/2)/d
		let focus_dist = (this.xaxis.span / (2.0 * Math.tan(this.camera.fovX / 2.0)));
		let pos = Vec4_.SetW1(Vec4_.Add(focus, Vec4_.MulS(M4x4_.GetZ(c2w), focus_dist)))

		// Set the new camera position and focus distance
		this.camera.focus_dist = focus_dist;
		M4x4_.SetW(c2w, pos);
		this.camera.Commit();
	}

	/**
	 * Calculate the areas for the chart, and axes
	 */
	ChartDimensions(): ChartDimensions
	{
		let out = new ChartDimensions();
		let rect = Rect_.create(0, 0, this.canvas2d.width, this.canvas2d.height);
		let r = Size_.create();

		// Save the total chart area
		out.area = Rect_.clone(rect);

		// Add margins
		rect.x += this.options.margin.left;
		rect.y += this.options.margin.top;
		rect.w -= this.options.margin.left + this.options.margin.right;
		rect.h -= this.options.margin.top + this.options.margin.bottom;

		// Add space for the title
		if (this.title && this.title.length > 0)
		{
			r = Rdr_.Canvas.MeasureString(this.gfx2d, this.title, this.options.title_font);
			rect.y += r[1] + 2*this.options.title_padding;
			rect.h -= r[1] + 2*this.options.title_padding;
			out.title_size = r;
		}

		// Add space for the axes
		if (this.options.show_axes)
		{
			// Add space for tick marks
			if (this.options.yaxis.draw_tick_marks)
			{
				rect.x += this.options.yaxis.tick_length;
				rect.w -= this.options.yaxis.tick_length;
			}
			if (this.options.xaxis.draw_tick_marks)
			{
				rect.h -= this.options.xaxis.tick_length;
			}

			// Add space for the axis labels
			if (this.xaxis.label && this.xaxis.label.length > 0)
			{
				r = Rdr_.Canvas.MeasureString(this.gfx2d, this.xaxis.label, this.options.xaxis.label_font);
				rect.h -= r[1];
				out.xlabel_size = r;
			}
			if (this.yaxis.label && this.yaxis.label.length > 0)
			{
				r = Rdr_.Canvas.MeasureString(this.gfx2d, this.yaxis.label, this.options.yaxis.label_font);
				rect.x += r[1]; // will be rotated by 90deg
				rect.w -= r[1];
				out.ylabel_size = r;
			}

			// Add space for the tick labels
			// Note: If you're having trouble with the axis jumping around
			// check the 'TickText' callback is returning fixed length strings
			if (this.options.xaxis.draw_tick_labels)
			{
				// Measure the height of the tick text
				r = this.xaxis.MeasureTickText(this.gfx2d);
				rect.h -= r[1];
				out.xtick_label_size = r;
			}
			if (this.options.yaxis.draw_tick_labels)
			{
				// Measure the width of the tick text
				r = this.yaxis.MeasureTickText(this.gfx2d);
				rect.x += r[0];
				rect.w -= r[0];
				out.ytick_label_size = r;
			}
		}

		// Save the chart area
		if (rect.w < 0) rect.w = 0;
		if (rect.h < 0) rect.h = 0;
		out.chart_area = Rect_.clone(rect);

		return out;
	}

	/**
	 * Returns a point in chart space from a point in client space. Use to convert mouse (client-space) locations to chart coordinates
	 * @param client_point The client space point to convert to chart space
	 */
	ClientToChartPoint(client_point: Vec2): Vec2
	{
		return [
			(this.xaxis.min + (client_point[0] - this.dimensions.chart_area.l) * this.xaxis.span / this.dimensions.chart_area.w),
			(this.yaxis.min - (client_point[1] - this.dimensions.chart_area.b) * this.yaxis.span / this.dimensions.chart_area.h)];
	}

	/**
	 * Returns a size in chart space from a size in client space.
	 * @param client_size The client space size to convert to chart space
	 */
	ClientToChartSize(client_size: Size): Size
	{
		return [
			(client_size[0] * this.xaxis.span / this.dimensions.chart_area.w),
			(client_size[1] * this.yaxis.span / this.dimensions.chart_area.h)];
	}

	/**
	 * Returns a rectangle in chart space from a rectangle in client space
	 * @param client_rect The client space rectangle to convert to chart space
	 */
	ClientToChartRect(client_rect: Rect): Rect
	{
		let pt = this.ClientToChartPoint([client_rect.x, client_rect.y]);
		let sz = this.ClientToChartSize([client_rect.w, client_rect.h]);
		return Rect_.create(pt[0], pt[1], sz[0], sz[1]);
	}

	/**
	 * Returns a point in client space from a point in chart space. Inverse of ClientToChartPoint
	 * @param chart_point The chart space point to convert to client space
	 */
	ChartToClientPoint(chart_point: Vec2): Vec2
	{
		return [
			(this.dimensions.chart_area.l + (chart_point[0] - this.xaxis.min) * this.dimensions.chart_area.w / this.xaxis.span),
			(this.dimensions.chart_area.b - (chart_point[1] - this.yaxis.min) * this.dimensions.chart_area.h / this.yaxis.span)];
	}

	/**
	 * Returns a size in client space from a size in chart space. Inverse of ClientToChartSize
	 * @param chart_point The chart space size to convert to client space
	 */
	ChartToClientSize(chart_size: Size): Size
	{
		return [
			(chart_size[0] * this.dimensions.chart_area.w / this.xaxis.span),
			(chart_size[1] * this.dimensions.chart_area.h / this.yaxis.span)];
	}

	/**
	 * Returns a rectangle in client space from a rectangle in chart space. Inverse of ClientToChartRect
	 * @param chart_rect The chart space rectangle to convert to client space
	 */
	ChartToClientRect(chart_rect: Rect): Rect
	{
		let pt = this.ChartToClientPoint([chart_rect.x, chart_rect.y]);
		let sz = this.ChartToClientSize([chart_rect.w, chart_rect.h]);
		return Rect_.create(pt[0], pt[1], sz[0], sz[1]);
	}

	/**
	 * Return a point in camera space from a point in chart space (Z = focus plane)
	 * @param chart_point The chart space point to convert to camera space
	 */
	ChartToCamera(chart_point: Vec4): Vec4
	{
		// Remove the camera to origin offset
		let origin_cs = M4x4_.GetW(this.camera.w2c);
		let pt = [chart_point[0] + origin_cs[0], chart_point[1] + origin_cs[1]];
		return Vec4_.create(pt[0], pt[1], -this.camera.focus_dist, 1);
	}

	/**
	 * Return a point in chart space from a point in camera space
	 * @param camera_point The camera space point to convert to chart space
	 */
	CameraToChart(camera_point: Vec4): Vec2
	{
		if (camera_point[2] <= 0)
			throw new Error("Invalidate camera point. Points must be in front of the camera");

		// Project the camera space point onto the focus plane
		let proj = -this.camera.focus_dist / camera_point[2];
		let pt = [camera_point[0] * proj, camera_point[1] * proj];

		// Add the offset of the camera from the origin
		let origin_cs = M4x4_.GetW(this.camera.w2c);
		return [pt[0] - origin_cs[0], pt[1] - origin_cs[1]];
	}

	/**
	 * Get the scale and translation transform from chart space to client space.
	 * e.g. chart2client * Point(x_min, y_min) = plot_area.BottomLeft()
	 *      chart2client * Point(x_max, y_max) = plot_area.TopRight()
	 * @param plot_area The area of the chart content (defaults to the current content area)
	 */
	ChartToClientSpace(plot_area?: Rect): M4x4
	{
		plot_area = plot_area || this.dimensions.chart_area;

		let scale_x  = +(plot_area.w / this.xaxis.span);
		let scale_y  = -(plot_area.h / this.yaxis.span);
		let offset_x = +(plot_area.l - this.xaxis.min * scale_x);
		let offset_y = +(plot_area.b - this.yaxis.min * scale_y);

		// C = chart, c = client
		let C2c = M4x4_.create(
			Vec4_.create(scale_x  , 0        , 0, 0),
			Vec4_.create(0        , scale_y  , 0, 0),
			Vec4_.create(0        , 0        , 1, 0),
			Vec4_.create(offset_x , offset_y , 0, 1));

		return C2c;
	}

	/**
	 * Get the scale and translation transform from client space to chart space.
	 * e.g. client2chart * plot_area.BottomLeft() = Point(x_min, y_min)
	 *      client2chart * plot_area.TopRight()   = Point(x_max, y_max)
	 * @param plot_area The area of the chart content (defaults to the current content area)
	 */
	ClientToChartSpace(plot_area: Rect): M4x4
	{
		plot_area = plot_area || this.dimensions.chart_area;

		let scale_x  = +(this.xaxis.span / plot_area.w);
		let scale_y  = -(this.yaxis.span / plot_area.h);
		let offset_x = +(this.xaxis.min - plot_area.l * scale_x);
		let offset_y = +(this.yaxis.min - plot_area.b * scale_y);

		// C = chart, c = client
		let c2C = M4x4_.create(
			Vec4_.create(scale_x  , 0        , 0, 0),
			Vec4_.create(0        , scale_y  , 0, 0),
			Vec4_.create(0        , 0        , 1, 0),
			Vec4_.create(offset_x , offset_y , 0, 1));

		return c2C;
	}

	/**
	 * Convert a point between client space and normalised screen space
	 * @param client_point The client space point to convert to screen space
	 */
	ClientToNSSPoint(client_point: Vec2): Vec2
	{
		return [
			((client_point[0] - this.dimensions.chart_area.l) / this.dimensions.chart_area.w) * 2 - 1,
			((this.dimensions.chart_area.b - client_point[1]) / this.dimensions.chart_area.h) * 2 - 1];
	}

	/**
	 * Convert a size between client space and normalised screen space
	 * @param client_size The client space size to convert to normalised screen space
	 */
	ClientToNSSSize(client_size: Size): Size
	{
		return [
			(client_size[0] / this.dimensions.chart_area.w) * 2,
			(client_size[1] / this.dimensions.chart_area.h) * 2];
	}

	/**
	 * Convert a rectangle between client space and normalised screen space
	 * @param client_rect The client space rectangle to convert to normalised screen space
	 */
	ClientToNSSRect(client_rect: Rect): Rect
	{
		let pt = this.ClientToNSSPoint([client_rect.x, client_rect.y]);
		let sz = this.ClientToNSSSize([client_rect.w, client_rect.h]);
		return Rect_.create(pt[0], pt[1], sz[0], sz[1]);
	}

	/**
	 * Convert a normalised screen space point to client space
	 * @param nss_point The normalised screen space point to convert to client space
	 */
	NSSToClientPoint(nss_point: Vec2): Vec2
	{
		return [
			this.dimensions.chart_area.l + 0.5 * (nss_point[0] + 1) * this.dimensions.chart_area.w,
			this.dimensions.chart_area.b - 0.5 * (nss_point[1] + 1) * this.dimensions.chart_area.h];
	}

	/**
	 * Convert a normalised screen space size to client space
	 * @param nss_size The normalised screen space size to convert to client space
	 */
	NSSToClientSize(nss_size: Size): Size
	{
		return [
			0.5 * nss_size[0] * this.dimensions.chart_area.w,
			0.5 * nss_size[1] * this.dimensions.chart_area.h];
	}

	/**
	 * Convert a normalised screen space rectangle to client space
	 * @param nss_rect The normalised screen space rectangle to convert to client space
	 */
	NSSToClient(nss_rect: Rect): Rect
	{
		let pt = this.NSSToClientPoint([nss_rect.x, nss_rect.y]);
		let sz = this.NSSToClientSize([nss_rect.w, nss_rect.h]);
		return Rect_.create(pt[0], pt[1], sz[0], sz[1]);
	}

	/**
	 * Perform a hit test on the chart (in client space)
	 * @param client_point The client space point to hit test
	 * @param modifier_keys The state of shift, alt, ctrl keys
	 * @param pred A predicate function for filtering hit objects
	 */
	HitTestCS(client_point: Vec2, modifier_keys: IModifierKeys, pred?: (x: IElement)=>boolean): HitTestResult
	{
		// Determine the hit zone of the control
		let zone = EZone.None;
		if (Rect_.Contains(this.dimensions.chart_area, client_point)) zone |= EZone.Chart;
		if (Rect_.Contains(this.dimensions.xaxis_area, client_point)) zone |= EZone.XAxis;
		if (Rect_.Contains(this.dimensions.yaxis_area, client_point)) zone |= EZone.YAxis;
		if (Rect_.Contains(this.dimensions.title_area, client_point)) zone |= EZone.Title;

		// The hit test point in chart space
		let chart_point = this.ClientToChartPoint(client_point);

		// Find elements that overlap 'client_point'
		let hits:HitTestResult.Hit[] = [];
		if (zone & EZone.Chart)
		{
			let elements = pred ? this.elements.filter(pred) : this.elements;
			for (let elem of elements)
			{
				let hit = elem.HitTest(chart_point, client_point, modifier_keys, this.camera);
				if (hit != null)
					hits.push(hit);
			}

			// Sort the results by z order
			hits.sort((l,r) => r.element.posZ - l.element.posZ);
		}

		return new HitTestResult(zone, client_point, chart_point, modifier_keys, hits, this.camera);
	}

	/**
	 * Find the default range, then reset to the default range
	 */
	AutoRange(): void
	{
		// Allow the auto range to be handled by event
		let args = {who:"all", view_bbox:BBox_.create(), dims:this.dimensions, handled:false};
		this.OnAutoRange.invoke(this, args);

		// For now, auto ranging must be done by user code since the chart
		// doesn't know about what's in the scene.
		if (!args.handled)
			return;

		// If user code handled the auto ranging, sanity check
		if (args.handled && (args.view_bbox == null || !args.view_bbox.is_valid || Vec4_.LengthSq(args.view_bbox.radius) == 0))
			throw new Error("Caller provided view bounding box is invalid: " + args.view_bbox);

		// Get the bounding box to fit into the view
		let bbox = args.view_bbox;//args.handled ? args.view_bbox : Window.SceneBounds(who, except:new[] { ChartTools.Id });

		// Position the camera to view the bounding box
		this.camera.ViewBBox(bbox,
			this.options.reset_forward,
			this.options.reset_up,
			0, this.lock_aspect, true);

		// Set the axis range from the camera position
		this.SetRangeFromCamera();
		this.Invalidate();
	}

	/**
	 * Adjust the X/Y axis of the chart to cover the given area.
	 * @param area The bounding region to display in the chart
	 */
	PositionChartBBox(area: BBox): void
	{
		// Ignore if the given area is smaller than the minimum selection distance (in screen space)
		var bl = this.ChartToClientPoint(area.lower);
		var tr = this.ChartToClientPoint(area.upper);
		if (Math.abs(bl[0] - tr[0]) < this.options.min_selection_distance ||
			Math.abs(bl[1] - tr[1]) < this.options.min_selection_distance)
			return;

		this.xaxis.Set(bl[0], tr[0]);
		this.yaxis.Set(bl[1], tr[1]);
		this.SetCameraFromRange();
	}

	/**
	 * Shifts the X and Y range of the chart so that chart space position 'chart_point' is at client space position 'client_point'
	 * @param client_point The target position to align 'chart_point' to
	 * @param chart_point The point in chart space to move to 'client_point'
	 */
	PositionChartPoint(client_point: Vec2, chart_point: Vec2): void
	{
		var dst = this.ClientToChartPoint(client_point);
		var c2w = this.camera.c2w;

		M4x4_.SetW(c2w, Vec4_.AddN(
			Vec4_.MulS(M4x4_.GetX(c2w), (chart_point[0] - dst[0])),
			Vec4_.MulS(M4x4_.GetY(c2w), (chart_point[1] - dst[1])),
			M4x4_.GetW(c2w)));

		this.Invalidate();
	}

	/**
	 * Select elements that are wholly within 'rect'. (rect is in chart space)
	 * If no modifier keys are down, elements not in 'rect' are deselected.
	 * If 'shift' is down, elements within 'rect' are selected in addition to the existing selection
	 * If 'ctrl' is down, elements within 'rect' are removed from the existing selection.
	 * @param rect The rectangular area in which to do the selection
	 * @param modifier_keys The state of the modifier keys
	 */
	SelectElements(rect: Rect, modifier_keys: IModifierKeys): void
	{
		if (!this.options.allow_selection)
			return;

		// Normalise the selection
		var r = BBox_.create(
			Vec4_.create(rect.centre[0], rect.centre[1], 0, 1),
			Vec4_.create(Math.abs(rect.w * 0.5), Math.abs(rect.h * 0.5), 1, 0));

		// If the area of selection is less than the min drag distance, assume click selection
		var is_click = r.diametre_sq < Math_.Sqr(this.options.min_drag_pixel_distance);
		if (is_click)
		{
			var pt = this.ChartToClientPoint(rect.tl);
			var htr = this.HitTestCS(pt, modifier_keys, x => x.enabled);

			// If control is down, deselect the first selected element in the hit list
			if (modifier_keys.ctrl)
			{
				var first = Alg_.FirstOrDefault(htr.hits, x => x.element.selected);
				if (first != null)
					first.element.selected = false;
			}
			// If shift is down, select the first element not already selected in the hit list
			else if (modifier_keys.shift)
			{
				var first = Alg_.FirstOrDefault(htr.hits, x => !x.element.selected);
				if (first != null)
					first.element.selected = true;
			}
			// Otherwise select the next element below the current selection or the top
			// element if nothing is selected. Clear all other selections
			else
			{
				// Find the element to select
				var to_select = Alg_.FirstOrDefault(htr.hits);
				for (let i = 0; i < htr.hits.length - 1; ++i)
				{
					if (!htr.hits[i].element.selected) continue;
					to_select = htr.hits[i + 1];
					break;
				}

				// Deselect all elements and select the next element
				this.selected.length = 0;
				if (to_select != null)
					to_select.element.selected = true;
			}
		}
		// Otherwise it's area selection
		else
		{
			// If control is down, those in the selection area become deselected
			if (modifier_keys.ctrl)
			{
				// Only need to look in the selected list
				this.selected.every(x =>
				{
					x.selected = BBox_.IsBBoxWithin(r, x.bounds) == false;
					return true;
				});
			}
			// If shift is down, those in the selection area become selected
			else if (modifier_keys.shift)
			{
				this.elements.every(x =>
				{
					x.selected = x.selected || BBox_.IsBBoxWithin(r, x.bounds);
					return true;
				});
			}
			// Otherwise, the existing selection is cleared, and those within the selection area become selected
			else
			{
				this.elements.every(x =>
				{
					x.selected = BBox_.IsBBoxWithin(r, x.bounds);
					return true;
				});
			}
		}

		this.Invalidate();
	}

	/**
	 * Raise the OnChartMoved event on the next message.
	 * This method is designed to be called several times within one thread message.
	 * @param {EMove} move_type The movement type that occurred
	 */
	_RaiseChartMoved(move_type: EMove): void
	{
		if (move_type & EMove.Zoomed) this.xaxis._InvalidateGfx();
		if (move_type & EMove.Zoomed) this.yaxis._InvalidateGfx();

		if (!this._chart_moved_args)
		{
			// Trigger a redraw of the chart
			this.Invalidate();

			// Notify of the chart moving
			this._chart_moved_args = {move_type: EMove.None};
			Util_.BeginInvoke(() =>
			{
				this.OnChartMoved.invoke(this, <IChartMovedArgs>this._chart_moved_args);
				this._chart_moved_args = null;
			});
		}

		// Accumulate the moved flags until 'OnChartMoved' has been called.
		this._chart_moved_args.move_type |= move_type;
	}
	private _chart_moved_args: IChartMovedArgs | null;

	/**
	 * Handle area selection on the chart
	 * @param args
	 */
	_HandleChartAreaSelect(a: IChartAreaSelectArgs): void
	{
		this.OnChartAreaSelect.invoke(this, a);

		// Select chart elements by default
		if (!a.handled)
		{
			switch (this.options.area_select_mode)
			{
			default: throw new Error("Unknown area select mode");
			case EAreaSelectMode.Disabled:
				{
					break;
				}
			case EAreaSelectMode.SelectElements:
				{
					let min = a.selection_area.lower;
					let size = a.selection_area.size;
					let rect = Rect_.create(min[0], min[1], size[0], size[1]);
					this.SelectElements(rect, a.modifier_keys);
					break;
				}
			case EAreaSelectMode.Zoom:
				{
					this.PositionChartBBox(a.selection_area);
					break;
				}
			}
		}
	}

	/**
	 * Move the selected elements by 'delta'.
	 * @param delta The drag vector
	 * @param commit Commit the drag
	 */
	_DragSelected(delta: Vec2, commit: boolean): void
	{
		if (!this.options.allow_editing) return;
		for (let i = 0; i != this.selected.length; ++i)
		{
			// Drag the element 'delta' from the DragStartPosition
			let elem = this.selected[i];
			M4x4_.clone(elem.drag_start_position, elem.o2w);
			elem.o2w[12] += delta[0];
			elem.o2w[13] += delta[1];
			if (commit)
				M4x4_.clone(elem.o2w, elem.drag_start_position);
		}
	}
}

/** Chart layout data */
export class ChartDimensions
{
	constructor()
	{
		this.area = Rect_.create();
		this.chart_area = Rect_.create();
		this.title_size = Size_.create();
		this.xlabel_size = Size_.create();
		this.ylabel_size = Size_.create();
		this.xtick_label_size = Size_.create();
		this.ytick_label_size = Size_.create();
	}

	/** The total area of the whole chart and surrounding frame */
	public area: Rect;

	/** The area of the plot area */
	public chart_area: Rect;

	/** The width/height of the title text */
	public title_size: Size;

	/** The width/height of the x label text */
	public xlabel_size: Size;

	/** The width/height of the y label text */
	public ylabel_size: Size;

	/** The width/height for each x axis tick label */
	public xtick_label_size: Size;

	/** The width/height for each y axis tick label */
	public ytick_label_size: Size;

	/** The zone for the x axis region of the chart */
	get xaxis_area(): Rect
	{
		return Rect_.ltrb(this.chart_area.l, this.chart_area.b, this.chart_area.r, this.area.b);
	}

	/** The zone for the y axis region of the chart */
	get yaxis_area(): Rect
	{
		return Rect_.ltrb(0, this.chart_area.t, this.chart_area.l, this.chart_area.b);
	}

	/** The zone for the title region of the chart */
	get title_area(): Rect
	{
		return Rect_.ltrb(this.chart_area.l, 0, this.chart_area.r, this.chart_area.t);
	}
}

/** Hit test results structure */
export class HitTestResult
{
	/**
	 * The result of a chart hit test
	 * @param zone The zone on the chart that was hit
	 * @param client_point
	 * @param chart_point
	 * @param modifier_keys
	 * @param hits
	 * @param camera
	 */
	constructor(zone: EZone, client_point: Vec2, chart_point: Vec2, modifier_keys: IModifierKeys, hits: HitTestResult.Hit[], camera: Rdr_.Camera.ICamera)
	{
		this.zone = zone;
		this.client_point = client_point;
		this.chart_point = chart_point;
		this.modifier_keys = modifier_keys;
		this.hits = hits;
		this.camera = Rdr_.Camera.clone(camera);
	}

	/** The zone on the chart that was hit */
	public zone: EZone;

	/** The point on the chart that was hit (in client/screen space) */
	public client_point: Vec2;

	/** The point on the chart that was hit (in chart space) */
	public chart_point: Vec2;

	/** The modifier keys that where pressed at the time of the hit test */
	public modifier_keys: IModifierKeys;

	/** The chart elements that were hit */
	public hits: HitTestResult.Hit[];

	/** The state of the chart camera at the time of the hit test */
	public camera: Rdr_.Camera.ICamera;
}
export namespace HitTestResult
{
	export interface Hit
	{
		/** The element that was hit */
		element: IElement;

		/** Where on the element it was hit (in element space) */
		point: Vec2;

		/** The element's chart location at the time it was hit */
		location: M4x4;

		/** Optional context information collected during the hit test */
		context?: any;
	}
}

/** Chart axis data */
export class Axis
{
	/**
	 * An axis on the chart
	 * @param chart The owning chart
	 * @param options Options for this axis
	 * @param label The text label for the axis
	 */
	constructor(chart: Chart, options: AxisOptions, label: string)
	{
		this.min = 0;
		this.max = 1;
		this.chart = chart;
		this.label = label;
		this.options = options;
		this._geom_lines = null;
		this._allow_scroll = true;
		this._allow_zoom = true;
		this._lock_range = false;

		this.TickText = this.TickTextDefault;
		this.MeasureTickText = this.MeasureTickTextDefault;
		
		this.OnScroll = new MC_.MulticastDelegate();
		this.OnZoomed = new MC_.MulticastDelegate();
	}

	/** The minimum axis value */
	public min: number;

	/** The maximum axis value */
	public max: number;

	/** The owning chart */
	public chart: Chart;

	/** The axis label text */
	public label: string;

	/** The axis specific options */
	public options: AxisOptions;

	/** The function for converting the axis value to text */
	public TickText: (x: number, step: number) => string;

	/** The function for measuring the tick text */
	public MeasureTickText: (gfx: CanvasRenderingContext2D) => Size;

	/** Events */
	public OnScroll: MC_.MulticastDelegate<Axis, IScrollArgs>;
	public OnZoomed: MC_.MulticastDelegate<Axis, IZoomedArgs>;

	/**
	 * Get/Set the range of the axis
	 * Setting the span leaves the centre unchanged
	 */
	get span(): number
	{
		return this.max - this.min;
	}
	set span(value: number)
	{
		if (this.span == value) return;
		if (value <= 0) throw new Error("Invalid axis span: " + value);
		this.Set(this.centre - 0.5*value, this.centre + 0.5*value);
	}

	/**
	 * Get/Set the centre position of the axis
	 */
	get centre(): number
	{
		return (this.max + this.min) * 0.5;
	}
	set centre(value: number)
	{
		if (this.centre == value) return;
		this.Set(this.min + value - this.centre, this.max + value - this.centre);
	}

	/**
	 * Get/Set whether scrolling on this axis is allowed
	 */
	get allow_scroll(): boolean
	{
		return this._allow_scroll && !this.lock_range;
	}
	set allow_scroll(value: boolean)
	{
		this._allow_scroll = value;
	}
	private _allow_scroll: boolean;

	/**
	 * Get/Set whether zooming on this axis is allowed
	 */
	get allow_zoom(): boolean
	{
		return this._allow_zoom && !this.lock_range;
	}
	set allow_zoom(value: boolean)
	{
		this._allow_zoom = value;
	}
	private _allow_zoom: boolean;

	/**
	 * Get/Set whether the range can be changed by user input
	 */
	get lock_range(): boolean
	{
		return this._lock_range;
	}
	set lock_range(value: boolean)
	{
		this._lock_range = value;
	}
	private _lock_range: boolean;

	/**
	 * Return a string description of the axis state. Used to detect changes
	 */
	get hash(): string
	{
		return "" + this.min + this.max + this.label;
	}

	/**
	 * Set the range without risk of an assert if 'min' is greater than 'Max' or visa versa.
	 */
	Set(min: number, max: number): void
	{
		if (min >= max) throw new Error("Range must be positive and non-zero");
		let zoomed = !Math_.FEql(max - min, this.max - this.min);
		let scroll = !Math_.FEql((max + min)*0.5, (this.max + this.min)*0.5);

		this.min = min;
		this.max = max;

		if (zoomed)
		{
			this.OnZoomed.invoke(this, {});
		}
		if (scroll)
		{
			this.OnScroll.invoke(this, {});
		}
	}

	/**
	 * Scroll the axis by 'delta'
	 * @param delta The distance to shift the axis
	 */
	Shift(delta: number): void
	{
		if (!this.allow_scroll) return;
		this.centre += delta;
	}
	
	/**
	 * Default value to text conversion
	 * @param x The tick value on the axis
	 * @param step The current axis step size, from one tick to the next
	 */
	TickTextDefault(x: number): string
	{
		// This solves the rounding problem for values near zero when the axis span could be anything
		if (Math_.FEql(x / this.span, 0.0))
			return "0";

		// Use 5dp of precision
		let text = x.toPrecision(5);
		if (text.indexOf('.') != -1)
		{
			text = Util_.TrimRight(text, "0");
			if (text.slice(-1) == '.') text += '0';
		}
		return text;
	}

	/**
	 * Measure the tick text width
	 * @param gfx The canvas 2D context
	 */
	MeasureTickTextDefault(gfx: CanvasRenderingContext2D): Size
	{
		// Can't use 'GridLines' here because creates an infinite recursion.
		// Using TickText(Min/Max, 0.0) causes the axes to jump around.
		// The best option is to use one fixed length string.
		return Rdr_.Canvas.MeasureString(gfx, "-9.99999", this.options.tick_font);
	}

	/**
	 * Return the position of the grid lines for this axis
	 * @param axis_length The size of the chart area in the direction of this axis (i.e. dims.chart_area.w or dims.chart_area.h)
	 * @param pixels_per_tick The ideal number of pixels between each tick mark
	 */
	GridLines(axis_length: number, pixels_per_tick: number): { min: number, max: number, step: number }
	{
		let max_ticks = axis_length / pixels_per_tick;

		// Choose step sizes
		let step_base = Math.pow(10.0, Math.floor(Math_.Log10(this.span))), step = step_base;
		let quantisations = [0.05, 0.1, 0.2, 0.25, 0.5, 1.0, 2.0, 5.0, 10.0, 20.0, 50.0];
		for (let i = 0; i != quantisations.length; ++i)
		{
			let s = quantisations[i];
			if (s * this.span > max_ticks * step_base) continue;
			step = step_base / s;
		}

		let out =
			{
				step: step,
				min: (this.min - Math_.IEEERemainder(this.min, step)) - this.min,
				max: this.span * 1.0001,
			};
		if (out.min < 0.0) out.min += out.step;

		// Protect against increments smaller than can be represented
		if (out.min + out.step == out.min)
			out.step = (out.max - out.min) * 0.01;

		// Protect against too many ticks along the axis
		if (out.max - out.min > out.step*100)
			out.step = (out.max - out.min) * 0.01;

		return out;
	}

	/**
	 * Return a renderer instance for the grid lines for this axis.
	 */
	GridLineGfx(): Rdr_.Instance.IInstance
	{
		// If there is no cached grid line graphics, generate them
		if (this._geom_lines == null)
		{
			let dims = this.chart.dimensions;
			let rdr = this.chart.gfx3d;
			let webgl = rdr.webgl;

			// Create a model for the grid lines
			// Need to allow for one step in either direction because we only create the grid lines
			// model when scaling and we can translate by a max of one step in either direction.
			let axis_length =
				this === this.chart.xaxis ? dims.chart_area.w :
				this === this.chart.yaxis ? dims.chart_area.h :
				null;
			let gl = this.GridLines(<number>axis_length, this.options.pixels_per_tick);
			let num_lines = Math.floor(2 + (gl.max - gl.min) / gl.step);

			// Create the grid lines at the origin, they get positioned as the camera moves
			let verts = new Float32Array(num_lines * 2 * 3);
			let indices = new Uint16Array(num_lines * 2);
			let nuggets = [];
			let name = "";
			let v = 0;
			let i = 0;

			// Grid verts
			if (this === this.chart.xaxis)
			{
				name = "x_axis_grid";
				let x = 0, y0 = 0, y1 = this.chart.yaxis.span;
				for (let l = 0; l != num_lines; ++l)
				{
					verts[v++] = x;
					verts[v++] = y0;
					verts[v++] = 0;

					verts[v++] = x;
					verts[v++] = y1;
					verts[v++] = 0;

					x += gl.step;
				}
			}
			if (this === this.chart.yaxis)
			{
				name = "y_axis_grid";
				let y = 0, x0 = 0, x1 = this.chart.xaxis.span;
				for (let l = 0; l != num_lines; ++l)
				{
					verts[v++] = x0;
					verts[v++] = y;
					verts[v++] = 0;

					verts[v++] = x1;
					verts[v++] = y;
					verts[v++] = 0;

					y += gl.step;
				}
			}

			// Grid indices
			for (let l = 0; l != num_lines; ++l)
			{
				indices[i] = i++;
				indices[i] = i++;
			}

			// Grid nugget
			let nugget:Rdr_.Model.INugget = { topo: webgl.LINES, shader: rdr.shader_programs.forward, tint: Util_.ColourToV4(this.chart.options.grid_colour) };
			nuggets.push(nugget);

			// Create the model
			let model = Rdr_.Model.CreateRaw(rdr, verts, null, null, null, indices, nuggets);

			// Create the instance
			this._geom_lines = Rdr_.Instance.Create(name, model, M4x4_.create(), Rdr_.EFlags.SceneBoundsExclude|Rdr_.EFlags.NoZWrite);
		}

		// Return the graphics model
		return this._geom_lines;
	}
	private _geom_lines: Rdr_.Instance.IInstance | null;

	/**
	 * Invalidate the graphics, causing them to be recreated next time they're needed
	 */
	_InvalidateGfx(): void
	{
		this._geom_lines = null;
	}
}

/** Chart navigation */
export class Navigation
{
	constructor(chart: Chart)
	{
		this.chart = chart;
		this.active = null;
		this.pending = [];

		// Hook up mouse event handlers
		chart.canvas2d.onpointerdown = function(ev) { chart.navigation.OnMouseDown(ev); }
		chart.canvas2d.onpointermove = function(ev) { chart.navigation.OnMouseMove(ev); };
		chart.canvas2d.onpointerup   = function(ev) { chart.navigation.OnMouseUp(ev); };
		chart.canvas2d.onmousewheel  = function(ev) { chart.navigation.OnMouseWheel(ev); };
		chart.canvas2d.oncontextmenu = function(ev) { ev.preventDefault(); };
	}

	/** The owning chart */
	public chart: Chart;

	/** The currently active mouse operation */
	public active: MouseOp | null;

	/** The pending mouse operations (per mouse button) */
	public pending: (MouseOp|null)[];

	/**
	 * Start a mouse operation associated with the given mouse button index
	 * @param btn 
	 */
	BeginOp(btn: EButton): void
	{
		this.active = this.pending[btn];
		this.pending[btn] = null;
	}

	/**
	 * End a mouse operation for the given mouse button index
	 * @param btn 
	 */
	EndOp(btn: EButton): void
	{
		this.active = null;
		let pending = this.pending[btn];
		if (pending && !pending.start_on_mouse_down)
			this.BeginOp(btn);
	}

	/**
	 * Handle mouse down on the chart
	 * @param ev 
	 */
	OnMouseDown(ev: PointerEvent): void
	{
		// If a mouse op is already active, ignore mouse down
		if (this.active != null)
			return;

		// Look for the mouse op to perform
		if (this.pending[ev.button] == null)
		{
			switch (ev.button)
			{
			default: return;
			case EButton.Left: this.pending[ev.button] = new MouseOpDefaultLButton(this.chart); break;
			//case MouseButtons.Middle: MouseOperations.SetPending(e.Button, new MouseOpDefaultMButton(this)); break;
			case EButton.Right: this.pending[ev.button] = new MouseOpDefaultRButton(this.chart); break;
			}
		}

		// Start the next mouse op
		this.BeginOp(ev.button);
		
		// Get the mouse op, save mouse location and hit test data, then call op.MouseDown()
		let op = <MouseOp | null>this.active;
		if (op && !op.cancelled)
		{
			op.btn_down    = true;
			op.grab_client = [ev.offsetX, ev.offsetY];
			op.grab_chart  = this.chart.ClientToChartPoint(op.grab_client);
			op.hit_result  = this.chart.HitTestCS(op.grab_client, {shift: ev.shiftKey, alt: ev.altKey, ctrl: ev.ctrlKey});
			op.MouseDown(ev);
			this.chart.canvas2d.setPointerCapture(ev.pointerId);
		}
	}
	
	/**
	 * Handle mouse move on the chart
	 * @param {MouseEvent} ev 
	 */
	OnMouseMove(ev: PointerEvent): void
	{
		// Look for the mouse op to perform
		let op = this.active;
		if (op != null)
		{
			if (!op.cancelled)
				op.MouseMove(ev);
		}
		// Otherwise, provide mouse hover detection
		else
		{
			//let client_point = [ev.offsetX, ev.offsetY];
			//let hit_result = this.chart.HitTestCS(client_point, {shift: ev.shiftKey, alt: ev.altKey, ctrl: ev.ctrlKey}, null);
			/*
				let hovered = hit_result.hits.Select(x => x.Element).ToHashSet();

				// Remove elements that are no longer hovered
				// and remove existing hovered's from the set.
				for (int i = Hovered.Count; i-- != 0;)
				{
					if (hovered.Contains(Hovered[i]))
						hovered.Remove(Hovered[i]);
					else
						Hovered.RemoveAt(i);
				}
				
				// Add elements that are now hovered
				Hovered.AddRange(hovered);
			*/
		}
	}

	/**
	 * Handle mouse up on the chart
	 * @param {MouseEvent} ev 
	 */
	OnMouseUp(ev: PointerEvent): void
	{
		try {
			this.chart.canvas2d.releasePointerCapture(ev.pointerId);

			// Look for the mouse op to perform
			let op = this.active;
			if (op != null && !op.cancelled)
				op.MouseUp(ev);
		}
		finally {
			this.EndOp(ev.button);
		}
	}

	/**
	 * Handle mouse wheel changes on the chart
	 * @param {MouseWheelEvent} ev 
	 */
	OnMouseWheel(ev: WheelEvent): void
	{
		let client_pt = [ev.offsetX, ev.offsetY];
		let chart_pt = this.chart.ClientToChartPoint(client_pt);
		let dims = this.chart.dimensions;
		let perp_z = this.chart.options.perpendicular_z_translation != ev.altKey;
		if (Rect_.Contains(dims.chart_area, client_pt))
		{
			// If there is a mouse op in progress, ignore the wheel
			let op = this.active;
			if (op == null || op.cancelled)
			{
				// Set a scaling factor from the mouse wheel clicks
				let scale = 1.0 / 120.0;
				let delta = Math_.Clamp(ev.wheelDelta * scale, -100, 100);

				// Convert 'client_pt' to normalised camera space
				let nss_point = Rect_.NormalisePoint(dims.chart_area, client_pt, +1, -1);

				// Translate the camera along a ray through 'point'
				this.chart.camera.MouseControlZ(nss_point, delta, !perp_z);
				this.chart.Invalidate();
			}
		}
		else if (this.chart.options.show_axes)
		{
			let scale = 0.001;
			let delta = Math_.Clamp(ev.wheelDelta * scale, -0.999, 0.999);

			let xaxis = this.chart.xaxis;
			let yaxis = this.chart.yaxis;
			let chg = false;

			// Change the aspect ratio by zooming on the XAxis
			if (Rect_.Contains(dims.xaxis_area, client_pt) && !xaxis.lock_range)
			{
				if (ev.altKey && xaxis.allow_scroll)
				{
					xaxis.Shift(xaxis.span * delta);
					chg = true;
				}
				if (!ev.altKey && xaxis.allow_zoom)
				{
					let x = perp_z ? xaxis.centre : chart_pt[0];
					let left = (xaxis.min - x) * (1 - delta);
					let rite = (xaxis.max - x) * (1 - delta);
					xaxis.Set(chart_pt[0] + left, chart_pt[0] + rite);
					if (this.chart.lock_aspect)
						yaxis.span *= (1 - delta);

					chg = true;
				}
			}

			// Check the aspect ratio by zooming on the YAxis
			if (Rect_.Contains(dims.yaxis_area, client_pt) && !yaxis.lock_range)
			{
				if (ev.altKey && yaxis.allow_scroll)
				{
					yaxis.Shift(yaxis.span * delta);
					chg = true;
				}
				if (!ev.altKey && yaxis.allow_zoom)
				{
					let y = perp_z ? yaxis.centre : chart_pt[1];
					let left = (yaxis.min - y) * (1 - delta);
					let rite = (yaxis.max - y) * (1 - delta);
					yaxis.Set(chart_pt[1] + left, chart_pt[1] + rite);
					if (this.chart.lock_aspect)
						xaxis.span *= (1 - delta);

					chg = true;
				}
			}

			// Set the camera position from the Axis ranges
			if (chg)
			{
				this.chart.SetCameraFromRange();
				this.chart.Invalidate();
			}
		}
	}

	/*
		protected override void OnKeyDown(KeyEventArgs e)
		{
			SetCursor();

			let op = MouseOperations.Active;
			if (op != null && !e.Handled)
				op.OnKeyDown(e);

			// Allow derived classes to handle the key
			base.OnKeyDown(e);

			// If the current mouse operation doesn't use the key,
			// see if it's a default keyboard shortcut.
			if (!e.Handled && DefaultKeyboardShortcuts)
				TranslateKey(e);

		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			SetCursor();

			let op = MouseOperations.Active;
			if (op != null && !e.Handled)
				op.OnKeyUp(e);

			base.OnKeyUp(e);
		}
	*/
};

/** Mouse operation base class */
export abstract class MouseOp
{
	constructor(chart: Chart)
	{
		this.chart = chart;
		this.start_on_mouse_down = true;
		this.cancelled = false;
		this.btn_down = false;
		this.grab_client = Vec2_.Zero;
		this.grab_chart = Vec2_.Zero;
		this.hit_result = new HitTestResult(EZone.None, Vec2_.Zero, Vec2_.Zero, {shift:false, ctrl:false, alt:false}, [], chart.camera);
		this._is_click = true;
	}

	/** The owning chart */
	public chart: Chart;

	/** True if the mouse operation starts on the next mouse down event */
	public start_on_mouse_down: boolean;

	/** True if the mouse operation has been cancelled, and we're waiting for mouse up before starting the next operation */
	public cancelled: boolean;

	/** True if the mouse button is currently down */
	public btn_down: boolean;

	/** Where on the chart the mouse down event occurred (in client space) */
	public grab_client: Vec2;

	/** Where on the chart the mouse down event occurred (in chart space) */
	public grab_chart: Vec2;

	/** The hit information at the time of the mouse down */
	public hit_result: HitTestResult;

	/**
	 * Test a mouse location as being a click (as opposed to a drag)
	 * @param point The mouse pointer location (client space)
	 * @returns True if the distance between 'location' and mouse down should be treated as a click
	 */
	IsClick(point: Vec2)
	{
		if (!this._is_click) return false;
		let diff = [point[0] - this.grab_client[0], point[1] - this.grab_client[1]];
		return this._is_click = Vec2_.LengthSq(diff) < Math_.Sqr(this.chart.options.min_drag_pixel_distance);
	}
	private _is_click: boolean;

	/** Mouse event handler methods */
	abstract MouseDown(ev: PointerEvent): void;
	abstract MouseMove(ev: PointerEvent): void;
	abstract MouseUp(ev: PointerEvent): void;
}
export class MouseOpDefaultLButton extends MouseOp
{
	constructor(chart: Chart)
	{
		super(chart);
		this.hit_selected = null;
		this._selection_graphic_added = false;
	}

	/** The element that is selected by the mouse click (null if no selection) */
	public hit_selected: HitTestResult.Hit | null;

	MouseDown(ev: PointerEvent): void
	{
		// Look for a selected object that the mouse operation starts on
		for (let i = 0; i != this.hit_result.hits.length; ++i)
		{
			if (!this.hit_result.hits[i].element.selected) continue;
			this.hit_selected = this.hit_result.hits[i];
			break;
		}

		// Record the drag start positions for selected objects
		for (let i = 0; i != this.chart.selected.length; ++i)
		{
			let elem:IElement = this.chart.selected[i];
			elem.drag_start_position = elem.pos;
		}

		// For 3D scenes, left mouse rotates if mouse down is within the chart bounds
		if (this.chart.options.navigation_mode == ENavMode.Scene3D && this.hit_result.zone == EZone.Chart && ev.button == EButton.Left)
			this.chart.camera.MouseControl(this.grab_client, Rdr_.Camera.ENavOp.Rotate, true);
	}
	MouseMove(ev: PointerEvent): void
	{
		let client_point = [ev.offsetX, ev.offsetY];

		// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
		if (this.IsClick(client_point) && !this._selection_graphic_added)
			return;

		let drag_selected = this.chart.options.allow_editing && this.hit_selected != null;
		if (drag_selected)
		{
			// If the drag operation started on a selected element then drag the
			// selected elements within the diagram.
			let delta = Vec2_.Sub(this.chart.ClientToChartPoint(client_point), this.grab_chart);
			this.chart._DragSelected(delta, false);
		}
		else if (this.chart.options.navigation_mode == ENavMode.Chart2D)
		{
			if (this.chart.options.area_select_mode != EAreaSelectMode.Disabled)
			{
				// Otherwise change the selection area
				if (!this._selection_graphic_added)
				{
					this.chart.instances.push(this.chart._tools.area_select);
					this._selection_graphic_added = true;
				}

				// Position the selection graphic
				let area = Rect_.bound(this.grab_chart, this.chart.ClientToChartPoint(client_point));
				let scale = Vec4_.create(area.centre[0], area.centre[1], 0, 1);
				this.chart._tools.area_select.o2w = M4x4_.Scale(area.w, area.h, 1, scale);
			}
		}
		else if (this.chart.options.navigation_mode == ENavMode.Scene3D)
		{
			this.chart.camera.MouseControl(client_point, Rdr_.Camera.ENavOp.Rotate, false);
		}

		// Render immediately, for smoother navigation
		this.chart.Render();
	}
	MouseUp(ev: PointerEvent): void
	{
		let client_point = [ev.offsetX, ev.offsetY];
		let modifier_keys = { shift: ev.shiftKey, alt: ev.altKey, ctrl: ev.ctrlKey };

		// If this is a single click...
		if (this.IsClick(client_point))
		{
			// Pass the click event out to users first
			let args: IChartClickedArgs = { hit_result: this.hit_result, handled: false };
			this.chart.OnChartClicked.invoke(this.chart, args);

			// If a selected element was hit on mouse down, see if it handles the click
			if (!args.handled && this.hit_selected != null)
			{
				let elem = this.hit_selected.element;
				elem.OnClicked.invoke(elem, args);
			}

			// If no selected element was hit, try hovered elements
			if (!args.handled && this.hit_result.hits.length != 0)
			{
				for (let i = 0; i != this.hit_result.hits.length && !args.handled; ++i)
				{
					let elem = this.hit_result.hits[i].element;
					elem.OnClicked.invoke(elem, args);
				}
			}

			// If the click is still unhandled, use the click to try to select something (if within the chart)
			if (!args.handled && (this.hit_result.zone & EZone.Chart))
			{
				let area = Rect_.create(this.grab_chart[0], this.grab_chart[1], 0, 0);
				this.chart.SelectElements(area, modifier_keys);
			}
		}
		// Otherwise this is a drag action
		else
		{
			// If an element was selected, drag it around
			if (this.hit_selected != null && this.chart.options.allow_editing)
			{
				let delta = Vec2_.Sub(this.chart.ClientToChartPoint(client_point), this.grab_chart);
				this.chart._DragSelected(delta, true);
			}
			else if (this.chart.options.navigation_mode == ENavMode.Chart2D)
			{
				// Otherwise create an area selection if the click started within the chart
				if ((this.hit_result.zone & EZone.Chart) && this.chart.options.area_select_mode != EAreaSelectMode.Disabled)
				{
					let chart_point = this.chart.ClientToChartPoint(client_point);
					let area = BBox_.bound(
						Vec4_.create(this.grab_chart[0], this.grab_chart[1], 0, 1),
						Vec4_.create(chart_point[0], chart_point[1], 0, 1));

					// Handle area selection on the chart
					let args = { selection_area: area, modifier_keys: modifier_keys, handled: false };
					this.chart._HandleChartAreaSelect(args);
				}
			}
			else if (this.chart.options.navigation_mode == ENavMode.Scene3D)
			{
				// For 3D scenes, left mouse rotates
				this.chart.camera.MouseControl(client_point, Rdr_.Camera.ENavOp.Rotate, true);
			}
		}

		// Remove the area selection graphic
		if (this._selection_graphic_added)
		{
			let idx = this.chart.instances.indexOf(this.chart._tools.area_select);
			if (idx != -1) this.chart.instances.splice(idx, 1);
		}

		//this.chart.Cursor = Cursors.Default;
		this.chart.Invalidate();
	}
	private _selection_graphic_added: boolean;
}
export class MouseOpDefaultRButton extends MouseOp
{
	constructor(chart: Chart)
	{
		super(chart);
	}
	MouseDown(): void
	{
		// Convert 'grab_client' to normalised camera space
		let nss_point = Rect_.NormalisePoint(this.chart.dimensions.chart_area, this.grab_client, +1, -1);

		// Right mouse translates for 2D and 3D scene
		this.chart.camera.MouseControl(nss_point, Rdr_.Camera.ENavOp.Translate, true);
	}
	MouseMove(ev: PointerEvent): void
	{
		let client_point = [ev.offsetX, ev.offsetY];

		// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
		if (this.IsClick(client_point))
			return;

		// Change the cursor once dragging
		//this.chart.Cursor = Cursors.SizeAll;

		// Limit the drag direction
		let drop_loc = Rect_.NormalisePoint(this.chart.dimensions.chart_area, client_point, +1, -1);
		let grab_loc = Rect_.NormalisePoint(this.chart.dimensions.chart_area, this.grab_client, +1, -1);
		if ((this.hit_result.zone & EZone.YAxis) || this.chart.xaxis.lock_range) drop_loc[0] = grab_loc[0];
		if ((this.hit_result.zone & EZone.XAxis) || this.chart.yaxis.lock_range) drop_loc[1] = grab_loc[1];

		this.chart.camera.MouseControl(drop_loc, Rdr_.Camera.ENavOp.Translate, false);
		this.chart.SetRangeFromCamera();
		this.chart.Invalidate();
	}
	MouseUp(ev: PointerEvent): void
	{
		let client_point = [ev.offsetX, ev.offsetY];

		//this.chart.Cursor = Cursors.Default;

		// If we haven't dragged, treat it as a click instead
		if (this.IsClick(client_point))
		{
			var args = {hit_result:this.hit_result, handled:false};
			//m_chart.OnChartClicked(args);

			if (!args.handled)
			{
				// Show the context menu on right click
				if (ev.button == EButton.Right)
				{
					//if (this.hit_result.Zone & EZone.Chart)
					//	this.chart.ShowContextMenu(client_point, this.hit_result);
					//else if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.XAxis))
					//	m_chart.XAxis.ShowContextMenu(args.Location, args.HitResult);
					//else if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.YAxis))
					//	m_chart.YAxis.ShowContextMenu(args.Location, args.HitResult);
				}
			}
			this.chart.Invalidate();
		}
		else
		{
			// Limit the drag direction
			let drop_loc = Rect_.NormalisePoint(this.chart.dimensions.chart_area, client_point, +1, -1);
			let grab_loc = Rect_.NormalisePoint(this.chart.dimensions.chart_area, this.grab_client, +1, -1);
			if ((this.hit_result.zone & EZone.YAxis) || this.chart.xaxis.lock_range) drop_loc[0] = grab_loc[0];
			if ((this.hit_result.zone & EZone.XAxis) || this.chart.yaxis.lock_range) drop_loc[1] = grab_loc[1];
	
			this.chart.camera.MouseControl(drop_loc, Rdr_.Camera.ENavOp.None, true);
			this.chart.SetRangeFromCamera();
			this.chart.Invalidate();
		}
	}
}

/** Chart utility graphics */
export class Tools
{
	constructor(chart: Chart)
	{
		this.chart = chart;
		this._area_select = null;
	}

	/** The owning chart */
	public chart: Chart;

	/** Get the area selection graphics */
	get area_select(): Rdr_.Instance.IInstance
	{
		if (!this._area_select)
		{
			let rdr = this.chart.gfx3d;
			let model = Rdr_.Model.Create(rdr,
				[// Verts
					{ pos: [+0.0, +0.0, +0.0] },
					{ pos: [+0.0, +1.0, +0.0] },
					{ pos: [+1.0, +1.0, +0.0] },
					{ pos: [+1.0, +0.0, +0.0] },
				],
				[// Indices
					0, 1, 2,
					0, 2, 3,
				],
				[// Nuggets
					{ topo: rdr.webgl.TRIANGLES, shader: rdr.shader_programs.forward, tint: Util_.ColourToV4(this.chart.options.selection_colour) }
				]);
			this._area_select = Rdr_.Instance.Create("area_select", model, M4x4_.create(), Rdr_.EFlags.SceneBoundsExclude | Rdr_.EFlags.NoZWrite | Rdr_.EFlags.NoZTest);
		}
		return this._area_select;
	}
	private _area_select: Rdr_.Instance.IInstance | null;
}

/** Options for the behaviour of the chart */
export class ChartOptions
{
	constructor()
	{
		this.bk_colour = '#F0F0F0';
		this.chart_bk_colour = '#FFFFFF';
		this.axis_colour = 'black';
		this.grid_colour = '#EEEEEE';
		this.title_colour = 'black';
		this.title_font = "12pt tahoma bold";
		this.note_font = "8pt tahoma";
		this.title_transform = { m00: 1, m01: 0, m10: 0, m11: 1, dx: 0, dy: 0 };
		this.title_padding = 3;
		this.margin = { left: 3, top: 3, right: 3, bottom: 3 };
		this.axis_thickness = 0;
		this.navigation_mode = ENavMode.Chart2D;
		this.show_grid_lines = true;
		this.show_axes = true;
		this.lock_aspect = null;
		this.perpendicular_z_translation = false;
		this.grid_z_offset = 0.001;
		this.min_drag_pixel_distance = 5;
		this.min_selection_distance = 10;
		this.allow_editing = true;
		this.allow_selection = true;
		this.area_select_mode = EAreaSelectMode.Zoom;
		this.selection_colour = "#80808080";
		//AntiAliasing              = true;
		//FillMode                  = View3d.EFillMode.Solid;
		//CullMode                  = View3d.ECullMode.Back;
		//Orthographic              = false;
		//MinSelectionDistance      = 10f;
		this.reset_forward = Vec4_.Neg(Vec4_.ZAxis);
		this.reset_up = Vec4_.clone(Vec4_.YAxis);
		this.xaxis = new AxisOptions();
		this.yaxis = new AxisOptions();
	}

	bk_colour: string;
	chart_bk_colour: string;
	axis_colour: string;
	grid_colour: string;
	title_colour: string;
	title_font: string;
	note_font: string;
	title_transform: { m00: number, m01: number, m10: number, m11: number, dx: number, dy: number };
	title_padding: number;
	margin: { left: number, top: number, right: number, bottom: number };
	axis_thickness: number;
	navigation_mode: ENavMode;
	show_grid_lines: boolean;
	show_axes: boolean;
	lock_aspect: number | null;
	perpendicular_z_translation: boolean;
	grid_z_offset: number;
	min_drag_pixel_distance: number;
	min_selection_distance: number;
	allow_editing: boolean;
	allow_selection: boolean;
	area_select_mode: EAreaSelectMode;
	selection_colour: string;
	//AntiAliasing              = true;
	//FillMode                  = View3d.EFillMode.Solid;
	//CullMode                  = View3d.ECullMode.Back;
	//Orthographic              = false;
	reset_forward: Vec4;
	reset_up: Vec4;
	xaxis = new AxisOptions();
	yaxis = new AxisOptions();
}

/** Options for an axis */
export class AxisOptions
{
	constructor()
	{
		this.label_colour = 'black';
		this.tick_colour = 'black';
		this.label_font = "10pt tahoma";
		this.tick_font = "8pt tahoma";
		this.draw_tick_labels = true;
		this.draw_tick_marks = true;
		this.tick_length = 5;
		this.min_tick_size = 30;
		this.pixels_per_tick = 30;
		this.label_transform = { m00: 1, m01: 0, m10: 0, m11: 1, dx: 0, dy: 0 };
	}

	/** The colour of the label text */
	label_colour: string;

	/** The colour of the tick marks */
	tick_colour: string;

	/** The font for the label */
	label_font: string;

	/** The font for the tick marks */
	tick_font: string;

	/** True to draw the tick labels */
	draw_tick_labels: boolean;

	/** True to draw the tick marks */
	draw_tick_marks: boolean;

	/** The length of the tick marks */
	tick_length: number;

	/** */
	min_tick_size: number;

	/** The approximate number of pixels between each tick mark */
	pixels_per_tick: number;

	/** The transform for positioning the axis label */
	label_transform: Rdr_.Canvas.IMatrix2D;
}

/** Event args */
export interface IChartMovedArgs
{
	move_type: EMove;
}
export interface IChartClickedArgs
{
	hit_result: HitTestResult;
	handled: boolean;
}
export interface IChartAreaSelectArgs
{
	/** The area (actually volume if you include Z) of the selection */
	selection_area: BBox;

	/** The state of the modifier keys */
	modifier_keys: IModifierKeys;

	/** Set to true to suppress default chart click behaviour */
	handled: boolean;
}
export interface IRenderingArgs
{
}
export interface IAutoRangeArgs
{
	who: string;
	view_bbox: BBox;
	dims: ChartDimensions;
	handled: boolean;
}
export interface IScrollArgs
{
}
export interface IZoomedArgs
{
}