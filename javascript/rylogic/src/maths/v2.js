/**
 * @module v2
 */

import * as Maths from "./maths";

let FVec = Float32Array;
export const XAxis  = new FVec([1,0]);
export const YAxis  = new FVec([0,1]);

/**
 * Create a 2-vector containing zeros
 * @returns {v2}
 */
export function create()
{
	var out = new FVec(2);
	return out;
}

/**
 * Construct from components.
 * If only 'x' is given, then the returned vector is equivalent to v2.make(x,x)
 * @param {Number} x
 * @param {Number} y
 * @returns {v2}
 */
export function make(x,y)
{
	var out = create();
	if (y === undefined) y = x;
	return set(out, x, y);
}

/**
 * Assign the values of a 2-vector
 * @param {v2} vec the vector to be assigned
 * @param {Number} x
 * @param {Number} y
 * @returns {v2}
 */
export function set(vec, x, y)
{
	vec[0] = x;
	vec[1] = y;
	return vec;
}

/**
 * Create a new copy of 'vec'
 * @param {v2} vec the source vector to copy
 * @param {v2} out (optional) the vector to write to
 * @returns {v2} the clone of 'vec'
 */
export function clone(vec, out)
{
	out = out || create();
	if (vec !== out)
	{
		out[0] = vec[0];
		out[1] = vec[1];
	}
	return out;
}

/**
 * Exact equality of two 2-vectors
 * @param {v2} a
 * @param {v2} b
 * @returns {boolean}
 */
export function Eql(a, b)
{
	let eql =
		a[0] == b[0] &&
		a[1] == b[1];
	return eql;
}

/**
 * Approximate equality of two 2-vectors
 * @param {v2} a
 * @param {v2} b
 * @returns {boolean}
 */
export function FEql(a, b)
{
	let feql =
		Maths.FEql(a[0], b[0]) &&
		Maths.FEql(a[1], b[1]);
	return feql;
}

/**
 * Returns true if 'vec' contains any 'NaN's
 * @param {v2} vec
 * @returns {boolean}
 */
export function IsNaN(vec)
{
	let is_nan =
		isNaN(vec[0]) ||
		isNaN(vec[1]);
	return is_nan;
}

/**
 * Return the negation of 'vec'
 * @param {v2} vec the vector to negate
 * @param {v2} out (optional) the vector to write to
 * @returns {v2} the negative of 'vec'
 */
export function Neg(vec, out)
{
	out = out || create();
	out[0] = -vec[0];
	out[1] = -vec[1];
	return out;
}

/**
 * Absolute value of a 2-vector (component-wise)
 * @param {v2} vec the vector to find the absolute value of
 * @param {v2} out (optional) where to write the result
 * @returns {v2}
 */
export function Abs(vec, out)
{
	out = out || create();
	out[0] = Math.abs(vec[0]);
	out[1] = Math.abs(vec[1]);
	return out;
}

/**
 * Clamp the components of 'vec' to the inclusive range given by [min,max]
 * @param {v2} vec The vector to be clamped
 * @param {v2} min The inclusive minimum value
 * @param {v2} max The inclusive maximum value
 * @param {v2} out (optional) where to write the result
 * @returns {v2}
 */
export function Clamp(vec, min, max, out)
{
	out = out || create();
	out[0] = Maths.Clamp(vec[0], min[0], max[0]);
	out[1] = Maths.Clamp(vec[1], min[1], max[1]);
	return out;
}

/**
 * Compute the squared length of a 2-vector
 * @param {v2} vec the vector to compute the squared length of
 * @returns {Number} the squared length of the vector
 */
export function LengthSq(vec)
{
	return vec[0]*vec[0] + vec[1]*vec[1];
}

/**
 * Compute the length of a 2-vector
 * @param {v2} vec the vector to find the length of
 * @returns {Number} the length of the vector
 */
export function Length(vec)
{
	return Math.sqrt(LengthSq(vec));
}

/**
 * Normalise a 2-vector to length = 1
 * @param {v2} vec the vector to normalise
 * @param {v2} out (optional) the vector to write the result to
 * @param {Object} opts optional parameters: {def:[..]}
 * @returns {v2} the vector with components normalised
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
	return out;
}

/**
 * Return the dot product of two 2-vectors
 * @param {v2} a the first vector
 * @param {v2} b the second vector
 * @returns {Number}
 */
export function Dot(a, b)
{
	return a[0]*b[0] + a[1]*b[1];
}

/**
 * Cross product: a x b
 * (Equivalent to dot(rotate90cw(a), b))
 * @param {v2} a
 * @param {v2} b
 * @param {v2} out (optional) where the result is written to
 * @returns {v2}
 */
export function Cross(a, b, out)
{
	out = out || create();
	set(out, a[1] * b[0] - a[0] * b[1]);
	return out;
}

/**
 * Return 'a + b'
 * @param {v2} a
 * @param {v2} b
 * @param {v2} out (optional) where to write the result
 * @returns {v2}
 */
export function Add(a, b, out)
{
	out = out || create();
	set(out, a[0] + b[0], a[1] + b[1]);
	return out;
}

/**
 * Add an array of vectors
 * @param {[v2]} arr 
 * @returns {v2} The sum of the given vectors
 */
export function AddN(...arr)
{
	let sum = v2.create();
	for (let i = 0; i != arr.length; ++i)
		Add(arr[i], sum, sum);
	return sum;
}

/**
 * Return 'a - b'
 * @param {v2} a
 * @param {v2} b
 * @param {v2} out (optional) where the result is written
 * @returns {v2}
 */
export function Sub(a, b, out)
{
	out = out || create();
	set(out, a[0] - b[0], a[1] - b[1]);
	return out;
}

/**
 * Multiply a vector by a scalar
 * @param {v2} a the vector to multiply
 * @param {Number} b the number to scale by
 * @param {v2} out (optional) where to write the result
 * @returns {v2}
 */
export function MulS(a, b, out)
{
	out = out || create();
	out[0] = a[0] * b;
	out[1] = a[1] * b;
	return out;
}

/**
 * Multiply a vector by another vector (component-wise)
 * @param {v2} a the vector to multiply
 * @param {v2} b the vector to scale by
 * @param {v2} out (optional) where to write the result
 * @returns {v2}
 */
export function MulV(a, b, out)
{
	out = out || create();
	out[0] = a[0] * b[0];
	out[1] = a[1] * b[1];
	return out;
}
