import * as m4x4 from "../maths/m4x4";
import * as Rect from "../maths/rect";
import * as Rdr from "../renderer/renderer";
import * as Util from "../utility/util";

export class Chart
{
	/**
	 * Create a chart in the given canvas element
	 * @param {HTMLCanvasElement} canvas the HTML element to turn into a chart
	 * @returns {Chart}
	 */
	constructor(canvas)
	{
		let chart = this;

		// Hold a reference to the element we're drawing on
		// Create a duplicate canvas to draw 2d stuff on, and position it
		// at the same location, and over the top of, 'canvas'.
		// 'canvas2d' is an overlay over 'canvas3d'.
		this.canvas3d = canvas;
		this.canvas2d = canvas.cloneNode(false);
		this.canvas2d.style.position = "absolute";
		this.canvas2d.style.border = this.canvas3d.style.border;
		this.canvas2d.style.left = this.canvas3d.style.left;
		this.canvas2d.style.top = this.canvas3d.style.top;
		canvas.parentNode.insertBefore(this.canvas2d, this.canvas3d);
	
		// Get the 2D drawing interface
		this.m_gfx = this.canvas2d.getContext("2d");
	
		// Initialise WebGL
		this.m_rdr = Rdr.Create(this.canvas3d);
		this.m_rdr.back_colour = v4.make(1,1,1,1);
	
		// Create an area for storing cached data
		this.m_cache = new function()
		{
			this.chart_frame = null;
			this.xaxis_hash = null;
			this.yaxis_hash = null;
			this.redraw_pending = false;
			this.invalidate = function(){ this.chart_frame = null; };
		};
	
		// The buffer of instances to render on the chart
		this.instances = [];
	
		// Initialise the chart camera
		this.camera = Rdr.Camera.Create(Maths.TauBy8, this.m_rdr.AspectRatio);
		this.camera.orthographic = true;
		this.camera.pos = v4.make(0, 0, 10, 1);
	
		// The light source the illuminates the chart
		this.light = Rdr.Light.Create(Rdr.ELight.Directional, v4.Origin, v4.Neg(v4.ZAxis), null, [0.5,0.5,0.5], null, null);
	
		// Navigation
		this.navigation = new Navigation(chart);
		chart.canvas2d.onmousedown = function(ev){ chart.navigation.OnMouseDown(ev); }
		chart.canvas2d.onmousemove = function(ev) { chart.navigation.OnMouseMove(ev); };
		chart.canvas2d.onmouseup = function(ev) { chart.navigation.OnMouseUp(ev); };
		chart.canvas2d.onmousewheel = function(ev) { chart.navigation.OnMouseWheel(ev); };
	
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
			this.axis_thickness = 1;
			this.navigation_mode = "2d";
			this.show_grid_lines = true;
			this.show_axes = true;
			this.lock_aspect = null;
			this.perpendicular_z_translation = false;
			//SelectionColour           = Color.FromArgb(0x80, Color.DarkGray);
			//GridZOffset               = 0.001f;
			//AntiAliasing              = true;
			//FillMode                  = View3d.EFillMode.Solid;
			//CullMode                  = View3d.ECullMode.Back;
			//Orthographic              = false;
			//MinSelectionDistance      = 10f;
			//MinDragPixelDistance      = 5f;
			//ResetForward              = -v4.ZAxis;
			//ResetUp                   = +v4.YAxis;
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
	
		// Chart title
		this.title = "MyChart";
	
		// The data series
		this.data = [];
	
		// Initialise Axis data
		this.xaxis = new Axis(this.options.xaxis);
		this.yaxis = new Axis(this.options.yaxis);
		this.xaxis.label = "X Axis";
		this.yaxis.label = "Y Axis";

		// The plot area of the chart
		this.dimensions = {};
	}

	/**
	 * Render the chart
	 * @param {Chart} chart The chart to be rendered
	 */
	Render()
	{
		// Clear the redraw pending flag
		this.m_cache.redraw_pending = false;

		// Navigation moves the camera and axis ranges are derived from the camera position/view
		this.SetRangeFromCamera();

		// Calculate the chart dimensions
		let dims = this.ChartDimensions();

		// Draw the chart frame
		this.RenderChartFrame(dims);

		// Draw the chart content
		this.RenderChartContent(dims);
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
			//Debug.Assert(xaxis.span > 0, "Negative x range");
			//Debug.Assert(yaxis.span > 0, "Negative y range");

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
					bmp.fillStyle = opts.xaxis.tick_colour;
					bmp.textAlign = "center";
					bmp.beginPath();

					let Y = (dims.chart_area.b + opts.xaxis.tick_length + dims.xtick_label_size.height + 1);
					let gl = xaxis.GridLines(dims.chart_area.w, opts.xaxis.pixels_per_tick);
					for (let x = gl.min; x < gl.max; x += gl.step)
					{
						let X = Math.floor(dims.chart_area.l + x * dims.chart_area.w / xaxis.span);
						let s = xaxis.TickText(x + xaxis.min, gl.step);
						if (opts.xaxis.draw_tick_labels)
						{
							bmp.fillText(s, X, Y, dims.xtick_label_size.width);
						}
						if (opts.xaxis.draw_tick_marks)
						{
							bmp.moveTo(X, dims.chart_area.b);
							bmp.lineTo(X, dims.chart_area.b + opts.xaxis.tick_length);
						}
					}

					bmp.stroke();
					bmp.restore();
				}
				if (opts.yaxis.draw_tick_labels || opts.yaxis.draw_tick_marks)
				{
					bmp.save();
					bmp.font = opts.yaxis.tick_font;
					bmp.fillStyle = opts.yaxis.tick_colour;
					bmp.textAlign = "right";
					bmp.beginPath();

					let X = (dims.chart_area.l - opts.yaxis.tick_length - 1);
					let gl = yaxis.GridLines(dims.chart_area.h, opts.yaxis.pixels_per_tick);
					for (let y = gl.min; y < gl.max; y += gl.step)
					{
						let Y = Math.floor(dims.chart_area.b - y * dims.chart_area.h / yaxis.span);
						let s = yaxis.TickText(y + yaxis.min, gl.step);
						if (opts.yaxis.draw_tick_labels)
						{
							bmp.fillText(s, X, Y + dims.ytick_label_size.height * 0.5, dims.ytick_label_size.width);
						}
						if (opts.yaxis.draw_tick_marks)
						{
							bmp.moveTo(dims.chart_area.l - opts.yaxis.tick_length, Y);
							bmp.lineTo(dims.chart_area.l, Y);
						}
					}

					bmp.stroke();
					bmp.restore();
				}

				{// Axes
					bmp.save();
					bmp.fillStyle = opts.axis_colour;
					bmp.lineWidth = opts.axis_thickness;
					bmp.moveTo(dims.chart_area.l, dims.chart_area.t);
					bmp.lineTo(dims.chart_area.l, dims.chart_area.b);
					bmp.lineTo(dims.chart_area.r, dims.chart_area.b);
					bmp.stroke();
					bmp.restore();
				}

				// Record cache invalidating values
				this.m_cache.xaxis_hash = this.xaxis.hash;
				this.m_cache.yaxis_hash = this.yaxis.hash;
			}
			bmp.restore();

			// Update the clipping region
			this.m_gfx.rect(dims.area.l, dims.area.t, dims.area.w, dims.area.h);
			this.m_gfx.rect(dims.chart_area.l, dims.chart_area.t, dims.chart_area.w, dims.chart_area.h);
			this.m_gfx.clip();
		}

		// Blit the cached image to the screen
		this.m_gfx.drawImage(this.m_cache.chart_frame, dims.area.l, dims.area.t);
	}

	/**
	 * Draw the chart content
	 * @param {ChartDimensions} dims The chart size data returned from ChartDimensions
	 */
	RenderChartContent(dims)
	{
		this.instances.length = 0;
		
		this.test_object = this.test_object || Rdr.CreateTestModel(this.m_rdr);
		this.instances.push(this.test_object);

		// Add axis graphics
		if (this.options.show_grid_lines)
		{
			let xlines = this.xaxis.GridLineGfx(this, dims);
			let ylines = this.yaxis.GridLineGfx(this, dims);
			this.instances.push(xlines);
			this.instances.push(ylines);
		}


		// Add chart elements

		// Add user graphics

		// Ensure the viewport matches the chart context area
		this.m_rdr.viewport(dims.chart_area.l, dims.chart_area.t, dims.chart_area.w, dims.chart_area.h);
		
		// Render the chart
		Rdr.Render(this.m_rdr, this.instances, this.camera, this.light);
	}

	/**
	 * Invalidate cached data before a Render
	 */
	Invalidate()
	{
		this.m_cache.invalidate();
		if (this.m_cache.redraw_pending)
			return;

		// Set a flag to indicate redraw pending
		this.m_cache.redraw_pending = true;

		// Set a timer to redraw the chart
		let chart = this;
		setTimeout(function(){ chart.Render(); }, 10);
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
		let xmin = x - wh.width * 0.5;
		let xmax = x + wh.width * 0.5;
		let ymin = y - wh.height * 0.5;
		let ymax = y + wh.height * 0.5;
		if (xmin < xmax) this.xaxis.Set(xmin, xmax);
		if (ymin < ymax) this.yaxis.Set(ymin, ymax);
	}

	/**
	 * Position the camera based on the axis range.
	 */
	SetCameraFromRange()
	{
		let camera = this.camera;

		// Set the aspect ratio from the axis range and scene bounds
		camera.aspect = (this.xaxis.span / this.yaxis.span);

		// Find the world space position of the new focus point.
		// Move the focus point within the focus plane (parallel to the camera XY plane)
		// and adjust the focus distance so that the view has a width equal to XAxis.Span.
		// The camera aspect ratio should ensure the YAxis is correct.
		let c2w = camera.c2w;
		let focus = v4.AddN([
			v4.MulS(v4.GetX(c2w), this.xaxis.centre),
			v4.MulS(v4.GetY(c2w), this.yaxis.centre),
			v4.MulS(v4.GetZ(c2w), v4.Dot3(camera.focus_point - v4.Origin, v4.GetZ(c2w)))
			]);

		// Move the camera in the camera Z axis direction so that the width at the focus dist
		// matches the XAxis range. Tan(FovX/2) = (XAxis.Span/2)/d
		camera.focus_dist = (this.xaxis.span / (2.0 * Math.tan(camera.fovX / 2.0)));
		camera.pos = v4.SetW1(v4.Add(focus, v4.MulS(v4.GetZ(c2w), camera.focus_dist)));

		camera.c2w = c2w;
		camera.Commit();
	}

	/**
	 * Calculate the areas for the chart, and axes
	 * @returns {{area, chart_area}}
	 */
	ChartDimensions()
	{
		let rect = Rect.make(0, 0, this.canvas2d.width, this.canvas2d.height);
		let r = {};

		// Save the total chart area
		this.dimensions.area = Rect.clone(rect);

		// Add margins
		rect.x += this.options.margin.left;
		rect.y += this.options.margin.top;
		rect.w -= this.options.margin.left + this.options.margin.right;
		rect.h -= this.options.margin.top + this.options.margin.bottom;

		// Add space for the title
		if (this.title && this.title.length > 0)
		{
			r = MeasureString(this.m_gfx, this.title, this.options.title_font);
			rect.y += r.height + 2*this.options.title_padding;
			rect.h -= r.height + 2*this.options.title_padding;
			this.dimensions.title_size = r;
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
				r = MeasureString(this.m_gfx, this.xaxis.label, this.options.xaxis.label_font);
				rect.h -= r.height;
				this.dimensions.xlabel_size = r;
			}
			if (this.yaxis.label && this.yaxis.label.length > 0)
			{
				r = MeasureString(this.m_gfx, this.yaxis.label, this.options.yaxis.label_font);
				rect.x += r.height; // will be rotated by 90deg
				rect.w -= r.height;
				this.dimensions.ylabel_size = r;
			}

			// Add space for the tick labels
			// Note: If you're having trouble with the axis jumping around
			// check the 'TickText' callback is returning fixed length strings
			if (this.options.xaxis.draw_tick_labels)
			{
				// Measure the height of the tick text
				r = this.xaxis.MeasureTickText(this.m_gfx, false);
				rect.h -= r.height;
				this.dimensions.xtick_label_size = r;
			}
			if (this.options.yaxis.draw_tick_labels)
			{
				// Measure the width of the tick text
				r = this.yaxis.MeasureTickText(this.m_gfx, true);
				rect.x += r.width;
				rect.w -= r.width;
				this.dimensions.ytick_label_size = r;
			}
		}

		// Save the chart area
		if (rect.w < 0) rect.w = 0;
		if (rect.h < 0) rect.h = 0;
		this.dimensions.chart_area = Rect.clone(rect);

		return this.dimensions;
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
		var origin_cs = m4x4.GetW(this.camera.w2c);
		var pt = [chart_point[0] + origin_cs[0], chart_point[1] + origin_cs[1]];
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
		var proj = -this.camera.focus_dist / camera_point[2];
		var pt = [camera_point[0] * proj, camera_point[1] * proj];

		// Add the offset of the camera from the origin
		var origin_cs = m4x4.GetW(this.camera.w2c);
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

		var scale_x  = +(plot_area.w / this.xaxis.span);
		var scale_y  = -(plot_area.h / this.yaxis.span);
		var offset_x = +(plot_area.l - this.xaxis.min * scale_x);
		var offset_y = +(plot_area.b - this.yaxis.min * scale_y);

		// C = chart, c = client
		var C2c = m4x4.make(
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

		var scale_x  = +(this.xaxis.span / plot_area.w);
		var scale_y  = -(this.yaxis.span / plot_area.h);
		var offset_x = +(this.xaxis.min - plot_area.l * scale_x);
		var offset_y = +(this.yaxis.min - plot_area.b * scale_y);

		// C = chart, c = client
		var c2C = m4x4.make(
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

/*
		/// <summary>Perform a hit test on the chart</summary>
		public HitTestResult HitTestCS(Point client_point, Keys modifier_keys, Func<Element, bool> pred)
		{
			// Determine the hit zone of the control
			var zone = HitTestResult.EZone.None;
			if (SceneBounds.Contains(client_point)) zone |= HitTestResult.EZone.Chart;
			if (XAxisBounds.Contains(client_point)) zone |= HitTestResult.EZone.XAxis;
			if (YAxisBounds.Contains(client_point)) zone |= HitTestResult.EZone.YAxis;
			if (TitleBounds.Contains(client_point)) zone |= HitTestResult.EZone.Title;

			// The hit test point in chart space
			var chart_point = ClientToChart(client_point);

			// Find elements that overlap 'client_point'
			var hits = new List<HitTestResult.Hit>();
			if (zone.HasFlag(HitTestResult.EZone.Chart))
			{
				var elements = pred != null ? Elements.Where(pred) : Elements;
				foreach (var elem in elements)
				{
					var hit = elem.HitTest(chart_point, client_point, modifier_keys, Scene.Camera);
					if (hit != null)
						hits.Add(hit);
				}

				// Sort the results by z order
				hits.Sort((l,r) => -l.Element.PositionZ.CompareTo(r.Element.PositionZ));
			}

			return new HitTestResult(zone, client_point, chart_point, modifier_keys, hits, Scene.Camera);
		}

		/// <summary>Find the default range, then reset to the default range</summary>
		public void AutoRange(View3d.ESceneBounds who = View3d.ESceneBounds.All)
		{
			// Allow the auto range to be handled by event
			var args = new AutoRangeEventArgs(who);
			RaiseOnAutoRanging(args);
			if (args.Handled && (!args.ViewBBox.IsValid || args.ViewBBox.Radius == v4.Zero))
				throw new Exception($"Caller provided view bounding box is invalid: {args.ViewBBox}");

			// Get the bounding box to fit into the view
			var bbox = args.Handled
				? args.ViewBBox
				: Window.SceneBounds(who, except:new[] { ChartTools.Id });

			// Position the camera to view the bounding box
			Camera.ResetView(bbox, Options.ResetForward, Options.ResetUp,
				dist: 0f,
				preserve_aspect: LockAspect,
				commit: true);

			// Set the axis range from the camera position
			SetRangeFromCamera();
			Invalidate();
		}
*/

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
 * Measure the width and height of 'text'
 * @param {HTMLCanvas2dContext} gfx
 * @param {string} text The text to measure
 * @param {Font} font The font to use to render the text
 * @returns {{width, height}} 
 */
function MeasureString(gfx, text, font)
{
	// Measure width using the 2D canvas API
	let width = gfx.measureText(text, font).width;

	// Measure height using a hack because the API is missing AFAIK.
	// Chrome requires the dummy element to be added to the document 
	// before getComputedStyle works.
	let elem = document.createElement("div");
	elem.style.font = font;
	elem.appendChild(document.createTextNode(text));
	document.body.appendChild(elem);
	let cs = window.getComputedStyle(elem,null);
	let height = parseFloat(cs.getPropertyValue('font-size'));
	document.body.removeChild(elem);

	return {width: width, height: height};
}

class Axis
{
	constructor(options)
	{
		this.min = 0;
		this.max = 1;
		this.label = "";
		this.options = options;
		this.m_geom_lines = null;
		this.m_allow_scroll = true;
		this.m_allow_zoom = true;
		this.m_lock_range = false;
		this.TickText = this.TickTextDefault;
		this.MeasureTickText = this.MeasureTickTextDefault;
		
		// OnScroll(sender, event);
		this.OnScroll =
		[
		];

		// OnZoomed(sender, event);
		this.OnZoomed =
		[
			function(s,a){ s.m_geom_lines = null; }
		];
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
		Set(this.centre - 0.5*value, this.centre + 0.5*value);
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
		Set(this.min + value - this.centre, this.max + value - this.centre);
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
			for (let i = 0; i != this.OnZoomed.length; ++i)
				this.OnZoomed[i](this, null);
		}
		if (scroll)
		{
			for (let i = 0; i != this.OnScroll.length; ++i)
				this.OnScroll[i](this, null);
		}
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
		return !Maths.FEql(x / this.span, 0.0) ? Util.TrimRight(x.toPrecision(5), ['0']) : "0";
	}

	/**
	 * Measure the tick text width
	 * @param {Canvas2dContext} gfx 
	 * @param {boolean} width True if the width should be returned, false for height
	 * @returns {Number}
	 */
	MeasureTickTextDefault(gfx, width)
	{
		// Can't use 'GridLines' here because creates an infinite recursion.
		// Using TickText(Min/Max, 0.0) causes the axes to jump around.
		// The best option is to use one fixed length string.
		return MeasureString(gfx, "-9.99999", this.options.tick_font);
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
		let span = this.span;
		let step_base = Math.pow(10.0, Math.floor(Math.log10(this.span))), step = step_base;
		let quantisations = [0.05, 0.1, 0.2, 0.25, 0.5, 1.0, 2.0, 5.0, 10.0, 20.0, 50.0];
		for (let i = 0; i != quantisations.length; ++i)
		{
			let s = quantisations[i];
			if (s * span > max_ticks * step_base) continue;
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
			let gl = this.GridLines(axis_length, chart.options.pixels_per_tick);
			let num_lines = Math.floor(2 + (gl.max - gl.min) / gl.step);

			// Create the grid lines at the origin, they get positioned as the camera moves
			let verts = new Float32Array(num_lines * 2);
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
					Util.CopyTo(verts, v, x, y0, 0, 1); v += 4;
					Util.CopyTo(verts, v, x, y1, 0, 1); v += 4;
					x += gl.step;
				}
			}
			if (this === chart.yaxis)
			{
				name = "yaxis_grid";
				let y = 0, x0 = 0, x1 = chart.xaxis.span;
				for (let l = 0; l != num_lines; ++l)
				{
					Util.CopyTo(verts, v, x0, y, 0, 1); v += 4;
					Util.CopyTo(verts, v, x1, y, 0, 1); v += 4;
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
			nuggets.push({topo: gl.LINES, shader: chart.m_rdr.shader_programs["forward"], tint: Util.ColourToV4(chart.options.grid_colour) });

			// Create the model
			let model = Rdr.Model.CreateRaw(chart.m_rdr, verts, null, null, null, indices, nuggets);

			// Create the instance
			this.m_geom_lines = Rdr.Instance.Create(name, model, m4x4.create(), Rdr.EFlags.SceneBoundsExclude|Rdr.EFlags.NoZWrite);
		}

		// Return the graphics model
		return this.m_geom_lines;
	}
}

class Navigation
{
	constructor(chart)
	{
		this.chart = chart;
		this.active = null;
		this.pending = [];
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
		console.log("Mouse Down " + ev);

		// If a mouse op is already active, ignore mouse down
		if (this.active != null)
			return;

		// Look for the mouse op to perform
		if (this.pending[ev.button] == null) //  && DefaultMouseControl
		{
			switch (ev.button)
			{
			default: return;
			case 1: this.pending[ev.button] = new MouseOpDefaultLButton(chart); break;
			//case MouseButtons.Middle: MouseOperations.SetPending(e.Button, new MouseOpDefaultMButton(this)); break;
			//case MouseButtons.Right:  MouseOperations.SetPending(e.Button, new MouseOpDefaultRButton(this)); break;
			}
		}

		// Start the next mouse op
		this.BeginOp(ev.button);
		
		// Get the mouse op, save mouse location and hit test data, then call op.MouseDown()
		var op = this.active;
		if (op != null && !op.cancelled)
		{
			op.btn_down    = true;
			op.grab_client = e.Location; // Note: in ChartControl space, not ChartPanel space
			op.grab_chart  = this.chart.ClientToChart(op.grab_client);
			op.hit_result  = this.chart.HitTestCS(op.grab_client, ModifierKeys, null);
			op.MouseDown(e);
			this.chart.canvas2d.setCapture();
		}
	}
	
	/**
	 * Handle mouse move on the chart
	 * @param {MouseEvent} ev 
	 */
	OnMouseMove(ev)
	{
		console.log("Mouse Move " + ev);
	/*
		// Look for the mouse op to perform
		var op = this.Active;
		if (op != null)
		{
			if (!op.cancelled)
				op.MouseMove(e);
		}
		// Otherwise, provide mouse hover detection
		else
		{
			var hit = HitTestCS(e.Location, ModifierKeys, null);
			var hovered = hit.Hits.Select(x => x.Element).ToHashSet();

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
		}
	*/
	}

	/**
	 * Handle mouse up on the chart
	 * @param {MouseEvent} ev 
	 */
	OnMouseUp(ev)
	{
		console.log("Mouse Up " + ev);
	/*
		// Only release the mouse when all buttons are up
		if (MouseButtons == MouseButtons.None)
			Capture = false;

		// Look for the mouse op to perform
		var op = MouseOperations.Active;
		if (op != null && !op.Cancelled)
			op.MouseUp(e);
		
		MouseOperations.EndOp(e.Button);
	*/
	}	

	/**
	 * Handle mouse wheel changes on the chart
	 * @param {MouseWheelEvent} ev 
	 */
	OnMouseWheel(ev)
	{
		let client_pt = [ev.clientX, ev.clientY];
		let chart_pt = this.chart.ClientToChartPoint(client_pt);
		let chart_area = this.chart.dimensions.chart_area;
		let perp_z = this.chart.options.perpendicular_z_translation != ev.altKey;
		if (Rect.Contains(chart_area, client_pt))
		{
			// If there is a mouse op in progress, ignore the wheel
			var op = this.active;
			if (op == null || op.cancelled)
			{
				// Convert 'client_pt' to normalised camera space
				let nss_point = Rect.NormalisePoint(chart_area, client_pt);

				// Translate the camera along a ray through 'point'
				this.chart.camera.MouseControlZ(nss_point, ev.wheelDelta, !perp_z);
				this.chart.Invalidate();
			}
		}
		else if (this.chart.options.show_axes)
		{
			var scale = 0.001;
			if (ev.shiftKey) scale *= 0.1;
			if (ev.altKey) scale *= 0.01;
			var delta = Maths.Clamp(e.wheelDelta * scale, -0.999, 0.999);

			var chg = false;

			// Change the aspect ratio by zooming on the XAxis
			var xaxis_bounds = Rect.ltrb(chart_area.l, chart_area.b, chart_area.r, this.chart.dimensions.h);
			if (Rect.Contains(xaxis_bounds, client_pt) && !this.chart.xaxis.LockRange)
			{
				if (ModifierKeys == Keys.Control && XAxis.AllowScroll)
				{
					XAxis.Shift(XAxis.Span * delta);
					chg = true;
				}
				if (ModifierKeys != Keys.Control && XAxis.AllowZoom)
				{
					var x = perp_z ? XAxis.Centre : chart_pt.X;
					var left = (XAxis.Min - x) * (1f - delta);
					var rite = (XAxis.Max - x) * (1f - delta);
					XAxis.Set(chart_pt.X + left, chart_pt.X + rite);
					if (Options.LockAspect != null)
						YAxis.Span *= (1f - delta);

					chg = true;
				}
			}

			// Check the aspect ratio by zooming on the YAxis
			var yaxis_bounds = Rectangle.FromLTRB(0, scene_bounds.Top, scene_bounds.Left, scene_bounds.Bottom);
			if (yaxis_bounds.Contains(e.Location) && !YAxis.LockRange)
			{
				if (ModifierKeys == Keys.Control && YAxis.AllowScroll)
				{
					YAxis.Shift(YAxis.Span * delta);
					chg = true;;
				}
				if (ModifierKeys != Keys.Control && YAxis.AllowZoom)
				{
					var y = perp_z ? YAxis.Centre : chart_pt.Y;
					var left = (YAxis.Min - y) * (1f - delta);
					var rite = (YAxis.Max - y) * (1f - delta);
					YAxis.Set(chart_pt.Y + left, chart_pt.Y + rite);
					if (Options.LockAspect != null)
						XAxis.Span *= (1f - delta);

					chg = true;
				}
			}

			// Set the camera position from the Axis ranges
			if (chg)
			{
				SetCameraFromRange();
				Invalidate();
			}
			*/
		}
	}
	/*
		protected override void OnKeyDown(KeyEventArgs e)
		{
			SetCursor();

			var op = MouseOperations.Active;
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

			var op = MouseOperations.Active;
			if (op != null && !e.Handled)
				op.OnKeyUp(e);

			base.OnKeyUp(e);
		}
	*/
};

class MouseOp
{
	constructor()
	{
		this.start_on_mouse_down = true;
		this.cancelled = false;
		this.btn_down = false;
		this.grab_client = null;
		this.grab_chart = null;
		this.hit_result = null;
	}
}





