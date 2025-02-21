//********************************
// Ldraw Binary Script file format
//  Copyright (c) Rylogic Ltd 2014
//********************************
// Binary 3d model format
#pragma once
#include "pr/view3d-12/forward.h"

// Plans:
// - Implement a Chunker-like format for ldr files
//   Support reading binary ldr files using script::ByteReader or maybe this file
//   Update LdrObject.h/cpp to use a reader that works with binary or text
// - Make 'pr::ldraw' the new namespace, and deprecate 'pr::ldr'
namespace pr::ldraw
{
	#pragma region Ldr_Keywords
	// *Keywords in ldraw script
	// This includes object types and field names because the need to have unique hashes
	#define PR_ENUM_LDRKEYWORDS(x)\
		/* Object types */\
		x(Unknown    ,= hash::HashICT("Unknown"   ))\
		x(Point      ,= hash::HashICT("Point"     ))\
		x(Line       ,= hash::HashICT("Line"      ))\
		x(LineD      ,= hash::HashICT("LineD"     ))\
		x(LineStrip  ,= hash::HashICT("LineStrip" ))\
		x(LineBox    ,= hash::HashICT("LineBox"   ))\
		x(Grid       ,= hash::HashICT("Grid"      ))\
		x(Spline     ,= hash::HashICT("Spline"    ))\
		x(Arrow      ,= hash::HashICT("Arrow"     ))\
		x(Circle     ,= hash::HashICT("Circle"    ))\
		x(Pie        ,= hash::HashICT("Pie"       ))\
		x(Rect       ,= hash::HashICT("Rect"      ))\
		x(Polygon    ,= hash::HashICT("Polygon"   ))\
		x(Matrix3x3  ,= hash::HashICT("Matrix3x3" ))\
		x(CoordFrame ,= hash::HashICT("CoordFrame"))\
		x(Triangle   ,= hash::HashICT("Triangle"  ))\
		x(Quad       ,= hash::HashICT("Quad"      ))\
		x(Plane      ,= hash::HashICT("Plane"     ))\
		x(Ribbon     ,= hash::HashICT("Ribbon"    ))\
		x(Box        ,= hash::HashICT("Box"       ))\
		x(Bar        ,= hash::HashICT("Bar"       ))\
		x(BoxList    ,= hash::HashICT("BoxList"   ))\
		x(FrustumWH  ,= hash::HashICT("FrustumWH" ))\
		x(FrustumFA  ,= hash::HashICT("FrustumFA" ))\
		x(Sphere     ,= hash::HashICT("Sphere"    ))\
		x(Cylinder   ,= hash::HashICT("Cylinder"  ))\
		x(Cone       ,= hash::HashICT("Cone"      ))\
		x(Tube       ,= hash::HashICT("Tube"      ))\
		x(Mesh       ,= hash::HashICT("Mesh"      ))\
		x(ConvexHull ,= hash::HashICT("ConvexHull"))\
		x(Model      ,= hash::HashICT("Model"     ))\
		x(Equation   ,= hash::HashICT("Equation"  ))\
		x(Chart      ,= hash::HashICT("Chart"     ))\
		x(Series     ,= hash::HashICT("Series"    ))\
		x(Group      ,= hash::HashICT("Group"     ))\
		x(Text       ,= hash::HashICT("Text"      ))\
		x(Instance   ,= hash::HashICT("Instance"  ))\
		x(DirLight   ,= hash::HashICT("DirLight"  ))\
		x(PointLight ,= hash::HashICT("PointLight"))\
		x(SpotLight  ,= hash::HashICT("SpotLight" ))\
		x(Custom     ,= hash::HashICT("Custom"    ))\
		/* Field Names */\
		x(Name            ,= hash::HashICT("Name"                ))\
		x(Txfm            ,= hash::HashICT("Txfm"                ))\
		x(O2W             ,= hash::HashICT("O2W"                 ))\
		x(M4x4            ,= hash::HashICT("M4x4"                ))\
		x(M3x3            ,= hash::HashICT("M3x3"                ))\
		x(Pos             ,= hash::HashICT("Pos"                 ))\
		x(Up              ,= hash::HashICT("Up"                  ))\
		x(Direction       ,= hash::HashICT("Direction"           ))\
		x(Quat            ,= hash::HashICT("Quat"                ))\
		x(QuatPos         ,= hash::HashICT("QuatPos"             ))\
		x(Rand4x4         ,= hash::HashICT("Rand4x4"             ))\
		x(RandPos         ,= hash::HashICT("RandPos"             ))\
		x(RandOri         ,= hash::HashICT("RandOri"             ))\
		x(Euler           ,= hash::HashICT("Euler"               ))\
		x(Dim             ,= hash::HashICT("Dim"                 ))\
		x(Scale           ,= hash::HashICT("Scale"               ))\
		x(Size            ,= hash::HashICT("Size"                ))\
		x(Weight          ,= hash::HashICT("Weight"              ))\
		x(Transpose       ,= hash::HashICT("Transpose"           ))\
		x(Inverse         ,= hash::HashICT("Inverse"             ))\
		x(Normalise       ,= hash::HashICT("Normalise"           ))\
		x(Orthonormalise  ,= hash::HashICT("Orthonormalise"      ))\
		x(Colour          ,= hash::HashICT("Colour"              ))\
		x(ForeColour      ,= hash::HashICT("ForeColour"          ))\
		x(BackColour      ,= hash::HashICT("BackColour"          ))\
		x(PerItemColour   ,= hash::HashICT("PerItemColour"       ))\
		x(Font            ,= hash::HashICT("Font"                ))\
		x(Stretch         ,= hash::HashICT("Stretch"             ))\
		x(Underline       ,= hash::HashICT("Underline"           ))\
		x(Strikeout       ,= hash::HashICT("Strikeout"           ))\
		x(NewLine         ,= hash::HashICT("NewLine"             ))\
		x(CString         ,= hash::HashICT("CString"             ))\
		x(AxisId          ,= hash::HashICT("AxisId"              ))\
		x(Solid           ,= hash::HashICT("Solid"               ))\
		x(Facets          ,= hash::HashICT("Facets"              ))\
		x(CornerRadius    ,= hash::HashICT("CornerRadius"        ))\
		x(RandColour      ,= hash::HashICT("RandColour"          ))\
		x(ColourMask      ,= hash::HashICT("ColourMask"          ))\
		x(Reflectivity    ,= hash::HashICT("Reflectivity"        ))\
		x(Animation       ,= hash::HashICT("Animation"           ))\
		x(Style           ,= hash::HashICT("Style"               ))\
		x(Format          ,= hash::HashICT("Format"              ))\
		x(TextLayout      ,= hash::HashICT("TextLayout"          ))\
		x(Anchor          ,= hash::HashICT("Anchor"              ))\
		x(Padding         ,= hash::HashICT("Padding"             ))\
		x(Period          ,= hash::HashICT("Period"              ))\
		x(Velocity        ,= hash::HashICT("Velocity"            ))\
		x(Accel           ,= hash::HashICT("Accel"               ))\
		x(AngVelocity     ,= hash::HashICT("AngVelocity"         ))\
		x(AngAccel        ,= hash::HashICT("AngAccel"            ))\
		x(Axis            ,= hash::HashICT("Axis"                ))\
		x(Hidden          ,= hash::HashICT("Hidden"              ))\
		x(Wireframe       ,= hash::HashICT("Wireframe"           ))\
		x(Delimiters      ,= hash::HashICT("Delimiters"          ))\
		x(Camera          ,= hash::HashICT("Camera"              ))\
		x(LookAt          ,= hash::HashICT("LookAt"              ))\
		x(Align           ,= hash::HashICT("Align"               ))\
		x(Aspect          ,= hash::HashICT("Aspect"              ))\
		x(FovX            ,= hash::HashICT("FovX"                ))\
		x(FovY            ,= hash::HashICT("FovY"                ))\
		x(Fov             ,= hash::HashICT("Fov"                 ))\
		x(Near            ,= hash::HashICT("Near"                ))\
		x(Far             ,= hash::HashICT("Far"                 ))\
		x(Orthographic    ,= hash::HashICT("Orthographic"        ))\
		x(Lock            ,= hash::HashICT("Lock"                ))\
		x(Width           ,= hash::HashICT("Width"               ))\
		x(Dashed          ,= hash::HashICT("Dashed"              ))\
		x(Smooth          ,= hash::HashICT("Smooth"              ))\
		x(XAxis           ,= hash::HashICT("XAxis"               ))\
		x(YAxis           ,= hash::HashICT("YAxis"               ))\
		x(ZAxis           ,= hash::HashICT("ZAxis"               ))\
		x(XColumn         ,= hash::HashICT("XColumn"             ))\
		x(Closed          ,= hash::HashICT("Closed"              ))\
		x(Param           ,= hash::HashICT("Param"               ))\
		x(Texture         ,= hash::HashICT("Texture"             ))\
		x(Video           ,= hash::HashICT("Video"               ))\
		x(Resolution      ,= hash::HashICT("Resolution"          ))\
		x(Divisions       ,= hash::HashICT("Divisions"           ))\
		x(Layers          ,= hash::HashICT("Layers"              ))\
		x(Wedges          ,= hash::HashICT("Wedges"              ))\
		x(ViewPlaneZ      ,= hash::HashICT("ViewPlaneZ"          ))\
		x(Verts           ,= hash::HashICT("Verts"               ))\
		x(Normals         ,= hash::HashICT("Normals"             ))\
		x(Colours         ,= hash::HashICT("Colours"             ))\
		x(TexCoords       ,= hash::HashICT("TexCoords"           ))\
		x(Lines           ,= hash::HashICT("Lines"               ))\
		x(LineList        ,= hash::HashICT("LineList"            ))\
		x(LineStrip       ,= hash::HashICT("LineStrip"           ))\
		x(Faces           ,= hash::HashICT("Faces"               ))\
		x(TriList         ,= hash::HashICT("TriList"             ))\
		x(TriStrip        ,= hash::HashICT("TriStrip"            ))\
		x(Tetra           ,= hash::HashICT("Tetra"               ))\
		x(Part            ,= hash::HashICT("Part"                ))\
		x(GenerateNormals ,= hash::HashICT("GenerateNormals"     ))\
		x(BakeTransform   ,= hash::HashICT("BakeTransform"       ))\
		x(Step            ,= hash::HashICT("Step"                ))\
		x(Addr            ,= hash::HashICT("Addr"                ))\
		x(Filter          ,= hash::HashICT("Filter"              ))\
		x(Alpha           ,= hash::HashICT("Alpha"               ))\
		x(Range           ,= hash::HashICT("Range"               ))\
		x(Specular        ,= hash::HashICT("Specular"            ))\
		x(ScreenSpace     ,= hash::HashICT("ScreenSpace"         ))\
		x(NoZTest         ,= hash::HashICT("NoZTest"             ))\
		x(NoZWrite        ,= hash::HashICT("NoZWrite"            ))\
		x(Billboard       ,= hash::HashICT("Billboard"           ))\
		x(Billboard3D     ,= hash::HashICT("Billboard3D"         ))\
		x(Depth           ,= hash::HashICT("Depth"               ))\
		x(LeftHanded      ,= hash::HashICT("LeftHanded"          ))\
		x(CastShadow      ,= hash::HashICT("CastShadow"          ))\
		x(NonAffine       ,= hash::HashICT("NonAffine"           ))\
		x(Source          ,= hash::HashICT("Source"              ))\
		x(Data            ,= hash::HashICT("Data"                ))\
		x(Series          ,= hash::HashICT("Series"              ))
	PR_DEFINE_ENUM2(EKeyword, PR_ENUM_LDRKEYWORDS);
	#pragma endregion

	#pragma region Ldr_Object_Types
	// An enum of just the object types
	#define PR_ENUM_LDROBJECTS(x)\
		x(Unknown    ,= ELdrKeyword::Unknown    )\
		x(Point      ,= ELdrKeyword::Point      )\
		x(Line       ,= ELdrKeyword::Line       )\
		x(LineD      ,= ELdrKeyword::LineD      )\
		x(LineStrip  ,= ELdrKeyword::LineStrip  )\
		x(LineBox    ,= ELdrKeyword::LineBox    )\
		x(Grid       ,= ELdrKeyword::Grid       )\
		x(Spline     ,= ELdrKeyword::Spline     )\
		x(Arrow      ,= ELdrKeyword::Arrow      )\
		x(Circle     ,= ELdrKeyword::Circle     )\
		x(Pie        ,= ELdrKeyword::Pie        )\
		x(Rect       ,= ELdrKeyword::Rect       )\
		x(Polygon    ,= ELdrKeyword::Polygon    )\
		x(Matrix3x3  ,= ELdrKeyword::Matrix3x3  )\
		x(CoordFrame ,= ELdrKeyword::CoordFrame )\
		x(Triangle   ,= ELdrKeyword::Triangle   )\
		x(Quad       ,= ELdrKeyword::Quad       )\
		x(Plane      ,= ELdrKeyword::Plane      )\
		x(Ribbon     ,= ELdrKeyword::Ribbon     )\
		x(Box        ,= ELdrKeyword::Box        )\
		x(Bar        ,= ELdrKeyword::Bar        )\
		x(BoxList    ,= ELdrKeyword::BoxList    )\
		x(FrustumWH  ,= ELdrKeyword::FrustumWH  )\
		x(FrustumFA  ,= ELdrKeyword::FrustumFA  )\
		x(Sphere     ,= ELdrKeyword::Sphere     )\
		x(Cylinder   ,= ELdrKeyword::Cylinder   )\
		x(Cone       ,= ELdrKeyword::Cone       )\
		x(Tube       ,= ELdrKeyword::Tube       )\
		x(Mesh       ,= ELdrKeyword::Mesh       )\
		x(ConvexHull ,= ELdrKeyword::ConvexHull )\
		x(Model      ,= ELdrKeyword::Model      )\
		x(Equation   ,= ELdrKeyword::Equation   )\
		x(Chart      ,= ELdrKeyword::Chart      )\
		x(Series     ,= ELdrKeyword::Series     )\
		x(Group      ,= ELdrKeyword::Group      )\
		x(Text       ,= ELdrKeyword::Text       )\
		x(Instance   ,= ELdrKeyword::Instance   )\
		x(DirLight   ,= ELdrKeyword::DirLight   )\
		x(PointLight ,= ELdrKeyword::PointLight )\
		x(SpotLight  ,= ELdrKeyword::SpotLight  )\
		x(Custom     ,= ELdrKeyword::Custom     )
	PR_DEFINE_ENUM2(EObject, PR_ENUM_LDROBJECTS);
	#pragma endregion

	struct Section;

	// This is the raw layout for any section
	struct Section
	{
		// The hash of the keyword (4-bytes) (assumes no
		union {
			EObject m_object;
			EKeyword m_field;
		};

		// The length of the section in bytes (including the header size)
		int m_size;
	};

	#pragma region Traits
	template <typename TOut> struct traits;
	template <> struct traits<byte_data<4>>
	{
		using TOut = byte_data<4>;

		int64_t tellp(TOut& out)
		{
			return isize(out);
		}
	};
	#pragma endregion

	#pragma region Write

	// Notes:
	//  - Each Write function returns the size (in bytes) added to 'out'
	//  - To write out only part of a File, delete the parts in a temporary copy of the file.

	template <typename TOut, std::invocable<TOut&> AddBodyFn> int64_t Write(TOut& out, EKeyword keyword, AddBodyFn body_cb)
	{
		// Record the write pointer position
		auto ofs = traits<TOut>::tellp(out);
		
		// Write a dummy header
		traits<TOut>::write(out, Section{});
		
		// Write the section body
		body_cb(out);

		auto size = traits<TOut>::tellp(out) - ofs;

		// Update the header with the correct size
		traits<TOut>::seekp(out, ofs);
		traits<TOut>::write(out, Section{ .m_field = keyword, .m_size = s_cast<int>(size) });
		traits<TOut>::seekp(out, ofs + size);
		return size;
	}

	#pragma endregion

	#pragma region Read
	#pragma endregion
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::ldr
{

}
#endif
