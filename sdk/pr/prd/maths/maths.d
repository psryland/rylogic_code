module prd.maths.maths;

import std.math;
import std.conv;
import std.stdio;
import std.algorithm;
import prd.maths.rand;

const float  tiny      = 1.000000e-4F;
const float  tiny_sq   = 1.000000e-8F;
const float  phi       = 1.618034e+0F; // "Golden Ratio"
const float  pi        = 3.141592e+0F; // mmmmm pie
const float  inv_pi    = 3.183099e-1F;
const float  two_pi    = 6.283185e+0F;
const float  pi_by_2   = 1.570796e+0F;
const float  pi_by_4   = 7.853982e-1F;
const float  pi_by_180 = 1.745329e-2F;
const float  l80_by_pi = 5.729578e+1F;
const double dbl_tiny  = 1.000000e-12;

align (16) struct v2
{
	union
	{
		struct { float x,y; }
		float[2] arr;
	}

	//bug immutable static v2 zero   = v2(0,0);
	//bug immutable static v2 xaxis  = v2(1,0);
	//bug immutable static v2 yaxis  = v2(0,1);
	@property static v2 zero () { return v2(0,0); }
	@property static v2 xaxis() { return v2(1,0); }
	@property static v2 yaxis() { return v2(0,1); }

	this(float x_)                                          { set(x_); }
	this(float x_, float y_)                                { set(x_, y_); }

	ref v2 set(float x_, float y_)                          { x = x_; y = y_; return this; }
	ref v2 set(float x_)                                    { return set(x_,x_); }

	@property int     ix() const pure                       { return cast(int)x; }
	@property int     iy() const pure                       { return cast(int)y; }
	@property string  string2() const                       { return to!string(x)~' '~to!string(y); }
	@property float   length2sq() const pure                { return Length2sq(x, y); }
	@property float   length2() const pure                  { return Length2(x, y); }
	
	float     opIndex(size_t i) const pure                  { return arr[i]; }
	ref float opIndex(size_t i) pure                        { return arr[i]; }
	void  opIndexAssign(T)(T value, size_t i)               { arr[i] = cast(float)value; }
	
	ref v2 opOpAssign(string op)(float rhs)
	{
		static if (op == "+") { x += rhs; y += rhs; return this; }
		static if (op == "-") { x -= rhs; y -= rhs; return this; }
		static if (op == "*") { x *= rhs; y *= rhs; return this; }
		static if (op == "/") { assert(rhs != 0); x /= rhs; y /= rhs; return this; }
		static if (op == "%") { assert(rhs != 0); x %= rhs; y %= rhs; return this; }
	}
	ref v2 opOpAssign(string op)(in v2 rhs)
	{
		static if (op == "+") { x += rhs.x; y += rhs.y; return this; }
		static if (op == "-") { x -= rhs.x; y -= rhs.y; return this; }
		static if (op == "*") { x *= rhs.x; y *= rhs.y; return this; }
		static if (op == "/") { assert(all2(rhs, &IsNonZero)); x /= rhs.x; y /= rhs.y; return this; }
		static if (op == "%") { assert(all2(rhs, &IsNonZero)); x %= rhs.x; y %= rhs.y; return this; }
	}
	v2 opUnary(string op)() const
	{
		static if (op == "+") { return this; }
		static if (op == "-") { return v2(-x, -y); }
	}
	v2 opBinary(string op)(float rhs) const
	{
		static if (op == "+") { v2 v = this; return v += rhs; }
		static if (op == "-") { v2 v = this; return v -= rhs; }
		static if (op == "*") { v2 v = this; return v *= rhs; }
		static if (op == "/") { v2 v = this; return v /= rhs; }
		static if (op == "%") { v2 v = this; return v %= rhs; }
	}
	v2 opBinary(string op)(in v2 rhs) const
	{
		static if (op == "+") { v2 v = this; return v += rhs; }
		static if (op == "-") { v2 v = this; return v -= rhs; }
		static if (op == "*") { v2 v = this; return v *= rhs; }
		static if (op == "/") { v2 v = this; return v /= rhs; }
		static if (op == "%") { v2 v = this; return v %= rhs; }
	}
	v2 opBinaryRight(string op)(float lhs) const
	{
		static if (op == "+") { v2 v = this; return v += lhs; }
		static if (op == "*") { v2 v = this; return v *= lhs; }
	}
	
	static v2 normal2(float x, float y)   { v2 v = v2(x,y); return Normalise2(v); }
	static v2 random2()                   { return v2(FRand(),FRand()); }
	static v2 random2(float mn, float mx) { return FRand(mn,mx) * random_normal(); }
	static v2 random_normal()             { v2 v; do {v = random2(-1f,1f);} while (v.length2sq > 1f); return Normalise2(v); }
}
align (16) struct v3
{
	union {
	struct { float x,y,z; }
	struct { v2 xy; }
	float[3] arr;
	}

	//bug immutable static v3 zero  = v3(0,0,0);
	//bug immutable static v3 xaxis = v3(1,0,0);
	//bug immutable static v3 yaxis = v3(0,1,0);
	//bug immutable static v3 zaxis = v3(0,0,1);
	@property static v3 zero () { return v3(0,0,0); }
	@property static v3 xaxis() { return v3(1,0,0); }
	@property static v3 yaxis() { return v3(0,1,0); }
	@property static v3 zaxis() { return v3(0,0,1); }

	this(float x_)                                          { set(x_); }
	this(float x_, float y_, float z_)                      { set(x_, y_, z_); }
	this(in v2 xy_, float z_)                               { set(xy_, z_); }

	ref v3 set(float x_, float y_, float z_)                { x = x_; y = y_; z = z_; return this; }
	ref v3 set(in v2 xy, float z)                           { return set(xy.x, xy.y, z); }
	ref v3 set(float x_)                                    { return set(x_,x_,x_); }

	@property int      ix() const                           { return cast(int)x; }
	@property int      iy() const                           { return cast(int)y; }
	@property int      iz() const                           { return cast(int)z; }
	@property string   string3() const                      { return to!string(x)~' '~to!string(y)~' '~to!string(z); }
	@property float    length3sq() const                    { return Length3sq(x, y, z); }
	@property float    length3() const                      { return Length3(x, y, z); }

	float     opIndex(size_t i) const                       { return arr[i]; }
	ref float opIndex(size_t i)                             { return arr[i]; }
	void      opIndexAssign(T)(T value, size_t i)           { arr[i] = cast(float)value; }

	ref v3 opOpAssign(string op)(float rhs)
	{
		static if (op == "+") { xy += rhs; z += rhs; return this; }
		static if (op == "-") { xy -= rhs; z -= rhs; return this; }
		static if (op == "*") { xy *= rhs; z *= rhs; return this; }
		static if (op == "/") { assert(rhs != 0); xy /= rhs; z /= rhs; return this; }
		static if (op == "%") { assert(rhs != 0); xy %= rhs; z %= rhs; return this; }
	}
	ref v3 opOpAssign(string op)(in v3 rhs)
	{
		static if (op == "+") { xy += rhs.xy; z += rhs.z; return this; }
		static if (op == "-") { xy -= rhs.xy; z -= rhs.z; return this; }
		static if (op == "*") { xy *= rhs.xy; z *= rhs.z; return this; }
		static if (op == "/") { assert(all3(rhs, &IsNonZero)); xy /= rhs.xy; z /= rhs.z; return this; }
		static if (op == "%") { assert(all3(rhs, &IsNonZero)); xy %= rhs.xy; z %= rhs.z; return this; }
	}
	v3 opUnary(string op)() const
	{
		static if (op == "+") { return this; }
		static if (op == "-") { return v3(-x, -y, -z); }
	}
	v3 opBinary(string op)(float rhs) const
	{
		static if (op == "+") { v3 v = this; return v += rhs; }
		static if (op == "-") { v3 v = this; return v -= rhs; }
		static if (op == "*") { v3 v = this; return v *= rhs; }
		static if (op == "/") { v3 v = this; return v /= rhs; }
		static if (op == "%") { v3 v = this; return v %= rhs; }
	}
	v3 opBinary(string op)(in v3 rhs) const
	{
		static if (op == "+") { v3 v = this; return v += rhs; }
		static if (op == "-") { v3 v = this; return v -= rhs; }
		static if (op == "*") { v3 v = this; return v *= rhs; }
		static if (op == "/") { v3 v = this; return v /= rhs; }
		static if (op == "%") { v3 v = this; return v %= rhs; }
	}
	v3 opBinaryRight(string op)(float lhs) const
	{
		static if (op == "+") { v3 v = this; return v += lhs; }
		static if (op == "*") { v3 v = this; return v *= lhs; }
	}
	
	static v3 normal3(float x, float y, float z)            { v3 v = v3(x,y,z); return Normalise3(v); }
	static v3 random3()                                     { return v3(v2.random2(), FRand()); }
	static v3 random3(float mn, float mx)                   { return FRand(mn,mx) * random_normal(); }
	static v3 random_normal()                               { v3 v; do {v.set(FRand(-1f,1f),FRand(-1f,1f),FRand(-1f,1f));} while (v.length3sq > 1f); return Normalise3(v); }
}
align (16) struct v4
{
	union {
	struct { float x,y,z,w; }
	struct { v3 xyz; }
	struct { v2 xy,zw; }
	float[4] arr;
	}

	//bug immutable static v4 zero   = v4(0,0,0,0);
	//bug immutable static v4 xaxis  = v4(1,0,0,0);
	//bug immutable static v4 yaxis  = v4(0,1,0,0);
	//bug immutable static v4 zaxis  = v4(0,0,1,0);
	//bug immutable static v4 waxis  = v4(0,0,0,1);
	//bug immutable static v4 origin = v4(0,0,0,1);
	@property static v4 zero  () pure nothrow { return v4(0,0,0,0); }
	@property static v4 xaxis () pure nothrow { return v4(1,0,0,0); }
	@property static v4 yaxis () pure nothrow { return v4(0,1,0,0); }
	@property static v4 zaxis () pure nothrow { return v4(0,0,1,0); }
	@property static v4 waxis () pure nothrow { return v4(0,0,0,1); }
	@property static v4 origin() pure nothrow { return v4(0,0,0,1); }

	this(float x_) pure nothrow                                     { set(x_); }
	this(float x_, float y_, float z_, float w_) pure nothrow       { set(x_, y_, z_, w_); }
	this(in v3 xyz_, float w_) pure nothrow                         { set(xyz_, w_); }
	this(in v2 xy_, in v2 zw_) pure nothrow                         { set(xy_, zw_); }

	ref v4 set(float x_, float y_, float z_, float w_) pure nothrow { x = x_; y = y_; z = z_; w = w_; return this; }
	ref v4 set(in v3 xyz_, float w_) pure nothrow                   { return set(xyz.x, xyz.y, xyz.z, w_); }
	ref v4 set(in v2 xy_, in v2 zw_) pure nothrow                   { return set(xy_.x, xy_.y, zw_.x, zw_.y); }
	ref v4 set(float x_) pure nothrow                               { return set(x_,x_,x_,x_); }

	v4 w0() const pure nothrow                                      { return v4(x, y, z, 0f); }
	v4 w1() const pure nothrow                                      { return v4(x, y, z, 1f); }

	@property int     ix() const pure nothrow                       { return cast(int)x; }
	@property int     iy() const pure nothrow                       { return cast(int)y; }
	@property int     iz() const pure nothrow                       { return cast(int)z; }
	@property int     iw() const pure nothrow                       { return cast(int)w; }
	@property string  string3() const                       { return to!string(x)~' '~to!string(y)~' '~to!string(z); }
	@property string  string4() const                       { return to!string(x)~' '~to!string(y)~' '~to!string(z)~' '~to!string(w); }
	@property float   length3sq() const                     { return Length3sq(x, y, z); }
	@property float   length4sq() const                     { return Length4sq(x, y, z, w); }
	@property float   length3() const                       { return Length3(x, y, z); }
	@property float   length4() const                       { return Length4(x, y, z, w); }

	float     opIndex(size_t i) const                       { return arr[i]; }
	ref float opIndex(size_t i)                             { return arr[i]; }
	void      opIndexAssign(T)(T value, size_t i)           { arr[i] = cast(float)value; }

	ref v4 opOpAssign(string op)(float rhs)
	{
		static if (op == "+") { xyz += rhs; w += rhs; return this; }
		static if (op == "-") { xyz -= rhs; w -= rhs; return this; }
		static if (op == "*") { xyz *= rhs; w *= rhs; return this; }
		static if (op == "/") { assert(rhs != 0); xyz /= rhs; w /= rhs; return this; }
		static if (op == "%") { assert(rhs != 0); xyz %= rhs; w %= rhs; return this; }
	}
	ref v4 opOpAssign(string op)(in v4 rhs)
	{
		static if (op == "+") { xyz += rhs.xyz; w += rhs.w; return this; }
		static if (op == "-") { xyz -= rhs.xyz; w -= rhs.w; return this; }
		static if (op == "*") { xyz *= rhs.xyz; w *= rhs.w; return this; }
		static if (op == "/") { assert(all4(rhs, &IsNonZero)); xyz /= rhs.xyz; w /= rhs.w; return this; }
		static if (op == "%") { assert(all4(rhs, &IsNonZero)); xyz %= rhs.xyz; w %= rhs.w; return this; }
	}
	v4 opUnary (string op)() const
	{
		static if (op == "+") { return this; }
		static if (op == "-") { return v4(-x, -y, -z, -w); }
	}
	v4 opBinary(string op)(float rhs) const
	{
		static if (op == "+") { v4 v = this; return v += rhs; } // todo: compiler bug preventing float being a template parameter
		static if (op == "-") { v4 v = this; return v -= rhs; }
		static if (op == "*") { v4 v = this; return v *= rhs; }
		static if (op == "/") { v4 v = this; return v /= rhs; }
		static if (op == "%") { v4 v = this; return v %= rhs; }
	}
	v4 opBinary(string op)(in v4 rhs) const
	{
		static if (op == "+") { v4 v = this; return v += rhs; }
		static if (op == "-") { v4 v = this; return v -= rhs; }
		static if (op == "*") { v4 v = this; return v *= rhs; }
		static if (op == "/") { v4 v = this; return v /= rhs; }
		static if (op == "%") { v4 v = this; return v %= rhs; }
	}
	v4 opBinaryRight(string op)(float rhs) const
	{
		static if (op == "*") { v4 v = this; return v *= rhs; }
	}

	static v4 normal3(float x, float y, float z, float w)  { v4 v = v4(x,y,z,w); return Normalise3(v); }
	static v4 normal4(float x, float y, float z, float w)  { v4 v = v4(x,y,z,w); return Normalise4(v); }
	static v4 random3(float w)                             { return v4(v3.random3(), w); }
	static v4 random3(float mn, float mx, float w)         { return v4(v3.random3(mn,mx), w); }
	static v4 random_normal3()                             { return v4(v3.random_normal(), 0f); }
	static v4 random4()                                    { return v4(v3.random3(), FRand()); }
	static v4 random4(float mn, float mx)                  { return FRand(mn,mx) * random_normal4(); }
	static v4 random_normal4()                             { v4 v; do {v.set(FRand(-1f,1f),FRand(-1f,1f),FRand(-1f,1f),FRand(-1f,1f));} while (v.length3sq > 1f); return Normalise4(v); }
}
align (16) struct m3x3
{
	union {
	struct { v4 x,y,z; }
	v4[3] arr;
	}

	//bug immutable static m3x3 zero     = m3x3(v4.zero, v4.zero, v4.zero);
	//bug immutable static m3x3 identity = m3x3(v4.xaxis, v4.yaxis, v4.zaxis);
	@property static m3x3 zero    () { return m3x3(v4.zero, v4.zero, v4.zero);    }
	@property static m3x3 identity() { return m3x3(v4.xaxis, v4.yaxis, v4.zaxis); }

	this(float x_)                                          { set(x_); }
	this(in v4 x_, in v4 y_, in v4 z_)                      { set(x_, y_, z_); }
	
	ref m3x3 set(float x_)                                  { x.set(x_); y.set(x_); z.set(x_); return this; }
	ref m3x3 set(in v4 x_, in v4 y_, in v4 z_)              { x = x_; y = y_; z = z_; return this; }

	v4   row(int i) const                                   { return v4(x[i], y[i], z[i], 0f); }
	void row(int i, in v4 row)                              { x[i] = row.x; y[i] = row.y; z[i] = row.z; }
	v4   col(int i) const                                   { return arr[i]; }
	void col(int i, in v4 col)                              { arr[i] = col; }

	v4     opIndex(size_t i) const                          { return arr[i]; }
	ref v4 opIndex(size_t i)                                { return arr[i]; }
	void opIndexAssign(T)(T value, size_t i)                { arr[i] = cast(v4)value; }

	ref m3x3 opOpAssign(string op)(float rhs)
	{
		static if (op == "+") { x.xyz += rhs; y.xyz += rhs; z.xyz += rhs; return this; }
		static if (op == "-") { x.xyz -= rhs; y.xyz -= rhs; z.xyz -= rhs; return this; }
		static if (op == "*") { x *= rhs; y *= rhs; z *= rhs; return this; }
		static if (op == "/") { x /= rhs; y /= rhs; z /= rhs; return this; }
	}
	ref m3x3 opOpAssign(string op)(in m3x3 rhs)
	{
		static if (op == "+") { x.xyz += rhs.x.xyz; y.xyz += rhs.y.xyz; z.xyz += rhs.z.xyz; return this; }
		static if (op == "-") { x.xyz -= rhs.x.xyz; y.xyz -= rhs.y.xyz; z.xyz -= rhs.z.xyz; return this; }
	}
	m3x3 opUnary (string op)() const
	{
		static if (op == "+") { return this; }
		static if (op == "-") { return m3x3(-x, -y, -z); }
	}
	m3x3 opBinary(string op)(float rhs) const
	{
		static if (op == "+") { m3x3 m = this; return m += rhs; }
		static if (op == "-") { m3x3 m = this; return m -= rhs; }
		static if (op == "*") { m3x3 m = this; return m *= rhs; }
		static if (op == "/") { m3x3 m = this; return m /= rhs; }
	}
	v4 opBinary(string op)(in v4 rhs) const
	{
		static if (op == "*") { return Mul(this, rhs); }
	}
	m3x3 opBinary(string op)(in m3x3 rhs) const
	{
		static if (op == "+") { m3x3 m = this; return m += rhs; }
		static if (op == "-") { m3x3 m = this; return m -= rhs; }
		static if (op == "*") { return Mul(this, rhs); }
	}
	m3x3 opBinaryRight(string op)(float lhs) const
	{
		static if (op == "+") { m3x3 m = this; return m += lhs; }
		static if (op == "*") { m3x3 m = this; return m *= lhs; }
	}
}
align (16) struct m4x4
{
	union {
	struct { v4 x,y,z,w; }
	struct { m3x3 rot; v4 pos; }
	v4[4] arr;
	}

	//bug immutable static m4x4 zero     = m4x4(v4.zero, v4.zero, v4.zero, v4.zero);
	//bug immutable static m4x4 identity = m4x4(v4.xaxis, v4.yaxis, v4.zaxis, v4.waxis);
	@property static m4x4 zero    () { return m4x4(v4.zero, v4.zero, v4.zero, v4.zero);     }
	@property static m4x4 identity() { return m4x4(v4.xaxis, v4.yaxis, v4.zaxis, v4.waxis); }

	this(float x_)                                          { set(x_); }
	this(in v4 x_, in v4 y_, in v4 z_, in v4 w_)            { set(x_, y_, z_, w_); }
	
	ref m4x4 set(float x_)                                  { x.set(x_); y.set(x_); z.set(x_); w.set(x_); return this; }
	ref m4x4 set(in v4 x_, in v4 y_, in v4 z_, in v4 w_)    { x = x_; y = y_; z = z_; w = w_; return this; }

	v4   row(int i) const                                   { return v4(x[i], y[i], z[i], w[i]); }
	void row(int i, in v4 row)                              { x[i] = row.x; y[i] = row.y; z[i] = row.z; w[i] = row.w; }
	v4   col(int i) const                                   { return arr[i]; }
	void col(int i, in v4 col)                              { arr[i] = col; }

	v4     opIndex(size_t i) const                          { return arr[i]; }
	ref v4 opIndex(size_t i)                                { return arr[i]; }
	void opIndexAssign(T)(T value, size_t i)                { arr[i] = cast(v4)value; }

	ref m4x4 opOpAssign(string op)(float rhs)
	{
		static if (op == "+") { x += rhs; y += rhs; z += rhs; w += rhs; return this; }
		static if (op == "-") { x -= rhs; y -= rhs; z -= rhs; w -= rhs; return this; }
		static if (op == "*") { x *= rhs; y *= rhs; z *= rhs; w *= rhs; return this; }
		static if (op == "/") { x /= rhs; y /= rhs; z /= rhs; w /= rhs; return this; }
	}
	ref m4x4 opOpAssign(string op)(in m4x4 rhs)
	{
		static if (op == "+") { x += rhs.x; y += rhs.y; z += rhs.z; w += rhs.w; return this; }
		static if (op == "-") { x -= rhs.x; y -= rhs.y; z -= rhs.z; w -= rhs.w; return this; }
	}
	m4x4 opUnary(string op)() const
	{
		static if (op == "+") { return this; }
		static if (op == "-") { return m4x4(-x, -y, -z, -w); }
	}
	m4x4 opBinary(string op)(float rhs) const
	{
		static if (op == "+") { m4x4 m = this; return m += rhs; }
		static if (op == "-") { m4x4 m = this; return m -= rhs; }
		static if (op == "*") { m4x4 m = this; return m *= rhs; }
		static if (op == "/") { m4x4 m = this; return m /= rhs; }
	}
	v4 opBinary(string op)(in v4 rhs) const
	{
		static if (op == "*") { return Mul(this, rhs); }
	}
	m4x4 opBinary(string op)(in m4x4 rhs) const
	{
		static if (op == "+") { m4x4 m = this; return m += rhs; }
		static if (op == "-") { m4x4 m = this; return m -= rhs; }
		static if (op == "*") { return Mul(this, rhs); }
	}
	m4x4 opBinaryRight(string op)(float lhs) const
	{
		static if (op == "+") { m4x4 m = this; return m += lhs; }
		static if (op == "*") { m4x4 m = this; return m *= lhs; }
	}
}

// Functions **************************************************
bool     feql(float lhs, float rhs, float tol = tiny) pure
{
	return std.math.abs(lhs - rhs) < tol;
}
bool     feql(in v2 lhs, in v2 rhs, float tol = tiny)
{
	v2 v = abs(rhs - lhs);
	return v.x < tol && v.y < tol;
}
bool     feql(in v3 lhs, in v3 rhs, float tol = tiny)
{
	v3 v = abs(rhs - lhs);
	return v.x < tol && v.y < tol && v.z < tol;
}
bool     feql(in v4 lhs, in v4 rhs, float tol = tiny)
{
	v4 v = abs(rhs - lhs);
	return v.x < tol && v.y < tol && v.z < tol && v.w < tol;
}
bool     feql(in m3x3 lhs, in m3x3 rhs, float tol = tiny)
{
	return
		feql(lhs.x.xyz, rhs.x.xyz, tol) &&
		feql(lhs.y.xyz, rhs.y.xyz, tol) &&
		feql(lhs.z.xyz, rhs.z.xyz, tol);
}
bool     feql(in m4x4 lhs, in m4x4 rhs, float tol = tiny)
{
	return
		feql(lhs.x, rhs.x, tol) &&
		feql(lhs.y, rhs.y, tol) &&
		feql(lhs.z, rhs.z, tol) &&
		feql(lhs.w, rhs.w, tol);
}
v2       abs(in v2 vec)
{
	return v2(std.math.abs(vec.x), std.math.abs(vec.y));
}
v3       abs(in v3 vec)
{
	return v3(std.math.abs(vec.x), std.math.abs(vec.y), std.math.abs(vec.z));
}
v4       abs(in v4 vec)
{
	return v4(std.math.abs(vec.x), std.math.abs(vec.y), std.math.abs(vec.z), std.math.abs(vec.w));
}
m3x3     abs(in m3x3 mat)
{
	return m3x3(abs(mat.x), abs(mat.y), abs(mat.z));
}
m4x4     abs(in m4x4 mat)
{
	return m4x4(abs(mat.x), abs(mat.y), abs(mat.z), abs(mat.pos));
}
T        sign(T)(bool positive)
{
	return cast(T)(2 * cast(int)positive - 1);
}
float    sqr(float x)
{
	return x * x;
}
v4       sqr(in v4 vec)
{
	return v4(sqr(vec.x), sqr(vec.y), sqr(vec.z), sqr(vec.w));
}
float    Length2sq(float x, float y) pure
{
	return x*x + y*y;
}
float    Length3sq(float x, float y, float z) pure
{
	return x*x + y*y + z*z;
}
float    Length4sq(float x, float y, float z, float w) pure
{
	return x*x + y*y + z*z + w*w;
}
float    Length2(float x, float y) pure
{
	return sqrt(Length2sq(x, y));
}
float    Length3(float x, float y, float z) pure
{
	return sqrt(Length3sq(x, y, z));
}
float    Length4(float x, float y, float z, float w) pure
{
	return sqrt(Length4sq(x, y, z, w));
}
bool     any2(T,Pred)(in T vec, Pred pred = &IsNonZero) pure
{
	return pred(vec.x) || pred(vec.y);
}
bool     any3(T,Pred)(in T vec, Pred pred = &IsNonZero)
{
	return any2(vec,pred) || pred(vec.z);
}
bool     any4(T,Pred)(in T vec, Pred pred = &IsNonZero)
{
	return any3(vec,pred) || pred(vec.w);
}
bool     all2(T,Pred)(in T vec, Pred pred = &IsNonZero)
{
	return pred(vec.x) && pred(vec.y);
}
bool     all3(T,Pred)(in T vec, Pred pred = &IsNonZero)
{
	return all2(vec,pred) && pred(vec.z);
}
bool     all4(T,Pred)(in T vec, Pred pred = &IsNonZero)
{
	return all3(vec,pred) && pred(vec.w);
}
bool     IsNonZero(float x)
{
	return x != 0f;
}
bool     IsNonZero(in v2 vec)
{
	return IsNonZero(vec.x) && IsNonZero(vec.y);
}
bool     IsNonZero(in v3 vec)
{
	return IsNonZero(vec.xy) && IsNonZero(vec.z);
}
bool     IsNonZero(in v4 vec)
{
	return IsNonZero(vec.xyz) && IsNonZero(vec.w);
}
bool     IsZero(float x)
{
	return x == 0;
}
bool     IsZero(in v2 vec)
{
	return IsZero(vec.x) && IsZero(vec.y);
}
bool     IsZero(in v3 vec)
{
	return IsZero(vec.xy) && IsZero(vec.z);
}
bool     IsZero(in v4 vec)
{
	return IsZero(vec.xyz) && IsZero(vec.w);
}
float    Dot3(in v4 lhs, in v4 rhs)
{
	return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z;
}
float    Dot4(in v4 lhs, in v4 rhs)
{
	return lhs.x * rhs.x + lhs.y * rhs.y + lhs.z * rhs.z + lhs.w * rhs.w;
}
v4       Cross3(in v4 lhs, in v4 rhs)
{
	return v4(lhs.y*rhs.z - lhs.z*rhs.y, lhs.z*rhs.x - lhs.x*rhs.z, lhs.x*rhs.y - lhs.y*rhs.x, 0f);
}
float    Triple3(in v4 a, in v4 b, in v4 c)
{
	return Dot3(a, Cross3(b, c));
}
T        Lerp(T)(in T src, in T dest, float frac)
{
	return src*(1f - frac) + dest*(frac);
}
v4       Mul(in m3x3 lhs, in v4 rhs)
{
	v4 res;
	m3x3 lhs_t = GetTranspose(lhs);
	res.x = Dot3(lhs_t.x, rhs);
	res.y = Dot3(lhs_t.y, rhs);
	res.z = Dot3(lhs_t.z, rhs);
	res.w = rhs.w;
	return res;
}
v4       Mul(in m4x4 lhs, in v4 rhs)
{
	v4 res;
	m4x4 lhs_t = GetTranspose(lhs);
	res.x = Dot4(lhs_t.x, rhs);
	res.y = Dot4(lhs_t.y, rhs);
	res.z = Dot4(lhs_t.z, rhs);
	res.w = Dot4(lhs_t.w, rhs);
	return res;
}
m3x3     Mul(in m3x3 lhs, in m3x3 rhs)
{
	m3x3 res, lhs_t = GetTranspose(lhs);
	for (int j = 0; j < 3; ++j)
	{
		res[j].x = Dot3(lhs_t.x, rhs[j]);
		res[j].y = Dot3(lhs_t.y, rhs[j]);
		res[j].z = Dot3(lhs_t.z, rhs[j]);
		res[j].w = 0.0f;
	}
	return res;
}
m4x4     Mul(in m4x4 lhs, in m4x4 rhs)
{
	m4x4 res, lhs_t = GetTranspose(lhs);
	for (int j = 0; j < 4; ++j)
	{
		res[j].x = Dot4(lhs_t.x, rhs[j]);
		res[j].y = Dot4(lhs_t.y, rhs[j]);
		res[j].z = Dot4(lhs_t.z, rhs[j]);
		res[j].w = Dot4(lhs_t.w, rhs[j]);
	}
	return res;
}
ref T    Normalise2(T)(ref T vec)
{
	assert(vec.length2 != 0f);
	vec /= vec.length2;
	return vec;
}
ref T    Normalise3(T)(ref T vec)
{
	assert(vec.length3 != 0f);
	vec /= vec.length3;
	return vec;
}
ref T    Normalise4(T)(ref T vec)
{
	assert(vec.length4 != 0f);
	vec /= vec.length4;
	return vec;
}
T        GetNormal2(T)(in T vec)
{
	T v = vec;
	return Normalise2(v);
}
T        GetNormal3(T)(in T vec)
{
	T v = vec;
	return Normalise3(v);
}
T        GetNormal4(T)(const T vec)
{
	T v = vec;
	return Normalise4(v);
}
bool     Parallel(in v4 v0, in v4 v1, float tol = tiny)
{
	return Cross3(v0, v1).length3sq <= tol;
}
v4       CreateNotParallelTo(in v4 vec)
{
	bool x_aligned = std.math.abs(vec.x) > std.math.abs(vec.y) && std.math.abs(vec.x) > std.math.abs(vec.z);
	return v4(cast(float)(!x_aligned), 0f, cast(float)(x_aligned), vec.w);
}
v4       Perpendicular(in v4 vec)
{
	assert(vec.length3sq != 0f, "zero vector passed to Perpendicular()");
	v4 v = Cross3(vec, CreateNotParallelTo(vec));
	v *= vec.length3 / v.length3;
	return v;
}
ref m3x3 Transpose(ref m3x3 mat)
{
	swap(mat.x.y, mat.y.x);
	swap(mat.x.z, mat.z.x);
	swap(mat.y.z, mat.z.y);
	return mat;
}
ref m4x4 Transpose(ref m4x4 mat)
{
	swap(mat.x.y, mat.y.x);
	swap(mat.x.z, mat.z.x);
	swap(mat.x.w, mat.w.x);
	swap(mat.y.z, mat.z.y);
	swap(mat.y.w, mat.w.y);
	swap(mat.z.w, mat.w.z);
	return mat;
}
m3x3     GetTranspose(in m3x3 mat)
{
	m3x3 m = mat;
	return Transpose(m);
}
m4x4     GetTranspose(in m4x4 mat)
{
	m4x4 m = mat;
	return Transpose(m);
}
ref m3x3 Inverse(ref m3x3 mat)
{
	assert(IsInvertable(mat), "Matrix has no inverse");
	float inv_det = 1.0f / Determinant3(mat);
	m3x3  tmp = GetTranspose(mat);
	mat.x = Cross3(tmp.y, tmp.z) * inv_det;
	mat.y = Cross3(tmp.z, tmp.x) * inv_det;
	mat.z = Cross3(tmp.x, tmp.y) * inv_det;
	return mat;
}
ref m4x4 Inverse(ref m4x4 mat)
{
	m4x4 A = GetTranspose(mat); // Take the transpose so that row operations are faster
	alias mat B; B = m4x4.identity;

	// Loop through columns
	for (int j = 0; j < 4; ++j)
	{
		// Select pivot element: maximum magnitude in this column1
		v4 col = abs(A.row(j)); // Remember, we've transposed '*this'
		int pivot = j;
		for (int i = pivot + 1; i != 4; ++i)
		{
			if (col[i] > col[pivot])
				pivot = i;
		}
		if (col[pivot] < tiny)
		{
			assert(false, "Matrix has no inverse");
			//return mat;
		}

		// Interchange rows to put pivot element on the diagonal
		if (pivot != j) // skip if already on diagonal
		{
			swap(A[j], A[pivot]);
			swap(B[j], B[pivot]);
		}

		// Divide row by pivot element
		float scale = A[j][j];
		if( scale != 1.0f ) // skip if already equal to 1
		{
			A[j] /= scale;
			B[j] /= scale;
			// now the pivot element is 1
		}

		// Subtract this row from others to make the rest of column j zero
		if (j != 0) { scale = A[0][j]; A[0] -= scale * A[j]; B[0] -= scale * B[j]; }
		if (j != 1) { scale = A[1][j]; A[1] -= scale * A[j]; B[1] -= scale * B[j]; }
		if (j != 2) { scale = A[2][j]; A[2] -= scale * A[j]; B[2] -= scale * B[j]; }
		if (j != 3) { scale = A[3][j]; A[3] -= scale * A[j]; B[3] -= scale * B[j]; }
	}
	// When these operations have been completed, A should have been transformed to the identity matrix
	// and B should have been transformed into the inverse of the original A
	Transpose(B);
	return mat;
}
m3x3     GetInverse(in m3x3 mat)
{
	assert(IsInvertable(mat), "Matrix has no inverse");
	float inv_det = 1.0f / Determinant3(mat);
	m3x3 tmp;
	tmp.x = Cross3(mat.y, mat.z) * inv_det;
	tmp.y = Cross3(mat.z, mat.x) * inv_det;
	tmp.z = Cross3(mat.x, mat.y) * inv_det;
	return Transpose(tmp);
}
m4x4     GetInverse(in m4x4 mat)
{
	m4x4 m = mat;
	return Inverse(m);
}
ref m3x3 InverseFast(ref m3x3 mat)
{
	assert(IsOrthonormal(mat), "Matrix is not orthonormal");
	return Transpose(mat);
}
ref m4x4 InverseFast(ref m4x4 mat)
{
	// Find the inverse of this matrix. It must be orthonormal
	assert(IsOrthonormal(mat), "Matrix is not orthonormal");
	
	v4 translation = mat.pos; Transpose(mat.rot);
	mat.pos.x = -(translation.x * mat.x.x + translation.y * mat.y.x + translation.z * mat.z.x);
	mat.pos.y = -(translation.x * mat.x.y + translation.y * mat.y.y + translation.z * mat.z.y);
	mat.pos.z = -(translation.x * mat.x.z + translation.y * mat.y.z + translation.z * mat.z.z);
	return mat;
}
m3x3     GetInverseFast(in m3x3 mat)
{
	m3x3 m = mat;
	return InverseFast(m);
}
m4x4     GetInverseFast(in m4x4 mat)
{
	// Return the inverse of this matrix. It must be orthonormal
	m4x4 m = mat;
	return InverseFast(m);
}
float    Determinant3(in m3x3 mat)
{
	return Triple3(mat.x, mat.y, mat.z);
}
float    Determinant4(in m4x4 mat)
{
	// Return the 4x4 determinant of the arbitrary transform 'mat'
	float c1 = (mat.z.z * mat.w.w) - (mat.z.w * mat.w.z);
	float c2 = (mat.z.y * mat.w.w) - (mat.z.w * mat.w.y);
	float c3 = (mat.z.y * mat.w.z) - (mat.z.z * mat.w.y);
	float c4 = (mat.z.x * mat.w.w) - (mat.z.w * mat.w.x);
	float c5 = (mat.z.x * mat.w.z) - (mat.z.z * mat.w.x);
	float c6 = (mat.z.x * mat.w.y) - (mat.z.y * mat.w.x);
	return
		mat.x.x * (mat.y.y*c1 - mat.y.z*c2 + mat.y.w*c3) -
		mat.x.y * (mat.y.x*c1 - mat.y.z*c4 + mat.y.w*c5) +
		mat.x.z * (mat.y.x*c2 - mat.y.y*c4 + mat.y.w*c6) -
		mat.x.w * (mat.y.x*c3 - mat.y.y*c5 + mat.y.z*c6);
}
float    DeterminantFast4(in m4x4 mat)
{
	// Return the 4x4 determinant of the affine transform 'mat'
	assert(IsAffine(mat), "'mat' must be an affine transform to use this function");
	return
		(mat.x.x * mat.y.y * mat.z.z) +
		(mat.x.y * mat.y.z * mat.z.x) +
		(mat.x.z * mat.y.x * mat.z.y) -
		(mat.x.z * mat.y.y * mat.z.x) -
		(mat.x.y * mat.y.x * mat.z.z) -
		(mat.x.x * mat.y.z * mat.z.y);
}
float    Trace3(in m3x3 mat)
{
	return mat.x.x + mat.y.y + mat.z.z;
}
float    Trace3(in m4x4 mat)
{
	return Trace3(mat.rot);
}
float    Trace4(in m4x4 mat)
{
	return mat.x.x + mat.y.y + mat.z.z + mat.w.w;
}
v4       Kernel(in m3x3 mat)
{
	return v4(
		 mat.y.y*mat.z.z - mat.y.z*mat.z.y,
		-mat.y.x*mat.z.z + mat.y.z*mat.z.x,
		 mat.y.x*mat.z.y - mat.y.y*mat.z.x,
		 0.0f);
}
v4       Kernel(in m4x4 mat)
{
	return Kernel(mat.rot);
}
bool     IsAffine(in m4x4 mat)
{
	// Return true if 'mat' is an affine transform
	return
		mat.x.w == 0f &&
		mat.y.w == 0f &&
		mat.z.w == 0f &&
		mat.w.w == 1f;
}
bool     IsInvertable(in m3x3 mat)
{
	return !feql(Determinant3(mat), 0.0f);
}
bool     IsOrthonormal(in m3x3 mat)
{
	// Return true if 'mat' is orthonormal
	return	feql(mat.x.length3sq, 1f) &&
			feql(mat.y.length3sq, 1f) &&
			feql(mat.z.length3sq, 1f) &&
			feql(std.math.abs(Determinant3(mat)), 1f);
}
bool     IsOrthonormal(in m4x4 mat)
{
	// Return true if 'mat' is orthonormal
	return IsOrthonormal(mat.rot);
}
ref m3x3 Orthonormalise(ref m3x3 mat)
{
	// Orthonormalises 'mat'
	Normalise3(mat.x);
	mat.y = GetNormal3(Cross3(mat.z, mat.x));
	mat.z = Cross3(mat.x, mat.y);
	assert(IsOrthonormal(mat), "Orthonormalise is broken");
	return mat;
}
ref m4x4 Orthonormalise(ref m4x4 mat)
{
	// Orthonormalises the rotation component of the matrix
	Orthonormalise(mat.rot);
	return mat;
}
ref m4x4 Translation(ref m4x4 mat, in v3 xyz)
{
	mat = m4x4.identity;
	mat.pos.xyz = xyz;
	mat.pos.w = 1f;
	return mat;
}
m4x4     Translation(in v3 xyz)
{
	m4x4 mat;
	return Translation(mat, xyz);
}
ref m4x4 Translation(ref m4x4 mat, in v4 xyz)
{
	mat = m4x4.identity;
	mat.pos = xyz.w1();
	return mat;
}
m4x4     Translation(in v4 xyz)
{
	m4x4 mat;
	return Translation(mat, xyz);
}
ref m4x4 Translation(ref m4x4 mat, float x, float y, float z)
{
	mat = m4x4.identity;
	mat.pos.set(x,y,z,1f);
	return mat;
}
m4x4     Translation(float x, float y, float z)
{
	m4x4 mat;
	return Translation(mat, x, y, z);
}
ref m3x3 Rotation3x3(ref m3x3 mat, in v4 axis_norm, in v4 axis_sine_angle, float cos_angle)
{
	// Create a rotation matrix from an axis, angle
	assert(feql(axis_norm.length3, 1f), "'axis_norm' should be normalised");
	
	v4 trace_vec = axis_norm * (1.0f - cos_angle);
	mat.x.x = trace_vec.x * axis_norm.x + cos_angle;
	mat.y.y = trace_vec.y * axis_norm.y + cos_angle;
	mat.z.z = trace_vec.z * axis_norm.z + cos_angle;
	
	trace_vec.x *= axis_norm.y;
	trace_vec.z *= axis_norm.x;
	trace_vec.y *= axis_norm.z;
	
	mat.x.y = trace_vec.x + axis_sine_angle.z;
	mat.x.z = trace_vec.z - axis_sine_angle.y;
	mat.x.w = 0.0f;
	mat.y.x = trace_vec.x - axis_sine_angle.z;
	mat.y.z = trace_vec.y + axis_sine_angle.x;
	mat.y.w = 0.0f;
	mat.z.x = trace_vec.z + axis_sine_angle.y;
	mat.z.y = trace_vec.y - axis_sine_angle.x;
	mat.z.w = 0.0f;
	return mat;
}
m3x3     Rotation3x3(in v4 axis_norm, in v4 axis_sine_angle, float cos_angle)
{
	m3x3 mat;
	return Rotation3x3(mat, axis_norm, axis_sine_angle, cos_angle);
}
ref m4x4 Rotation4x4(ref m4x4 mat, in v4 axis_norm, in v4 axis_sine_angle, float cos_angle, in v4 translation)
{
	Rotation3x3(mat.rot, axis_norm, axis_sine_angle, cos_angle);
	mat.pos = translation;
	return mat;
}
m4x4     Rotation4x4(in v4 axis_norm, in v4 axis_sine_angle, float cos_angle, in v4 translation)
{
	m4x4 mat;
	return Rotation4x4(mat, axis_norm, axis_sine_angle, cos_angle, translation);
}
ref m3x3 Rotation3x3(ref m3x3 mat, in v4 from, in v4 to)
{
	// Create a transform representing the rotation from one vector to another.
	assert(feql(from.length3, 1f) && feql(to.length3, 1f), "'from' and 'to' should be normalised");
	
	float cos_angle    = Dot3(from, to);      // Cos angle
	v4 axis_sine_angle = Cross3(from, to);    // Axis multiplied by sine of the angle
	v4 axis_norm       = GetNormal3(axis_sine_angle);
	return Rotation3x3(mat, axis_norm, axis_sine_angle, cos_angle);
}
m3x3     Rotation3x3(in v4 from, in v4 to)
{
	m3x3 mat;
	return Rotation3x3(mat, from, to);
}
ref m4x4 Rotation4x4(ref m4x4 mat, in v4 from, in v4 to, in v4 translation)
{
	Rotation3x3(mat.rot, from, to);
	mat.pos = translation;
	return mat;
}
m4x4     Rotation4x4(in v4 from, in v4 to, in v4 translation)
{
	m4x4 mat;
	return Rotation4x4(mat, from, to, translation);
}
ref m3x3 Rotation3x3(ref m3x3 mat, in v4 axis_norm, float angle)
{
	// Create from an axis and angle.
	assert(feql(axis_norm.length3, 1f), "'axis_norm' should be normalised");
	return Rotation3x3(mat, axis_norm, axis_norm * sin(angle), cos(angle));
}
m3x3     Rotation3x3(in v4 axis_norm, float angle)
{
	m3x3 mat;
	return Rotation3x3(mat, axis_norm, angle);
}
ref m4x4 Rotation4x4(ref m4x4 mat, in v4 axis_norm, float angle, in v4 translation)
{
	Rotation3x3(mat.rot, axis_norm, angle);
	mat.pos = translation;
	return mat;
}
m4x4     Rotation4x4(in v4 axis_norm, float angle, in v4 translation)
{
	m4x4 mat;
	return Rotation4x4(mat, axis_norm, angle, translation);
}
ref m3x3 Rotation3x3(ref m3x3 mat, float pitch, float yaw, float roll)
{
	// Create from an pitch, yaw, and roll.
	// Order is roll, pitch, yaw because objects usually face along Z and have Y as up.
	float cos_p = cos(pitch), sin_p = sin(pitch);
	float cos_y = cos(yaw  ), sin_y = sin(yaw  );
	float cos_r = cos(roll ), sin_r = sin(roll );
	mat.x.set( cos_y*cos_r + sin_y*sin_p*sin_r , cos_p*sin_r , -sin_y*cos_r + cos_y*sin_p*sin_r , 0f);
	mat.y.set(-cos_y*sin_r + sin_y*sin_p*cos_r , cos_p*cos_r ,  sin_y*sin_r + cos_y*sin_p*cos_r , 0f);
	mat.z.set( sin_y*cos_p                     ,      -sin_p ,                      cos_y*cos_p , 0f);
	return mat;
}
m3x3     Rotation3x3(float pitch, float yaw, float roll)
{
	m3x3 mat;
	return Rotation3x3(mat, pitch, yaw, roll);
}
ref m4x4 Rotation4x4(ref m4x4 mat, float pitch, float yaw, float roll, in v4 translation)
{
	Rotation3x3(mat.rot, pitch, yaw, roll);
	mat.pos = translation;
	return mat;
}
m4x4     Rotation4x4(float pitch, float yaw, float roll, in v4 translation)
{
	m4x4 mat;
	return Rotation4x4(mat, pitch, yaw, roll, translation);
}
ref m3x3 Rotation3x3(ref m3x3 mat, in v4 quat)
{
	assert(feql(quat.length4sq, 1f, 0.001f), "'quat' is not a unit quaternion");
	
	float len_sq = quat.length4sq;
	float s      = 2.0f / len_sq;
	
	float xs = quat.x *  s, ys = quat.y *  s, zs = quat.z *  s;
	float wx = quat.w * xs, wy = quat.w * ys, wz = quat.w * zs;
	float xx = quat.x * xs, xy = quat.x * ys, xz = quat.x * zs;
	float yy = quat.y * ys, yz = quat.y * zs, zz = quat.z * zs;

	mat.x.x = 1.0f - (yy + zz); mat.y.x = xy - wz;          mat.z.x = xz + wy;
	mat.x.y = xy + wz;          mat.y.y = 1.0f - (xx + zz); mat.z.y = yz - wx;
	mat.x.z = xz - wy;          mat.y.z = yz + wx;          mat.z.z = 1.0f - (xx + yy);
	mat.x.w =                   mat.y.w =                   mat.z.w = 0.0f;
	return mat;
}
m3x3     Rotation3x3(in v4 quat)
{
	m3x3 mat;
	return Rotation3x3(mat, quat);
}
ref m4x4 Rotation4x4(ref m4x4 mat, in v4 quat, in v4 translation)
{
	Rotation3x3(mat.rot, quat);
	mat.pos = translation;
	return mat;
}
m4x4     Rotation4x4(in v4 quat, in v4 translation)
{
	m4x4 mat;
	return Rotation4x4(mat, quat, translation);
}
ref m3x3 Scale3x3(ref m3x3 mat, float scale)
{
	mat = m3x3.zero;
	mat.x.x = mat.y.y = mat.z.z = scale;
	return mat;
}
m3x3     Scale3x3(float scale)
{
	m3x3 mat;
	return Scale3x3(mat, scale);
}
ref m3x3 Scale3x3(ref m3x3 mat, float sx, float sy, float sz)
{
	mat = m3x3.zero;
	mat.x.x = sx;
	mat.y.y = sy;
	mat.z.z = sz;
	return mat;
}
m3x3     Scale3x3(float sx, float sy, float sz)
{
	m3x3 mat;
	return Scale3x3(mat, sx, sy, sz);
}
ref m4x4 Scale4x4(ref m4x4 mat, float scale, in v4 translation)
{
	Scale3x3(mat.rot, scale);
	mat.pos = translation;
	return mat;
}
m4x4     Scale4x4(float scale, in v4 translation)
{
	m4x4 mat;
	return Scale4x4(mat, scale, translation);
}
ref m4x4 Scale4x4(ref m4x4 mat, float sx, float sy, float sz, in v4 translation)
{
	Scale3x3(mat.rot, sx, sy, sz);
	mat.pos = translation;
	return mat;
}
m4x4     Scale4x4(float sx, float sy, float sz, in v4 translation)
{
	m4x4 mat;
	return Scale4x4(mat, sx, sy, sz, translation);
}
ref m3x3 Shear3x3(ref m3x3 mat, float sxy, float sxz, float syx, float syz, float szx, float szy)
{
	mat.x.set(1.0f, sxy, sxz, 0.0f);
	mat.y.set(syx, 1.0f, syz, 0.0f);
	mat.z.set(szx, szy, 1.0f, 0.0f);
	return mat;
}
m3x3     Shear3x3(float sxy, float sxz, float syx, float syz, float szx, float szy)
{
	m3x3 mat;
	return Shear3x3(mat, sxy, sxz, syx, syz, szx, szy);
}
ref m4x4 Shear4x4(ref m4x4 mat, float sxy, float sxz, float syx, float syz, float szx, float szy, in v4 translation)
{
	Shear3x3(mat.rot, sxy, sxz, syx, syz, szx, szy);
	mat.pos = translation;
	return mat;
}
m4x4     Shear4x4(float sxy, float sxz, float syx, float syz, float szx, float szy, in v4 translation)
{
	m4x4 mat;
	return Shear4x4(mat, sxy, sxz, syx, syz, szx, szy, translation);
}
ref m4x4 LookAt(ref m4x4 mat, in v4 eye, in v4 at, in v4 up)
{
	assert(!Parallel(at - eye, up), "Lookat point and up axis are aligned");
	mat.z = GetNormal3(eye - at);
	mat.x = GetNormal3(Cross3(up, mat.z));
	mat.y = Cross3(mat.z, mat.x);
	mat.pos = eye;
	return mat;
}
m4x4     LookAt(in v4 eye, in v4 at, in v4 up)
{
	m4x4 mat;
	return LookAt(mat, eye, at, up);
}
ref m4x4 ProjectionOrthographic(ref m4x4 mat, float w, float h, float Znear, float Zfar, bool righthanded)
{
	// Construct an orthographic projection matrix
	float diff = Zfar - Znear;
	mat = m4x4.zero;
	mat.x.x = 2.0f / w;
	mat.y.y = 2.0f / h;
	mat.z.z = sign!(float)(!righthanded) / diff;
	mat.w.w = 1.0f;
	mat.w.z = -Znear / diff;
	return mat;
}
m4x4     ProjectionOrthographic(float w, float h, float Znear, float Zfar, bool righthanded)
{
	m4x4 mat;
	return ProjectionOrthographic(mat, w, h, Znear, Zfar, righthanded);
}
ref m4x4 ProjectionPerspective(ref m4x4 mat, float w, float h, float Znear, float Zfar, bool righthanded)
{
	// Construct a perspective projection matrix
	float zn   = 2.0f * Znear;
	float diff = Zfar - Znear;
	mat = m4x4.zero;
	mat.x.x = zn / w;
	mat.y.y = zn / h;
	mat.z.w = sign!(float)(!righthanded);
	mat.z.z = mat.z.w * Zfar / diff;
	mat.w.z = -Znear * Zfar / diff;
	return mat;
}
m4x4     ProjectionPerspective(float w, float h, float Znear, float Zfar, bool righthanded)
{
	m4x4 mat;
	return ProjectionPerspective(mat, w, h, Znear, Zfar, righthanded);
}
ref m4x4 ProjectionPerspective(ref m4x4 mat, float l, float r, float t, float b, float Znear, float Zfar, bool righthanded)
{
	// Construct a perspective projection matrix offset from the centre
	float zn   = 2.0f * Znear;
	float diff = Zfar - Znear;
	mat = m4x4.zero;
	mat.x.x = zn / (r - l);
	mat.y.y = zn / (t - b);
	mat.z.x = (l+r)/(l-r);
	mat.z.y = (t+b)/(b-t);
	mat.z.w = sign!(float)(!righthanded);
	mat.z.z = mat.z.w * Zfar / diff;
	mat.w.z = -Znear * Zfar / diff;
	return mat;
}
m4x4     ProjectionPerspective(float l, float r, float t, float b, float Znear, float Zfar, bool righthanded)
{
	m4x4 mat;
	return ProjectionPerspective(mat, l, r, t, b, Znear, Zfar, righthanded);
}
ref m4x4 ProjectionPerspectiveFOV(ref m4x4 mat, float fovY, float aspect, float Znear, float Zfar, bool righthanded)
{
	// Construct a perspective projection matrix using field of view
	float diff = Zfar - Znear;
	mat = m4x4.zero;
	mat.y.y = 1.0f / tan(fovY/2);
	mat.x.x = mat.y.y / aspect;
	mat.z.w = sign!(float)(!righthanded);
	mat.z.z = mat.z.w * Zfar / diff;
	mat.w.z = -Znear * Zfar / diff;
	return mat;
}
m4x4     ProjectionPerspectiveFOV(float fovY, float aspect, float Znear, float Zfar, bool righthanded)
{
	m4x4 mat;
	return ProjectionPerspectiveFOV(mat, fovY, aspect, Znear, Zfar, righthanded);
}
ref m4x4 CrossProductMatrix4x4(ref m4x4 mat, in v4 vec)
{
	// Return the cross product matrix for 'vec'. This matrix can be used to take the
	// cross product of another vector: e.g. Cross(v1, v2) == CrossProductMatrix4x4(v1) * v2
	return mat.set(
		v4(    0f,  vec.z, -vec.y, 0f),
		v4(-vec.z,     0f,  vec.x, 0f),
		v4( vec.y, -vec.x,     0f, 0f),
		v4.zero);
}
m4x4     CrossProductMatrix4x4(in v4 vec)
{
	m4x4 mat;
	return CrossProductMatrix4x4(mat, vec);
}
ref m4x4 OrientationFromDirection(ref m4x4 orientation, in v4 direction, int axis, v4 preferred_up)
{
	//// Make an orientation matrix from a direction. Note the rotation around the direction
	//// vector is not defined. 'axis' is the axis that 'direction' will become
	//if (Parallel(preferred_up, direction)) preferred_up = Perpendicular(direction);
	//orientation[ axis     ] = GetNormal3(direction);
	//orientation[(axis+1)%3] = GetNormal3(Cross3(preferred_up, orientation[axis]));
	//orientation[(axis+2)%3] = Cross3(orientation[axis], orientation[(axis+1)%3]);
	//orientation.pos = v4.origin;
	return orientation;
}
ref m4x4 OrientationFromDirection(ref m4x4 orientation, v4 direction, int axis)
{
	return OrientationFromDirection(orientation, direction, axis, Perpendicular(direction));
}
m4x4     OrientationFromDirection4x4(in v4 direction, int axis)
{
	m4x4 mat;
	return OrientationFromDirection(mat, direction, axis);
}
m4x4     Sqrt(in m4x4 mat)
{
	// Return the square root of a matrix. The square root is the matrix B where B.B = mat.
	// Using Denman-Beavers square root iteration. Should converge quadratically
	m4x4 Y = mat;            // Converges to mat^0.5
	m4x4 Z = m4x4.identity;  // Converges to mat^-0.5
	for (int i = 0; i != 10; ++i)
	{
		m4x4 Y_next = 0.5 * (Y + GetInverse(Z));
		m4x4 Z_next = 0.5 * (Z + GetInverse(Y));
		Y = Y_next;
		Z = Z_next;
	}
	return Y;
}

v4       Quat(in v4 axis, float angle)
{
	// Create a quaternion from an axis and an angle
	float s = sin(0.5f*angle);
	return v4(s*axis.x, s*axis.y, s*axis.z, cos(0.5f*angle));
}
v4       Quat(float pitch, float yaw, float roll)
{
	float cos_r = cos(roll  * 0.5f), sin_r = sin(roll  * 0.5f);
	float cos_p = cos(pitch * 0.5f), sin_p = sin(pitch * 0.5f);
	float cos_y = cos(yaw   * 0.5f), sin_y = sin(yaw   * 0.5f);
	return v4(
		cos_r * sin_p * cos_y + sin_r * cos_p * sin_y,
		cos_r * cos_p * sin_y - sin_r * sin_p * cos_y,
		sin_r * cos_p * cos_y - cos_r * sin_p * sin_y,
		cos_r * cos_p * cos_y + sin_r * sin_p * sin_y);
}
v4       Quat(in m3x3 m)
{
	// Create a quaternion from a rotation matrix
	float trace = m.x.x + m.y.y + m.z.z;
	if (trace >= 0.0f)
	{
		float r = sqrt(1.0f + trace);
		float s = 0.5f / r;
		return v4(
			(m.z.y - m.y.z) * s,
			(m.x.z - m.z.x) * s,
			(m.y.x - m.x.y) * s,
			0.5f * r);
	}
	else
	{
		int i = 0;
		if (m.y.y > m.x.x  ) ++i;
		if (m.z.z > m[i][i]) ++i;
		int j = (i + 1) % 3;
		int k = (i + 2) % 3;
		float r = sqrt(m[i][i] - m[j][j] - m[k][k] + 1.0f);
		float s = 0.5f / r;
		v4 q;
		q[i] = 0.5f * r;
		q[j] = (m[i][j] + m[j][i]) * s;
		q[k] = (m[k][i] + m[i][k]) * s;
		q.w  = (m[k][j] - m[j][k]) * s;
		return q;
	}
}
v4       Quat(in v4 from, in v4 to)
{
	// Construct a quaternion from two vectors represent start and end orientations
	float d = Dot3(from, to); 
	v4 axis = Cross3(from, to);
	float s = sqrt(from.length3sq * to.length3sq) + d;
	if (feql(s, 0.0f)) { axis = Perpendicular(to); s = 0.0f; } // vectors are 180 degrees apart
	return v4.normal4(axis.x, axis.y, axis.z, s);
}
v4       QConjugate(in v4 quat)
{
	return v4(-quat.x, -quat.y, -quat.z, quat.w);
}
v4       QMul(in v4 lhs, in v3 rhs)
{
	// Quaternion multiply. Same sematics at matrix multiply
	return v4(
		 lhs.w*rhs.x + lhs.y*rhs.z - lhs.z*rhs.y,
		 lhs.w*rhs.y - lhs.x*rhs.z + lhs.z*rhs.x,
		 lhs.w*rhs.z + lhs.x*rhs.y - lhs.y*rhs.x,
		-lhs.x*rhs.x - lhs.y*rhs.y - lhs.z*rhs.z);
}
v4       QMul(in v4 lhs, in v4 rhs)
{
	// Quaternion multiply. Same sematics at matrix multiply
	return v4(
		lhs.w*rhs.x + lhs.x*rhs.w + lhs.y*rhs.z - lhs.z*rhs.y,
		lhs.w*rhs.y - lhs.x*rhs.z + lhs.y*rhs.w + lhs.z*rhs.x,
		lhs.w*rhs.z + lhs.x*rhs.y - lhs.y*rhs.x + lhs.z*rhs.w,
		lhs.w*rhs.w - lhs.x*rhs.x - lhs.y*rhs.y - lhs.z*rhs.z);
}
v4       QAxisAngle(in v4 quat)
{
	// Return the axis and angle from a quaternion, xyz = axis, w = angle
	float ang = 2.0f * acos(quat.w);
	float s = sqrt(1f - quat.w*quat.w); assert(s != 0f);
	return v4(quat.x/s, quat.y/s, quat.z/s, ang);
}
v4       QSlerp(in v4 src, in v4 dst, float frac)
{
	// Spherically interpolate between quaternions
	if (frac < 0.0f) return src;
	if (frac > 1.0f) return dst;
	
	// Calculate cosine
	float cos_angle  = Dot4(src, dst);
	float sign       = sign!(float)(cos_angle >= 0);
	const v4 abs_dst = sign * dst;
	cos_angle        *= sign;

	// Calculate coefficients
	if (1.0f - cos_angle < 0.05f) // "src" and "dst" quaternions are very close 
		return GetNormal4!(v4)(Lerp(src, abs_dst, frac));
	else
	{
		// standard case (slerp)
		float angle     = acos(cos_angle);
		float sin_angle = sin (angle);
		float scale0    = sin ((1f - frac) * angle);
		float scale1    = sin ((     frac) * angle);
		return (scale0*src + scale1*abs_dst) * (1.0f / sin_angle);
	}
}
v4       QQuatRotate(in v4 rotator, in v4 rotatee)
{
	// Rotate 'rotatee' by 'rotator'
	assert(feql(rotator.length4sq, 1.0f), "Non-unit quaternion used for rotation");
	return QMul(QMul(rotator, rotatee), QConjugate(rotator));
}
v3       QVecRotate(in v4 rotator, in v3 rotatee)
{
	// Rotate 'rotatee' by 'rotator'
	assert(feql(rotator.length4sq, 1.0f), "Non-unit quaternion used for rotation");
	return QMul(QMul(rotator, rotatee), QConjugate(rotator)).xyz;
}
v4       QVecRotate(in v4 rotator, in v4 rotatee)
{
	// Rotate 'rotatee' by 'rotator'
	return v4(QVecRotate(rotator, rotatee.xyz), rotatee.w);
}

unittest
{
	write("pr.maths unittest ... ");
}
unittest
{//scaler
	assert(feql(0.001f, 0.002f, 0.01f));
	assert(feql(Length2sq(3f,4f), 5f*5f));
	assert(feql(Length3sq(3f,4f,5f), 50f));
	assert(feql(Length4sq(3f,4f,5f,6f), 86f));
	assert(feql(Length2(3f,4f), 5f));
	assert(feql(Length3(3f,4f,5f), sqrt(50f)));
	assert(feql(Length4(3f,4f,5f,6f), sqrt(86f)));
}
unittest
{//v2
	assert(v2(3f).length2 == sqrt(18f));
	assert(v2(3f,4f).length2 == 5f);
}
unittest
{//v4
	v4 z = v4.zero;

	assert(cast(void*)&z == cast(void*)&z.x);
	assert(cast(void*)&z == cast(void*)&z.xyz);
	assert(cast(void*)&z == cast(void*)&z.xy);
	assert(cast(void*)&z == cast(void*)z.arr);

	assert(v4(1f) == v4(1f,1f,1f,1f));
	assert(v4(v2(1f), v2(2f)) == v4(1f,1f,2f,2f));
	
	assert(v4.zero.w1() == v4.origin);
	assert(v4.origin.w0() == v4.zero);
	
	v4 a = v4(1.1f, 2.2f, 3.3f, 4.4f);
	assert(a.ix == 1 && a.iy == 2 && a.iz == 3 && a.iw == 4);
	assert(a.string3 == "1.1 2.2 3.3");
	assert(a.string4 == "1.1 2.2 3.3 4.4");
	assert(feql(a.length3sq, 16.94f));
	assert(feql(a.length4sq, 36.3f));
	assert(feql(a.length3, 4.115823f));
	assert(feql(a.length4, 6.024948f));

	a[0] = -1.1f;
	a[3] = -4.4f;
	assert(a.x == -1.1f);
	assert(a.w == -4.4f);

	a[0] *= -1f;
	a[3] *= -1f;
	assert(a.x == 1.1f);
	assert(a.w == 4.4f);

	a += 1f; assert(feql(a, v4(2.1f, 3.2f, 4.3f, 5.4f)));
	a -= 1f; assert(feql(a, v4(1.1f, 2.2f, 3.3f, 4.4f)));
	a *= 2f; assert(feql(a, v4(2.2f, 4.4f, 6.6f, 8.8f)));
	a /= 2f; assert(feql(a, v4(1.1f, 2.2f, 3.3f, 4.4f)));
	a %= 1f; assert(feql(a, v4(0.1f, 0.2f, 0.3f, 0.4f)));
	
	a += v4(1f); assert(feql(a, v4(1.1f, 1.2f, 1.3f, 1.4f)));
	a -= v4(1f); assert(feql(a, v4(0.1f, 0.2f, 0.3f, 0.4f)));

	assert(feql(+a, v4(+0.1f, +0.2f, +0.3f, +0.4f)));
	assert(feql(-a, v4(-0.1f, -0.2f, -0.3f, -0.4f)));

	assert(feql(v4(1f)   + v4(2f), v4(  3f)));
	assert(feql(v4(1f)   - v4(2f), v4( -1f)));
	assert(feql(v4(2f)   * v4(2f), v4(  4f)));
	assert(feql(v4(2f)   / v4(2f), v4(  1f)));
	assert(feql(v4(1.1f) % v4(1f), v4(0.1f)));
	
	assert(2f * v4(1f) == v4(2f));

	assert(v4.normal3(2f,0f,0f,0f) == v4(1f,0,0,0));
	assert(v4.normal4(0f,0f,0f,2f) == v4(0,0,0,1f));
	assert(v4.random3(0f) != v4.zero);
	assert(feql(v4.random3(1f, 1f, 0f).length3, 1f));
	assert(feql(v4.random_normal4().length4, 1f));
}
unittest
{// quat
	v4 a2b = v4(-0.57315874f, -0.57733983f, 0.39024505f, 0.43113413f);
	v4 b2c = v4(-0.28671566f, 0.72167641f, -0.59547395f, 0.20588370f);
	v4 a2c = QMul(b2c, a2b);
	assert(feql(a2b.length4sq, 1f));
	assert(feql(b2c.length4sq, 1f));
	assert(feql(a2c.length4sq, 1f));
	v4 a = v4(-7.8858266f, -0.29560062f, 6.0255852f, 1.0f);
	v4 b = QVecRotate(a2b, a);
	v4 c = QVecRotate(b2c, b);
	v4 d = QVecRotate(a2c, a);
	assert(feql(c, d));

	float ang = FRand(-1.0f, 1.0f);
	v4 axis = v4.random_normal3();
	v4   q = Quat(axis, ang);
	m4x4 m = Rotation4x4(axis, ang, v4.origin);
	m4x4 M = Rotation4x4(q, v4.origin);
	assert(feql(M, m));

	b = m * a;
	c = QVecRotate(q, a);
	assert(feql(b, c));

	v4 axis1 = v4.random_normal3();
	v4 axis2 = v4.random_normal3();
	float ang1 = FRand(-1f, 1f);
	float ang2 = FRand(-1f, 1f);

	m4x4 m_a2b = Rotation4x4(axis1, ang1, v4.origin);
	m4x4 m_b2c = Rotation4x4(axis2, ang2, v4.origin);
	m4x4 m_a2c = m_b2c * m_a2b;

	v4 q_a2b = Quat(axis1, ang1);
	v4 q_b2c = Quat(axis2, ang2);
	v4 q_a2c = QMul(q_b2c, q_a2b);

	b = m_a2c * a;
	c = QVecRotate(q_a2c, a);
	assert(feql(b, c));

	a = v4.random_normal3();
	b = v4.random_normal3();
	q_a2b = Quat(a, b);
	assert(feql(QConjugate(QConjugate(q_a2b)), q_a2b));
	c = QVecRotate(q_a2b, a);
	assert(feql(b, c));

	q = Quat(axis, ang);
	d = QAxisAngle(q);
	assert(	(feql( d.xyz, axis.xyz, 0.001f) && feql( d.w, ang, 0.001f)) ||
			(feql(-d.xyz, axis.xyz, 0.001f) && feql(-d.w, ang, 0.001f)) );
}
unittest
{// m4x4
	m4x4 m1 = m4x4(v4.xaxis, v4.yaxis, v4.zaxis, v4(1.0f, 2.0f, 3.0f, 1.0f));
	m4x4 m2 = Translation(v3(1.0f, 2.0f, 3.0f));
	m4x4 m3 = Translation(v4(1.0f, 2.0f, 3.0f, 0.0f));
	assert(feql(m1, m2));
	assert(feql(m1, m3));

	m4x4 a2b = Rotation4x4(v4.random_normal3(), FRand(-pi, pi), v4.random3(0.0f, 10.0f, 1.0f));
	m4x4 b2c = Rotation4x4(v4.random_normal3(), FRand(-pi, pi), v4.random3(0.0f, 10.0f, 1.0f));
	assert(IsOrthonormal(a2b));
	assert(IsOrthonormal(b2c));

	v4 a = v4.random3(0.0f, 10.0f, 1.0f);
	v4 b = a2b * a;
	v4 c = b2c * b;

	m4x4 a2c = b2c * a2b;
	v4 d = a2c * a;
	assert(feql(c, d));

	m3x3 m4 = Rotation3x3(1.0f, 0.5f, 0.7f);
	m3x3 m5 = Rotation3x3(Quat(1.0f, 0.5f, 0.7f));
	assert(IsOrthonormal(m4));
	assert(IsOrthonormal(m5));
	assert(feql(m4, m5));

	float ang = FRand(-1.0f, 1.0f);
	v4 axis = v4.random_normal3();
	m1 = Rotation4x4(axis, ang, v4.origin);
	m2 = Rotation4x4(Quat(axis, ang), v4.origin);
	assert(IsOrthonormal(m1));
	assert(IsOrthonormal(m2));
	assert(feql(m1, m2));

	a2b = m4x4(
		v4( 0.58738488f,  0.60045743f,  0.54261398f, 0.0f),
		v4(-0.47383153f,  0.79869330f, -0.37090793f, 0.0f),
		v4(-0.65609658f, -0.03924191f,  0.75365603f, 0.0f),
		v4( 0.09264841f,  6.84435890f,  3.09618950f, 1.0f));
	m4x4 b2a = GetInverse(a2b);
	m4x4 id = a2b * b2a;
	assert(feql(id, m4x4.identity));

	m4x4 b2a_fast = GetInverseFast(a2b);
	assert(feql(b2a_fast, b2a));

	a2b.x.set(-2.0f, 3.0f, 1.0f, 0.0f);
	a2b.y.set( 4.0f,-1.0f, 2.0f, 0.0f);
	a2b.z.set( 1.0f,-2.0f, 4.0f, 0.0f);
	a2b.w.set( 1.0f, 2.0f, 3.0f, 1.0f);
	assert(IsOrthonormal(Orthonormalise(a2b)));
}
unittest
{
	writeln("complete");

	//TEST(m4x4GetAxisAngle)
	//{
	//	CHECK(true);
	//	//float ang = FRand(-1.0f, 1.0f);
	//	//v4 axis = v4RandomNormal3(0.0f);
	//	//m4x4 a2b; Rotation(a2b, axis, ang);

	//	//v4 X = v4XAxis;
	//	//v4 Xprim = a2b * X;

	//	//v4 Y = v4YAxis;
	//	//v4 Yprim = a2b * Y;

	//	//v4 Z = v4ZAxis;
	//	//v4 Zprim = a2b * Z;

	//	//v4 XcXp = Cross3(X, Xprim).Normalise3();
	//	//v4 YcYp = Cross3(Y, Yprim).Normalise3();
	//	//v4 ZcZp = Cross3(Z, Zprim).Normalise3();

	//	//v4 axis_out = Cross3(X_Xp, Y_Yp);
	//	//axis_out.Normalise3();

	//	//axis_out = axis_out;


	//	//float det4 = a2b.Determinant4(); det4;
	//	//float det3 = a2b.Determinant3(); det3;
	//	//CHECK(FEql(det3, det4));

	//	//float angle_out;
	//	//v4 axis_out;
	//	//a2b.GetAxisAngle(axis_out, angle_out);

	//	//bool correct = (FEql(ang,  0.0f,      0.001f) && FEql (angle_out, 0.0f, 0.001f)) ||
	//	//			   (FEql(ang,  angle_out, 0.001f) && FEql3(axis,  axis_out, 0.01f)) ||
	//	//			   (FEql(ang, -angle_out, 0.001f) && FEql3(axis, -axis_out, 0.01f));
	//	//CHECK(correct);
	//}


	//// Stat ***************************************************************************
	//TEST(Stat)
	//{
	//	{
	//		const double num[] = {2.0,4.0,7.0,3.0,2.0,-5.0,-4.0,1.0,-7.0,3.0,6.0,-8.0};
	//		const int count = sizeof(num)/sizeof(num[0]);
	//		Stat s;
	//		for( int i = 0; i != count; ++i )
	//			s.Add(num[i]);
	//	
	//		CHECK(s.Count()					== count);
	//		CHECK(FEql(s.Sum()				,4.0		,pr::maths::dbl_tiny));
	//		CHECK(FEql(s.Minimum()			,-8.0		,pr::maths::dbl_tiny));
	//		CHECK(FEql(s.Maximum()			,7.0		,pr::maths::dbl_tiny));
	//		CHECK(FEql(s.Mean()				,1.0/3.0	,pr::maths::dbl_tiny));
	//		CHECK(FEql(s.StandardDeviation(),4.83621	,0.00001));
	//		CHECK(FEql(s.StandardVariance()	,23.38889	,0.00001));
	//		CHECK(FEql(s.SampleDeviation()	,5.0512524699475787686684767441111 ,pr::maths::dbl_tiny));
	//		CHECK(FEql(s.SampleVariance()	,25.515151515151515151515151515152 ,pr::maths::dbl_tiny));
	//	}
	//	{
	//		const double num[] = {-0.50, 0.06, -0.31, 0.31, 0.09, -0.02, -0.15, 0.40, 0.32, 0.25, -0.33, 0.36, 0.21, 0.01, -0.20, -0.49, -0.41, -0.14, -0.35, -0.33};
	//		const int count = sizeof(num)/sizeof(num[0]);
	//		Stat s;
	//		for( int i = 0; i != count; ++i )
	//			s.Add(num[i]);
	//	
	//		CHECK(s.Count()					== count);
	//		CHECK(FEql(s.Sum()				,-1.22		,pr::maths::dbl_tiny));
	//		CHECK(FEql(s.Minimum()			,-0.5		,pr::maths::dbl_tiny));
	//		CHECK(FEql(s.Maximum()			,0.4		,pr::maths::dbl_tiny));
	//		CHECK(FEql(s.Mean()				,-0.0610	,0.00001));
	//		CHECK(FEql(s.StandardDeviation(),0.29233	,0.00001));
	//		CHECK(FEql(s.StandardVariance()	,0.08546	,0.00001));
	//		CHECK(FEql(s.SampleDeviation()	,0.29993	,0.00001));
	//		CHECK(FEql(s.SampleVariance()	,0.08996	,0.00001));
	//	}
	//}

	//// Frustum ***************************************************************************
	//TEST(Frustum)
	//{
	//	float aspect = 1.4f;
	//	float fovY = pr::maths::pi / 3.0f;
	//	pr::Frustum f = pr::Frustum::makeFA(fovY, aspect, 0.0f);

	//	CHECK(FEql(f.Width(), 0.0f));
	//	CHECK(FEql(f.Height(), 0.0f));
	//	CHECK(FEql(f.FovY(), fovY));
	//	CHECK(FEql(f.Aspect(), aspect));

	//	f.ZDist(1.0f);
	//	CHECK(FEql(f.Width() , 2.0f * pr::Tan(0.5f * fovY) * aspect));
	//	CHECK(FEql(f.Height(), 2.0f * pr::Tan(0.5f * fovY)));
	//}


	//// Geometry ***************************************************************************
	//TEST(Geometry)
	//{
	//	{// Intersect_LineToTriangle
	//		pr::v4 a = v4::make(-1.0f, -1.0f,  0.0f, 1.0f);
	//		pr::v4 b = v4::make( 1.0f, -1.0f,  0.0f, 1.0f);
	//		pr::v4 c = v4::make( 0.0f,  1.0f,  0.0f, 1.0f);
	//		pr::v4 s = v4::make( 0.0f,  0.0f,  1.0f, 1.0f);
	//		pr::v4 e = v4::make( 0.0f,  0.0f, -1.0f, 1.0f);
	//		pr::v4 e2= v4::make( 0.0f,  1.0f,  1.0f, 1.0f);
	//		
	//		float t = 0, f2b = 0; pr::v4 bary = pr::v4Zero;
	//		CHECK(pr::Intersect_LineToTriangle(s, e, a, b, c, &t, &bary, &f2b));
	//		CHECK(pr::FEql3(bary, pr::v4::make(0.25f, 0.25f, 0.5f, 0.0f)));
	//		CHECK(pr::FEql(t, 0.5f));
	//		CHECK(f2b == 1.0f);

	//		CHECK(pr::Intersect_LineToTriangle(e, s, a, b, c, &t, &bary, &f2b));
	//		CHECK(pr::FEql3(bary, pr::v4::make(0.25f, 0.25f, 0.5f, 0.0f)));
	//		CHECK(pr::FEql(t, 0.5f));
	//		CHECK(f2b == -1.0f);

	//		CHECK(!pr::Intersect_LineToTriangle(s, e, a, b, c, 0, 0, 0, 0.7f, 1.0f));
	//		CHECK(!pr::Intersect_LineToTriangle(s, e, a, b, c, 0, 0, 0, 0.0f, 0.3f));
	//		CHECK(!pr::Intersect_LineToTriangle(s, e2, a, b, c));
	//	}
	//}
}
	
