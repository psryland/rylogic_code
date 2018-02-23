import * as Bits from "./bits";

// Epsilon
export const Tiny      = 1.00000007e-05;
export const TinySq    = 1.00000015e-10;
export const TinySqrt  = 3.16227786e-03;

// Constants
export const Phi       = 1.618033988749894848204586834; // "Golden Ratio"
export const Tau       = 6.283185307179586476925286766559; // circle constant
export const InvTau    = 1.0 / Tau;
export const TauBy2    = Tau / 2.0;
export const TauBy3    = Tau / 3.0;
export const TauBy4    = Tau / 4.0;
export const TauBy5    = Tau / 5.0;
export const TauBy6    = Tau / 6.0;
export const TauBy7    = Tau / 7.0;
export const TauBy8    = Tau / 8.0;
export const TauBy10   = Tau / 10.0;
export const TauBy16   = Tau / 16.0;
export const TauBy32   = Tau / 32.0;
export const TauBy360  = Tau / 360.0;
export const _360ByTau = 360.0 / Tau;
export const Root2     = 1.4142135623730950488016887242097;
export const Root3     = 1.7320508075688772935274463415059;
export const InvRoot2  = 1.0 / 1.4142135623730950488016887242097;
export const InvRoot3  = 1.0 / 1.7320508075688772935274463415059;

// Types
export interface Vec2
{
	[index: number]: number;
	length: number;
}
export interface Vec4
{
	[index: number]: number;
	length: number;
}
export interface M4x4
{
	[index: number]: number;
	length: number;
}
export interface Range
{
	/** The inclusive first value in the range */
	beg: number;

	/** The exclusive last value in the range */
	end: number;

	/** The centre value in the range */
	centre: number;

	/** The size of the range: end - beg */
	size: number;

	/** The inclusive first value in the range as an integer. i.e. floor(beg) */
	begi: number;

	/** The exclusive last value in the range as an integer. i.e. floor(end) */
	endi: number;

	/** The integer size of the range. i.e. iend - ibeg */
	count: number;
}
export interface Size
{
	/** Indexer for width/height */
	[index: number]: number;
	length?: number;
}
export interface Rect
{
	/** The X position of the rectangle */
	x: number;

	/** The Y position of the rectangle */
	y: number;

	/** The width of the rectangle */
	w: number;

	/** The height of the rectangle */
	h: number;

	/** The left edge of the rectangle */
	l: number;

	/** The top edge of the rectangle */
	t: number;

	/** The right edge of the rectangle */
	r: number;

	/** The bottom edge of the rectangle */
	b: number;

	/** The centre point of the rectangle */
	centre: Vec2;

	/** The width/height of the rectangle */
	size: Size;

	/** The corners */
	tl: Vec2;
	tr: Vec2;
	bl: Vec2;
	br: Vec2;
}
export interface BBox
{
	/** The position of the centre of the bounding box */
	centre: Vec4;

	/** The radius of the bounding box in each axis direction */
	radius: Vec4;

	/** True if the bounding box has a volume >= 0 */
	is_valid: boolean;

	/** True if the bounding box has a volume == 0 */
	is_point: boolean;

	/** The minimum x,y,z corner of the bounding box */
	lower: Vec4;

	/** The maximum x,y,z corner of the bounding box */
	upper: Vec4;

	/** The dimensions of the bounding box */
	size: Vec4;

	/** The squared length of the bounding box diagonal */
	diametre_sq: number;

	/** The length of the bounding box diagonal */
	diametre: number;

	/** The volume of the bounding box */
	volume: number;
}

export
{
	Bits
}

/**
 * Floating point comparison
 * @param a the first value to compare
 * @param b the second value to compare
 * @param tol the tolerance value. *WARNING* 'tol' is a relative tolerance, relative to the largest of 'a' or 'b'
 * @returns true if the values are equal within 'tol*largest(a,b)'
 */
export function FEqlRelative(a:number, b:number, tol:number) :boolean
{
	// Floating point compare is dangerous and subtle.
	// See: https://randomascii.wordpress.com/2012/02/25/comparing-floating-point-numbers-2012-edition/
	// and: http://floating-point-gui.de/errors/NearlyEqualsTest.java
	// Tests against zero treat 'tol' as an absolute difference threshold.
	// Tests between two non-zero values use 'tol' as a relative difference threshold.
	// i.e.
	//    FEql(2e-30, 1e-30) == false
	//    FEql(2e-30 - 1e-30, 0) == true

	// Handles tests against zero where relative error is meaningless
	// Tests with 'b == 0' are the most common so do them first
	if (b == 0) return Math.abs(a) < tol;
	if (a == 0) return Math.abs(b) < tol;

	// Handle infinities and exact values
	if (a == b) return true;

	// When float operations are performed at compile time, the compiler warnings about 'inf'
	let diff = a - b;

	// Test relative error as a fraction of the largest value
	return Math.abs(diff) < tol * Math.max(Math.abs(a), Math.abs(b));
}

/**
 * Compare 'a' and 'b' for approximate equality
 * @param a the first value to compare
 * @param b the second value to compare
 * @returns true if the values are equal within 'Tiny*largest(a,b)'
 */
export function FEql(a:number, b:number) :boolean
{
	// Don't add a 'tol' parameter because it looks like the function
	// should perform a == b +- tol, which isn't what it does.
	return FEqlRelative(a, b, Tiny);
}

/**
 * Compare all elements of 'a' and 'b' for exact equality.
 * @param a The array to compare with 'b'
 * @param b The array to compare with 'a'
 * @param start (optional) The index of the element to begin comparing from
 * @param count (optional) The index of the element to begin comparing from
 * @returns True if the compared values are exactly equal
 */
export function EqlN(a:number[], b:number[], start:number, count:number) :boolean
{
	if (start === undefined)
	{
		if (a.length != b.length) return false;
		let i = 0, iend = a.length;
		for (; i != iend && a[i] == b[i]; ++i) {}
		return i == iend;
	}
	else
	{
		if (count === undefined)
			count = Math.max(a.length - start, b.length - start);
		if (a.length < start + count)
			return false;
		if (b.length < start + count)
			return false;

		let i = start, iend = start + count;
		for (; i != iend && a[i] == b[i]; ++i) {}
		return i == iend;
	}
}

/**
 * IEEERemainder
 * @returns The IEEE remainder of x % y
 */
export function IEEERemainder(x:number, y:number)
{
	let mod = x % y;
	if (isNaN(mod))
		return NaN;

	if (mod == 0)
		if (x < -0.0)
			return -0.0;

	let alt_mod = mod - (Math.abs(y) * Sign(x));
	if (Math.abs(alt_mod) == Math.abs(mod))
	{
		let div = x / y;
		let div_rounded = Math.round(div);
		if (Math.abs(div_rounded) > Math.abs(div))
			return alt_mod;

		return mod;
	}

	if (Math.abs(alt_mod) < Math.abs(mod))
		return alt_mod;

	return mod;
}

/**
 * Clamp 'x' to the given inclusive range [min,max]
 * @param x The value to be clamped
 * @param mn The inclusive minimum value
 * @param mx The inclusive maximum value
 * @returns 'x' clamped to the inclusive range [mn,mx]
 */
export function Clamp(x:number, mn:number, mx:number) :number
{
	if (mn > mx) throw new Error("[min,max] must be a positive range");
	return (mx < x) ? mx : (x < mn) ? mn : x;
}

/**
 * Return the square of 'x'
 * @param x
 * @returns The result of x * x
 */
export function Sqr(x:number) :number
{
	return x * x;
}

/**
 * Return +1 or -1 depending on the sign of 'x'
 * @param x The value to return the sign of
 * @returns +1 if 'x >= 0', -1 if 'x < 0'
 */
export function Sign(x:number) :number
{
	// If x is NaN, the result is NaN.
	// If x is -0, the result is -0.
	// If x is +0, the result is +0.
	// If x is negative and not -0, the result is -1.
	// If x is positive and not +0, the result is +1.
	return (+(x > 0) - +(x < 0)) || +x;
	// A more aesthetical persuado-representation is shown below
	// ( (x > 0) ? 0 : 1 )  // if x is negative then negative one
	//          +           // else (because you cant be both - and +)
	// ( (x < 0) ? 0 : -1 ) // if x is positive then positive one
	//         ||           // if x is 0, -0, or NaN, or not a number,
	//         +x           // Then the result will be x, (or) if x is
	//                      // not a number, then x converts to number
}

/**
 * Return Log base 10 of 'x'
 * @param x
 */
export function Log10(x:number) :number
{
	return Math.log(x) * Math.LOG10E;
}

/**
 * Compute the squared length of an array of numbers
 * @param vec the vector to compute the squared length of
 * @returns The squared length of the vector
 */
export function LengthSq(vec:number[]) :number
{
	let sum_sq = 0;
	for (let i = 0; i != vec.length; ++i) sum_sq += vec[i]*vec[i];
	return sum_sq;
}

/**
 * Compute the length of an array of numbers.
 * @param vec the vector to find the length of
 * @returns The length of the vector
 */
export function Length(vec:number[]) :number
{
	return Math.sqrt(LengthSq(vec));
}

/**
 * Normalise a vector to length = 1 in place
 * @param vec The array to normalise
 * @param opts Parameters: {def:[..] = default value to return if the length of 'vec' is zero}
 * @returns The vector with components normalised
 */
export function Normalise(vec:number[], opts?:{def:number[]}) :number[]
{
	let len = Length(vec);
	if (len <= 0)
	{
		if (opts && opts.def) return opts.def;
		throw new Error("Cannot normalise a zero vector");
	}
	for (let i = 0; i != vec.length; ++i)
	{
		vec[i] /= len;
	}
	return vec;
}

/**
 * Return the component-wise sum of two arrays: a + b
 * @param a
 * @param b
 * @param out Where to write the result
 * @returns The result of 'a + b'
 */
export function Add(a:number[], b:number[], out?:number[]) :number[]
{
	if (a.length != b.length)
		throw new Error("Vectors must have the same length");

	out = out || a.slice();
	for (let i = 0; i != a.length; ++i)
		out[i] = a[i] + b[i];
	
	return out;
}

/**
 * Return the sum of a collection of vectors
 * @param vec 
 * @returns The result of 'a + b + c + ...'
 */
export function AddN(...vec:number[][]) :number[]
{
	let sum = vec[0].slice();
	for (let i = 0; i != vec.length; ++i)
		Add(sum, vec[i], sum);
	return sum;
}

/**
 * Compute the dot product of two arrays
 * @param a the first vector
 * @param b the second vector
 * @returns The dot product
 */
export function Dot(a:number[], b:number[]) :number
{
	if (a.length != b.length)
		throw new Error("Dot product requires vectors of equal length.");

	let dp = 0;
	for (let i = 0; i != a.length; ++i) dp += a[i] * b[i];
	return dp;
}

// // Workarounds
// Math["log10"] = Math["log10"]:function || Math_.Log10;
// Math["sign"] = Math["sign"]:function || Math_.Sign;

