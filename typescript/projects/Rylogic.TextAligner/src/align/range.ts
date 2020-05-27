export class Range
{
	constructor(beg:number=Number.POSITIVE_INFINITY, end:number=Number.NEGATIVE_INFINITY)
	{
		this.beg = beg;
		this.end = end;
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

	/** Return the beginning of the range, rounded down to an integer */
	get begi(): number
	{
		return Math.floor(this.beg);
	}

	/** Return the end of the range, rounded down to an integer */
	get endi(): number
	{
		return Math.floor(this.end);
	}

	/** Return the span of this range as an integer */
	get count(): number
	{
		return this.endi - this.begi;
	}

	/**
	 * Returns true if 'value' is within this range [beg,end) (i.e. end exclusive)
	 * @param value The number to test for being within this range
	 * @returns True if 'value' is within this range
	 */
	contains(value: number): boolean
	{
		return value >= this.beg && value < this.end;
	}

	/**
	 * Returns true if 'value' is within this range [Begin,End] (i.e. end inclusive)
	 * @param value The number to test for being within this range
	 * @returns True if 'value' is within this range
	 */
	contains_inclusive(value: number): boolean
	{
		return value >= this.beg && value <= this.end;
	}

	/**
	 * Grow the bounds of this range to include 'value'
	 * @param value The number of include in the range
	 * @returns 'value' for fluent forwarding
	 */
	encompass(value: number): number
	{
		if (value < this.beg) this.beg = value;
		if (value > this.end) this.end = value;
		return value;
	}

	/**
	 * Grow the bounds of this range to include 'range'
	 * @param range The range to encompass within this range
	 * @returns 'range' for fluent forwarding
	 */
	encompass_range(range: Range): Range
	{
		if (range.beg < this.beg) this.beg = range.beg;
		if (range.end > this.end) this.end = range.end;
		return range;
	}
}