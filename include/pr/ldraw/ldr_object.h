//***************************************************************************************************
// Ldr Object
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#pragma once

#include <string>
#include <regex>
#include "pr/macros/enum.h"
#include "pr/common/min_max_fix.h"
#include "pr/common/new.h"
#include "pr/common/guid.h"
#include "pr/common/hash.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"
#include "pr/common/user_data.h"
#include "pr/common/flags_enum.h"
#include "pr/common/static_callback.h"
#include "pr/container/vector.h"
#include "pr/maths/maths.h"
#include "pr/str/string.h"
#include "pr/script/reader.h"
#include "pr/renderer11/instances/instance.h"
#include "pr/renderer11/models/model_generator.h"

namespace pr::ldr
{
	// Forwards
	struct LdrObject;
	struct ObjectAttributes;
	using LdrObjectPtr  = RefPtr<LdrObject>;
	using ObjectCont    = vector<LdrObjectPtr, 8>;
	using string32      = string<char, 32>;
	using Location      = script::Loc;

	// Map the compile time hash function to this namespace
	using HashValue = hash::HashValue;
	constexpr HashValue HashI(char const* str) { return hash::HashICT(str); }

	#pragma region Ldr object types
	#define PR_ENUM(x)\
		x(Unknown    ,= HashI("Unknown"   ))\
		x(Point      ,= HashI("Point"     ))\
		x(Line       ,= HashI("Line"      ))\
		x(LineD      ,= HashI("LineD"     ))\
		x(LineStrip  ,= HashI("LineStrip" ))\
		x(LineBox    ,= HashI("LineBox"   ))\
		x(Grid       ,= HashI("Grid"      ))\
		x(Spline     ,= HashI("Spline"    ))\
		x(Arrow      ,= HashI("Arrow"     ))\
		x(Circle     ,= HashI("Circle"    ))\
		x(Pie        ,= HashI("Pie"       ))\
		x(Rect       ,= HashI("Rect"      ))\
		x(Polygon    ,= HashI("Polygon"   ))\
		x(Matrix3x3  ,= HashI("Matrix3x3" ))\
		x(CoordFrame ,= HashI("CoordFrame"))\
		x(Triangle   ,= HashI("Triangle"  ))\
		x(Quad       ,= HashI("Quad"      ))\
		x(Plane      ,= HashI("Plane"     ))\
		x(Ribbon     ,= HashI("Ribbon"    ))\
		x(Box        ,= HashI("Box"       ))\
		x(Bar        ,= HashI("Bar"       ))\
		x(BoxList    ,= HashI("BoxList"   ))\
		x(FrustumWH  ,= HashI("FrustumWH" ))\
		x(FrustumFA  ,= HashI("FrustumFA" ))\
		x(Sphere     ,= HashI("Sphere"    ))\
		x(CylinderHR ,= HashI("CylinderHR"))\
		x(ConeHA     ,= HashI("ConeHA"    ))\
		x(Tube       ,= HashI("Tube"      ))\
		x(Mesh       ,= HashI("Mesh"      ))\
		x(ConvexHull ,= HashI("ConvexHull"))\
		x(Model      ,= HashI("Model"     ))\
		x(Chart      ,= HashI("Chart"     ))\
		x(Group      ,= HashI("Group"     ))\
		x(Text       ,= HashI("Text"      ))\
		x(Instance   ,= HashI("Instance"  ))\
		x(DirLight   ,= HashI("DirLight"  ))\
		x(PointLight ,= HashI("PointLight"))\
		x(SpotLight  ,= HashI("SpotLight" ))\
		x(Custom     ,= HashI("Custom"    ))
	PR_DEFINE_ENUM2(ELdrObject, PR_ENUM);
	#undef PR_ENUM
	#pragma endregion

	#pragma region Ldr script keywords
	#define PR_ENUM(x)\
		x(Name                 ,= HashI("Name"                ))\
		x(Txfm                 ,= HashI("Txfm"                ))\
		x(O2W                  ,= HashI("O2W"                 ))\
		x(M4x4                 ,= HashI("M4x4"                ))\
		x(M3x3                 ,= HashI("M3x3"                ))\
		x(Pos                  ,= HashI("Pos"                 ))\
		x(Up                   ,= HashI("Up"                  ))\
		x(Direction            ,= HashI("Direction"           ))\
		x(Quat                 ,= HashI("Quat"                ))\
		x(QuatPos              ,= HashI("QuatPos"             ))\
		x(Rand4x4              ,= HashI("Rand4x4"             ))\
		x(RandPos              ,= HashI("RandPos"             ))\
		x(RandOri              ,= HashI("RandOri"             ))\
		x(Euler                ,= HashI("Euler"               ))\
		x(Dim                  ,= HashI("Dim"                 ))\
		x(Scale                ,= HashI("Scale"               ))\
		x(Size                 ,= HashI("Size"                ))\
		x(Weight               ,= HashI("Weight"              ))\
		x(Transpose            ,= HashI("Transpose"           ))\
		x(Inverse              ,= HashI("Inverse"             ))\
		x(Normalise            ,= HashI("Normalise"           ))\
		x(Orthonormalise       ,= HashI("Orthonormalise"      ))\
		x(Colour               ,= HashI("Colour"              ))\
		x(ForeColour           ,= HashI("ForeColour"          ))\
		x(BackColour           ,= HashI("BackColour"          ))\
		x(Font                 ,= HashI("Font"                ))\
		x(Stretch              ,= HashI("Stretch"             ))\
		x(Underline            ,= HashI("Underline"           ))\
		x(Strikeout            ,= HashI("Strikeout"           ))\
		x(NewLine              ,= HashI("NewLine"             ))\
		x(CString              ,= HashI("CString"             ))\
		x(AxisId               ,= HashI("AxisId"              ))\
		x(Solid                ,= HashI("Solid"               ))\
		x(Facets               ,= HashI("Facets"              ))\
		x(CornerRadius         ,= HashI("CornerRadius"        ))\
		x(RandColour           ,= HashI("RandColour"          ))\
		x(ColourMask           ,= HashI("ColourMask"          ))\
		x(Reflectivity          ,=HashI("Reflectivity"        ))\
		x(Animation            ,= HashI("Animation"           ))\
		x(Style                ,= HashI("Style"               ))\
		x(Format               ,= HashI("Format"              ))\
		x(TextLayout           ,= HashI("TextLayout"          ))\
		x(Anchor               ,= HashI("Anchor"              ))\
		x(Padding              ,= HashI("Padding"             ))\
		x(Period               ,= HashI("Period"              ))\
		x(Velocity             ,= HashI("Velocity"            ))\
		x(AngVelocity          ,= HashI("AngVelocity"         ))\
		x(Axis                 ,= HashI("Axis"                ))\
		x(Hidden               ,= HashI("Hidden"              ))\
		x(Wireframe            ,= HashI("Wireframe"           ))\
		x(Delimiters           ,= HashI("Delimiters"          ))\
		x(Camera               ,= HashI("Camera"              ))\
		x(LookAt               ,= HashI("LookAt"              ))\
		x(Align                ,= HashI("Align"               ))\
		x(Aspect               ,= HashI("Aspect"              ))\
		x(FovX                 ,= HashI("FovX"                ))\
		x(FovY                 ,= HashI("FovY"                ))\
		x(Fov                  ,= HashI("Fov"                 ))\
		x(Near                 ,= HashI("Near"                ))\
		x(Far                  ,= HashI("Far"                 ))\
		x(Orthographic         ,= HashI("Orthographic"        ))\
		x(Lock                 ,= HashI("Lock"                ))\
		x(Width                ,= HashI("Width"               ))\
		x(Dashed               ,= HashI("Dashed"              ))\
		x(Smooth               ,= HashI("Smooth"              ))\
		x(XAxis                ,= HashI("XAxis"               ))\
		x(YAxis                ,= HashI("YAxis"               ))\
		x(XColumn              ,= HashI("XColumn"             ))\
		x(Closed               ,= HashI("Closed"              ))\
		x(Param                ,= HashI("Param"               ))\
		x(Texture              ,= HashI("Texture"             ))\
		x(Video                ,= HashI("Video"               ))\
		x(Divisions            ,= HashI("Divisions"           ))\
		x(Layers               ,= HashI("Layers"              ))\
		x(Wedges               ,= HashI("Wedges"              ))\
		x(ViewPlaneZ           ,= HashI("ViewPlaneZ"          ))\
		x(Verts                ,= HashI("Verts"               ))\
		x(Normals              ,= HashI("Normals"             ))\
		x(Colours              ,= HashI("Colours"             ))\
		x(TexCoords            ,= HashI("TexCoords"           ))\
		x(Lines                ,= HashI("Lines"               ))\
		x(Faces                ,= HashI("Faces"               ))\
		x(Tetra                ,= HashI("Tetra"               ))\
		x(Part                 ,= HashI("Part"                ))\
		x(GenerateNormals      ,= HashI("GenerateNormals"     ))\
		x(BakeTransform        ,= HashI("BakeTransform"       ))\
		x(Step                 ,= HashI("Step"                ))\
		x(Addr                 ,= HashI("Addr"                ))\
		x(Filter               ,= HashI("Filter"              ))\
		x(Alpha                ,= HashI("Alpha"               ))\
		x(Range                ,= HashI("Range"               ))\
		x(Specular             ,= HashI("Specular"            ))\
		x(ScreenSpace          ,= HashI("ScreenSpace"         ))\
		x(NoZTest              ,= HashI("NoZTest"             ))\
		x(NoZWrite             ,= HashI("NoZWrite"            ))\
		x(Billboard            ,= HashI("Billboard"           ))\
		x(Depth                ,= HashI("Depth"               ))\
		x(LeftHanded           ,= HashI("LeftHanded"          ))\
		x(CastShadow           ,= HashI("CastShadow"          ))
	PR_DEFINE_ENUM2(EKeyword, PR_ENUM);
	#undef PR_ENUM
	#pragma endregion

	#pragma region Enums

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
		_bitwise_operators_allowed,
	};

	// Simple animation styles
	enum class EAnimStyle
	{
		NoAnimation,
		PlayOnce,
		PlayReverse,
		PingPong,
		PlayContinuous,
	};

	// Flags for partial update of a model
	enum class EUpdateObject :unsigned int
	{
		None         = 0U,
		All          = ~0U,
		Name         = 1 << 0,
		Model        = 1 << 1,
		Transform    = 1 << 2,
		Children     = 1 << 3,
		Colour       = 1 << 4,
		ColourMask   = 1 << 5,
		Reflectivity = 1 << 6,
		Flags        = 1 << 7,
		Animation    = 1 << 8,
		_bitwise_operators_allowed,
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

		// Set when an object is selected. The meaning of 'selected' is up to the application
		Selected = 1 << 8,

		// Doesn't contribute to the bounding box on an object.
		BBoxExclude = 1 << 9,

		// Should not be included when determining the bounds of a scene.
		SceneBoundsExclude = 1 << 10,

		// Ignored for hit test ray casts
		HitTestExclude = 1 << 11,

		// Bitwise operators
		_bitwise_operators_allowed,
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

	#pragma endregion

	#pragma region Types

	// An instance type for line drawer stock objects
	#define PR_RDR_INST(x)\
		x(m4x4          ,m_i2w   ,rdr::EInstComp::I2WTransform)\
		x(rdr::ModelPtr ,m_model ,rdr::EInstComp::ModelPtr    )
	PR_RDR_DEFINE_INSTANCE(StockInstance, PR_RDR_INST);
	#undef PR_RDR_INST

	// An instance type for object bounding boxes
	#define PR_RDR_INST(x)\
		x(m4x4            ,m_i2w    ,rdr::EInstComp::I2WTransform   )\
		x(rdr::ModelPtr   ,m_model  ,rdr::EInstComp::ModelPtr       )
	PR_RDR_DEFINE_INSTANCE(BBoxInstance, PR_RDR_INST);
	#undef PR_RDR_INST

	// An instance for passing to the renderer
	// A renderer instance type for the body
	// Note: don't use 'm_i2w' to control the object transform, use m_o2p in the LdrObject instead
	#define PR_RDR_INST(x) \
		x(m4x4            ,m_i2w    ,rdr::EInstComp::I2WTransform       )/*     16 bytes */\
		x(m4x4            ,m_c2s    ,rdr::EInstComp::C2SOptional        )/*     16 bytes */\
		x(rdr::ModelPtr   ,m_model  ,rdr::EInstComp::ModelPtr           )/* 4 or 8 bytes */\
		x(Colour32        ,m_colour ,rdr::EInstComp::TintColour32       )/*      4 bytes */\
		x(float           ,m_env    ,rdr::EInstComp::EnvMapReflectivity )/*      4 bytes */\
		x(rdr::SKOverride ,m_sko    ,rdr::EInstComp::SortkeyOverride    )/*      8 bytes */\
		x(rdr::BSBlock    ,m_bsb    ,rdr::EInstComp::BSBlock            )/*    296 bytes */\
		x(rdr::DSBlock    ,m_dsb    ,rdr::EInstComp::DSBlock            )/*     60 bytes */\
		x(rdr::RSBlock    ,m_rsb    ,rdr::EInstComp::RSBlock            )/*     44 bytes */
	PR_RDR_DEFINE_INSTANCE(RdrInstance, PR_RDR_INST);
	#undef PR_RDR_INST

	// Attributes (with defaults) for an LdrObject
	struct ObjectAttributes
	{
		ELdrObject   m_type;     // Object type
		string32     m_name;     // Name of the object
		Colour32 m_colour;   // Base colour of the object

		ObjectAttributes() :m_type(ELdrObject::Unknown) ,m_name("unnamed") ,m_colour(Colour32White) {}
		ObjectAttributes(ELdrObject type) :m_type(type), m_name("unnamed") ,m_colour(Colour32White) {}
		ObjectAttributes(ELdrObject type, char const* name) :m_type(type), m_name(name) ,m_colour(Colour32White) {}
		ObjectAttributes(ELdrObject type, char const* name, Colour32 colour) :m_type(type), m_name(name) ,m_colour(colour) {}
	};

	// Mesh creation data
	using MeshCreationData = rdr::MeshCreationData;

	// Info on how to animate a ldr object
	struct Animation
	{
		EAnimStyle m_style;
		float      m_period;       // Seconds
		v4         m_velocity;     // Linear velocity of the animation in m/s
		v4         m_ang_velocity; // Angular velocity of the animation in rad/s

		Animation()
			:m_style(EAnimStyle::NoAnimation)
			,m_period(1.0f)
			,m_velocity(v4Zero)
			,m_ang_velocity(v4Zero)
		{}

		// Return a transform representing the offset
		// added by this object at time 'time_s'
		m4x4 Step(float time_s) const
		{
			if (m_style == EAnimStyle::NoAnimation)
				return m4x4Identity;

			auto t = 0.0f;
			switch (m_style)
			{
			default: throw std::exception("Unknown animation style");
			case EAnimStyle::NoAnimation:
				break;
			case EAnimStyle::PlayOnce:
				t = time_s < m_period ? time_s : m_period;
				break;
			case EAnimStyle::PlayReverse:
				t = time_s < m_period ? m_period - time_s : 0.0f;
				break;
			case EAnimStyle::PingPong:
				t = Fmod(time_s, 2.0f*m_period) >= m_period
					? m_period - Fmod(time_s, m_period)
					: Fmod(time_s, m_period);
				break;
			case EAnimStyle::PlayContinuous:
				t = time_s;
				break;
			}

			return m4x4::Transform(m_ang_velocity*t, m_velocity*t + v4Origin);
		}
	};

	// Add to scene callback
	using AddToSceneCB = StaticCB<void, LdrObject*, rdr::Scene const&>;

	#pragma endregion

	#pragma region Parse Result

	// The results of parsing ldr script
	struct ParseResult
	{
		using ModelLookup = std::unordered_map<size_t, rdr::ModelPtr>;

		ObjectCont  m_objects;    // Reference to the objects container to fill
		ModelLookup m_models;     // A lookup map for models based on hashed object name
		Camera      m_cam;        // Camera description has been read
		ECamField   m_cam_fields; // Bitmask of fields in 'm_cam' that were given in the camera description
		bool        m_wireframe;  // True if '*Wireframe' was read in the script
			
		ParseResult()
			:m_objects()
			,m_models()
			,m_cam()
			,m_cam_fields()
			,m_wireframe()
		{}
	};

	#pragma endregion

	// A line drawer object
	struct LdrObject
		:RdrInstance
		,RefCount<LdrObject>
	{
		// Note: try not to use the RdrInstance members for things other than rendering
		// they can temporarily have different models/transforms/etc during rendering of
		// object bounding boxes etc.
		m4x4         m_o2p;           // Object to parent transform (or object to world if this is a top level object)
		ELdrObject   m_type;          // Object type
		LdrObject*   m_parent;        // The parent of this object, 0 for top level instances.
		ObjectCont   m_child;         // A container of pointers to child instances
		string32     m_name;          // A name for the object
		GUID         m_context_id;    // The id of the context this instance was created in
		Colour32     m_base_colour;   // The original colour of this object
		uint         m_colour_mask;   // A bit mask for applying the base colour to child objects
		Animation    m_anim;          // Animation data
		BBoxInstance m_bbox_instance; // Used for rendering the bounding box for this instance
		Sub          m_screen_space;  // True if this object should be rendered in screen space
		ELdrFlags    m_flags;         // Property flags controlling meta behaviour of the object
		UserData     m_user_data;     // User data

		LdrObject(ObjectAttributes const& attr, LdrObject* parent, Guid const& context_id);
		~LdrObject();

		// Return the type and name of this object
		string32 TypeAndName() const;

		// Called just prior to this object being added to a scene.
		// Allows handlers to change the object's 'i2w' transform, visibility, etc.
		EventHandler<LdrObject&, rdr::Scene const&, true> OnAddToScene;

		// Recursively add this object and its children to a scene
		void AddToScene(rdr::Scene& scene, float time_s = 0.0f, m4x4 const* p2w = &m4x4Identity);

		// Recursively add the bounding box instance for this object using 'bbox_model'
		// located and scaled to the transform and box of this object
		void AddBBoxToScene(rdr::Scene& scene, rdr::ModelPtr bbox_model, float time_s = 0.0f, m4x4 const* p2w = &m4x4Identity);

		// Notes:
		//  - Methods with a 'name' parameter apply an operation on this object
		//    or any of its child objects that match 'name'. If 'name' is null,
		//    then the change is applied to this object only. If 'name' is "",
		//    then the change is applied to this object and all children recursively.
		//	  Otherwise, the change is applied to all child objects that match name.
		//  - If 'name' begins with '#' then the name parameter is treated as a regular
		//    expression.

		// Apply an operation on this object or any of its child objects that match 'name'.
		// 'func' should have a signature: 'bool func(ldr::LdrObject* obj);' returning false to 'quick-out'.
		// 'obj' is a recursion parameter, callers should use 'nullptr'
		// Returns 'true' if 'func' always returns 'true'.
		template <typename TFunc> bool Apply(TFunc func, char const* name = nullptr, LdrObject* obj = nullptr) const
		{
			if (obj == nullptr)
			{
				// The 'const-ness' of this function depends on 'func'
				obj = const_cast<LdrObject*>(this);
			}
			if (name == nullptr)
			{
				if (!func(obj))
					return false;
			}
			else if (name[0] == '\0')
			{
				if (!func(obj)) return false;
				for (auto& child : obj->m_child)
					if (!Apply(func, name, child.m_ptr))
						return false;
			}
			else
			{
				if ((name[0] == '#' && std::regex_match(std::string(obj->m_name.c_str()), std::regex(&name[1]))) || obj->m_name == name)
				{
					if (!func(obj))
						return false;
				}
				for (auto& child : obj->m_child)
					if (!Apply(func, name, child.m_ptr))
						return false;
			}
			return true;
		}

		// Get the first child object of this object that matches 'name' (see Apply)
		LdrObject* Child(char const* name) const;

		// Get a child object of this object by index
		LdrObject* Child(int index) const;

		// Get/Set the object to world transform of this object or the first child object matching 'name' (see Apply)
		// Note, it is more efficient to set O2P.
		m4x4 O2W(char const* name = nullptr) const;
		void O2W(m4x4 const& o2w, char const* name = nullptr);

		// Get/Set the object to parent transform of this object or child objects matching 'name' (see Apply)
		m4x4 O2P(char const* name = nullptr) const;
		void O2P(m4x4 const& o2p, char const* name = nullptr);

		// Get/Set the visibility of this object or child objects matching 'name' (see Apply)
		bool Visible(char const* name = nullptr) const;
		void Visible(bool visible, char const* name = nullptr);

		// Get/Set the render mode for this object or child objects matching 'name' (see Apply)
		bool Wireframe(char const* name = nullptr) const;
		void Wireframe(bool wireframe, char const* name = nullptr);

		// Get/Set screen space rendering mode for this object (and all child objects)
		bool ScreenSpace() const;
		void ScreenSpace(bool screen_space3);

		// Get/Set meta behaviour flags for this object or child objects matching 'name' (see Apply)
		ELdrFlags Flags(char const* name = nullptr) const;
		void Flags(ELdrFlags flags, bool state, char const* name = nullptr);

		// Get/Set the render group for this object or child objects matching 'name' (see Apply)
		rdr::ESortGroup SortGroup(char const* name = nullptr) const;
		void SortGroup(rdr::ESortGroup grp, char const* name = nullptr);

		// Get/Set the nugget flags for this object or child objects matching 'name' (see Apply)
		rdr::ENuggetFlag NuggetFlags(char const* name, int index) const;
		void NuggetFlags(rdr::ENuggetFlag flags, bool state, char const* name, int index);

		// Get/Set the nugget tint colour for this object or child objects matching 'name' (see Apply)
		Colour32 NuggetTint(char const* name, int index) const;
		void NuggetTint(Colour32 tint, char const* name, int index);

		// Get/Set the colour of this object or child objects matching 'name' (see Apply)
		// For 'Get', the colour of the first object to match 'name' is returned
		// For 'Set', the object base colour is not changed, only the tint colour = tint
		Colour32 Colour(bool base_colour, char const* name = nullptr) const;
		void Colour(Colour32 colour, uint mask, char const* name = nullptr, EColourOp op = EColourOp::Overwrite, float op_value = 0.0f);

		// Restore the colour to the initial colour for this object or child objects matching 'name' (see Apply)
		void ResetColour(char const* name = nullptr);

		// Get/Set the reflectivity of this object or child objects matching 'name' (see Apply)
		float Reflectivity(char const* name = nullptr) const;
		void Reflectivity(float reflectivity, char const* name = nullptr);

		// Set the texture on this object or child objects matching 'name' (see Apply)
		// Note for difference mode drawlist management, if the object is currently in
		// one or more drawlists (i.e. added to a scene) it will need to be removed and
		// re-added so that the sort order is correct.
		void SetTexture(rdr::Texture2D* tex, char const* name = nullptr);

		// Return the bounding box for this object in model space
		// To convert this to parent space multiply by 'm_o2p'
		// e.g. BBoxMS() for "*Box { 1 2 3 *o2w{*rand} }" will return bb.m_centre = origin, bb.m_radius = (1,2,3)
		template <typename Pred> BBox BBoxMS(bool include_children, Pred pred, float time_s = 0.0f, m4x4 const* p2w = &m4x4Identity) const
		{
			auto i2w = *p2w * m_anim.Step(time_s);

			// Start with the bbox for this object
			BBox bbox = BBoxReset;
			if (m_model && !AllSet(m_flags,ELdrFlags::BBoxExclude) && pred(*this)) // Get the bbox from the graphics model
			{
				auto bb = i2w * m_model->m_bbox;
				if (bb.valid()) Encompass(bbox, bb);
			}
			if (include_children) // Add the bounding boxes of the children
			{
				for (auto& child : m_child)
				{
					auto c2w = i2w * child->m_o2p;
					auto cbbox = child->BBoxMS(include_children, pred, time_s, &c2w);
					if (cbbox.valid()) Encompass(bbox, cbbox);
				}
			}
			return bbox;
		}
		BBox BBoxMS(bool include_children) const
		{
			return BBoxMS(include_children, [](LdrObject const&){ return true; });
		}

		// Return the bounding box for this object in world space.
		// If this is a top level object, this will be equivalent to 'm_o2p * BBoxMS()'
		// If not then, then the returned bbox will be transformed to the top level object space
		template <typename Pred> BBox BBoxWS(bool include_children, Pred pred, float time_s = 0.0f) const
		{
			// Get the combined o2w transform;
			m4x4 o2w = m_o2p;
			for (ldr::LdrObject* parent = m_parent; parent; parent = parent->m_parent)
				o2w = parent->m_o2p * parent->m_anim.Step(time_s) * o2w;

			return BBoxMS(include_children, pred, time_s, &o2w);
		}
		BBox BBoxWS(bool include_children) const
		{
			return BBoxWS(include_children, [](LdrObject const&){ return true; });
		}

		// Add/Remove 'child' as a child of this object
		void AddChild(LdrObjectPtr& child);
		LdrObjectPtr RemoveChild(LdrObjectPtr& child);
		LdrObjectPtr RemoveChild(size_t i);
		void RemoveAllChildren();

		// Predicate for matching this object by context id
		struct MatchId
		{
			GUID m_id;
			MatchId(GUID const& id) :m_id(id) {}
			bool operator()(LdrObject const& obj) const { return obj.m_context_id == m_id; }
			bool operator()(LdrObject const* obj) const { return obj && obj->m_context_id == m_id; }
		};

		// Called when there are no more references to this object
		static void RefCountZero(RefCount<LdrObject>* doomed);
		long AddRef() const;
		long Release() const;
	};

	// LdrObject Creation functions *********************************************

	// Callback function type used during script parsing
	// 'bool function(Guid context_id, ParseResult& out, Location const& loc, bool complete)'
	// Returns 'true' to continue parsing, false to abort parsing.
	using ParseProgressCB = StaticCB<bool, Guid const&, ParseResult const&, Location const&, bool>;

	// Parse the ldr script in 'reader' adding the results to 'out'.
	// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
	// There is synchronisation in the renderer for creating/allocating models. The calling thread must control the
	// life-times of the script reader, the parse output, and the 'store' container it refers to.
	void Parse(
		Renderer& rdr,                          // The renderer to create models for
		script::Reader& reader,                 // The source of the script
		ParseResult& out,                       // The results of parsing the script
		Guid const& context_id = GuidZero,      // The context id to assign to each created object
		ParseProgressCB progress_cb = nullptr); // Progress callback

	// Parse ldr script from a text file.
	// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
	// There is synchronisation in the renderer for creating/allocating models. The calling thread must control the
	// life-times of the script reader, the parse output, and the 'store' container it refers to.
	inline void ParseFile(
		Renderer& rdr,                         // The renderer to create models for
		wchar_t const* filename,               // The file containing the ldr script
		ParseResult& out,                      // The results of parsing the script
		Guid const& context_id = GuidZero,     // The context id to assign to each created object
		ParseProgressCB progress_cb = nullptr) // Progress callback
	{
		script::FileSrc src(filename);
		script::Reader reader(src);
		Parse(rdr, reader, out, context_id, progress_cb);
	}

	// Parse ldr script from a string
	// This function can be called from any thread (main or worker) and may be called concurrently by multiple threads.
	// There is synchronisation in the renderer for creating/allocating models. The calling thread must control the
	// life-times of the script reader, the parse output, and the 'store' container it refers to.
	template <typename Char>
	inline void ParseString(
		Renderer& rdr,                         // The reader to create models for
		Char const* ldr_script,                // The string containing the script
		ParseResult& out,                      // The results of parsing the script
		Guid const& context_id = GuidZero,     // The context id to assign to each created object
		ParseProgressCB progress_cb = nullptr) // Progress callback
	{
		using namespace pr::script;
		StringSrc src(ldr_script);
		Reader reader(src);
		Parse(rdr, reader, out, context_id, progress_cb);
	}

	// Callback function for editing a dynamic model
	// This callback is intentionally low level, providing the whole model for editing.
	// Remember to update the bounding box, vertex and index ranges, and regenerate nuggets.
	typedef void (__stdcall *EditObjectCB)(rdr::Model* model, void* ctx, Renderer& rdr);

	// Create an ldr object from creation data.
	LdrObjectPtr Create(
		Renderer& rdr,                      // The reader to create models for
		ObjectAttributes attr,              // Object attributes to use with the created object
		MeshCreationData const& cdata,      // Model creation data
		Guid const& context_id = GuidZero); // The context id to assign to the object
		
	// Create an instance of an existing ldr object.
	LdrObjectPtr CreateInstance(
		LdrObject const* existing);         // The existing object whose model the instance will use.

	// Create an ldr object using a callback to populate the model data.
	// Objects created by this method will have dynamic usage and are suitable for updating every frame via the 'Edit' function.
	LdrObjectPtr CreateEditCB(
		Renderer& rdr,                      // The reader to create models for
		ObjectAttributes attr,              // Object attributes to use with the created object
		int vcount,                         // The number of verts to create the model with
		int icount,                         // The number of indices to create the model with
		int ncount,                         // The number of nuggets to create the model with
		EditObjectCB edit_cb,               // The callback function, called after the model is created, to populate the model data
		void* ctx,                          // Callback user context
		Guid const& context_id = GuidZero); // The context id to assign to the object

	// Modify the geometry of an LdrObject
	void Edit(Renderer& rdr, LdrObject* object, EditObjectCB edit_cb, void* ctx);

	// Update 'object' with info from 'desc'. 'keep' describes the properties of 'object' to update
	void Update(Renderer& rdr, LdrObject* object, script::Reader& reader, EUpdateObject flags = EUpdateObject::All);

	// Remove all objects from 'objects' that have a context id matching one in 'doomed' and not in 'excluded'
	// If 'doomed' is 0, all are assumed doomed. If 'excluded' is 0, none are assumed excluded
	// 'excluded' is considered after 'doomed' so if any context ids are in both arrays, they will be excluded.
	void Remove(ObjectCont& objects, Guid const* doomed, std::size_t dcount, Guid const* excluded, std::size_t ecount);

	// Remove 'obj' from 'objects'
	void Remove(ObjectCont& objects, LdrObject* obj);

	// Generate a scene that demos the supported object types and modifiers.
	std::string CreateDemoScene();

	// Return the auto completion templates
	std::string AutoCompleteTemplates();
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{

	}
}
#endif
