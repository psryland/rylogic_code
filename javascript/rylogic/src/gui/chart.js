/**
 * @module Chart
 */

import * as Maths from "../maths/maths";
import * as m4x4 from "../maths/m4x4";
import * as v4 from "../maths/v4";
import * as v2 from "../maths/v2";
import * as Rect from "../maths/rect";
import * as BBox from "../maths/bbox";
import * as Rdr from "../renderer/renderer";
import * as Util from "../utility/util";

/**
 * Enumeration of areas on the chart
 */
export var EZone = Object.freeze(
{
	None: 0,
	Chart: 1 << 0,
	XAxis: 1 << 1,
	YAxis: 1 << 2,
	Title: 1 << 3,
});

/**
 * Chart movement types
 */
export var EMove = Object.freeze(
{
	None: 0,
	XZoomed: 1 << 0,
	YZoomed: 1 << 1,
	Zoomed: 0x3 << 0,
	XScroll: 1 << 2,
	YScroll: 1 << 3,
	Scroll: 0x3 << 2,
});

/**
 * Mouse button enumeration
 */
var EButton = Object.freeze(
{
	Left: 0,
	Middle: 1,
	Right: 2,
	Back: 3,
	Forward: 4,
});

/**
 * Chart navigation modes
 */
var ENavMode = Object.freeze(
{
	Chart2D: 0,
	Scene3D: 1,
});

/**
 * Area selection modes
 */
var EAreaSelectMode = Object.freeze(
{
	Disabled: 0,
	SelectElements: 1,
	Zoom: 2,
});


export class Chart
{
	/**
	 * Create a chart in the given canvas element
	 * @param {HTMLCanvasElement} canvas the HTML element to turn into a chart
	 * @returns {Chart}
	 */
	constructor(canvas)
	{
		let This = this;

		// Hold a reference to the element we're drawing on
		// Create a duplicate canvas to draw 2d stuff on, and position it
		// at the same location, and over the top of, 'canvas'.
		// 'canvas2d' is an overlay over 'canvas3d'.
		this.canvas3d = canvas;
		this.canvas2d = canvas.cloneNode(false);
		this.canvas2d.style.position = "absolute";
		this.canvas2d.style.border = "none";//this.canvas3d.style.border;
		this.canvas2d.style.left = this.canvas3d.style.left;
		this.canvas2d.style.top = this.canvas3d.style.top;
		canvas.parentNode.insertBefore(this.canvas2d, this.canvas3d);
	
		// Get the 2D drawing interface
		this.m_gfx = this.canvas2d.getContext("2d");
	
		// Initialise WebGL
		this.m_rdr = Rdr.Create(this.canvas3d);
		this.m_rdr.back_colour = v4.make(1,1,1,1);
		this.m_redraw_pending = false;
	
		// Create an area for storing cached data
		this.m_cache = new function()
		{
			this.chart_frame = null;
			this.xaxis_hash = null;
			this.yaxis_hash = null;
			this.invalidate = function(){ this.chart_frame = null; };
		};

		// The manager of chart utility graphics
		this.m_tools = new Tools(This);

		// The collection of instances to render on the chart
		this.instances = [];
	
		// Initialise the chart camera
		this.camera = Rdr.Camera.Create(Maths.TauBy8, this.m_rdr.AspectRatio);
		this.camera.orthographic = true;
		this.camera.LookAt(v4.make(0, 0, 10, 1), v4.Origin, v4.YAxis);
		this.camera.KeyDown = function(nav_key)
		{
			return false;
		};
	
		// The light source the illuminates the chart
		this.light = Rdr.Light.Create(Rdr.ELight.Directional, v4.Origin, v4.Neg(v4.ZAxis), null, [0.5,0.5,0.5], null, null);
	
		// Navigation
		this.navigation = new Navigation(This);
	
		// Chart options
		this.options = new ChartOptions();
		function ChartOptions()
		{
			this.bk_colour = '#F0F0F0';
			this.chart_bk_colour = '#FFFFFF';
			this.axis_colour = 'black';
			this.grid_colour = '#EEEEEE';
			this.title_colour = 'black';
			this.title_font = "12pt tahoma bold";
			this.note_font = "8pt tahoma";
			this.title_transform = {m00: 1, m01: 0, m10: 0, m11: 1, dx: 0, dy: 0};
			this.title_padding = 3;
			this.margin = {left: 3, top: 3, right: 3, bottom: 3};
			this.axis_thickness = 0;
			this.navigation_mode = ENavMode.Chart2D;
			this.show_grid_lines = true;
			this.show_axes = true;
			this.lock_aspect = null;
			this.perpendicular_z_translation = false;
			this.grid_z_offset = 0.001;
			this.min_drag_pixel_distance = 5;
			this.allow_editing = true;
			this.area_select_mode = EAreaSelectMode.Zoom;
			this.selection_colour = "#80808080";
			//AntiAliasing              = true;
			//FillMode                  = View3d.EFillMode.Solid;
			//CullMode                  = View3d.ECullMode.Back;
			//Orthographic              = false;
			//MinSelectionDistance      = 10f;
			this.reset_forward = v4.Neg(v4.ZAxis);
			this.reset_up = v4.clone(v4.YAxis);
			this.xaxis = new AxisOptions();
			this.yaxis = new AxisOptions();
		};
		function AxisOptions()
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
			this.label_transform = {m00: 1, m01: 0, m10: 0, m11: 1, dx: 0, dy: 0};
		};

		// Events
		this.OnChartMoved = new Util.MulticastDelegate();
		this.OnChartClicked = new Util.MulticastDelegate();
		this.OnRendering = new Util.MulticastDelegate();
		this.OnAutoRange = new Util.MulticastDelegate();
	
		// Chart title
		this.title = "";
	
		// The data series
		this.data = [];
	
		// Initialise Axis data
		this.xaxis = new Axis(this, this.options.xaxis, "");
		this.yaxis = new Axis(this, this.options.yaxis, "");
		this.xaxis.OnScroll.sub((s,a) => This._RaiseChartMoved(EMove.XScroll));
		this.xaxis.OnZoomed.sub((s,a) => This._RaiseChartMoved(EMove.XZoomed));
		this.yaxis.OnScroll.sub((s,a) => This._RaiseChartMoved(EMove.YScroll));
		this.yaxis.OnZoomed.sub((s,a) => This._RaiseChartMoved(EMove.YZoomed));

		// Layout data for the chart
		this.dimensions = this.ChartDimensions();

		window.requestAnimationFrame(function loop()
		{
			if (This.m_redraw_pending) This.Render();
			window.requestAnimationFrame(loop);
		});
	}

	/**
	 * Get the renderer instance created by this chart
	 */
	get rdr()
	{
		return this.m_rdr;
	}

	/**
	 * Get/Set whether the aspect ratio is locked to the current value
	 * @returns {boolean}
	 */
	get lock_aspect()
	{
		return this.options.lock_aspect != null;
	}
	set lock_aspect(value)
	{
		if (this.lock_aspect == value) return;
		if (value)
			this.options.lock_aspect = (this.xaxis.span * this.dimensions.h) / (this.yaxis.span * this.dimensions.w);
		else
			this.options.lock_aspect = null;
	}

	/**
	 * Render the chart
	 * @param {Chart} chart The chart to be rendered
	 */
	Render()
	{
		// Clear the redraw pending flag
		this.m_redraw_pending = false;

		// Navigation moves the camera and axis ranges are derived from the camera position/view
		this.SetRangeFromCamera();

		// Calculate the chart dimensions
		this.dimensions = this.ChartDimensions();

		// Draw the chart frame
		this.RenderChartFrame(this.dimensions);

		// Draw the chart content
		this.RenderChartContent(this.dimensions);
	}

	/**
	 * Draw the chart frame
	 * @param {ChartDimensions} dims The chart size data returned from ChartDimensions
	 */
	RenderChartFrame(dims)
	{
		// Drawing the frame can take a while. Cache the frame image so if the chart
		// is not moved, we can just blit the cached copy to the screen.
		let repaint = true;
		for (;;)
		{
			// Control size changed?
			if (this.m_cache.chart_frame == null ||
				this.m_cache.chart_frame.width != dims.area.w ||
				this.m_cache.chart_frame.height != dims.area.h)
			{
				// Create a double buffer
				this.m_cache.chart_frame = this.canvas2d.cloneNode(false);
				break;
			}

			// Axes zoomed/scrolled?
			if (this.xaxis.hash != this.m_cache.xaxis_hash)
				break;
			if (this.yaxis.hash != this.m_cache.yaxis_hash)
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
			let bmp = this.m_cache.chart_frame.getContext("2d");
			let buffer = this.m_cache.chart_frame;
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
				let x = dims.area.l + (dims.area.w - r.width) * 0.5;
				let y = dims.area.t + (opts.margin.top + opts.title_padding + r.height);
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
				let x = dims.area.l + (dims.area.w - r.width) * 0.5;
				let y = dims.area.b - opts.margin.bottom;// - r.height;
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
				let x = dims.area.l + opts.margin.left + r.height;
				let y = dims.area.t + (dims.area.h - r.width) * 0.5;
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

					let fh = Util.FontHeight(bmp, bmp.font);
					let Y = (dims.chart_area.b + opts.xaxis.tick_length + 1);
					let gl = xaxis.GridLines(dims.chart_area.w, opts.xaxis.pixels_per_tick);
					for (let x = gl.min; x < gl.max; x += gl.step)
					{
						let X = dims.chart_area.l + x*dims.chart_area.w/xaxis.span;
						if (opts.xaxis.draw_tick_labels)
						{
							let s = xaxis.TickText(x + xaxis.min, gl.step).split('\n');
							for (let i = 0; i != s.length; ++i)
								bmp.fillText(s[i], X, Y+i*fh, dims.xtick_label_size.width);
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

					let fh = Util.FontHeight(bmp, bmp.font);
					let X = (dims.chart_area.l - opts.yaxis.tick_length - 1);
					let gl = yaxis.GridLines(dims.chart_area.h, opts.yaxis.pixels_per_tick);
					for (let y = gl.min; y < gl.max; y += gl.step)
					{
						let Y = dims.chart_area.b - y*dims.chart_area.h/yaxis.span;
						if (opts.yaxis.draw_tick_labels)
						{
							let s = yaxis.TickText(y + yaxis.min, gl.step).split('\n');
							for (let i = 0; i != s.length; ++i)
								bmp.fillText(s[i], X, Y+i*fh - dims.ytick_label_size.height*0.5, dims.ytick_label_size.width);
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
				this.m_cache.xaxis_hash = this.xaxis.hash;
				this.m_cache.yaxis_hash = this.yaxis.hash;
			}
			bmp.restore();
		}

		this.m_gfx.save();

		// Update the clipping region
		this.m_gfx.rect(dims.area.l, dims.area.t, dims.area.w, dims.area.h);
		this.m_gfx.rect(dims.chart_area.l, dims.chart_area.t, dims.chart_area.w, dims.chart_area.h);
		this.m_gfx.clip();

		// Blit the cached image to the screen
		this.m_gfx.imageSmoothingEnabled = false;
		this.m_gfx.drawImage(this.m_cache.chart_frame, dims.area.l, dims.area.t);
		this.m_gfx.restore();
	}

	/**
	 * Draw the chart content
	 * @param {ChartDimensions} dims The chart size data returned from ChartDimensions
	 */
	RenderChartContent(dims)
	{
		this.instances.length = 0;

		// Add axis graphics
		if (this.options.show_grid_lines)
		{
			// Position the grid lines so that they line up with the axis tick marks.
			// Grid lines are modelled from the bottom left corner
			let wh = this.camera.ViewArea();
			
			{// X axis
				let xlines = this.xaxis.GridLineGfx(this, dims);
				let gl = this.xaxis.GridLines(dims.chart_area.w, this.options.xaxis.pixels_per_tick);

				let pos = v4.make(wh[0]/2 - gl.min, wh[1]/2, this.options.grid_z_offset, 0);
				m4x4.MulMV(this.camera.c2w, pos, pos);
				v4.Sub(this.camera.focus_point, pos, pos);

				m4x4.clone(this.camera.c2w, xlines.o2w);
				m4x4.SetW(xlines.o2w, pos);
				this.instances.push(xlines);
			}
			{// Y axis
				let ylines = this.yaxis.GridLineGfx(this, dims);
				let gl = this.yaxis.GridLines(dims.chart_area.h, this.options.yaxis.pixels_per_tick);

				let pos = v4.make(wh[0]/2, wh[1]/2 - gl.min, this.options.grid_z_offset, 0);
				m4x4.MulMV(this.camera.c2w, pos, pos);
				v4.Sub(this.camera.focus_point, pos, pos);

				m4x4.clone(this.camera.c2w, ylines.o2w);
				m4x4.SetW(ylines.o2w, pos);
				this.instances.push(ylines);
			}
		}

		// Add chart elements

		// Add user graphics
		this.OnRendering.invoke(this, {});

		// Ensure the viewport matches the chart context area
		// Webgl viewports have 0,0 in the lower left, but 'dims.chart_area' has 0,0 at top left.
		this.m_rdr.viewport(dims.chart_area.l - dims.area.l, dims.area.b - dims.chart_area.b, dims.chart_area.w, dims.chart_area.h);
		
		// Render the chart
		Rdr.Render(this.m_rdr, this.instances, this.camera, this.light);
	}

	/**
	 * Invalidate cached data before a Render
	 */
	Invalidate()
	{
		this.m_cache.invalidate();
		if (this.m_redraw_pending)
			return;

		// Set a flag to indicate redraw pending
		this.m_redraw_pending = true;

	//	let This = this;
	//	setTimeout(function() { This.Render(); }, 10);
	}

	/**
	 * Set the axis range based on the position of the camera and the field of view.
	 */
	SetRangeFromCamera()
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
	SetCameraFromRange()
	{
		// Set the aspect ratio from the axis range and scene bounds
		this.camera.aspect = (this.xaxis.span / this.yaxis.span);

		// Find the world space position of the new focus point.
		// Move the focus point within the focus plane (parallel to the camera XY plane)
		// and adjust the focus distance so that the view has a width equal to XAxis.Span.
		// The camera aspect ratio should ensure the YAxis is correct.
		let c2w = this.camera.c2w;
		let focus = v4.AddN(
			v4.MulS(m4x4.GetX(c2w), this.xaxis.centre),
			v4.MulS(m4x4.GetY(c2w), this.yaxis.centre),
			v4.MulS(m4x4.GetZ(c2w), v4.Dot(v4.Sub(this.camera.focus_point, v4.Origin), m4x4.GetZ(c2w)))
			);

		// Move the camera in the camera Z axis direction so that the width at the focus dist
		// matches the XAxis range. Tan(FovX/2) = (XAxis.Span/2)/d
		let focus_dist = (this.xaxis.span / (2.0 * Math.tan(this.camera.fovX / 2.0)));
		let pos = v4.SetW1(v4.Add(focus, v4.MulS(m4x4.GetZ(c2w), focus_dist)))

		// Set the new camera position and focus distance
		this.camera.focus_dist = focus_dist;
		m4x4.SetW(c2w, pos);
		this.camera.Commit();
	}

	/**
	 * Calculate the areas for the chart, and axes
	 * @returns {ChartDimensions}
	 */
	ChartDimensions()
	{
		let out = new ChartDimensions();
		let rect = Rect.make(0, 0, this.canvas2d.width, this.canvas2d.height);
		let r = {};

		// Save the total chart area
		out.area = Rect.clone(rect);

		// Add margins
		rect.x += this.options.margin.left;
		rect.y += this.options.margin.top;
		rect.w -= this.options.margin.left + this.options.margin.right;
		rect.h -= this.options.margin.top + this.options.margin.bottom;

		// Add space for the title
		if (this.title && this.title.length > 0)
		{
			r = Util.MeasureString(this.m_gfx, this.title, this.options.title_font);
			rect.y += r.height + 2*this.options.title_padding;
			rect.h -= r.height + 2*this.options.title_padding;
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
				r = Util.MeasureString(this.m_gfx, this.xaxis.label, this.options.xaxis.label_font);
				rect.h -= r.height;
				out.xlabel_size = r;
			}
			if (this.yaxis.label && this.yaxis.label.length > 0)
			{
				r = Util.MeasureString(this.m_gfx, this.yaxis.label, this.options.yaxis.label_font);
				rect.x += r.height; // will be rotated by 90deg
				rect.w -= r.height;
				out.ylabel_size = r;
			}

			// Add space for the tick labels
			// Note: If you're having trouble with the axis jumping around
			// check the 'TickText' callback is returning fixed length strings
			if (this.options.xaxis.draw_tick_labels)
			{
				// Measure the height of the tick text
				r = this.xaxis.MeasureTickText(this.m_gfx);
				rect.h -= r.height;
				out.xtick_label_size = r;
			}
			if (this.options.yaxis.draw_tick_labels)
			{
				// Measure the width of the tick text
				r = this.yaxis.MeasureTickText(this.m_gfx);
				rect.x += r.width;
				rect.w -= r.width;
				out.ytick_label_size = r;
			}
		}

		// Save the chart area
		if (rect.w < 0) rect.w = 0;
		if (rect.h < 0) rect.h = 0;
		out.chart_area = Rect.clone(rect);

		return out;
	}

	/**
	 * Returns a point in chart space from a point in client space. Use to convert mouse (client-space) locations to chart coordinates
	 * @param {[x,y]} client_point 
	 * @returns {[x,y]}
	 */
	ClientToChartPoint(client_point)
	{
		return [
			(this.xaxis.min + (client_point[0] - this.dimensions.chart_area.l) * this.xaxis.span / this.dimensions.chart_area.w),
			(this.yaxis.min - (client_point[1] - this.dimensions.chart_area.b) * this.yaxis.span / this.dimensions.chart_area.h)];
	}

	/**
	 * Returns a size in chart space from a size in client space.
	 * @param {[w,h]} client_size 
	 * @returns {[w,h]}
	 */
	ClientToChartSize(client_size)
	{
		return [
			(client_size[0] * this.xaxis.span / this.dimensions.chart_area.w),
			(client_size[1] * this.yaxis.span / this.dimensions.chart_area.h)];
	}

	/**
	 * Returns a rectangle in chart space from a rectangle in client space
	 * @param {{x,y,w,h}} client_rect
	 * @returns {{x,y,w,h}}
	 */
	ClientToChartRect(client_rect)
	{
		let pt = ClientToChartPoint([client_rect.x, client_rect.y]);
		let sz = ClientToChartSize([client_rect.w, client_rect.h]);
		return {x:pt[0], y:pt[1], w:sz[0], h:sz[1]};
	}

	/**
	 * Returns a point in client space from a point in chart space. Inverse of ClientToChartPoint
	 * @param {[x,y]} chart_point 
	 * @returns {[x,y]}
	 */
	ChartToClientPoint(chart_point)
	{
		return [
			(this.dimensions.chart_area.l + (chart_point[0] - this.xaxis.min) * this.dimensions.chart_area.w / this.xaxis.span),
			(this.dimensions.chart_area.b - (chart_point[1] - this.yaxis.min) * this.dimensions.chart_area.h / this.yaxis.span)];
	}

	/**
	 * Returns a size in client space from a size in chart space. Inverse of ClientToChartSize
	 * @param {[w,h]} chart_point 
	 * @returns {[w,h]}
	 */
	ChartToClientSize(chart_size)
	{
		return [
			(chart_size[0] * this.dimensions.chart_area.w / this.xaxis.span),
			(chart_size[1] * this.dimensions.chart_area.h / this.yaxis.span)];
	}

	/**
	 * Returns a rectangle in client space from a rectangle in chart space. Inverse of ClientToChartRect
	 * @param {{x,y,w,h}} chart_rect
	 * @returns {{x,y,w,h}}
	 */
	ChartToClientRect(chart_rect)
	{
		let pt = ChartToClientPoint([chart_rect.x, client_rect.y]);
		let sz = ChartToClientSize([chart_rect.w, client_rect.h]);
		return {x:pt[0], y:pt[1], w:sz[0], h:sz[1]};
	}

	/**
	 * Return a point in camera space from a point in chart space (Z = focus plane)
	 * @param {[x,y]} chart_point
	 * @returns {v4}
	 */
	ChartToCamera(chart_point)
	{
		// Remove the camera to origin offset
		let origin_cs = m4x4.GetW(this.camera.w2c);
		let pt = [chart_point[0] + origin_cs[0], chart_point[1] + origin_cs[1]];
		return v4.make(pt[0], pt[1], -this.camera.focus_dist, 1);
	}

	/**
	 * Return a point in chart space from a point in camera space
	 * @param {v4} camera_point
	 * @returns {[x,y]}
	 */
	CameraToChart(camera_point)
	{
		if (camera_point.z <= 0)
			throw new Error("Invalidate camera point. Points must be in front of the camera");

		// Project the camera space point onto the focus plane
		let proj = -this.camera.focus_dist / camera_point[2];
		let pt = [camera_point[0] * proj, camera_point[1] * proj];

		// Add the offset of the camera from the origin
		let origin_cs = m4x4.GetW(this.camera.w2c);
		return [pt[0] - origin_cs[0], pt[1] - origin_cs[1]];
	}

	/**
	 * Get the scale and translation transform from chart space to client space.
	 * e.g. chart2client * Point(x_min, y_min) = plot_area.BottomLeft()
	 *      chart2client * Point(x_max, y_max) = plot_area.TopRight()
	 * @param {{l,t,r,b,w,h}} plot_area (optional) The area of the chart content (defaults to the current content area)
	 * @returns {m4x4}
	 */
	ChartToClientSpace(plot_area)
	{
		plot_area = plot_area || this.dimensions.chart_area;

		let scale_x  = +(plot_area.w / this.xaxis.span);
		let scale_y  = -(plot_area.h / this.yaxis.span);
		let offset_x = +(plot_area.l - this.xaxis.min * scale_x);
		let offset_y = +(plot_area.b - this.yaxis.min * scale_y);

		// C = chart, c = client
		let C2c = m4x4.make(
			v4.make(scale_x  , 0        , 0, 0),
			v4.make(0        , scale_y  , 0, 0),
			v4.make(0        , 0        , 1, 0),
			v4.make(offset_x , offset_y , 0, 1));

		return C2c;
	}

	/**
	 * Get the scale and translation transform from client space to chart space.
	 * e.g. client2chart * plot_area.BottomLeft() = Point(x_min, y_min)
	 *      client2chart * plot_area.TopRight()   = Point(x_max, y_max)
	 * @param {{l,t,r,b,w,h}} plot_area (optional) The area of the chart content (defaults to the current content area)
	 * @returns {m4x4}
	 */
	ClientToChartSpace(plot_area)
	{
		plot_area = plot_area || this.dimensions.chart_area;

		let scale_x  = +(this.xaxis.span / plot_area.w);
		let scale_y  = -(this.yaxis.span / plot_area.h);
		let offset_x = +(this.xaxis.min - plot_area.l * scale_x);
		let offset_y = +(this.yaxis.min - plot_area.b * scale_y);

		// C = chart, c = client
		let c2C = m4x4.make(
			v4.make(scale_x  , 0        , 0, 0),
			v4.make(0        , scale_y  , 0, 0),
			v4.make(0        , 0        , 1, 0),
			v4.make(offset_x , offset_y , 0, 1));

		return c2C;
	}

	/**
	 * Convert a point between client space and normalised screen space
	 * @param {[x,y]} client_point
	 * @returns {[x,y]}
	 */
	ClientToNSSPoint(client_point)
	{
		return [
			((client_point[0] - this.dimensions.chart_area.l) / this.dimensions.chart_area.w) * 2 - 1,
			((this.dimensions.chart_area.b - client_point[1]) / this.dimensions.chart_area.h) * 2 - 1];
	}

	/**
	 * Convert a size between client space and normalised screen space
	 * @param {[w,h]} client_size
	 * @returns {[w,h]}
	 */
	ClientToNSSSize(client_size)
	{
		return [
			(client_size[0] / this.dimensions.chart_area.w) * 2,
			(client_size[1] / this.dimensions.chart_area.h) * 2];
	}

	/**
	 * Convert a rectangle between client space and normalised screen space
	 * @param {{x,y,w,h}} client_rect
	 * @returns {{x,y,w,h}}
	 */
	ClientToNSSRect(client_rect)
	{
		let pt = ClientToNSSPoint([client_rect.x, client_rect.y]);
		let sz = ClientToNSSSize([client_rect.w, client_rect.h]);
		return {x:pt[0], y:pt[1], w:sz[0], h:sz[1]};
	}

	/**
	 * Convert a normalised screen space point to client space
	 * @param {[x,y]} nss_point
	 * @returns {[x,y]}
	 */
	NSSToClientPoint(nss_point)
	{
		return [
			this.dimensions.chart_area.l + 0.5 * (nss_point[0] + 1) * this.dimensions.chart_area.w,
			this.dimensions.chart_area.b - 0.5 * (nss_point[1] + 1) * this.dimensions.chart_area.h];
	}

	/**
	 * Convert a normalised screen space size to client space
	 * @param {[w,h]} nss_size
	 * @returns {[w,h]}
	 */
	NSSToClientSize(nss_size)
	{
		return [
			0.5 * nss_size[0] * this.dimensions.chart_area.w,
			0.5 * nss_size[1] * this.dimensions.chart_area.h];
	}

	/**
	 * Convert a normalised screen space rectangle to client space
	 * @param {{x,y,w,h}}
	 * @return {{x,y,w,h}}
	 */
	NSSToClient(nss_rect)
	{
		let pt = NSSToClientPoint([nss_rect.x, nss_rect.y]);
		let sz = NSSToClientSize([nss_rect.w, nss_rect.h]);
		return {x:pt[0], y:pt[1], w:sz[0], h:sz[1]};
	}

	/**
	 * Perform a hit test on the chart
	 * @param {[x,y]} client_point The client space point to hit test
	 * @param {{shift, alt, ctrl}} modifier_keys The state of shift, alt, ctrl keys
	 * @param {function} pred A predicate function for filtering hit objects
	 * @returns {HitTestResult}
	 */
	HitTestCS(client_point, modifier_keys, pred)
	{
		// Determine the hit zone of the control
		let zone = EZone.None;
		if (Rect.Contains(this.dimensions.chart_area, client_point)) zone |= EZone.Chart;
		if (Rect.Contains(this.dimensions.xaxis_area, client_point)) zone |= EZone.XAxis;
		if (Rect.Contains(this.dimensions.yaxis_area, client_point)) zone |= EZone.YAxis;
		if (Rect.Contains(this.dimensions.title_area, client_point)) zone |= EZone.Title;

		// The hit test point in chart space
		let chart_point = this.ClientToChartPoint(client_point);

		// Find elements that overlap 'client_point'
		let hits = [];
		/* todo
			if (zone & EZone.Chart)
			{
				let elements = pred != null ? Elements.Where(pred) : Elements;
				foreach (let elem in elements)
				{
					let hit = elem.HitTest(chart_point, client_point, modifier_keys, Scene.Camera);
					if (hit != null)
						hits.Add(hit);
				}

				// Sort the results by z order
				hits.Sort((l,r) => -l.Element.PositionZ.CompareTo(r.Element.PositionZ));
			}
		*/

		return new HitTestResult(zone, client_point, chart_point, modifier_keys, hits, this.camera);
	}

	/**
	 * Find the default range, then reset to the default range
	 */
	AutoRange()
	{
		// Allow the auto range to be handled by event
		let args = {who:"all", view_bbox:BBox.create(), dims:this.dimensions, handled:false};
		this.OnAutoRange.invoke(this, args);

		// For now, auto ranging must be done by user code since the chart
		// doesn't know about what's in the scene.
		if (!args.handled)
			return;

		// If user code handled the auto ranging, sanity check
		if (args.handled && (args.view_bbox == null || !args.view_bbox.is_valid || v4.LengthSq(args.view_bbox.radius) == 0))
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
	 * Raise the OnChartMoved event on the next message.
	 * This method is designed to be called several times within one thread message.
	 * @param {EMove} move_type The movement type that occurred
	 */
	_RaiseChartMoved(move_type)
	{
		if (move_type & EMove.Zoomed) this.xaxis._InvalidateGfx();
		if (move_type & EMove.Zoomed) this.yaxis._InvalidateGfx();

		if (!this.m_chart_moved_args)
		{
			// Trigger a redraw of the chart
			this.Invalidate();

			// Notify of the chart moving
			this.m_chart_moved_args = {move_type: EMove.None};
			Util.BeginInvoke(() =>
			{
				this.OnChartMoved.invoke(this, this.m_chart_moved_args);
				this.m_chart_moved_args = null;
			});
		}

		// Accumulate the moved flags until 'OnChartMoved' has been called.
		this.m_chart_moved_args.move_type |= move_type;
	}
}

/**
 * Create a chart in the given canvas element
 * @param {HTMLCanvasElement} canvas the HTML element to turn into a chart
 * @returns {Chart}
 */
export function Create(canvas)
{
	return new Chart(canvas);
}

/**
 * Create a chart data series instance.
 * Data can be added/removed from a chart via its 'data' array
 * @param {ChartDataSeries} title 
 */
export function CreateDataSeries(title)
{
	this.title = title;
	this.options = {};
	this.data = [];
}

/**
 * Chart layout data
 */
export class ChartDimensions
{
	constructor()
	{
		this.area = Rect.create();
		this.chart_area = Rect.create();
		this.title_size = [0,0];
		this.xlabel_size = [0,0];
		this.ylabel_size = [0,0];
		this.xtick_label_size = [0,0];
		this.ytick_label_size = [0,0];
	}
	get xaxis_area()
	{
		return Rect.ltrb(this.chart_area.l, this.chart_area.b, this.chart_area.r, this.area.b);
	}
	get yaxis_area()
	{
		return Rect.ltrb(0, this.chart_area.t, this.chart_area.l, this.chart_area.b);
	}
	get title_area()
	{
		return Rect.ltrb(this.chart_area.l, 0, this.chart_area.r, this.chart_area.t);
	}
}

/**
 * Hit test results structure
 */
export class HitTestResult
{
	constructor(zone, client_point, chart_point, modifier_keys, hits, camera)
	{
		this.zone = zone;
		this.client_point = client_point;
		this.chart_point = chart_point;
		this.modifier_keys = modifier_keys;
		this.hits = hits;
		this.camera = camera;
	}	
}

/**
 * Chart axis data
 */
class Axis
{
	constructor(chart, options, label)
	{
		this.min = 0;
		this.max = 1;
		this.chart = chart;
		this.label = label;
		this.options = options;
		this.m_geom_lines = null;
		this.m_allow_scroll = true;
		this.m_allow_zoom = true;
		this.m_lock_range = false;
		this.TickText = this.TickTextDefault;
		this.MeasureTickText = this.MeasureTickTextDefault;
		
		this.OnScroll = new Util.MulticastDelegate();
		this.OnZoomed = new Util.MulticastDelegate();
	}

	/**
	 * Get/Set the range of the axis
	 * Setting the span leaves the centre unchanged
	 */
	get span()
	{
		return this.max - this.min;
	}
	set span(value)
	{
		if (this.span == value) return;
		if (value <= 0) throw new Error("Invalid axis span: " + value);
		this.Set(this.centre - 0.5*value, this.centre + 0.5*value);
	}

	/**
	 * Get/Set the centre position of the axis
	 */
	get centre()
	{
		return (this.max + this.min) * 0.5;
	}
	set centre(value)
	{
		if (this.centre == value) return;
		this.Set(this.min + value - this.centre, this.max + value - this.centre);
	}

	/**
	 * Get/Set whether scrolling on this axis is allowed
	 */
	get allow_scroll()
	{
		return this.m_allow_scroll && !this.lock_range;
	}
	set allow_scroll(value)
	{
		this.m_allow_scroll = value;
	}

	/**
	 * Get/Set whether zooming on this axis is allowed
	 */
	get allow_zoom()
	{
		return this.m_allow_zoom && !this.lock_range;
	}
	set allow_zoom(value)
	{
		this.m_allow_zoom = value;
	}

	/**
	 * Get/Set whether the range can be changed by user input
	 */
	get lock_range()
	{
		return this.m_lock_range;
	}
	set lock_range(value)
	{
		this.m_lock_range = value;
	}

	/**
	 * Return a string description of the axis state. Used to detect changes
	 * @returns {string}
	 */
	get hash()
	{
		return "" + this.min + this.max + this.label;
	}

	/**
	 * Set the range without risk of an assert if 'min' is greater than 'Max' or visa versa.
	 */
	Set(min, max)
	{
		if (min >= max) throw new Error("Range must be positive and non-zero");
		let zoomed = !Maths.FEql(max - min, this.max - this.min);
		let scroll = !Maths.FEql((max + min)*0.5, (this.max + this.min)*0.5);

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
	 * @param {Number} delta
	 */
	Shift(delta)
	{
		if (!this.allow_scroll) return;
		this.centre += delta;
	}
	
	/**
	 * Default value to text conversion
	 * @param {Number} x The tick value on the axis
	 * @param {Number} step The current axis step size, from one tick to the next
	 * @returns {string}
	 */
	TickTextDefault(x, step)
	{
		// This solves the rounding problem for values near zero when the axis span could be anything
		if (Maths.FEql(x / this.span, 0.0)) return "0";

		// Use 5dp of precision
		let text = x.toPrecision(5);
		if (text.indexOf('.') != -1)
		{
			text = Util.TrimRight(text, ['0']);
			if (text.slice(-1) == '.') text += '0';
		}		
		return text;
	}

	/**
	 * Measure the tick text width
	 * @param {Canvas2dContext} gfx 
	 * @returns {{width, height}}
	 */
	MeasureTickTextDefault(gfx)
	{
		// Can't use 'GridLines' here because creates an infinite recursion.
		// Using TickText(Min/Max, 0.0) causes the axes to jump around.
		// The best option is to use one fixed length string.
		return Util.MeasureString(gfx, "-9.99999", this.options.tick_font);
	}

	/**
	 * Return the position of the grid lines for this axis
	 * @param {Number} axis_length The size of the chart area in the direction of this axis (i.e. dims.chart_area.w or dims.chart_area.h)
	 * @param {Number} pixels_per_tick The ideal number of pixels between each tick mark
	 * @returns {{min,max,step}}
	 */
	GridLines(axis_length, pixels_per_tick)
	{
		let out = {};
		let max_ticks = axis_length / pixels_per_tick;

		// Choose step sizes
		let step_base = Math.pow(10.0, Math.floor(Math.log10(this.span))), step = step_base;
		let quantisations = [0.05, 0.1, 0.2, 0.25, 0.5, 1.0, 2.0, 5.0, 10.0, 20.0, 50.0];
		for (let i = 0; i != quantisations.length; ++i)
		{
			let s = quantisations[i];
			if (s * this.span > max_ticks * step_base) continue;
			step = step_base / s;
		}

		out.step = step;
		out.min = (this.min - Maths.IEEERemainder(this.min, step)) - this.min;
		out.max = this.span * 1.0001;
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
	 * Return a renderer instance for the grid lines for this axis
	 * @param {Chart} chart
	 * @param {ChartDimensions} dims
	 * @returns {Rdr.Instance}
	 */
	GridLineGfx(chart, dims)
	{
		// If there is no cached grid line graphics, generate them
		if (this.m_geom_lines == null)
		{
			// Create a model for the grid lines
			// Need to allow for one step in either direction because we only create the grid lines
			// model when scaling and we can translate by a max of one step in either direction.
			let axis_length =
				this === chart.xaxis ? dims.chart_area.w :
				this === chart.yaxis ? dims.chart_area.h :
				null;
			let gl = this.GridLines(axis_length, this.options.pixels_per_tick);
			let num_lines = Math.floor(2 + (gl.max - gl.min) / gl.step);

			// Create the grid lines at the origin, they get positioned as the camera moves
			let verts = new Float32Array(num_lines * 2 * 3);
			let indices = new Uint16Array(num_lines * 2);
			let nuggets = [];
			let name = "";
			let v = 0;
			let i = 0;

			// Grid verts
			if (this === chart.xaxis)
			{
				name = "xaxis_grid";
				let x = 0, y0 = 0, y1 = chart.yaxis.span;
				for (let l = 0; l != num_lines; ++l)
				{
					Util.CopyTo(verts, v, x, y0, 0); v += 3;
					Util.CopyTo(verts, v, x, y1, 0); v += 3;
					x += gl.step;
				}
			}
			if (this === chart.yaxis)
			{
				name = "yaxis_grid";
				let y = 0, x0 = 0, x1 = chart.xaxis.span;
				for (let l = 0; l != num_lines; ++l)
				{
					Util.CopyTo(verts, v, x0, y, 0); v += 3;
					Util.CopyTo(verts, v, x1, y, 0); v += 3;
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
			nuggets.push({topo: chart.m_rdr.LINES, shader: chart.m_rdr.shader_programs["forward"], tint: Util.ColourToV4(chart.options.grid_colour) });

			// Create the model
			let model = Rdr.Model.CreateRaw(chart.m_rdr, verts, null, null, null, indices, nuggets);

			// Create the instance
			this.m_geom_lines = Rdr.Instance.Create(name, model, m4x4.create(), Rdr.EFlags.SceneBoundsExclude|Rdr.EFlags.NoZWrite);
		}

		// Return the graphics model
		return this.m_geom_lines;
	}

	/**
	 * Invalidate the graphics, causing them to be recreated next time they're needed
	 */
	_InvalidateGfx()
	{
		this.m_geom_lines = null;
	}
}

/**
 * Chart naviation
 */
class Navigation
{
	constructor(chart)
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

	/**
	 * Start a mouse operation associated with the given mouse button index
	 * @param {MouseButton} btn 
	 */
	BeginOp(btn)
	{
		this.active = this.pending[btn];
		this.pending[btn] = null;
	}

	/**
	 * End a mouse operation for the given mouse button index
	 * @param {MouseButton} btn 
	 */
	EndOp(btn)
	{
		this.active = null;
		if (this.pending[btn] && !this.pending[btn].start_on_mouse_down)
			this.BeginOp(btn);
	}

	/**
	 * Handle mouse down on the chart
	 * @param {MouseEventData} ev 
	 */
	OnMouseDown(ev)
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
		let op = this.active;
		if (op != null && !op.cancelled)
		{
			op.btn_down    = true;
			op.grab_client = [ev.offsetX, ev.offsetY];
			op.grab_chart  = this.chart.ClientToChartPoint(op.grab_client);
			op.hit_result  = this.chart.HitTestCS(op.grab_client, {shift: ev.shiftKey, alt: ev.altKey, ctrl: ev.ctrlKey}, null);
			op.MouseDown(ev);
			this.chart.canvas2d.setPointerCapture(ev.pointerId);
		}
	}
	
	/**
	 * Handle mouse move on the chart
	 * @param {MouseEvent} ev 
	 */
	OnMouseMove(ev)
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
			let client_point = [ev.offsetX, ev.offsetY];
			let hit_result = this.chart.HitTestCS(client_point, {shift: ev.shiftKey, alt: ev.altKey, ctrl: ev.ctrlKey}, null);
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
	OnMouseUp(ev)
	{
		// Only release the mouse when all buttons are up
		//if (ev.button == EButton. MouseButtons.None)
			this.chart.canvas2d.releasePointerCapture(ev.pointerId);

		// Look for the mouse op to perform
		let op = this.active;
		if (op != null && !op.cancelled)
			op.MouseUp(ev);
		
		this.EndOp(ev.button);
	}	

	/**
	 * Handle mouse wheel changes on the chart
	 * @param {MouseWheelEvent} ev 
	 */
	OnMouseWheel(ev)
	{
		let client_pt = [ev.offsetX, ev.offsetY];
		let chart_pt = this.chart.ClientToChartPoint(client_pt);
		let dims = this.chart.dimensions;
		let perp_z = this.chart.options.perpendicular_z_translation != ev.altKey;
		if (Rect.Contains(dims.chart_area, client_pt))
		{
			// If there is a mouse op in progress, ignore the wheel
			let op = this.active;
			if (op == null || op.cancelled)
			{
				// Set a scaling factor from the mouse wheel clicks
				let scale = 1.0 / 120.0;
				let delta = Maths.Clamp(ev.wheelDelta * scale, -100, 100);

				// Convert 'client_pt' to normalised camera space
				let nss_point = Rect.NormalisePoint(dims.chart_area, client_pt, +1, -1);

				// Translate the camera along a ray through 'point'
				this.chart.camera.MouseControlZ(nss_point, delta, !perp_z);
				this.chart.Invalidate();
			}
		}
		else if (this.chart.options.show_axes)
		{
			let scale = 0.001;
			let delta = Maths.Clamp(ev.wheelDelta * scale, -0.999, 0.999);

			let xaxis = this.chart.xaxis;
			let yaxis = this.chart.yaxis;
			let chg = false;

			// Change the aspect ratio by zooming on the XAxis
			if (Rect.Contains(dims.xaxis_area, client_pt) && !xaxis.lock_range)
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
					if (this.chart.options.lock_aspect != null)
						yaxis.span *= (1 - delta);

					chg = true;
				}
			}

			// Check the aspect ratio by zooming on the YAxis
			if (Rect.Contains(dims.yaxis_area, client_pt) && !yaxis.lock_range)
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
					if (this.chart.options.lock_aspect != null)
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

/**
 * Mouse operation base class
 */
class MouseOp
{
	constructor(chart)
	{
		this.chart = chart;
		this.start_on_mouse_down = true;
		this.cancelled = false;
		this.btn_down = false;
		this.grab_client = null;
		this.grab_chart = null;
		this.hit_result = null;
		this.m_is_click = true;
	}

	/**
	 * Test a mouse location as being a click (as apposed to a drag)
	 * @param {[x,y]} point The mouse pointer location (client space)
	 * @returns {boolean} True if the distance between 'location' and mouse down should be treated as a click
	 */
	IsClick(point)
	{
		if (!this.m_is_click) return false;
		let diff = [point[0] - this.grab_client[0], point[1] - this.grab_client[1]];
		return this.m_is_click = v2.LengthSq(diff) < Maths.Sqr(this.chart.options.min_drag_pixel_distance);
	}
}
class MouseOpDefaultLButton extends MouseOp
{
	constructor(chart)
	{
		super(chart);
		this.hit_selected = null;
		this.m_selection_graphic_added = false;
	}
	MouseDown(ev)
	{
		// Look for a selected object that the mouse operation starts on
		//this.m_hit_selected = this.hit_result.hits.find(function(x){ return x.Element.Selected; });

		//// Record the drag start positions for selected objects
		//foreach (let elem in m_chart.Selected)
		//	elem.DragStartPosition = elem.Position;

		// For 3D scenes, left mouse rotates if mouse down is within the chart bounds
		if (this.chart.options.navigation_mode == ENavMode.Scene3D && this.hit_result.zone == EZone.Chart && ev.button == EButton.Left)
			this.chart.camera.MouseControl(this.grab_client, Rdr.Camera.ENavOp.Rotate, true);

		//// Prevent events while dragging the elements around
		//m_suspend_scope = m_chart.SuspendChartChanged(raise_on_resume:true);
	}
	MouseMove(ev)
	{
		let client_point = [ev.offsetX, ev.offsetY];

		// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
		if (this.IsClick(client_point) && !this.m_selection_graphic_added)
			return;

		let drag_selected = this.chart.options.allow_editing && this.hit_selected != null;
		if (drag_selected)
		{
			// If the drag operation started on a selected element then drag the
			// selected elements within the diagram.
			//let delta = v2.Sub(this.chart.ClientToChartPoint(client_point), this.grab_chart);
			//this.chart.DragSelected(delta, false);
		}
		else if (this.chart.options.navigation_mode == ENavMode.Chart2D)
		{
			if (this.chart.options.area_select_mode != EAreaSelectMode.Disabled)
			{
				// Otherwise change the selection area
				if (!this.m_selection_graphic_added)
				{
					this.chart.instances.push(this.chart.m_tools.area_select);
					this.m_selection_graphic_added = true;
				}

				// Position the selection graphic
				let area = Rect.bound(this.grab_chart, this.chart.ClientToChartPoint(client_point));
				m4x4.clone(m4x4.Scale(area.w, area.h, 1, v4.make(area.centre[0], area.centre[1], 0, 1), this.chart.m_tools.area_select.o2w));
			}
		}
		else if (this.chart.options.navigation_mode == ENavMode.Scene3D)
		{
			this.chart.camera.MouseControl(client_point, Rdr.Camera.ENavOp.Rotate, false);
		}

		// Render immediately, for smoother navigation
		this.chart.Render();
	}
	MouseUp(ev)
	{
		let client_point = [ev.offsetX, ev.offsetY];

		// If this is a single click...
		if (this.IsClick(client_point))
		{
			// Pass the click event out to users first
			let args = { hit_result:this.hit_result, handled:false };
			this.chart.OnChartClicked.invoke(this.chart, args);

			// If a selected element was hit on mouse down, see if it handles the click
			if (!args.handled && this.hit_selected != null)
			{
				//this.hit_selected.Element.HandleClicked(args);
			}

			// If no selected element was hit, try hovered elements
			if (!args.handled && this.hit_result.hits.length != 0)
			{
				for (let i = 0; i != this.hit_result.hits.length && !args.handled; ++i)
					this.hit_result.hits[i].Element.HandleClicked(args);
			}

			// If the click is still unhandled, use the click to try to select something (if within the chart)
			if (!args.handled && (this.hit_result.zone & EZone.Chart))
			{
				let area = Rect.make(this.grab_chart[0], this.grab_chart[1], 0, 0);
				//this.chart.SelectElements(area, {shift: ev.shiftKey, alt: ev.altKey, ctrl: ev.ctrlKey});
			}
		}
		// Otherwise this is a drag action
		else
		{
			// If an element was selected, drag it around
			if (this.hit_selected != null && this.chart.options.allow_editing)
			{
				let delta = v2.Sub(this.chart.ClientToChartPoint(client_point), this.grab_chart);
				//this.chart.DragSelected(delta, true);
			}
			else if (this.chart.options.navigation_mode == ENavMode.Chart2D)
			{
				// Otherwise create an area selection if the click started within the chart
				if ((this.hit_result.zone & EZone.Chart) && this.chart.options.area_select_mode != EAreaSelectMode.Disabled)
				{
					let area = Rect.bound(this.grab_chart, this.chart.ClientToChartPoint(client_point));
					//m_chart.OnChartAreaSelect(new ChartAreaSelectEventArgs(selection_area));
				}
			}
			else if (this.chart.options.navigation_mode == ENavMode.Scene3D)
			{
				// For 3D scenes, left mouse rotates
				this.chart.camera.MouseControl(client_point, Rdr.Camera.ENavOp.Rotate, true);
			}
		}

		// Remove the area selection graphic
		if (this.m_selection_graphic_added)
		{
			let idx = this.chart.instances.indexOf(this.chart.m_tools.area_select);
			if (idx != -1) this.chart.instances.splice(idx, 1);
		}

		//this.chart.Cursor = Cursors.Default;
		this.chart.Invalidate();
	}
}
class MouseOpDefaultRButton extends MouseOp
{
	constructor(chart)
	{
		super(chart);
	}
	MouseDown(ev)
	{
		// Convert 'grab_client' to normalised camera space
		let nss_point = Rect.NormalisePoint(this.chart.dimensions.chart_area, this.grab_client, +1, -1);

		// Right mouse translates for 2D and 3D scene
		this.chart.camera.MouseControl(nss_point, Rdr.Camera.ENavOp.Translate, true);
	}
	MouseMove(ev)
	{
		let client_point = [ev.offsetX, ev.offsetY];

		// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
		if (this.IsClick(client_point))
			return;

		// Change the cursor once dragging
		//this.chart.Cursor = Cursors.SizeAll;

		// Limit the drag direction
		let drop_loc = Rect.NormalisePoint(this.chart.dimensions.chart_area, client_point, +1, -1);
		let grab_loc = Rect.NormalisePoint(this.chart.dimensions.chart_area, this.grab_client, +1, -1);
		if ((this.hit_result.zone & EZone.YAxis) || this.chart.xaxis.lock_range) drop_loc[0] = grab_loc[0];
		if ((this.hit_result.zone & EZone.XAxis) || this.chart.yaxis.lock_range) drop_loc[1] = grab_loc[1];

		this.chart.camera.MouseControl(drop_loc, Rdr.Camera.ENavOp.Translate, false);
		this.chart.SetRangeFromCamera();
		this.chart.Invalidate();
	}
	MouseUp(ev)
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
			let drop_loc = Rect.NormalisePoint(this.chart.dimensions.chart_area, client_point, +1, -1);
			let grab_loc = Rect.NormalisePoint(this.chart.dimensions.chart_area, this.grab_client, +1, -1);
			if ((this.hit_result.zone & EZone.YAxis) || this.chart.xaxis.lock_range) drop_loc[0] = grab_loc[0];
			if ((this.hit_result.zone & EZone.XAxis) || this.chart.yaxis.lock_range) drop_loc[1] = grab_loc[1];
	
			this.chart.camera.MouseControl(drop_loc, Rdr.Camera.ENavOp.None, true);
			this.chart.SetRangeFromCamera();
			this.chart.Invalidate();
		}
	}
}


/**
 * Chart utility graphics
 */
class Tools
{
	constructor(chart)
	{
		this.chart = chart;
		this.m_area_select = null;
	}

	get area_select()
	{
		if (!this.m_area_select)
		{
			let model = Rdr.Model.Create(this.chart.m_rdr,
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
				{ topo:this.chart.m_rdr.TRIANGLES, shader:this.chart.m_rdr.shader_programs.forward, tint:this.chart.options.selection_colour }
			]);
			this.m_area_select = Rdr.Instance.Create("area_select", model, m4x4.create(), Rdr.EFlags.SceneBoundsExclude|Rdr.EFlags.NoZWrite|Rdr.EFlags.NoZTest);
		}
		return this.m_area_select;
	}
}

