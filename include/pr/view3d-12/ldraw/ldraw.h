//********************************
// Ldraw Binary Script file format
//  Copyright (c) Rylogic Ltd 2014
//********************************
// Binary 3d model format
#pragma once
#include <span>
#include <string_view>
#include <concepts>
#include "pr/common/cast.h"
#include "pr/common/hash.h"
#include "pr/macros/enum.h"
#include "pr/container/byte_data.h"

// Plans:
// - Implement a Chunker-like format for ldr files
//   Support reading binary ldr files using script::ByteReader or maybe this file
//   Update LdrObject.h/cpp to use a reader that works with binary or text
// - Make 'pr::ldraw' the new namespace, and deprecate 'pr::ldr'
namespace pr::ldraw
{
	// Keywords in ldraw script
	#pragma region Ldr_Keywords
	// This includes object types and field names because they need to have unique hashes
	#define PR_ENUM_LDRAW_KEYWORDS(x)\
		/* Object types */\
		x(Arrow      ,= hash::HashICT("Arrow"     ))\
		x(Bar        ,= hash::HashICT("Bar"       ))\
		x(Box        ,= hash::HashICT("Box"       ))\
		x(BoxList    ,= hash::HashICT("BoxList"   ))\
		x(Chart      ,= hash::HashICT("Chart"     ))\
		x(Circle     ,= hash::HashICT("Circle"    ))\
		x(Cone       ,= hash::HashICT("Cone"      ))\
		x(ConvexHull ,= hash::HashICT("ConvexHull"))\
		x(CoordFrame ,= hash::HashICT("CoordFrame"))\
		x(Custom     ,= hash::HashICT("Custom"    ))\
		x(Cylinder   ,= hash::HashICT("Cylinder"  ))\
		x(DirLight   ,= hash::HashICT("DirLight"  ))\
		x(Equation   ,= hash::HashICT("Equation"  ))\
		x(FrustumFA  ,= hash::HashICT("FrustumFA" ))\
		x(FrustumWH  ,= hash::HashICT("FrustumWH" ))\
		x(Grid       ,= hash::HashICT("Grid"      ))\
		x(Group      ,= hash::HashICT("Group"     ))\
		x(Instance   ,= hash::HashICT("Instance"  ))\
		x(Line       ,= hash::HashICT("Line"      ))\
		x(LineBox    ,= hash::HashICT("LineBox"   ))\
		x(LineD      ,= hash::HashICT("LineD"     ))\
		x(LineStrip  ,= hash::HashICT("LineStrip" ))\
		x(Matrix3x3  ,= hash::HashICT("Matrix3x3" ))\
		x(Mesh       ,= hash::HashICT("Mesh"      ))\
		x(Model      ,= hash::HashICT("Model"     ))\
		x(Pie        ,= hash::HashICT("Pie"       ))\
		x(Plane      ,= hash::HashICT("Plane"     ))\
		x(Point      ,= hash::HashICT("Point"     ))\
		x(PointLight ,= hash::HashICT("PointLight"))\
		x(Polygon    ,= hash::HashICT("Polygon"   ))\
		x(Quad       ,= hash::HashICT("Quad"      ))\
		x(Rect       ,= hash::HashICT("Rect"      ))\
		x(Ribbon     ,= hash::HashICT("Ribbon"    ))\
		x(Series     ,= hash::HashICT("Series"    ))\
		x(Sphere     ,= hash::HashICT("Sphere"    ))\
		x(Spline     ,= hash::HashICT("Spline"    ))\
		x(SpotLight  ,= hash::HashICT("SpotLight" ))\
		x(Text       ,= hash::HashICT("Text"      ))\
		x(Triangle   ,= hash::HashICT("Triangle"  ))\
		x(Tube       ,= hash::HashICT("Tube"      ))\
		x(Unknown    ,= hash::HashICT("Unknown"   ))\
		/* Field Names */\
		x(Accel           ,= hash::HashICT("Accel"               ))\
		x(Addr            ,= hash::HashICT("Addr"                ))\
		x(Align           ,= hash::HashICT("Align"               ))\
		x(Alpha           ,= hash::HashICT("Alpha"               ))\
		x(Anchor          ,= hash::HashICT("Anchor"              ))\
		x(AngAccel        ,= hash::HashICT("AngAccel"            ))\
		x(AngVelocity     ,= hash::HashICT("AngVelocity"         ))\
		x(Animation       ,= hash::HashICT("Animation"           ))\
		x(Aspect          ,= hash::HashICT("Aspect"              ))\
		x(Axis            ,= hash::HashICT("Axis"                ))\
		x(AxisId          ,= hash::HashICT("AxisId"              ))\
		x(BackColour      ,= hash::HashICT("BackColour"          ))\
		x(BakeTransform   ,= hash::HashICT("BakeTransform"       ))\
		x(Billboard       ,= hash::HashICT("Billboard"           ))\
		x(Billboard3D     ,= hash::HashICT("Billboard3D"         ))\
		x(Camera          ,= hash::HashICT("Camera"              ))\
		x(CastShadow      ,= hash::HashICT("CastShadow"          ))\
		x(Closed          ,= hash::HashICT("Closed"              ))\
		x(Colour          ,= hash::HashICT("Colour"              ))\
		x(ColourMask      ,= hash::HashICT("ColourMask"          ))\
		x(Colours         ,= hash::HashICT("Colours"             ))\
		x(CornerRadius    ,= hash::HashICT("CornerRadius"        ))\
		x(CString         ,= hash::HashICT("CString"             ))\
		x(Dashed          ,= hash::HashICT("Dashed"              ))\
		x(Data            ,= hash::HashICT("Data"                ))\
		x(Delimiters      ,= hash::HashICT("Delimiters"          ))\
		x(Depth           ,= hash::HashICT("Depth"               ))\
		x(Dim             ,= hash::HashICT("Dim"                 ))\
		x(Direction       ,= hash::HashICT("Direction"           ))\
		x(Divisions       ,= hash::HashICT("Divisions"           ))\
		x(Euler           ,= hash::HashICT("Euler"               ))\
		x(Faces           ,= hash::HashICT("Faces"               ))\
		x(Facets          ,= hash::HashICT("Facets"              ))\
		x(Far             ,= hash::HashICT("Far"                 ))\
		x(Filter          ,= hash::HashICT("Filter"              ))\
		x(Font            ,= hash::HashICT("Font"                ))\
		x(ForeColour      ,= hash::HashICT("ForeColour"          ))\
		x(Format          ,= hash::HashICT("Format"              ))\
		x(Fov             ,= hash::HashICT("Fov"                 ))\
		x(FovX            ,= hash::HashICT("FovX"                ))\
		x(FovY            ,= hash::HashICT("FovY"                ))\
		x(GenerateNormals ,= hash::HashICT("GenerateNormals"     ))\
		x(Hidden          ,= hash::HashICT("Hidden"              ))\
		x(Inverse         ,= hash::HashICT("Inverse"             ))\
		x(Layers          ,= hash::HashICT("Layers"              ))\
		x(LeftHanded      ,= hash::HashICT("LeftHanded"          ))\
		x(LineList        ,= hash::HashICT("LineList"            ))\
		x(Lines           ,= hash::HashICT("Lines"               ))\
		x(Lock            ,= hash::HashICT("Lock"                ))\
		x(LookAt          ,= hash::HashICT("LookAt"              ))\
		x(M3x3            ,= hash::HashICT("M3x3"                ))\
		x(M4x4            ,= hash::HashICT("M4x4"                ))\
		x(Name            ,= hash::HashICT("Name"                ))\
		x(Near            ,= hash::HashICT("Near"                ))\
		x(NewLine         ,= hash::HashICT("NewLine"             ))\
		x(NonAffine       ,= hash::HashICT("NonAffine"           ))\
		x(Normalise       ,= hash::HashICT("Normalise"           ))\
		x(Normals         ,= hash::HashICT("Normals"             ))\
		x(NoZTest         ,= hash::HashICT("NoZTest"             ))\
		x(NoZWrite        ,= hash::HashICT("NoZWrite"            ))\
		x(O2W             ,= hash::HashICT("O2W"                 ))\
		x(Orthographic    ,= hash::HashICT("Orthographic"        ))\
		x(Orthonormalise  ,= hash::HashICT("Orthonormalise"      ))\
		x(Padding         ,= hash::HashICT("Padding"             ))\
		x(Param           ,= hash::HashICT("Param"               ))\
		x(Part            ,= hash::HashICT("Part"                ))\
		x(Period          ,= hash::HashICT("Period"              ))\
		x(PerItemColour   ,= hash::HashICT("PerItemColour"       ))\
		x(Pos             ,= hash::HashICT("Pos"                 ))\
		x(Quat            ,= hash::HashICT("Quat"                ))\
		x(QuatPos         ,= hash::HashICT("QuatPos"             ))\
		x(Rand4x4         ,= hash::HashICT("Rand4x4"             ))\
		x(RandColour      ,= hash::HashICT("RandColour"          ))\
		x(RandOri         ,= hash::HashICT("RandOri"             ))\
		x(RandPos         ,= hash::HashICT("RandPos"             ))\
		x(Range           ,= hash::HashICT("Range"               ))\
		x(Reflectivity    ,= hash::HashICT("Reflectivity"        ))\
		x(Resolution      ,= hash::HashICT("Resolution"          ))\
		x(Scale           ,= hash::HashICT("Scale"               ))\
		x(ScreenSpace     ,= hash::HashICT("ScreenSpace"         ))\
		x(Size            ,= hash::HashICT("Size"                ))\
		x(Smooth          ,= hash::HashICT("Smooth"              ))\
		x(Solid           ,= hash::HashICT("Solid"               ))\
		x(Source          ,= hash::HashICT("Source"              ))\
		x(Specular        ,= hash::HashICT("Specular"            ))\
		x(Step            ,= hash::HashICT("Step"                ))\
		x(Stretch         ,= hash::HashICT("Stretch"             ))\
		x(Strikeout       ,= hash::HashICT("Strikeout"           ))\
		x(Style           ,= hash::HashICT("Style"               ))\
		x(Tetra           ,= hash::HashICT("Tetra"               ))\
		x(TexCoords       ,= hash::HashICT("TexCoords"           ))\
		x(TextLayout      ,= hash::HashICT("TextLayout"          ))\
		x(Texture         ,= hash::HashICT("Texture"             ))\
		x(Transpose       ,= hash::HashICT("Transpose"           ))\
		x(TriList         ,= hash::HashICT("TriList"             ))\
		x(TriStrip        ,= hash::HashICT("TriStrip"            ))\
		x(Txfm            ,= hash::HashICT("Txfm"                ))\
		x(Underline       ,= hash::HashICT("Underline"           ))\
		x(Up              ,= hash::HashICT("Up"                  ))\
		x(Velocity        ,= hash::HashICT("Velocity"            ))\
		x(Verts           ,= hash::HashICT("Verts"               ))\
		x(Video           ,= hash::HashICT("Video"               ))\
		x(ViewPlaneZ      ,= hash::HashICT("ViewPlaneZ"          ))\
		x(Wedges          ,= hash::HashICT("Wedges"              ))\
		x(Weight          ,= hash::HashICT("Weight"              ))\
		x(Width           ,= hash::HashICT("Width"               ))\
		x(Wireframe       ,= hash::HashICT("Wireframe"           ))\
		x(XAxis           ,= hash::HashICT("XAxis"               ))\
		x(XColumn         ,= hash::HashICT("XColumn"             ))\
		x(YAxis           ,= hash::HashICT("YAxis"               ))\
		x(ZAxis           ,= hash::HashICT("ZAxis"               ))
	PR_DEFINE_ENUM2(EKeyword, PR_ENUM_LDRAW_KEYWORDS);
	#pragma endregion

	// An enum of just the object types
	#pragma region Ldr_Object_Types
	#define PR_ENUM_LDRAW_OBJECTS(x)\
		x(Arrow      ,= EKeyword::Arrow      )\
		x(Bar        ,= EKeyword::Bar        )\
		x(Box        ,= EKeyword::Box        )\
		x(BoxList    ,= EKeyword::BoxList    )\
		x(Chart      ,= EKeyword::Chart      )\
		x(Circle     ,= EKeyword::Circle     )\
		x(Cone       ,= EKeyword::Cone       )\
		x(ConvexHull ,= EKeyword::ConvexHull )\
		x(CoordFrame ,= EKeyword::CoordFrame )\
		x(Custom     ,= EKeyword::Custom     )\
		x(Cylinder   ,= EKeyword::Cylinder   )\
		x(DirLight   ,= EKeyword::DirLight   )\
		x(Equation   ,= EKeyword::Equation   )\
		x(FrustumFA  ,= EKeyword::FrustumFA  )\
		x(FrustumWH  ,= EKeyword::FrustumWH  )\
		x(Grid       ,= EKeyword::Grid       )\
		x(Group      ,= EKeyword::Group      )\
		x(Instance   ,= EKeyword::Instance   )\
		x(Line       ,= EKeyword::Line       )\
		x(LineBox    ,= EKeyword::LineBox    )\
		x(LineD      ,= EKeyword::LineD      )\
		x(LineStrip  ,= EKeyword::LineStrip  )\
		x(Matrix3x3  ,= EKeyword::Matrix3x3  )\
		x(Mesh       ,= EKeyword::Mesh       )\
		x(Model      ,= EKeyword::Model      )\
		x(Pie        ,= EKeyword::Pie        )\
		x(Plane      ,= EKeyword::Plane      )\
		x(Point      ,= EKeyword::Point      )\
		x(PointLight ,= EKeyword::PointLight )\
		x(Polygon    ,= EKeyword::Polygon    )\
		x(Quad       ,= EKeyword::Quad       )\
		x(Rect       ,= EKeyword::Rect       )\
		x(Ribbon     ,= EKeyword::Ribbon     )\
		x(Series     ,= EKeyword::Series     )\
		x(Sphere     ,= EKeyword::Sphere     )\
		x(Spline     ,= EKeyword::Spline     )\
		x(SpotLight  ,= EKeyword::SpotLight  )\
		x(Text       ,= EKeyword::Text       )\
		x(Triangle   ,= EKeyword::Triangle   )\
		x(Tube       ,= EKeyword::Tube       )\
		x(Unknown    ,= EKeyword::Unknown    )
	PR_DEFINE_ENUM2(EObject, PR_ENUM_LDRAW_OBJECTS);
	#pragma endregion

	// The section header
	struct Section
	{
		// The hash of the keyword (4-bytes)
		EKeyword m_keyword;

		// The length of the section in bytes (including the header size)
		int m_size;
	};

	#pragma region Traits
	template <typename TOut> struct traits
	{
		static int64_t tellp(TOut& out)
		{
			return out.tellp();
		}
		static TOut& write(TOut& out, std::span<std::byte const> data, int64_t ofs = -1)
		{
			if (ofs != -1)
				out.seekp(ofs).write(char_ptr(data.data()), data.size()).seekp(0, std::ios::end);
			else
				out.write(char_ptr(data.data()), data.size());
			return out;
		}
	};
	template <> struct traits<byte_data<4>>
	{
		static int64_t tellp(byte_data<4>& out)
		{
			return out.size();
		}
		static byte_data<4>& write(byte_data<4>& out, std::span<std::byte const> data, int64_t ofs = -1)
		{
			if (ofs != -1)
				out.overwrite(ofs, data);
			else
				out.append(data);
			return out;
		}
	};
	#pragma endregion

	#pragma region Write

	// Notes:
	//  - Each Write function returns the size (in bytes) added to 'out'
	//  - To write out only part of a File, delete the parts in a temporary copy of the file.

	// Types that can be serialized into the buffer
	template <typename T, typename... Args>
	concept PodType =
		std::is_trivial_v<T> &&
		std::is_standard_layout_v<T> &&
		!std::is_invocable_v<T, Args...>;

	template <typename T>
	concept IterableType = requires(T t)
	{
		{ std::begin(t) } -> std::input_or_output_iterator;
		{ std::end(t) } -> std::input_or_output_iterator;
	};

	// Write custom data within a section
	template <typename TOut, std::invocable<TOut&> AddBodyFn>
	int64_t Write(TOut& out, EKeyword keyword, AddBodyFn body_cb)
	{
		// Record the write pointer position
		auto ofs = traits<TOut>::tellp(out);

		// Write a dummy header
		Section header = { .m_keyword = keyword };
		traits<TOut>::write(out, { byte_ptr(&header), sizeof(header) });
		
		// Write the section body
		body_cb(out);

		// Update the header with the correct size
		header.m_size = s_cast<int>(traits<TOut>::tellp(out) - ofs);
		traits<TOut>::write(out, { byte_ptr(&header), sizeof(header) }, ofs);
		return header.m_size;
	}

	// Write an empty section
	template <typename TOut>
	int64_t Write(TOut& out, EKeyword keyword)
	{
		return Write(out, keyword, [](auto&) {});
	}

	// Write a single item
	template <typename TOut> 
	int64_t Write(TOut& out, EKeyword keyword, uint32_t item)
	{
		return Write(out, keyword, [&](auto&)
		{
			traits<TOut>::write(out, { byte_ptr(&item), sizeof(item) });
		});
	}

	// Write a string section
	template <typename TOut>
	int64_t Write(TOut& out, EKeyword keyword, std::string_view str)
	{
		return Write(out, keyword, [&](auto&)
		{
			traits<TOut>::write(out, { byte_ptr(str.data()), str.size() });
		});
	}

	// Write a span of items
	template <typename TOut, IterableType TIterable>
	int64_t Write(TOut& out, EKeyword keyword, TIterable const& items)
	{
		return Write(out, keyword, [&](auto&)
		{
			for (auto const& item : items)
			{
				static_assert(std::is_trivial_v<decltype(item)>);
				traits<TOut>::write(out, { byte_ptr(&item), sizeof(item) });
			}
		});
	}

	// Write an immediate list of items
	template <typename TOut, PodType TItem>
	int64_t Write(TOut& out, EKeyword keyword, std::initializer_list<TItem const> items)
	{
		return Write(out, keyword, [&](auto&)
		{
			for (auto const& item : items)
				traits<TOut>::write(out, { byte_ptr(&item), sizeof(item) });
		});
	}

	#pragma endregion

	#pragma region Read
	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "pr/maths/maths.h"
namespace pr::ldraw
{
	PRUnitTest(LdrawBinaryTests)
	{
		byte_data<4> data;
		Write(data, EKeyword::Point, [](auto& data)
		{
			Write(data, EKeyword::Name, "TestPoints");
			Write(data, EKeyword::Colour, 0xFF00FF00);
			Write(data, EKeyword::Data, { v4(1,1,1,1), v4(2,2,2,1), v4(3,3,3,1) });
		});


	}
}
#endif
