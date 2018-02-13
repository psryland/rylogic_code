/**
 * @module Range
 */

import * as Maths from "./maths";

/**
 * Constants
 */
const Invalid = make(Number.MAX_SAFE_INTEGER,-Number.MAX_SAFE_INTEGER);

/**
 * Create a zero range: [0,0)
 * @returns {beg,end}
 */
export function create()
{
	return new class
	{
		constructor()
		{
			this.beg = 0;
			this.end = 0;
		}

		/**
		 * Return the centre of the range
		 * @returns {Number}
		 */
		get centre()
		{
			return (this.beg + this.end) * 0.5;
		}

		/**
		 * Return the size of range, i.e. end - beg
		 * @returns {Number}
		 */
		get size()
		{
			return this.end - this.beg;
		}

		/**
		 * Return the beginning of the range, rounded to an integer
		 * @returns {Number}
		 */
		get begi()
		{
			return Math.floor(this.beg);
		}

		/**
		 * Return the end of the range, rounded to an integer
		 * @returns {Number}
		 */
		get endi()
		{
			return Math.floor(this.end);
		}

		/**
		 * Return the span of this range as an integer
		 * @returns {Number}
		 */
		get count()
		{
			return this.endi - this.begi;
		}
	}
}

/**
 * Create a new range from [beg, end)
 * @param {Number} beg The inclusive start of the range
 * @param {Number} end The exclusive end of the range
 * @returns {Range}
 */
export function make(beg, end)
{
	let out = create();
	return set(out, beg, end);
}

/**
 * Set the components of a range
 * @param {Range} range
 * @param {Number} beg 
 * @param {Number} end 
 * @returns {Range}
 */
export function set(range, beg, end)
{
	range.beg = beg;
	range.end = end;
	return range;
}


/**
 * Create a new copy of 'range'
 * @param {Range} range The source range to copy
 * @param {Range} out (optional) The range to write to
 * @returns {Range} The clone of 'range'
 */
export function clone(range, out)
{
	out = out || create();
	if (range !== out)
	{
		out.beg = range.beg;
		out.end = range.end;
	}
	return out;
}

/**
 * Returns true if 'value' is within the range [beg,end) (i.e. end exclusive)
 * @param {Range} range The range to test for containing 'value'
 * @param {Number} value The number to test for being within 'range'
 * @returns {boolean} True if 'value' is within 'range'
 */
export function Contains(range, value)
{
	return range.beg <= value && value < range.end;
}

/**
 * Returns true if 'value' is within the range [Begin,End] (i.e. end inclusive)
 * @param {Range} range The range to test for containing 'value'
 * @param {Number} value The number to test for being within 'range'
 * @returns {boolean} True if 'value' is within 'range'
 */
export function ContainsInclusive(range, value)
{
	return range.beg <= value && value <= range.end;
}

/**
 * Compare two ranges</summary>
 * -1 if 'range' is less than 'value'
 * +1 if 'range' is greater than 'value'
 * Otherwise 0.
 * @param {Range} range
 * @param {Number} value
 * @returns {Number} -1 is 'range' < 'value', +1 if 'range' > 'value, 0 otherwise
 */
export function Compare(range, value)
{
	return range.end <= value ? -1 : range.beg > value ? +1 : 0;
}

/**
 * Grow the bounds of 'range' to include 'value'
 * @param {Range} range
 * @param {Number} value
 */
export function Encompass(range, value)
{
	range.beg = Math.min(range.beg, value);
	range.end = Math.max(range.end, value);
}

/**
 * Grow the bounds of 'range0' to include 'range1'
 * @param {Range} range0
 * @param {Range} range1
 */
export function EncompassRange(range0, range1)
{
	range0.beg = Math.min(range0.beg, range1.beg);
	range0.end = Math.max(range0.end, range1.end);
}

/**
 * Returns a range that is the union of 'lhs' with 'rhs'
 * (basically the same as 'Encompass' except 'lhs' isn't modified.
 * @param {Range} lhs
 * @param {Range} rhs
 * @returns {Range}
 */
export function Union(lhs, rhs)
{
	//Debug.Assert(Size >= 0, "this range is inside out");
	//Debug.Assert(rng.Size >= 0, "'rng' is inside out");
	return make(Math.min(lhs.beg, rhs.beg), Math.max(lhs.end, rhs.end));
}

/**
 * Returns the intersection of 'lhs' with 'rhs'.
 * If there is no intersection, returns [lhs.beg, lhs.beg) or [lhs.end,lhs.end).
 * Note: this means A.Intersect(B) != B.Intersect(A)
 * @param {Range} lhs
 * @param {Range} rhs
 * @returns {Range}
 */
export function Intersect(lhs, rhs)
{
	//Debug.Assert(Size >= 0, "this range is inside out");
	//Debug.Assert(rng.Size >= 0, "'rng' is inside out");
	if (rhs.end <= lhs.beg) return make(lhs.beg, lhs.beg);
	if (rhs.beg >= lhs.end) return make(lhs.end, lhs.end);
	return make(Math.max(lhs.beg, rhs.beg), Math.min(lhs.end, rhs.end));
}
