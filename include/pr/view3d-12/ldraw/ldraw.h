//********************************
// View3d
//  Copyright (c) Rylogic Ltd 2014
//********************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12::ldraw
{
	// Compile time hash
	inline constexpr int HashI(char const* str)
	{
		constexpr uint32_t FNV_offset_basis32 = 2166136261U;
		constexpr uint32_t FNV_prime32 = 16777619U;

		// Copied from pr/common/hash
		constexpr auto Mul32 = [](uint32_t a, uint32_t b) -> uint32_t
		{
			return uint32_t((uint64_t(a) * uint64_t(b)) & ~0U);
		};
		constexpr auto Lower = [](char c) -> char
		{
			return (c >= 'A' && c <= 'Z') ? c + ('a' - 'A') : c;
		};
		constexpr auto Hash32CT = [](uint32_t ch, uint32_t h) -> uint32_t
		{
			return Mul32(h ^ ch, FNV_prime32);
		};
		constexpr auto HashI32CT = [](char const* str, uint32_t h) -> uint32_t
		{
			for (; *str != 0; ++str) h = Hash32CT(static_cast<uint32_t>(Lower(*str)), h);
			return h;
		};
		return static_cast<int>(HashI32CT(str, FNV_offset_basis32));
	}

	// Keywords in ldraw script. This includes object types and field names because they need to have unique hashes
	enum class EKeyword : int
	{
		#define PR_ENUM_LDRAW_KEYWORDS(x)\
		x(Accel             , = HashI("Accel"                 ))\
		x(Addr              , = HashI("Addr"                  ))\
		x(Align             , = HashI("Align"                 ))\
		x(Alpha             , = HashI("Alpha"                 ))\
		x(Ambient           , = HashI("Ambient"               ))\
		x(Anchor            , = HashI("Anchor"                ))\
		x(AngAccel          , = HashI("AngAccel"              ))\
		x(AngVelocity       , = HashI("AngVelocity"           ))\
		x(Animation         , = HashI("Animation"             ))\
		x(Arrow             , = HashI("Arrow"                 ))\
		x(Aspect            , = HashI("Aspect"                ))\
		x(Axis              , = HashI("Axis"                  ))\
		x(AxisId            , = HashI("AxisId"                ))\
		x(BackColour        , = HashI("BackColour"            ))\
		x(BakeTransform     , = HashI("BakeTransform"         ))\
		x(Bar               , = HashI("Bar"                   ))\
		x(Billboard         , = HashI("Billboard"             ))\
		x(Billboard3D       , = HashI("Billboard3D"           ))\
		x(BinaryStream      , = HashI("BinaryStream"          ))\
		x(Box               , = HashI("Box"                   ))\
		x(BoxList           , = HashI("BoxList"               ))\
		x(Camera            , = HashI("Camera"                ))\
		x(CastShadow        , = HashI("CastShadow"            ))\
		x(Chart             , = HashI("Chart"                 ))\
		x(Circle            , = HashI("Circle"                ))\
		x(Closed            , = HashI("Closed"                ))\
		x(Colour            , = HashI("Colour"                ))\
		x(Colours           , = HashI("Colours"               ))\
		x(Commands          , = HashI("Commands"              ))\
		x(Cone              , = HashI("Cone"                  ))\
		x(ConvexHull        , = HashI("ConvexHull"            ))\
		x(CoordFrame        , = HashI("CoordFrame"            ))\
		x(CornerRadius      , = HashI("CornerRadius"          ))\
		x(CrossSection      , = HashI("CrossSection"          ))\
		x(CString           , = HashI("CString"               ))\
		x(Custom            , = HashI("Custom"                ))\
		x(Cylinder          , = HashI("Cylinder"              ))\
		x(Dashed            , = HashI("Dashed"                ))\
		x(Data              , = HashI("Data"                  ))\
		x(Depth             , = HashI("Depth"                 ))\
		x(Diffuse           , = HashI("Diffuse"               ))\
		x(Dim               , = HashI("Dim"                   ))\
		x(Direction         , = HashI("Direction"             ))\
		x(Divisions         , = HashI("Divisions"             ))\
		x(Equation          , = HashI("Equation"              ))\
		x(Euler             , = HashI("Euler"                 ))\
		x(Faces             , = HashI("Faces"                 ))\
		x(Facets            , = HashI("Facets"                ))\
		x(Far               , = HashI("Far"                   ))\
		x(FilePath          , = HashI("FilePath"              ))\
		x(Filter            , = HashI("Filter"                ))\
		x(Font              , = HashI("Font"                  ))\
		x(ForeColour        , = HashI("ForeColour"            ))\
		x(Format            , = HashI("Format"                ))\
		x(Fov               , = HashI("Fov"                   ))\
		x(FovX              , = HashI("FovX"                  ))\
		x(FovY              , = HashI("FovY"                  ))\
		x(Frame             , = HashI("Frame"                 ))\
		x(FrameRange        , = HashI("FrameRange"            ))\
		x(FrustumFA         , = HashI("FrustumFA"             ))\
		x(FrustumWH         , = HashI("FrustumWH"             ))\
		x(GenerateNormals   , = HashI("GenerateNormals"       ))\
		x(Grid              , = HashI("Grid"                  ))\
		x(Group             , = HashI("Group"                 ))\
		x(GroupColour       , = HashI("GroupColour"           ))\
		x(Hidden            , = HashI("Hidden"                ))\
		x(Instance          , = HashI("Instance"              ))\
		x(Inverse           , = HashI("Inverse"               ))\
		x(Layers            , = HashI("Layers"                ))\
		x(LeftHanded        , = HashI("LeftHanded"            ))\
		x(LightSource       , = HashI("LightSource"           ))\
		x(Line              , = HashI("Line"                  ))\
		x(LineBox           , = HashI("LineBox"               ))\
		x(LineD             , = HashI("LineD"                 ))\
		x(LineList          , = HashI("LineList"              ))\
		x(Lines             , = HashI("Lines"                 ))\
		x(LineStrip         , = HashI("LineStrip"             ))\
		x(LookAt            , = HashI("LookAt"                ))\
		x(M3x3              , = HashI("M3x3"                  ))\
		x(M4x4              , = HashI("M4x4"                  ))\
		x(Mesh              , = HashI("Mesh"                  ))\
		x(Model             , = HashI("Model"                 ))\
		x(Name              , = HashI("Name"                  ))\
		x(Near              , = HashI("Near"                  ))\
		x(NewLine           , = HashI("NewLine"               ))\
		x(NonAffine         , = HashI("NonAffine"             ))\
		x(Normalise         , = HashI("Normalise"             ))\
		x(Normals           , = HashI("Normals"               ))\
		x(NoZTest           , = HashI("NoZTest"               ))\
		x(NoZWrite          , = HashI("NoZWrite"              ))\
		x(O2W               , = HashI("O2W"                   ))\
		x(Orthographic      , = HashI("Orthographic"          ))\
		x(Orthonormalise    , = HashI("Orthonormalise"        ))\
		x(Padding           , = HashI("Padding"               ))\
		x(Param             , = HashI("Param"                 ))\
		x(Parametrics       , = HashI("Parametrics"           ))\
		x(Part              , = HashI("Part"                  ))\
		x(Parts             , = HashI("Parts"                 ))\
		x(Period            , = HashI("Period"                ))\
		x(PerItemColour     , = HashI("PerItemColour"         ))\
		x(PerItemParametrics, = HashI("PerItemParametrics"    ))\
		x(Pie               , = HashI("Pie"                   ))\
		x(Plane             , = HashI("Plane"                 ))\
		x(Point             , = HashI("Point"                 ))\
		x(Polygon           , = HashI("Polygon"               ))\
		x(Pos               , = HashI("Pos"                   ))\
		x(Position          , = HashI("Position"              ))\
		x(Quad              , = HashI("Quad"                  ))\
		x(Quat              , = HashI("Quat"                  ))\
		x(QuatPos           , = HashI("QuatPos"               ))\
		x(Rand4x4           , = HashI("Rand4x4"               ))\
		x(RandColour        , = HashI("RandColour"            ))\
		x(RandOri           , = HashI("RandOri"               ))\
		x(RandPos           , = HashI("RandPos"               ))\
		x(Range             , = HashI("Range"                 ))\
		x(Rect              , = HashI("Rect"                  ))\
		x(Reflectivity      , = HashI("Reflectivity"          ))\
		x(Resolution        , = HashI("Resolution"            ))\
		x(Ribbon            , = HashI("Ribbon"                ))\
		x(RootAnimation     , = HashI("RootAnimation"         ))\
		x(Round             , = HashI("Round"                 ))\
		x(Scale             , = HashI("Scale"                 ))\
		x(ScreenSpace       , = HashI("ScreenSpace"           ))\
		x(Series            , = HashI("Series"                ))\
		x(Size              , = HashI("Size"                  ))\
		x(Smooth            , = HashI("Smooth"                ))\
		x(Solid             , = HashI("Solid"                 ))\
		x(Source            , = HashI("Source"                ))\
		x(Specular          , = HashI("Specular"              ))\
		x(Sphere            , = HashI("Sphere"                ))\
		x(Spline            , = HashI("Spline"                ))\
		x(Square            , = HashI("Square"                ))\
		x(Step              , = HashI("Step"                  ))\
		x(Stretch           , = HashI("Stretch"               ))\
		x(Strikeout         , = HashI("Strikeout"             ))\
		x(Style             , = HashI("Style"                 ))\
		x(Tetra             , = HashI("Tetra"                 ))\
		x(TexCoords         , = HashI("TexCoords"             ))\
		x(Text              , = HashI("Text"                  ))\
		x(TextLayout        , = HashI("TextLayout"            ))\
		x(TextStream        , = HashI("TextStream"            ))\
		x(Texture           , = HashI("Texture"               ))\
		x(TimeRange         , = HashI("TimeRange"             ))\
		x(Transpose         , = HashI("Transpose"             ))\
		x(Triangle          , = HashI("Triangle"              ))\
		x(TriList           , = HashI("TriList"               ))\
		x(TriStrip          , = HashI("TriStrip"              ))\
		x(Tube              , = HashI("Tube"                  ))\
		x(Txfm              , = HashI("Txfm"                  ))\
		x(Underline         , = HashI("Underline"             ))\
		x(Unknown           , = HashI("Unknown"               ))\
		x(Up                , = HashI("Up"                    ))\
		x(Velocity          , = HashI("Velocity"              ))\
		x(Verts             , = HashI("Verts"                 ))\
		x(Video             , = HashI("Video"                 ))\
		x(ViewPlaneZ        , = HashI("ViewPlaneZ"            ))\
		x(Wedges            , = HashI("Wedges"                ))\
		x(Weight            , = HashI("Weight"                ))\
		x(Width             , = HashI("Width"                 ))\
		x(Wireframe         , = HashI("Wireframe"             ))\
		x(XAxis             , = HashI("XAxis"                 ))\
		x(XColumn           , = HashI("XColumn"               ))\
		x(YAxis             , = HashI("YAxis"                 ))\
		x(ZAxis             , = HashI("ZAxis"                 ))
		PR_ENUM_MEMBERS2(PR_ENUM_LDRAW_KEYWORDS)
	};
	PR_ENUM_REFLECTION2(EKeyword, PR_ENUM_LDRAW_KEYWORDS);

	// An enum of just the object types
	enum class ELdrObject : int
	{
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
		PR_ENUM_MEMBERS2(PR_ENUM_LDRAW_OBJECTS)
	};
	PR_ENUM_REFLECTION2(ELdrObject, PR_ENUM_LDRAW_OBJECTS);

	// Arrow styles
	enum class EArrowType : uint8_t
	{
		#define PR_ENUM(x)\
		x(Line        ,= 0)\
		x(Fwd         ,= 1 << 0)\
		x(Back        ,= 1 << 1)\
		x(FwdBack     ,= Fwd | Back)\
		x(_flags_enum ,= 0xFF)
		PR_ENUM_MEMBERS2(PR_ENUM)
	};
	PR_ENUM_REFLECTION2(EArrowType, PR_ENUM);
	#undef PR_ENUM

	// Point styles
	enum class EPointStyle : uint8_t
	{
		#define PR_ENUM(x)\
		x(Square)\
		x(Circle)\
		x(Triangle)\
		x(Star)\
		x(Annulus)
		PR_ENUM_MEMBERS1(PR_ENUM)
	};
	PR_ENUM_REFLECTION1(EPointStyle, PR_ENUM);
	#undef PR_ENUM

	// Camera fields
	enum class ECamField
	{
		None = 0,
		C2W = 1 << 0,
		Focus = 1 << 1,
		Align = 1 << 2,
		Aspect = 1 << 3,
		FovY = 1 << 4,
		Near = 1 << 5,
		Far = 1 << 6,
		Ortho = 1 << 7,
		_flags_enum = 0,
	};

	// Flags for partial update of a model
	enum class EUpdateObject :int
	{
		None = 0,
		Name = 1 << 0,
		Model = 1 << 1,
		Transform = 1 << 2,
		Children = 1 << 3,
		Colour = 1 << 4,
		GroupColour = 1 << 5,
		Reflectivity = 1 << 6,
		Flags = 1 << 7,
		Animation = 1 << 8,
		All = 0x1FF,
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

		// True if the object has animation data
		Animated = 1 << 13,

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

	// Hard types for ldraw::Builder output
	struct textbuf :std::string
	{
		template <typename... Args> textbuf& append(Args... args)
		{
			std::string::append(std::forward<Args>(args)...);
			return *this;
		}
		template <typename T> friend std::basic_ostream<T>& operator << (std::basic_ostream<T>& out, textbuf const& ldr)
		{
			out.write(type_ptr<T>(ldr.data()), ldr.size());
			return out;
		}
		friend std::ptrdiff_t ssize(textbuf const& buf)
		{
			return std::ssize(buf);
		}
	};
	struct bytebuf :std::vector<std::byte>
	{
		template <typename T> friend std::basic_ostream<T>& operator << (std::basic_ostream<T>& out, bytebuf const& ldr)
		{
			out.write(type_ptr<T>(ldr.data()), ldr.size());
			return out;
		}
		friend std::ptrdiff_t ssize(bytebuf const& buf)
		{
			return std::ssize(buf);
		}
	};
}
namespace pr
{
	template <> struct is_string<rdr12::ldraw::textbuf> : std::true_type {};
	template <> struct string_traits<rdr12::ldraw::textbuf const> : string_traits<std::basic_string<char> const> {};
	template <> struct string_traits<rdr12::ldraw::textbuf> : string_traits<std::basic_string<char>> {};
	static_assert(pr::is_string_v<rdr12::ldraw::textbuf>);
}