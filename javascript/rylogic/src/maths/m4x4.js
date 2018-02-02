/**
 * @module m4x4
 */

import * as Maths from "./maths";
import * as v4 from "./v4";

let FMat = Float32Array;

/**
 * Create a new 4x4 identity matrix
 * @returns {m4x4}
 */
export function create()
{
	let out = new FMat(16);
	out[ 0] = 1;
	out[ 5] = 1;
	out[10] = 1;
	out[15] = 1;
	return out;
}

/**
 * Construct from components
 * @param {v4} x 
 * @param {v4} y 
 * @param {v4} z 
 * @param {v4} w 
 * @returns {m4x4}
 */
export function make(x,y,z,w)
{
	var out = create();
	SetX(out, x);
	SetY(out, y);
	SetZ(out, z);
	SetW(out, w);
	return out;
}

/**
 * Assign the values of a 4x4 matrix
 * @param {m4x4} mat the matrix to be assigned
 * @param {v4} x
 * @param {v4} y
 * @param {v4} z
 * @param {v4} w
 * @returns {m4x4}
 */
export function set(mat, x, y, z, w)
{
	SetX(mat, x);
	SetY(mat, y);
	SetZ(mat, z);
	SetW(mat, w);
	return mat;
}

/**
 * Create a new copy of 'mat'
 * @param {m4x4} mat the matrix to copy
 * @param {m4x4} out (optional) where to write the copy to
 * @returns {m4x4}
 */
export function clone(mat, out)
{
	out = out || create();
	if (mat !== out)
	{
		out[ 0] = mat[ 0];
		out[ 1] = mat[ 1];
		out[ 2] = mat[ 2];
		out[ 3] = mat[ 3];
		out[ 4] = mat[ 4];
		out[ 5] = mat[ 5];
		out[ 6] = mat[ 6];
		out[ 7] = mat[ 7];
		out[ 8] = mat[ 8];
		out[ 9] = mat[ 9];
		out[10] = mat[10];
		out[11] = mat[11];
		out[12] = mat[12];
		out[13] = mat[13];
		out[14] = mat[14];
		out[15] = mat[15];
	}
	return out;
}

/**
 * Set 'mat' to the identity matrix
 * @param {m4x4} mat the matrix to set to identity
 * @returns {m4x4}
 */
export function Identity(mat)
{
	for (let i = 0; i != mat.length; ++i)
		mat[i] = 0;

	mat[ 0] = 1;
	mat[ 5] = 1;
	mat[10] = 1;
	mat[15] = 1;
	return mat;
}

/**
 * Get the X vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
export function GetX(mat)
{
	return v4.make(mat[0], mat[1], mat[2], mat[3]);
}

/**
 * Set the X vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
export function SetX(mat, vec)
{
	mat[0] = vec[0];
	mat[1] = vec[1];
	mat[2] = vec[2];
	mat[3] = vec[3];
}

/**
 * Get the Y vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
export function GetY(mat)
{
	return v4.make(mat[4], mat[5], mat[6], mat[7]);
}

/**
 * Set the Y vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
export function SetY(mat, vec)
{
	mat[4] = vec[0];
	mat[5] = vec[1];
	mat[6] = vec[2];
	mat[7] = vec[3];
}

/**
 * Get the Z vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
export function GetZ(mat)
{
	return v4.make(mat[8], mat[9], mat[10], mat[11]);
}

/**
 * Set the Z vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
export function SetZ(mat, vec)
{
	mat[ 8] = vec[0];
	mat[ 9] = vec[1];
	mat[10] = vec[2];
	mat[11] = vec[3];
}

/**
 * Get the W vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
export function GetW(mat)
{
	return v4.make(mat[12], mat[13], mat[14], mat[15]);
}

/**
 * Set the W vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
export function SetW(mat, vec)
{
	mat[12] = vec[0];
	mat[13] = vec[1];
	mat[14] = vec[2];
	mat[15] = vec[3];
}

/**
 * Multiply a 4-vector by a 4x4 matrix
 * @param {m4x4} m
 * @param {v4} v 
 * @param {v4} out (optional) Where to write the result
 * @returns {v4}
 */
export function MulMV(m, v, out)
{
	out = out || v4.create();
	let x = v[0], y = v[1], z = v[2], w = v[3];
	out[0] = m[0] * x + m[4] * y + m[ 8] * z + m[12] * w;
	out[1] = m[1] * x + m[5] * y + m[ 9] * z + m[13] * w;
	out[2] = m[2] * x + m[6] * y + m[10] * z + m[14] * w;
	out[3] = m[3] * x + m[7] * y + m[11] * z + m[15] * w;
	return out;
}

/**
 * Multiply two 4x4 matrices
 * @param {m4x4} a the lhs matrix
 * @param {m4x4} b the rhs matrix
 * @param {m4x4} out (optional) where to write the result
 * @returns {m4x4} a x b
 */
export function MulMM(a, b, out)
{
	out = out || create();

	let a00 = a[ 0], a01 = a[ 1], a02 = a[ 2], a03 = a[ 3];
	let a10 = a[ 4], a11 = a[ 5], a12 = a[ 6], a13 = a[ 7];
	let a20 = a[ 8], a21 = a[ 9], a22 = a[10], a23 = a[11];
	let a30 = a[12], a31 = a[13], a32 = a[14], a33 = a[15];

	// Cache only the current line of the second matrix
	let b0 = b[0], b1 = b[1], b2 = b[2], b3 = b[3];
	out[0] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
	out[1] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
	out[2] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
	out[3] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

	b0 = b[4]; b1 = b[5]; b2 = b[6]; b3 = b[7];
	out[4] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
	out[5] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
	out[6] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
	out[7] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

	b0 = b[8]; b1 = b[9]; b2 = b[10]; b3 = b[11];
	out[ 8] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
	out[ 9] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
	out[10] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
	out[11] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

	b0 = b[12]; b1 = b[13]; b2 = b[14]; b3 = b[15];
	out[12] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
	out[13] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
	out[14] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
	out[15] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

	return out;
}

/**
 * Get the transpose of the 3x3 part of 'mat', storing the result in 'out' if provided.
 */
export function Transpose3x3(mat, out)
{
	if (mat === out)
	{
		let m01 = out[1], m02 = out[2];
		let m12 = out[6];
		out[1] = out[4];
		out[2] = out[8];
		out[6] = out[9];
		out[4] = m01;
		out[8] = m02;
		out[9] = m12;
	}
	else
	{
		out = out || create();
		out[ 0] = mat[ 0];
		out[ 1] = mat[ 4];
		out[ 2] = mat[ 8];
		out[ 3] = mat[ 3];
		out[ 4] = mat[ 1];
		out[ 5] = mat[ 5];
		out[ 6] = mat[ 9];
		out[ 7] = mat[ 7];
		out[ 8] = mat[ 2];
		out[ 9] = mat[ 6];
		out[10] = mat[10];
		out[11] = mat[11];
		out[12] = mat[12];
		out[13] = mat[13];
		out[14] = mat[14];
		out[15] = mat[15];
	}
	return out;
}

/**
 * Get the transpose of 'mat', storing the result in 'out' if provided.
 * @param {m4x4} mat the matrix to transpose
 * @param {m4x4} out (optional) where to write the transposed matrix
 * @returns {m4x4}
 */
export function Transpose(mat, out)
{
	if (mat === out)
	{
		let m01 = a[1], m02 = a[2], m03 = a[3];
		let m12 = a[6], m13 = a[7];
		let m23 = a[11];
		out[ 1] = out[ 4];
		out[ 2] = out[ 8];
		out[ 3] = out[12];
		out[ 6] = out[ 9];
		out[ 7] = out[13];
		out[11] = out[14];
		out[ 4] = m01;
		out[ 8] = m02;
		out[12] = m03;
		out[ 9] = m12;
		out[13] = m13;
		out[14] = m23;
	}
	else
	{
		out = out || create();
		out[ 0] = mat[ 0];
		out[ 1] = mat[ 4];
		out[ 2] = mat[ 8];
		out[ 3] = mat[12];
		out[ 4] = mat[ 1];
		out[ 5] = mat[ 5];
		out[ 6] = mat[ 9];
		out[ 7] = mat[13];
		out[ 8] = mat[ 2];
		out[ 9] = mat[ 6];
		out[10] = mat[10];
		out[11] = mat[14];
		out[12] = mat[ 3];
		out[13] = mat[ 7];
		out[14] = mat[11];
		out[15] = mat[15];
	}

	return out;
}

/**
 * Get the determinant of the rotation part of 'mat'
 * @param {m4x4} mat the matrix to get the determinant of
 * @returns {Number}
 */
export function Determinant3(mat)
{
	return Maths.Triple3(GetX(mat), GetY(mat), GetZ(mat));
}

/**
 * Get the determinant of the matrix 'mat'
 * @param {m4x4} mat the matrix to get the determinant of
 * @returns {Number}
 */
export function Determinant(mat)
{
	let a00 = mat[ 0], a01 = mat[ 1], a02 = mat[ 2], a03 = mat[ 3];
	let a10 = mat[ 4], a11 = mat[ 5], a12 = mat[ 6], a13 = mat[ 7];
	let a20 = mat[ 8], a21 = mat[ 9], a22 = mat[10], a23 = mat[11];
	let a30 = mat[12], a31 = mat[13], a32 = mat[14], a33 = mat[15];

	let b00 = a00 * a11 - a01 * a10;
	let b01 = a00 * a12 - a02 * a10;
	let b02 = a00 * a13 - a03 * a10;
	let b03 = a01 * a12 - a02 * a11;
	let b04 = a01 * a13 - a03 * a11;
	let b05 = a02 * a13 - a03 * a12;
	let b06 = a20 * a31 - a21 * a30;
	let b07 = a20 * a32 - a22 * a30;
	let b08 = a20 * a33 - a23 * a30;
	let b09 = a21 * a32 - a22 * a31;
	let b10 = a21 * a33 - a23 * a31;
	let b11 = a22 * a33 - a23 * a32;

	return b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;
}

/**
 * Returns true if 'mat' is an orthonormal matrix
 * @param {m4x4} mat the matrix to test for orthonormality
 * @returns {boolean} true if orthonormal, false if not
 */
export function IsOrthonormal(mat)
{
	let x_lensq = Maths.LengthSq(GetX(mat));
	let y_lensq = Maths.LengthSq(GetY(mat));
	let z_lensq = Maths.LengthSq(GetZ(mat));
	let det3 = Math.abs(Determinant3(mat));
	return Maths.FEql(x_lensq, 1) && Maths.FEql(y_lensq, 1) && Maths.FEql(z_lensq, 1) && Maths.FEql(det3, 1);
}

/**
 * Orthonormalises the rotation component of the matrix
 * @param {m4x4} mat
 * @param {m4x4} out (optional) where to write the result
 * @returns {m4x4}
 */
export function Orthonorm(mat, out)
{
	out = out || create();
	SetX(out, v4.Normalise(GetX(mat)));
	SetY(out, v4.Normalise(v4.Cross(GetZ(mat), GetX(mat))));
	SetZ(out, v4.Cross(GetX(mat), GetY(mat)));
	//assert(IsOrthonormal(m));
	return out;
}

/**
 * Invert an orthonormal 4x4 matrix.
 * @param {m4x4} mat the matrix to return the inverse of
 * @param {m4x4} out (optional) the matrix to write the result to
 */
export function InvertFast(mat, out)
{
	//if (!IsOrthonormal(mat))
	//	throw new Error("Matrix is not orthonormal");

	let m = Transpose3x3(mat, out);
	m[12] = -Maths.Dot3(GetX(mat), GetW(mat));
	m[13] = -Maths.Dot3(GetY(mat), GetW(mat));
	m[14] = -Maths.Dot3(GetZ(mat), GetW(mat));
	return m; 
}

/**
 * Invert a 4x4 matrix
 * @param {m4x4} mat the matrix to invert
 * @param {m4x4} out (optional) where to write the result
 * @returns {m4x4}
 */
export function Invert(mat, out)
{
	out = out || create();

	let a00 = mat[ 0], a01 = mat[ 1], a02 = mat[ 2], a03 = mat[ 3];
	let a10 = mat[ 4], a11 = mat[ 5], a12 = mat[ 6], a13 = mat[ 7];
	let a20 = mat[ 8], a21 = mat[ 9], a22 = mat[10], a23 = mat[11];
	let a30 = mat[12], a31 = mat[13], a32 = mat[14], a33 = mat[15];

	let b00 = a00 * a11 - a01 * a10;
	let b01 = a00 * a12 - a02 * a10;
	let b02 = a00 * a13 - a03 * a10;
	let b03 = a01 * a12 - a02 * a11;
	let b04 = a01 * a13 - a03 * a11;
	let b05 = a02 * a13 - a03 * a12;
	let b06 = a20 * a31 - a21 * a30;
	let b07 = a20 * a32 - a22 * a30;
	let b08 = a20 * a33 - a23 * a30;
	let b09 = a21 * a32 - a22 * a31;
	let b10 = a21 * a33 - a23 * a31;
	let b11 = a22 * a33 - a23 * a32;

	// Calculate the determinant
	let det = b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;
	if (!det)
		throw new Error("Matrix is singular");
	
	det = 1.0 / det;
	out[ 0] = (a11 * b11 - a12 * b10 + a13 * b09) * det;
	out[ 1] = (a02 * b10 - a01 * b11 - a03 * b09) * det;
	out[ 2] = (a31 * b05 - a32 * b04 + a33 * b03) * det;
	out[ 3] = (a22 * b04 - a21 * b05 - a23 * b03) * det;
	out[ 4] = (a12 * b08 - a10 * b11 - a13 * b07) * det;
	out[ 5] = (a00 * b11 - a02 * b08 + a03 * b07) * det;
	out[ 6] = (a32 * b02 - a30 * b05 - a33 * b01) * det;
	out[ 7] = (a20 * b05 - a22 * b02 + a23 * b01) * det;
	out[ 8] = (a10 * b10 - a11 * b08 + a13 * b06) * det;
	out[ 9] = (a01 * b08 - a00 * b10 - a03 * b06) * det;
	out[10] = (a30 * b04 - a31 * b02 + a33 * b00) * det;
	out[11] = (a21 * b02 - a20 * b04 - a23 * b00) * det;
	out[12] = (a11 * b07 - a10 * b09 - a12 * b06) * det;
	out[13] = (a00 * b09 - a01 * b07 + a02 * b06) * det;
	out[14] = (a31 * b01 - a30 * b03 - a32 * b00) * det;
	out[15] = (a20 * b03 - a21 * b01 + a22 * b00) * det;

	return out;
}

/**
 * Return a 4x4 translation matrix
 * @param {[Number]} pos the position offset
 * @returns {m4x4}
 */
export function Translation(pos)
{
	var out = create();
	out[12] = pos[0];
	out[13] = pos[1];
	out[14] = pos[2];
	return out;
}

/**
 * Create a rotation matrix from Euler angles. Order is: roll, pitch, yaw (to match DirectX)
 * @param {Number} pitch
 * @param {Number} yaw
 * @param {Number} roll
 * @param {v4} pos
 * @returns {m4x4}
 */
export function Euler(pitch, yaw, roll, pos)
{
	let cos_p = Math.cos(pitch), sin_p = Math.sin(pitch);
	let cos_y = Math.cos(yaw  ), sin_y = Math.sin(yaw  );
	let cos_r = Math.cos(roll ), sin_r = Math.sin(roll );
	
	let out = create();
	out[0] = +cos_y*cos_r + sin_y*sin_p*sin_r ; out[1] = cos_p*sin_r ; out[ 2] = -sin_y*cos_r + cos_y*sin_p*sin_r ; out[ 3] = 0;
	out[4] = -cos_y*sin_r + sin_y*sin_p*cos_r ; out[5] = cos_p*cos_r ; out[ 6] = +sin_y*sin_r + cos_y*sin_p*cos_r ; out[ 7] = 0;
	out[8] =  sin_y*cos_p                     ; out[9] = -sin_p      ; out[10] = cos_y*cos_p                      ; out[11] = 0;
	out[12] = pos[0]                          ; out[13] = pos[1]     ; out[14] = pos[2]                           ; out[15] = 1;
	return out;
}

/**
 * Create a scale matrix
 * @param {Number} sx 
 * @param {Number} sy 
 * @param {Number} sz 
 * @param {v4} pos 
 * @returns {m4x4}
 */
export function Scale(sx, sy, sz, pos)
{
	let out = create();
	out[ 0] = sx;
	out[ 5] = sy;
	out[10] = sz;
	out[12] = pos[0];
	out[13] = pos[1];
	out[14] = pos[2];
	return out;
}

/**
 * Pre-multiply 'mat' by a translation of 'ofs'
 * @param {m4x4} mat the matrix to translate
 * @param {v4} ofs the distance to translate by
 * @param {m4x4} out (optional) the matrix to write the result to
 * @return {m4x4} the translated matrix
 */
export function Translate(mat, ofs, out)
{
	let x = ofs[0], y = ofs[1], z = ofs[2];
	if (mat === out)
	{
		out[12] = mat[0] * x + mat[4] * y + mat[ 8] * z + mat[12];
		out[13] = mat[1] * x + mat[5] * y + mat[ 9] * z + mat[13];
		out[14] = mat[2] * x + mat[6] * y + mat[10] * z + mat[14];
		out[15] = mat[3] * x + mat[7] * y + mat[11] * z + mat[15];
	}
	else
	{
		out = out || create();

		let a00 = mat[0], a01 = mat[1], a02 = mat[ 2], a03 = mat[ 3];
		let a10 = mat[4], a11 = mat[5], a12 = mat[ 6], a13 = mat[ 7];
		let a20 = mat[8], a21 = mat[9], a22 = mat[10], a23 = mat[11];

		out[0] = a00; out[1] = a01; out[ 2] = a02; out[ 3] = a03;
		out[4] = a10; out[5] = a11; out[ 6] = a12; out[ 7] = a13;
		out[8] = a20; out[9] = a21; out[10] = a22; out[11] = a23;

		out[12] = a00 * x + a10 * y + a20 * z + mat[12];
		out[13] = a01 * x + a11 * y + a21 * z + mat[13];
		out[14] = a02 * x + a12 * y + a22 * z + mat[14];
		out[15] = a03 * x + a13 * y + a23 * z + mat[15];
	}

	return out;
}

/**
 * Pre-multiple 'mat' by a rotation of 'angle' radians about 'axis'
 * @param {m4x4} mat the matrix to rotate
 * @param {Number} angle the angle to rotate by (in radians)
 * @param {v4} axis the axis to rotate around
 * @param {m4x4} out (optional) the receiving matrix
 * @returns {m4x4} 'mat' pre-multiplied by the rotation
 */
export function Rotate(mat, angle, axis, out)
{
	let len = Maths.Length(axis);
	if (Math.abs(len) < Maths.Tiny)
		throw new Error("Axis of rotation is a zero vector");
		
	let a00 = mat[0], a01 = mat[1], a02 = mat[ 2], a03 = mat[ 3];
	let a10 = mat[4], a11 = mat[5], a12 = mat[ 6], a13 = mat[ 7];
	let a20 = mat[8], a21 = mat[9], a22 = mat[10], a23 = mat[11];
	
	let s = Math.sin(angle);
	let c = Math.cos(angle);
	let t = 1 - c;

	let x = axis[0], y = axis[1], z = axis[2];
	len = 1 / len;
	x *= len;
	y *= len;
	z *= len;

	// Construct the elements of the rotation matrix
	let b00 = x * x * t + c,     b01 = y * x * t + z * s, b02 = z * x * t - y * s;
	let b10 = x * y * t - z * s, b11 = y * y * t + c,     b12 = z * y * t + x * s;
	let b20 = x * z * t + y * s, b21 = y * z * t - x * s, b22 = z * z * t + c;

	out = out || create();

	// Perform rotation-specific matrix multiplication
	out[ 0] = a00 * b00 + a10 * b01 + a20 * b02;
	out[ 1] = a01 * b00 + a11 * b01 + a21 * b02;
	out[ 2] = a02 * b00 + a12 * b01 + a22 * b02;
	out[ 3] = a03 * b00 + a13 * b01 + a23 * b02;
	out[ 4] = a00 * b10 + a10 * b11 + a20 * b12;
	out[ 5] = a01 * b10 + a11 * b11 + a21 * b12;
	out[ 6] = a02 * b10 + a12 * b11 + a22 * b12;
	out[ 7] = a03 * b10 + a13 * b11 + a23 * b12;
	out[ 8] = a00 * b20 + a10 * b21 + a20 * b22;
	out[ 9] = a01 * b20 + a11 * b21 + a21 * b22;
	out[10] = a02 * b20 + a12 * b21 + a22 * b22;
	out[11] = a03 * b20 + a13 * b21 + a23 * b22;

	// If the source and destination differ, copy the unchanged last row
	if (mat !== out)
	{
		out[12] = mat[12];
		out[13] = mat[13];
		out[14] = mat[14];
		out[15] = mat[15];
	}
	return out;
}

/**
 * Construct an orthographic projection matrix
 * @param {Number} w the width of the viewport
 * @param {Number} h the height of the viewport
 * @param {Number} zn the distance to the near plane
 * @param {Number} zf the distance to the far plane
 * @param {boolean} righthanded true if the projection is right-handed
 * @returns {m4x4} the projection matrix
 */
export function ProjectionOrthographic(w, h, zn, zf, righthanded)
{
	//assert("invalid view rect" && IsFinite(w) && IsFinite(h) && w > 0 && h > 0);
	//assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && (zn - zf) != 0);
	let rh = righthanded ? +1 : -1;
	let mat = create();
	mat[ 0] = 2.0 / w;
	mat[ 5] = 2.0 / h;
	mat[10] = rh / (zn - zf);
	mat[15] = 1.0;
	mat[14] = rh * zn / (zn - zf);
	return mat;
}

/**
 * Construct a perspective projection matrix
 * @param {Number} w the width of the viewport at unit distance from the camera
 * @param {Number} h the height of the viewport at unit distance from the camera
 * @param {Number} zn the distance to the near plane
 * @param {Number} zf the distance to the far plane
 * @param {boolean} righthanded true if the projection is a right-handed projection
 * @returns {m4x4} the projection matrix
 */
export function ProjectionPerspective(w, h, zn, zf, righthanded)
{
	//assert("invalid view rect" && IsFinite(w) && IsFinite(h) && w > 0 && h > 0);
	//assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
	let rh = righthanded ? +1 : -1;
	let mat = create();
	mat[ 0] = 2.0 * zn / w;
	mat[ 5] = 2.0 * zn / h;
	mat[11] = -rh;
	mat[10] = rh * zf / (zn - zf);
	mat[14] = zn * zf / (zn - zf);
	return mat;
}

/**
 * Construct a perspective projection matrix offset from the centre
 * @param {Number} l the left side of the view frustum
 * @param {Number} r the right side of the view frustum
 * @param {Number} t the top of the view frustum
 * @param {Number} b the bottom of the view frustum
 * @param {Number} zn the distance to the near plane
 * @param {Number} zf the distance to the far plane
 * @param {boolean} righthanded true if the projection is a right-handed projection
 * @returns {m4x4} the projection matrix
 */
export function ProjectionPerspectiveLRTB(l, r, t, b, zn, zf, righthanded)
{
	//assert("invalid view rect" && IsFinite(l)  && IsFinite(r) && IsFinite(t) && IsFinite(b) && (r - l) > 0 && (t - b) > 0);
	//assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
	let rh = righthanded ? +1 : -1;
	let mat = create();
	mat[ 0] = 2.0 * zn / (r - l);
	mat[ 5] = 2.0 * zn / (t - b);
	mat[ 8] = rh * (r + l) / (r - l);
	mat[ 9] = rh * (t + b) / (t - b);
	mat[11] = -rh;
	mat[10] = rh * zf / (zn - zf);
	mat[14] = zn * zf / (zn - zf);
	return mat;
}

/**
 * Construct a perspective projection matrix using field of view
 * @param {Number} fovY the field of view in the Y direction (i.e. vertically)
 * @param {Number} aspect the aspect ratio of the view
 * @param {Number} zn the distance to the near plane
 * @param {Number} zf the distance to the far plane
 * @param {boolean} righthanded true if the projection is a right-handed projection
 * @returns {m4x4} the projection matrix
 */
export function ProjectionPerspectiveFOV(fovY, aspect, zn, zf, righthanded)
{
	//assert("invalid aspect ratio" && IsFinite(aspect) && aspect > 0);
	//assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
	let rh = righthanded ? +1 : -1;
	let mat = create();
	mat[ 5] = 1.0 / Math.tan(fovY/2);
	mat[ 0] = mat[ 5] / aspect;
	mat[11] = -rh;
	mat[10] = rh * zf / (zn - zf);
	mat[14] = zn * zf / (zn - zf);
	return mat;
}

/**
 * Create a "look-at" matrix
 * @param {v4} pos the position of the camera
 * @param {v4} focus_point the point to look at
 * @param {v4} up the up direction of the camera in world space
 * @param {m4x4} out (optional) where the matrix is written to
 * @returns {mat4} the camera to world transform
 */
export function LookAt(pos, focus_point, up, out)
{
	// Z vector
	let z = v4.Sub(pos, focus_point);
	let zlen = v4.Length(z);
	if (zlen < Maths.Tiny) throw new Error("position and focus point are coincident");
	v4.MulS(z, 1/zlen, z);	

	// X vector
	let x = v4.Cross(up, z);
	let xlen = v4.Length(x);
	if (xlen < Maths.Tiny) throw new Error("Focus point is aligned with the up direction");
	v4.MulS(x, 1/xlen, x);

	// Y vector
	let y = v4.Cross(z, x);

	out = out || create();
	set(out, x, y, z, pos);
	return out;
}
