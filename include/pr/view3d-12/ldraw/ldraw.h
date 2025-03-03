//********************************
// Ldraw Script keywords
//  Copyright (c) Rylogic Ltd 2014
//********************************

#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12::ldraw
{
	// Map the compile time hash function to this namespace
	template <std::integral T = int> constexpr T HashI(char const* str)
	{
		return static_cast<T>(hash::HashICT(str));
	}

	// Keywords in ldraw script
	#pragma region Ldr_Keywords
	// This includes object types and field names because they need to have unique hashes
	#define PR_ENUM_LDRAW_KEYWORDS(x)\
		/* Object types */\
		x(Arrow      ,= HashI("Arrow"      ))\
		x(Bar        ,= HashI("Bar"        ))\
		x(Box        ,= HashI("Box"        ))\
		x(BoxList    ,= HashI("BoxList"    ))\
		x(Chart      ,= HashI("Chart"      ))\
		x(Circle     ,= HashI("Circle"     ))\
		x(Cone       ,= HashI("Cone"       ))\
		x(ConvexHull ,= HashI("ConvexHull" ))\
		x(CoordFrame ,= HashI("CoordFrame" ))\
		x(Custom     ,= HashI("Custom"     ))\
		x(Cylinder   ,= HashI("Cylinder"   ))\
		x(Equation   ,= HashI("Equation"   ))\
		x(FrustumFA  ,= HashI("FrustumFA"  ))\
		x(FrustumWH  ,= HashI("FrustumWH"  ))\
		x(Grid       ,= HashI("Grid"       ))\
		x(Group      ,= HashI("Group"      ))\
		x(Instance   ,= HashI("Instance"   ))\
		x(LightSource,= HashI("LightSource"))\
		x(Line       ,= HashI("Line"       ))\
		x(LineBox    ,= HashI("LineBox"    ))\
		x(LineD      ,= HashI("LineD"      ))\
		x(LineStrip  ,= HashI("LineStrip"  ))\
		x(Matrix3x3  ,= HashI("Matrix3x3"  ))\
		x(Mesh       ,= HashI("Mesh"       ))\
		x(Model      ,= HashI("Model"      ))\
		x(Pie        ,= HashI("Pie"        ))\
		x(Plane      ,= HashI("Plane"      ))\
		x(Point      ,= HashI("Point"      ))\
		x(Polygon    ,= HashI("Polygon"    ))\
		x(Quad       ,= HashI("Quad"       ))\
		x(Rect       ,= HashI("Rect"       ))\
		x(Ribbon     ,= HashI("Ribbon"     ))\
		x(Series     ,= HashI("Series"     ))\
		x(Sphere     ,= HashI("Sphere"     ))\
		x(Spline     ,= HashI("Spline"     ))\
		x(Text       ,= HashI("Text"       ))\
		x(Triangle   ,= HashI("Triangle"   ))\
		x(Tube       ,= HashI("Tube"       ))\
		x(Unknown    ,= HashI("Unknown"    ))\
		/* Field Names */\
		x(Accel           ,= HashI("Accel"               ))\
		x(Addr            ,= HashI("Addr"                ))\
		x(Align           ,= HashI("Align"               ))\
		x(Alpha           ,= HashI("Alpha"               ))\
		x(Ambient         ,= HashI("Ambient"             ))\
		x(Anchor          ,= HashI("Anchor"              ))\
		x(AngAccel        ,= HashI("AngAccel"            ))\
		x(AngVelocity     ,= HashI("AngVelocity"         ))\
		x(Animation       ,= HashI("Animation"           ))\
		x(Aspect          ,= HashI("Aspect"              ))\
		x(Axis            ,= HashI("Axis"                ))\
		x(AxisId          ,= HashI("AxisId"              ))\
		x(BackColour      ,= HashI("BackColour"          ))\
		x(BakeTransform   ,= HashI("BakeTransform"       ))\
		x(Billboard       ,= HashI("Billboard"           ))\
		x(Billboard3D     ,= HashI("Billboard3D"         ))\
		x(Camera          ,= HashI("Camera"              ))\
		x(CastShadow      ,= HashI("CastShadow"          ))\
		x(Closed          ,= HashI("Closed"              ))\
		x(Colour          ,= HashI("Colour"              ))\
		x(ColourMask      ,= HashI("ColourMask"          ))\
		x(Colours         ,= HashI("Colours"             ))\
		x(CornerRadius    ,= HashI("CornerRadius"        ))\
		x(CrossSection    ,= HashI("CrossSection"        ))\
		x(CString         ,= HashI("CString"             ))\
		x(Dashed          ,= HashI("Dashed"              ))\
		x(Data            ,= HashI("Data"                ))\
		x(Depth           ,= HashI("Depth"               ))\
		x(Diffuse         ,= HashI("Diffuse"             ))\
		x(Dim             ,= HashI("Dim"                 ))\
		x(Direction       ,= HashI("Direction"           ))\
		x(Divisions       ,= HashI("Divisions"           ))\
		x(Euler           ,= HashI("Euler"               ))\
		x(Faces           ,= HashI("Faces"               ))\
		x(Facets          ,= HashI("Facets"              ))\
		x(Far             ,= HashI("Far"                 ))\
		x(FilePath        ,= HashI("FilePath"            ))\
		x(Filter          ,= HashI("Filter"              ))\
		x(Font            ,= HashI("Font"                ))\
		x(ForeColour      ,= HashI("ForeColour"          ))\
		x(Format          ,= HashI("Format"              ))\
		x(Fov             ,= HashI("Fov"                 ))\
		x(FovX            ,= HashI("FovX"                ))\
		x(FovY            ,= HashI("FovY"                ))\
		x(GenerateNormals ,= HashI("GenerateNormals"     ))\
		x(Hidden          ,= HashI("Hidden"              ))\
		x(Inverse         ,= HashI("Inverse"             ))\
		x(Layers          ,= HashI("Layers"              ))\
		x(LeftHanded      ,= HashI("LeftHanded"          ))\
		x(LineList        ,= HashI("LineList"            ))\
		x(Lines           ,= HashI("Lines"               ))\
		x(LookAt          ,= HashI("LookAt"              ))\
		x(M3x3            ,= HashI("M3x3"                ))\
		x(M4x4            ,= HashI("M4x4"                ))\
		x(Name            ,= HashI("Name"                ))\
		x(Near            ,= HashI("Near"                ))\
		x(NewLine         ,= HashI("NewLine"             ))\
		x(NonAffine       ,= HashI("NonAffine"           ))\
		x(Normalise       ,= HashI("Normalise"           ))\
		x(Normals         ,= HashI("Normals"             ))\
		x(NoZTest         ,= HashI("NoZTest"             ))\
		x(NoZWrite        ,= HashI("NoZWrite"            ))\
		x(O2W             ,= HashI("O2W"                 ))\
		x(Orthographic    ,= HashI("Orthographic"        ))\
		x(Orthonormalise  ,= HashI("Orthonormalise"      ))\
		x(Padding         ,= HashI("Padding"             ))\
		x(Param           ,= HashI("Param"               ))\
		x(Part            ,= HashI("Part"                ))\
		x(Period          ,= HashI("Period"              ))\
		x(PerItemColour   ,= HashI("PerItemColour"       ))\
		x(Pos             ,= HashI("Pos"                 ))\
		x(Position        ,= HashI("Position"            ))\
		x(Quat            ,= HashI("Quat"                ))\
		x(QuatPos         ,= HashI("QuatPos"             ))\
		x(Rand4x4         ,= HashI("Rand4x4"             ))\
		x(RandColour      ,= HashI("RandColour"          ))\
		x(RandOri         ,= HashI("RandOri"             ))\
		x(RandPos         ,= HashI("RandPos"             ))\
		x(Range           ,= HashI("Range"               ))\
		x(Reflectivity    ,= HashI("Reflectivity"        ))\
		x(Resolution      ,= HashI("Resolution"          ))\
		x(Round           ,= HashI("Round"               ))\
		x(Scale           ,= HashI("Scale"               ))\
		x(ScreenSpace     ,= HashI("ScreenSpace"         ))\
		x(Size            ,= HashI("Size"                ))\
		x(Smooth          ,= HashI("Smooth"              ))\
		x(Solid           ,= HashI("Solid"               ))\
		x(Source          ,= HashI("Source"              ))\
		x(Specular        ,= HashI("Specular"            ))\
		x(Square          ,= HashI("Square"              ))\
		x(Step            ,= HashI("Step"                ))\
		x(Stretch         ,= HashI("Stretch"             ))\
		x(Strikeout       ,= HashI("Strikeout"           ))\
		x(Style           ,= HashI("Style"               ))\
		x(Tetra           ,= HashI("Tetra"               ))\
		x(TexCoords       ,= HashI("TexCoords"           ))\
		x(TextLayout      ,= HashI("TextLayout"          ))\
		x(Texture         ,= HashI("Texture"             ))\
		x(Transpose       ,= HashI("Transpose"           ))\
		x(TriList         ,= HashI("TriList"             ))\
		x(TriStrip        ,= HashI("TriStrip"            ))\
		x(Txfm            ,= HashI("Txfm"                ))\
		x(Underline       ,= HashI("Underline"           ))\
		x(Up              ,= HashI("Up"                  ))\
		x(Velocity        ,= HashI("Velocity"            ))\
		x(Verts           ,= HashI("Verts"               ))\
		x(Video           ,= HashI("Video"               ))\
		x(ViewPlaneZ      ,= HashI("ViewPlaneZ"          ))\
		x(Wedges          ,= HashI("Wedges"              ))\
		x(Weight          ,= HashI("Weight"              ))\
		x(Width           ,= HashI("Width"               ))\
		x(Wireframe       ,= HashI("Wireframe"           ))\
		x(XAxis           ,= HashI("XAxis"               ))\
		x(XColumn         ,= HashI("XColumn"             ))\
		x(YAxis           ,= HashI("YAxis"               ))\
		x(ZAxis           ,= HashI("ZAxis"               ))
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
		x(Equation   ,= EKeyword::Equation   )\
		x(FrustumFA  ,= EKeyword::FrustumFA  )\
		x(FrustumWH  ,= EKeyword::FrustumWH  )\
		x(Grid       ,= EKeyword::Grid       )\
		x(Group      ,= EKeyword::Group      )\
		x(Instance   ,= EKeyword::Instance   )\
		x(LightSource,= EKeyword::LightSource)\
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
		x(Polygon    ,= EKeyword::Polygon    )\
		x(Quad       ,= EKeyword::Quad       )\
		x(Rect       ,= EKeyword::Rect       )\
		x(Ribbon     ,= EKeyword::Ribbon     )\
		x(Series     ,= EKeyword::Series     )\
		x(Sphere     ,= EKeyword::Sphere     )\
		x(Spline     ,= EKeyword::Spline     )\
		x(Text       ,= EKeyword::Text       )\
		x(Triangle   ,= EKeyword::Triangle   )\
		x(Tube       ,= EKeyword::Tube       )\
		x(Unknown    ,= EKeyword::Unknown    )
	PR_DEFINE_ENUM2(ELdrObject, PR_ENUM_LDRAW_OBJECTS);
	#pragma endregion

	// Camera fields
	enum class ECamField
	{
		None    = 0,
		C2W     = 1 << 0,
		Focus   = 1 << 1,
		Align   = 1 << 2,
		Aspect  = 1 << 3,
		FovY    = 1 << 4,
		Near    = 1 << 5,
		Far     = 1 << 6,
		Ortho   = 1 << 7,
		_flags_enum = 0,
	};

	// Simple animation styles
	enum class EAnimStyle
	{
		NoAnimation,
		Once,
		Repeat,
		Continuous,
		PingPong,
	};

	// Flags for partial update of a model
	enum class EUpdateObject :int
	{
		None         = 0,
		Name         = 1 << 0,
		Model        = 1 << 1,
		Transform    = 1 << 2,
		Children     = 1 << 3,
		Colour       = 1 << 4,
		ColourMask   = 1 << 5,
		Reflectivity = 1 << 6,
		Flags        = 1 << 7,
		Animation    = 1 << 8,
		All          = 0x1FF,
		_flags_enum = 0,
	};

	// Flags for extra behaviour of an object
	enum class ELdrFlags
	{
		None = 0,

		// The object is hidden
		Hidden = 1 << 0,

		// The object is filled in wireframe mode
		Wireframe = 1 << 1,

		// Render the object without testing against the depth buffer
		NoZTest = 1 << 2,

		// Render the object without effecting the depth buffer
		NoZWrite = 1 << 3,

		// The object has normals shown
		Normals = 1 << 4,

		// The object to world transform is not an affine transform
		NonAffine = 1 << 5,

		// Set when an instance is "selected". The meaning of 'selected' is up to the application
		Selected = 1 << 8,

		// Doesn't contribute to the bounding box
		BBoxExclude = 1 << 9,

		// Should not be included when determining the bounds of a scene.
		SceneBoundsExclude = 1 << 10,

		// Ignored for hit test ray casts
		HitTestExclude = 1 << 11,

		// Doesn't cast a shadow
		ShadowCastExclude = 1 << 12,

		// Bitwise operators
		_flags_enum = 0,
	};

	// Colour blend operations
	enum class EColourOp
	{
		Overwrite,
		Add,
		Subtract,
		Multiply,
		Lerp,
	};

	// Info on how to animate a ldr object
	struct Animation
	{
		EAnimStyle m_style;
		float      m_period; // Seconds
		v4         m_vel;    // Linear velocity of the animation in m/s
		v4         m_acc;    // Linear velocity of the animation in m/s
		v4         m_avel;   // Angular velocity of the animation in rad/s
		v4         m_aacc;   // Angular velocity of the animation in rad/s

		Animation()
			:m_style(EAnimStyle::NoAnimation)
			,m_period(1.0f)
			,m_vel(v4Zero)
			,m_acc(v4Zero)
			,m_avel(v4Zero)
			,m_aacc(v4Zero)
		{}

		// Return a transform representing the offset
		// added by this object at time 'time_s'
		m4x4 Step(float time_s) const
		{
			auto t = 0.0f;
			switch (m_style)
			{
			default: throw std::exception("Unknown animation style");
			case EAnimStyle::NoAnimation:
				return m4x4Identity;
			case EAnimStyle::Once:
				t = time_s < m_period ? time_s : m_period;
				break;
			case EAnimStyle::Repeat:
				t = Fmod(time_s, m_period);
				break;
			case EAnimStyle::Continuous:
				t = time_s;
				break;
			case EAnimStyle::PingPong:
				t = Fmod(time_s, 2.0f*m_period) >= m_period
					? m_period - Fmod(time_s, m_period)
					: Fmod(time_s, m_period);
				break;
			}

			auto l = 0.5f*m_acc*Sqr(t) + m_vel*t + v4Origin;
			auto a = 0.5f*m_aacc*Sqr(t) + m_avel*t;
			return m4x4::Transform(a, l);
		}
	};
}
