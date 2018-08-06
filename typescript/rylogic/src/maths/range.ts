import * as Math_ from "./maths";
import Range = Math_.Range;

class RangeImpl implements Range
{
	constructor()
	{
		this.beg = Number.POSITIVE_INFINITY;
		this.end = Number.NEGATIVE_INFINITY;
	}

	/** The inclusive first value of the range */
	public beg: number;

	/** The exclusive last value of the range */
	public end: number;

	/** Return the centre of the range */
	get centre(): number
	{
		return (this.beg + this.end) * 0.5;
	}

	/** Return the size of range, i.e. end - beg */
	get size(): number
	{
		return this.end - this.beg;
	}

	/** Return the beginning of the range, rounded to an integer */
	get begi(): number
	{
		return Math.floor(this.beg);
	}

	/** Return the end of the range, rounded to an integer */
	get endi(): number
	{
		return Math.floor(this.end);
	}

	/** Return the span of this range as an integer */
	get count(): number
	{
		return this.endi - this.begi;
	}
}

/**
 * Create an new, invalid, range: [+MAX_VALUE,-MAX_VALUE)
 */
export function create(beg?: number, end?: number): Range
{
	let out = new RangeImpl();
	if (beg === undefined) beg = Number.POSITIVE_INFINITY;
	if (end === undefined) end = Number.NEGATIVE_INFINITY;
	return set(out, beg, end);
}

/**
 * Set the components of a range
 * @param range
 * @param beg
 * @param end
 */
export function set(range: Range, beg: number, end: number): Range
{
	range.beg = beg;
	range.end = end;
	return range;
}

/**
 * Create a new copy of 'range'
 * @param range The source range to copy
 * @param out Where to write the result
 * @returns The clone of 'range'
 */
export function clone(range: Range, out?: Range): Range
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
 * @param range The range to test for containing 'value'
 * @param value The number to test for being within 'range'
 * @returns True if 'value' is within 'range'
 */
export function Contains(range: Range, value: number): boolean
{
	return range.beg <= value && value < range.end;
}

/**
 * Returns true if 'value' is within the range [Begin,End] (i.e. end inclusive)
 * @param range The range to test for containing 'value'
 * @param value The number to test for being within 'range'
 * @returns True if 'value' is within 'range'
 */
export function ContainsInclusive(range: Range, value: number): boolean
{
	return range.beg <= value && value <= range.end;
}

/**
 * Compare two ranges</summary>
 * -1 if 'range' is less than 'value'
 * +1 if 'range' is greater than 'value'
 * Otherwise 0.
 * @param range
 * @param value
 * @returns -1 if 'range' < 'value', +1 if 'range' > 'value, 0 otherwise
 */
export function Compare(range: Range, value: number): number
{
	return range.end <= value ? -1 : range.beg > value ? +1 : 0;
}

/**
 * Grow the bounds of 'range' to include 'value'
 * @param range The range that is modified to include 'value'
 * @param value The number of include in the range
 */
export function Encompass(range: Range, value: number): void
{
	range.beg = Math.min(range.beg, value);
	range.end = Math.max(range.end, value);
}

/**
 * Grow the bounds of 'range0' to include 'range1'
 * @param {Range} range0
 * @param {Range} range1
 */
export function EncompassRange(range0: Range, range1: Range): void
{
	range0.beg = Math.min(range0.beg, range1.beg);
	range0.end = Math.max(range0.end, range1.end);
}

/**
 * Returns a range that is the union of 'lhs' with 'rhs'
 * (basically the same as 'Encompass' except 'lhs' isn't modified.
 * @param lhs
 * @param rhs
 */
export function Union(lhs: Range, rhs: Range): Range
{
	if (lhs.size < 0) throw new Error("Left range is inside out");
	if (rhs.size < 0) throw new Error("Right range is inside out");
	return create(
		Math.min(lhs.beg, rhs.beg),
		Math.max(lhs.end, rhs.end));
}

/**
 * Returns the intersection of 'lhs' with 'rhs'.
 * If there is no intersection, returns [lhs.beg, lhs.beg) or [lhs.end,lhs.end).
 * Note: this means A.Intersect(B) != B.Intersect(A)
 * @param lhs
 * @param rhs
 */
export function Intersect(lhs: Range, rhs: Range): Range
{
	if (lhs.size < 0) throw new Error("Left range is inside out");
	if (rhs.size < 0) throw new Error("Right range is inside out");
	if (rhs.end <= lhs.beg) return create(lhs.beg, lhs.beg);
	if (rhs.beg >= lhs.end) return create(lhs.end, lhs.end);
	return create(
		Math.max(lhs.beg, rhs.beg),
		Math.min(lhs.end, rhs.end));
}
