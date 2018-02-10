/**
 * @module Maths
 */

import * as v2 from "./v2";
import * as v4 from "./v4";
import * as m4x4 from "./m4x4";
import * as Range from "./range";
import * as Rect from "./rect";
import * as BBox from "./bbox";

"use strict";

export { v2, v4, m4x4, Range, Rect, BBox };

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

/**
 * Floating point comparison
 * @param {Number} a the first value to compare
 * @param {Number} b the second value to compare
 * @param {Number} tol the tolerance value. *WARNING* 'tol' is a relative tolerance, relative to the largest of 'a' or 'b'
 * @returns true if the values are equal within 'tol*largest(a,b)'
 */
export function FEqlRelative(a, b, tol)
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
 * @param {Number} a the first value to compare
 * @param {Number} b the second value to compare
 * @returns {boolean} true if the values are equal within 'Tiny*largest(a,b)'
 */
export function FEql(a, b)
{
	// Don't add a 'tol' parameter because it looks like the function
	// should perform a == b +- tol, which isn't what it does.
	return FEqlRelative(a, b, Tiny);
}

/**
 * Compare all elements of 'a' and 'b' for exact equality.
 * @param {[Number]} a The array to compare with 'b'
 * @param {[Number]} b The array to compare with 'a'
 * @param {[Number]} start (optional) The index of the element to begin comparing from
 * @param {[Number]} count (optional) The index of the element to begin comparing from
 * @returns {boolean}
 */
export function EqlN(a,b,start,count)
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
 */
export function IEEERemainder(x, y)
{
	let mod = x % y;
	if (isNaN(mod))
		return NaN;

	if (mod == 0)
		if (x < -0.0)
			return -0.0;

	let alt_mod = mod - (Math.abs(y) * Math.sign(x));
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
 * @param {Number} x The value to be clamped
 * @param {Number} mn The inclusive minimum value
 * @param {Number} mx The inclusive maximum value
 * @returns {Number}
 */
export function Clamp(x, mn, mx)
{
	//assert(mn <= mx, "[min,max] must be a positive range");
	return (mx < x) ? mx : (x < mn) ? mn : x;
}

/**
 * Return the square of 'x'
 * @param {Number} x
 * @returns {Number}
 */
export function Sqr(x)
{
	return x * x;
}

/**
 * Compute the squared length of a vector of numbers
 * @param {[Number]} vec the vector to compute the squared length of
 * @returns {Number} the squared length of the vector
 */
export function LengthSq(vec)
{
	let sum_sq = 0;
	for (let i = 0; i != vec.length; ++i) sum_sq += vec[i]*vec[i];
	return sum_sq;    
}

/**
 * Compute the length of a vector of numbers
 * @param {[Number]} vec the vector to find the length of
 * @returns {Number} the length of the vector
 */
export function Length(vec)
{
	return Math.sqrt(LengthSq(vec));
}

/**
 * Normalise a vector to length = 1 in place
 * @param {[Number]} vec the vector to normalise
 * @param {Object} opts optional parameters: {def:[..]}
 * @returns {[Number]} the vector with components normalised
 */
export function Normalise(vec, opts)
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
 * Return the sum of two vectors: a + b
 * @param {[Number]} a
 * @param {[Number]} b
 * @param {[Number]} out (optional). Where to write the result
 * @returns {[Number]}
 */
export function Add(a,b,out)
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
 * @param {[[Number]]} vec 
 * @returns {[Number]}
 */
export function AddN(...vec)
{
	let sum = vec[0].slice();
	for (let i = 0; i != vec.length; ++i)
		Add(sum, vec[i], sum);
	return sum;
}

/**
 * Compute the dot product of two vectors
 * @param {[Number]} a the first vector
 * @param {[Number]} b the second vector
 * @returns {Number} the dot product
 */
export function Dot(a, b)
{
	if (a.length != b.length)
		throw new Error("Dot product requires vectors of equal length.");

	let dp = 0;
	for (let i = 0; i != a.length; ++i) dp += a[i] * b[i];
	return dp;
}
export function Dot2(a, b)
{
	return a[0]*b[0] + a[1]*b[1];
}
export function Dot3(a, b)
{
	return a[0]*b[0] + a[1]*b[1] + a[2]*b[2];
}
export function Dot4(a, b)
{
	return a[0]*b[0] + a[1]*b[1] + a[2]*b[2] + a[3]*b[3];
}

// Cross product: a x b
export function Cross3(a, b)
{
	return v4.make(a[1]*b[2] - a[2]*b[1], a[2]*b[0] - a[0]*b[2], a[0]*b[1] - a[1]*b[0], 0);
}

// Triple product: a . b x c
export function Triple3(a, b, c)
{
	return Dot3(a, Cross3(b, c));
}

// Workarounds
Math.log10 = Math.log10 || function(x) { return Math.log(x) * Math.LOG10E; };
Math.sign = Math.sign || function(x)
{
	// If x is NaN, the result is NaN.
	// If x is -0, the result is -0.
	// If x is +0, the result is +0.
	// If x is negative and not -0, the result is -1.
	// If x is positive and not +0, the result is +1.
	return ((x > 0) - (x < 0)) || +x;
	// A more aesthetical persuado-representation is shown below
	// ( (x > 0) ? 0 : 1 )  // if x is negative then negative one
	//          +           // else (because you cant be both - and +)
	// ( (x < 0) ? 0 : -1 ) // if x is positive then positive one
	//         ||           // if x is 0, -0, or NaN, or not a number,
	//         +x           // Then the result will be x, (or) if x is
	//                      // not a number, then x converts to number
};