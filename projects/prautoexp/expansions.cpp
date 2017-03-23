//***********************************************************
// PR AutoExp
// Copyright (c) Rylogic Ltd 2002
//***********************************************************

#include <atomic>
#include <windows.h>
#include <fstream>
#include <stdio.h>
#include <string.h>
#include <tchar.h>
#include <malloc.h>
#include <string>
#include "pr/common/datetime.h"
#include "pr/common/fmt.h"
#include "pr/common/alloca.h"
#include "pr/common/cast.h"
#include "pr/macros/link.h"
#include "pr/maths/maths.h"
#include "pr/maths/spatial.h"
#include "pr/maths/large_int.h"
#include "pr/physics/physics.h"
#include "pr/lua/lua.h"
#include "lstate.h"
#include "prautoexp/expansions.h"

using namespace pr;

// Helper for debugging expansion functions.
// Stops the debugger expanding types while in expansion functions
struct ReentryGuard
{
#ifdef _DEBUG
	std::atomic_flag& guard() { static std::atomic_flag m_guard; return m_guard; }
	ReentryGuard()            { if (guard().test_and_set()) throw std::exception(); } // Throws if already set
	~ReentryGuard()           { guard().clear(); }
#else
	ReentryGuard() {} // prevents unused local variable warning
#endif
};

/*
This is specific to the VS2013 implementation of std::string/wstring which changed
in VS2015. Using visualizers now anyway.

// Helper for std::string and std::wstring
template <typename Str> struct stdstring :Str
{
	 Read an std::string/wstring from debuggee memory
	HRESULT Read(DbgHelper* pHelper, Str& str, size_t ofs)
	{
		// Read the std::string/wstring structure.
		// Internal pointers will still point off into inaccessible memory
		if (FAILED(pHelper->Read(*this, ofs))) return E_FAIL;
		if (_Mysize > _Myres) return E_FAIL;
		if (_Myres == _BUF_SIZE-1) str.append(_Bx._Buf, _Mysize);
		else
		{
			size_t size = (_Mysize > 1024) ? 1024 : _Mysize;
			Str::value_type* buf = (Str::value_type*)_alloca(size);
			if (FAILED(pHelper->Read(buf, size, _Bx._Ptr))) return E_FAIL;
			str.append(buf, size);
		}
		return S_OK;
	}
};
template <> HRESULT DbgHelper::Read(std::string& str, size_t ofs)
{
	stdstring<std::string> s;
	return s.Read(this, str, ofs);
}
template <> HRESULT DbgHelper::Read(std::wstring& str, size_t ofs)
{
	stdstring<std::wstring> s;
	return s.Read(this, str, ofs);
}
*/
// Expansions ***********************************************

// Expand a v2
ADDIN_API HRESULT WINAPI AddIn_v2(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::v2 vec;
	if (FAILED(pHelper->Read(vec))) return E_FAIL;
	_snprintf(pResult, max, "{%+g %+g} Len2=%g", vec.x, vec.y, pr::Length2(vec));
	return S_OK;
}

// Expand a v3
ADDIN_API HRESULT WINAPI AddIn_v3(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::v3 vec;
	if (FAILED(pHelper->Read(vec))) return E_FAIL;
	_snprintf(pResult, max, "{%+g %+g %+g} Len3=%g", vec.x, vec.y, vec.z, Length3(vec));
	return S_OK;
}

// Expand a v4
ADDIN_API HRESULT WINAPI AddIn_v4(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::v4 vec;
	if (FAILED(pHelper->Read(vec))) return E_FAIL;
	_snprintf(pResult, max, "{%+g %+g %+g %+g} Len3=%g Len4=%g", vec.x, vec.y, vec.z, vec.w, Length3(vec), Length4(vec));
	return S_OK;
}

// Expand a v8
ADDIN_API HRESULT WINAPI AddIn_v8(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::v8 vec;
	if (FAILED(pHelper->Read(vec))) return E_FAIL;
	_snprintf(pResult, max, "{{%+g %+g %+g}  {%+g %+g %+g}}", vec.ang.x, vec.ang.y, vec.ang.z, vec.lin.x, vec.lin.y, vec.lin.z);
	return S_OK;
}

// Expand an iv2
ADDIN_API HRESULT WINAPI AddIn_iv2(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::iv2 vec;
	if (FAILED(pHelper->Read(vec))) return E_FAIL;
	_snprintf(pResult, max, "{%+d %+d} Len2=%g", vec.x, vec.y, Length2(vec));
	return S_OK;
}

// Expand an iv4
ADDIN_API HRESULT WINAPI AddIn_iv4(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::iv4 vec;
	if (FAILED(pHelper->Read(vec))) return E_FAIL;
	_snprintf(pResult, max, "{%+d %+d %+d %+d} Len3=%g Len4=%g", vec.x, vec.y, vec.z, vec.w, Length3(vec), Length4(vec));
	return S_OK;
}

// Expand an i64v4
ADDIN_API HRESULT WINAPI AddIn_i64v4(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::int64 vec[4];
	if (FAILED(pHelper->Read(vec))) return E_FAIL;
	auto len3 = static_cast<double>(pr::Len3(vec[0], vec[1], vec[2]));
	auto len4 = static_cast<double>(pr::Len4(vec[0], vec[1], vec[2], vec[3]));
	_snprintf(pResult, max, "{%+lld %+lld %+lld %+lld} Len3=%g Len4=%g", vec[0], vec[1], vec[2], vec[3], len3, len4);
	return S_OK;
}

// Expand a m2x2
ADDIN_API HRESULT WINAPI AddIn_m2x2(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::m2x2 mat;
	if (FAILED(pHelper->Read(mat))) return E_FAIL;
	float ortho = Cross2(Normalise2(mat.x), Normalise2(mat.y));
	if (mat == pr::m2x2Identity)
		_snprintf(pResult, max, "identity 2x2");
	else
		_snprintf(pResult, max,
			"{%+g %+g} \n"
			"{%+g %+g} \n"
			"Len={%+g %+g} \n"
			"Ortho=%g Det=%g \n"
			,mat.x.x ,mat.x.y
			,mat.y.x ,mat.y.y
			,Length2(mat.x), Length2(mat.y)
			,ortho, Determinant(mat));
	return S_OK;
}

// Expand a m3x4
ADDIN_API HRESULT WINAPI AddIn_m3x4(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::m3x4 mat;
	if (FAILED(pHelper->Read(mat))) return E_FAIL;
	float ortho = Length3(Cross3(Normalise3(mat.x), Normalise3(mat.y)) - Normalise3(mat.z));
	if (mat == pr::m3x4Identity)
		_snprintf(pResult, max, "identity 3x4");
	else
		_snprintf(pResult, max,
			"{%+g %+g %+g} \n"
			"{%+g %+g %+g} \n"
			"{%+g %+g %+g} \n"
			"Len={%+g %+g %+g} \n"
			"Ortho=%g Det=%g \n"
			,mat.x.x ,mat.x.y ,mat.x.z
			,mat.y.x ,mat.y.y ,mat.y.z
			,mat.z.x ,mat.z.y ,mat.z.z
			,Length3(mat.x), Length3(mat.y), Length3(mat.z)
			,ortho, Determinant(mat));
	return S_OK;
}

// Expand a m4x4
ADDIN_API HRESULT WINAPI AddIn_m4x4(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::m4x4 mat;
	if (FAILED(pHelper->Read(mat))) return E_FAIL;
	float ortho = Length3(Cross3(Normalise3(mat.x), Normalise3(mat.y)) - Normalise3(mat.z));
	if (mat == m4x4Identity)
		_snprintf(pResult, max, "identity 4x4");
	else
		_snprintf(pResult, max,
			"{%+g %+g %+g %+g} \n"
			"{%+g %+g %+g %+g} \n"
			"{%+g %+g %+g %+g} \n"
			"{%+g %+g %+g %+g} \n"
			"Len={%+g %+g %+g %+g} \n"
			"Ortho=%g Det=%g \n"
			,mat.x.x ,mat.x.y ,mat.x.z ,mat.x.w
			,mat.y.x ,mat.y.y ,mat.y.z ,mat.y.w
			,mat.z.x ,mat.z.y ,mat.z.z ,mat.z.w
			,mat.w.x ,mat.w.y ,mat.w.z ,mat.w.w
			,Length3(mat.x), Length3(mat.y), Length3(mat.z) ,Length3(mat.w)
			,ortho ,Determinant4(mat));
	return S_OK;
}

// Expand a m6x8
ADDIN_API HRESULT WINAPI AddIn_m6x8(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::m6x8 mat;
	if (FAILED(pHelper->Read(mat))) return E_FAIL;
	if (mat == m6x8Identity)
		_snprintf(pResult, max, "identity 6x8");
	else
		_snprintf(pResult, max,
			"{%+g %+g %+g  %+g %+g %+g} \n"
			"{%+g %+g %+g  %+g %+g %+g} \n"
			"{%+g %+g %+g  %+g %+g %+g} \n"
			"{%+g %+g %+g  %+g %+g %+g} \n"
			"{%+g %+g %+g  %+g %+g %+g} \n"
			"{%+g %+g %+g  %+g %+g %+g} \n"
			,mat.m11.x.x ,mat.m11.x.y ,mat.m11.x.z ,mat.m12.x.x ,mat.m12.x.y ,mat.m12.x.z
			,mat.m11.y.x ,mat.m11.y.y ,mat.m11.y.z ,mat.m12.y.x ,mat.m12.y.y ,mat.m12.y.z
			,mat.m11.z.x ,mat.m11.z.y ,mat.m11.z.z ,mat.m12.z.x ,mat.m12.z.y ,mat.m12.z.z
			,mat.m21.x.x ,mat.m21.x.y ,mat.m21.x.z ,mat.m22.x.x ,mat.m22.x.y ,mat.m22.x.z
			,mat.m21.y.x ,mat.m21.y.y ,mat.m21.y.z ,mat.m22.y.x ,mat.m22.y.y ,mat.m22.y.z
			,mat.m21.z.x ,mat.m21.z.y ,mat.m21.z.z ,mat.m22.z.x ,mat.m22.z.y ,mat.m22.z.z
			);
	return S_OK;
}

// Expand a MAX Matrix3
ADDIN_API HRESULT WINAPI AddIn_MAXMatrix3(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	float mat[3][4];
	if (FAILED(pHelper->Read(mat))) return E_FAIL;
	_snprintf(pResult, max,
		"\r\n%3.3f\t%3.3f\t%3.3f"
		"\r\n%3.3f\t%3.3f\t%3.3f"
		"\r\n%3.3f\t%3.3f\t%3.3f"
		"\r\n%3.3f\t%3.3f\t%3.3f"
		,mat[0][0], mat[1][0], mat[2][0]
	,mat[0][1], mat[1][1], mat[2][1]
	,mat[0][2], mat[1][2], mat[2][2]
	,mat[0][3], mat[1][3], mat[2][3]);
	return S_OK;
}

// Show the size of a vector
ADDIN_API HRESULT WINAPI AddIn_stdvector(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	DWORD buffer[4];
	if (FAILED(pHelper->Read(buffer))) return E_FAIL;
	_snprintf(pResult, max, "size=%d bytes", buffer[2] - buffer[1]);
	return S_OK;
}

// Show the contents of a std::string
ADDIN_API HRESULT WINAPI AddIn_stdstring(DWORD, DbgHelper* pHelper, int, BOOL, char*pResult, size_t max, DWORD)
{
	(void)pHelper, pResult, max;
	return E_FAIL;
	// Not supported in VS2015
	//ReentryGuard guard;
	//std::string str;
	//if (FAILED(pHelper->Read(str))) return E_FAIL;
	//_snprintf(pResult, max, "\"%s\"", str.c_str());
	//return S_OK;
}

// inherit std::basic_ios because basic_ios has a protected empty constructor
struct stdbase_ios :std::basic_ios<char, std::char_traits<char> > {};

// Show the state of a std::stringstream
ADDIN_API HRESULT WINAPI AddIn_stdstringstream(DWORD, DbgHelper* pHelper, int, BOOL, char*pResult, size_t max, DWORD)
{
	ReentryGuard guard;

	std::stringstream strm;
	size_t iofs = pr::byte_ptr(static_cast<std::istream::_Myios*>(&strm)) - pr::byte_ptr(&strm);
	size_t oofs = pr::byte_ptr(static_cast<std::ostream::_Myios*>(&strm)) - pr::byte_ptr(&strm);

	stdbase_ios ibase_ios, obase_ios;
	if (FAILED(pHelper->Read(ibase_ios, iofs))) return E_FAIL;
	if (FAILED(pHelper->Read(obase_ios, oofs))) return E_FAIL;

	std::stringstream s;
	s << "in:< ";
	if (ibase_ios.bad() ) s << "bad ";
	if (ibase_ios.eof() ) s << "eof ";
	if (ibase_ios.fail()) s << "fail ";
	if (ibase_ios.good()) s << "good ";
	s << "> out:< ";
	if (ibase_ios.bad() ) s << "bad ";
	if (ibase_ios.eof() ) s << "eof ";
	if (ibase_ios.fail()) s << "fail ";
	if (ibase_ios.good()) s << "good ";
	s << ">";
	_snprintf(pResult, max, "%s", s.str().c_str());
	return S_OK;
}
ADDIN_API HRESULT WINAPI AddIn_stdifstream(DWORD, DbgHelper* pHelper, int, BOOL, char*pResult, size_t max, DWORD)
{
	ReentryGuard guard;

	std::ifstream strm;
	size_t ofs = pr::byte_ptr(static_cast<std::ifstream::_Myios*>(&strm)) - pr::byte_ptr(&strm);

	stdbase_ios base_ios;
	if (FAILED(pHelper->Read(base_ios, ofs))) return E_FAIL;

	std::stringstream s;
	s << "in:< ";
	if (base_ios.bad() ) s << "bad ";
	if (base_ios.eof() ) s << "eof ";
	if (base_ios.fail()) s << "fail ";
	if (base_ios.good()) s << "good ";
	s << ">";
	_snprintf(pResult, max, "%s", s.str().c_str());
	return S_OK;
}
ADDIN_API HRESULT WINAPI AddIn_stdofstream(DWORD, DbgHelper* pHelper, int, BOOL, char*pResult, size_t max, DWORD)
{
	ReentryGuard guard;

	std::ofstream strm;
	size_t ofs = pr::byte_ptr(static_cast<std::ofstream::_Myios*>(&strm)) - pr::byte_ptr(&strm);

	stdbase_ios base_ios;
	if (FAILED(pHelper->Read(base_ios, ofs))) return E_FAIL;

	std::stringstream s;
	s << "out:< ";
	if (base_ios.bad() ) s << "bad ";
	if (base_ios.eof() ) s << "eof ";
	if (base_ios.fail()) s << "fail ";
	if (base_ios.good()) s << "good ";
	s << ">";
	_snprintf(pResult, max, "%s", s.str().c_str());
	return S_OK;
}
//struct stdistream :virtual public std::istream::_Myios // copy of std::basic_istream with public members
//{
//	std::streamsize _Chcount;
//};
//template <> HRESULT DbgHelper::Read(std::istream& istrm, size_t ofs)
//{
//	stdistream* s = reinterpret_cast<stdistream*>(&istrm);
//	std::istream::_Myios* base_ios = static_cast<std::istream::_Myios*>(s);
//
//	if (FAILED(Read(s->_Chcount, ofs + (pr::byte_ptr_cast(&s->_Chcount) - pr::byte_ptr_cast(s))))) return E_FAIL;
//	if (FAILED(Read(*base_ios  , ofs + (pr::byte_ptr_cast(base_ios)     - pr::byte_ptr_cast(s))))) return E_FAIL;
//	base_ios->bad();
//	return S_OK;
//}

// Expand a quaternion
ADDIN_API HRESULT WINAPI AddIn_Quaternion(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::quat q;
	if (FAILED(pHelper->Read(q))) return E_FAIL;
	pr::v4 axis; float angle; AxisAngle(q, axis, angle);
	_snprintf(pResult, max, "%f %f %f %f //Ang: %fdeg Len: %f", q[0], q[1], q[2], q[3], pr::RadiansToDegrees(angle), pr::Length4(q));
	return S_OK;
}

// Expand an MD5 value
ADDIN_API HRESULT WINAPI AddIn_MD5(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::uint8 md5[16];
	if (FAILED(pHelper->Read(md5))) return E_FAIL;
	_snprintf(pResult, max,
		"%0.2x%0.2x%0.2x%0.2x-"
		"%0.2x%0.2x%0.2x%0.2x-"
		"%0.2x%0.2x%0.2x%0.2x-"
		"%0.2x%0.2x%0.2x%0.2x"
		,md5[15], md5[14], md5[13], md5[12]
		,md5[11], md5[10], md5[ 9], md5[ 8]
		,md5[ 7], md5[ 6], md5[ 5], md5[ 4]
		,md5[ 3], md5[ 2], md5[ 1], md5[ 0]);
	return S_OK;
}

// Expand a pr::LargeInt
ADDIN_API HRESULT WINAPI AddIn_LargeInt(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::LargeInt large_int;
	if (FAILED(pHelper->Read(large_int))) return E_FAIL;

	std::string str = pr::ToString(large_int);
	_snprintf(pResult, max, "%s", str.c_str());
	return S_OK;
}

// Expand a pr::Quat as a matrix
ADDIN_API HRESULT WINAPI AddIn_QuaternionAsMatrix(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;
	pr::quat q;
	if (FAILED(pHelper->Read(q))) return E_FAIL;

	pr::m3x4 mat(q);
	_snprintf(pResult, max,
		"\r\n%f\t%f\t%f"
		"\r\n%f\t%f\t%f"
		"\r\n%f\t%f\t%f"
		,mat.x.x, mat.y.x, mat.z.x
		,mat.x.y, mat.y.y, mat.z.y
		,mat.x.z, mat.y.z, mat.z.z);
	return S_OK;
}

// Expand a physics shape
ADDIN_API HRESULT WINAPI AddIn_phShape(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	using namespace pr::ph;
	ReentryGuard guard;

	Shape base;
	if (FAILED(pHelper->Read(base))) return E_FAIL;
	switch (base.m_type)
	{
	case EShape_Sphere:
		{
			ShapeSphere shape;
			if (FAILED(pHelper->Read(shape))) return E_FAIL;
			_snprintf(pResult, max, "Sph(%d): r=%f", (int)shape.m_base.m_size, shape.m_radius);
		}break;
	case EShape_Cylinder:
		{
			ShapeCylinder shape;
			if (FAILED(pHelper->Read(shape))) return E_FAIL;
			_snprintf(pResult, max, "Cyl(%d): r=%f h=%f", (int)shape.m_base.m_size, shape.m_radius, shape.m_height);
		}break;
	case EShape_Box:
		{
			ShapeBox shape;
			if (FAILED(pHelper->Read(shape))) return E_FAIL;
			_snprintf(pResult, max, "Box(%d): w=%f h=%f d=%f", (int)shape.m_base.m_size, shape.m_radius.x, shape.m_radius.y, shape.m_radius.z);
		}break;
	case EShape_Polytope:
		{
			ShapePolytope shape;
			if (FAILED(pHelper->Read(shape))) return E_FAIL;
			_snprintf(pResult, max, "Poly(%d): v=%d f=%d", (int)shape.m_base.m_size, shape.m_vert_count, shape.m_face_count);
		}break;
	case EShape_Triangle:
		{
			ShapeTriangle shape;
			if (FAILED(pHelper->Read(shape))) return E_FAIL;
			_snprintf(pResult, max, "Tri(%d): <%3.3f,%3.3f,%3.3f> <%3.3f,%3.3f,%3.3f> <%3.3f,%3.3f,%3.3f>"
				,(int)shape.m_base.m_size
				,shape.m_v.x.x ,shape.m_v.x.y ,shape.m_v.x.z
				,shape.m_v.y.x ,shape.m_v.y.y ,shape.m_v.y.z
				,shape.m_v.z.x ,shape.m_v.z.y ,shape.m_v.z.z);
		}break;
	case EShape_Terrain:
		{
			ShapeTerrain shape;
			if (FAILED(pHelper->Read(shape))) return E_FAIL;
			_snprintf(pResult, max, "Terr(%d): ", (int)shape.m_base.m_size);
		}break;
	case EShape_Array:
		{
			ShapeArray shape;
			if (FAILED(pHelper->Read(shape))) return E_FAIL;
			_snprintf(pResult, max, "Array(%d): n=%d", (int)shape.m_base.m_size, (int)shape.m_num_shapes);
		}break;
	default:
		_snprintf(pResult, max, "Unknown Shape");
		break;
	}
	return S_OK;
}

// Expand a lua state stack
ADDIN_API HRESULT WINAPI AddIn_LuaState(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;

	lua_State lua_state;
	if (FAILED(pHelper->Read(lua_state))) return E_FAIL;

	std::vector<TValue> stack(lua_state.stacksize);
	if (lua_state.stacksize > 0)
	{
		DWORD got, addr = *(DWORD*)&lua_state.stack;
		if (FAILED(pHelper->ReadDebuggeeMemory(pHelper, addr, (DWORD)(stack.size()*sizeof(TValue)), &stack[0], &got))) return E_FAIL;
		if (got != stack.size()*sizeof(TValue)) return E_FAIL;

		lua_state.top   = &stack[lua_state.top - lua_state.stack];
		lua_state.base  = &stack[lua_state.base - lua_state.stack];
		lua_state.stack = &stack[0];
	}

	int count = lua_gettop(&lua_state);
	std::string s = pr::FmtS("stack: %d", count);
	for (int i = 0; i != count && i != 10; ++i)
	{
		TValue* tv = lua_state.base + i;
		switch (tv->tt)
		{
		case LUA_TNONE:          s += "\n  none";          break;
		case LUA_TNIL:           s += "\n  nil";           break;
		case LUA_TBOOLEAN:       s += "\n  bool";          break;
		case LUA_TNUMBER:        s += "\n  number";        break;
		case LUA_TSTRING:        s += "\n  string";        break;
		case LUA_TTABLE:         s += "\n  table";         break;
		case LUA_TFUNCTION:      s += "\n  function";      break;
		case LUA_TUSERDATA:      s += "\n  userdata";      break;
		case LUA_TTHREAD:        s += "\n  thread";        break;
		case LUA_TLIGHTUSERDATA: s += "\n  lightuserdata"; break;
		default:                 s += "\n  unknown";       break;
		}
	}
	_snprintf(pResult, max, "%s", s.c_str());
	return S_OK;
}

// Expand a pr::DateTime
ADDIN_API HRESULT WINAPI AddIn_DateTime(DWORD, DbgHelper* pHelper, int, BOOL, char *pResult, size_t max, DWORD)
{
	ReentryGuard guard;

	pr::DateTime dt;
	if (FAILED(pHelper->Read(dt))) return E_FAIL;
	//auto tm = dt.utc_time();
	//tm.pretty();
	_snprintf(pResult, max, "%s", dt.ToString().c_str());
	return S_OK;
}
