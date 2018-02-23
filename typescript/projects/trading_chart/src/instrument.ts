import * as Math_ from "../../../rylogic/src/maths/maths";
import * as Range_ from "../../../rylogic/src/maths/range";
import * as Alg_ from "../../../rylogic/src/common/algorithm";
import * as MC_ from "../../../rylogic/src/common/multicast";
import * as TF_ from "./time_frame";
import Range = Math_.Range;

export interface Candle
{
	/** Timestamp */
	ts: number;

	/** Open price */
	o: number;

	/** Close price */
	c: number;

	/** Highest price */
	h: number;

	/** Lowest price */
	l: number;

	/** Volume traded */
	v: number;
}

/** Trading instrument (currency pair) */
export class Instrument
{
	/**
	 * Create an instrument based on 'currency_pair'
	 * @param currency_pair The current pair, e.g. BTCUSD
	 * @param time_frame The period of each candle
	 * @param epoch_time The time of the first data for the currency pair
	 */
	constructor(currency_pair: string, time_frame: TF_.ETimeFrame, epoch_time: number)
	{
		this.currency_pair = currency_pair;
		this.epoch_time = epoch_time;
		this.time_frame = time_frame;
		this.data = [];

		this.OnDataChanged = new MC_.MulticastDelegate();

		let now = Date.now();
		this._request = null;
		this._request_time = now;
		this._requested_ranges = [];
		this.RequestData(now - TF_.TimeFrameToUnixMS(1000, this.time_frame), now);

		// Poll for data requests
		let This = this;
		window.requestAnimationFrame(function poll()
		{
			This._SubmitRequestData();
			This._PollLatestCandle();
			window.requestAnimationFrame(poll);
		});
	}

	/** The pair that this instrument represents */
	public currency_pair: string;

	/** The unit of time for the instrument data */
	public time_frame: TF_.ETimeFrame;

	/** The earliest valid time for this instrument */
	public epoch_time: number;

	/** The candle data for this instrument */
	public data: number[][];

	/** Return the number of candles in this instrument */
	get count(): number
	{
		return this.data.length;
	}

	/** Return the latest candle */
	get latest(): Candle | null
	{
		let count = this.count;
		return count != 0 ? this.candle(count - 1) : null;
	}

	/** Data changed event */
	public OnDataChanged: MC_.MulticastDelegate<Instrument, IDataChangedArgs>;

	/**
	 * Return the candle at 'idx'. Returns null if 'idx' is out of range.
	 * @param idx The index of the candle to return
	 */
	candle(idx: number): Candle | null
	{
		let candle = this.data[idx];
		if (!candle) return null;
		return { ts: candle[0], o: candle[1], c: candle[2], h: candle[3], l: candle[4], v: candle[5] };
	}

	/**
	 * Enumerate the candles in the index range [beg,end) calling 'cb' for each
	 * @param beg The first (inclusive) index of the candles to return
	 * @param end The last (exclusive) index of the candles to return
	 * @param cb The callback function to pass each candle to
	 */
	candles(beg: number, end: number, cb: (x:Candle)=>void): void
	{
		let ibeg = Math.floor(Math.max(0, beg));
		let iend = Math.floor(Math.min(this.count, end))
		for (let i = ibeg; i < iend; ++i)
			cb(<Candle>this.candle(i));
	}

	/**
	 * Pull candle data
	 * @param beg_time_ms The start time to get candles from (in UTC Unix time ms)
	 * @param end_time_ms The end time to get candles from (in UTC Unix time ms)
	*/
	RequestData(beg_time_ms?: number, end_time_ms?: number): void
	{
		// Determine the range to get
		if (!end_time_ms)
			end_time_ms = beg_time_ms ? beg_time_ms + TF_.TimeFrameToUnixMS(100, this.time_frame) : Date.now();
		if (!beg_time_ms)
			beg_time_ms = end_time_ms ? end_time_ms - TF_.TimeFrameToUnixMS(100, this.time_frame) : Date.now();

		// Clip to the valid time range
		let latest = this.latest;
		beg_time_ms = Math.max(beg_time_ms, this.epoch_time);
		end_time_ms = Math.min(end_time_ms, latest ? latest.ts : Date.now());

		// Trim the request range by the existing pending requests
		for (let i = 0; i != this._requested_ranges.length; ++i)
		{
			let rng = this._requested_ranges[i];
			if (rng.end >= end_time_ms) end_time_ms = Math.min(end_time_ms, rng.beg);
			if (rng.beg <= beg_time_ms) beg_time_ms = Math.max(beg_time_ms, rng.end);
		}

		// If the request is already covered by pending requests, then no need to request again
		if (beg_time_ms >= end_time_ms)
			return;

		// Queue the data request
		this._requested_ranges.push(Range_.create(beg_time_ms, end_time_ms));
	}
	private _requested_ranges: Range[];
	private _request_time: number;
	private _request: XMLHttpRequest | null;

	/**
	 * Returns true if it's too soon to send another data request
	*/
	_LimitRequestRate(): boolean
	{
		// Request in flight?
		if (this._request != null)
			return true;

		// Too soon since the last request?
		const seconds_per_request = 3;
		if (Date.now() - this._request_time < seconds_per_request * 1000)
			return true;

		return false;
	}

	/**
	 * Read the updated data for the latest candle
	*/
	_PollLatestCandle(): void
	{
		// Allowed to send another request?
		if (this._LimitRequestRate())
			return;

		// Request the last candle from bitfinex
		let url = "https://api.bitfinex.com/v2/candles/trade:" +
			TF_.ToBitfinexTimeFrame(this.time_frame) +
			":t" + this.currency_pair +
			"/last";

		// Make the request
		this._SendRequest(url, false);
	}

	/**
	 * Submit requests for data
	 */
	_SubmitRequestData(): void
	{
		// Allowed to send another request?
		if (this._LimitRequestRate())
			return;

		// No requests pending?
		if (this._requested_ranges.length == 0)
			return;

		// Get the next pending request
		let range = this._requested_ranges[0];

		// Request the data from bitfinex
		let url = "https://api.bitfinex.com/v2/candles/trade:" +
			TF_.ToBitfinexTimeFrame(this.time_frame) +
			":t" + this.currency_pair +
			"/hist?start=" + range.beg + "&end=" + range.end;

		// Make the request
		this._SendRequest(url, true);
	}

	/**
	 * Create and send a request
	 * @param url The REST-API URL
	 * @param history True if this is a history request
	 */
	_SendRequest(url: string, history: boolean): void
	{
		let This = this;

		// Make the request
		this._request = new XMLHttpRequest();
		this._request.open("GET", url);
		this._request.ontimeout = function()
		{
			This._request = null;
			This._request_time = Date.now();

			// Remove from the pending request list
			if (history)
				This._requested_ranges.shift();
		};
		this._request.onreadystatechange = function()
		{
			// Not ready yet?
			if (this.readyState != 4)
				return;

			This._request = null;
			This._request_time = Date.now();

			// Remove from the pending request list
			if (history)
				This._requested_ranges.shift();

			// Null or empty response...
			if (this.responseText == null || this.responseText.length == 0)
				return;

			// Read the data
			let data = <number[][]>(history ? JSON.parse(this.responseText) : [JSON.parse(this.responseText)]);
			if (data.length == 0)
				return;

			// Sort by time
			data.sort((l,r) => { return l[0] - r[0]; });

			// Add the data to the instrument
			This._MergeData(data);
		}

		// Send the request
		this._request.send();
	}

	/**
	 * Add 'data' to the instrument data.
	 * @param data The array of candle data to add
	 */
	_MergeData(data: number[][]): void
	{
		// Find the location in the instrument data to insert/add/replace with 'data'
		let beg = data[0];
		let end = data[data.length - 1];
		let ibeg = Alg_.BinarySearch(this.data, function(x) { return x[0] - beg[0]; }, true);
		let iend = Alg_.BinarySearch(this.data, function(x) { return x[0] - end[0]; }, true);
		for (; ibeg > 0 && this.data[ibeg - 1][0] >= beg[0]; --ibeg) { }
		for (; iend < this.data.length && this.data[iend + 0][0] <= end[0]; ++iend) { }

		// Splice the new data into 'this.data'
		this.data = this.data.slice(0, ibeg).concat(data).concat(this.data.slice(iend));

		// Notify of the changed data range.
		// All indices from 'ibeg' to the end are changed
		this.OnDataChanged.invoke(this, { beg: ibeg, end: this.data.length, ofs: data.length - (iend - ibeg) });
	}
}

/** Event args */
export interface IDataChangedArgs
{
	beg: number;
	end: number;
	ofs: number;
}