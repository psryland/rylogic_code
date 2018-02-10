/**
 * @module v4
 */

import * as Maths from "./maths";

var FVec = Float32Array;

export const Zero   = new FVec([0,0,0,0]);
export const XAxis  = new FVec([1,0,0,0]);
export const YAxis  = new FVec([0,1,0,0]);
export const ZAxis  = new FVec([0,0,1,0]);
export const WAxis  = new FVec([0,0,0,1]);
export const Origin = new FVec([0,0,0,1]);

/**
 * Create a 4-vector containing zeros
 * @returns {v4}
 */
export function create()
{
	var out = new FVec(4);
	return out;
}

/**
 * Construct from components.
 * If only 'x' is given, then the returned vector is equivalent to make(x,x,x,x)
 * @param {Number} x 
 * @param {Number} y 
 * @param {Number} z 
 * @param {Number} w 
 * @returns {v4}
 */
export function make(x,y,z,w)
{
	var out = create();
	if (y === undefined) y = x;
	if (z === undefined) z = x;
	if (w === undefined) w = x;
	return set(out, x, y, z, w);
}

/**
 * Assign the values of a 4-vector
 * @param {v4} vec the vector to be assigned
 * @param {Number} x
 * @param {Number} y
 * @param {Number} z
 * @param {Number} w
 * @returns {v4}
 */
export function set(vec, x, y, z, w)
{
	vec[0] = x;
	vec[1] = y;
	vec[2] = z;
	vec[3] = w;
	return vec;
}

/**
 * Create a new copy of 'vec'
 * @param {v4} vec the source vector to copy
 * @param {v4} out (optional) the vector to write to
 * @returns {v4} the clone of 'vec'
 */
export function clone(vec, out)
{
	out = out || create();
	if (vec !== out)
	{
		out[0] = vec[0];
		out[1] = vec[1];
		out[2] = vec[2];
		out[3] = vec[3];
	}
	return out;
}

/**
 * Exact equality of two 4-vectors
 * @param {v4} a
 * @param {v4} b
 * @returns {boolean}
 */
export function Eql(a, b)
{
	let eql =
		a[0] == b[0] &&
		a[1] == b[1] &&
		a[2] == b[2] &&
		a[3] == b[3];
	return eql;
}

/**
 * Approximate equality of two 4-vectors
 * @param {v4} a
 * @param {v4} b
 * @returns {boolean}
 */
export function FEql(a, b)
{
	let feql =
		Maths.FEql(a[0], b[0]) &&
		Maths.FEql(a[1], b[1]) &&
		Maths.FEql(a[2], b[2]) &&
		Maths.FEql(a[3], b[3]);
	return feql;
}

/**
 * Returns true if 'vec' contains any 'NaN's
 * @param {v4} vec 
 * @returns {boolean}
 */
export function IsNaN(vec)
{
	let is_nan =
		isNaN(vec[0]) ||
		isNaN(vec[1]) ||
		isNaN(vec[2]) ||
		isNaN(vec[3]);
	return is_nan;
}

/**
 * Return the negation of 'vec'
 * @param {v4} vec the vector to negate
 * @param {v4} out (optional) the vector to write to
 * @returns {v4} the negative of 'vec'
 */
export function Neg(vec, out)
{
	out = out || create();
	out[0] = -vec[0];
	out[1] = -vec[1];
	out[2] = -vec[2];
	out[3] = -vec[3];
	return out;
}

/**
 * Return 'vec' with the w component set to 1
 * @param {v4} vec 
 * @returns {v4}
 */
export function SetW1(vec)
{
	vec[3] = 1;
	return vec;
}

/**
 * Absolute value of a 4-vector (component-wise)
 * @param {v4} vec the vector to find the absolute value of
 * @param {v4} out (optional) where to write the result
 * @returns {v4}
 */
export function Abs(vec, out)
{
	out = out || create();
	out[0] = Math.abs(vec[0]);
	out[1] = Math.abs(vec[1]);
	out[2] = Math.abs(vec[2]);
	out[3] = Math.abs(vec[3]);
	return out;
}

/**
 * Clamp the components of 'vec' to the inclusive range given by [min,max]
 * @param {v4} vec The vector to be clamped
 * @param {v4} min The inclusive minimum value
 * @param {v4} max The inclusive maximum value
 * @param {v4} out (optional) where to write the result
 * @returns {v4}
 */
export function Clamp(vec, min, max, out)
{
	out = out || create();
	out[0] = Maths.Clamp(vec[0], min[0], max[0]);
	out[1] = Maths.Clamp(vec[1], min[1], max[1]);
	out[2] = Maths.Clamp(vec[2], min[2], max[2]);
	out[3] = Maths.Clamp(vec[3], min[3], max[3]);
	return out;
}

/**
 * Compute the squared length of a 4-vector
 * @param {v4} vec the vector to compute the squared length of
 * @returns {Number} the squared length of the vector
 */
export function LengthSq(vec)
{
	return vec[0]*vec[0] + vec[1]*vec[1] + vec[2]*vec[2] + vec[3]*vec[3];
}

/**
 * Compute the length of a 4-vector
 * @param {v4} vec the vector to find the length of
 * @returns {Number} the length of the vector
 */
export function Length(vec)
{
	return Math.sqrt(LengthSq(vec));
}

/**
 * Normalise a 4-vector to length = 1
 * @param {v4} vec the vector to normalise
 * @param {v4} out (optional) the vector to write the result to
 * @param {Object} opts optional parameters: {def:[..]}
 * @returns {v4} the vector with components normalised
 */
export function Normalise(vec, out, opts)
{
	out = out || create();
	let len = Length(vec);
	if (len == 0)
	{
		if (opts && opts.def) out = opts.def;
		else throw new Error("Cannot normalise a zero vector");
	}
	out[0] = vec[0] / len;
	out[1] = vec[1] / len;
	out[2] = vec[2] / len;
	out[3] = vec[3] / len;
	return out;
}

/**
 * Return the dot product of two 4-vectors
 * @param {v4} a the first vector
 * @param {v4} b the second vector
 * @returns {Number}
 */
export function Dot(a, b)
{
	return a[0]*b[0] + a[1]*b[1] + a[2]*b[2] + a[3]*b[3];
}

/**
 * Cross product: a x b
 * @param {v4} a
 * @param {v4} b
 * @param {v4} out (optional) where the result is written to
 * @returns {v4}
 */
export function Cross(a, b, out)
{
	out = out || create();
	set(out, a[1]*b[2] - a[2]*b[1], a[2]*b[0] - a[0]*b[2], a[0]*b[1] - a[1]*b[0], 0);
	return out;
}

/**
 * Return 'a + b'
 * @param {v4} a
 * @param {v4} b
 * @param {v4} out (optional) where to write the result
 * @returns {v4}
 */
export function Add(a, b, out)
{
	out = out || create();
	set(out, a[0] + b[0], a[1] + b[1], a[2] + b[2], a[3] + b[3]);
	return out;
}

/**
 * Add an array of vectors
 * @param {[v4]} arr 
 * @returns {v4} The sum of the given vectors
 */
export function AddN(...arr)
{
	let sum = create();
	for (let i = 0; i != arr.length; ++i)
		Add(arr[i], sum, sum);
	return sum;
}

/**
 * Return 'a - b'
 * @param {v4} a
 * @param {v4} b
 * @param {v4} out (optional) where the result is written
 * @returns {v4}
 */
export function Sub(a, b, out)
{
	out = out || create();
	set(out, a[0] - b[0], a[1] - b[1], a[2] - b[2], a[3] - b[3]);
	return out;
}

/**
 * Multiply a vector by a scalar
 * @param {v4} a the vector to multiply
 * @param {Number} b the number to scale by
 * @param {v4} out (optional) where to write the result
 * @returns {v4}
 */
export function MulS(a, b, out)
{
	out = out || create();
	out[0] = a[0] * b;
	out[1] = a[1] * b;
	out[2] = a[2] * b;
	out[3] = a[3] * b;
	return out;
}

/**
 * Multiply a vector by another vector (component-wise)
 * @param {v4} a the vector to multiply
 * @param {v4} b the vector to scale by
 * @param {v4} out (optional) where to write the result
 * @returns {v4}
 */
export function MulV(a, b, out)
{
	out = out || create();
	out[0] = a[0] * b[0];
	out[1] = a[1] * b[1];
	out[2] = a[2] * b[2];
	out[3] = a[3] * b[3];
	return out;
}

/**
 * Returns true if 'a' and 'b' parallel
 * @param {v4} v0
 * @param {v4} v1
 * @returns {boolean}
 */
export function Parallel(v0, v1)
{
	return LengthSq(Cross(v0, v1)) < Maths.TinySq;
}

/**
 * Returns a vector guaranteed not parallel to 'v'
 * @param {v4} v
 * @returns {v4}
 */
export function CreateNotParallelTo(v)
{
	let x_aligned = Math.abs(v[0]) > Math.abs(v[1]) && Math.abs(v[0]) > Math.abs(v[2]);
	let out = make(!x_aligned, 0, x_aligned, v[3]);
}

/**
 * Returns a vector perpendicular to 'vec' favouring 'previous' as the preferred perpendicular.
 * @param {v4} vec The vector to be perpendicular to.
 * @param {v4} previous (optional) The previously used perpendicular, for returning consistent results.
 * @returns {v4}
 */
export function Perpendicular(vec, previous)
{
	if (LengthSq(vec) < Maths.Tiny)
		throw new Error("Cannot make a perpendicular to a zero vector");

	let out = create();

	// If 'previous' is parallel to 'vec', choose a new perpendicular (includes previous == zero)
	if (!previous || Parallel(vec, previous))
	{
		Cross(vec, CreateNotParallelTo(vec), out);
		MulVS(out, Length(vec) / Length(out), out);
	}
	else
	{
		// If 'previous' is still perpendicular, keep it
		if (Maths.FEql(Dot(vec, previous), 0))
			return previous;

		// Otherwise, make a perpendicular that is close to 'previous'
		Cross(vec, previous, out);
		Cross(out, vec, out);
		Normalise(out, out);
	}
	return out;
}

