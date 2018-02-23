import * as Math_ from "../../../rylogic/src/maths/maths";
import * as M4x4_ from "../../../rylogic/src/maths/m4x4";
import * as Vec4_ from "../../../rylogic/src/maths/v4";
import * as BBox_ from "../../../rylogic/src/maths/bbox";
import * as Range_ from "../../../rylogic/src/maths/range";
import * as Rdr_ from "../../../rylogic/src/renderer/renderer";
import * as Util_ from "../../../rylogic/src/utility/util";
import * as Chart_ from "../../../rylogic/src/gui/chart";
import * as Instr_ from "./instrument";
import * as TF_ from "./time_frame";
import Instrument = Instr_.Instrument;
import Chart = Chart_.Chart;
import Range = Math_.Range;
import Size = Math_.Size;

export { Instr_, TF_, Rdr_ };

/** The label mode for the X axis */
export enum EXAxisLabelMode
{
	LocalTime = 0,
	UtcTime = 1,
	CandleIndex = 2,
}

/** Chart for displaying candle sticks */
export class TradingChart
{
	/**
	 * Create a currency/stock trading chart.
	 * @param canvas The canvas to draw the chart on
	 * @param instrument The instrument that provides the data for the chart
	 */
	constructor(canvas: HTMLCanvasElement, instrument: Instrument)
	{
		let This = this;

		// Options for this chart
		this.options = new Options();

		// Save the interface to the chart data
		this.instrument = instrument;
		this.instrument.OnDataChanged.sub(function(_,a){ This._HandleCandleDataChanged(a); });

		// Create a chart instance
		this.chart = new Chart(canvas);
		this.chart.xaxis.options.pixels_per_tick = 50;
		this.chart.xaxis.MeasureTickText = function(gfx){ return This._HandleXAxisMeasureTickText(gfx); };
		this.chart.xaxis.TickText = function(x,step){ return This._HandleChartXAxisLabels(x,step); };
		this.chart.OnRendering.sub(function(){ This._HandleRendering(); });
		this.chart.OnAutoRange.sub(function(_,a){ This._HandleAutoRange(a); });
		this.chart.OnChartMoved.sub(function(){ This._HandleChartMoved(); });
		this.chart.OnChartClicked.sub(function(_s,a){ console.log(a); });

		// Create a candle graphics cache
		this._candles = new Cache(this);
	}

	/** The trading instrument displayed in this chart */
	public instrument: Instrument;

	/** Chart rendering options */
	public options: Options;

	/** The Rylogic chart control */
	public chart: Chart;

	/** The cache of candle graphics */
	private _candles: Cache;

	/** The X axis of the chart */
	get xaxis(): Chart_.Axis
	{
		return this.chart.xaxis;
	}

	/** The Y axis of the chart */
	get yaxis(): Chart_.Axis
	{
		return this.chart.yaxis;
	}

	/** Automatically set the axis ranges to display the latest candles */
	AutoRange(): void
	{
		this.chart.AutoRange();
	}

	/** Render the chart */
	Render(): void
	{
		this.chart.Render();
	}

	/** Handle rendering */
	_HandleRendering(): void
	{
		// Add candle graphics for the displayed range
		let instances = this.chart.instances;
		this._candles.Get(this.xaxis.min - 1, this.xaxis.max + 1, (gfx) =>
		{
			if (gfx.inst == null)
				return;

			// Position the graphics
			gfx.inst.o2w = M4x4_.Translation([gfx.candle_range.beg, 0, 0, 1]);
			instances.push(gfx.inst);
		});
	}

	/** Handle auto ranging of chart data */
	_HandleAutoRange(a: Chart_.IAutoRangeArgs): void
	{
		if (this.instrument == null || this.instrument.count == 0)
			return;

		// Display the last few candles @ N pixels per candle
		let bb = BBox_.create();
		const pixels_per_candle = 6;
		let width = a.dims.chart_area.w / pixels_per_candle; // in candles
		let idx_max = this.instrument.count + width * 1 / 5;
		let idx_min = this.instrument.count - width * 4 / 5;
		this.instrument.candles(idx_min, idx_max, function(candle)
		{
			BBox_.EncompassPoint(bb, Vec4_.create(idx_min, candle.l, 0, 1), bb);
			BBox_.EncompassPoint(bb, Vec4_.create(idx_max, candle.h, 0, 1), bb);
		});
		if (!bb.is_valid)
			return;

		// Swell the box a little for margins
		Vec4_.set(bb.radius, bb.radius[0], bb.radius[1] * 1.1, bb.radius[2], 0);
		BBox_.EncompassBBox(a.view_bbox, bb, a.view_bbox);
		a.handled = true;
	}

	/** Handle the chart zooming or scrolling */
	_HandleChartMoved(): void
	{
		// See if the displayed X axis range is available in
		// the chart data. If not, request it in the instrument.
		if (this.instrument.count != 0)
		{
			let first = <Instr_.Candle>this.instrument.candle(0);

			// Request data for the missing regions.
			if (this.chart.xaxis.min < 0)
			{
				// Request candles before 'first'
				let num = Math.max(0 - this.chart.xaxis.min, this._candles.BatchSize*10);
				let period_ms = TF_.TimeFrameToUnixMS(num, this.instrument.time_frame);
				this.instrument.RequestData(first.ts - period_ms, first.ts);
			}
		}
	}

	/** Convert the XAxis values into pretty datetime strings */
	_HandleChartXAxisLabels(x: number, step: number): string
	{
		// Note:
		// The X axis is not a linear time axis because the markets may not be online all the time.
		// Use 'Instrument.TimeToIndexRange' to convert time values to X Axis values.

		// Draw the X Axis labels as indices instead of time stamps
		if (this.options.xaxis_label_mode == EXAxisLabelMode.CandleIndex)
			return this.chart.xaxis.TickTextDefault(x);
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
		var candle_beg = <Instr_.Candle>this.instrument.candle(first);
		var candle_end = <Instr_.Candle>this.instrument.latest;

		// First tick on the x axis
		var first_tick = curr == first || prev < first || x - step < this.xaxis.min;

		// Get the date/time for the tick
		var dt_curr = 
			curr >= first && curr < last ? (<Instr_.Candle>this.instrument.candle(curr)).ts :
			curr < first ? candle_beg.ts - TF_.TimeFrameToUnixMS(first - curr, this.instrument.time_frame) :
			curr >= last ? candle_end.ts + TF_.TimeFrameToUnixMS(curr - last + 1, this.instrument.time_frame) :
			0;
		var dt_prev =
			first_tick ? null :
			prev >= first && prev < last ? (<Instr_.Candle>this.instrument.candle(prev)).ts :
			prev < first ? candle_beg.ts - TF_.TimeFrameToUnixMS(first - prev, this.instrument.time_frame) :
			prev >= last ? candle_end.ts + TF_.TimeFrameToUnixMS(prev - last + 1, this.instrument.time_frame) :
			0;

		// Display in local time zone
		var locale_time = this.options.xaxis_label_mode == EXAxisLabelMode.LocalTime;

		// Show more of the time stamp depending on how it differs from the previous time stamp
		return TF_.ShortTimeString(dt_curr, dt_prev, locale_time);
	}

	/** Return the space to reserve for each XAxis tick label */
	_HandleXAxisMeasureTickText(gfx: CanvasRenderingContext2D): Size
	{
		let sz = this.xaxis.MeasureTickTextDefault(gfx);
		sz[1] *= 2;
		return sz;
	}

	/** Handle the instrument data changing */
	_HandleCandleDataChanged(a: Instr_.IDataChangedArgs): void
	{
		// Invalidate the graphics for the candles over the range
		this._candles.InvalidateRange(a.beg, a.end);

		// Shift the X axis range so that the chart doesn't jump
		if (this.chart.xaxis.max > a.beg)
		{
			this.chart.xaxis.Shift(a.ofs);
			this.chart.SetCameraFromRange();
		}

		// Invalidate the chart
		this.chart.Invalidate();
	}
}

/** Chart rendering options */
export class Options
{
	constructor()
	{
		this.colour_bullish = "#00C000";
		this.colour_bearish = "#C00000";
		this.xaxis_label_mode = EXAxisLabelMode.LocalTime;
	}

	public colour_bullish: string;
	public colour_bearish: string;
	public xaxis_label_mode: EXAxisLabelMode;
}

/** The cache of candle graphics */
class Cache
{
	constructor(trading_chart: TradingChart)
	{
		this.trading_chart = trading_chart;
		this._cache = {};

		// Recycle buffers for vertex, index, and nugget data
		this._vbuf = [];
		this._ibuf = [];
		this._nbuf = [];
	}

	/** The number of candles per batch */
	get BatchSize(): number
	{
		return 1024;
	}

	/** The owning trading chart */
	public trading_chart: TradingChart;

	/** The cache of candle graphics */
	private _cache: { [_: number]: CandleGfx|null };

	private _vbuf: Rdr_.Model.IVertex[];
	private _ibuf: number[];
	private _nbuf: Rdr_.Model.INugget[];

	/**
	* Enumerate the candle graphics instances over the given candle index range
	* @param beg The first candle index
	* @param end The last candle index
	* @param cb Callback with the candle graphics
	*/
	Get(beg: number, end: number, cb: (_: CandleGfx) => void): void
	{
		// Convert the candle indices to cache indices
		let idx0 = Math.floor(beg / this.BatchSize);
		let idx1 = Math.floor(end / this.BatchSize);
		for (let i = idx0; i <= idx1; ++i)
		{
			// Get the graphics model at 'i'
			if (i < 0) continue;
			let gfx = this.At(i);
			cb(gfx);
		}
	}

	/**
	* Get the candle graphics at 'cache_idx'
	* @param cache_idx The index into the cache of graphics objects
	*/
	At(cache_idx: number): CandleGfx
	{
		// Look in the cache first
		let gfx = this._cache[cache_idx];
		if (gfx) return gfx;

		// On miss, generate the graphics model for the data range [idx, idx + min(BatchSize, Count-idx))
		// where 'idx' is the candle index of the first candle in the batch.
		let rdr = this.trading_chart.chart.gfx3d;
		let instrument = this.trading_chart.instrument;
		let colour_bullish = Util_.ColourToV4(this.trading_chart.options.colour_bullish);
		let colour_bearish = Util_.ColourToV4(this.trading_chart.options.colour_bearish);
		let colour_doji = Util_.ColourUintToV4(0xFFA0A0A0);
		let shader = rdr.shader_programs.forward;

		// Get the candle range from the cache index
		let candle_range = Range_.create((cache_idx+0) * this.BatchSize, (cache_idx+1) * this.BatchSize);
		candle_range.beg = Math_.Clamp(candle_range.beg, 0, instrument.count);
		candle_range.end = Math_.Clamp(candle_range.end, 0, instrument.count);
		if (candle_range.count == 0)
			return this._cache[cache_idx] = new CandleGfx(null, candle_range);

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
		this._vbuf.length = 8 * count;
		this._ibuf.length = (6+4) * count;
		this._nbuf.length = 2;

		// Index of the first body index and the first wick index.
		let vert = 0;
		let body = 0;
		let wick = 6 * count;
		let nugt = 0;

		// Create the geometry
		for (let candle_idx = 0; candle_idx != count;)
		{
			let candle = <Instr_.Candle>instrument.candle(candle_range.beg + candle_idx);

			// Create the graphics with the first candle at x == 0
			let x = candle_idx++;
			let o = Math.max(candle.o, candle.c);
			let h = candle.h;
			let l = candle.l;
			let c = Math.min(candle.o, candle.c);
			let col = candle.c > candle.o ? colour_bullish : candle.c < candle.o ? colour_bearish : colour_doji;
			let v = vert;

			// Candle verts
			this._vbuf[vert++] = {pos: [x       , h, 0, 1], col:col};
			this._vbuf[vert++] = {pos: [x       , o, 0, 1], col:col};
			this._vbuf[vert++] = {pos: [x - 0.4 , o, 0, 1], col:col};
			this._vbuf[vert++] = {pos: [x + 0.4 , o, 0, 1], col:col};
			this._vbuf[vert++] = {pos: [x - 0.4 , c, 0, 1], col:col};
			this._vbuf[vert++] = {pos: [x + 0.4 , c, 0, 1], col:col};
			this._vbuf[vert++] = {pos: [x       , c, 0, 1], col:col};
			this._vbuf[vert++] = {pos: [x       , l, 0, 1], col:col};

			// Candle body
			this._ibuf[body++] = (v + 3);
			this._ibuf[body++] = (v + 2);
			this._ibuf[body++] = (v + 4);
			this._ibuf[body++] = (v + 4);
			this._ibuf[body++] = (v + 5);
			this._ibuf[body++] = (v + 3);

			// Candle wick
			if (o != c)
			{
				this._ibuf[wick++] = (v + 0);
				this._ibuf[wick++] = (v + 1);
				this._ibuf[wick++] = (v + 6);
				this._ibuf[wick++] = (v + 7);
			}
			else
			{
				this._ibuf[wick++] = (v + 0);
				this._ibuf[wick++] = (v + 7);
				this._ibuf[wick++] = (v + 2);
				this._ibuf[wick++] = (v + 5);
			}
		}

		this._nbuf[nugt++] = { topo: rdr.webgl.TRIANGLES, shader:shader, vrange:{ofs:0, count:vert}, irange:{ofs:0, count:body} };
		this._nbuf[nugt++] = { topo: rdr.webgl.LINES, shader:shader, vrange:{ofs:0, count:vert}, irange:{ofs:body, count:wick - body} };

		// Create the graphics
		let model = Rdr_.Model.Create(rdr, this._vbuf, this._ibuf, this._nbuf);
		let inst = Rdr_.Instance.Create("Candles-["+candle_range.beg+","+candle_range.end+")", model);
		let candle_graphics = new CandleGfx(inst, candle_range);
		return this._cache[cache_idx] = candle_graphics;
	}

	/**
	* Invalidate the graphics models for the given candle index range
	* @param beg The first invalid candle index
	* @param end One past the last invalid candle index 
	*/
	InvalidateRange(beg: number, end: number): void
	{
		let idx0 = Math.floor(beg / this.BatchSize);
		let idx1 = Math.floor(end / this.BatchSize);
		for (let i = idx0; i <= idx1; ++i)
		{
			if (!this._cache[i]) continue;
			this._cache[i] = null;
		}
	}
}

/** The cache element */
class CandleGfx
{
	constructor(inst: Rdr_.Instance.IInstance | null, candle_range: Range)
	{
		this.inst = inst;
		this.candle_range = candle_range;
	}

	/** The candle graphics instance */
	inst: Rdr_.Instance.IInstance | null;

	/** The range of candle data covered by this graphics instance */
	candle_range: Range;
}
