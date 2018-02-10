/**
 * @module TradingChart
 */

import * as Ry from "../../../lib/rylogic";
import * as TF from "./time_frame";
import * as Instr from "./instrument";
const v4 = Ry.Maths.v4;
const m4x4 = Ry.Maths.m4x4;
const BBox = Ry.Maths.BBox;

export var TradingChart = new function()
{
	this.Create = function(canvas, instrument)
	{
		return new Chart(canvas, instrument);
	}
	this.EXAxisLabelMode = Object.freeze(
	{
		LocalTime: 0,
		UtcTime: 1,
		CandleIndex: 2,
	});
}
export var TimeFrame = TF;
export var ETimeFrame = TF.ETimeFrame;
export var Instrument = Instr;

/**
 * Chart for displaying candle sticks
 */
class Chart
{
	constructor(canvas, instrument)
	{
		let This = this;

		// Save the interface to the chart data
		this.instrument = instrument;
		this.instrument.OnDataChanged.sub(function(s,a){ This._HandleCandleDataChanged(a); });

		// Create a chart instance
		this.chart = Ry.Chart.Create(canvas);
		this.chart.xaxis.options.pixels_per_tick = 50;
		this.chart.xaxis.MeasureTickText = function(gfx){ return This._HandleXAxisMeasureTickText(gfx); };
		this.chart.xaxis.TickText = function(x,step){ return This._HandleChartXAxisLabels(x,step); };
		this.chart.OnRendering.sub(function(s,a){ This._HandleRendering(); });
		this.chart.OnAutoRange.sub(function(s,a){ This._HandleAutoRange(a); });
		this.chart.OnChartClicked.sub(function(s,a){ console.log(a); });

		// Create a candle graphics cache
		this.candles = new CandleCache(This);

		// Options for this chart
		this.options = new Options();
		function Options()
		{
			this.colour_bullish = "#00C000";
			this.colour_bearish = "#C00000";
			this.xaxis_label_mode = TradingChart.EXAxisLabelMode.LocalTime;
		};
	}
	get xaxis()
	{
		return this.chart.xaxis;
	}
	get yaxis()
	{
		return this.chart.yaxis;
	}

	/**
	 * Automatically set the axis ranges to display the latest candles
	 */
	AutoRange()
	{
		this.chart.AutoRange();
	}

	/**
	 * Render the chart
	 */
	Render()
	{
		this.chart.Render();
	}

	/**
	 * Handle rendering
	 */
	_HandleRendering()
	{
		// Add candle graphics for the displayed range
		let instances = this.chart.instances;
		this.candles.Get(this.xaxis.min - 1, this.xaxis.max + 1, function(gfx)
		{
			instances.push(gfx);
		});

		//test_object = test_object || Rdr.CreateTestModel(s.rdr);
		//s.instances.push(test_object);
	}

	/**
	 * Handle auto ranging of chart data
	 * @param {Args} a 
	 */
	_HandleAutoRange(a)
	{
		if (this.instrument == null || this.instrument.count == 0)
			return;

		// Display the last few candles @ N pixels per candle
		let bb = BBox.create();
		const pixels_per_candle = 6;
		let width = a.dims.chart_area.w / pixels_per_candle; // in candles
		let idx_max = this.instrument.count + width * 1 / 5;
		let idx_min = this.instrument.count - width * 4 / 5;
		this.instrument.candles(idx_min, idx_max, function(candle)
		{
			BBox.EncompassPoint(bb, v4.make(idx_min, candle.l, 0, 1), bb);
			BBox.EncompassPoint(bb, v4.make(idx_max, candle.h, 0, 1), bb);
		});

		if (!bb.is_valid)
			return;

		// Swell the box a little for margins
		v4.set(bb.radius, bb.radius[0], bb.radius[1] * 1.1, bb.radius[2], 0);
		BBox.EncompassBBox(a.view_bbox, bb, a.view_bbox);
		a.handled = true;
	}

	/**
	 * Convert the XAxis values into pretty datetime strings
	 */
	_HandleChartXAxisLabels(x, step)
	{
		// Note:
		// The X axis is not a linear time axis because the markets may not be online all the time.
		// Use 'Instrument.TimeToIndexRange' to convert time values to X Axis values.

		// Draw the X Axis labels as indices instead of time stamps
		if (this.options.xaxis_label_mode == TradingChart.EXAxisLabelMode.CandleIndex)
			return x.ToString();
		if (this.instrument == null || this.instrument.count == 0)
			return "";

		// If the ticks are within the range of instrument data, use the actual time stamp.
		// This accounts for missing candles in the data range.
		var prev = Math.floor(x - step);
		var curr = Math.floor(x);

		// If the current tick mark represents the same candle as the previous one, no text is required
		if (prev == curr)
			return "";

		// The range of indices
		var first = 0;
		var last  = this.instrument.count;
		var candle_beg = this.instrument.candle(first);
		var candle_end = this.instrument.candle(last-1);

		// Get the date/time for the tick
		var dt_curr = 
			curr >= first && curr < last ? this.instrument.candle(curr).ts :
			curr < first ? candle_beg.ts - TF.TimeFrameToUnixMS(first - curr, this.instrument.time_frame) :
			curr >= last ? candle_end.ts + TF.TimeFrameToUnixMS(curr - last + 1, this.instrument.time_frame) :
			null;
		var dt_prev =
			prev >= first && prev < last ? this.instrument.candle(prev).ts :
			prev < first ? candle_beg.ts - TF.TimeFrameToUnixMS(first - prev, this.instrument.time_frame) :
			prev >= last ? candle_end.ts + TF.TimeFrameToUnixMS(prev - last + 1, this.instrument.time_frame) :
			null;

		// Display in local time zone
		var locale_time = this.options.xaxis_label_mode == TradingChart.EXAxisLabelMode.LocalTime;

		// First tick on the x axis
		var first_tick = curr == first || prev < first || x - step < this.xaxis.min;

		// Show more of the time stamp depending on how it differs from the previous time stamp
		return TF.ShortTimeString(dt_curr, dt_prev, locale_time, first_tick);
	}

	/**
	 * Return the space to reserve for each XAxis tick label
	 */
	_HandleXAxisMeasureTickText(gfx)
	{
		let sz = this.xaxis.MeasureTickTextDefault(gfx);
		sz.height *= 2;
		return sz;
	}

	/**
	 * Handle the instrument data changing
	 * @param {Args} args 
	 */
	_HandleCandleDataChanged(args)
	{
		this.candles.InvalidateRange(args.beg, args.end);
	}
}

class CandleCache
{
	constructor(chart)
	{
		this.BatchSize = 1024;
		this.chart = chart;
		this.cache = {};

		// Recycle buffers for vertex, index, and nugget data
		this.m_vbuf = [];
		this.m_ibuf = [];
		this.m_nbuf = [];
	}

	/**
	 * Enumerate the candle graphics instances over the given candle index range
	 * @param {Number} beg The first candle index
	 * @param {Number} end The last candle index
	 * @param {function} cb Callback with the candle graphics
	 */
	Get(beg, end, cb)
	{
		// Convert the candle indices to cache indices
		let idx0 = Math.floor(beg / this.BatchSize);
		let idx1 = Math.floor(end / this.BatchSize);
		for (let i = idx0; i <= idx1; ++i)
		{
			// Get the graphics model at 'i'
			let gfx = this.At(i);
			if (gfx.inst == null)
				continue;

			// Position the graphics object
			gfx.inst.o2w = m4x4.Translation([gfx.candle_range.beg, 0, 0, 1]);
			cb(gfx.inst);
		}
	}

	/**
	 * Get the candle graphics at 'cache_idx'
	 * @param {Number} cache_idx The index into the cache of graphics objects
	 * @returns {gfx, candle_range}
	 */
	At(cache_idx)
	{
		// Look in the cache first
		let gfx = this.cache[cache_idx];
		if (gfx) return gfx;

		// On miss, generate the graphics model for the data range [idx, idx + min(BatchSize, Count-idx))
		// where 'idx' is the candle index of the first candle in the batch.
		let rdr = this.chart.chart.rdr;
		let instrument = this.chart.instrument;
		let colour_bullish = Ry.Util.ColourToV4(this.chart.options.colour_bullish);
		let colour_bearish = Ry.Util.ColourToV4(this.chart.options.colour_bearish);
		let shader = rdr.shader_programs.forward;

		// Get the candle range from the cache index
		let candle_range = Ry.Range.make((cache_idx+0) * this.BatchSize, (cache_idx+1) * this.BatchSize);
		candle_range.beg = Ry.Maths.Clamp(candle_range.beg, 0, instrument.count);
		candle_range.end = Ry.Maths.Clamp(candle_range.end, 0, instrument.count);
		if (candle_range.count == 0)
			return this.cache[cache_idx] = new CandleGfx(null, candle_range);

		// Use TriList for the bodies, and LineList for the wicks.
		// So:    6 indices for the body, 4 for the wicks
		//   __|__
		//  |\    |
		//  |  \  |
		//  |____\|
		//     |

		// Divide the index buffer into [bodies, wicks]

		// Resize the cache buffers
		let count = candle_range.count;
		this.m_vbuf.length = 8 * count;
		this.m_ibuf.length = (6+4) * count;
		this.m_nbuf.length = 2;

		// Index of the first body index and the first wick index.
		let vert = 0;
		let body = 0;
		let wick = 6 * count;
		let nugt = 0;

		// Create the geometry
		for (let candle_idx = 0; candle_idx != count;)
		{
			let candle = instrument.candle(candle_idx);

			// Create the graphics with the first candle at x == 0
			let x = candle_idx++;
			let o = Math.max(candle.o, candle.c);
			let h = candle.h;
			let l = candle.l;
			let c = Math.min(candle.o, candle.c);
			let col = candle.c > candle.o ? colour_bullish : candle.c < candle.o ? colour_bearish : 0xFFA0A0A0;
			let v = vert;

			// Candle verts
			this.m_vbuf[vert++] = {pos: [x       , h, 0, 1], col:col};
			this.m_vbuf[vert++] = {pos: [x       , o, 0, 1], col:col};
			this.m_vbuf[vert++] = {pos: [x - 0.4 , o, 0, 1], col:col};
			this.m_vbuf[vert++] = {pos: [x + 0.4 , o, 0, 1], col:col};
			this.m_vbuf[vert++] = {pos: [x - 0.4 , c, 0, 1], col:col};
			this.m_vbuf[vert++] = {pos: [x + 0.4 , c, 0, 1], col:col};
			this.m_vbuf[vert++] = {pos: [x       , c, 0, 1], col:col};
			this.m_vbuf[vert++] = {pos: [x       , l, 0, 1], col:col};

			// Candle body
			this.m_ibuf[body++] = (v + 3);
			this.m_ibuf[body++] = (v + 2);
			this.m_ibuf[body++] = (v + 4);
			this.m_ibuf[body++] = (v + 4);
			this.m_ibuf[body++] = (v + 5);
			this.m_ibuf[body++] = (v + 3);

			// Candle wick
			if (o != c)
			{
				this.m_ibuf[wick++] = (v + 0);
				this.m_ibuf[wick++] = (v + 1);
				this.m_ibuf[wick++] = (v + 6);
				this.m_ibuf[wick++] = (v + 7);
			}
			else
			{
				this.m_ibuf[wick++] = (v + 0);
				this.m_ibuf[wick++] = (v + 7);
				this.m_ibuf[wick++] = (v + 2);
				this.m_ibuf[wick++] = (v + 5);
			}
		}

		this.m_nbuf[nugt++] = { topo: rdr.TRIANGLES, shader:shader, vrange:{ofs:0, count:vert}, irange:{ofs:0, count:body} };
		this.m_nbuf[nugt++] = { topo: rdr.LINES, shader:shader, vrange:{ofs:0, count:vert}, irange:{ofs:body, count:wick - body} };

		// Create the graphics
		let model = Ry.Rdr.Model.Create(rdr, this.m_vbuf, this.m_ibuf, this.m_nbuf);
		let inst = Ry.Rdr.Instance.Create("Candles-["+candle_range.beg+","+candle_range.end+")", model);
		return this.cache[cache_idx] = new CandleGfx(inst, candle_range);

		// Cache element
		function CandleGfx(inst, candle_range)
		{
			this.inst = inst;
			this.candle_range = candle_range;
		}
	}

	/**
	 * Invalidate the graphics models for the given candle index range
	 * @param {Number} beg The first invalid candle index
	 * @param {Number} end One past the last invalid candle index 
	 */
	InvalidateRange(beg, end)
	{
		let idx0 = Math.floor(beg / this.BatchSize);
		let idx1 = Math.floor(end / this.BatchSize);
		for (let i = idx0; i <= idx1; ++i)
		{
			if (!this.cache[i]) continue;
			this.cache[i] = null;
		}
	}
}


