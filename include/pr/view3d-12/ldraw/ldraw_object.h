//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/ldraw/ldraw.h"

namespace pr::rdr12::ldraw
{
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

	// Attributes (with defaults) for an LdrObject
	struct ObjectAttributes
	{
		ELdrObject m_type;     // Object type
		string32   m_name;     // Name of the object
		Colour32   m_colour;   // Base colour of the object

		ObjectAttributes() :m_type(ELdrObject::Unknown) ,m_name("unnamed") ,m_colour(Colour32White) {}
		ObjectAttributes(ELdrObject type) :m_type(type), m_name("unnamed") ,m_colour(Colour32White) {}
		ObjectAttributes(ELdrObject type, char const* name) :m_type(type), m_name(name) ,m_colour(Colour32White) {}
		ObjectAttributes(ELdrObject type, char const* name, Colour32 colour) :m_type(type), m_name(name) ,m_colour(colour) {}
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

	#pragma region Instance Types

	// An instance type for line drawer stock objects
	#define PR_RDR_INST(x)\
		x(m4x4     ,m_i2w   ,EInstComp::I2WTransform)\
		x(ModelPtr ,m_model ,EInstComp::ModelPtr    )
	PR_RDR12_DEFINE_INSTANCE(StockInstance, PR_RDR_INST);
	#undef PR_RDR_INST

	// An instance type for object bounding boxes
	#define PR_RDR_INST(x)\
		x(m4x4     ,m_i2w   ,EInstComp::I2WTransform   )\
		x(ModelPtr ,m_model ,EInstComp::ModelPtr       )
	PR_RDR12_DEFINE_INSTANCE(BBoxInstance, PR_RDR_INST);
	#undef PR_RDR_INST

	// An instance for passing to the renderer
	// A renderer instance type for the body
	// Note: don't use 'm_i2w' to control the object transform, use m_o2p in the LdrObject instead
	#define PR_RDR_INST(x) \
		x(m4x4       ,m_i2w    ,EInstComp::I2WTransform       )/*     16 bytes, align 16 */\
		x(m4x4       ,m_c2s    ,EInstComp::C2SOptional        )/*     16 bytes, align 16 */\
	 	x(PipeStates ,m_pso    ,EInstComp::PipeStates         )/*    104 bytes, align 8 */\
		x(ModelPtr   ,m_model  ,EInstComp::ModelPtr           )/* 4 or 8 bytes, align 8 */\
		x(Colour32   ,m_colour ,EInstComp::TintColour32       )/*      4 bytes, align 4 */\
		x(float      ,m_env    ,EInstComp::EnvMapReflectivity )/*      4 bytes, align 4 */\
		x(EInstFlag  ,m_iflags ,EInstComp::Flags              )/*      4 bytes, align 4 */\
		x(SKOverride ,m_sko    ,EInstComp::SortkeyOverride    )/*      8 bytes, align 4 */
	PR_RDR12_DEFINE_INSTANCE(RdrInstance , PR_RDR_INST);
	#undef PR_RDR_INST

	#pragma endregion

	// A line drawer object
	struct LdrObject
		:RdrInstance
		,RefCounted<LdrObject>
	{
		// Note: try not to use the RdrInstance members for things other than rendering
		// they can temporarily have different models/transforms/etc during rendering of
		// object bounding boxes etc.
		m4x4         m_o2p;           // Object to parent transform (or object to world if this is a top level object)
		ELdrObject   m_type;          // Object type
		LdrObject*   m_parent;        // The parent of this object, nullptr for top level instances.
		ObjectCont   m_child;         // A container of pointers to child instances
		string32     m_name;          // A name for the object
		GUID         m_context_id;    // The id of the context this instance was created in
		Colour32     m_base_colour;   // The original colour of this object
		uint32_t     m_colour_mask;   // A bit mask for applying the base colour to child objects
		Animation    m_anim;          // Animation data
		BBoxInstance m_bbox_instance; // Used for rendering the bounding box for this instance
		Sub          m_screen_space;  // True if this object should be rendered in screen space
		ELdrFlags    m_ldr_flags;     // Property flags controlling meta behaviour of the object
		UserData     m_user_data;     // User data

		LdrObject(ObjectAttributes const& attr, LdrObject* parent, Guid const& context_id);
		~LdrObject();

		// Return the type and name of this object
		string32 TypeAndName() const;

		// Called just prior to this object being added to a scene.
		// Allows handlers to change the object's 'i2w' transform, visibility, etc.
		EventHandler<LdrObject&, Scene const&, true> OnAddToScene;

		// Recursively add this object and its children to a scene
		void AddToScene(Scene& scene, float time_s = 0.0f, m4x4 const* p2w = &m4x4Identity, ELdrFlags parent_flags = ELdrFlags::None);

		// Recursively add the bounding box instance for this object using 'bbox_model'
		// located and scaled to the transform and box of this object
		void AddBBoxToScene(Scene& scene, float time_s = 0.0f, m4x4 const* p2w = &m4x4Identity, ELdrFlags parent_flags = ELdrFlags::None);

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
				{
					if (!Apply(func, name, child.m_ptr))
						return false;
				}
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

		// Get/Set the visibility of normals for this object or child objects matching 'name' (see Apply)
		bool Normals(char const* name = nullptr) const;
		void Normals(bool wireframe, char const* name = nullptr);

		// Get/Set screen space rendering mode for this object (and all child objects)
		bool ScreenSpace() const;
		void ScreenSpace(bool screen_space3);

		// Get/Set meta behaviour flags for this object or child objects matching 'name' (see Apply)
		ELdrFlags Flags(char const* name = nullptr) const;
		void Flags(ELdrFlags flags, bool state, char const* name = nullptr);

		// Get/Set the render group for this object or child objects matching 'name' (see Apply)
		ESortGroup SortGroup(char const* name = nullptr) const;
		void SortGroup(ESortGroup grp, char const* name = nullptr);

		// Get/Set the nugget flags for this object or child objects matching 'name' (see Apply)
		ENuggetFlag NuggetFlags(char const* name, int index) const;
		void NuggetFlags(ENuggetFlag flags, bool state, char const* name, int index);

		// Get/Set the nugget tint colour for this object or child objects matching 'name' (see Apply)
		Colour32 NuggetTint(char const* name, int index) const;
		void NuggetTint(Colour32 tint, char const* name, int index);

		// Get/Set the colour of this object or child objects matching 'name' (see Apply)
		// For 'Get', the colour of the first object to match 'name' is returned
		// For 'Set', the object base colour is not changed, only the tint colour = tint
		Colour32 Colour(bool base_colour, char const* name = nullptr) const;
		void Colour(Colour32 colour, uint32_t mask, char const* name = nullptr, EColourOp op = EColourOp::Overwrite, float op_value = 0.0f);

		// Restore the colour to the initial colour for this object or child objects matching 'name' (see Apply)
		void ResetColour(char const* name = nullptr);

		// Get/Set the reflectivity of this object or child objects matching 'name' (see Apply)
		float Reflectivity(char const* name = nullptr) const;
		void Reflectivity(float reflectivity, char const* name = nullptr);

		// Set the texture on this object or child objects matching 'name' (see Apply).
		// Note for difference mode drawlist management, if the object is currently in one or more drawlists
		// (i.e. added to a scene) it will need to be removed and re-added so that the sort order is correct.
		void SetTexture(Texture2D* tex, char const* name = nullptr);

		// Set the sampler on the nuggets of this object or child objects matching 'name' (see Apply).
		// Note for 'difference-mode' drawlist management: if the object is currently in one or more drawlists
		// (i.e. added to a scene) it will need to be removed and re-added so that the sort order is correct.
		void SetSampler(Sampler* sam, char const* name = nullptr);

		// Return the bounding box for this object in model space
		// To convert this to parent space multiply by 'm_o2p'
		// e.g. BBoxMS() for "*Box { 1 2 3 *o2w{*rand} }" will return bb.m_centre = origin, bb.m_radius = (1,2,3)
		BBox BBoxMS(bool include_children, std::function<bool(LdrObject const&)> pred, float time_s = 0.0f, m4x4 const* p2w = nullptr, ELdrFlags parent_flags = ELdrFlags::None) const;
		BBox BBoxMS(bool include_children) const;

		// Return the bounding box for this object in world space.
		// If this is a top level object, this will be equivalent to 'm_o2p * BBoxMS()'
		// If not then, then the returned bbox will be transformed to the top level object space
		BBox BBoxWS(bool include_children, std::function<bool(LdrObject const&)> pred, float time_s = 0.0f) const;
		BBox BBoxWS(bool include_children) const;

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
}
