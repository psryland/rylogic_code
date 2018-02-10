/**
 * @module Instrument
 */

import * as Ry from "../../../lib/rylogic";
import * as TF from "./time_frame";

/**
 * Create an instrument based on 'currency_pair'
 * @param {String} currency_pair The current pair, e.g. BTCUSD
 * @param {ETimeFrame} time_frame The period of each candle
 * @returns {Instrument}
 */
export function Create(currency_pair, time_frame)
{
	return new class
	{
		constructor()
		{
			this.currency_pair = currency_pair;
			this.time_frame = time_frame;
			this.data = [];

			this.OnDataChanged = new Ry.Util.MulticastDelegate();

			let now = Date.now();
			RequestData(now - TF.TimeFrameToUnixMS(1000, this.time_frame), now);
		}

		/**
		 * Return the number of candles in this instrument
		 * @returns {Number}
		 */
		get count()
		{
			return this.data.length;
		}

		/**
		 * Return the candle at 'idx'. Returns null if 'idx' is out of range.
		 * @param {Number} idx 
		 * @returns {Candle}
		 */
		candle(idx)
		{
			let candle = this.data[idx];
			if (!candle) return null;
			return {ts:candle[0], o:candle[1], c:candle[2], h:candle[3], l:candle[4], v:candle[5]};
		}

		/**
		 * Enumerate the candles in the index range [beg,end) calling 'cb' for each
		 * @param {Number} beg 
		 * @param {Number} end 
		 * @param {Function} cb 
		 */
		candles(beg, end, cb)
		{
			let ibeg = Math.floor(Math.max(0, beg));
			let iend = Math.floor(Math.min(this.count, end))
			for (let i = ibeg; i < iend; ++i)
				cb(this.candle(i));
		}

		/**
		 * Pull candle data
		 * @param {Number} beg_time_ms (optional) The start time to get candles from (in UTC Unix time ms)
		 * @param {Number} end_time_ms (optional) The end time to get candles from (in UTC Unix time ms)
		*/
		RequestData(beg_time_ms, end_time_ms)
		{
			// Determine the range to get
			if (end_time_ms == null)
				end_time_ms = beg_time_ms ? beg_time_ms + TF.TimeFrameToUnixMS(100, this.time_frame) : Date.now();
			if (beg_time_ms == null)
				beg_time_ms = end_time_ms ? end_time_ms - TF.TimeFrameToUnixMS(100, this.time_frame) : Date.now();

			// Request the initiqal data from bitfinex
			let url = "https://api.bitfinex.com/v2/candles/trade:"+
				TimeFrame.ToBitfinexTimeFrame(this.time_frame)+
				":t"+this.currency_pair+
				"/hist?start="+beg_time_ms+"&end="+end_time_ms;

			var request = new XMLHttpRequest();
			request.open("GET", url);
			request.onreadystatechange = function()
			{
				// Not ready yet?
				if (request.readyState != 4)
					return;

				// Read and sort the data by time
				let data = JSON.parse(request.responseText);
				data.sort(function(l,r) { return l[0] - r[0]; });
				if (data.length == 0)
					return;

				// Add the data to the instrument
				this._MergeData(data);
			}
			request.send();
		}

		/**
		 * Add 'data' to the instrument data.
		 * @param {Array} data 
		 */
		_MergeData(data)
		{
			// Find the location in the instrument data to insert/add/replace with 'data'
			let beg = data[0];
			let end = data[data.length-1];
			let ibeg = Ry.Alg.BinarySearch(this.data, function(x){ return beg[0] - x[0]; }, true);
			let iend = Ry.Alg.BinarySearch(this.data, function(x){ return end[0] - x[0]; }, true);
			for (;ibeg > 0                && this.data[ibeg-1][0] >= beg[0]; --ibeg) {}
			for (;iend < this.data.length && this.data[iend+0][0] <= end[0]; ++iend) {}

			// Splice the new data into 'this.data'
			Array.prototype.splice.apply(this.data, [ibeg, iend - ibeg].concat(data));

			// Notify of the changed data range.
			// All indices from 'ibeg' to the end are changed
			this.OnDataChanged.invoke(this, {beg:ibeg, end:this.data.length});
		}
	};
}
