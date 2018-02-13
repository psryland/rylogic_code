/**
 * @module Instrument
 */

import * as Ry from "../../../lib/rylogic";
import * as TF from "./time_frame";

var Range = Ry.Maths.Range;

/**
 * Create an instrument based on 'currency_pair'
 * @param {String} currency_pair The current pair, e.g. BTCUSD
 * @param {ETimeFrame} time_frame The period of each candle
 * @param {Number} epoch_time The time of the first data for the currency pair
 * @returns {Instrument}
 */
export function Create(currency_pair, time_frame, epoch_time)
{
	return new class
	{
		constructor()
		{
			this.currency_pair = currency_pair;
			this.epoch_time = epoch_time;
			this.time_frame = time_frame;
			this.data = [];

			this.OnDataChanged = new Ry.Util.MulticastDelegate();

			let now = Date.now();
			this.m_request = null;
			this.m_request_time = now;
			this.m_requested_ranges = [];
			this.RequestData(now - TF.TimeFrameToUnixMS(1000, this.time_frame), now);
			
			// Poll for data requests
			let This = this;
			window.requestAnimationFrame(function poll()
			{
				This._SubmitRequestData();
				This._PollLatestCandle();
				window.requestAnimationFrame(poll);
			});
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
		 * Return the latest candle
		 * @returns {Candle}
		 */
		get latest()
		{
			let count = this.count;
			return count != 0 ? this.candle(count - 1) : null;
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

			// Clip to the valid time range
			let latest = this.latest;
			beg_time_ms = Math.max(beg_time_ms, this.epoch_time);
			end_time_ms = Math.min(end_time_ms, latest ? latest.ts : Date.now());

			// Trim the request range by the existing pending requests
			for (let i = 0; i != this.m_requested_ranges.length; ++i)
			{
				let rng = this.m_requested_ranges[i];
				if (rng.end >= end_time_ms) end_time_ms = Math.min(end_time_ms, rng.beg);
				if (rng.beg <= beg_time_ms) beg_time_ms = Math.max(beg_time_ms, rng.end);
			}

			// If the request is already covered by pending requests, then no need to request again
			if (beg_time_ms >= end_time_ms)
				return;

			// Queue the data request
			this.m_requested_ranges.push(Range.make(beg_time_ms, end_time_ms));
		}

		/**
		 * Returns true if it's too soon to send another data request
		 * @returns {boolean}
		*/
		_LimitRequestRate()
		{
			// Request in flight?
			if (this.m_request != null)
				return true;

			// Too soon since the last request?
			const seconds_per_request = 3;
			if (Date.now() - this.m_request_time < seconds_per_request * 1000)
				return true;

			return false;
		}

		/**
		 * Read the updated data for the latest candle
		*/
		_PollLatestCandle()
		{
			// Allowed to send another request?
			if (this._LimitRequestRate())
				return;

			// Request the last candle from bitfinex
			let url = "https://api.bitfinex.com/v2/candles/trade:"+
				TimeFrame.ToBitfinexTimeFrame(this.time_frame)+
				":t"+this.currency_pair+
				"/last";

			// Make the request
			this._SendRequest(url, false);
		}

		/**
		 * Submit requests for data
		 */
		_SubmitRequestData()
		{
			// Allowed to send another request?
			if (this._LimitRequestRate())
				return;

			// No requests pending?
			if (this.m_requested_ranges.length == 0)
				return;

			// Get the next pending request
			let range = this.m_requested_ranges[0];

			// Request the data from bitfinex
			let url = "https://api.bitfinex.com/v2/candles/trade:"+
				TimeFrame.ToBitfinexTimeFrame(this.time_frame)+
				":t"+this.currency_pair+
				"/hist?start="+range.beg+"&end="+range.end;

			// Make the request
			this._SendRequest(url, true);
		}

		/**
		 * Create and send a request
		 * @param {string} url The REST API url
		 * @param {boolean} history True if this is a history request
		 */
		_SendRequest(url, history)
		{
			let This = this;

			// Make the request
			this.m_request = new XMLHttpRequest();
			this.m_request.open("GET", url);
			this.m_request.ontimeout = function()
			{
				This.m_request = null;
				This.m_request_time = Date.now();

				// Remove from the pending request list
				if (history)
					This.m_requested_ranges.shift();
			};
			this.m_request.onreadystatechange = function()
			{
				// Not ready yet?
				if (this.readyState != 4)
					return;

				This.m_request = null;
				This.m_request_time = Date.now();

				// Remove from the pending request list
				if (history)
					This.m_requested_ranges.shift();

				// Null or empty response...
				if (this.responseText == null || this.responseText.length == 0)
					return;

				// Read the data
				let data = JSON.parse(this.responseText);
				if (!history) data = [data];
				if (data.length == 0)
					return;

				// Sort by time
				data.sort(function(l,r) { return l[0] - r[0]; });

				// Add the data to the instrument
				This._MergeData(data);
			}

			// Send the request
			this.m_request.send();
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
			let ibeg = Ry.Alg.BinarySearch(this.data, function(x){ return x[0] - beg[0]; }, true);
			let iend = Ry.Alg.BinarySearch(this.data, function(x){ return x[0] - end[0]; }, true);
			for (;ibeg > 0                && this.data[ibeg-1][0] >= beg[0]; --ibeg) {}
			for (;iend < this.data.length && this.data[iend+0][0] <= end[0]; ++iend) {}

			// Splice the new data into 'this.data'
			Array.prototype.splice.apply(this.data, [ibeg, iend - ibeg].concat(data));

			// Notify of the changed data range.
			// All indices from 'ibeg' to the end are changed
			this.OnDataChanged.invoke(this, {beg:ibeg, end:this.data.length, ofs:data.length - (iend-ibeg)});
		}
	};
}
