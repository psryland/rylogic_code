"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const Bits = require("./bits");
exports.Bits = Bits;
// Epsilon
exports.Tiny = 1.00000007e-05;
exports.TinySq = 1.00000015e-10;
exports.TinySqrt = 3.16227786e-03;
// Constants
exports.Phi = 1.618033988749894848204586834; // "Golden Ratio"
exports.Tau = 6.283185307179586476925286766559; // circle constant
exports.InvTau = 1.0 / exports.Tau;
exports.TauBy2 = exports.Tau / 2.0;
exports.TauBy3 = exports.Tau / 3.0;
exports.TauBy4 = exports.Tau / 4.0;
exports.TauBy5 = exports.Tau / 5.0;
exports.TauBy6 = exports.Tau / 6.0;
exports.TauBy7 = exports.Tau / 7.0;
exports.TauBy8 = exports.Tau / 8.0;
exports.TauBy10 = exports.Tau / 10.0;
exports.TauBy16 = exports.Tau / 16.0;
exports.TauBy32 = exports.Tau / 32.0;
exports.TauBy360 = exports.Tau / 360.0;
exports._360ByTau = 360.0 / exports.Tau;
exports.Root2 = 1.4142135623730950488016887242097;
exports.Root3 = 1.7320508075688772935274463415059;
exports.InvRoot2 = 1.0 / 1.4142135623730950488016887242097;
exports.InvRoot3 = 1.0 / 1.7320508075688772935274463415059;
/**
 * Floating point comparison
 * @param a the first value to compare
 * @param b the second value to compare
 * @param tol the tolerance value. *WARNING* 'tol' is a relative tolerance, relative to the largest of 'a' or 'b'
 * @returns true if the values are equal within 'tol*largest(a,b)'
 */
function FEqlRelative(a, b, tol) {
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
    if (b == 0)
        return Math.abs(a) < tol;
    if (a == 0)
        return Math.abs(b) < tol;
    // Handle infinities and exact values
    if (a == b)
        return true;
    // When float operations are performed at compile time, the compiler warnings about 'inf'
    let diff = a - b;
    // Test relative error as a fraction of the largest value
    return Math.abs(diff) < tol * Math.max(Math.abs(a), Math.abs(b));
}
exports.FEqlRelative = FEqlRelative;
/**
 * Compare 'a' and 'b' for approximate equality
 * @param a the first value to compare
 * @param b the second value to compare
 * @returns true if the values are equal within 'Tiny*largest(a,b)'
 */
function FEql(a, b) {
    // Don't add a 'tol' parameter because it looks like the function
    // should perform a == b +- tol, which isn't what it does.
    return FEqlRelative(a, b, exports.Tiny);
}
exports.FEql = FEql;
/**
 * Compare all elements of 'a' and 'b' for exact equality.
 * @param a The array to compare with 'b'
 * @param b The array to compare with 'a'
 * @param start (optional) The index of the element to begin comparing from
 * @param count (optional) The index of the element to begin comparing from
 * @returns True if the compared values are exactly equal
 */
function EqlN(a, b, start, count) {
    if (start === undefined) {
        if (a.length != b.length)
            return false;
        let i = 0, iend = a.length;
        for (; i != iend && a[i] == b[i]; ++i) { }
        return i == iend;
    }
    else {
        if (count === undefined)
            count = Math.max(a.length - start, b.length - start);
        if (a.length < start + count)
            return false;
        if (b.length < start + count)
            return false;
        let i = start, iend = start + count;
        for (; i != iend && a[i] == b[i]; ++i) { }
        return i == iend;
    }
}
exports.EqlN = EqlN;
/**
 * IEEERemainder
 * @returns The IEEE remainder of x % y
 */
function IEEERemainder(x, y) {
    let mod = x % y;
    if (isNaN(mod))
        return NaN;
    if (mod == 0)
        if (x < -0.0)
            return -0.0;
    let alt_mod = mod - (Math.abs(y) * Sign(x));
    if (Math.abs(alt_mod) == Math.abs(mod)) {
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
exports.IEEERemainder = IEEERemainder;
/**
 * Clamp 'x' to the given inclusive range [min,max]
 * @param x The value to be clamped
 * @param mn The inclusive minimum value
 * @param mx The inclusive maximum value
 * @returns 'x' clamped to the inclusive range [mn,mx]
 */
function Clamp(x, mn, mx) {
    if (mn > mx)
        throw new Error("[min,max] must be a positive range");
    return (mx < x) ? mx : (x < mn) ? mn : x;
}
exports.Clamp = Clamp;
/**
 * Return the square of 'x'
 * @param x
 * @returns The result of x * x
 */
function Sqr(x) {
    return x * x;
}
exports.Sqr = Sqr;
/**
 * Return +1 or -1 depending on the sign of 'x'
 * @param x The value to return the sign of
 * @returns +1 if 'x >= 0', -1 if 'x < 0'
 */
function Sign(x) {
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
exports.Sign = Sign;
/**
 * Return Log base 10 of 'x'
 * @param x
 */
function Log10(x) {
    return Math.log(x) * Math.LOG10E;
}
exports.Log10 = Log10;
/**
 * Compute the squared length of an array of numbers
 * @param vec the vector to compute the squared length of
 * @returns The squared length of the vector
 */
function LengthSq(vec) {
    let sum_sq = 0;
    for (let i = 0; i != vec.length; ++i)
        sum_sq += vec[i] * vec[i];
    return sum_sq;
}
exports.LengthSq = LengthSq;
/**
 * Compute the length of an array of numbers.
 * @param vec the vector to find the length of
 * @returns The length of the vector
 */
function Length(vec) {
    return Math.sqrt(LengthSq(vec));
}
exports.Length = Length;
/**
 * Normalise a vector to length = 1 in place
 * @param vec The array to normalise
 * @param opts Parameters: {def:[..] = default value to return if the length of 'vec' is zero}
 * @returns The vector with components normalised
 */
function Normalise(vec, opts) {
    let len = Length(vec);
    if (len <= 0) {
        if (opts && opts.def)
            return opts.def;
        throw new Error("Cannot normalise a zero vector");
    }
    for (let i = 0; i != vec.length; ++i) {
        vec[i] /= len;
    }
    return vec;
}
exports.Normalise = Normalise;
/**
 * Return the component-wise sum of two arrays: a + b
 * @param a
 * @param b
 * @param out Where to write the result
 * @returns The result of 'a + b'
 */
function Add(a, b, out) {
    if (a.length != b.length)
        throw new Error("Vectors must have the same length");
    out = out || a.slice();
    for (let i = 0; i != a.length; ++i)
        out[i] = a[i] + b[i];
    return out;
}
exports.Add = Add;
/**
 * Return the sum of a collection of vectors
 * @param vec
 * @returns The result of 'a + b + c + ...'
 */
function AddN(...vec) {
    let sum = vec[0].slice();
    for (let i = 0; i != vec.length; ++i)
        Add(sum, vec[i], sum);
    return sum;
}
exports.AddN = AddN;
/**
 * Compute the dot product of two arrays
 * @param a the first vector
 * @param b the second vector
 * @returns The dot product
 */
function Dot(a, b) {
    if (a.length != b.length)
        throw new Error("Dot product requires vectors of equal length.");
    let dp = 0;
    for (let i = 0; i != a.length; ++i)
        dp += a[i] * b[i];
    return dp;
}
exports.Dot = Dot;
// // Workarounds
// Math["log10"] = Math["log10"]:function || Math_.Log10;
// Math["sign"] = Math["sign"]:function || Math_.Sign;
//# sourceMappingURL=maths.js.map