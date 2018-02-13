(function webpackUniversalModuleDefinition(root, factory) {
	if(typeof exports === 'object' && typeof module === 'object')
		module.exports = factory();
	else if(typeof define === 'function' && define.amd)
		define([], factory);
	else {
		var a = factory();
		for(var i in a) (typeof exports === 'object' ? exports : root)[i] = a[i];
	}
})(typeof self !== 'undefined' ? self : this, function() {
return /******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, {
/******/ 				configurable: false,
/******/ 				enumerable: true,
/******/ 				get: getter
/******/ 			});
/******/ 		}
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = 2);
/******/ })
/************************************************************************/
/******/ ([
/* 0 */
/***/ (function(module, exports, __webpack_require__) {

(function webpackUniversalModuleDefinition(root, factory) {
	if(true)
		module.exports = factory();
	else if(typeof define === 'function' && define.amd)
		define([], factory);
	else {
		var a = factory();
		for(var i in a) (typeof exports === 'object' ? exports : root)[i] = a[i];
	}
})(typeof self !== 'undefined' ? self : this, function() {
return /******/ (function(modules) { // webpackBootstrap
/******/ 	// The module cache
/******/ 	var installedModules = {};
/******/
/******/ 	// The require function
/******/ 	function __webpack_require__(moduleId) {
/******/
/******/ 		// Check if module is in cache
/******/ 		if(installedModules[moduleId]) {
/******/ 			return installedModules[moduleId].exports;
/******/ 		}
/******/ 		// Create a new module (and put it into the cache)
/******/ 		var module = installedModules[moduleId] = {
/******/ 			i: moduleId,
/******/ 			l: false,
/******/ 			exports: {}
/******/ 		};
/******/
/******/ 		// Execute the module function
/******/ 		modules[moduleId].call(module.exports, module, module.exports, __webpack_require__);
/******/
/******/ 		// Flag the module as loaded
/******/ 		module.l = true;
/******/
/******/ 		// Return the exports of the module
/******/ 		return module.exports;
/******/ 	}
/******/
/******/
/******/ 	// expose the modules object (__webpack_modules__)
/******/ 	__webpack_require__.m = modules;
/******/
/******/ 	// expose the module cache
/******/ 	__webpack_require__.c = installedModules;
/******/
/******/ 	// define getter function for harmony exports
/******/ 	__webpack_require__.d = function(exports, name, getter) {
/******/ 		if(!__webpack_require__.o(exports, name)) {
/******/ 			Object.defineProperty(exports, name, {
/******/ 				configurable: false,
/******/ 				enumerable: true,
/******/ 				get: getter
/******/ 			});
/******/ 		}
/******/ 	};
/******/
/******/ 	// getDefaultExport function for compatibility with non-harmony modules
/******/ 	__webpack_require__.n = function(module) {
/******/ 		var getter = module && module.__esModule ?
/******/ 			function getDefault() { return module['default']; } :
/******/ 			function getModuleExports() { return module; };
/******/ 		__webpack_require__.d(getter, 'a', getter);
/******/ 		return getter;
/******/ 	};
/******/
/******/ 	// Object.prototype.hasOwnProperty.call
/******/ 	__webpack_require__.o = function(object, property) { return Object.prototype.hasOwnProperty.call(object, property); };
/******/
/******/ 	// __webpack_public_path__
/******/ 	__webpack_require__.p = "";
/******/
/******/ 	// Load entry module and return exports
/******/ 	return __webpack_require__(__webpack_require__.s = 8);
/******/ })
/************************************************************************/
/******/ ([
/* 0 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.InvRoot3 = exports.InvRoot2 = exports.Root3 = exports.Root2 = exports._360ByTau = exports.TauBy360 = exports.TauBy32 = exports.TauBy16 = exports.TauBy10 = exports.TauBy8 = exports.TauBy7 = exports.TauBy6 = exports.TauBy5 = exports.TauBy4 = exports.TauBy3 = exports.TauBy2 = exports.InvTau = exports.Tau = exports.Phi = exports.TinySqrt = exports.TinySq = exports.Tiny = exports.BBox = exports.Rect = exports.Range = exports.m4x4 = exports.v4 = exports.v2 = undefined;
exports.FEqlRelative = FEqlRelative;
exports.FEql = FEql;
exports.EqlN = EqlN;
exports.IEEERemainder = IEEERemainder;
exports.Clamp = Clamp;
exports.Sqr = Sqr;
exports.LengthSq = LengthSq;
exports.Length = Length;
exports.Normalise = Normalise;
exports.Add = Add;
exports.AddN = AddN;
exports.Dot = Dot;
exports.Dot2 = Dot2;
exports.Dot3 = Dot3;
exports.Dot4 = Dot4;
exports.Cross3 = Cross3;
exports.Triple3 = Triple3;

var _v = __webpack_require__(6);

var v2 = _interopRequireWildcard(_v);

var _v2 = __webpack_require__(1);

var v4 = _interopRequireWildcard(_v2);

var _m4x = __webpack_require__(2);

var m4x4 = _interopRequireWildcard(_m4x);

var _range = __webpack_require__(10);

var Range = _interopRequireWildcard(_range);

var _rect = __webpack_require__(5);

var Rect = _interopRequireWildcard(_rect);

var _bbox = __webpack_require__(4);

var BBox = _interopRequireWildcard(_bbox);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

/**
 * @module Maths
 */

"use strict";

exports.v2 = v2;
exports.v4 = v4;
exports.m4x4 = m4x4;
exports.Range = Range;
exports.Rect = Rect;
exports.BBox = BBox;

// Epsilon

var Tiny = exports.Tiny = 1.00000007e-05;
var TinySq = exports.TinySq = 1.00000015e-10;
var TinySqrt = exports.TinySqrt = 3.16227786e-03;

// Constants
var Phi = exports.Phi = 1.618033988749894848204586834; // "Golden Ratio"
var Tau = exports.Tau = 6.283185307179586476925286766559; // circle constant
var InvTau = exports.InvTau = 1.0 / Tau;
var TauBy2 = exports.TauBy2 = Tau / 2.0;
var TauBy3 = exports.TauBy3 = Tau / 3.0;
var TauBy4 = exports.TauBy4 = Tau / 4.0;
var TauBy5 = exports.TauBy5 = Tau / 5.0;
var TauBy6 = exports.TauBy6 = Tau / 6.0;
var TauBy7 = exports.TauBy7 = Tau / 7.0;
var TauBy8 = exports.TauBy8 = Tau / 8.0;
var TauBy10 = exports.TauBy10 = Tau / 10.0;
var TauBy16 = exports.TauBy16 = Tau / 16.0;
var TauBy32 = exports.TauBy32 = Tau / 32.0;
var TauBy360 = exports.TauBy360 = Tau / 360.0;
var _360ByTau = exports._360ByTau = 360.0 / Tau;
var Root2 = exports.Root2 = 1.4142135623730950488016887242097;
var Root3 = exports.Root3 = 1.7320508075688772935274463415059;
var InvRoot2 = exports.InvRoot2 = 1.0 / 1.4142135623730950488016887242097;
var InvRoot3 = exports.InvRoot3 = 1.0 / 1.7320508075688772935274463415059;

/**
 * Floating point comparison
 * @param {Number} a the first value to compare
 * @param {Number} b the second value to compare
 * @param {Number} tol the tolerance value. *WARNING* 'tol' is a relative tolerance, relative to the largest of 'a' or 'b'
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
	if (b == 0) return Math.abs(a) < tol;
	if (a == 0) return Math.abs(b) < tol;

	// Handle infinities and exact values
	if (a == b) return true;

	// When float operations are performed at compile time, the compiler warnings about 'inf'
	var diff = a - b;

	// Test relative error as a fraction of the largest value
	return Math.abs(diff) < tol * Math.max(Math.abs(a), Math.abs(b));
}

/**
 * Compare 'a' and 'b' for approximate equality
 * @param {Number} a the first value to compare
 * @param {Number} b the second value to compare
 * @returns {boolean} true if the values are equal within 'Tiny*largest(a,b)'
 */
function FEql(a, b) {
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
function EqlN(a, b, start, count) {
	if (start === undefined) {
		if (a.length != b.length) return false;
		var i = 0,
		    iend = a.length;
		for (; i != iend && a[i] == b[i]; ++i) {}
		return i == iend;
	} else {
		if (count === undefined) count = Math.max(a.length - start, b.length - start);
		if (a.length < start + count) return false;
		if (b.length < start + count) return false;
		var _i = start,
		    _iend = start + count;
		for (; _i != _iend && a[_i] == b[_i]; ++_i) {}
		return _i == _iend;
	}
}

/**
 * IEEERemainder
 */
function IEEERemainder(x, y) {
	var mod = x % y;
	if (isNaN(mod)) return NaN;

	if (mod == 0) if (x < -0.0) return -0.0;

	var alt_mod = mod - Math.abs(y) * Math.sign(x);
	if (Math.abs(alt_mod) == Math.abs(mod)) {
		var div = x / y;
		var div_rounded = Math.round(div);
		if (Math.abs(div_rounded) > Math.abs(div)) return alt_mod;

		return mod;
	}

	if (Math.abs(alt_mod) < Math.abs(mod)) return alt_mod;

	return mod;
}

/**
 * Clamp 'x' to the given inclusive range [min,max]
 * @param {Number} x The value to be clamped
 * @param {Number} mn The inclusive minimum value
 * @param {Number} mx The inclusive maximum value
 * @returns {Number}
 */
function Clamp(x, mn, mx) {
	//assert(mn <= mx, "[min,max] must be a positive range");
	return mx < x ? mx : x < mn ? mn : x;
}

/**
 * Return the square of 'x'
 * @param {Number} x
 * @returns {Number}
 */
function Sqr(x) {
	return x * x;
}

/**
 * Compute the squared length of a vector of numbers
 * @param {[Number]} vec the vector to compute the squared length of
 * @returns {Number} the squared length of the vector
 */
function LengthSq(vec) {
	var sum_sq = 0;
	for (var i = 0; i != vec.length; ++i) {
		sum_sq += vec[i] * vec[i];
	}return sum_sq;
}

/**
 * Compute the length of a vector of numbers
 * @param {[Number]} vec the vector to find the length of
 * @returns {Number} the length of the vector
 */
function Length(vec) {
	return Math.sqrt(LengthSq(vec));
}

/**
 * Normalise a vector to length = 1 in place
 * @param {[Number]} vec the vector to normalise
 * @param {Object} opts optional parameters: {def:[..]}
 * @returns {[Number]} the vector with components normalised
 */
function Normalise(vec, opts) {
	var len = Length(vec);
	if (len <= 0) {
		if (opts && opts.def) return opts.def;
		throw new Error("Cannot normalise a zero vector");
	}
	for (var i = 0; i != vec.length; ++i) {
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
function Add(a, b, out) {
	if (a.length != b.length) throw new Error("Vectors must have the same length");

	out = out || a.slice();
	for (var i = 0; i != a.length; ++i) {
		out[i] = a[i] + b[i];
	}return out;
}

/**
 * Return the sum of a collection of vectors
 * @param {[[Number]]} vec 
 * @returns {[Number]}
 */
function AddN() {
	for (var _len = arguments.length, vec = Array(_len), _key = 0; _key < _len; _key++) {
		vec[_key] = arguments[_key];
	}

	var sum = vec[0].slice();
	for (var i = 0; i != vec.length; ++i) {
		Add(sum, vec[i], sum);
	}return sum;
}

/**
 * Compute the dot product of two vectors
 * @param {[Number]} a the first vector
 * @param {[Number]} b the second vector
 * @returns {Number} the dot product
 */
function Dot(a, b) {
	if (a.length != b.length) throw new Error("Dot product requires vectors of equal length.");

	var dp = 0;
	for (var i = 0; i != a.length; ++i) {
		dp += a[i] * b[i];
	}return dp;
}
function Dot2(a, b) {
	return a[0] * b[0] + a[1] * b[1];
}
function Dot3(a, b) {
	return a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
}
function Dot4(a, b) {
	return a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3];
}

// Cross product: a x b
function Cross3(a, b) {
	return v4.make(a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0], 0);
}

// Triple product: a . b x c
function Triple3(a, b, c) {
	return Dot3(a, Cross3(b, c));
}

// Workarounds
Math.log10 = Math.log10 || function (x) {
	return Math.log(x) * Math.LOG10E;
};
Math.sign = Math.sign || function (x) {
	// If x is NaN, the result is NaN.
	// If x is -0, the result is -0.
	// If x is +0, the result is +0.
	// If x is negative and not -0, the result is -1.
	// If x is positive and not +0, the result is +1.
	return (x > 0) - (x < 0) || +x;
	// A more aesthetical persuado-representation is shown below
	// ( (x > 0) ? 0 : 1 )  // if x is negative then negative one
	//          +           // else (because you cant be both - and +)
	// ( (x < 0) ? 0 : -1 ) // if x is positive then positive one
	//         ||           // if x is 0, -0, or NaN, or not a number,
	//         +x           // Then the result will be x, (or) if x is
	//                      // not a number, then x converts to number
};

/***/ }),
/* 1 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.Origin = exports.WAxis = exports.ZAxis = exports.YAxis = exports.XAxis = exports.Zero = undefined;
exports.create = create;
exports.make = make;
exports.set = set;
exports.clone = clone;
exports.Eql = Eql;
exports.FEql = FEql;
exports.IsNaN = IsNaN;
exports.Neg = Neg;
exports.SetW1 = SetW1;
exports.Abs = Abs;
exports.Clamp = Clamp;
exports.LengthSq = LengthSq;
exports.Length = Length;
exports.Normalise = Normalise;
exports.Dot = Dot;
exports.Cross = Cross;
exports.Add = Add;
exports.AddN = AddN;
exports.Sub = Sub;
exports.MulS = MulS;
exports.MulV = MulV;
exports.Parallel = Parallel;
exports.CreateNotParallelTo = CreateNotParallelTo;
exports.Perpendicular = Perpendicular;

var _maths = __webpack_require__(0);

var Maths = _interopRequireWildcard(_maths);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

var FVec = Float32Array; /**
                          * @module v4
                          */

var Zero = exports.Zero = new FVec([0, 0, 0, 0]);
var XAxis = exports.XAxis = new FVec([1, 0, 0, 0]);
var YAxis = exports.YAxis = new FVec([0, 1, 0, 0]);
var ZAxis = exports.ZAxis = new FVec([0, 0, 1, 0]);
var WAxis = exports.WAxis = new FVec([0, 0, 0, 1]);
var Origin = exports.Origin = new FVec([0, 0, 0, 1]);

/**
 * Create a 4-vector containing zeros
 * @returns {v4}
 */
function create() {
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
function make(x, y, z, w) {
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
function set(vec, x, y, z, w) {
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
function clone(vec, out) {
  out = out || create();
  if (vec !== out) {
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
function Eql(a, b) {
  var eql = a[0] == b[0] && a[1] == b[1] && a[2] == b[2] && a[3] == b[3];
  return eql;
}

/**
 * Approximate equality of two 4-vectors
 * @param {v4} a
 * @param {v4} b
 * @returns {boolean}
 */
function FEql(a, b) {
  var feql = Maths.FEql(a[0], b[0]) && Maths.FEql(a[1], b[1]) && Maths.FEql(a[2], b[2]) && Maths.FEql(a[3], b[3]);
  return feql;
}

/**
 * Returns true if 'vec' contains any 'NaN's
 * @param {v4} vec 
 * @returns {boolean}
 */
function IsNaN(vec) {
  var is_nan = isNaN(vec[0]) || isNaN(vec[1]) || isNaN(vec[2]) || isNaN(vec[3]);
  return is_nan;
}

/**
 * Return the negation of 'vec'
 * @param {v4} vec the vector to negate
 * @param {v4} out (optional) the vector to write to
 * @returns {v4} the negative of 'vec'
 */
function Neg(vec, out) {
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
function SetW1(vec) {
  vec[3] = 1;
  return vec;
}

/**
 * Absolute value of a 4-vector (component-wise)
 * @param {v4} vec the vector to find the absolute value of
 * @param {v4} out (optional) where to write the result
 * @returns {v4}
 */
function Abs(vec, out) {
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
function Clamp(vec, min, max, out) {
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
function LengthSq(vec) {
  return vec[0] * vec[0] + vec[1] * vec[1] + vec[2] * vec[2] + vec[3] * vec[3];
}

/**
 * Compute the length of a 4-vector
 * @param {v4} vec the vector to find the length of
 * @returns {Number} the length of the vector
 */
function Length(vec) {
  return Math.sqrt(LengthSq(vec));
}

/**
 * Normalise a 4-vector to length = 1
 * @param {v4} vec the vector to normalise
 * @param {v4} out (optional) the vector to write the result to
 * @param {Object} opts optional parameters: {def:[..]}
 * @returns {v4} the vector with components normalised
 */
function Normalise(vec, out, opts) {
  out = out || create();
  var len = Length(vec);
  if (len == 0) {
    if (opts && opts.def) out = opts.def;else throw new Error("Cannot normalise a zero vector");
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
function Dot(a, b) {
  return a[0] * b[0] + a[1] * b[1] + a[2] * b[2] + a[3] * b[3];
}

/**
 * Cross product: a x b
 * @param {v4} a
 * @param {v4} b
 * @param {v4} out (optional) where the result is written to
 * @returns {v4}
 */
function Cross(a, b, out) {
  out = out || create();
  set(out, a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0], 0);
  return out;
}

/**
 * Return 'a + b'
 * @param {v4} a
 * @param {v4} b
 * @param {v4} out (optional) where to write the result
 * @returns {v4}
 */
function Add(a, b, out) {
  out = out || create();
  set(out, a[0] + b[0], a[1] + b[1], a[2] + b[2], a[3] + b[3]);
  return out;
}

/**
 * Add an array of vectors
 * @param {[v4]} arr 
 * @returns {v4} The sum of the given vectors
 */
function AddN() {
  var sum = create();

  for (var _len = arguments.length, arr = Array(_len), _key = 0; _key < _len; _key++) {
    arr[_key] = arguments[_key];
  }

  for (var i = 0; i != arr.length; ++i) {
    Add(arr[i], sum, sum);
  }return sum;
}

/**
 * Return 'a - b'
 * @param {v4} a
 * @param {v4} b
 * @param {v4} out (optional) where the result is written
 * @returns {v4}
 */
function Sub(a, b, out) {
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
function MulS(a, b, out) {
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
function MulV(a, b, out) {
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
function Parallel(v0, v1) {
  return LengthSq(Cross(v0, v1)) < Maths.TinySq;
}

/**
 * Returns a vector guaranteed not parallel to 'v'
 * @param {v4} v
 * @returns {v4}
 */
function CreateNotParallelTo(v) {
  var x_aligned = Math.abs(v[0]) > Math.abs(v[1]) && Math.abs(v[0]) > Math.abs(v[2]);
  var out = make(!x_aligned, 0, x_aligned, v[3]);
}

/**
 * Returns a vector perpendicular to 'vec' favouring 'previous' as the preferred perpendicular.
 * @param {v4} vec The vector to be perpendicular to.
 * @param {v4} previous (optional) The previously used perpendicular, for returning consistent results.
 * @returns {v4}
 */
function Perpendicular(vec, previous) {
  if (LengthSq(vec) < Maths.Tiny) throw new Error("Cannot make a perpendicular to a zero vector");

  var out = create();

  // If 'previous' is parallel to 'vec', choose a new perpendicular (includes previous == zero)
  if (!previous || Parallel(vec, previous)) {
    Cross(vec, CreateNotParallelTo(vec), out);
    MulVS(out, Length(vec) / Length(out), out);
  } else {
    // If 'previous' is still perpendicular, keep it
    if (Maths.FEql(Dot(vec, previous), 0)) return previous;

    // Otherwise, make a perpendicular that is close to 'previous'
    Cross(vec, previous, out);
    Cross(out, vec, out);
    Normalise(out, out);
  }
  return out;
}

/***/ }),
/* 2 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.create = create;
exports.make = make;
exports.set = set;
exports.clone = clone;
exports.Identity = Identity;
exports.GetX = GetX;
exports.SetX = SetX;
exports.GetY = GetY;
exports.SetY = SetY;
exports.GetZ = GetZ;
exports.SetZ = SetZ;
exports.GetW = GetW;
exports.SetW = SetW;
exports.GetI = GetI;
exports.SetI = SetI;
exports.MulMV = MulMV;
exports.MulMM = MulMM;
exports.MulMB = MulMB;
exports.Transpose3x3 = Transpose3x3;
exports.Transpose = Transpose;
exports.Determinant3 = Determinant3;
exports.Determinant = Determinant;
exports.IsOrthonormal = IsOrthonormal;
exports.Orthonorm = Orthonorm;
exports.InvertFast = InvertFast;
exports.Invert = Invert;
exports.Translation = Translation;
exports.Euler = Euler;
exports.Scale = Scale;
exports.Translate = Translate;
exports.Rotate = Rotate;
exports.ProjectionOrthographic = ProjectionOrthographic;
exports.ProjectionPerspective = ProjectionPerspective;
exports.ProjectionPerspectiveLRTB = ProjectionPerspectiveLRTB;
exports.ProjectionPerspectiveFOV = ProjectionPerspectiveFOV;
exports.LookAt = LookAt;

var _maths = __webpack_require__(0);

var Maths = _interopRequireWildcard(_maths);

var _v = __webpack_require__(1);

var v4 = _interopRequireWildcard(_v);

var _bbox = __webpack_require__(4);

var BBox = _interopRequireWildcard(_bbox);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

var FMat = Float32Array;

/**
 * Create a new 4x4 identity matrix
 * @returns {m4x4}
 */
/**
 * @module m4x4
 */

function create() {
	var out = new FMat(16);
	out[0] = 1;
	out[5] = 1;
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
function make(x, y, z, w) {
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
function set(mat, x, y, z, w) {
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
function clone(mat, out) {
	out = out || create();
	if (mat !== out) {
		out[0] = mat[0];
		out[1] = mat[1];
		out[2] = mat[2];
		out[3] = mat[3];
		out[4] = mat[4];
		out[5] = mat[5];
		out[6] = mat[6];
		out[7] = mat[7];
		out[8] = mat[8];
		out[9] = mat[9];
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
function Identity(mat) {
	for (var i = 0; i != mat.length; ++i) {
		mat[i] = 0;
	}mat[0] = 1;
	mat[5] = 1;
	mat[10] = 1;
	mat[15] = 1;
	return mat;
}

/**
 * Get the X vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
function GetX(mat) {
	return v4.make(mat[0], mat[1], mat[2], mat[3]);
}

/**
 * Set the X vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
function SetX(mat, vec) {
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
function GetY(mat) {
	return v4.make(mat[4], mat[5], mat[6], mat[7]);
}

/**
 * Set the Y vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
function SetY(mat, vec) {
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
function GetZ(mat) {
	return v4.make(mat[8], mat[9], mat[10], mat[11]);
}

/**
 * Set the Z vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
function SetZ(mat, vec) {
	mat[8] = vec[0];
	mat[9] = vec[1];
	mat[10] = vec[2];
	mat[11] = vec[3];
}

/**
 * Get the W vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
function GetW(mat) {
	return v4.make(mat[12], mat[13], mat[14], mat[15]);
}

/**
 * Set the W vector from 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @returns {v4}
 */
function SetW(mat, vec) {
	mat[12] = vec[0];
	mat[13] = vec[1];
	mat[14] = vec[2];
	mat[15] = vec[3];
}

/**
 * Get the ith vector from 'mat'
 * @param {m4x4} mat The matrix to read the vector from
 * @param {Number} i The index of the vector to return
 * @returns {v4}
 */
function GetI(mat, i) {
	i *= 4;
	return v4.make(mat[i + 0], mat[i + 1], mat[i + 2], mat[i + 3]);
}

/**
 * Set the ith vector in 'mat'
 * @param {m4x4} mat the matrix to read the vector from
 * @param {Number} i The index of the vector to set
 * @param {v4} vec The value to set the ith vector to
 * @returns {v4}
 */
function SetI(mat, i, vec) {
	i *= 4;
	mat[i + 0] = vec[0];
	mat[i + 1] = vec[1];
	mat[i + 2] = vec[2];
	mat[i + 3] = vec[3];
}

/**
 * Multiply a 4-vector by a 4x4 matrix
 * @param {m4x4} m
 * @param {v4} v 
 * @param {v4} out (optional) Where to write the result
 * @returns {v4}
 */
function MulMV(m, v, out) {
	out = out || v4.create();
	var x = v[0],
	    y = v[1],
	    z = v[2],
	    w = v[3];
	out[0] = m[0] * x + m[4] * y + m[8] * z + m[12] * w;
	out[1] = m[1] * x + m[5] * y + m[9] * z + m[13] * w;
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
function MulMM(a, b, out) {
	out = out || create();

	var a00 = a[0],
	    a01 = a[1],
	    a02 = a[2],
	    a03 = a[3];
	var a10 = a[4],
	    a11 = a[5],
	    a12 = a[6],
	    a13 = a[7];
	var a20 = a[8],
	    a21 = a[9],
	    a22 = a[10],
	    a23 = a[11];
	var a30 = a[12],
	    a31 = a[13],
	    a32 = a[14],
	    a33 = a[15];

	// Cache only the current line of the second matrix
	var b0 = b[0],
	    b1 = b[1],
	    b2 = b[2],
	    b3 = b[3];
	out[0] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
	out[1] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
	out[2] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
	out[3] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

	b0 = b[4];b1 = b[5];b2 = b[6];b3 = b[7];
	out[4] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
	out[5] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
	out[6] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
	out[7] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

	b0 = b[8];b1 = b[9];b2 = b[10];b3 = b[11];
	out[8] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
	out[9] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
	out[10] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
	out[11] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

	b0 = b[12];b1 = b[13];b2 = b[14];b3 = b[15];
	out[12] = b0 * a00 + b1 * a10 + b2 * a20 + b3 * a30;
	out[13] = b0 * a01 + b1 * a11 + b2 * a21 + b3 * a31;
	out[14] = b0 * a02 + b1 * a12 + b2 * a22 + b3 * a32;
	out[15] = b0 * a03 + b1 * a13 + b2 * a23 + b3 * a33;

	return out;
}

/**
 * Multiply a bounding box by a transformation matrix
 * @param {m4x4} a The matrix
 * @param {BBox} b The bounding box
 * @param {BBox} out (optional) where to write the result
 * @returns {BBox} a x b
 */
function MulMB(m, rhs, out) {
	//assert("Transforming an invalid bounding box" && rhs.valid());
	out = out || BBox.create(); //	BBox bb(m.pos, v4Zero);
	var mT = Transpose3x3(m);
	for (var i = 0; i != 3; ++i) {
		out.centre[i] += v4.Dot(GetI(mT, i), rhs.centre);
		out.radius[i] += v4.Dot(v4.Abs(GetI(mT, i)), rhs.radius);
	}
	return out;
}

/**
 * Get the transpose of the 3x3 part of 'mat', storing the result in 'out' if provided.
 */
function Transpose3x3(mat, out) {
	if (mat === out) {
		var m01 = out[1],
		    m02 = out[2];
		var m12 = out[6];
		out[1] = out[4];
		out[2] = out[8];
		out[6] = out[9];
		out[4] = m01;
		out[8] = m02;
		out[9] = m12;
	} else {
		out = out || create();
		out[0] = mat[0];
		out[1] = mat[4];
		out[2] = mat[8];
		out[3] = mat[3];
		out[4] = mat[1];
		out[5] = mat[5];
		out[6] = mat[9];
		out[7] = mat[7];
		out[8] = mat[2];
		out[9] = mat[6];
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
function Transpose(mat, out) {
	if (mat === out) {
		var m01 = a[1],
		    m02 = a[2],
		    m03 = a[3];
		var m12 = a[6],
		    m13 = a[7];
		var m23 = a[11];
		out[1] = out[4];
		out[2] = out[8];
		out[3] = out[12];
		out[6] = out[9];
		out[7] = out[13];
		out[11] = out[14];
		out[4] = m01;
		out[8] = m02;
		out[12] = m03;
		out[9] = m12;
		out[13] = m13;
		out[14] = m23;
	} else {
		out = out || create();
		out[0] = mat[0];
		out[1] = mat[4];
		out[2] = mat[8];
		out[3] = mat[12];
		out[4] = mat[1];
		out[5] = mat[5];
		out[6] = mat[9];
		out[7] = mat[13];
		out[8] = mat[2];
		out[9] = mat[6];
		out[10] = mat[10];
		out[11] = mat[14];
		out[12] = mat[3];
		out[13] = mat[7];
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
function Determinant3(mat) {
	return Maths.Triple3(GetX(mat), GetY(mat), GetZ(mat));
}

/**
 * Get the determinant of the matrix 'mat'
 * @param {m4x4} mat the matrix to get the determinant of
 * @returns {Number}
 */
function Determinant(mat) {
	var a00 = mat[0],
	    a01 = mat[1],
	    a02 = mat[2],
	    a03 = mat[3];
	var a10 = mat[4],
	    a11 = mat[5],
	    a12 = mat[6],
	    a13 = mat[7];
	var a20 = mat[8],
	    a21 = mat[9],
	    a22 = mat[10],
	    a23 = mat[11];
	var a30 = mat[12],
	    a31 = mat[13],
	    a32 = mat[14],
	    a33 = mat[15];

	var b00 = a00 * a11 - a01 * a10;
	var b01 = a00 * a12 - a02 * a10;
	var b02 = a00 * a13 - a03 * a10;
	var b03 = a01 * a12 - a02 * a11;
	var b04 = a01 * a13 - a03 * a11;
	var b05 = a02 * a13 - a03 * a12;
	var b06 = a20 * a31 - a21 * a30;
	var b07 = a20 * a32 - a22 * a30;
	var b08 = a20 * a33 - a23 * a30;
	var b09 = a21 * a32 - a22 * a31;
	var b10 = a21 * a33 - a23 * a31;
	var b11 = a22 * a33 - a23 * a32;

	return b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;
}

/**
 * Returns true if 'mat' is an orthonormal matrix
 * @param {m4x4} mat the matrix to test for orthonormality
 * @returns {boolean} true if orthonormal, false if not
 */
function IsOrthonormal(mat) {
	var x_lensq = Maths.LengthSq(GetX(mat));
	var y_lensq = Maths.LengthSq(GetY(mat));
	var z_lensq = Maths.LengthSq(GetZ(mat));
	var det3 = Math.abs(Determinant3(mat));
	return Maths.FEql(x_lensq, 1) && Maths.FEql(y_lensq, 1) && Maths.FEql(z_lensq, 1) && Maths.FEql(det3, 1);
}

/**
 * Orthonormalises the rotation component of the matrix
 * @param {m4x4} mat
 * @param {m4x4} out (optional) where to write the result
 * @returns {m4x4}
 */
function Orthonorm(mat, out) {
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
function InvertFast(mat, out) {
	//if (!IsOrthonormal(mat))
	//	throw new Error("Matrix is not orthonormal");

	var m = Transpose3x3(mat, out);
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
function Invert(mat, out) {
	out = out || create();

	var a00 = mat[0],
	    a01 = mat[1],
	    a02 = mat[2],
	    a03 = mat[3];
	var a10 = mat[4],
	    a11 = mat[5],
	    a12 = mat[6],
	    a13 = mat[7];
	var a20 = mat[8],
	    a21 = mat[9],
	    a22 = mat[10],
	    a23 = mat[11];
	var a30 = mat[12],
	    a31 = mat[13],
	    a32 = mat[14],
	    a33 = mat[15];

	var b00 = a00 * a11 - a01 * a10;
	var b01 = a00 * a12 - a02 * a10;
	var b02 = a00 * a13 - a03 * a10;
	var b03 = a01 * a12 - a02 * a11;
	var b04 = a01 * a13 - a03 * a11;
	var b05 = a02 * a13 - a03 * a12;
	var b06 = a20 * a31 - a21 * a30;
	var b07 = a20 * a32 - a22 * a30;
	var b08 = a20 * a33 - a23 * a30;
	var b09 = a21 * a32 - a22 * a31;
	var b10 = a21 * a33 - a23 * a31;
	var b11 = a22 * a33 - a23 * a32;

	// Calculate the determinant
	var det = b00 * b11 - b01 * b10 + b02 * b09 + b03 * b08 - b04 * b07 + b05 * b06;
	if (!det) throw new Error("Matrix is singular");

	det = 1.0 / det;
	out[0] = (a11 * b11 - a12 * b10 + a13 * b09) * det;
	out[1] = (a02 * b10 - a01 * b11 - a03 * b09) * det;
	out[2] = (a31 * b05 - a32 * b04 + a33 * b03) * det;
	out[3] = (a22 * b04 - a21 * b05 - a23 * b03) * det;
	out[4] = (a12 * b08 - a10 * b11 - a13 * b07) * det;
	out[5] = (a00 * b11 - a02 * b08 + a03 * b07) * det;
	out[6] = (a32 * b02 - a30 * b05 - a33 * b01) * det;
	out[7] = (a20 * b05 - a22 * b02 + a23 * b01) * det;
	out[8] = (a10 * b10 - a11 * b08 + a13 * b06) * det;
	out[9] = (a01 * b08 - a00 * b10 - a03 * b06) * det;
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
function Translation(pos) {
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
function Euler(pitch, yaw, roll, pos) {
	var cos_p = Math.cos(pitch),
	    sin_p = Math.sin(pitch);
	var cos_y = Math.cos(yaw),
	    sin_y = Math.sin(yaw);
	var cos_r = Math.cos(roll),
	    sin_r = Math.sin(roll);

	var out = create();
	out[0] = +cos_y * cos_r + sin_y * sin_p * sin_r;out[1] = cos_p * sin_r;out[2] = -sin_y * cos_r + cos_y * sin_p * sin_r;out[3] = 0;
	out[4] = -cos_y * sin_r + sin_y * sin_p * cos_r;out[5] = cos_p * cos_r;out[6] = +sin_y * sin_r + cos_y * sin_p * cos_r;out[7] = 0;
	out[8] = sin_y * cos_p;out[9] = -sin_p;out[10] = cos_y * cos_p;out[11] = 0;
	out[12] = pos[0];out[13] = pos[1];out[14] = pos[2];out[15] = 1;
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
function Scale(sx, sy, sz, pos) {
	var out = create();
	out[0] = sx;
	out[5] = sy;
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
function Translate(mat, ofs, out) {
	var x = ofs[0],
	    y = ofs[1],
	    z = ofs[2];
	if (mat === out) {
		out[12] = mat[0] * x + mat[4] * y + mat[8] * z + mat[12];
		out[13] = mat[1] * x + mat[5] * y + mat[9] * z + mat[13];
		out[14] = mat[2] * x + mat[6] * y + mat[10] * z + mat[14];
		out[15] = mat[3] * x + mat[7] * y + mat[11] * z + mat[15];
	} else {
		out = out || create();

		var a00 = mat[0],
		    a01 = mat[1],
		    a02 = mat[2],
		    a03 = mat[3];
		var a10 = mat[4],
		    a11 = mat[5],
		    a12 = mat[6],
		    a13 = mat[7];
		var a20 = mat[8],
		    a21 = mat[9],
		    a22 = mat[10],
		    a23 = mat[11];

		out[0] = a00;out[1] = a01;out[2] = a02;out[3] = a03;
		out[4] = a10;out[5] = a11;out[6] = a12;out[7] = a13;
		out[8] = a20;out[9] = a21;out[10] = a22;out[11] = a23;

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
function Rotate(mat, angle, axis, out) {
	var len = Maths.Length(axis);
	if (Math.abs(len) < Maths.Tiny) throw new Error("Axis of rotation is a zero vector");

	var a00 = mat[0],
	    a01 = mat[1],
	    a02 = mat[2],
	    a03 = mat[3];
	var a10 = mat[4],
	    a11 = mat[5],
	    a12 = mat[6],
	    a13 = mat[7];
	var a20 = mat[8],
	    a21 = mat[9],
	    a22 = mat[10],
	    a23 = mat[11];

	var s = Math.sin(angle);
	var c = Math.cos(angle);
	var t = 1 - c;

	var x = axis[0],
	    y = axis[1],
	    z = axis[2];
	len = 1 / len;
	x *= len;
	y *= len;
	z *= len;

	// Construct the elements of the rotation matrix
	var b00 = x * x * t + c,
	    b01 = y * x * t + z * s,
	    b02 = z * x * t - y * s;
	var b10 = x * y * t - z * s,
	    b11 = y * y * t + c,
	    b12 = z * y * t + x * s;
	var b20 = x * z * t + y * s,
	    b21 = y * z * t - x * s,
	    b22 = z * z * t + c;

	out = out || create();

	// Perform rotation-specific matrix multiplication
	out[0] = a00 * b00 + a10 * b01 + a20 * b02;
	out[1] = a01 * b00 + a11 * b01 + a21 * b02;
	out[2] = a02 * b00 + a12 * b01 + a22 * b02;
	out[3] = a03 * b00 + a13 * b01 + a23 * b02;
	out[4] = a00 * b10 + a10 * b11 + a20 * b12;
	out[5] = a01 * b10 + a11 * b11 + a21 * b12;
	out[6] = a02 * b10 + a12 * b11 + a22 * b12;
	out[7] = a03 * b10 + a13 * b11 + a23 * b12;
	out[8] = a00 * b20 + a10 * b21 + a20 * b22;
	out[9] = a01 * b20 + a11 * b21 + a21 * b22;
	out[10] = a02 * b20 + a12 * b21 + a22 * b22;
	out[11] = a03 * b20 + a13 * b21 + a23 * b22;

	// If the source and destination differ, copy the unchanged last row
	if (mat !== out) {
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
function ProjectionOrthographic(w, h, zn, zf, righthanded) {
	//assert("invalid view rect" && IsFinite(w) && IsFinite(h) && w > 0 && h > 0);
	//assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && (zn - zf) != 0);
	var rh = righthanded ? +1 : -1;
	var mat = create();
	mat[0] = 2.0 / w;
	mat[5] = 2.0 / h;
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
function ProjectionPerspective(w, h, zn, zf, righthanded) {
	//assert("invalid view rect" && IsFinite(w) && IsFinite(h) && w > 0 && h > 0);
	//assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
	var rh = righthanded ? +1 : -1;
	var mat = create();
	mat[0] = 2.0 * zn / w;
	mat[5] = 2.0 * zn / h;
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
function ProjectionPerspectiveLRTB(l, r, t, b, zn, zf, righthanded) {
	//assert("invalid view rect" && IsFinite(l)  && IsFinite(r) && IsFinite(t) && IsFinite(b) && (r - l) > 0 && (t - b) > 0);
	//assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
	var rh = righthanded ? +1 : -1;
	var mat = create();
	mat[0] = 2.0 * zn / (r - l);
	mat[5] = 2.0 * zn / (t - b);
	mat[8] = rh * (r + l) / (r - l);
	mat[9] = rh * (t + b) / (t - b);
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
function ProjectionPerspectiveFOV(fovY, aspect, zn, zf, righthanded) {
	//assert("invalid aspect ratio" && IsFinite(aspect) && aspect > 0);
	//assert("invalid near/far planes" && IsFinite(zn) && IsFinite(zf) && zn > 0 && zf > 0 && (zn - zf) != 0);
	var rh = righthanded ? +1 : -1;
	var mat = create();
	mat[5] = 1.0 / Math.tan(fovY / 2);
	mat[0] = mat[5] / aspect;
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
function LookAt(pos, focus_point, up, out) {
	// Z vector
	var z = v4.Sub(pos, focus_point);
	var zlen = v4.Length(z);
	if (zlen < Maths.Tiny) throw new Error("position and focus point are coincident");
	v4.MulS(z, 1 / zlen, z);

	// X vector
	var x = v4.Cross(up, z);
	var xlen = v4.Length(x);
	if (xlen < Maths.Tiny) throw new Error("Focus point is aligned with the up direction");
	v4.MulS(x, 1 / xlen, x);

	// Y vector
	var y = v4.Cross(z, x);

	out = out || create();
	set(out, x, y, z, pos);
	return out;
}

/***/ }),
/* 3 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.EFlags = exports.ELight = exports.EGeom = exports.Texture = exports.Camera = exports.Instance = exports.Model = exports.Light = undefined;
exports.Create = Create;
exports.Render = Render;
exports.CreateDemoScene = CreateDemoScene;
exports.CreateTestModel = CreateTestModel;

var _v = __webpack_require__(1);

var v4 = _interopRequireWildcard(_v);

var _m4x = __webpack_require__(2);

var m4x4 = _interopRequireWildcard(_m4x);

var _maths = __webpack_require__(0);

var Maths = _interopRequireWildcard(_maths);

var _model = __webpack_require__(11);

var Model = _interopRequireWildcard(_model);

var _instance = __webpack_require__(12);

var Instance = _interopRequireWildcard(_instance);

var _texture = __webpack_require__(13);

var Texture = _interopRequireWildcard(_texture);

var _light = __webpack_require__(14);

var Light = _interopRequireWildcard(_light);

var _forward = __webpack_require__(15);

var FwdVS = _interopRequireWildcard(_forward);

var _forward2 = __webpack_require__(16);

var FwdPS = _interopRequireWildcard(_forward2);

var _camera = __webpack_require__(17);

var Camera = _interopRequireWildcard(_camera);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

exports.Light = Light;
exports.Model = Model;
exports.Instance = Instance;
exports.Camera = Camera;
exports.Texture = Texture;

/**
 * Geometry type enumeration
 */

var EGeom = exports.EGeom = Object.freeze({
	"None": 0,
	"Vert": 1 << 0,
	"Colr": 1 << 1,
	"Norm": 1 << 2,
	"Tex0": 1 << 3
});

/**
 * Light source type enumeration
 */
var ELight = exports.ELight = Object.freeze({
	"Ambient": 0,
	"Directional": 1,
	"Radial": 2,
	"Spot": 3
});

/**
 * Rendering control flags
 */
var EFlags = exports.EFlags = Object.freeze({
	"None": 0,

	// The object is hidden
	"Hidden": 1 << 0,

	// The object is filled in wireframe mode
	"Wireframe": 1 << 1,

	// Render the object without testing against the depth buffer
	"NoZTest": 1 << 2,

	// Render the object without effecting the depth buffer
	"NoZWrite": 1 << 3,

	// Set when an object is selected. The meaning of 'selected' is up to the application
	"Selected": 1 << 8,

	// Doesn't contribute to the bounding box on an object.
	"BBoxExclude": 1 << 9,

	// Should not be included when determining the bounds of a scene.
	"SceneBoundsExclude": 1 << 10,

	// Ignored for hit test ray casts
	"HitTestExclude": 1 << 11
});

/**
 * Create a renderer instance for a canvas element.
 * @param {HTMLCanvasElement} canvas the html canvas element to render on.
 * @returns the WebGL context object.
 */
function Create(canvas) {
	var rdr = canvas.getContext("experimental-webgl", { antialias: true, depth: true });
	if (!rdr) throw new Error("webgl not available");

	// Set the initial viewport dimensions
	rdr.viewport(0, 0, canvas.width, canvas.height);

	// Set the default back colour (r,g,b,a)
	rdr.back_colour = v4.make(0.5, 0.5, 0.5, 1.0);

	// Create stock textures.
	Texture.CreateStockTextures(rdr);

	// Initialise the standard forward rendering shader
	// Component shaders are stored in 'shaders'
	// Compiled shader programs are stored in 'shader_programs
	// User code can do similar to add new shader programs constructed
	// from existing or new shader components.
	rdr.shaders = [];
	rdr.shaders['forward_vs'] = FwdVS.CompileShader(rdr);
	rdr.shaders['forward_ps'] = FwdPS.CompileShader(rdr);

	rdr.shader_programs = [];
	rdr.shader_programs['forward'] = function () {
		// Compile into a shader 'program'
		var shader = rdr.createProgram();
		rdr.attachShader(shader, rdr.shaders['forward_vs']);
		rdr.attachShader(shader, rdr.shaders['forward_ps']);
		rdr.linkProgram(shader);
		if (!rdr.getProgramParameter(shader, rdr.LINK_STATUS)) throw new Error('Could not compile WebGL program. \n\n' + rdr.getProgramInfoLog(shader));

		// Read variables from the shaders and save them in 'shader'
		shader.position = rdr.getAttribLocation(shader, "position");
		shader.normal = rdr.getAttribLocation(shader, "normal");
		shader.colour = rdr.getAttribLocation(shader, "colour");
		shader.texcoord = rdr.getAttribLocation(shader, "texcoord");

		shader.o2w = rdr.getUniformLocation(shader, "o2w");
		shader.w2c = rdr.getUniformLocation(shader, "w2c");
		shader.c2s = rdr.getUniformLocation(shader, "c2s");

		shader.tint_colour = rdr.getUniformLocation(shader, "tint_colour");

		shader.camera = {};
		shader.camera.ws_position = rdr.getUniformLocation(shader, "cam_ws_position");
		shader.camera.ws_forward = rdr.getUniformLocation(shader, "cam_ws_forward");

		shader.tex_diffuse = {};
		shader.tex_diffuse.enabled = rdr.getUniformLocation(shader, "has_tex_diffuse");
		shader.tex_diffuse.sampler = rdr.getUniformLocation(shader, "sampler_diffuse");

		shader.light = {};
		shader.light.lighting_type = rdr.getUniformLocation(shader, "lighting_type");
		shader.light.position = rdr.getUniformLocation(shader, "light_position");
		shader.light.direction = rdr.getUniformLocation(shader, "light_direction");
		shader.light.ambient = rdr.getUniformLocation(shader, "light_ambient");
		shader.light.diffuse = rdr.getUniformLocation(shader, "light_diffuse");
		shader.light.specular = rdr.getUniformLocation(shader, "light_specular");
		shader.light.specpwr = rdr.getUniformLocation(shader, "light_specpwr");

		// Return the shader
		return shader;
	}();

	// Return the WebGL context
	return rdr;
}

/**
 * Render a scene
 * @param {WebGL} rdr the WebGL instance created by 'Initialise'
 * @param {[Instance]} instances the instances that make up the scene
 * @param {Camera} camera the view into the scene
 * @param {Light} global_light the global light source
 */
function Render(rdr, instances, camera, global_light) {
	// Build a nugget draw list
	var drawlist = BuildDrawList(instances);

	// Clear the back/depth buffer
	rdr.clearColor(rdr.back_colour[0], rdr.back_colour[1], rdr.back_colour[2], rdr.back_colour[3]);
	rdr.clear(rdr.COLOR_BUFFER_BIT | rdr.DEPTH_BUFFER_BIT);

	// Render each nugget
	for (var i = 0; i != drawlist.length; ++i) {
		var dle = drawlist[i];
		var nug = dle.nug;
		var inst = dle.inst;
		var model = inst.model;
		var shader = nug.shader;

		// Bind the shader to the gfx card
		rdr.useProgram(shader);

		{
			// Set render states
			// Z Test
			if ((inst.flags & EFlags.NoZTest) == 0) rdr.enable(rdr.DEPTH_TEST);else rdr.disable(rdr.DEPTH_TEST);

			// Z Write
			if ((inst.flags & EFlags.NoZWrite) == 0) rdr.depthMask(true);else rdr.depthMask(false);
		}

		{
			// Bind shader input streams
			// Bind vertex buffer
			if (model.geom & EGeom.Vert) {
				rdr.bindBuffer(rdr.ARRAY_BUFFER, model.vbuffer);
				rdr.vertexAttribPointer(shader.position, model.vbuffer.stride, rdr.FLOAT, false, 0, 0);
				rdr.enableVertexAttribArray(shader.position);
			} else if (shader.position != -1) {
				rdr.vertexAttrib3fv(shader.colour, [0, 0, 0]);
				rdr.disableVertexAttribArray(shader.position);
			}

			// Bind normals buffer
			if (model.geom & EGeom.Norm) {
				rdr.bindBuffer(rdr.ARRAY_BUFFER, model.nbuffer);
				rdr.vertexAttribPointer(shader.normal, model.nbuffer.stride, rdr.FLOAT, false, 0, 0);
				rdr.enableVertexAttribArray(shader.normal);
			} else if (shader.normal != -1) {
				rdr.vertexAttrib3fv(shader.normal, [0, 0, 0]);
				rdr.disableVertexAttribArray(shader.normal);
			}

			// Bind colours buffer
			if (model.geom & EGeom.Colr) {
				rdr.bindBuffer(rdr.ARRAY_BUFFER, model.cbuffer);
				rdr.vertexAttribPointer(shader.colour, model.cbuffer.stride, rdr.FLOAT, false, 0, 0);
				rdr.enableVertexAttribArray(shader.colour);
			} else if (shader.colour != -1) {
				rdr.vertexAttrib3fv(shader.colour, [1, 1, 1]);
				rdr.disableVertexAttribArray(shader.colour);
			}

			// Bind texture coords buffer
			if (model.geom & EGeom.Tex0) {
				rdr.bindBuffer(rdr.ARRAY_BUFFER, model.tbuffer);
				rdr.vertexAttribPointer(shader.texcoord, model.tbuffer.stride, rdr.FLOAT, false, 0, 0);
				rdr.enableVertexAttribArray(shader.texcoord);
			} else if (shader.texcoord != -1) {
				rdr.vertexAttrib2fv(shader.texcoord, [0, 0]);
				rdr.disableVertexAttribArray(shader.texcoord);
			}
		}

		{
			// Bind the diffuse texture
			// Get the texture to bind, or the default texture if not ready yet
			var texture = nug.tex_diffuse && nug.tex_diffuse.image_loaded ? nug.tex_diffuse : rdr.stock_textures.white;

			// Set the enable texture flag
			if (shader.tex_diffuse.enabled != -1) rdr.uniform1i(shader.tex_diffuse.enabled, nug.tex_diffuse ? +1 : 0);

			// Set the texture unit
			if (shader.tex_diffuse.sampler != -1) rdr.uniform1i(shader.tex_diffuse.sampler, 0);

			// Bind the texture
			rdr.bindTexture(rdr.TEXTURE_2D, texture);
			rdr.activeTexture(rdr.TEXTURE0);
		}

		{
			// Set lighting
			// Use the global light, or one from the nugget
			var light = nug.hasOwnProperty("light") ? nug.light : global_light;
			if (model.geom & EGeom.Norm) {
				rdr.uniform1i(shader.light.lighting_type, light.lighting_type);
				rdr.uniform3fv(shader.light.position, light.position.subarray(0, 3));
				rdr.uniform3fv(shader.light.direction, light.direction.subarray(0, 3));
				rdr.uniform3fv(shader.light.ambient, light.ambient.slice(0, 3));
				rdr.uniform3fv(shader.light.diffuse, light.diffuse.slice(0, 3));
				rdr.uniform3fv(shader.light.specular, light.specular.slice(0, 3));
				rdr.uniform1f(shader.light.specpwr, light.specpwr);
			} else if (shader.light.lighting_type != -1) {
				rdr.uniform1i(shader.light.lighting_type, 0);
			}
		}

		{
			// Set tint
			var tint = inst.hasOwnProperty("tint") ? inst.tint : nug.hasOwnProperty("tint") ? nug.tint : [1, 1, 1, 1];
			rdr.uniform4fv(shader.tint_colour, new Float32Array(tint));
		}

		{
			// Set transforms
			// Set the projection matrix
			var c2s = nug.hasOwnProperty("c2s") ? nug.c2s : camera.c2s;
			rdr.uniformMatrix4fv(shader.c2s, false, c2s);

			// Set the camera matrix
			rdr.uniformMatrix4fv(shader.w2c, false, camera.w2c);
			rdr.uniform3fv(shader.camera.ws_position, camera.pos.subarray(0, 3));
			rdr.uniform3fv(shader.camera.ws_forward, camera.fwd.subarray(0, 3));

			// Set the o2w transform
			rdr.uniformMatrix4fv(shader.o2w, false, inst.o2w);
		}

		{
			// Render the nugget
			if (model.ibuffer != null) {
				// Get the buffer range to render
				var irange = nug.hasOwnProperty("irange") ? nug.irange : { ofs: 0, count: model.ibuffer.count };
				rdr.bindBuffer(rdr.ELEMENT_ARRAY_BUFFER, model.ibuffer);
				rdr.drawElements(nug.topo, irange.count, rdr.UNSIGNED_SHORT, irange.ofs * 2); // offset in bytes
			} else {
				// Get the buffer range to render
				var vrange = nug.hasOwnProperty("vrange") ? nug.vrange : { ofs: 0, count: model.vbuffer.count };
				rdr.drawArrays(nug.topo, vrange.ofs, vrange.count);
			}
		}

		// Clean up
		rdr.bindTexture(rdr.TEXTURE_2D, null);
		rdr.bindBuffer(rdr.ELEMENT_ARRAY_BUFFER, null);
		rdr.bindBuffer(rdr.ARRAY_BUFFER, null);
	}
}

/**
 * Returns an ordered list of nuggets to render
 * @param {[Instance]} instances the instances to build the drawlist from
 * @returns {[Nugget]} an ordered list of nuggets to render
 */
function BuildDrawList(instances) {
	var drawlist = [];
	for (var i = 0; i != instances.length; ++i) {
		var inst = instances[i];

		if (!inst.model) continue;

		for (var j = 0; j != inst.model.gbuffer.length; ++j) {
			var nug = inst.model.gbuffer[j];
			drawlist.push({ nug: nug, inst: inst });
		}
	}
	return drawlist;
}

/**
 * Create a demo scene to show everything is working
 */
function CreateDemoScene(rdr) {
	var instances = [CreateTestModel(rdr)];

	var camera = Camera.Create(Maths.TauBy8, rdr.AspectRatio);
	m4x4.LookAt(v4.make(4, 4, 10, 1), v4.Origin, v4.YAxis, camera.c2w);

	var light = new Light.Create(Rdr.ELight.Directional, v4.create(), v4.make(-0.5, -0.5, -1.0, 0), [0.1, 0.1, 0.1], [0.5, 0.5, 0.5], [0.5, 0.5, 0.5], 100);

	Render(rdr, instances, camera, light);
}

/**
 * Create a test model instance
 * @returns {Instance}
 */
function CreateTestModel(rdr) {
	// Use the standard forward render shader
	var shader = rdr.shader_programs.forward;

	// Pyramid
	var model = Rdr.Model.Create(rdr, [// verts
	{ pos: [+0.0, +1.0, +0.0], norm: [+0.00, +1.00, +0.00], col: [1, 0, 0, 1], tex0: [0.50, 1] }, { pos: [-1.0, -1.0, -1.0], norm: [-0.57, -0.57, -0.57], col: [0, 1, 0, 1], tex0: [0.00, 0] }, { pos: [+1.0, -1.0, -1.0], norm: [+0.57, -0.57, -0.57], col: [0, 0, 1, 1], tex0: [0.25, 0] }, { pos: [+1.0, -1.0, +1.0], norm: [+0.57, -0.57, +0.57], col: [1, 1, 0, 1], tex0: [0.50, 0] }, { pos: [-1.0, -1.0, +1.0], norm: [-0.57, -0.57, +0.57], col: [0, 1, 1, 1], tex0: [0.75, 0] }], [// indices
	0, 1, 2, 0, 2, 3, 0, 3, 4, 0, 4, 1, 4, 3, 2, 2, 1, 4], [// nuggets
	{ topo: rdr.TRIANGLES, shader: shader //, tex_diffuse: rdr.stock_textures.checker1 }
	}]);

	var o2w = m4x4.Translation([0, 0, 0, 1]);
	var inst = Rdr.Instance.Create("pyramid", model, o2w);
	return inst;
}

/***/ }),
/* 4 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});

var _createClass = function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; }(); /**
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      * @module BBox
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      */

exports.create = create;
exports.clone = clone;
exports.GetCorner = GetCorner;
exports.IsPointWithin = IsPointWithin;
exports.IsBBoxWithin = IsBBoxWithin;
exports.EncompassPoint = EncompassPoint;
exports.EncompassBBox = EncompassBBox;
exports.IsIntersection = IsIntersection;
exports.ToRectXY = ToRectXY;

var _maths = __webpack_require__(0);

var Maths = _interopRequireWildcard(_maths);

var _v = __webpack_require__(1);

var v4 = _interopRequireWildcard(_v);

var _m4x = __webpack_require__(2);

var m4x4 = _interopRequireWildcard(_m4x);

var _rect = __webpack_require__(5);

var Rect = _interopRequireWildcard(_rect);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

/**
 * Constants
 */
var Invalid = create();

/**
 * Create a new, invalid, bounding box
 * @returns {centre,radius}
 */
function create() {
	return new (function () {
		function _class() {
			_classCallCheck(this, _class);

			this.centre = v4.make(0, 0, 0, 1);
			this.radius = v4.make(-1, -1, -1, 0);
		}

		/**
   * Return true if this bounding box represents a point or volume
   * @returns {boolean}
   */


		_createClass(_class, [{
			key: "is_valid",
			get: function get() {
				return this.radius[0] >= 0 && this.radius[1] >= 0 && this.radius[2] >= 0;
			}

			/**
    * Returns true if this bounding box is a degenerate point
    * @returns {boolean}
    */

		}, {
			key: "is_point",
			get: function get() {
				return v4.Eql(this.radius, v4.Zero);
			}

			/**
    * Return the lower bound
    * @returns {v4}
    */

		}, {
			key: "lower",
			get: function get() {
				return v4.Sub(this.centre, this.radius);
			}

			/**
    * Return the upper bound
    * @returns {v4}
    */

		}, {
			key: "upper",
			get: function get() {
				return v4.Add(this.centre, this.radius);
			}

			/**
    * Return the size of this bounding box [x,y,z,0]
    * @returns {v4}
    */

		}, {
			key: "size",
			get: function get() {
				return v4.MulS(this.radius, 2);
			}

			/**
    * Gets the squared length of the diagonal of the bounding box
    * * @returns {Number}
    */

		}, {
			key: "diametre_sq",
			get: function get() {
				return 4 * v4.LengthSq(this.radius);
			}

			/**
    * Gets the length of the diagonal of the bounding box
    * @returns {Number}
    */

		}, {
			key: "diametre",
			get: function get() {
				return Math.sqrt(this.diametre_sq);
			}

			/**
    * Gets the volume of the bounding box
    * @returns {Number}
    */

		}, {
			key: "volume",
			get: function get() {
				var sz = this.size;
				return sz[0] * sz[1] * sz[2];
			}
		}]);

		return _class;
	}())();
}

/**
 * Create a new copy of 'bbox'
 * @param {BBox} bbox the source bounding box to copy
 * @param {BBox} out (optional) the bounding box to write to
 * @returns {BBox} The clone of 'bbox'
 */
function clone(bbox, out) {
	out = out || create();
	if (bbox !== out) {
		v4.clone(bbox.centre, out.centre);
		v4.clone(bbox.radius, out.radius);
	}
	return out;
}

/**
 * Returns a corner point of the bounding box.
 * @param {BBox} bbox The bounding box to return the corner from
 * @param {Number} corner A 3-bit mask of the corner to return: 000 = -x,-y,-z, 111 = +x,+y,+z
 * @returns {v4}
 */
function GetCorner(bbox, corner) {
	if (corner >= 8) throw new Error("Invalid corner index");
	var x = (corner >> 0 & 0x1) * 2 - 1;
	var y = (corner >> 1 & 0x1) * 2 - 1;
	var z = (corner >> 2 & 0x1) * 2 - 1;
	var c = v4.make(bbox.centre[0] + x * bbox.radius[0], bbox.centre[1] + y * bbox.radius[1], bbox.centre[2] + z * bbox.radius[2], 1);
	return c;
}

/**
 * Returns true if 'point' is within this bounding box (within tolerance).
 * @param {BBox} bbox The bounding box to check for surrounding 'point'
 * @param {point} point The point to test for being within 'bbox'
 * @param {Number} tol (optional) Tolerance
 * @returns {boolean}
 */
function IsPointWithin(bbox, point, tol) {
	tol = tol || 0;
	var within = Math.abs(point[0] - bbox.centre[0]) <= bbox.radius[0] + tol && Math.abs(point[1] - bbox.centre[1]) <= bbox.radius[1] + tol && Math.abs(point[2] - bbox.centre[2]) <= bbox.radius[2] + tol;
	return within;
}

/**
 * Returns true if 'bbox' is within this bounding box (within 'tol'erance)
 * @param {BBox} bbox The bounding box to check for surrounding 'rhs'
 * @param {BBox} rhs The point to test for being within 'bbox'
 * @param {Number} tol (optional) Tolerance
 * @returns {boolean}
 */
function IsBBoxWithin(bbox, rhs, tol) {
	tol = tol || 0;
	var within = Math.abs(rhs.centre[0] - bbox.centre[0]) <= bbox.radius[0] - rhs.radius[0] + tol && Math.abs(rhs.centre[1] - bbox.centre[1]) <= bbox.radius[1] - rhs.radius[1] + tol && Math.abs(rhs.centre[2] - bbox.centre[2]) <= bbox.radius[2] - rhs.radius[2] + tol;
	return within;
}

/**
 * Expands the bounding box to include 'point'
 * @param {BBox} bbox The bounding box to grow
 * @param {v4} point The point to encompass in 'bbox'
 * @param {BBox} out (optional). The bounding box to write the result to
 * @returns {BBox}
 */
function EncompassPoint(bbox, point, out) {
	clone(bbox, out);
	for (var i = 0; i != 3; ++i) {
		if (out.radius[i] < 0) {
			out.centre[i] = point[i];
			out.radius[i] = 0;
		} else {
			var signed_dist = point[i] - out.centre[i];
			var length = Math.abs(signed_dist);
			if (length > out.radius[i]) {
				var new_radius = (length + out.radius[i]) / 2;
				out.centre[i] += signed_dist * (new_radius - out.radius[i]) / length;
				out.radius[i] = new_radius;
			}
		}
	}
	return out;
}

/**
 * Expands the bounding box to include 'rhs'.
 * @param {BBox} bbox The bounding box to grow
 * @param {BBox} rhs The bounding box to encompass in 'bbox'
 * @param {BBox} out (optional). The bounding box to write the result to
 * @returns {BBox}
 */
function EncompassBBox(bbox, rhs, out) {
	if (!rhs.is_valid) throw new Error("Encompassing an invalid bounding box");
	clone(bbox, out);
	EncompassPoint(out, v4.Add(rhs.centre, rhs.radius), out);
	EncompassPoint(out, v4.Sub(rhs.centre, rhs.radius), out);
	return out;
}

/**
 * Returns true if 'rhs' intersects with this bounding box
 * @returns {boolean}
 * @param {BBox} lhs The bounding box to intersect
 * @param {BBox} rhs The other bounding box to intersect
 * @returns {boolean}
 */
function IsIntersection(lhs, rhs) {
	var intersection = Math.abs(lhs.centre[0] - rhs.centre[0]) <= lhs.radius[0] + rhs.radius[0] && Math.abs(lhs.centre[1] - rhs.centre[1]) <= lhs.radius[1] + rhs.radius[1] && Math.abs(lhs.centre[2] - rhs.centre[2]) <= lhs.radius[2] + rhs.radius[2];
	return intersection;
}

/**
 * Create a RectangleF from the X,Y axes of a bounding box
 * @param {BBox} bbox
 * @returns {Rect}
 */
function ToRectXY(bbox) {
	var mn = bbox.min;
	var sz = bbox.size;
	return Rect.make(mn[0], mn[1], sz[0], sz[1]);
}

/***/ }),
/* 5 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
  value: true
});

var _createClass = function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; }(); /**
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      * @module Rect
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      */

exports.create = create;
exports.make = make;
exports.ltrb = ltrb;
exports.bound = bound;
exports.set = set;
exports.clone = clone;
exports.Contains = Contains;
exports.NormalisePoint = NormalisePoint;
exports.ScalePoint = ScalePoint;
exports.EncompassPoint = EncompassPoint;
exports.EncompassRect = EncompassRect;

var _maths = __webpack_require__(0);

var Maths = _interopRequireWildcard(_maths);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

/**
 * Constants
 */
var Invalid = make(0, 0, -1, -1);

/**
 * Create a zero rectangle
 * @returns {x,y,w,h}
 */
function create() {
  return new (function () {
    function _class() {
      _classCallCheck(this, _class);

      this.x = 0;
      this.y = 0;
      this.w = 0;
      this.h = 0;
    }

    _createClass(_class, [{
      key: "l",
      get: function get() {
        return this.x;
      },
      set: function set(v) {
        this.x = v;
      }
    }, {
      key: "t",
      get: function get() {
        return this.y;
      },
      set: function set(v) {
        this.y = v;
      }
    }, {
      key: "r",
      get: function get() {
        return this.x + this.w;
      },
      set: function set(v) {
        this.w = v - this.x;
      }
    }, {
      key: "b",
      get: function get() {
        return this.y + this.h;
      },
      set: function set(v) {
        this.h = v - this.y;
      }
    }, {
      key: "centre",
      get: function get() {
        return [this.x + this.w * 0.5, this.y + this.h * 0.5];
      },
      set: function set(v) {
        this.x = v[0] - this.w * 0.5;this.y = v[1] - this.h * 0.5;
      }
    }]);

    return _class;
  }())();
}

/**
 * Create a rectangle from a point and size
 * @param {Number} x
 * @param {Number} y
 * @param {Number} w
 * @param {Number} h
 * @returns {Rect}
 */
function make(x, y, w, h) {
  var out = create();
  return set(out, x, y, w, h);
}

/**
 * Create from left, top, right, bottom
 * @param {Nummber} l 
 * @param {Nummber} t 
 * @param {Nummber} r 
 * @param {Nummber} b 
 * @returns {Rect}
 */
function ltrb(l, t, r, b) {
  var out = create();
  return set(out, l, t, r - l, b - t);
}

/**
 * Return a bounding rect about the given points
 * @param {[v2]} points 
 */
function bound() {
  var br = clone(Invalid);

  for (var _len = arguments.length, points = Array(_len), _key = 0; _key < _len; _key++) {
    points[_key] = arguments[_key];
  }

  for (var i = 0; i != points.length; ++i) {
    EncompassPoint(br, points[i], br);
  }return br;
}

/**
 * Set the components of a rectangle
 * @param {Rect} rect 
 * @param {Number} x 
 * @param {Number} y 
 * @param {Number} w 
 * @param {Number} h 
 * @returns {Rect}
 */
function set(rect, x, y, w, h) {
  rect.x = x;
  rect.y = y;
  rect.w = w;
  rect.h = h;
  return rect;
}

/**
 * Create a new copy of 'rect'
 * @param {Rect} rect the source rect to copy
 * @param {Rect} out (optional) the rect to write to
 * @returns {Rect} the clone of 'rect'
 */
function clone(rect, out) {
  out = out || create();
  if (rect !== out) {
    out.x = rect.x;
    out.y = rect.y;
    out.w = rect.w;
    out.h = rect.h;
  }
  return out;
}

/**
 * Returns true of 'point' is within [[x,y], [x+w,y+h])
 * @param {Rect} rect 
 * @param {[x,y]} point
 * @returns {boolean}
 */
function Contains(rect, point) {
  var contained = point[0] >= rect.l && point[0] < rect.r && point[1] >= rect.t && point[1] < rect.b;
  return contained;
}

/**
 * Return 'point' scaled by the transform that maps 'rect' to the square (bottom left:-1,-1)->(top right:1,1) 
 * Inverse of 'ScalePoint'
 * @param {Rect} rect
 * @param {[x,y]} point
 * @param {Number} xsign Set to +1 if the rect origin is on the left, -1 if on the right
 * @param {Number} ysign Set to +1 if the rect origin is at the top, -1 if at the bottom
 * @returns {[x,y]}
 */
function NormalisePoint(rect, point, xsign, ysign) {
  return [(xsign || +1) * (2 * (point[0] - rect.x) / rect.w - 1), (ysign || +1) * (2 * (point[1] - rect.y) / rect.h - 1)];
}

/**
 * Scales a normalised 'point' by the transform that maps the square (bottom left:-1,-1)->(top right:1,1) to 'rect'
 * Inverse of 'NormalisedPoint'
 * @param {Rect} rect
 * @param {[x,y]} point
 * @param {Number} xsign Set to +1 if the rect origin is on the left, -1 if on the right
 * @param {Number} ysign Set to +1 if the rect origin is at the top, -1 if at the bottom
 * @returns {[x,y]}
 */
function ScalePoint(rect, point, xsign, ysign) {
  return [rect.x + rect.w * (1 + (xsign || +1) * point[0]) / 2, rect.y + rect.h * (1 + (ysign || +1) * point[1]) / 2];
}

/**
 * Expand 'rect' to include 'point'
 * @param {Rect} rect The initial rectangle. (Start with Rect.Invalid when finding a bounding rect)
 * @param {v2} point The point to encompass with 'rect'
 * @param {Rect} out (optional) where to write the expanded rect to
 * @returns {Rect}
 */
function EncompassPoint(rect, point, out) {
  out = out || create();
  out.x = rect.w >= 0 ? Math.min(rect.x, point[0]) : point[0];
  out.y = rect.h >= 0 ? Math.min(rect.y, point[1]) : point[1];
  out.w = rect.w >= 0 ? Math.max(rect.w, point[0] - rect.l, rect.r - point[0]) : 0;
  out.h = rect.h >= 0 ? Math.max(rect.h, point[1] - rect.t, rect.b - point[1]) : 0;
  return out;
}

/**
 * Expand 'rect' to include 'rhs'
 * @param {Rect} rect The initial rectangle. (Start with Rect.Invalid when finding a bounding rect)
 * @param {Rect} rhs The rectangle to encompass with 'rect'
 * @param {Rect} out (optional) where to write the expanded rect to
 * @returns {Rect}
 */
function EncompassRect(rect, rhs, out) {
  out = out || create();
  out.x = rect.w >= 0 ? Math.min(rect.x, rhs.x) : rhs.x;
  out.y = rect.h >= 0 ? Math.min(rect.y, rhs.y) : rhs.y;
  out.w = rect.w >= 0 ? Math.max(rect.w, rhs.r - rect.l, rect.r - rhs.l) : 0;
  out.h = rect.h >= 0 ? Math.max(rect.h, rhs.b - rect.t, rect.b - rhs.t) : 0;
  return out;
}

/***/ }),
/* 6 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
  value: true
});
exports.YAxis = exports.XAxis = undefined;
exports.create = create;
exports.make = make;
exports.set = set;
exports.clone = clone;
exports.Eql = Eql;
exports.FEql = FEql;
exports.IsNaN = IsNaN;
exports.Neg = Neg;
exports.Abs = Abs;
exports.Clamp = Clamp;
exports.LengthSq = LengthSq;
exports.Length = Length;
exports.Normalise = Normalise;
exports.Dot = Dot;
exports.Cross = Cross;
exports.Add = Add;
exports.AddN = AddN;
exports.Sub = Sub;
exports.MulS = MulS;
exports.MulV = MulV;

var _maths = __webpack_require__(0);

var Maths = _interopRequireWildcard(_maths);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

var FVec = Float32Array; /**
                          * @module v2
                          */

var XAxis = exports.XAxis = new FVec([1, 0]);
var YAxis = exports.YAxis = new FVec([0, 1]);

/**
 * Create a 2-vector containing zeros
 * @returns {v2}
 */
function create() {
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
function make(x, y) {
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
function set(vec, x, y) {
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
function clone(vec, out) {
  out = out || create();
  if (vec !== out) {
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
function Eql(a, b) {
  var eql = a[0] == b[0] && a[1] == b[1];
  return eql;
}

/**
 * Approximate equality of two 2-vectors
 * @param {v2} a
 * @param {v2} b
 * @returns {boolean}
 */
function FEql(a, b) {
  var feql = Maths.FEql(a[0], b[0]) && Maths.FEql(a[1], b[1]);
  return feql;
}

/**
 * Returns true if 'vec' contains any 'NaN's
 * @param {v2} vec
 * @returns {boolean}
 */
function IsNaN(vec) {
  var is_nan = isNaN(vec[0]) || isNaN(vec[1]);
  return is_nan;
}

/**
 * Return the negation of 'vec'
 * @param {v2} vec the vector to negate
 * @param {v2} out (optional) the vector to write to
 * @returns {v2} the negative of 'vec'
 */
function Neg(vec, out) {
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
function Abs(vec, out) {
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
function Clamp(vec, min, max, out) {
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
function LengthSq(vec) {
  return vec[0] * vec[0] + vec[1] * vec[1];
}

/**
 * Compute the length of a 2-vector
 * @param {v2} vec the vector to find the length of
 * @returns {Number} the length of the vector
 */
function Length(vec) {
  return Math.sqrt(LengthSq(vec));
}

/**
 * Normalise a 2-vector to length = 1
 * @param {v2} vec the vector to normalise
 * @param {v2} out (optional) the vector to write the result to
 * @param {Object} opts optional parameters: {def:[..]}
 * @returns {v2} the vector with components normalised
 */
function Normalise(vec, out, opts) {
  out = out || create();
  var len = Length(vec);
  if (len == 0) {
    if (opts && opts.def) out = opts.def;else throw new Error("Cannot normalise a zero vector");
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
function Dot(a, b) {
  return a[0] * b[0] + a[1] * b[1];
}

/**
 * Cross product: a x b
 * (Equivalent to dot(rotate90cw(a), b))
 * @param {v2} a
 * @param {v2} b
 * @param {v2} out (optional) where the result is written to
 * @returns {v2}
 */
function Cross(a, b, out) {
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
function Add(a, b, out) {
  out = out || create();
  set(out, a[0] + b[0], a[1] + b[1]);
  return out;
}

/**
 * Add an array of vectors
 * @param {[v2]} arr 
 * @returns {v2} The sum of the given vectors
 */
function AddN() {
  var sum = v2.create();

  for (var _len = arguments.length, arr = Array(_len), _key = 0; _key < _len; _key++) {
    arr[_key] = arguments[_key];
  }

  for (var i = 0; i != arr.length; ++i) {
    Add(arr[i], sum, sum);
  }return sum;
}

/**
 * Return 'a - b'
 * @param {v2} a
 * @param {v2} b
 * @param {v2} out (optional) where the result is written
 * @returns {v2}
 */
function Sub(a, b, out) {
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
function MulS(a, b, out) {
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
function MulV(a, b, out) {
  out = out || create();
  out[0] = a[0] * b[0];
  out[1] = a[1] * b[1];
  return out;
}

/***/ }),
/* 7 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.MulticastDelegate = exports.DateFormat = undefined;

var _createClass = function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; }(); /**
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      * @module Util
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      */

exports.BeginInvoke = BeginInvoke;
exports.MemCopy = MemCopy;
exports.CopyTo = CopyTo;
exports.TrimLeft = TrimLeft;
exports.TrimRight = TrimRight;
exports.Trim = Trim;
exports.ColourToUint = ColourToUint;
exports.ColourToV4 = ColourToV4;
exports.MeasureString = MeasureString;
exports.FontHeight = FontHeight;

var _v = __webpack_require__(1);

var v4 = _interopRequireWildcard(_v);

var _date_format = __webpack_require__(19);

var DF = _interopRequireWildcard(_date_format);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

var DateFormat = exports.DateFormat = DF.DateFormat;

/**
 * Invoke a function on the next message
 * @param {function} func 
 */
function BeginInvoke(func) {
	setTimeout(func, 0);
}

/**
 * Copy a range of objects from one array to another
 * @param {Array} dst 
 * @param {Number} dst_index 
 * @param {Array} src 
 * @param {Number} src_index 
 * @param {Number} count 
 */
function MemCopy(dst, dst_index, src, src_index, count) {
	for (; count-- != 0;) {
		dst[dst_index++] = src[src_index++];
	}
}

/**
 * Variadic function that copies objects into 'dst'
 * Use: CopyTo(my_array, 0, thing1, thing2, thing3, ...)
 * @param {Array} dst 
 * @param {Number} dst_index 
 * @returns {Array} Returns 'dst'
 */
function CopyTo(dst, dst_index) {
	for (var i = 2; i != arguments.length; ++i) {
		dst[dst_index++] = arguments[i];
	}return dst;
}

/**
 * Return a string with 'chars' removed from the left hand side
 * @param {string} str The string to trim
 * @param {[]} chars An array of characters to remove from 'str'
 * @returns {string}
 */
function TrimLeft(str, chars) {
	for (var s = 0; s != str.length && chars.indexOf(str[s]) != -1; ++s) {}
	return str.slice(s);
}

/**
 * Return a string with 'chars' removed from the right hand side
 * @param {string} str The string to trim
 * @param {[]} chars An array of characters to remove from 'str'
 * @returns {string}
 */
function TrimRight(str, chars) {
	for (var e = str.length; e-- != 0 && chars.indexOf(str[e]) != -1;) {}
	return str.slice(0, e + 1);
}

/**
 * Return a string with 'chars' removed from both ends
 * @param {string} str The string to trim
 * @param {[]} chars An array of characters to remove from 'str'
 * @returns {string}
 */
function Trim(str, chars) {
	for (var s = 0; s != str.length && chars.indexOf(str[s]) != -1; ++s) {}
	for (var e = str.length; e-- != 0 && chars.indexOf(str[e]) != -1;) {}
	return str.slice(s, e + 1);
}

/**
 * Convert a CSS colour to an unsigned int
 * @param {string} colour A CSS colour, e.g. #RRGGBB
 * @returns {Number} The colour as a uint (alpha = 0xff)
 */
function ColourToUint(colour) {
	if (colour[0] == '#') {
		if (colour.length == 9) // #aarrggbb
			{
				return parseInt(colour.substr(1), 16);
			}
		if (colour.length == 7) // #rrggbb
			{
				return 0xFF000000 | parseInt(colour.substr(1), 16);
			}
		if (colour.length == 4) // #rgb
			{
				var g = parseInt(colour[1], 16) * 0xff / 0x0f;
				var r = parseInt(colour[2], 16) * 0xff / 0x0f;
				var b = parseInt(colour[3], 16) * 0xff / 0x0f;
				return 0xFF000000 | r << 16 | g << 8 | b;
			}
	}
	switch (colour.toLowerCase()) {
		case 'black':
			return 0xFF000000;
		case 'white':
			return 0xFFFFFFFF;
		case 'red':
			return 0xFFFF0000;
		case 'green':
			return 0xFF00FF00;
		case 'blue':
			return 0xFF0000FF;
	}
	throw new Error("Unsupported colour format");
}

/**
 * Convert a CSS colour to an array of floats [r,g,b,a]
 * @param {string} colour A CSS colour, e.g. #RRGGBB
 * @returns {v4} The colour as an array of floats
 */
function ColourToV4(colour) {
	if (colour[0] == '#') {
		if (colour.length == 9) // #aarrggbb
			{
				var a = parseInt(colour.substr(1, 2), 16) / 0xff;
				var r = parseInt(colour.substr(3, 2), 16) / 0xff;
				var g = parseInt(colour.substr(5, 2), 16) / 0xff;
				var b = parseInt(colour.substr(7, 2), 16) / 0xff;
				return v4.make(r, g, b, a);
			}
		if (colour.length == 7) // #rrggbb
			{
				var _r = parseInt(colour.substr(1, 2), 16) / 0xff;
				var _g = parseInt(colour.substr(3, 2), 16) / 0xff;
				var _b = parseInt(colour.substr(5, 2), 16) / 0xff;
				return v4.make(_r, _g, _b, 1);
			}
		if (colour.length == 4) // #rgb
			{
				var _g2 = parseInt(colour[1], 16) / 0x0f;
				var _r2 = parseInt(colour[2], 16) / 0x0f;
				var _b2 = parseInt(colour[3], 16) / 0x0f;
				return v4.make(_r2, _g2, _b2, 1);
			}
	}
	switch (colour.toLowerCase()) {
		case 'black':
			return v4.make(0, 0, 0, 1);
		case 'white':
			return v4.make(1, 1, 1, 1);
		case 'red':
			return v4.make(1, 0, 0, 1);
		case 'green':
			return v4.make(0, 1, 0, 1);
		case 'blue':
			return v4.make(0, 0, 1, 1);
	}
	throw new Error("Unsupported colour format");
}

/**
 * Measure the width and height of 'text'
 * @param {CanvasRenderingContext2D} gfx
 * @param {string} text The text to measure
 * @param {Font} font The font to use to render the text
 * @returns {{width, height}} 
 */
function MeasureString(gfx, text, font) {
	// Measure width using the 2D canvas API
	var width = gfx.measureText(text, font).width;

	// Get the font height
	var height = FontHeight(gfx, font);

	return { width: width, height: height };
}

/**
 * Measure the height of a font
 * @param {CanvasRenderingContext2D} gfx
 * @param {*} font 
 */
function FontHeight(gfx, font) {
	// Get the font height
	var height = font_height_cache[font] || function () {
		var height = 0;

		// Create an off-screen canvas
		var cv = gfx.canvas.cloneNode(false);
		var ctx = cv.getContext("2d");

		// Measure the width of 'M' and resize the canvas
		ctx.font = font;
		cv.width = ctx.measureText("M", font).width;
		cv.height = cv.width * 2;
		if (cv.width != 0) {
			// Draw 'M'and 'p' onto the canvas so we can measure the height.
			// Changing the width/height means the properties need setting again.
			ctx.fillRect(0, 0, cv.width, cv.height);
			ctx.imageSmoothingEnabled = false;
			ctx.textBaseline = 'top';
			ctx.fillStyle = 'white';
			ctx.font = font;
			ctx.fillText("M", 0, 0);
			ctx.fillText("p", 0, 0);

			// Scan for white pixels
			// Record the last row to have any non-black pixels
			var pixels = ctx.getImageData(0, 0, cv.width, cv.height).data;
			for (var y = 0; y != cv.height; ++y) {
				var i = y * cv.height,
				    iend = i + cv.width;
				for (; i != iend && pixels[i] == 0; ++i) {}
				if (i != iend) height = y;
			}
		}
		return font_height_cache[font] = height;
	}();
	return height;
}
var font_height_cache = {};

/**
 * A C#-isk event type
 */

var MulticastDelegate = exports.MulticastDelegate = function () {
	function MulticastDelegate() {
		_classCallCheck(this, MulticastDelegate);

		this.m_handlers = [];
	}

	_createClass(MulticastDelegate, [{
		key: "sub",
		value: function sub(handler) {
			var idx = this.m_handlers.indexOf(handler);
			this.m_handlers.push(handler);
		}
	}, {
		key: "unsub",
		value: function unsub(handler) {
			var idx = this.m_handlers.indexOf(handler);
			if (idx != -1) this.m_handlers.splice(idx, 1);
		}
	}, {
		key: "invoke",
		value: function invoke(sender, args) {
			for (var i = 0; i != this.m_handlers.length; ++i) {
				this.m_handlers[i](sender, args);
			}
		}
	}]);

	return MulticastDelegate;
}();

/***/ }),
/* 8 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.Range = exports.m4x4 = exports.v4 = exports.Util = exports.Chart = exports.Rdr = exports.Maths = exports.Alg = undefined;

var _algorithm = __webpack_require__(9);

var Alg = _interopRequireWildcard(_algorithm);

var _maths = __webpack_require__(0);

var Maths = _interopRequireWildcard(_maths);

var _renderer = __webpack_require__(3);

var Rdr = _interopRequireWildcard(_renderer);

var _chart = __webpack_require__(18);

var Chart = _interopRequireWildcard(_chart);

var _util = __webpack_require__(7);

var Util = _interopRequireWildcard(_util);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

exports.Alg = Alg;
exports.Maths = Maths;
exports.Rdr = Rdr;
exports.Chart = Chart;
exports.Util = Util;

// Aliases

var v4 = exports.v4 = Maths.v4;
var m4x4 = exports.m4x4 = Maths.m4x4;
var Range = exports.Range = Maths.Range;

/***/ }),
/* 9 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.Swap = Swap;
exports.Partition = Partition;
exports.Erase = Erase;
exports.EraseIf = EraseIf;
exports.BinarySearch = BinarySearch;
/**
 * @module algorithm
 */

/**
 * Swap elements 'i' and 'j' in an array
 * @param {Array} arr The array containing the elements to swap
 * @param {Number} i The index of an element to swap
 * @param {Number} j The index of an element to swap
 */
function Swap(arr, i, j) {
	var tmp = arr[i];
	arr[i] = arr[j];
	arr[j] = tmp;
}

/**
 * Move elements in 'arr' that don't satisfy 'pred' to the end of the array.
 * @param {Array} arr The array to move elements in
 * @param {Function} pred A function that returns true for elements to be moved
 * @param {boolean} stable True if the order of 'arr' must be preserved. Note: the front half of the array is always stable, only the moved elements may not be stable
 * @returns {Number} Returns the index of the first element not satisfying 'pred'
 */
function Partition(arr, pred, stable) {
	var first = 0,
	    last = arr.length;
	stable = stable !== undefined && stable;

	// Skip in-place elements at beginning
	for (; first != last && pred(arr[first]); ++first) {}
	if (first == last) return first;

	// Skip in-place elements at the end
	for (; last != first && !pred(arr[last - 1]); --last) {}
	if (first == last) return first;

	if (stable) {
		// Store out of place elements in a temporary buffer while moving others forward
		var buffer = [arr[first]];
		for (var next = first; ++next != last;) {
			if (pred(arr[next])) {
				arr[first] = arr[next];
				++first;
			} else {
				buffer.push(arr[next]);
			}
		}

		// Copy the temporary buffer elements back into 'arr' at 'first'
		for (var _next = first; _next != last; ++_next) {
			arr[_next] = buffer.shift();
		}return first;
	} else {
		// Swap out of place elements
		for (var _next2 = first; ++_next2 != last;) {
			if (pred(arr[_next2])) {
				// out of place, swap and loop
				Swap(arr, first, _next2);
				++first;
			}
		}
		return first;
	}
}

/**
 * Erase an element from an array
 * @param {Array} arr The array to search in
 * @param {} element The element to erase
 * @param {Number} start (optional) The index to start searching from
 * @returns {Number} The index position of where the element was erased from
 */
function Erase(arr, element, start) {
	var idx = arr.indexOf(element, start);
	if (idx != -1) arr.splice(idx, 1);
	return idx;
}

/**
 * Remove elements from an array that satisfy 'pred'
 * @param {Array} arr 
 * @param {Function} pred 
 * @param {boolean} stable 
 * @returns Returns the number of elements removed
 */
function EraseIf(arr, pred, stable) {
	var len = Partition(arr, function (x) {
		return !pred(x);
	}, false);
	arr.length = len;
	return len;
}

/**
 * Binary search for an element using only a predicate function.
 * Returns the index of the element if found or the 2s-complement of the first.
 * @param {Array} arr The array to search
 * @param {Function} cmp Comparison function, return <0 if T is less than the target, >0 if greater, or ==0 if equal
 * @param {boolean} insert_position (optional, default = false) True to always return positive indices (i.e. when searching to find insert position)
 * @returns {Number} The index of the element if found or the 2s-complement of the first element larger than the one searched for.
 */
function BinarySearch(arr, cmp, find_insert_position) {
	var idx = ~0;
	if (arr.length != 0) {
		for (var b = 0, e = arr.length;;) {
			var m = b + (e - b >> 1); // prevent overflow
			var c = cmp(arr[m]); // <0 means arr[m] is less than the target element
			if (c == 0) {
				idx = m;break;
			}
			if (c < 0) {
				if (m == b) {
					idx = ~e;break;
				}b = m;continue;
			}
			if (c > 0) {
				if (m == b) {
					idx = ~b;break;
				}e = m;
			}
		}
	}
	if (find_insert_position !== undefined && find_insert_position && idx < 0) idx = ~idx;
	return idx;
}

/***/ }),
/* 10 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});

var _createClass = function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; }(); /**
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      * @module Range
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      */

exports.create = create;
exports.make = make;
exports.set = set;
exports.clone = clone;
exports.Contains = Contains;
exports.ContainsInclusive = ContainsInclusive;
exports.Compare = Compare;
exports.Encompass = Encompass;
exports.EncompassRange = EncompassRange;
exports.Union = Union;
exports.Intersect = Intersect;

var _maths = __webpack_require__(0);

var Maths = _interopRequireWildcard(_maths);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

/**
 * Constants
 */
var Invalid = make(Number.MAX_SAFE_INTEGER, -Number.MAX_SAFE_INTEGER);

/**
 * Create a zero range: [0,0)
 * @returns {beg,end}
 */
function create() {
	return new (function () {
		function _class() {
			_classCallCheck(this, _class);

			this.beg = 0;
			this.end = 0;
		}

		/**
   * Return the centre of the range
   * @returns {Number}
   */


		_createClass(_class, [{
			key: "centre",
			get: function get() {
				return (this.beg + this.end) * 0.5;
			}

			/**
    * Return the size of range, i.e. end - beg
    * @returns {Number}
    */

		}, {
			key: "size",
			get: function get() {
				return this.end - this.beg;
			}

			/**
    * Return the beginning of the range, rounded to an integer
    * @returns {Number}
    */

		}, {
			key: "begi",
			get: function get() {
				return Math.floor(this.beg);
			}

			/**
    * Return the end of the range, rounded to an integer
    * @returns {Number}
    */

		}, {
			key: "endi",
			get: function get() {
				return Math.floor(this.end);
			}

			/**
    * Return the span of this range as an integer
    * @returns {Number}
    */

		}, {
			key: "count",
			get: function get() {
				return this.endi - this.begi;
			}
		}]);

		return _class;
	}())();
}

/**
 * Create a new range from [beg, end)
 * @param {Number} beg The inclusive start of the range
 * @param {Number} end The exclusive end of the range
 * @returns {Range}
 */
function make(beg, end) {
	var out = create();
	return set(out, beg, end);
}

/**
 * Set the components of a range
 * @param {Range} range
 * @param {Number} beg 
 * @param {Number} end 
 * @returns {Range}
 */
function set(range, beg, end) {
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
function clone(range, out) {
	out = out || create();
	if (range !== out) {
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
function Contains(range, value) {
	return range.beg <= value && value < range.end;
}

/**
 * Returns true if 'value' is within the range [Begin,End] (i.e. end inclusive)
 * @param {Range} range The range to test for containing 'value'
 * @param {Number} value The number to test for being within 'range'
 * @returns {boolean} True if 'value' is within 'range'
 */
function ContainsInclusive(range, value) {
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
function Compare(range, value) {
	return range.end <= value ? -1 : range.beg > value ? +1 : 0;
}

/**
 * Grow the bounds of 'range' to include 'value'
 * @param {Range} range
 * @param {Number} value
 */
function Encompass(range, value) {
	range.beg = Math.min(range.beg, value);
	range.end = Math.max(range.end, value);
}

/**
 * Grow the bounds of 'range0' to include 'range1'
 * @param {Range} range0
 * @param {Range} range1
 */
function EncompassRange(range0, range1) {
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
function Union(lhs, rhs) {
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
function Intersect(lhs, rhs) {
	//Debug.Assert(Size >= 0, "this range is inside out");
	//Debug.Assert(rng.Size >= 0, "'rng' is inside out");
	if (rhs.end <= lhs.beg) return make(lhs.beg, lhs.beg);
	if (rhs.beg >= lhs.end) return make(lhs.end, lhs.end);
	return make(Math.max(lhs.beg, rhs.beg), Math.min(lhs.end, rhs.end));
}

/***/ }),
/* 11 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.Create = Create;
exports.CreateRaw = CreateRaw;

var _renderer = __webpack_require__(3);

var Rdr = _interopRequireWildcard(_renderer);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

/**
 * Create a model.
 * 'nuggets' is an array of
 *    [{
 *      topo: rdr.TRIANGLES,
 *      shader: null,
 *      tex_diffuse: null,                               // optional
 *      tint: [1,1,1,1],                                 // optional
 *      light: Light.Directional([-1,-1,-1], [1,1,0,1]), // optional
 *      irange: {ofs:0, count:indices.length},           // optional
 *      vrange: {ofs:0, count:verts.length}              // optional
 *    },..]
 * @param {WebGL} rdr the WebGL instance returned from rdr.Initialise
 * @param {[{pos:[x,y,z], norm:[x,y,z], col:[r,g,b,a], tex0:[u,v]},...]} verts the array of vertex data for the model
 * @param {[Number]} indices the index data for the model (ushort)
 * @param {[]} nuggets the nugget data for the model
 * @param {const} opts (optional) the usage for the model buffers (default: rdr.STATIC_DRAW)
 * @returns {Model}
 */
function Create(rdr, verts, indices, nuggets, opts) {
	var pos = null;
	var norm = null;
	var col = null;
	var tex0 = null;
	var indx = null;

	// Fill verts buffer
	if (verts != null) {
		// Verts have position data
		if (verts[0].hasOwnProperty("pos")) {
			pos = new Float32Array(verts.length * 3);
			for (var i = 0, j = 0; i != verts.length; ++i) {
				var p = verts[i].pos;
				pos[j++] = p[0];
				pos[j++] = p[1];
				pos[j++] = p[2];
			}
		}

		// Verts have normals data
		if (verts[0].hasOwnProperty("norm")) {
			norm = new Float32Array(verts.length * 3);
			for (var _i = 0, _j = 0; _i != verts.length; ++_i) {
				var n = verts[_i].norm;
				norm[_j++] = n[0];
				norm[_j++] = n[1];
				norm[_j++] = n[2];
			}
		}

		// Verts have colour data
		if (verts[0].hasOwnProperty("col")) {
			col = new Float32Array(verts.length * 4);
			for (var _i2 = 0, _j2 = 0; _i2 != verts.length; ++_i2) {
				var c = verts[_i2].col;
				col[_j2++] = c[0];
				col[_j2++] = c[1];
				col[_j2++] = c[2];
				col[_j2++] = c[3];
			}
		}

		// Verts have texture coords
		if (verts[0].hasOwnProperty("tex0")) {
			tex0 = new Float32Array(verts.length * 2);
			for (var _i3 = 0, _j3 = 0; _i3 != verts.length; ++_i3) {
				var t = verts[_i3].tex0;
				tex0[_j3++] = t[0];
				tex0[_j3++] = t[1];
			}
		}
	}

	// Fill the index buffer
	if (indices != null) {
		indx = new Uint16Array(indices);
	}

	return CreateRaw(rdr, pos, norm, col, tex0, indx, nuggets, opts);
}

/**
 * Create a model from separate buffers of vertices, normals, colours, and texture coords
 * @param {Renderer} rdr The renderer main object
 * @param {Float32Array} pos The buffer of vertex positions (stride = 3)
 * @param {Float32Array} norm The buffer of vertex normals (stride = 3)
 * @param {Float32Array} col The buffer of vertex colours (stride = 4)
 * @param {Float32Array} tex0 The buffer of vectex texture coords (stride = 2)
 * @param {Uint16Array} indices The buffer or index data
 * @param {[]} nuggets The buffer of render nuggets
 * @param {} opts (Optional) Optional settings
 * @returns {Model}
 */
function CreateRaw(rdr, pos, norm, col, tex0, indices, nuggets, opts) {
	var model = {
		geom: Rdr.EGeom.None,
		vbuffer: null,
		nbuffer: null,
		cbuffer: null,
		tbuffer: null,
		ibuffer: null,
		gbuffer: []
	};

	// Default buffer usage
	var usage = opts && opts.usage ? opts.usage : rdr.STATIC_DRAW;

	// Verts
	if (pos != null) {
		model.geom = model.geom | Rdr.EGeom.Vert;
		model.vbuffer = rdr.createBuffer();
		model.vbuffer.stride = 3;
		model.vbuffer.count = pos.length / model.vbuffer.stride;
		rdr.bindBuffer(rdr.ARRAY_BUFFER, model.vbuffer);
		rdr.bufferData(rdr.ARRAY_BUFFER, pos, usage);
	}

	// Normals
	if (norm != null) {
		model.geom = model.geom | Rdr.EGeom.Norm;
		model.nbuffer = rdr.createBuffer();
		model.nbuffer.stride = 3;
		model.nbuffer.count = norm.length / model.nbuffer.stride;
		rdr.bindBuffer(rdr.ARRAY_BUFFER, model.nbuffer);
		rdr.bufferData(rdr.ARRAY_BUFFER, norm, usage);
	}

	// Colours
	if (col != null) {
		model.geom = model.geom | Rdr.EGeom.Colr;
		model.cbuffer = rdr.createBuffer();
		model.cbuffer.stride = 4;
		model.cbuffer.count = col.length / model.cbuffer.stride;
		rdr.bindBuffer(rdr.ARRAY_BUFFER, model.cbuffer);
		rdr.bufferData(rdr.ARRAY_BUFFER, col, usage);
	}

	// Texture Coords
	if (tex0 != null) {
		model.geom = model.geom | Rdr.EGeom.Tex0;
		model.tbuffer = rdr.createBuffer();
		model.tbuffer.stride = 2;
		model.tbuffer.count = tex0.length / model.tbuffer.stride;
		rdr.bindBuffer(rdr.ARRAY_BUFFER, model.tbuffer);
		rdr.bufferData(rdr.ARRAY_BUFFER, tex0, usage);
	}

	// Index buffer
	if (indices != null) {
		model.ibuffer = rdr.createBuffer();
		model.ibuffer.count = indices.length;
		rdr.bindBuffer(rdr.ELEMENT_ARRAY_BUFFER, model.ibuffer);
		rdr.bufferData(rdr.ELEMENT_ARRAY_BUFFER, indices, usage);
	}

	// Fill the nuggets buffer
	if (nuggets != null) {
		if (!nuggets[0].hasOwnProperty("topo")) throw new Error("Nuggets must include a topology field");
		if (!nuggets[0].hasOwnProperty("shader")) throw new Error("Nuggets must include a shader field");

		model.gbuffer = nuggets.slice();
	}

	return model;
}

/***/ }),
/* 12 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.Create = Create;

var _m4x = __webpack_require__(2);

var m4x4 = _interopRequireWildcard(_m4x);

var _renderer = __webpack_require__(3);

var Rdr = _interopRequireWildcard(_renderer);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

/**
 * Create an instance of a model
 * @param {string} name The name to associate with the instance
 * @param {*} model The model that this is an instance of
 * @param {*} o2w (optional) The initial object to world transform for the instance
 * @param {*} flags (optional) Rendering flags
 */
function Create(name, model, o2w, flags) {
	var inst = {
		name: name,
		model: model,
		o2w: o2w || m4x4.create(),
		flags: flags || Rdr.EFlags.None
	};
	return inst;
}

/***/ }),
/* 13 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.Create = Create;
exports.CreateStockTextures = CreateStockTextures;

var _renderer = __webpack_require__(3);

var Rdr = _interopRequireWildcard(_renderer);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

/**
 * Create a texture from a file.
 * @param {WebGL} rdr is the WebGL instance
 * @param {string} filepath is the filepath to an image to load into the texture
 * @param {} opts is an optional parameters structure:
 * {
 *    mag_filter: gl.LINEAR,
 *    min_filter: gl.LINEAR,
 *    wrap_t: gl.CLAMP_TO_EDGE,
 *    wrap_s: gl.CLAMP_TO_EDGE,
 * }
 * @returns {} the created texture
 */
function Create(rdr, filepath, opts) {
	var tex = rdr.createTexture();
	tex.image = new Image();
	tex.image.onload = function () {
		rdr.bindTexture(rdr.TEXTURE_2D, tex);
		rdr.pixelStorei(rdr.UNPACK_FLIP_Y_WEBGL, true);
		rdr.texImage2D(rdr.TEXTURE_2D, 0, rdr.RGBA, rdr.RGBA, rdr.UNSIGNED_BYTE, tex.image);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MAG_FILTER, opts && opts.mag_filter ? opts.mag_filter : rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MIN_FILTER, opts && opts.min_filter ? opts.min_filter : rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_T, opts && opts.wrap_t ? opts.wrap_t : rdr.CLAMP_TO_EDGE);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_S, opts && opts.wrap_s ? opts.wrap_s : rdr.CLAMP_TO_EDGE);
		rdr.generateMipmap(rdr.TEXTURE_2D);
		rdr.bindTexture(rdr.TEXTURE_2D, null);
		tex.image_loaded = true;
	};
	tex.image.src = filepath;
	tex.image_loaded = false;
	return tex;
}

/**
 * Create stock textures.
 * Used while textures are loading and to bind to the sampler when no texture is available.
 */
function CreateStockTextures(rdr) {
	rdr.stock_textures = {};

	{
		// White
		rdr.stock_textures.white = rdr.createTexture();
		rdr.bindTexture(rdr.TEXTURE_2D, rdr.stock_textures.white);
		rdr.texImage2D(rdr.TEXTURE_2D, 0, rdr.RGBA, 1, 1, 0, rdr.RGBA, rdr.UNSIGNED_BYTE, new Uint8Array([255, 255, 255, 255]));
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MAG_FILTER, rdr.NEAREST);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MIN_FILTER, rdr.NEAREST);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_T, rdr.CLAMP_TO_EDGE);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_S, rdr.CLAMP_TO_EDGE);
		rdr.generateMipmap(rdr.TEXTURE_2D);
		rdr.stock_textures.white.image_loaded = true;
	}
	{
		// Checker1
		var dim = 8;
		var c = 0xFF;
		var i = 0,
		    j = 0;
		var _data = new Uint8Array(dim * dim * 4);
		for (; i != 8;) {
			_data[i++] = 0xFF;
		}for (; i != 16;) {
			_data[i++] = 0x00;
		}for (; i != _data.length;) {
			_data[i++] = _data[j++];
			if (i % 64 == 0) j -= 8;
		}

		rdr.stock_textures.checker1 = rdr.createTexture();
		rdr.bindTexture(rdr.TEXTURE_2D, rdr.stock_textures.checker1);
		rdr.texImage2D(rdr.TEXTURE_2D, 0, rdr.RGBA, dim, dim, 0, rdr.RGBA, rdr.UNSIGNED_BYTE, _data);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MAG_FILTER, rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MIN_FILTER, rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_T, rdr.REPEAT);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_S, rdr.REPEAT);
		rdr.generateMipmap(rdr.TEXTURE_2D);
		rdr.stock_textures.checker1.image_loaded = true;
	}
	{
		// Checker3
		var _dim = 8;
		var _i = 0,
		    _j = 0;
		var data = new Uint8Array(_dim * _dim * 4);
		for (; _i != 9;) {
			data[_i++] = 0xFF;
		}for (; _i != 16;) {
			data[_i++] = 0xEE;
		}for (; _i != data.length;) {
			data[_i++] = data[_j++];
			if (_i % 64 == 0) _j -= 8;
		}

		rdr.stock_textures.checker3 = rdr.createTexture();
		rdr.bindTexture(rdr.TEXTURE_2D, rdr.stock_textures.checker3);
		rdr.texImage2D(rdr.TEXTURE_2D, 0, rdr.RGBA, _dim, _dim, 0, rdr.RGBA, rdr.UNSIGNED_BYTE, data);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MAG_FILTER, rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_MIN_FILTER, rdr.LINEAR);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_T, rdr.REPEAT);
		rdr.texParameteri(rdr.TEXTURE_2D, rdr.TEXTURE_WRAP_S, rdr.REPEAT);
		rdr.generateMipmap(rdr.TEXTURE_2D);
		rdr.stock_textures.checker3.image_loaded = true;
	}
	rdr.bindTexture(rdr.TEXTURE_2D, null);
}

/***/ }),
/* 14 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.Create = Create;

var _v = __webpack_require__(1);

var v4 = _interopRequireWildcard(_v);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

var Light = function Light(lighting_type, pos, dir, ambient, diffuse, specular, specpwr) {
	_classCallCheck(this, Light);

	this.lighting_type = lighting_type;
	this.position = pos;
	this.direction = v4.Normalise(dir);
	this.ambient = ambient || [0, 0, 0];
	this.diffuse = diffuse || [0.5, 0.5, 0.5];
	this.specular = specular || [0, 0, 0];
	this.specpwr = specpwr || 0;
};

/**
 * Create a light source.
 * @param {ELight} lighting_type 
 * @param {v4} pos the position of the light (in world space)
 * @param {v4} dir the direction the light is pointing (in world space)
 * @param {[Number]} ambient a 3 element array containing R,G,B for the ambient light
 * @param {[Number]} diffuse a 3 element array containing R,G,B for the diffuse light
 * @param {[Number]} specular a 3 element array containing R,G,B for the specular light
 * @param {Number} specpwr the specular power
 * @returns {Light}
 */


function Create(lighting_type, pos, dir, ambient, diffuse, specular, specpwr) {
	return new Light(lighting_type, pos, dir, ambient, diffuse, specular, specpwr);
}

/***/ }),
/* 15 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
    value: true
});
exports.CompileShader = CompileShader;
var forward_vs = '\nattribute vec3 position;\nattribute vec3 normal;\nattribute vec4 colour;\nattribute vec2 texcoord;\n\nuniform mat4 o2w;\nuniform mat4 w2c;\nuniform mat4 c2s;\n\nuniform vec4 tint_colour;\n\nvarying vec4 ps_ws_position;\nvarying vec4 ps_ws_normal;\nvarying vec4 ps_colour;\nvarying vec2 ps_texcoord;\n\nvoid main(void)\n{\n    ps_ws_position = o2w * vec4(position, 1.0);\n    ps_ws_normal = o2w * vec4(normal, 0.0);\n    ps_colour =  colour * tint_colour;\n    ps_texcoord = texcoord;\n    gl_Position = c2s * w2c * ps_ws_position;\n}\n';

// Return the compiled shader
function CompileShader(gl) {
    // Create a shader instance
    var shader = gl.createShader(gl.VERTEX_SHADER);

    // Compile the shader
    gl.shaderSource(shader, forward_vs);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) throw new Error('Could not compile shader. \n\n' + gl.getShaderInfoLog(shader));

    return shader;
}

/***/ }),
/* 16 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.CompileShader = CompileShader;
var forward_ps = '\nprecision mediump float;\n\nuniform vec3 cam_ws_position;\nuniform vec3 cam_ws_forward;\n\nuniform bool has_tex_diffuse;\nuniform sampler2D sampler_diffuse;\n\nuniform int lighting_type; // 0 = no, 1 = directional, 2 = radial, 3 = spot\nuniform vec3 light_position;\nuniform vec3 light_direction;\nuniform vec3 light_ambient;\nuniform vec3 light_diffuse;\nuniform vec3 light_specular;\nuniform float light_specpwr;\n\nvarying vec4 ps_ws_position;\nvarying vec4 ps_ws_normal;\nvarying vec4 ps_colour;\nvarying vec2 ps_texcoord;\n\nvec4 Illuminate(vec4 ws_pos, vec4 ws_norm, vec4 diff);\n\nvoid main(void)\n{\n\tvec4 ws_pos = ps_ws_position;\n\tvec4 ws_norm = normalize(ps_ws_normal);\n\tvec4 diff = ps_colour;\n\t\n\tif (has_tex_diffuse)\n\t\tdiff = diff * texture2D(sampler_diffuse, ps_texcoord);\n\n\tif (lighting_type != 0)\n\t\tdiff = Illuminate(ws_pos, ws_norm, diff);\n\n\tgl_FragColor = diff;\n}\n\n// Directional light intensity\nfloat LightDirectional(vec4 ws_norm, float alpha)\n{\n\tfloat brightness = -dot(light_direction, ws_norm.xyz);\n\treturn mix(clamp(brightness, 0.0, 1.0), (1.0 - alpha) * abs(brightness), 1.0 - alpha);\n}\n\n// Specular light intensity\nfloat LightSpecular(vec4 ws_norm, vec4 ws_toeye_norm, float alpha)\n{\n\tvec4 ws_H = normalize(ws_toeye_norm - vec4(light_direction, 0));\n\tfloat brightness = dot(ws_norm, ws_H);\n\tbrightness = mix(clamp(brightness, 0.0, 1.0), (1.0 - alpha) * abs(brightness), 1.0 - alpha);\n\treturn pow(clamp(brightness, 0.0, 1.0), light_specpwr);\n}\n\n// Illuminate \'diff\'\nvec4 Illuminate(vec4 ws_pos, vec4 ws_norm, vec4 diff)\n{\n\tfloat intensity =\n\t\tlighting_type == 1 ? LightDirectional(ws_norm, diff.a) :\n\t\tlighting_type == 2 ? 0.0 :\n\t\tlighting_type == 3 ? 0.0 :\n\t\t0.0;\n\n\tvec4 ltdiff = vec4(0,0,0,0);\n\tltdiff.rgb += light_ambient.rgb;\n\tltdiff.rgb += intensity * light_diffuse.rgb;\n\tltdiff.rgb  = 2.0 * (ltdiff.rgb - 0.5) * diff.rgb;\n\n\tvec4 ltspec = vec4(0,0,0,0);\n\tltspec.rgb += intensity * light_specular.rgb * LightSpecular(ws_norm, normalize(vec4(cam_ws_position, 1) - ws_pos), diff.a);\n\n\t//return vec4(ltdiff.rgb, diff.a);\n\treturn clamp(diff + ltdiff + ltspec, 0.0, 1.0);\n}\n';

// Return the compiled shader
function CompileShader(gl) {
	// Create a shader instance
	var shader = gl.createShader(gl.FRAGMENT_SHADER);

	// Compile the shader
	gl.shaderSource(shader, forward_ps);
	gl.compileShader(shader);
	if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) throw new Error('Could not compile shader. \n\n' + gl.getShaderInfoLog(shader));

	return shader;
}

/***/ }),
/* 17 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.Camera = exports.ELockMask = exports.ENavKey = exports.ENavOp = undefined;

var _createClass = function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; }();

exports.Create = Create;

var _maths = __webpack_require__(0);

var Maths = _interopRequireWildcard(_maths);

var _v = __webpack_require__(1);

var v4 = _interopRequireWildcard(_v);

var _m4x = __webpack_require__(2);

var m4x4 = _interopRequireWildcard(_m4x);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

var ENavOp = exports.ENavOp = Object.freeze({
	"None": 0,
	"Translate": 1 << 0,
	"Rotate": 1 << 1,
	"Zoom": 1 << 2
});

var ENavKey = exports.ENavKey = Object.freeze({
	"Left": 0,
	"Up": 1,
	"Right": 2,
	"Down": 3,
	"In": 4,
	"Out": 5,
	"Rotate": 6, // Key to enable camera rotations, maps translation keys to rotations
	"TranslateZ": 7, // Key to set In/Out to be z translations rather than zoom
	"Accurate": 8,
	"SuperAccurate": 9,
	"PerpendicularZ": 10
});

var ELockMask = exports.ELockMask = Object.freeze({
	"None": 0,
	"TransX": 1 << 0,
	"TransY": 1 << 1,
	"TransZ": 1 << 2,
	"RotX": 1 << 3,
	"RotY": 1 << 4,
	"RotZ": 1 << 5,
	"Zoom": 1 << 6,
	"All": (1 << 7) - 1, // Not including camera relative
	"CameraRelative": 1 << 7
});

/**
 * Camera matrix class
 */

var Camera = exports.Camera = function () {
	function Camera(fovY, aspect, focus_distance, near, far, orthographic) {
		_classCallCheck(this, Camera);

		this.m_c2w = m4x4.create(); // Camera to world transform
		this.m_fovY = fovY || Maths.TauBy8; // Field of view in the Y direction
		this.m_aspect = aspect || 1.0; // Aspect ratio = width/height
		this.m_focus_dist = focus_distance || 1.0; // Distance from the c2w position to the focus, down the z axis
		this.m_align = null; // The direction to align 'up' to, or v4Zero
		this.m_orthographic = orthographic || false; // True for orthographic camera to screen transforms, false for perspective
		this.m_default_fovY = this.fovY; // The default field of view
		this.m_near = near ? near : 0.01; // The near plane as a multiple of the focus distance
		this.m_far = far ? far : 100.0; // The near plane as a multiple of the focus distance
		this.m_base_c2w = m4x4.create(); // The starting position during a mouse movement
		this.m_base_fovY = this.fovY; // The starting FOV during a mouse movement
		this.m_base_focus_dist = this.focus_dist; // The starting focus distance during a mouse movement
		this.m_accuracy_scale = 0.1; // Scale factor for high accuracy control
		this.m_Tref = null; // Movement start reference point for translation
		this.m_Rref = null; // Movement start reference point for rotation
		this.m_Zref = null; // Movement start reference point for zoom
		this.m_lock_mask = ELockMask.None; // Locks on the allowed motion
		this.m_moved = null; // Dirty flag for when the camera moves

		// Default async key state callback function
		this.KeyDown = function (nav_key) {
			return false;
		};
	}

	/**
  * Get/Set the camera to world transform
  * @returns {m4x4}
  */


	_createClass(Camera, [{
		key: "near",


		/**
   * Get the near plane distance (in world space)
   * @param {boolean} focus_relative True to return the focus relative value
   * @returns {Number}
   */
		value: function near(focus_relative) {
			return (focus_relative ? 1 : this.m_focus_dist) * this.m_near;
		}

		/**
   * Get the far plane distance  (in world space)
   * @param {boolean} focus_relative True to return the focus relative value
   * @returns {Number}
   */

	}, {
		key: "far",
		value: function far(focus_relative) {
			return (focus_relative ? 1 : this.m_focus_dist) * this.m_far;
		}

		/**
   * Return the current zoom scaling factor.
   * @returns {Number}
   */

	}, {
		key: "ViewArea",


		/**
   * Return the size of the perpendicular area visible to the camera at 'dist' (in world space)
   * @param {Number} dist (optional) The distance from the camera to calculate the view area. (default: focus_distance)
   * @returns {[w,h]} The width/height of the view area
   */
		value: function ViewArea(dist) {
			dist = !dist || this.orthographic ? this.focus_dist : dist;
			var h = 2.0 * Math.tan(this.fovY * 0.5);
			return [dist * h * this.aspect, dist * h];
		}

		/**
   * Position the camera so that all of 'bbox' is visible to the camera when looking 'forward' and 'up'.
   * @param {BBox} bbox The bounding box to view
   * @param {v4} forward The forward direction of the camera
   * @param {v4} up The up direction of the camera
   * @param {Number} focus_dist (optional, default = 0)
   * @param {boolean} preserve_aspect (optional, default = true)
   * @param {boolean} commit (optional) True to commit the movement
   */

	}, {
		key: "ViewBBox",
		value: function ViewBBox(bbox_, forward, up, focus_dist, preserve_aspect, commit) {
			if (!bbox_.is_valid) throw new Error("Camera: Cannot view an invalid bounding box");
			if (bbox_.is_point) return;

			// Handle degenerate bounding boxes
			var bbox = bbox_;
			if (Maths.FEql(bbox.radius[0], 0)) bbox.radius[0] = 0.001 * v4.Length(bbox.radius);
			if (Maths.FEql(bbox.radius[1], 0)) bbox.radius[1] = 0.001 * v4.Length(bbox.radius);
			if (Maths.FEql(bbox.radius[2], 0)) bbox.radius[2] = 0.001 * v4.Length(bbox.radius);

			// This code projects 'bbox' onto a plane perpendicular to 'forward' and
			// at the nearest point of the bbox to the camera. It then ensures a circle
			// with radius of the projected 2D bbox fits within the view.
			var bbox_centre = bbox.centre;
			var bbox_radius = bbox.radius;

			// Get the distance from the centre of the bbox to the point nearest the camera
			var sizez = Number.MAX_VALUE;
			sizez = Math.min(sizez, Math.abs(v4.Dot(forward, v4.make(bbox_radius[0], bbox_radius[1], bbox_radius[2], 0))));
			sizez = Math.min(sizez, Math.abs(v4.Dot(forward, v4.make(-bbox_radius[0], bbox_radius[1], bbox_radius[2], 0))));
			sizez = Math.min(sizez, Math.abs(v4.Dot(forward, v4.make(bbox_radius[0], -bbox_radius[1], bbox_radius[2], 0))));
			sizez = Math.min(sizez, Math.abs(v4.Dot(forward, v4.make(bbox_radius[0], bbox_radius[1], -bbox_radius[2], 0))));

			// 'focus_dist' is the focus distance (chosen, or specified) from the centre of the bbox
			// to the camera. Since 'size' is the size to fit at the nearest point of the bbox,
			// the focus distance needs to be 'dist' + 'sizez'.
			focus_dist = focus_dist || 0;
			preserve_aspect = preserve_aspect === undefined || preserve_aspect;
			commit = commit === undefined || commit;

			// If not preserving the aspect ratio, determine the width
			// and height of the bbox as viewed from the camera.
			if (!preserve_aspect) {
				// Get the camera orientation matrix
				var c2w = m4x4.make(v4.Cross(up, forward), up, forward, v4.Origin);
				var w2c = m4x4.InvertFast(c2w);

				// Transform the bounding box to camera space
				var bbox_cs = m4x4.MulMB(w2c, bbox);

				// Get the dimensions
				var bbox_cs_size = bbox_cs.size;
				var width = bbox_cs_size[0];
				var height = bbox_cs_size[1];
				//assert(width != 0 && height != 0);

				// Choose the fields of view. If 'focus_dist' is given, then that determines
				// the X,Y field of view. If not, choose a focus distance based on a view size
				// equal to the average of 'width' and 'height' using the default FOV.
				if (focus_dist == 0) {
					var size = (width + height) / 2;
					focus_dist = 0.5 * size / Math.tan(0.5 * this.m_default_fovY);
				}

				// Set the aspect ratio
				var aspect = width / height;
				var d = focus_dist - sizez;

				// Set the aspect and FOV based on the view of the bbox
				this.aspect = aspect;
				this.fovY = 2 * Math.atan(0.5 * height / d);
			} else {
				// 'size' is the *radius* (i.e. not the full height) of the bounding box projected onto the 'forward' plane.
				var _size = Math.sqrt(Maths.Clamp(v4.LengthSq(bbox_radius) - Maths.Sqr(sizez), 0, Number.MAX_VALUE));

				// Choose the focus distance if not given
				if (focus_dist == 0 || focus_dist < sizez) {
					var _d = _size / (Math.tan(0.5 * this.fovY) * this.aspect);
					focus_dist = sizez + _d;
				}
				// Otherwise, set the FOV
				else {
						var _d2 = focus_dist - sizez;
						this.fovY = 2 * Math.atan(_size * this.aspect / _d2);
					}
			}

			// The distance from camera to bbox_centre is 'dist + sizez'
			this.LookAt(v4.Sub(bbox_centre, v4.MulS(forward, focus_dist)), bbox_centre, up, commit);
		}

		/**
   * Set the camera fields of view so that a rectangle with dimensions 'width'/'height' exactly fits the view at 'dist'.
   * @param {Number} width The width of the rectangle to view
   * @param {Number} height The height of the rectangle to view
   * @param {Number} focus_dist (Optional, default = 0)
   */

	}, {
		key: "ViewRect",
		value: function ViewRect(width, height, focus_dist) {
			//PR_ASSERT(PR_DBG, width > 0 && height > 0 && focus_dist >= 0, "");

			// This works for orthographic mode as well so long as we set FOV
			this.aspect = width / height;

			// If 'focus_dist' is given, choose FOV so that the view exactly fits
			if (focus_dist != 0) {
				this.fovY = 2 * Math.atan(0.5 * height / focus_dist);
				this.focus_dist = focus_dist;
			}
			// Otherwise, choose a focus distance that preserves FOV
			else {
					this.focus_dist = 0.5 * height / Math.tan(0.5 * this.fovY);
				}
		}

		/**
   * Position the camera at 'position' looking at 'lookat' with up pointing 'up'.
   * @param {v4} position The world space position of the camera
   * @param {v4} focus_point The target that the camera is to look at
   * @param {v4} up The up direction of the camera in world space
   * @param {*} commit (optional) True if the changes to the camera should be commited (default: true)
   */

	}, {
		key: "LookAt",
		value: function LookAt(position, focus_point, up, commit) {
			m4x4.LookAt(position, focus_point, up, this.m_c2w);
			this.m_focus_dist = v4.Length(v4.Sub(focus_point, position));

			// Set the base values
			if (commit === undefined || commit) this.Commit();
		}

		/**
   * Modify the camera position based on mouse movement.
   * @param {[x,y]} point The normalised screen space mouse position. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
   * @param {ENavOp} nav_op The type of navigation operation to perform
   * @param {boolean} ref_point True on the mouse down/up event, false while dragging
   * @returns {boolean} True if the camera has moved
   */

	}, {
		key: "MouseControl",
		value: function MouseControl(point, nav_op, ref_point) {
			// Navigation operations
			var translate = (nav_op & ENavOp.Translate) != 0;
			var rotate = (nav_op & ENavOp.Rotate) != 0;
			var zoom = (nav_op & ENavOp.Zoom) != 0;

			if (ref_point) {
				if ((nav_op & ENavOp.Translate) != 0) this.m_Tref = point.slice();
				if ((nav_op & ENavOp.Rotate) != 0) this.m_Rref = point.slice();
				if ((nav_op & ENavOp.Zoom) != 0) this.m_Zref = point.slice();
				this.Commit();
			}
			if (zoom || translate && rotate) {
				if (this.KeyDown(ENavKey.TranslateZ)) {
					// Move in a fraction of the focus distance
					var delta = zoom ? point[1] - this.m_Zref[1] : point[1] - this.m_Tref[1];
					this.Translate(0, 0, delta * 10.0, false);
				} else {
					// Zoom the field of view
					var zm = zoom ? this.m_Zref[1] - point[1] : this.m_Tref[1] - point[1];
					this.Zoom(zm, false);
				}
			}
			if (translate && !rotate) {
				var dx = (this.m_Tref[0] - point[0]) * this.focus_dist * Math.tan(this.fovY * 0.5) * this.aspect;
				var dy = (this.m_Tref[1] - point[1]) * this.focus_dist * Math.tan(this.fovY * 0.5);
				this.Translate(dx, dy, 0, false);
			}
			if (rotate && !translate) {
				// If in the roll zone
				if (Maths.Length(this.m_Rref) < 0.80) this.Rotate((point[1] - this.m_Rref[1]) * Maths.TayBy4, (this.m_Rref[0] - point[0]) * Maths.TauBy4, 0, false);else this.Rotate(0, 0, Math.atan2(this.m_Rref[1], this.m_Rref[0]) - Math.atan2(point[1], point[0]), false);
			}
			return this.m_moved;
		}

		/**
   * Modify the camera position in the camera Z direction based on mouse wheel.
   * @param {[x,y]} point A point in normalised camera space. i.e. x=[-1, -1], y=[-1,1] with (-1,-1) == (left,bottom). i.e. normal Cartesian axes
   * @param {Number} delta The mouse wheel scroll delta value (i.e. 120 = 1 click = 10% of the focus distance)
   * @param {boolean} along_ray True if the camera should move along the ray from the camera position to 'point', false to move in the camera z direction.
   * @param {boolean} move_focus True if the focus point should be moved along with the camera
   * @param {boolean} commit (optional) True to commit the movement
   * @returns {boolean} True if the camera has moved
   */

	}, {
		key: "MouseControlZ",
		value: function MouseControlZ(point, dist, along_ray, commit) {
			// Ignore if Z motion is locked
			if ((this.m_lock_mask & ELockMask.TransZ) != 0) return false;

			// Scale by the focus distance
			dist *= this.m_base_focus_dist * 0.1;

			// Get the ray in camera space to move the camera along
			var ray_cs = v4.Neg(v4.ZAxis);
			if (along_ray) {
				// Move along a ray cast from the camera position to the
				// mouse point projected onto the focus plane.
				var pt = this.NSSPointToWSPoint(v4.make(point[0], point[1], this.focus_dist, 0));
				var ray_ws = v4.Sub(pt, this.pos);
				ray_cs = v4.Normalise(m4x4.MulMV(this.w2c, ray_ws), null, { def: v4.Neg(v4.ZAxis) });
			}
			v4.MulS(ray_cs, dist, ray_cs);

			// If 'move_focus', move the focus point too.
			// Otherwise move the camera toward or away from the focus point.
			if (!this.KeyDown(ENavKey.TranslateZ)) this.m_focus_dist = Maths.Clamp(this.m_base_focus_dist + ray_cs[2], this.focus_dist_min, this.focus_dist_max);

			// Translate
			var pos = v4.Add(m4x4.GetW(this.m_base_c2w), m4x4.MulMV(this.m_base_c2w, ray_cs));
			if (!v4.IsNaN(pos)) m4x4.SetW(this.m_c2w, pos);

			// Apply non-camera relative locking
			if (this.m_lock_mask != ELockMask.None && !(this.m_lock_mask & ELockMask.CameraRelative)) {
				if (m_lock_mask & ELockMask.TransX) this.m_c2w[12] = this.m_base_c2w[12];
				if (m_lock_mask & ELockMask.TransY) this.m_c2w[13] = this.m_base_c2w[13];
				if (m_lock_mask & ELockMask.TransZ) this.m_c2w[14] = this.m_base_c2w[14];
			}

			// Set the base values
			if (commit === undefined || commit) this.Commit();

			this.m_moved = true;
			return this.m_moved;
		}

		/**
   * Translate by a camera relative amount.
   * @param {Number} dx The x distance to move
   * @param {Number} dy The y distance to move
   * @param {Number} dz The z distance to move
   * @param {boolean} commit (optional) True to commit the movement
   * @returns {boolean} True if the camera has moved (for consistency with MouseControl)
   */

	}, {
		key: "Translate",
		value: function Translate(dx, dy, dz, commit) {
			if (this.m_lock_mask != ELockMask.None && m_lock_mask & ELockMask.CameraRelative) {
				if (this.m_lock_mask & ELockMask.TransX) dx = 0;
				if (this.m_lock_mask & ELockMask.TransY) dy = 0;
				if (this.m_lock_mask & ELockMask.TransZ) dz = 0;
			}
			if (this.KeyDown(ENavKey.Accurate)) {
				dx *= this.m_accuracy_scale;
				dy *= this.m_accuracy_scale;
				dz *= this.m_accuracy_scale;
				if (this.KeyDown(ENavKey.SuperAccurate)) {
					dx *= this.m_accuracy_scale;
					dy *= this.m_accuracy_scale;
					dz *= this.m_accuracy_scale;
				}
			}

			// Move in a fraction of the focus distance
			dz = -this.m_base_focus_dist * dz * 0.1;
			if (!this.KeyDown(ENavKey.TranslateZ)) this.m_focus_dist = Maths.Clamp(this.m_base_focus_dist + dz, this.focus_dist_min, this.focus_dist_max);

			// Translate
			var pos = v4.Add(m4x4.GetW(this.m_base_c2w), m4x4.MulMV(this.m_base_c2w, v4.make(dx, dy, dz, 0)));
			if (!v4.IsNaN(pos)) m4x4.SetW(this.m_c2w, pos);

			// Apply non-camera relative locking
			if (this.m_lock_mask != ELockMask.None && !(this.m_lock_mask & ELockMask.CameraRelative)) {
				if (this.m_lock_mask & ELockMaskTransX) this.m_c2w[12] = this.m_base_c2w[12];
				if (this.m_lock_mask & ELockMaskTransY) this.m_c2w[13] = this.m_base_c2w[13];
				if (this.m_lock_mask & ELockMaskTransZ) this.m_c2w[14] = this.m_base_c2w[14];
			}

			// Set the base values
			if (commit === undefined || commit) this.Commit();

			this.m_moved = true;
			return this.m_moved;
		}

		/**
   * Rotate the camera by Euler angles about the focus point.
   * @param {Number} pitch
   * @param {Number} yaw
   * @param {Number} roll
   * @param {boolean} commit (optional) True to commit the movement
   * @returns {boolean} True if the camera has moved (for consistency with MouseControl)
   */

	}, {
		key: "Rotate",
		value: function Rotate(pitch, yaw, roll, commit) {
			if (this.m_lock_mask != ELockMask.None) {
				if (this.m_lock_mask & ELockMask.RotX) pitch = 0;
				if (this.m_lock_mask & ELockMask.RotY) yaw = 0;
				if (this.m_lock_mask & ELockMask.RotZ) roll = 0;
			}
			if (this.KeyDown(ENavKey.Accurate)) {
				pitch *= this.m_accuracy_scale;
				yaw *= this.m_accuracy_scale;
				roll *= this.m_accuracy_scale;
				if (this.KeyDown(ENavKey.SuperAccurate)) {
					pitch *= this.m_accuracy_scale;
					yaw *= this.m_accuracy_scale;
					roll *= this.m_accuracy_scale;
				}
			}

			// Save the world space position of the focus point
			var old_focus = this.focus_point;

			// Rotate the camera matrix
			m4x4.clone(m4x4.MulMM(this.m_base_c2w, m4x4.Euler(pitch, yaw, roll, v4.Origin)), this.m_c2w);

			// Position the camera so that the focus is still in the same position
			m4x4.SetW(this.m_c2w, v4.Add(old_focus, v4.MulS(m4x4.GetZ(m_c2w), this.focus_dist)));

			// If an align axis is given, align up to it
			if (this.m_align) {
				var up = v4.Perpendicular(v4.Sub(this.pos, old_focus), this.m_align);
				m4x4.LookAt(this.pos, old_focus, up, this.m_c2w);
			}

			// Set the base values
			if (commit === undefined || commit) this.Commit();

			this.m_moved = true;
			return this.m_moved;
		}

		/**
   * Zoom the field of view.
   * @param {Number} zoom The amount to zoom. Must be in the range (-1, 1) where negative numbers zoom in, positive out.
   * @param {boolean} commit (optional) True to commit the movement
   * @returns {boolean} True if the camera has moved (for consistency with MouseControl).
   */

	}, {
		key: "Zoom",
		value: function Zoom(zoom, commit) {
			if (this.m_lock_mask != ELockMask.None) {
				if (this.m_lock_mask & ELockMask.Zoom) return false;
			}
			if (this.KeyDown(ENavKey.Accurate)) {
				zoom *= this.m_accuracy_scale;
				if (this.KeyDown(ENavKey.SuperAccurate)) zoom *= this.m_accuracy_scale;
			}

			this.m_fovY = (1 + zoom) * this.m_base_fovY;
			this.m_fovY = Maths.Clamp(this.m_fovY, Maths.Tiny, Maths.TauBy2 - Maths.Tiny);

			// Set the base values
			if (commit === undefined || commit) this.Commit();

			this.m_moved = true;
			return this.m_moved;
		}

		/**
   * Return a point in world space corresponding to a normalised screen space point.
   * The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1)
   * The z component should be the depth into the screen (i.e. d*-c2w.z, where 'd' is typically positive)
   * @param {v4} nss_point
   * @returns {v4}
   */

	}, {
		key: "NSSPointToWSPoint",
		value: function NSSPointToWSPoint(nss_point) {
			var half_height = this.focus_dist * Math.tan(this.fovY * 0.5);

			// Calculate the point in camera space
			var point = v4.create();
			point[0] = nss_point[0] * this.aspect * half_height;
			point[1] = nss_point[1] * half_height;
			if (!this.orthographic) {
				var sz = nss_point[2] / this.focus_dist;
				point[0] *= sz;
				point[1] *= sz;
			}
			point[2] = -nss_point[2];
			point[3] = 1.0;
			return m4x4.MulMV(this.c2w, point); // camera space to world space
		}

		/**
   * Return a point in normalised screen space corresponding to 'ws_point'
   * The returned 'z' component will be the world space distance from the camera.
   * @param {v4} ws_point
   * @returns {v4}
   */

	}, {
		key: "WSPointToNSSPoint",
		value: function WSPointToNSSPoint(ws_point) {
			var half_height = this.focus_dist * Math.tan(this.fovY * 0.5);

			// Get the point in camera space and project into normalised screen space
			var cam = m4x4.MulMV(this.w2c, ws_point);

			var point = v4.create();
			point[0] = cam[0] / (this.aspect * half_height);
			point[1] = cam[1] / half_height;
			if (!this.orthographic) {
				var sz = -this.focus_dist / cam[2];
				point[0] *= sz;
				point[1] *= sz;
			}
			point[2] = -cam[2];
			point[3] = 1.0;
			return point;
		}

		/**
   * Return a point and direction in world space corresponding to a normalised screen space point.
   * The x,y components of 'nss_point' should be in normalised screen space, i.e. (-1,-1)->(1,1)
   * The z component should be the world space distance from the camera.
   * @param {v4} nss_point
   * @returns {{ws_point:v4,ws_direction:v4]}
   */

	}, {
		key: "NSSPointToWSRay",
		value: function NSSPointToWSRay(nss_point) {
			var pt = this.NSSPointToWSPoint(nss_point);
			var ws_point = m4x4.GetW(this.c2w);
			var ws_direction = v4.Normalise(v4.Sub(pt, ws_point));
			return { ws_point: ws_point, ws_direction: ws_direction };
		}

		/**
   * Set the current position, FOV, and focus distance as the position reference.
   */

	}, {
		key: "Commit",
		value: function Commit() {
			m4x4.Orthonorm(this.m_c2w, this.m_c2w);
			m4x4.clone(this.m_c2w, this.m_base_c2w);
			this.m_base_fovY = this.m_fovY;
			this.m_base_focus_dist = this.m_focus_dist;
		}

		/**
   * Revert navigation back to the last commit
   */

	}, {
		key: "Revert",
		value: function Revert() {
			m4x4.clone(this.m_base_c2w, this.m_c2w);
			this.m_fovY = this.m_base_fovY;
			this.m_focus_dist = this.m_base_focus_dist;
			this.m_moved = true;
		}
	}, {
		key: "c2w",
		get: function get() {
			return this.m_c2w;
		}

		/**
   * Get the world to camera transform
   * @returns {m4x4}
   */

	}, {
		key: "w2c",
		get: function get() {
			return m4x4.InvertFast(this.m_c2w);
		}

		/**
   * Get the camera to screen transform (i.e. projection transform)
   * @returns {m4x4}
   */

	}, {
		key: "c2s",
		get: function get() {
			var height = 2.0 * this.focus_dist * Math.tan(this.fovY * 0.5);
			return this.orthographic ? m4x4.ProjectionOrthographic(height * this.m_aspect, height, this.near(false), this.far(false), true) : m4x4.ProjectionPerspectiveFOV(this.m_fovY, this.m_aspect, this.near(false), this.far(false), true);
		}

		/**
   * Get/Set the position of the camera in world space
   * @returns {m4x4}
   */

	}, {
		key: "pos",
		get: function get() {
			return m4x4.GetW(this.m_c2w);
		},
		set: function set(value) {
			this.m_moved |= !v4.Eql(value, m4x4.GetW(this.m_c2w));
			m4x4.SetW(this.m_c2w, value);
			m4x4.SetW(this.m_base_c2w, value);
		}

		/**
   * Get/Set the focus distance
   * @returns {Number}
   */

	}, {
		key: "focus_dist",
		get: function get() {
			return this.m_focus_dist;
		},
		set: function set(value) {
			if (isNaN(value) || value < 0) throw new Error("Invalid focus distance");
			this.m_moved |= value != this.m_focus_dist;
			this.m_base_focus_dist = this.m_focus_dist = Maths.Clamp(value, this.focus_dist_min, this.focus_dist_max);
		}

		/**
   * Get the maximum allowed distance for 'focus_dist'
   */

	}, {
		key: "focus_dist_max",
		get: function get() {
			// Clamp so that Near*Far is finite
			// N*F == (m_near * dist) * (m_far * dist) < float_max
			//     == m_near * m_far * dist^2 < float_max
			// => dist < sqrt(float_max) / (m_near * m_far)
			//assert(m_near * m_far > 0);
			var sqrt_float_max = 1.84467435239537E+19;
			return sqrt_float_max / (this.m_near * this.m_far);
		}

		/**
   * Return the minimum allowed value for 'focus_dist'
   */

	}, {
		key: "focus_dist_min",
		get: function get() {
			// Clamp so that N - F is non-zero
			// Abs(N - F) == Abs((m_near * dist) - (m_far * dist)) > float_min
			//       == dist * Abs(m_near - m_far) > float_min
			//       == dist > float_min / Abs(m_near - m_far);
			//assert(m_near < m_far);
			var float_min = 1.175494351e-38;
			return float_min / Math.min(Math.abs(this.m_near - this.m_far), 1.0);
		}

		/**
   * Get/Set the world space position of the focus point
   * Set maintains the current camera orientation
   * @returns {v4}
  */

	}, {
		key: "focus_point",
		get: function get() {
			return v4.Sub(this.pos, v4.MulS(m4x4.GetZ(this.m_c2w), this.focus_dist));
		},
		set: function set(value) {
			// Move the camera position by the difference in focus_point positions
			var pos = v4.Add(this.pos, v4.Sub(value, this.focus_point));
			m4x4.SetW(this.m_c2w, pos);
			m4x4.SetW(this.m_base_c2w, pos);
			this.m_moved = true;
		}

		/**
   * Get the forward direction of the camera (i.e. -Z)
   */

	}, {
		key: "fwd",
		get: function get() {
			return v4.Neg(m4x4.GetW(this.m_c2w));
		}
	}, {
		key: "zoom",
		get: function get() {
			return this.m_default_fovY / this.m_fovY;
		}

		/**
   * Get/Set the horizontal field of view (in radians).
   */

	}, {
		key: "fovX",
		get: function get() {
			return 2.0 * Math.atan(Math.tan(this.m_fovY * 0.5) * this.m_aspect);
		},
		set: function set(value) {
			this.fovY = 2.0 * Math.atan(Math.tan(value * 0.5) / this.m_aspect);
		}

		/**
   * Get/Set the vertical field of view (in radians).
   */

	}, {
		key: "fovY",
		get: function get() {
			return this.m_fovY;
		},
		set: function set(value) {
			if (value <= 0 || value >= Maths.TauBy2) throw new Error("Invalid field of view: " + value);
			value = Maths.Clamp(value, Maths.Tiny, Maths.TauBy2 - Maths.Tiny);
			this.m_moved = value != this.m_fovY;
			this.m_fovY = this.m_base_fovY = value;
		}

		/**
   * Get/Set the aspect ratio
   * @returns {Number}
   */

	}, {
		key: "aspect",
		get: function get() {
			return this.m_aspect;
		},
		set: function set(aspect_w_by_h) {
			if (isNaN(aspect_w_by_h) || aspect_w_by_h <= 0) throw new Error("Invalid camera aspect ratio");

			this.m_moved = aspect_w_by_h != this.m_aspect;
			this.m_aspect = aspect_w_by_h;
		}

		/**
   * Get/Set orthographic projection mode
   * @returns {boolean}
   */

	}, {
		key: "orthographic",
		get: function get() {
			return this.m_orthographic;
		},
		set: function set(value) {
			this.m_orthographic = value;
		}

		/**
   * True if the camera has moved (dirty flag for user code)
   */

	}, {
		key: "moved",
		get: function get() {
			return this.m_moved;
		},
		set: function set(value) {
			this.m_moved = value;
		}
	}]);

	return Camera;
}();

/**
 * Create a Camera instance
 * @param {Number} fovY the field of view in the vertical direction (in radians)
 * @param {Number} aspect the aspect ratio of the view
 * @param {Number} focus_distance the distance to the focus point (in world space)
 * @param {Number} near the distance to the near clip plane (in world space)
 * @param {Number} far the distance to the far clip plane (in world space)
 * @param {boolean} orthographic true for orthographic projection, false for perspective
 * @returns {Camera}
 */


function Create(fovY, aspect, focus_distance, near, far, orthographic) {
	return new Camera(fovY, aspect, focus_distance, near, far, orthographic);
}

/***/ }),
/* 18 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.HitTestResult = exports.ChartDimensions = exports.Chart = exports.EMove = exports.EZone = undefined;

var _createClass = function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; }(); /**
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      * @module Chart
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      */

exports.Create = Create;
exports.CreateDataSeries = CreateDataSeries;

var _maths = __webpack_require__(0);

var Maths = _interopRequireWildcard(_maths);

var _m4x = __webpack_require__(2);

var m4x4 = _interopRequireWildcard(_m4x);

var _v = __webpack_require__(1);

var v4 = _interopRequireWildcard(_v);

var _v2 = __webpack_require__(6);

var v2 = _interopRequireWildcard(_v2);

var _rect = __webpack_require__(5);

var Rect = _interopRequireWildcard(_rect);

var _bbox = __webpack_require__(4);

var BBox = _interopRequireWildcard(_bbox);

var _renderer = __webpack_require__(3);

var Rdr = _interopRequireWildcard(_renderer);

var _util = __webpack_require__(7);

var Util = _interopRequireWildcard(_util);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

function _possibleConstructorReturn(self, call) { if (!self) { throw new ReferenceError("this hasn't been initialised - super() hasn't been called"); } return call && (typeof call === "object" || typeof call === "function") ? call : self; }

function _inherits(subClass, superClass) { if (typeof superClass !== "function" && superClass !== null) { throw new TypeError("Super expression must either be null or a function, not " + typeof superClass); } subClass.prototype = Object.create(superClass && superClass.prototype, { constructor: { value: subClass, enumerable: false, writable: true, configurable: true } }); if (superClass) Object.setPrototypeOf ? Object.setPrototypeOf(subClass, superClass) : subClass.__proto__ = superClass; }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

/**
 * Enumeration of areas on the chart
 */
var EZone = exports.EZone = Object.freeze({
	None: 0,
	Chart: 1 << 0,
	XAxis: 1 << 1,
	YAxis: 1 << 2,
	Title: 1 << 3
});

/**
 * Chart movement types
 */
var EMove = exports.EMove = Object.freeze({
	None: 0,
	XZoomed: 1 << 0,
	YZoomed: 1 << 1,
	Zoomed: 0x3 << 0,
	XScroll: 1 << 2,
	YScroll: 1 << 3,
	Scroll: 0x3 << 2
});

/**
 * Mouse button enumeration
 */
var EButton = Object.freeze({
	Left: 0,
	Middle: 1,
	Right: 2,
	Back: 3,
	Forward: 4
});

/**
 * Chart navigation modes
 */
var ENavMode = Object.freeze({
	Chart2D: 0,
	Scene3D: 1
});

/**
 * Area selection modes
 */
var EAreaSelectMode = Object.freeze({
	Disabled: 0,
	SelectElements: 1,
	Zoom: 2
});

var Chart = exports.Chart = function () {
	/**
  * Create a chart in the given canvas element
  * @param {HTMLCanvasElement} canvas the HTML element to turn into a chart
  * @returns {Chart}
  */
	function Chart(canvas) {
		_classCallCheck(this, Chart);

		var This = this;

		// Hold a reference to the element we're drawing on
		// Create a duplicate canvas to draw 2d stuff on, and position it
		// at the same location, and over the top of, 'canvas'.
		// 'canvas2d' is an overlay over 'canvas3d'.
		this.canvas3d = canvas;
		this.canvas2d = canvas.cloneNode(false);
		this.canvas2d.style.position = "absolute";
		this.canvas2d.style.border = "none"; //this.canvas3d.style.border;
		this.canvas2d.style.left = this.canvas3d.style.left;
		this.canvas2d.style.top = this.canvas3d.style.top;
		canvas.parentNode.insertBefore(this.canvas2d, this.canvas3d);

		// Get the 2D drawing interface
		this.m_gfx = this.canvas2d.getContext("2d");

		// Initialise WebGL
		this.m_rdr = Rdr.Create(this.canvas3d);
		this.m_rdr.back_colour = v4.make(1, 1, 1, 1);
		this.m_redraw_pending = false;

		// Create an area for storing cached data
		this.m_cache = new function () {
			this.chart_frame = null;
			this.xaxis_hash = null;
			this.yaxis_hash = null;
			this.invalidate = function () {
				this.chart_frame = null;
			};
		}();

		// The manager of chart utility graphics
		this.m_tools = new Tools(This);

		// The collection of instances to render on the chart
		this.instances = [];

		// Initialise the chart camera
		this.camera = Rdr.Camera.Create(Maths.TauBy8, this.m_rdr.AspectRatio);
		this.camera.orthographic = true;
		this.camera.LookAt(v4.make(0, 0, 10, 1), v4.Origin, v4.YAxis);
		this.camera.KeyDown = function (nav_key) {
			return false;
		};

		// The light source the illuminates the chart
		this.light = Rdr.Light.Create(Rdr.ELight.Directional, v4.Origin, v4.Neg(v4.ZAxis), null, [0.5, 0.5, 0.5], null, null);

		// Navigation
		this.navigation = new Navigation(This);

		// Chart options
		this.options = new ChartOptions();
		function ChartOptions() {
			this.bk_colour = '#F0F0F0';
			this.chart_bk_colour = '#FFFFFF';
			this.axis_colour = 'black';
			this.grid_colour = '#EEEEEE';
			this.title_colour = 'black';
			this.title_font = "12pt tahoma bold";
			this.note_font = "8pt tahoma";
			this.title_transform = { m00: 1, m01: 0, m10: 0, m11: 1, dx: 0, dy: 0 };
			this.title_padding = 3;
			this.margin = { left: 3, top: 3, right: 3, bottom: 3 };
			this.axis_thickness = 0;
			this.navigation_mode = ENavMode.Chart2D;
			this.show_grid_lines = true;
			this.show_axes = true;
			this.lock_aspect = null;
			this.perpendicular_z_translation = false;
			this.grid_z_offset = 0.001;
			this.min_drag_pixel_distance = 5;
			this.allow_editing = true;
			this.area_select_mode = EAreaSelectMode.Zoom;
			this.selection_colour = "#80808080";
			//AntiAliasing              = true;
			//FillMode                  = View3d.EFillMode.Solid;
			//CullMode                  = View3d.ECullMode.Back;
			//Orthographic              = false;
			//MinSelectionDistance      = 10f;
			this.reset_forward = v4.Neg(v4.ZAxis);
			this.reset_up = v4.clone(v4.YAxis);
			this.xaxis = new AxisOptions();
			this.yaxis = new AxisOptions();
		};
		function AxisOptions() {
			this.label_colour = 'black';
			this.tick_colour = 'black';
			this.label_font = "10pt tahoma";
			this.tick_font = "8pt tahoma";
			this.draw_tick_labels = true;
			this.draw_tick_marks = true;
			this.tick_length = 5;
			this.min_tick_size = 30;
			this.pixels_per_tick = 30;
			this.label_transform = { m00: 1, m01: 0, m10: 0, m11: 1, dx: 0, dy: 0 };
		};

		// Events
		this.OnChartMoved = new Util.MulticastDelegate();
		this.OnChartClicked = new Util.MulticastDelegate();
		this.OnRendering = new Util.MulticastDelegate();
		this.OnAutoRange = new Util.MulticastDelegate();

		// Chart title
		this.title = "";

		// The data series
		this.data = [];

		// Initialise Axis data
		this.xaxis = new Axis(this, this.options.xaxis, "");
		this.yaxis = new Axis(this, this.options.yaxis, "");
		this.xaxis.OnScroll.sub(function (s, a) {
			return This._RaiseChartMoved(EMove.XScroll);
		});
		this.xaxis.OnZoomed.sub(function (s, a) {
			return This._RaiseChartMoved(EMove.XZoomed);
		});
		this.yaxis.OnScroll.sub(function (s, a) {
			return This._RaiseChartMoved(EMove.YScroll);
		});
		this.yaxis.OnZoomed.sub(function (s, a) {
			return This._RaiseChartMoved(EMove.YZoomed);
		});

		// Layout data for the chart
		this.dimensions = this.ChartDimensions();

		window.requestAnimationFrame(function loop() {
			if (This.m_redraw_pending) This.Render();
			window.requestAnimationFrame(loop);
		});
	}

	/**
  * Get the renderer instance created by this chart
  */


	_createClass(Chart, [{
		key: "Render",


		/**
   * Render the chart
   * @param {Chart} chart The chart to be rendered
   */
		value: function Render() {
			// Clear the redraw pending flag
			this.m_redraw_pending = false;

			// Navigation moves the camera and axis ranges are derived from the camera position/view
			this.SetRangeFromCamera();

			// Calculate the chart dimensions
			this.dimensions = this.ChartDimensions();

			// Draw the chart frame
			this.RenderChartFrame(this.dimensions);

			// Draw the chart content
			this.RenderChartContent(this.dimensions);
		}

		/**
   * Draw the chart frame
   * @param {ChartDimensions} dims The chart size data returned from ChartDimensions
   */

	}, {
		key: "RenderChartFrame",
		value: function RenderChartFrame(dims) {
			// Drawing the frame can take a while. Cache the frame image so if the chart
			// is not moved, we can just blit the cached copy to the screen.
			var repaint = true;
			for (;;) {
				// Control size changed?
				if (this.m_cache.chart_frame == null || this.m_cache.chart_frame.width != dims.area.w || this.m_cache.chart_frame.height != dims.area.h) {
					// Create a double buffer
					this.m_cache.chart_frame = this.canvas2d.cloneNode(false);
					break;
				}

				// Axes zoomed/scrolled?
				if (this.xaxis.hash != this.m_cache.xaxis_hash) break;
				if (this.yaxis.hash != this.m_cache.yaxis_hash) break;

				// Rendering options changed?
				//if (m_this.XAxis.Options.ChangeCounter != m_xaxis_options)
				//	break;
				//if (m_this.YAxis.Options.ChangeCounter != m_yaxis_options)
				//	break;

				// Cached copy is still good
				repaint = false;
				break;
			}

			// Repaint the frame if the cached version is out of date
			if (repaint) {
				var bmp = this.m_cache.chart_frame.getContext("2d");
				var buffer = this.m_cache.chart_frame;
				var opts = this.options;
				var xaxis = this.xaxis;
				var yaxis = this.yaxis;

				// This is not enforced in the axis.Min/Max accessors because it's useful
				// to be able to change the min/max independently of each other, set them
				// to float max etc. It's only invalid to render a chart with a negative range
				//assert(xaxis.span > 0, "Negative x range");
				//assert(yaxis.span > 0, "Negative y range");

				bmp.save();
				bmp.clearRect(0, 0, buffer.width, buffer.height);

				// Clear to the background colour
				bmp.fillStyle = opts.bk_colour;
				bmp.fillRect(dims.area.l, dims.area.t, dims.chart_area.l - dims.area.l + 1, dims.area.b - dims.area.t + 1); // left band
				bmp.fillRect(dims.chart_area.r, dims.area.t, dims.area.r - dims.chart_area.r + 1, dims.area.b - dims.area.t + 1); // right band
				bmp.fillRect(dims.chart_area.l, dims.area.t, dims.chart_area.r - dims.chart_area.l + 1, dims.chart_area.t - dims.area.t + 1); // top band
				bmp.fillRect(dims.chart_area.l, dims.chart_area.b, dims.chart_area.r - dims.chart_area.l + 1, dims.area.b - dims.chart_area.b + 1); // bottom band

				// Draw the chart title and labels
				if (this.title && this.title.length > 0) {
					bmp.save();
					bmp.font = opts.title_font;
					bmp.fillStyle = opts.title_colour;

					var r = dims.title_size;
					var x = dims.area.l + (dims.area.w - r.width) * 0.5;
					var y = dims.area.t + (opts.margin.top + opts.title_padding + r.height);
					bmp.transform(1, 0, 0, 1, x, y);
					bmp.transform(opts.title_transform.m00, opts.title_transform.m01, opts.title_transform.m10, opts.title_transform.m11, opts.title_transform.dx, opts.title_transform.dy);
					bmp.fillText(this.title, 0, 0);
					bmp.restore();
				}
				if (xaxis.label && xaxis.label.length > 0 && opts.show_axes) {
					bmp.save();
					bmp.font = opts.xaxis.label_font;
					bmp.fillStyle = opts.xaxis.label_colour;

					var _r = dims.xlabel_size;
					var _x = dims.area.l + (dims.area.w - _r.width) * 0.5;
					var _y = dims.area.b - opts.margin.bottom; // - r.height;
					bmp.transform(1, 0, 0, 1, _x, _y);
					bmp.transform(opts.xaxis.label_transform.m00, opts.xaxis.label_transform.m01, opts.xaxis.label_transform.m10, opts.xaxis.label_transform.m11, opts.xaxis.label_transform.dx, opts.xaxis.label_transform.dy);
					bmp.fillText(xaxis.label, 0, 0);
					bmp.restore();
				}
				if (yaxis.label && yaxis.label.length > 0 && opts.show_axes) {
					bmp.save();
					bmp.font = opts.yaxis.label_font;
					bmp.fillStyle = opts.yaxis.label_colour;

					var _r2 = dims.ylabel_size;
					var _x2 = dims.area.l + opts.margin.left + _r2.height;
					var _y2 = dims.area.t + (dims.area.h - _r2.width) * 0.5;
					bmp.transform(0, -1, 1, 0, _x2, _y2);
					bmp.transform(opts.yaxis.label_transform.m00, opts.yaxis.label_transform.m01, opts.yaxis.label_transform.m10, opts.yaxis.label_transform.m11, opts.yaxis.label_transform.dx, opts.yaxis.label_transform.dy);
					bmp.fillText(yaxis.label, 0, 0);
					bmp.restore();
				}

				// Tick marks and labels
				if (opts.show_axes) {
					if (opts.xaxis.draw_tick_labels || opts.xaxis.draw_tick_marks) {
						bmp.save();
						bmp.font = opts.xaxis.tick_font;
						bmp.textBaseline = 'top';
						bmp.fillStyle = opts.xaxis.tick_colour;
						bmp.textAlign = "center";
						bmp.lineWidth = 0;
						bmp.beginPath();

						var fh = Util.FontHeight(bmp, bmp.font);
						var Y = dims.chart_area.b + opts.xaxis.tick_length + 1;
						var gl = xaxis.GridLines(dims.chart_area.w, opts.xaxis.pixels_per_tick);
						for (var _x3 = gl.min; _x3 < gl.max; _x3 += gl.step) {
							var X = dims.chart_area.l + _x3 * dims.chart_area.w / xaxis.span;
							if (opts.xaxis.draw_tick_labels) {
								var s = xaxis.TickText(_x3 + xaxis.min, gl.step).split('\n');
								for (var i = 0; i != s.length; ++i) {
									bmp.fillText(s[i], X, Y + i * fh, dims.xtick_label_size.width);
								}
							}
							if (opts.xaxis.draw_tick_marks) {
								bmp.moveTo(X, dims.chart_area.b);
								bmp.lineTo(X, dims.chart_area.b + opts.xaxis.tick_length);
							}
						}

						bmp.imageSmoothingEnabled = false;
						bmp.stroke();
						bmp.restore();
					}
					if (opts.yaxis.draw_tick_labels || opts.yaxis.draw_tick_marks) {
						bmp.save();
						bmp.font = opts.yaxis.tick_font;
						bmp.fillStyle = opts.yaxis.tick_colour;
						bmp.textBaseline = 'top';
						bmp.textAlign = "right";
						bmp.lineWidth = 0;
						bmp.beginPath();

						var _fh = Util.FontHeight(bmp, bmp.font);
						var _X = dims.chart_area.l - opts.yaxis.tick_length - 1;
						var _gl = yaxis.GridLines(dims.chart_area.h, opts.yaxis.pixels_per_tick);
						for (var _y3 = _gl.min; _y3 < _gl.max; _y3 += _gl.step) {
							var _Y = dims.chart_area.b - _y3 * dims.chart_area.h / yaxis.span;
							if (opts.yaxis.draw_tick_labels) {
								var _s = yaxis.TickText(_y3 + yaxis.min, _gl.step).split('\n');
								for (var _i = 0; _i != _s.length; ++_i) {
									bmp.fillText(_s[_i], _X, _Y + _i * _fh - dims.ytick_label_size.height * 0.5, dims.ytick_label_size.width);
								}
							}
							if (opts.yaxis.draw_tick_marks) {
								bmp.moveTo(dims.chart_area.l - opts.yaxis.tick_length, _Y);
								bmp.lineTo(dims.chart_area.l, _Y);
							}
						}

						bmp.imageSmoothingEnabled = false;
						bmp.stroke();
						bmp.restore();
					}

					{
						// Axes
						bmp.save();
						bmp.fillStyle = opts.axis_colour;
						bmp.lineWidth = opts.axis_thickness;
						bmp.moveTo(dims.chart_area.l + 1, dims.chart_area.t);
						bmp.lineTo(dims.chart_area.l + 1, dims.chart_area.b);
						bmp.lineTo(dims.chart_area.r, dims.chart_area.b);
						bmp.stroke();
						bmp.restore();
					}

					// Record cache invalidating values
					this.m_cache.xaxis_hash = this.xaxis.hash;
					this.m_cache.yaxis_hash = this.yaxis.hash;
				}
				bmp.restore();
			}

			this.m_gfx.save();

			// Update the clipping region
			this.m_gfx.rect(dims.area.l, dims.area.t, dims.area.w, dims.area.h);
			this.m_gfx.rect(dims.chart_area.l, dims.chart_area.t, dims.chart_area.w, dims.chart_area.h);
			this.m_gfx.clip();

			// Blit the cached image to the screen
			this.m_gfx.imageSmoothingEnabled = false;
			this.m_gfx.drawImage(this.m_cache.chart_frame, dims.area.l, dims.area.t);
			this.m_gfx.restore();
		}

		/**
   * Draw the chart content
   * @param {ChartDimensions} dims The chart size data returned from ChartDimensions
   */

	}, {
		key: "RenderChartContent",
		value: function RenderChartContent(dims) {
			this.instances.length = 0;

			// Add axis graphics
			if (this.options.show_grid_lines) {
				// Position the grid lines so that they line up with the axis tick marks.
				// Grid lines are modelled from the bottom left corner
				var wh = this.camera.ViewArea();

				{
					// X axis
					var xlines = this.xaxis.GridLineGfx(this, dims);
					var gl = this.xaxis.GridLines(dims.chart_area.w, this.options.xaxis.pixels_per_tick);

					var pos = v4.make(wh[0] / 2 - gl.min, wh[1] / 2, this.options.grid_z_offset, 0);
					m4x4.MulMV(this.camera.c2w, pos, pos);
					v4.Sub(this.camera.focus_point, pos, pos);

					m4x4.clone(this.camera.c2w, xlines.o2w);
					m4x4.SetW(xlines.o2w, pos);
					this.instances.push(xlines);
				}
				{
					// Y axis
					var ylines = this.yaxis.GridLineGfx(this, dims);
					var _gl2 = this.yaxis.GridLines(dims.chart_area.h, this.options.yaxis.pixels_per_tick);

					var _pos = v4.make(wh[0] / 2, wh[1] / 2 - _gl2.min, this.options.grid_z_offset, 0);
					m4x4.MulMV(this.camera.c2w, _pos, _pos);
					v4.Sub(this.camera.focus_point, _pos, _pos);

					m4x4.clone(this.camera.c2w, ylines.o2w);
					m4x4.SetW(ylines.o2w, _pos);
					this.instances.push(ylines);
				}
			}

			// Add chart elements

			// Add user graphics
			this.OnRendering.invoke(this, {});

			// Ensure the viewport matches the chart context area
			// Webgl viewports have 0,0 in the lower left, but 'dims.chart_area' has 0,0 at top left.
			this.m_rdr.viewport(dims.chart_area.l - dims.area.l, dims.area.b - dims.chart_area.b, dims.chart_area.w, dims.chart_area.h);

			// Render the chart
			Rdr.Render(this.m_rdr, this.instances, this.camera, this.light);
		}

		/**
   * Invalidate cached data before a Render
   */

	}, {
		key: "Invalidate",
		value: function Invalidate() {
			this.m_cache.invalidate();
			if (this.m_redraw_pending) return;

			// Set a flag to indicate redraw pending
			this.m_redraw_pending = true;

			//	let This = this;
			//	setTimeout(function() { This.Render(); }, 10);
		}

		/**
   * Set the axis range based on the position of the camera and the field of view.
   */

	}, {
		key: "SetRangeFromCamera",
		value: function SetRangeFromCamera() {
			// The grid is always parallel to the image plane of the camera.
			// The camera forward vector points at the centre of the grid.
			var camera = this.camera;

			// Project the camera to world vector into camera space to determine the centre of the X/Y axis. 
			var w2c = camera.w2c;
			var x = -w2c[12];
			var y = -w2c[13];

			// The span of the X/Y axis is determined by the FoV and the focus point distance.
			var wh = camera.ViewArea();

			// Set the axes range
			var xmin = x - wh[0] * 0.5;
			var xmax = x + wh[0] * 0.5;
			var ymin = y - wh[1] * 0.5;
			var ymax = y + wh[1] * 0.5;
			if (xmin < xmax) this.xaxis.Set(xmin, xmax);
			if (ymin < ymax) this.yaxis.Set(ymin, ymax);
		}

		/**
   * Position the camera based on the axis range.
   */

	}, {
		key: "SetCameraFromRange",
		value: function SetCameraFromRange() {
			// Set the aspect ratio from the axis range and scene bounds
			this.camera.aspect = this.xaxis.span / this.yaxis.span;

			// Find the world space position of the new focus point.
			// Move the focus point within the focus plane (parallel to the camera XY plane)
			// and adjust the focus distance so that the view has a width equal to XAxis.Span.
			// The camera aspect ratio should ensure the YAxis is correct.
			var c2w = this.camera.c2w;
			var focus = v4.AddN(v4.MulS(m4x4.GetX(c2w), this.xaxis.centre), v4.MulS(m4x4.GetY(c2w), this.yaxis.centre), v4.MulS(m4x4.GetZ(c2w), v4.Dot(v4.Sub(this.camera.focus_point, v4.Origin), m4x4.GetZ(c2w))));

			// Move the camera in the camera Z axis direction so that the width at the focus dist
			// matches the XAxis range. Tan(FovX/2) = (XAxis.Span/2)/d
			var focus_dist = this.xaxis.span / (2.0 * Math.tan(this.camera.fovX / 2.0));
			var pos = v4.SetW1(v4.Add(focus, v4.MulS(m4x4.GetZ(c2w), focus_dist)));

			// Set the new camera position and focus distance
			this.camera.focus_dist = focus_dist;
			m4x4.SetW(c2w, pos);
			this.camera.Commit();
		}

		/**
   * Calculate the areas for the chart, and axes
   * @returns {ChartDimensions}
   */

	}, {
		key: "ChartDimensions",
		value: function ChartDimensions() {
			var out = new _ChartDimensions();
			var rect = Rect.make(0, 0, this.canvas2d.width, this.canvas2d.height);
			var r = {};

			// Save the total chart area
			out.area = Rect.clone(rect);

			// Add margins
			rect.x += this.options.margin.left;
			rect.y += this.options.margin.top;
			rect.w -= this.options.margin.left + this.options.margin.right;
			rect.h -= this.options.margin.top + this.options.margin.bottom;

			// Add space for the title
			if (this.title && this.title.length > 0) {
				r = Util.MeasureString(this.m_gfx, this.title, this.options.title_font);
				rect.y += r.height + 2 * this.options.title_padding;
				rect.h -= r.height + 2 * this.options.title_padding;
				out.title_size = r;
			}

			// Add space for the axes
			if (this.options.show_axes) {
				// Add space for tick marks
				if (this.options.yaxis.draw_tick_marks) {
					rect.x += this.options.yaxis.tick_length;
					rect.w -= this.options.yaxis.tick_length;
				}
				if (this.options.xaxis.draw_tick_marks) {
					rect.h -= this.options.xaxis.tick_length;
				}

				// Add space for the axis labels
				if (this.xaxis.label && this.xaxis.label.length > 0) {
					r = Util.MeasureString(this.m_gfx, this.xaxis.label, this.options.xaxis.label_font);
					rect.h -= r.height;
					out.xlabel_size = r;
				}
				if (this.yaxis.label && this.yaxis.label.length > 0) {
					r = Util.MeasureString(this.m_gfx, this.yaxis.label, this.options.yaxis.label_font);
					rect.x += r.height; // will be rotated by 90deg
					rect.w -= r.height;
					out.ylabel_size = r;
				}

				// Add space for the tick labels
				// Note: If you're having trouble with the axis jumping around
				// check the 'TickText' callback is returning fixed length strings
				if (this.options.xaxis.draw_tick_labels) {
					// Measure the height of the tick text
					r = this.xaxis.MeasureTickText(this.m_gfx);
					rect.h -= r.height;
					out.xtick_label_size = r;
				}
				if (this.options.yaxis.draw_tick_labels) {
					// Measure the width of the tick text
					r = this.yaxis.MeasureTickText(this.m_gfx);
					rect.x += r.width;
					rect.w -= r.width;
					out.ytick_label_size = r;
				}
			}

			// Save the chart area
			if (rect.w < 0) rect.w = 0;
			if (rect.h < 0) rect.h = 0;
			out.chart_area = Rect.clone(rect);

			return out;
		}

		/**
   * Returns a point in chart space from a point in client space. Use to convert mouse (client-space) locations to chart coordinates
   * @param {[x,y]} client_point 
   * @returns {[x,y]}
   */

	}, {
		key: "ClientToChartPoint",
		value: function ClientToChartPoint(client_point) {
			return [this.xaxis.min + (client_point[0] - this.dimensions.chart_area.l) * this.xaxis.span / this.dimensions.chart_area.w, this.yaxis.min - (client_point[1] - this.dimensions.chart_area.b) * this.yaxis.span / this.dimensions.chart_area.h];
		}

		/**
   * Returns a size in chart space from a size in client space.
   * @param {[w,h]} client_size 
   * @returns {[w,h]}
   */

	}, {
		key: "ClientToChartSize",
		value: function ClientToChartSize(client_size) {
			return [client_size[0] * this.xaxis.span / this.dimensions.chart_area.w, client_size[1] * this.yaxis.span / this.dimensions.chart_area.h];
		}

		/**
   * Returns a rectangle in chart space from a rectangle in client space
   * @param {{x,y,w,h}} client_rect
   * @returns {{x,y,w,h}}
   */

	}, {
		key: "ClientToChartRect",
		value: function ClientToChartRect(client_rect) {
			var pt = ClientToChartPoint([client_rect.x, client_rect.y]);
			var sz = ClientToChartSize([client_rect.w, client_rect.h]);
			return { x: pt[0], y: pt[1], w: sz[0], h: sz[1] };
		}

		/**
   * Returns a point in client space from a point in chart space. Inverse of ClientToChartPoint
   * @param {[x,y]} chart_point 
   * @returns {[x,y]}
   */

	}, {
		key: "ChartToClientPoint",
		value: function ChartToClientPoint(chart_point) {
			return [this.dimensions.chart_area.l + (chart_point[0] - this.xaxis.min) * this.dimensions.chart_area.w / this.xaxis.span, this.dimensions.chart_area.b - (chart_point[1] - this.yaxis.min) * this.dimensions.chart_area.h / this.yaxis.span];
		}

		/**
   * Returns a size in client space from a size in chart space. Inverse of ClientToChartSize
   * @param {[w,h]} chart_point 
   * @returns {[w,h]}
   */

	}, {
		key: "ChartToClientSize",
		value: function ChartToClientSize(chart_size) {
			return [chart_size[0] * this.dimensions.chart_area.w / this.xaxis.span, chart_size[1] * this.dimensions.chart_area.h / this.yaxis.span];
		}

		/**
   * Returns a rectangle in client space from a rectangle in chart space. Inverse of ClientToChartRect
   * @param {{x,y,w,h}} chart_rect
   * @returns {{x,y,w,h}}
   */

	}, {
		key: "ChartToClientRect",
		value: function ChartToClientRect(chart_rect) {
			var pt = ChartToClientPoint([chart_rect.x, client_rect.y]);
			var sz = ChartToClientSize([chart_rect.w, client_rect.h]);
			return { x: pt[0], y: pt[1], w: sz[0], h: sz[1] };
		}

		/**
   * Return a point in camera space from a point in chart space (Z = focus plane)
   * @param {[x,y]} chart_point
   * @returns {v4}
   */

	}, {
		key: "ChartToCamera",
		value: function ChartToCamera(chart_point) {
			// Remove the camera to origin offset
			var origin_cs = m4x4.GetW(this.camera.w2c);
			var pt = [chart_point[0] + origin_cs[0], chart_point[1] + origin_cs[1]];
			return v4.make(pt[0], pt[1], -this.camera.focus_dist, 1);
		}

		/**
   * Return a point in chart space from a point in camera space
   * @param {v4} camera_point
   * @returns {[x,y]}
   */

	}, {
		key: "CameraToChart",
		value: function CameraToChart(camera_point) {
			if (camera_point.z <= 0) throw new Error("Invalidate camera point. Points must be in front of the camera");

			// Project the camera space point onto the focus plane
			var proj = -this.camera.focus_dist / camera_point[2];
			var pt = [camera_point[0] * proj, camera_point[1] * proj];

			// Add the offset of the camera from the origin
			var origin_cs = m4x4.GetW(this.camera.w2c);
			return [pt[0] - origin_cs[0], pt[1] - origin_cs[1]];
		}

		/**
   * Get the scale and translation transform from chart space to client space.
   * e.g. chart2client * Point(x_min, y_min) = plot_area.BottomLeft()
   *      chart2client * Point(x_max, y_max) = plot_area.TopRight()
   * @param {{l,t,r,b,w,h}} plot_area (optional) The area of the chart content (defaults to the current content area)
   * @returns {m4x4}
   */

	}, {
		key: "ChartToClientSpace",
		value: function ChartToClientSpace(plot_area) {
			plot_area = plot_area || this.dimensions.chart_area;

			var scale_x = +(plot_area.w / this.xaxis.span);
			var scale_y = -(plot_area.h / this.yaxis.span);
			var offset_x = +(plot_area.l - this.xaxis.min * scale_x);
			var offset_y = +(plot_area.b - this.yaxis.min * scale_y);

			// C = chart, c = client
			var C2c = m4x4.make(v4.make(scale_x, 0, 0, 0), v4.make(0, scale_y, 0, 0), v4.make(0, 0, 1, 0), v4.make(offset_x, offset_y, 0, 1));

			return C2c;
		}

		/**
   * Get the scale and translation transform from client space to chart space.
   * e.g. client2chart * plot_area.BottomLeft() = Point(x_min, y_min)
   *      client2chart * plot_area.TopRight()   = Point(x_max, y_max)
   * @param {{l,t,r,b,w,h}} plot_area (optional) The area of the chart content (defaults to the current content area)
   * @returns {m4x4}
   */

	}, {
		key: "ClientToChartSpace",
		value: function ClientToChartSpace(plot_area) {
			plot_area = plot_area || this.dimensions.chart_area;

			var scale_x = +(this.xaxis.span / plot_area.w);
			var scale_y = -(this.yaxis.span / plot_area.h);
			var offset_x = +(this.xaxis.min - plot_area.l * scale_x);
			var offset_y = +(this.yaxis.min - plot_area.b * scale_y);

			// C = chart, c = client
			var c2C = m4x4.make(v4.make(scale_x, 0, 0, 0), v4.make(0, scale_y, 0, 0), v4.make(0, 0, 1, 0), v4.make(offset_x, offset_y, 0, 1));

			return c2C;
		}

		/**
   * Convert a point between client space and normalised screen space
   * @param {[x,y]} client_point
   * @returns {[x,y]}
   */

	}, {
		key: "ClientToNSSPoint",
		value: function ClientToNSSPoint(client_point) {
			return [(client_point[0] - this.dimensions.chart_area.l) / this.dimensions.chart_area.w * 2 - 1, (this.dimensions.chart_area.b - client_point[1]) / this.dimensions.chart_area.h * 2 - 1];
		}

		/**
   * Convert a size between client space and normalised screen space
   * @param {[w,h]} client_size
   * @returns {[w,h]}
   */

	}, {
		key: "ClientToNSSSize",
		value: function ClientToNSSSize(client_size) {
			return [client_size[0] / this.dimensions.chart_area.w * 2, client_size[1] / this.dimensions.chart_area.h * 2];
		}

		/**
   * Convert a rectangle between client space and normalised screen space
   * @param {{x,y,w,h}} client_rect
   * @returns {{x,y,w,h}}
   */

	}, {
		key: "ClientToNSSRect",
		value: function ClientToNSSRect(client_rect) {
			var pt = ClientToNSSPoint([client_rect.x, client_rect.y]);
			var sz = ClientToNSSSize([client_rect.w, client_rect.h]);
			return { x: pt[0], y: pt[1], w: sz[0], h: sz[1] };
		}

		/**
   * Convert a normalised screen space point to client space
   * @param {[x,y]} nss_point
   * @returns {[x,y]}
   */

	}, {
		key: "NSSToClientPoint",
		value: function NSSToClientPoint(nss_point) {
			return [this.dimensions.chart_area.l + 0.5 * (nss_point[0] + 1) * this.dimensions.chart_area.w, this.dimensions.chart_area.b - 0.5 * (nss_point[1] + 1) * this.dimensions.chart_area.h];
		}

		/**
   * Convert a normalised screen space size to client space
   * @param {[w,h]} nss_size
   * @returns {[w,h]}
   */

	}, {
		key: "NSSToClientSize",
		value: function NSSToClientSize(nss_size) {
			return [0.5 * nss_size[0] * this.dimensions.chart_area.w, 0.5 * nss_size[1] * this.dimensions.chart_area.h];
		}

		/**
   * Convert a normalised screen space rectangle to client space
   * @param {{x,y,w,h}}
   * @return {{x,y,w,h}}
   */

	}, {
		key: "NSSToClient",
		value: function NSSToClient(nss_rect) {
			var pt = NSSToClientPoint([nss_rect.x, nss_rect.y]);
			var sz = NSSToClientSize([nss_rect.w, nss_rect.h]);
			return { x: pt[0], y: pt[1], w: sz[0], h: sz[1] };
		}

		/**
   * Perform a hit test on the chart
   * @param {[x,y]} client_point The client space point to hit test
   * @param {{shift, alt, ctrl}} modifier_keys The state of shift, alt, ctrl keys
   * @param {function} pred A predicate function for filtering hit objects
   * @returns {HitTestResult}
   */

	}, {
		key: "HitTestCS",
		value: function HitTestCS(client_point, modifier_keys, pred) {
			// Determine the hit zone of the control
			var zone = EZone.None;
			if (Rect.Contains(this.dimensions.chart_area, client_point)) zone |= EZone.Chart;
			if (Rect.Contains(this.dimensions.xaxis_area, client_point)) zone |= EZone.XAxis;
			if (Rect.Contains(this.dimensions.yaxis_area, client_point)) zone |= EZone.YAxis;
			if (Rect.Contains(this.dimensions.title_area, client_point)) zone |= EZone.Title;

			// The hit test point in chart space
			var chart_point = this.ClientToChartPoint(client_point);

			// Find elements that overlap 'client_point'
			var hits = [];
			/* todo
   	if (zone & EZone.Chart)
   	{
   		let elements = pred != null ? Elements.Where(pred) : Elements;
   		foreach (let elem in elements)
   		{
   			let hit = elem.HitTest(chart_point, client_point, modifier_keys, Scene.Camera);
   			if (hit != null)
   				hits.Add(hit);
   		}
   				// Sort the results by z order
   		hits.Sort((l,r) => -l.Element.PositionZ.CompareTo(r.Element.PositionZ));
   	}
   */

			return new HitTestResult(zone, client_point, chart_point, modifier_keys, hits, this.camera);
		}

		/**
   * Find the default range, then reset to the default range
   */

	}, {
		key: "AutoRange",
		value: function AutoRange() {
			// Allow the auto range to be handled by event
			var args = { who: "all", view_bbox: BBox.create(), dims: this.dimensions, handled: false };
			this.OnAutoRange.invoke(this, args);

			// For now, auto ranging must be done by user code since the chart
			// doesn't know about what's in the scene.
			if (!args.handled) return;

			// If user code handled the auto ranging, sanity check
			if (args.handled && (args.view_bbox == null || !args.view_bbox.is_valid || v4.LengthSq(args.view_bbox.radius) == 0)) throw new Error("Caller provided view bounding box is invalid: " + args.view_bbox);

			// Get the bounding box to fit into the view
			var bbox = args.view_bbox; //args.handled ? args.view_bbox : Window.SceneBounds(who, except:new[] { ChartTools.Id });

			// Position the camera to view the bounding box
			this.camera.ViewBBox(bbox, this.options.reset_forward, this.options.reset_up, 0, this.lock_aspect, true);

			// Set the axis range from the camera position
			this.SetRangeFromCamera();
			this.Invalidate();
		}

		/**
   * Raise the OnChartMoved event on the next message.
   * This method is designed to be called several times within one thread message.
   * @param {EMove} move_type The movement type that occurred
   */

	}, {
		key: "_RaiseChartMoved",
		value: function _RaiseChartMoved(move_type) {
			var _this = this;

			if (move_type & EMove.Zoomed) this.xaxis._InvalidateGfx();
			if (move_type & EMove.Zoomed) this.yaxis._InvalidateGfx();

			if (!this.m_chart_moved_args) {
				// Trigger a redraw of the chart
				this.Invalidate();

				// Notify of the chart moving
				this.m_chart_moved_args = { move_type: EMove.None };
				Util.BeginInvoke(function () {
					_this.OnChartMoved.invoke(_this, _this.m_chart_moved_args);
					_this.m_chart_moved_args = null;
				});
			}

			// Accumulate the moved flags until 'OnChartMoved' has been called.
			this.m_chart_moved_args.move_type |= move_type;
		}
	}, {
		key: "rdr",
		get: function get() {
			return this.m_rdr;
		}

		/**
   * Get/Set whether the aspect ratio is locked to the current value
   * @returns {boolean}
   */

	}, {
		key: "lock_aspect",
		get: function get() {
			return this.options.lock_aspect != null;
		},
		set: function set(value) {
			if (this.lock_aspect == value) return;
			if (value) this.options.lock_aspect = this.xaxis.span * this.dimensions.h / (this.yaxis.span * this.dimensions.w);else this.options.lock_aspect = null;
		}
	}]);

	return Chart;
}();

/**
 * Create a chart in the given canvas element
 * @param {HTMLCanvasElement} canvas the HTML element to turn into a chart
 * @returns {Chart}
 */


function Create(canvas) {
	return new Chart(canvas);
}

/**
 * Create a chart data series instance.
 * Data can be added/removed from a chart via its 'data' array
 * @param {ChartDataSeries} title 
 */
function CreateDataSeries(title) {
	this.title = title;
	this.options = {};
	this.data = [];
}

/**
 * Chart layout data
 */

var _ChartDimensions = function () {
	function _ChartDimensions() {
		_classCallCheck(this, _ChartDimensions);

		this.area = Rect.create();
		this.chart_area = Rect.create();
		this.title_size = [0, 0];
		this.xlabel_size = [0, 0];
		this.ylabel_size = [0, 0];
		this.xtick_label_size = [0, 0];
		this.ytick_label_size = [0, 0];
	}

	_createClass(_ChartDimensions, [{
		key: "xaxis_area",
		get: function get() {
			return Rect.ltrb(this.chart_area.l, this.chart_area.b, this.chart_area.r, this.area.b);
		}
	}, {
		key: "yaxis_area",
		get: function get() {
			return Rect.ltrb(0, this.chart_area.t, this.chart_area.l, this.chart_area.b);
		}
	}, {
		key: "title_area",
		get: function get() {
			return Rect.ltrb(this.chart_area.l, 0, this.chart_area.r, this.chart_area.t);
		}
	}]);

	return _ChartDimensions;
}();

/**
 * Hit test results structure
 */


exports.ChartDimensions = _ChartDimensions;

var HitTestResult = exports.HitTestResult = function HitTestResult(zone, client_point, chart_point, modifier_keys, hits, camera) {
	_classCallCheck(this, HitTestResult);

	this.zone = zone;
	this.client_point = client_point;
	this.chart_point = chart_point;
	this.modifier_keys = modifier_keys;
	this.hits = hits;
	this.camera = camera;
};

/**
 * Chart axis data
 */


var Axis = function () {
	function Axis(chart, options, label) {
		_classCallCheck(this, Axis);

		this.min = 0;
		this.max = 1;
		this.chart = chart;
		this.label = label;
		this.options = options;
		this.m_geom_lines = null;
		this.m_allow_scroll = true;
		this.m_allow_zoom = true;
		this.m_lock_range = false;
		this.TickText = this.TickTextDefault;
		this.MeasureTickText = this.MeasureTickTextDefault;

		this.OnScroll = new Util.MulticastDelegate();
		this.OnZoomed = new Util.MulticastDelegate();
	}

	/**
  * Get/Set the range of the axis
  * Setting the span leaves the centre unchanged
  */


	_createClass(Axis, [{
		key: "Set",


		/**
   * Set the range without risk of an assert if 'min' is greater than 'Max' or visa versa.
   */
		value: function Set(min, max) {
			if (min >= max) throw new Error("Range must be positive and non-zero");
			var zoomed = !Maths.FEql(max - min, this.max - this.min);
			var scroll = !Maths.FEql((max + min) * 0.5, (this.max + this.min) * 0.5);

			this.min = min;
			this.max = max;

			if (zoomed) {
				this.OnZoomed.invoke(this, {});
			}
			if (scroll) {
				this.OnScroll.invoke(this, {});
			}
		}

		/**
   * Scroll the axis by 'delta'
   * @param {Number} delta
   */

	}, {
		key: "Shift",
		value: function Shift(delta) {
			if (!this.allow_scroll) return;
			this.centre += delta;
		}

		/**
   * Default value to text conversion
   * @param {Number} x The tick value on the axis
   * @param {Number} step The current axis step size, from one tick to the next
   * @returns {string}
   */

	}, {
		key: "TickTextDefault",
		value: function TickTextDefault(x, step) {
			// This solves the rounding problem for values near zero when the axis span could be anything
			if (Maths.FEql(x / this.span, 0.0)) return "0";

			// Use 5dp of precision
			var text = x.toPrecision(5);
			if (text.indexOf('.') != -1) {
				text = Util.TrimRight(text, ['0']);
				if (text.slice(-1) == '.') text += '0';
			}
			return text;
		}

		/**
   * Measure the tick text width
   * @param {Canvas2dContext} gfx 
   * @returns {{width, height}}
   */

	}, {
		key: "MeasureTickTextDefault",
		value: function MeasureTickTextDefault(gfx) {
			// Can't use 'GridLines' here because creates an infinite recursion.
			// Using TickText(Min/Max, 0.0) causes the axes to jump around.
			// The best option is to use one fixed length string.
			return Util.MeasureString(gfx, "-9.99999", this.options.tick_font);
		}

		/**
   * Return the position of the grid lines for this axis
   * @param {Number} axis_length The size of the chart area in the direction of this axis (i.e. dims.chart_area.w or dims.chart_area.h)
   * @param {Number} pixels_per_tick The ideal number of pixels between each tick mark
   * @returns {{min,max,step}}
   */

	}, {
		key: "GridLines",
		value: function GridLines(axis_length, pixels_per_tick) {
			var out = {};
			var max_ticks = axis_length / pixels_per_tick;

			// Choose step sizes
			var step_base = Math.pow(10.0, Math.floor(Math.log10(this.span))),
			    step = step_base;
			var quantisations = [0.05, 0.1, 0.2, 0.25, 0.5, 1.0, 2.0, 5.0, 10.0, 20.0, 50.0];
			for (var i = 0; i != quantisations.length; ++i) {
				var s = quantisations[i];
				if (s * this.span > max_ticks * step_base) continue;
				step = step_base / s;
			}

			out.step = step;
			out.min = this.min - Maths.IEEERemainder(this.min, step) - this.min;
			out.max = this.span * 1.0001;
			if (out.min < 0.0) out.min += out.step;

			// Protect against increments smaller than can be represented
			if (out.min + out.step == out.min) out.step = (out.max - out.min) * 0.01;

			// Protect against too many ticks along the axis
			if (out.max - out.min > out.step * 100) out.step = (out.max - out.min) * 0.01;

			return out;
		}

		/**
   * Return a renderer instance for the grid lines for this axis
   * @param {Chart} chart
   * @param {ChartDimensions} dims
   * @returns {Rdr.Instance}
   */

	}, {
		key: "GridLineGfx",
		value: function GridLineGfx(chart, dims) {
			// If there is no cached grid line graphics, generate them
			if (this.m_geom_lines == null) {
				// Create a model for the grid lines
				// Need to allow for one step in either direction because we only create the grid lines
				// model when scaling and we can translate by a max of one step in either direction.
				var axis_length = this === chart.xaxis ? dims.chart_area.w : this === chart.yaxis ? dims.chart_area.h : null;
				var gl = this.GridLines(axis_length, this.options.pixels_per_tick);
				var num_lines = Math.floor(2 + (gl.max - gl.min) / gl.step);

				// Create the grid lines at the origin, they get positioned as the camera moves
				var verts = new Float32Array(num_lines * 2 * 3);
				var indices = new Uint16Array(num_lines * 2);
				var nuggets = [];
				var name = "";
				var v = 0;
				var i = 0;

				// Grid verts
				if (this === chart.xaxis) {
					name = "xaxis_grid";
					var x = 0,
					    y0 = 0,
					    y1 = chart.yaxis.span;
					for (var l = 0; l != num_lines; ++l) {
						Util.CopyTo(verts, v, x, y0, 0);v += 3;
						Util.CopyTo(verts, v, x, y1, 0);v += 3;
						x += gl.step;
					}
				}
				if (this === chart.yaxis) {
					name = "yaxis_grid";
					var y = 0,
					    x0 = 0,
					    x1 = chart.xaxis.span;
					for (var _l = 0; _l != num_lines; ++_l) {
						Util.CopyTo(verts, v, x0, y, 0);v += 3;
						Util.CopyTo(verts, v, x1, y, 0);v += 3;
						y += gl.step;
					}
				}

				// Grid indices
				for (var _l2 = 0; _l2 != num_lines; ++_l2) {
					indices[i] = i++;
					indices[i] = i++;
				}

				// Grid nugget
				nuggets.push({ topo: chart.m_rdr.LINES, shader: chart.m_rdr.shader_programs["forward"], tint: Util.ColourToV4(chart.options.grid_colour) });

				// Create the model
				var model = Rdr.Model.CreateRaw(chart.m_rdr, verts, null, null, null, indices, nuggets);

				// Create the instance
				this.m_geom_lines = Rdr.Instance.Create(name, model, m4x4.create(), Rdr.EFlags.SceneBoundsExclude | Rdr.EFlags.NoZWrite);
			}

			// Return the graphics model
			return this.m_geom_lines;
		}

		/**
   * Invalidate the graphics, causing them to be recreated next time they're needed
   */

	}, {
		key: "_InvalidateGfx",
		value: function _InvalidateGfx() {
			this.m_geom_lines = null;
		}
	}, {
		key: "span",
		get: function get() {
			return this.max - this.min;
		},
		set: function set(value) {
			if (this.span == value) return;
			if (value <= 0) throw new Error("Invalid axis span: " + value);
			this.Set(this.centre - 0.5 * value, this.centre + 0.5 * value);
		}

		/**
   * Get/Set the centre position of the axis
   */

	}, {
		key: "centre",
		get: function get() {
			return (this.max + this.min) * 0.5;
		},
		set: function set(value) {
			if (this.centre == value) return;
			this.Set(this.min + value - this.centre, this.max + value - this.centre);
		}

		/**
   * Get/Set whether scrolling on this axis is allowed
   */

	}, {
		key: "allow_scroll",
		get: function get() {
			return this.m_allow_scroll && !this.lock_range;
		},
		set: function set(value) {
			this.m_allow_scroll = value;
		}

		/**
   * Get/Set whether zooming on this axis is allowed
   */

	}, {
		key: "allow_zoom",
		get: function get() {
			return this.m_allow_zoom && !this.lock_range;
		},
		set: function set(value) {
			this.m_allow_zoom = value;
		}

		/**
   * Get/Set whether the range can be changed by user input
   */

	}, {
		key: "lock_range",
		get: function get() {
			return this.m_lock_range;
		},
		set: function set(value) {
			this.m_lock_range = value;
		}

		/**
   * Return a string description of the axis state. Used to detect changes
   * @returns {string}
   */

	}, {
		key: "hash",
		get: function get() {
			return "" + this.min + this.max + this.label;
		}
	}]);

	return Axis;
}();

/**
 * Chart naviation
 */


var Navigation = function () {
	function Navigation(chart) {
		_classCallCheck(this, Navigation);

		this.chart = chart;
		this.active = null;
		this.pending = [];

		// Hook up mouse event handlers
		chart.canvas2d.onpointerdown = function (ev) {
			chart.navigation.OnMouseDown(ev);
		};
		chart.canvas2d.onpointermove = function (ev) {
			chart.navigation.OnMouseMove(ev);
		};
		chart.canvas2d.onpointerup = function (ev) {
			chart.navigation.OnMouseUp(ev);
		};
		chart.canvas2d.onmousewheel = function (ev) {
			chart.navigation.OnMouseWheel(ev);
		};
		chart.canvas2d.oncontextmenu = function (ev) {
			ev.preventDefault();
		};
	}

	/**
  * Start a mouse operation associated with the given mouse button index
  * @param {MouseButton} btn 
  */


	_createClass(Navigation, [{
		key: "BeginOp",
		value: function BeginOp(btn) {
			this.active = this.pending[btn];
			this.pending[btn] = null;
		}

		/**
   * End a mouse operation for the given mouse button index
   * @param {MouseButton} btn 
   */

	}, {
		key: "EndOp",
		value: function EndOp(btn) {
			this.active = null;
			if (this.pending[btn] && !this.pending[btn].start_on_mouse_down) this.BeginOp(btn);
		}

		/**
   * Handle mouse down on the chart
   * @param {MouseEventData} ev 
   */

	}, {
		key: "OnMouseDown",
		value: function OnMouseDown(ev) {
			// If a mouse op is already active, ignore mouse down
			if (this.active != null) return;

			// Look for the mouse op to perform
			if (this.pending[ev.button] == null) {
				switch (ev.button) {
					default:
						return;
					case EButton.Left:
						this.pending[ev.button] = new MouseOpDefaultLButton(this.chart);break;
					//case MouseButtons.Middle: MouseOperations.SetPending(e.Button, new MouseOpDefaultMButton(this)); break;
					case EButton.Right:
						this.pending[ev.button] = new MouseOpDefaultRButton(this.chart);break;
				}
			}

			// Start the next mouse op
			this.BeginOp(ev.button);

			// Get the mouse op, save mouse location and hit test data, then call op.MouseDown()
			var op = this.active;
			if (op != null && !op.cancelled) {
				op.btn_down = true;
				op.grab_client = [ev.offsetX, ev.offsetY];
				op.grab_chart = this.chart.ClientToChartPoint(op.grab_client);
				op.hit_result = this.chart.HitTestCS(op.grab_client, { shift: ev.shiftKey, alt: ev.altKey, ctrl: ev.ctrlKey }, null);
				op.MouseDown(ev);
				this.chart.canvas2d.setPointerCapture(ev.pointerId);
			}
		}

		/**
   * Handle mouse move on the chart
   * @param {MouseEvent} ev 
   */

	}, {
		key: "OnMouseMove",
		value: function OnMouseMove(ev) {
			// Look for the mouse op to perform
			var op = this.active;
			if (op != null) {
				if (!op.cancelled) op.MouseMove(ev);
			}
			// Otherwise, provide mouse hover detection
			else {
					var client_point = [ev.offsetX, ev.offsetY];
					var hit_result = this.chart.HitTestCS(client_point, { shift: ev.shiftKey, alt: ev.altKey, ctrl: ev.ctrlKey }, null);
					/*
     	let hovered = hit_result.hits.Select(x => x.Element).ToHashSet();
     			// Remove elements that are no longer hovered
     	// and remove existing hovered's from the set.
     	for (int i = Hovered.Count; i-- != 0;)
     	{
     		if (hovered.Contains(Hovered[i]))
     			hovered.Remove(Hovered[i]);
     		else
     			Hovered.RemoveAt(i);
     	}
     	
     	// Add elements that are now hovered
     	Hovered.AddRange(hovered);
     */
				}
		}

		/**
   * Handle mouse up on the chart
   * @param {MouseEvent} ev 
   */

	}, {
		key: "OnMouseUp",
		value: function OnMouseUp(ev) {
			// Only release the mouse when all buttons are up
			//if (ev.button == EButton. MouseButtons.None)
			this.chart.canvas2d.releasePointerCapture(ev.pointerId);

			// Look for the mouse op to perform
			var op = this.active;
			if (op != null && !op.cancelled) op.MouseUp(ev);

			this.EndOp(ev.button);
		}

		/**
   * Handle mouse wheel changes on the chart
   * @param {MouseWheelEvent} ev 
   */

	}, {
		key: "OnMouseWheel",
		value: function OnMouseWheel(ev) {
			var client_pt = [ev.offsetX, ev.offsetY];
			var chart_pt = this.chart.ClientToChartPoint(client_pt);
			var dims = this.chart.dimensions;
			var perp_z = this.chart.options.perpendicular_z_translation != ev.altKey;
			if (Rect.Contains(dims.chart_area, client_pt)) {
				// If there is a mouse op in progress, ignore the wheel
				var op = this.active;
				if (op == null || op.cancelled) {
					// Set a scaling factor from the mouse wheel clicks
					var scale = 1.0 / 120.0;
					var delta = Maths.Clamp(ev.wheelDelta * scale, -100, 100);

					// Convert 'client_pt' to normalised camera space
					var nss_point = Rect.NormalisePoint(dims.chart_area, client_pt, +1, -1);

					// Translate the camera along a ray through 'point'
					this.chart.camera.MouseControlZ(nss_point, delta, !perp_z);
					this.chart.Invalidate();
				}
			} else if (this.chart.options.show_axes) {
				var _scale = 0.001;
				var _delta = Maths.Clamp(ev.wheelDelta * _scale, -0.999, 0.999);

				var xaxis = this.chart.xaxis;
				var yaxis = this.chart.yaxis;
				var chg = false;

				// Change the aspect ratio by zooming on the XAxis
				if (Rect.Contains(dims.xaxis_area, client_pt) && !xaxis.lock_range) {
					if (ev.altKey && xaxis.allow_scroll) {
						xaxis.Shift(xaxis.span * _delta);
						chg = true;
					}
					if (!ev.altKey && xaxis.allow_zoom) {
						var x = perp_z ? xaxis.centre : chart_pt[0];
						var left = (xaxis.min - x) * (1 - _delta);
						var rite = (xaxis.max - x) * (1 - _delta);
						xaxis.Set(chart_pt[0] + left, chart_pt[0] + rite);
						if (this.chart.options.lock_aspect != null) yaxis.span *= 1 - _delta;

						chg = true;
					}
				}

				// Check the aspect ratio by zooming on the YAxis
				if (Rect.Contains(dims.yaxis_area, client_pt) && !yaxis.lock_range) {
					if (ev.altKey && yaxis.allow_scroll) {
						yaxis.Shift(yaxis.span * _delta);
						chg = true;
					}
					if (!ev.altKey && yaxis.allow_zoom) {
						var y = perp_z ? yaxis.centre : chart_pt[1];
						var _left = (yaxis.min - y) * (1 - _delta);
						var _rite = (yaxis.max - y) * (1 - _delta);
						yaxis.Set(chart_pt[1] + _left, chart_pt[1] + _rite);
						if (this.chart.options.lock_aspect != null) xaxis.span *= 1 - _delta;

						chg = true;
					}
				}

				// Set the camera position from the Axis ranges
				if (chg) {
					this.chart.SetCameraFromRange();
					this.chart.Invalidate();
				}
			}
		}

		/*
  	protected override void OnKeyDown(KeyEventArgs e)
  	{
  		SetCursor();
  
  		let op = MouseOperations.Active;
  		if (op != null && !e.Handled)
  			op.OnKeyDown(e);
  
  		// Allow derived classes to handle the key
  		base.OnKeyDown(e);
  
  		// If the current mouse operation doesn't use the key,
  		// see if it's a default keyboard shortcut.
  		if (!e.Handled && DefaultKeyboardShortcuts)
  			TranslateKey(e);
  
  	}
  	protected override void OnKeyUp(KeyEventArgs e)
  	{
  		SetCursor();
  
  		let op = MouseOperations.Active;
  		if (op != null && !e.Handled)
  			op.OnKeyUp(e);
  
  		base.OnKeyUp(e);
  	}
  */

	}]);

	return Navigation;
}();

;

/**
 * Mouse operation base class
 */

var MouseOp = function () {
	function MouseOp(chart) {
		_classCallCheck(this, MouseOp);

		this.chart = chart;
		this.start_on_mouse_down = true;
		this.cancelled = false;
		this.btn_down = false;
		this.grab_client = null;
		this.grab_chart = null;
		this.hit_result = null;
		this.m_is_click = true;
	}

	/**
  * Test a mouse location as being a click (as apposed to a drag)
  * @param {[x,y]} point The mouse pointer location (client space)
  * @returns {boolean} True if the distance between 'location' and mouse down should be treated as a click
  */


	_createClass(MouseOp, [{
		key: "IsClick",
		value: function IsClick(point) {
			if (!this.m_is_click) return false;
			var diff = [point[0] - this.grab_client[0], point[1] - this.grab_client[1]];
			return this.m_is_click = v2.LengthSq(diff) < Maths.Sqr(this.chart.options.min_drag_pixel_distance);
		}
	}]);

	return MouseOp;
}();

var MouseOpDefaultLButton = function (_MouseOp) {
	_inherits(MouseOpDefaultLButton, _MouseOp);

	function MouseOpDefaultLButton(chart) {
		_classCallCheck(this, MouseOpDefaultLButton);

		var _this2 = _possibleConstructorReturn(this, (MouseOpDefaultLButton.__proto__ || Object.getPrototypeOf(MouseOpDefaultLButton)).call(this, chart));

		_this2.hit_selected = null;
		_this2.m_selection_graphic_added = false;
		return _this2;
	}

	_createClass(MouseOpDefaultLButton, [{
		key: "MouseDown",
		value: function MouseDown(ev) {
			// Look for a selected object that the mouse operation starts on
			//this.m_hit_selected = this.hit_result.hits.find(function(x){ return x.Element.Selected; });

			//// Record the drag start positions for selected objects
			//foreach (let elem in m_chart.Selected)
			//	elem.DragStartPosition = elem.Position;

			// For 3D scenes, left mouse rotates if mouse down is within the chart bounds
			if (this.chart.options.navigation_mode == ENavMode.Scene3D && this.hit_result.zone == EZone.Chart && ev.button == EButton.Left) this.chart.camera.MouseControl(this.grab_client, Rdr.Camera.ENavOp.Rotate, true);

			//// Prevent events while dragging the elements around
			//m_suspend_scope = m_chart.SuspendChartChanged(raise_on_resume:true);
		}
	}, {
		key: "MouseMove",
		value: function MouseMove(ev) {
			var client_point = [ev.offsetX, ev.offsetY];

			// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
			if (this.IsClick(client_point) && !this.m_selection_graphic_added) return;

			var drag_selected = this.chart.options.allow_editing && this.hit_selected != null;
			if (drag_selected) {
				// If the drag operation started on a selected element then drag the
				// selected elements within the diagram.
				//let delta = v2.Sub(this.chart.ClientToChartPoint(client_point), this.grab_chart);
				//this.chart.DragSelected(delta, false);
			} else if (this.chart.options.navigation_mode == ENavMode.Chart2D) {
				if (this.chart.options.area_select_mode != EAreaSelectMode.Disabled) {
					// Otherwise change the selection area
					if (!this.m_selection_graphic_added) {
						this.chart.instances.push(this.chart.m_tools.area_select);
						this.m_selection_graphic_added = true;
					}

					// Position the selection graphic
					var area = Rect.bound(this.grab_chart, this.chart.ClientToChartPoint(client_point));
					m4x4.clone(m4x4.Scale(area.w, area.h, 1, v4.make(area.centre[0], area.centre[1], 0, 1), this.chart.m_tools.area_select.o2w));
				}
			} else if (this.chart.options.navigation_mode == ENavMode.Scene3D) {
				this.chart.camera.MouseControl(client_point, Rdr.Camera.ENavOp.Rotate, false);
			}

			// Render immediately, for smoother navigation
			this.chart.Render();
		}
	}, {
		key: "MouseUp",
		value: function MouseUp(ev) {
			var client_point = [ev.offsetX, ev.offsetY];

			// If this is a single click...
			if (this.IsClick(client_point)) {
				// Pass the click event out to users first
				var args = { hit_result: this.hit_result, handled: false };
				this.chart.OnChartClicked.invoke(this.chart, args);

				// If a selected element was hit on mouse down, see if it handles the click
				if (!args.handled && this.hit_selected != null) {}
				//this.hit_selected.Element.HandleClicked(args);


				// If no selected element was hit, try hovered elements
				if (!args.handled && this.hit_result.hits.length != 0) {
					for (var i = 0; i != this.hit_result.hits.length && !args.handled; ++i) {
						this.hit_result.hits[i].Element.HandleClicked(args);
					}
				}

				// If the click is still unhandled, use the click to try to select something (if within the chart)
				if (!args.handled && this.hit_result.zone & EZone.Chart) {
					var area = Rect.make(this.grab_chart[0], this.grab_chart[1], 0, 0);
					//this.chart.SelectElements(area, {shift: ev.shiftKey, alt: ev.altKey, ctrl: ev.ctrlKey});
				}
			}
			// Otherwise this is a drag action
			else {
					// If an element was selected, drag it around
					if (this.hit_selected != null && this.chart.options.allow_editing) {
						var delta = v2.Sub(this.chart.ClientToChartPoint(client_point), this.grab_chart);
						//this.chart.DragSelected(delta, true);
					} else if (this.chart.options.navigation_mode == ENavMode.Chart2D) {
						// Otherwise create an area selection if the click started within the chart
						if (this.hit_result.zone & EZone.Chart && this.chart.options.area_select_mode != EAreaSelectMode.Disabled) {
							var _area = Rect.bound(this.grab_chart, this.chart.ClientToChartPoint(client_point));
							//m_chart.OnChartAreaSelect(new ChartAreaSelectEventArgs(selection_area));
						}
					} else if (this.chart.options.navigation_mode == ENavMode.Scene3D) {
						// For 3D scenes, left mouse rotates
						this.chart.camera.MouseControl(client_point, Rdr.Camera.ENavOp.Rotate, true);
					}
				}

			// Remove the area selection graphic
			if (this.m_selection_graphic_added) {
				var idx = this.chart.instances.indexOf(this.chart.m_tools.area_select);
				if (idx != -1) this.chart.instances.splice(idx, 1);
			}

			//this.chart.Cursor = Cursors.Default;
			this.chart.Invalidate();
		}
	}]);

	return MouseOpDefaultLButton;
}(MouseOp);

var MouseOpDefaultRButton = function (_MouseOp2) {
	_inherits(MouseOpDefaultRButton, _MouseOp2);

	function MouseOpDefaultRButton(chart) {
		_classCallCheck(this, MouseOpDefaultRButton);

		return _possibleConstructorReturn(this, (MouseOpDefaultRButton.__proto__ || Object.getPrototypeOf(MouseOpDefaultRButton)).call(this, chart));
	}

	_createClass(MouseOpDefaultRButton, [{
		key: "MouseDown",
		value: function MouseDown(ev) {
			// Convert 'grab_client' to normalised camera space
			var nss_point = Rect.NormalisePoint(this.chart.dimensions.chart_area, this.grab_client, +1, -1);

			// Right mouse translates for 2D and 3D scene
			this.chart.camera.MouseControl(nss_point, Rdr.Camera.ENavOp.Translate, true);
		}
	}, {
		key: "MouseMove",
		value: function MouseMove(ev) {
			var client_point = [ev.offsetX, ev.offsetY];

			// If we haven't dragged, treat it as a click instead (i.e. ignore till it's a drag operation)
			if (this.IsClick(client_point)) return;

			// Change the cursor once dragging
			//this.chart.Cursor = Cursors.SizeAll;

			// Limit the drag direction
			var drop_loc = Rect.NormalisePoint(this.chart.dimensions.chart_area, client_point, +1, -1);
			var grab_loc = Rect.NormalisePoint(this.chart.dimensions.chart_area, this.grab_client, +1, -1);
			if (this.hit_result.zone & EZone.YAxis || this.chart.xaxis.lock_range) drop_loc[0] = grab_loc[0];
			if (this.hit_result.zone & EZone.XAxis || this.chart.yaxis.lock_range) drop_loc[1] = grab_loc[1];

			this.chart.camera.MouseControl(drop_loc, Rdr.Camera.ENavOp.Translate, false);
			this.chart.SetRangeFromCamera();
			this.chart.Invalidate();
		}
	}, {
		key: "MouseUp",
		value: function MouseUp(ev) {
			var client_point = [ev.offsetX, ev.offsetY];

			//this.chart.Cursor = Cursors.Default;

			// If we haven't dragged, treat it as a click instead
			if (this.IsClick(client_point)) {
				var args = { hit_result: this.hit_result, handled: false };
				//m_chart.OnChartClicked(args);

				if (!args.handled) {
					// Show the context menu on right click
					if (ev.button == EButton.Right) {
						//if (this.hit_result.Zone & EZone.Chart)
						//	this.chart.ShowContextMenu(client_point, this.hit_result);
						//else if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.XAxis))
						//	m_chart.XAxis.ShowContextMenu(args.Location, args.HitResult);
						//else if (m_hit_result.Zone.HasFlag(HitTestResult.EZone.YAxis))
						//	m_chart.YAxis.ShowContextMenu(args.Location, args.HitResult);
					}
				}
				this.chart.Invalidate();
			} else {
				// Limit the drag direction
				var drop_loc = Rect.NormalisePoint(this.chart.dimensions.chart_area, client_point, +1, -1);
				var grab_loc = Rect.NormalisePoint(this.chart.dimensions.chart_area, this.grab_client, +1, -1);
				if (this.hit_result.zone & EZone.YAxis || this.chart.xaxis.lock_range) drop_loc[0] = grab_loc[0];
				if (this.hit_result.zone & EZone.XAxis || this.chart.yaxis.lock_range) drop_loc[1] = grab_loc[1];

				this.chart.camera.MouseControl(drop_loc, Rdr.Camera.ENavOp.None, true);
				this.chart.SetRangeFromCamera();
				this.chart.Invalidate();
			}
		}
	}]);

	return MouseOpDefaultRButton;
}(MouseOp);

/**
 * Chart utility graphics
 */


var Tools = function () {
	function Tools(chart) {
		_classCallCheck(this, Tools);

		this.chart = chart;
		this.m_area_select = null;
	}

	_createClass(Tools, [{
		key: "area_select",
		get: function get() {
			if (!this.m_area_select) {
				var model = Rdr.Model.Create(this.chart.m_rdr, [// Verts
				{ pos: [+0.0, +0.0, +0.0] }, { pos: [+0.0, +1.0, +0.0] }, { pos: [+1.0, +1.0, +0.0] }, { pos: [+1.0, +0.0, +0.0] }], [// Indices
				0, 1, 2, 0, 2, 3], [// Nuggets
				{ topo: this.chart.m_rdr.TRIANGLES, shader: this.chart.m_rdr.shader_programs.forward, tint: this.chart.options.selection_colour }]);
				this.m_area_select = Rdr.Instance.Create("area_select", model, m4x4.create(), Rdr.EFlags.SceneBoundsExclude | Rdr.EFlags.NoZWrite | Rdr.EFlags.NoZTest);
			}
			return this.m_area_select;
		}
	}]);

	return Tools;
}();

/***/ }),
/* 19 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


/*
 * Date Format 1.2.3
 * (c) 2007-2009 Steven Levithan <stevenlevithan.com>
 * http://blog.stevenlevithan.com/archives/date-time-format
 * MIT license
 *
 * Includes enhancements by Scott Trenda <scott.trenda.net>
 * and Kris Kowal <cixar.com/~kris.kowal/>
 *
 * Accepts a date, a mask, or a date and a mask.
 * Returns a formatted version of the given date.
 * The date defaults to the current date/time.
 * The mask defaults to dateFormat.masks.default.
 */

/*
Usage:
var now = new Date();

now.format("m/dd/yy");
// Returns, e.g., 6/09/07

// Can also be used as a standalone function
dateFormat(now, "dddd, mmmm dS, yyyy, h:MM:ss TT");
// Saturday, June 9th, 2007, 5:46:21 PM

// You can use one of several named masks
now.format("isoDateTime");
// 2007-06-09T17:46:21

// ...Or add your own
dateFormat.masks.hammerTime = 'HH:MM! "Can\'t touch this!"';
now.format("hammerTime");
// 17:46! Can't touch this!

// When using the standalone dateFormat function,
// you can also provide the date as a string
dateFormat("Jun 9 2007", "fullDate");
// Saturday, June 9, 2007

// Note that if you don't include the mask argument,
// dateFormat.masks.default is used
now.format();
// Sat Jun 09 2007 17:46:21

// And if you don't include the date argument,
// the current date and time is used
dateFormat();
// Sat Jun 09 2007 17:46:22

// You can also skip the date argument (as long as your mask doesn't
// contain any numbers), in which case the current date/time is used
dateFormat("longTime");
// 5:46:22 PM EST

// And finally, you can convert local time to UTC time. Either pass in
// true as an additional argument (no argument skipping allowed in this case):
dateFormat(now, "longTime", true);
now.format("longTime", true);
// Both lines return, e.g., 10:46:21 PM UTC

// ...Or add the prefix "UTC:" to your mask.
now.format("UTC:h:MM:ss TT Z");
// 10:46:21 PM UTC
*/

/*
d
Day of the month as digits; no leading zero for single-digit days.
dd
Day of the month as digits; leading zero for single-digit days.
ddd
Day of the week as a three-letter abbreviation.
dddd
Day of the week as its full name.
m
Month as digits; no leading zero for single-digit months.
mm
Month as digits; leading zero for single-digit months.
mmm
Month as a three-letter abbreviation.
mmmm
Month as its full name.
yy
Year as last two digits; leading zero for years less than 10.
yyyy
Year represented by four digits.
h
Hours; no leading zero for single-digit hours (12-hour clock).
hh
Hours; leading zero for single-digit hours (12-hour clock).
H
Hours; no leading zero for single-digit hours (24-hour clock).
HH
Hours; leading zero for single-digit hours (24-hour clock).
M
Minutes; no leading zero for single-digit minutes.
Uppercase M unlike CF timeFormat's m to avoid conflict with months.
MM
Minutes; leading zero for single-digit minutes.
Uppercase MM unlike CF timeFormat's mm to avoid conflict with months.
s
Seconds; no leading zero for single-digit seconds.
ss
Seconds; leading zero for single-digit seconds.
l or L
Milliseconds. l gives 3 digits. L gives 2 digits.
t
Lowercase, single-character time marker string: a or p.
No equivalent in CF.
tt
Lowercase, two-character time marker string: am or pm.
No equivalent in CF.
T
Uppercase, single-character time marker string: A or P.
Uppercase T unlike CF's t to allow for user-specified casing.
TT
Uppercase, two-character time marker string: AM or PM.
Uppercase TT unlike CF's tt to allow for user-specified casing.
Z
US timezone abbreviation, e.g. EST or MDT. With non-US timezones or in the Opera browser, the GMT/UTC offset is returned, e.g. GMT-0500
No equivalent in CF.
o
GMT/UTC timezone offset, e.g. -0500 or +0230.
No equivalent in CF.
S
The date's ordinal suffix (st, nd, rd, or th). Works well with d.
No equivalent in CF.
'…' or "…"
Literal character sequence. Surrounding quotes are removed.
No equivalent in CF.
UTC:
Must be the first four characters of the mask. Converts the date from local time to UTC/GMT/Zulu time before applying the mask. The "UTC:" prefix is removed.
No equivalent in CF.
*/
var DateFormat = function () {
	var token = /d{1,4}|m{1,4}|yy(?:yy)?|([HhMsTt])\1?|[LloSZ]|"[^"]*"|'[^']*'/g,
	    timezone = /\b(?:[PMCEA][SDP]T|(?:Pacific|Mountain|Central|Eastern|Atlantic) (?:Standard|Daylight|Prevailing) Time|(?:GMT|UTC)(?:[-+]\d{4})?)\b/g,
	    timezoneClip = /[^-+\dA-Z]/g,
	    pad = function pad(val, len) {
		val = String(val);
		len = len || 2;
		while (val.length < len) {
			val = "0" + val;
		}return val;
	};

	// Regexes and supporting functions are cached through closure
	return function (date, mask, utc) {
		var dF = DateFormat;

		// You can't provide utc if you skip other args (use the "UTC:" mask prefix)
		if (arguments.length == 1 && Object.prototype.toString.call(date) == "[object String]" && !/\d/.test(date)) {
			mask = date;
			date = undefined;
		}

		// Passing date through Date applies Date.parse, if necessary
		date = date ? new Date(date) : new Date();
		if (isNaN(date)) throw SyntaxError("invalid date");

		mask = String(dF.masks[mask] || mask || dF.masks["default"]);

		// Allow setting the utc argument via the mask
		if (mask.slice(0, 4) == "UTC:") {
			mask = mask.slice(4);
			utc = true;
		}

		var _ = utc ? "getUTC" : "get",
		    d = date[_ + "Date"](),
		    D = date[_ + "Day"](),
		    m = date[_ + "Month"](),
		    y = date[_ + "FullYear"](),
		    H = date[_ + "Hours"](),
		    M = date[_ + "Minutes"](),
		    s = date[_ + "Seconds"](),
		    L = date[_ + "Milliseconds"](),
		    o = utc ? 0 : date.getTimezoneOffset(),
		    flags = {
			d: d,
			dd: pad(d),
			ddd: dF.i18n.DayNames[D],
			dddd: dF.i18n.DayNames[D + 7],
			m: m + 1,
			mm: pad(m + 1),
			mmm: dF.i18n.MonthNames[m],
			mmmm: dF.i18n.MonthNames[m + 12],
			yy: String(y).slice(2),
			yyyy: y,
			h: H % 12 || 12,
			hh: pad(H % 12 || 12),
			H: H,
			HH: pad(H),
			M: M,
			MM: pad(M),
			s: s,
			ss: pad(s),
			l: pad(L, 3),
			L: pad(L > 99 ? Math.round(L / 10) : L),
			t: H < 12 ? "a" : "p",
			tt: H < 12 ? "am" : "pm",
			T: H < 12 ? "A" : "P",
			TT: H < 12 ? "AM" : "PM",
			Z: utc ? "UTC" : (String(date).match(timezone) || [""]).pop().replace(timezoneClip, ""),
			o: (o > 0 ? "-" : "+") + pad(Math.floor(Math.abs(o) / 60) * 100 + Math.abs(o) % 60, 4),
			S: ["th", "st", "nd", "rd"][d % 10 > 3 ? 0 : (d % 100 - d % 10 != 10) * d % 10]
		};

		return mask.replace(token, function ($0) {
			return $0 in flags ? flags[$0] : $0.slice(1, $0.length - 1);
		});
	};
}();

// Some common format strings
DateFormat.masks = {
	"default": "ddd mmm dd yyyy HH:MM:ss",
	shortDate: "m/d/yy",
	mediumDate: "mmm d, yyyy",
	longDate: "mmmm d, yyyy",
	fullDate: "dddd, mmmm d, yyyy",
	shortTime: "h:MM TT",
	mediumTime: "h:MM:ss TT",
	longTime: "h:MM:ss TT Z",
	isoDate: "yyyy-mm-dd",
	isoTime: "HH:MM:ss",
	isoDateTime: "yyyy-mm-dd'T'HH:MM:ss",
	isoUtcDateTime: "UTC:yyyy-mm-dd'T'HH:MM:ss'Z'"
};

// Internationalization strings
DateFormat.i18n = {
	DayNames: ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"],
	MonthNames: ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec", "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"]
};

// For convenience...
Date.prototype.format = function (mask, utc) {
	return DateFormat(this, mask, utc);
};

/***/ })
/******/ ]);
});

/***/ }),
/* 1 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.TicksToUnixMS = TicksToUnixMS;
exports.UnixMStoTimeFrame = UnixMStoTimeFrame;
exports.TimeFrameToUnixMS = TimeFrameToUnixMS;
exports.ShortTimeString = ShortTimeString;
exports.ToBitfinexTimeFrame = ToBitfinexTimeFrame;
/**
 * @module TimeFrame
 */

var ETimeFrame = exports.ETimeFrame = Object.freeze({
	None: "",
	Tick1: "T1 ",
	Min1: "M1 ",
	Min2: "M2 ",
	Min3: "M3 ",
	Min4: "M4 ",
	Min5: "M5 ",
	Min6: "M6 ",
	Min7: "M7 ",
	Min8: "M8 ",
	Min9: "M9 ",
	Min10: "M10",
	Min15: "M15",
	Min20: "M20",
	Min30: "M30",
	Min45: "M45",
	Hour1: "H1 ",
	Hour2: "H2 ",
	Hour3: "H3 ",
	Hour4: "H4 ",
	Hour6: "H6 ",
	Hour8: "H8 ",
	Hour12: "H12",
	Day1: "D1 ",
	Day2: "D2 ",
	Day3: "D3 ",
	Week1: "W1 ",
	Week2: "W2 ",
	Month1: "Month1"
});

/**
 * The value of "0 Ticks" in unix time (seconds)
 */
var UnixEpochInTicks = 621355968000000000;
var ticks_per_ms = 10000;
var ms_per_s = 1000;
var ms_per_min = ms_per_s * 60;
var ms_per_hour = ms_per_min * 60;
var ms_per_day = ms_per_hour * 24;
var ShortDayName = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
var ShortMonthName = ["Jan", "Feb", "Mar", "Apr", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

/**
 * Convert Windows "ticks" to Unix time (in ms)
 * @param {Number} time_in_ticks The time in windows "ticks" (100ns intervals since 0001-1-1 00:00:00)
 * @returns {Number} Returns the time value in Unix time (ms)
 */
function TicksToUnixMS(time_in_ticks) {
	var unix_time_in_ticks = time_in_ticks - UnixEpochInTicks;
	var unix_time_in_ms = unix_time_in_ticks / ticks_per_ms;
	return unix_time_in_ms;
}

/**
 * Convert a time value (in Unix ms) into units of 'time_frame'.
 *  e.g if 'unix_time_ms' is 4.3 hours, and 'time_frame' is Hour1, the 4.3 is returned.
 * @param {Number} unix_time_ms The time value in Unix time (ms)
 * @param {ETimeFrame} time_frame The time frame to convert the time into
 * @returns {Number} Returns a time in multiples of 'time_frame'
 */
function UnixMStoTimeFrame(unix_time_ms, time_frame) {
	switch (time_frame) {
		default:
			throw new Error("Unknown time frame");
		case ETimeFrame.Tick1:
			return unix_time_ms / ms_per_s / 1;
		case ETimeFrame.Min1:
			return unix_time_ms / ms_per_min / 1;
		case ETimeFrame.Min2:
			return unix_time_ms / ms_per_min / 2;
		case ETimeFrame.Min3:
			return unix_time_ms / ms_per_min / 3;
		case ETimeFrame.Min4:
			return unix_time_ms / ms_per_min / 4;
		case ETimeFrame.Min5:
			return unix_time_ms / ms_per_min / 5;
		case ETimeFrame.Min6:
			return unix_time_ms / ms_per_min / 6;
		case ETimeFrame.Min7:
			return unix_time_ms / ms_per_min / 7;
		case ETimeFrame.Min8:
			return unix_time_ms / ms_per_min / 8;
		case ETimeFrame.Min9:
			return unix_time_ms / ms_per_min / 9;
		case ETimeFrame.Min10:
			return unix_time_ms / ms_per_min / 10;
		case ETimeFrame.Min15:
			return unix_time_ms / ms_per_min / 15;
		case ETimeFrame.Min20:
			return unix_time_ms / ms_per_min / 20;
		case ETimeFrame.Min30:
			return unix_time_ms / ms_per_min / 30;
		case ETimeFrame.Min45:
			return unix_time_ms / ms_per_min / 45;
		case ETimeFrame.Hour1:
			return unix_time_ms / ms_per_hour / 1;
		case ETimeFrame.Hour2:
			return unix_time_ms / ms_per_hour / 2;
		case ETimeFrame.Hour3:
			return unix_time_ms / ms_per_hour / 3;
		case ETimeFrame.Hour4:
			return unix_time_ms / ms_per_hour / 4;
		case ETimeFrame.Hour6:
			return unix_time_ms / ms_per_hour / 6;
		case ETimeFrame.Hour8:
			return unix_time_ms / ms_per_hour / 8;
		case ETimeFrame.Hour12:
			return unix_time_ms / ms_per_hour / 12;
		case ETimeFrame.Day1:
			return unix_time_ms / ms_per_day / 1;
		case ETimeFrame.Day2:
			return unix_time_ms / ms_per_day / 2;
		case ETimeFrame.Day3:
			return unix_time_ms / ms_per_day / 3;
		case ETimeFrame.Week1:
			return unix_time_ms / ms_per_day / 7;
		case ETimeFrame.Week2:
			return unix_time_ms / ms_per_day / 14;
		case ETimeFrame.Month1:
			return unix_time_ms / ms_per_day / 30;
	}
}

/**
 * Convert a value in 'time_frame' units to unix time (in ms).
 * @param {Number} units The time value in units of 'time_frame'
 * @param {ETimeFrame} time_frame The time frame that 'units' is in
 * @returns {Number} The time value in unix time (in ms)
 */
function TimeFrameToUnixMS(units, time_frame) {
	// Use 1 second for all tick time-frames
	switch (time_frame) {
		default:
			throw new Error("Unknown time frame");
		case ETimeFrame.Tick1:
			return units * 1 * ms_per_s;
		case ETimeFrame.Min1:
			return units * 1 * ms_per_min;
		case ETimeFrame.Min2:
			return units * 2 * ms_per_min;
		case ETimeFrame.Min3:
			return units * 3 * ms_per_min;
		case ETimeFrame.Min4:
			return units * 4 * ms_per_min;
		case ETimeFrame.Min5:
			return units * 5 * ms_per_min;
		case ETimeFrame.Min6:
			return units * 6 * ms_per_min;
		case ETimeFrame.Min7:
			return units * 7 * ms_per_min;
		case ETimeFrame.Min8:
			return units * 8 * ms_per_min;
		case ETimeFrame.Min9:
			return units * 9 * ms_per_min;
		case ETimeFrame.Min10:
			return units * 10 * ms_per_min;
		case ETimeFrame.Min15:
			return units * 15 * ms_per_min;
		case ETimeFrame.Min20:
			return units * 20 * ms_per_min;
		case ETimeFrame.Min30:
			return units * 30 * ms_per_min;
		case ETimeFrame.Min45:
			return units * 45 * ms_per_min;
		case ETimeFrame.Hour1:
			return units * 1 * ms_per_hour;
		case ETimeFrame.Hour2:
			return units * 2 * ms_per_hour;
		case ETimeFrame.Hour3:
			return units * 3 * ms_per_hour;
		case ETimeFrame.Hour4:
			return units * 4 * ms_per_hour;
		case ETimeFrame.Hour6:
			return units * 6 * ms_per_hour;
		case ETimeFrame.Hour8:
			return units * 8 * ms_per_hour;
		case ETimeFrame.Hour12:
			return units * 12 * ms_per_hour;
		case ETimeFrame.Day1:
			return units * 1 * ms_per_day;
		case ETimeFrame.Day2:
			return units * 2 * ms_per_day;
		case ETimeFrame.Day3:
			return units * 3 * ms_per_day;
		case ETimeFrame.Week1:
			return units * 7 * ms_per_day;
		case ETimeFrame.Week2:
			return units * 14 * ms_per_day;
		case ETimeFrame.Month1:
			return units * 30 * ms_per_day;
	}
}

/**
 * Return a timestamp string suitable for a chart X tick value
 * @param {Number} dt_curr The time value to convert to a string
 * @param {Number} dt_prev The time value of the previous tick
 * @param {boolean} locale_time True if the time should be displayed in the local timezone.
 * @param {boolean} first True if 'dt_prev' is invalid
 * @returns {string}
 */
function ShortTimeString(dt_curr, dt_prev, locale_time, first) {
	var utc = locale_time ? "UTC:" : "";

	// First tick on the x axis
	if (first) {
		var dt0 = new Date(dt_curr);
		return dt0.format(utc + "HH:MM\nyyyy-mm-dd");
	} else {
		var _dt = new Date(dt_curr);
		var dt1 = new Date(dt_prev);

		// Show more of the time stamp depending on how it differs from the previous time stamp
		if (_dt.getYear() != dt1.getYear()) return _dt.format(utc + "HH:MM\nyyyy-mm-dd");else if (_dt.getMonth() != dt1.getMonth()) return _dt.format(utc + "HH:MM\nmmm-d");else if (_dt.getDay() != dt1.getDay()) return _dt.format(utc + "HH:MM\nddd d");else return _dt.format(utc + "HH:MM");
	}
}

/**
 * Convert a time frame to the nearest Bitfinex time frame
 * @param {ETimeFrame} tf 
 */
function ToBitfinexTimeFrame(tf) {
	switch (tf) {
		default:
			throw new Error("Unknown time frame");
		case ETimeFrame.None:
		case ETimeFrame.Tick1:
		case ETimeFrame.Min1:
			return "1m";
		case ETimeFrame.Min2:
		case ETimeFrame.Min3:
		case ETimeFrame.Min4:
		case ETimeFrame.Min5:
			return "5m";
		case ETimeFrame.Min6:
		case ETimeFrame.Min7:
		case ETimeFrame.Min8:
		case ETimeFrame.Min9:
		case ETimeFrame.Min10:
		case ETimeFrame.Min15:
			return "15m";
		case ETimeFrame.Min20:
		case ETimeFrame.Min30:
			return "30m";
		case ETimeFrame.Min45:
		case ETimeFrame.Hour1:
			return "1h";
		case ETimeFrame.Hour2:
		case ETimeFrame.Hour3:
			return "3h";
		case ETimeFrame.Hour4:
		case ETimeFrame.Hour6:
			return "6h";
		case ETimeFrame.Hour8:
		case ETimeFrame.Hour12:
			return "12h";
		case ETimeFrame.Day1:
			return "1D";
		case ETimeFrame.Day2:
		case ETimeFrame.Day3:
		case ETimeFrame.Week1:
			return "7D";
		case ETimeFrame.Week2:
			return "14D";
		case ETimeFrame.Month1:
			return "1M";
	}
}

/***/ }),
/* 2 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});
exports.Instrument = exports.ETimeFrame = exports.TimeFrame = exports.TradingChart = undefined;

var _createClass = function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; }(); /**
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      * @module TradingChart
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      */

var _rylogic = __webpack_require__(0);

var Ry = _interopRequireWildcard(_rylogic);

var _time_frame = __webpack_require__(1);

var TF = _interopRequireWildcard(_time_frame);

var _instrument = __webpack_require__(3);

var Instr = _interopRequireWildcard(_instrument);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

var v4 = Ry.Maths.v4;
var m4x4 = Ry.Maths.m4x4;
var BBox = Ry.Maths.BBox;

var TradingChart = exports.TradingChart = new function () {
	this.Create = function (canvas, instrument) {
		return new Chart(canvas, instrument);
	};
	this.EXAxisLabelMode = Object.freeze({
		LocalTime: 0,
		UtcTime: 1,
		CandleIndex: 2
	});
}();
var TimeFrame = exports.TimeFrame = TF;
var ETimeFrame = exports.ETimeFrame = TF.ETimeFrame;
var Instrument = exports.Instrument = Instr;

/**
 * Chart for displaying candle sticks
 */

var Chart = function () {
	function Chart(canvas, instrument) {
		_classCallCheck(this, Chart);

		var This = this;

		// Save the interface to the chart data
		this.instrument = instrument;
		this.instrument.OnDataChanged.sub(function (s, a) {
			This._HandleCandleDataChanged(a);
		});

		// Create a chart instance
		this.chart = Ry.Chart.Create(canvas);
		this.chart.xaxis.options.pixels_per_tick = 50;
		this.chart.xaxis.MeasureTickText = function (gfx) {
			return This._HandleXAxisMeasureTickText(gfx);
		};
		this.chart.xaxis.TickText = function (x, step) {
			return This._HandleChartXAxisLabels(x, step);
		};
		this.chart.OnRendering.sub(function (s, a) {
			This._HandleRendering();
		});
		this.chart.OnAutoRange.sub(function (s, a) {
			This._HandleAutoRange(a);
		});
		this.chart.OnChartMoved.sub(function (s, a) {
			This._HandleChartMoved(a);
		});
		this.chart.OnChartClicked.sub(function (s, a) {
			console.log(a);
		});

		// Create a candle graphics cache
		this.candles = new CandleCache(This);

		// Options for this chart
		this.options = new Options();
		function Options() {
			this.colour_bullish = "#00C000";
			this.colour_bearish = "#C00000";
			this.xaxis_label_mode = TradingChart.EXAxisLabelMode.LocalTime;
		};
	}

	_createClass(Chart, [{
		key: "AutoRange",


		/**
   * Automatically set the axis ranges to display the latest candles
   */
		value: function AutoRange() {
			this.chart.AutoRange();
		}

		/**
   * Render the chart
   */

	}, {
		key: "Render",
		value: function Render() {
			this.chart.Render();
		}

		/**
   * Handle rendering
   */

	}, {
		key: "_HandleRendering",
		value: function _HandleRendering() {
			// Add candle graphics for the displayed range
			var instances = this.chart.instances;
			this.candles.Get(this.xaxis.min - 1, this.xaxis.max + 1, function (gfx) {
				if (gfx.inst == null) return;

				// Position the graphics
				gfx.inst.o2w = m4x4.Translation([gfx.candle_range.beg, 0, 0, 1]);
				instances.push(gfx.inst);
			});
		}

		/**
   * Handle auto ranging of chart data
   * @param {Args} a 
   */

	}, {
		key: "_HandleAutoRange",
		value: function _HandleAutoRange(a) {
			if (this.instrument == null || this.instrument.count == 0) return;

			// Display the last few candles @ N pixels per candle
			var bb = BBox.create();
			var pixels_per_candle = 6;
			var width = a.dims.chart_area.w / pixels_per_candle; // in candles
			var idx_max = this.instrument.count + width * 1 / 5;
			var idx_min = this.instrument.count - width * 4 / 5;
			this.instrument.candles(idx_min, idx_max, function (candle) {
				BBox.EncompassPoint(bb, v4.make(idx_min, candle.l, 0, 1), bb);
				BBox.EncompassPoint(bb, v4.make(idx_max, candle.h, 0, 1), bb);
			});

			if (!bb.is_valid) return;

			// Swell the box a little for margins
			v4.set(bb.radius, bb.radius[0], bb.radius[1] * 1.1, bb.radius[2], 0);
			BBox.EncompassBBox(a.view_bbox, bb, a.view_bbox);
			a.handled = true;
		}

		/**
   * Handle the chart zooming or scrolling
   * @param {Args} a 
   */

	}, {
		key: "_HandleChartMoved",
		value: function _HandleChartMoved(a) {
			// See if the displayed X axis range is available in
			// the chart data. If not, request it in the instrument.
			if (this.instrument.count != 0) {
				var first = this.instrument.candle(0);
				var latest = this.instrument.latest;

				// Request data for the missing regions.
				if (this.chart.xaxis.min < 0) {
					// Request candles before 'first'
					var num = Math.max(0 - this.chart.xaxis.min, this.candles.BatchSize * 10);
					var period_ms = TF.TimeFrameToUnixMS(num, this.instrument.time_frame);
					this.instrument.RequestData(first.ts - period_ms, first.ts);
				}
			}
		}

		/**
   * Convert the XAxis values into pretty datetime strings
   */

	}, {
		key: "_HandleChartXAxisLabels",
		value: function _HandleChartXAxisLabels(x, step) {
			// Note:
			// The X axis is not a linear time axis because the markets may not be online all the time.
			// Use 'Instrument.TimeToIndexRange' to convert time values to X Axis values.

			// Draw the X Axis labels as indices instead of time stamps
			if (this.options.xaxis_label_mode == TradingChart.EXAxisLabelMode.CandleIndex) return this.chart.xaxis.TickTextDefault(x, step);
			if (this.instrument == null || this.instrument.count == 0) return "";

			// If the ticks are within the range of instrument data, use the actual time stamp.
			// This accounts for missing candles in the data range.
			var prev = Math.floor(x - step);
			var curr = Math.floor(x);

			// If the current tick mark represents the same candle as the previous one, no text is required
			if (prev == curr) return "";

			// The range of indices
			var first = 0;
			var last = this.instrument.count;
			var candle_beg = this.instrument.candle(first);
			var candle_end = this.instrument.candle(last - 1);

			// Get the date/time for the tick
			var dt_curr = curr >= first && curr < last ? this.instrument.candle(curr).ts : curr < first ? candle_beg.ts - TF.TimeFrameToUnixMS(first - curr, this.instrument.time_frame) : curr >= last ? candle_end.ts + TF.TimeFrameToUnixMS(curr - last + 1, this.instrument.time_frame) : null;
			var dt_prev = prev >= first && prev < last ? this.instrument.candle(prev).ts : prev < first ? candle_beg.ts - TF.TimeFrameToUnixMS(first - prev, this.instrument.time_frame) : prev >= last ? candle_end.ts + TF.TimeFrameToUnixMS(prev - last + 1, this.instrument.time_frame) : null;

			// Display in local time zone
			var locale_time = this.options.xaxis_label_mode == TradingChart.EXAxisLabelMode.LocalTime;

			// First tick on the x axis
			var first_tick = curr == first || prev < first || x - step < this.xaxis.min;

			// Show more of the time stamp depending on how it differs from the previous time stamp
			return TF.ShortTimeString(dt_curr, dt_prev, locale_time, first_tick);
		}

		/**
   * Return the space to reserve for each XAxis tick label
   */

	}, {
		key: "_HandleXAxisMeasureTickText",
		value: function _HandleXAxisMeasureTickText(gfx) {
			var sz = this.xaxis.MeasureTickTextDefault(gfx);
			sz.height *= 2;
			return sz;
		}

		/**
   * Handle the instrument data changing
   * @param {Args} args 
   */

	}, {
		key: "_HandleCandleDataChanged",
		value: function _HandleCandleDataChanged(args) {
			// Invalidate the graphics for the candles over the range
			this.candles.InvalidateRange(args.beg, args.end);

			// Shift the xaxis range so that the chart doesn't jump
			if (this.chart.xaxis.max > args.beg) {
				this.chart.xaxis.Shift(args.ofs);
				this.chart.SetCameraFromRange();
			}

			// Invalidate the chart
			this.chart.Invalidate();
		}
	}, {
		key: "xaxis",
		get: function get() {
			return this.chart.xaxis;
		}
	}, {
		key: "yaxis",
		get: function get() {
			return this.chart.yaxis;
		}
	}]);

	return Chart;
}();

var CandleCache = function () {
	function CandleCache(chart) {
		_classCallCheck(this, CandleCache);

		this.BatchSize = 1024;
		this.chart = chart;
		this.cache = {};

		// Recycle buffers for vertex, index, and nugget data
		this.m_vbuf = [];
		this.m_ibuf = [];
		this.m_nbuf = [];
	}

	/**
  * Enumerate the candle graphics instances over the given candle index range
  * @param {Number} beg The first candle index
  * @param {Number} end The last candle index
  * @param {function} cb Callback with the candle graphics
  */


	_createClass(CandleCache, [{
		key: "Get",
		value: function Get(beg, end, cb) {
			// Convert the candle indices to cache indices
			var idx0 = Math.floor(beg / this.BatchSize);
			var idx1 = Math.floor(end / this.BatchSize);
			for (var i = idx0; i <= idx1; ++i) {
				// Get the graphics model at 'i'
				if (i < 0) continue;
				var gfx = this.At(i);
				cb(gfx);
			}
		}

		/**
   * Get the candle graphics at 'cache_idx'
   * @param {Number} cache_idx The index into the cache of graphics objects
   * @returns {gfx, candle_range}
   */

	}, {
		key: "At",
		value: function At(cache_idx) {
			// Look in the cache first
			var gfx = this.cache[cache_idx];
			if (gfx) return gfx;

			// On miss, generate the graphics model for the data range [idx, idx + min(BatchSize, Count-idx))
			// where 'idx' is the candle index of the first candle in the batch.
			var rdr = this.chart.chart.rdr;
			var instrument = this.chart.instrument;
			var colour_bullish = Ry.Util.ColourToV4(this.chart.options.colour_bullish);
			var colour_bearish = Ry.Util.ColourToV4(this.chart.options.colour_bearish);
			var shader = rdr.shader_programs.forward;

			// Get the candle range from the cache index
			var candle_range = Ry.Range.make((cache_idx + 0) * this.BatchSize, (cache_idx + 1) * this.BatchSize);
			candle_range.beg = Ry.Maths.Clamp(candle_range.beg, 0, instrument.count);
			candle_range.end = Ry.Maths.Clamp(candle_range.end, 0, instrument.count);
			if (candle_range.count == 0) return this.cache[cache_idx] = new CandleGfx(null, candle_range);

			// Use TriList for the bodies, and LineList for the wicks.
			// So:    6 indices for the body, 4 for the wicks
			//   __|__
			//  |\    |
			//  |  \  |
			//  |____\|
			//     |

			// Divide the index buffer into [bodies, wicks]

			// Resize the cache buffers
			var count = candle_range.count;
			this.m_vbuf.length = 8 * count;
			this.m_ibuf.length = (6 + 4) * count;
			this.m_nbuf.length = 2;

			// Index of the first body index and the first wick index.
			var vert = 0;
			var body = 0;
			var wick = 6 * count;
			var nugt = 0;

			// Create the geometry
			for (var candle_idx = 0; candle_idx != count;) {
				var candle = instrument.candle(candle_range.beg + candle_idx);

				// Create the graphics with the first candle at x == 0
				var x = candle_idx++;
				var o = Math.max(candle.o, candle.c);
				var h = candle.h;
				var l = candle.l;
				var c = Math.min(candle.o, candle.c);
				var col = candle.c > candle.o ? colour_bullish : candle.c < candle.o ? colour_bearish : 0xFFA0A0A0;
				var v = vert;

				// Candle verts
				this.m_vbuf[vert++] = { pos: [x, h, 0, 1], col: col };
				this.m_vbuf[vert++] = { pos: [x, o, 0, 1], col: col };
				this.m_vbuf[vert++] = { pos: [x - 0.4, o, 0, 1], col: col };
				this.m_vbuf[vert++] = { pos: [x + 0.4, o, 0, 1], col: col };
				this.m_vbuf[vert++] = { pos: [x - 0.4, c, 0, 1], col: col };
				this.m_vbuf[vert++] = { pos: [x + 0.4, c, 0, 1], col: col };
				this.m_vbuf[vert++] = { pos: [x, c, 0, 1], col: col };
				this.m_vbuf[vert++] = { pos: [x, l, 0, 1], col: col };

				// Candle body
				this.m_ibuf[body++] = v + 3;
				this.m_ibuf[body++] = v + 2;
				this.m_ibuf[body++] = v + 4;
				this.m_ibuf[body++] = v + 4;
				this.m_ibuf[body++] = v + 5;
				this.m_ibuf[body++] = v + 3;

				// Candle wick
				if (o != c) {
					this.m_ibuf[wick++] = v + 0;
					this.m_ibuf[wick++] = v + 1;
					this.m_ibuf[wick++] = v + 6;
					this.m_ibuf[wick++] = v + 7;
				} else {
					this.m_ibuf[wick++] = v + 0;
					this.m_ibuf[wick++] = v + 7;
					this.m_ibuf[wick++] = v + 2;
					this.m_ibuf[wick++] = v + 5;
				}
			}

			this.m_nbuf[nugt++] = { topo: rdr.TRIANGLES, shader: shader, vrange: { ofs: 0, count: vert }, irange: { ofs: 0, count: body } };
			this.m_nbuf[nugt++] = { topo: rdr.LINES, shader: shader, vrange: { ofs: 0, count: vert }, irange: { ofs: body, count: wick - body } };

			// Create the graphics
			var model = Ry.Rdr.Model.Create(rdr, this.m_vbuf, this.m_ibuf, this.m_nbuf);
			var inst = Ry.Rdr.Instance.Create("Candles-[" + candle_range.beg + "," + candle_range.end + ")", model);
			var candle_graphics = new CandleGfx(inst, candle_range);
			return this.cache[cache_idx] = candle_graphics;

			// Cache element
			function CandleGfx(inst, candle_range) {
				this.inst = inst;
				this.candle_range = candle_range;
			}
		}

		/**
   * Invalidate the graphics models for the given candle index range
   * @param {Number} beg The first invalid candle index
   * @param {Number} end One past the last invalid candle index 
   */

	}, {
		key: "InvalidateRange",
		value: function InvalidateRange(beg, end) {
			var idx0 = Math.floor(beg / this.BatchSize);
			var idx1 = Math.floor(end / this.BatchSize);
			for (var i = idx0; i <= idx1; ++i) {
				if (!this.cache[i]) continue;
				this.cache[i] = null;
			}
		}
	}]);

	return CandleCache;
}();

/***/ }),
/* 3 */
/***/ (function(module, exports, __webpack_require__) {

"use strict";


Object.defineProperty(exports, "__esModule", {
	value: true
});

var _createClass = function () { function defineProperties(target, props) { for (var i = 0; i < props.length; i++) { var descriptor = props[i]; descriptor.enumerable = descriptor.enumerable || false; descriptor.configurable = true; if ("value" in descriptor) descriptor.writable = true; Object.defineProperty(target, descriptor.key, descriptor); } } return function (Constructor, protoProps, staticProps) { if (protoProps) defineProperties(Constructor.prototype, protoProps); if (staticProps) defineProperties(Constructor, staticProps); return Constructor; }; }(); /**
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      * @module Instrument
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      */

exports.Create = Create;

var _rylogic = __webpack_require__(0);

var Ry = _interopRequireWildcard(_rylogic);

var _time_frame = __webpack_require__(1);

var TF = _interopRequireWildcard(_time_frame);

function _interopRequireWildcard(obj) { if (obj && obj.__esModule) { return obj; } else { var newObj = {}; if (obj != null) { for (var key in obj) { if (Object.prototype.hasOwnProperty.call(obj, key)) newObj[key] = obj[key]; } } newObj.default = obj; return newObj; } }

function _classCallCheck(instance, Constructor) { if (!(instance instanceof Constructor)) { throw new TypeError("Cannot call a class as a function"); } }

var Range = Ry.Maths.Range;

/**
 * Create an instrument based on 'currency_pair'
 * @param {String} currency_pair The current pair, e.g. BTCUSD
 * @param {ETimeFrame} time_frame The period of each candle
 * @param {Number} epoch_time The time of the first data for the currency pair
 * @returns {Instrument}
 */
function Create(currency_pair, time_frame, epoch_time) {
	return new (function () {
		function _class() {
			_classCallCheck(this, _class);

			this.currency_pair = currency_pair;
			this.epoch_time = epoch_time;
			this.time_frame = time_frame;
			this.data = [];

			this.OnDataChanged = new Ry.Util.MulticastDelegate();

			var now = Date.now();
			this.m_request = null;
			this.m_request_time = now;
			this.m_requested_ranges = [];
			this.RequestData(now - TF.TimeFrameToUnixMS(1000, this.time_frame), now);

			// Poll for data requests
			var This = this;
			window.requestAnimationFrame(function poll() {
				This._SubmitRequestData();
				This._PollLatestCandle();
				window.requestAnimationFrame(poll);
			});
		}

		/**
   * Return the number of candles in this instrument
   * @returns {Number}
   */


		_createClass(_class, [{
			key: "candle",


			/**
    * Return the candle at 'idx'. Returns null if 'idx' is out of range.
    * @param {Number} idx 
    * @returns {Candle}
    */
			value: function candle(idx) {
				var candle = this.data[idx];
				if (!candle) return null;
				return { ts: candle[0], o: candle[1], c: candle[2], h: candle[3], l: candle[4], v: candle[5] };
			}

			/**
    * Enumerate the candles in the index range [beg,end) calling 'cb' for each
    * @param {Number} beg 
    * @param {Number} end 
    * @param {Function} cb 
    */

		}, {
			key: "candles",
			value: function candles(beg, end, cb) {
				var ibeg = Math.floor(Math.max(0, beg));
				var iend = Math.floor(Math.min(this.count, end));
				for (var i = ibeg; i < iend; ++i) {
					cb(this.candle(i));
				}
			}

			/**
    * Pull candle data
    * @param {Number} beg_time_ms (optional) The start time to get candles from (in UTC Unix time ms)
    * @param {Number} end_time_ms (optional) The end time to get candles from (in UTC Unix time ms)
   */

		}, {
			key: "RequestData",
			value: function RequestData(beg_time_ms, end_time_ms) {
				// Determine the range to get
				if (end_time_ms == null) end_time_ms = beg_time_ms ? beg_time_ms + TF.TimeFrameToUnixMS(100, this.time_frame) : Date.now();
				if (beg_time_ms == null) beg_time_ms = end_time_ms ? end_time_ms - TF.TimeFrameToUnixMS(100, this.time_frame) : Date.now();

				// Clip to the valid time range
				var latest = this.latest;
				beg_time_ms = Math.max(beg_time_ms, this.epoch_time);
				end_time_ms = Math.min(end_time_ms, latest ? latest.ts : Date.now());

				// Trim the request range by the existing pending requests
				for (var i = 0; i != this.m_requested_ranges.length; ++i) {
					var rng = this.m_requested_ranges[i];
					if (rng.end >= end_time_ms) end_time_ms = Math.min(end_time_ms, rng.beg);
					if (rng.beg <= beg_time_ms) beg_time_ms = Math.max(beg_time_ms, rng.end);
				}

				// If the request is already covered by pending requests, then no need to request again
				if (beg_time_ms >= end_time_ms) return;

				// Queue the data request
				this.m_requested_ranges.push(Range.make(beg_time_ms, end_time_ms));
			}

			/**
    * Returns true if it's too soon to send another data request
    * @returns {boolean}
   */

		}, {
			key: "_LimitRequestRate",
			value: function _LimitRequestRate() {
				// Request in flight?
				if (this.m_request != null) return true;

				// Too soon since the last request?
				var seconds_per_request = 3;
				if (Date.now() - this.m_request_time < seconds_per_request * 1000) return true;

				return false;
			}

			/**
    * Read the updated data for the latest candle
   */

		}, {
			key: "_PollLatestCandle",
			value: function _PollLatestCandle() {
				// Allowed to send another request?
				if (this._LimitRequestRate()) return;

				// Request the last candle from bitfinex
				var url = "https://api.bitfinex.com/v2/candles/trade:" + TimeFrame.ToBitfinexTimeFrame(this.time_frame) + ":t" + this.currency_pair + "/last";

				// Make the request
				this._SendRequest(url, false);
			}

			/**
    * Submit requests for data
    */

		}, {
			key: "_SubmitRequestData",
			value: function _SubmitRequestData() {
				// Allowed to send another request?
				if (this._LimitRequestRate()) return;

				// No requests pending?
				if (this.m_requested_ranges.length == 0) return;

				// Get the next pending request
				var range = this.m_requested_ranges[0];

				// Request the data from bitfinex
				var url = "https://api.bitfinex.com/v2/candles/trade:" + TimeFrame.ToBitfinexTimeFrame(this.time_frame) + ":t" + this.currency_pair + "/hist?start=" + range.beg + "&end=" + range.end;

				// Make the request
				this._SendRequest(url, true);
			}

			/**
    * Create and send a request
    * @param {string} url The REST API url
    * @param {boolean} history True if this is a history request
    */

		}, {
			key: "_SendRequest",
			value: function _SendRequest(url, history) {
				var This = this;

				// Make the request
				this.m_request = new XMLHttpRequest();
				this.m_request.open("GET", url);
				this.m_request.ontimeout = function () {
					This.m_request = null;
					This.m_request_time = Date.now();

					// Remove from the pending request list
					if (history) This.m_requested_ranges.shift();
				};
				this.m_request.onreadystatechange = function () {
					// Not ready yet?
					if (this.readyState != 4) return;

					This.m_request = null;
					This.m_request_time = Date.now();

					// Remove from the pending request list
					if (history) This.m_requested_ranges.shift();

					// Null or empty response...
					if (this.responseText == null || this.responseText.length == 0) return;

					// Read the data
					var data = JSON.parse(this.responseText);
					if (!history) data = [data];
					if (data.length == 0) return;

					// Sort by time
					data.sort(function (l, r) {
						return l[0] - r[0];
					});

					// Add the data to the instrument
					This._MergeData(data);
				};

				// Send the request
				this.m_request.send();
			}

			/**
    * Add 'data' to the instrument data.
    * @param {Array} data 
    */

		}, {
			key: "_MergeData",
			value: function _MergeData(data) {
				// Find the location in the instrument data to insert/add/replace with 'data'
				var beg = data[0];
				var end = data[data.length - 1];
				var ibeg = Ry.Alg.BinarySearch(this.data, function (x) {
					return x[0] - beg[0];
				}, true);
				var iend = Ry.Alg.BinarySearch(this.data, function (x) {
					return x[0] - end[0];
				}, true);
				for (; ibeg > 0 && this.data[ibeg - 1][0] >= beg[0]; --ibeg) {}
				for (; iend < this.data.length && this.data[iend + 0][0] <= end[0]; ++iend) {}

				// Splice the new data into 'this.data'
				Array.prototype.splice.apply(this.data, [ibeg, iend - ibeg].concat(data));

				// Notify of the changed data range.
				// All indices from 'ibeg' to the end are changed
				this.OnDataChanged.invoke(this, { beg: ibeg, end: this.data.length, ofs: data.length - (iend - ibeg) });
			}
		}, {
			key: "count",
			get: function get() {
				return this.data.length;
			}

			/**
    * Return the latest candle
    * @returns {Candle}
    */

		}, {
			key: "latest",
			get: function get() {
				var count = this.count;
				return count != 0 ? this.candle(count - 1) : null;
			}
		}]);

		return _class;
	}())();
}

/***/ })
/******/ ]);
});