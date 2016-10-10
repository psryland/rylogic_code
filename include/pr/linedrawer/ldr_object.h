//***************************************************************************************************
// Ldr Object
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#pragma once

#include <string>
#include <regex>
#include "pr/macros/enum.h"
#include "pr/common/min_max_fix.h"
#include "pr/common/hash.h"
#include "pr/common/new.h"
#include "pr/common/guid.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"
#include "pr/common/user_data.h"
#include "pr/container/vector.h"
#include "pr/maths/maths.h"
#include "pr/str/string.h"
#include "pr/script/reader.h"
#include "pr/renderer11/instance.h"
#include "pr/renderer11/models/model_generator.h"

namespace pr
{
	namespace ldr
	{
		// Forwards
		struct LdrObject;
		struct ObjectAttributes;
		using LdrObjectPtr  = pr::RefPtr<LdrObject>;
		using ObjectCont    = pr::vector<LdrObjectPtr, 8>;
		using string32      = pr::string<char, 32>;

		// Map the compile time hash function to this namespace
		using HashValue = pr::hash::HashValue;
		constexpr HashValue HashI(char const* str) { return pr::hash::HashI(str); }

		#pragma region Ldr object types
		#define PR_ENUM(x)\
			x(Unknown    ,= HashI("Unknown"   ))\
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
			x(Matrix3x3  ,= HashI("Matrix3x3" ))\
			x(CoordFrame ,= HashI("CoordFrame"))\
			x(Triangle   ,= HashI("Triangle"  ))\
			x(Quad       ,= HashI("Quad"      ))\
			x(Plane      ,= HashI("Plane"     ))\
			x(Ribbon     ,= HashI("Ribbon"    ))\
			x(Box        ,= HashI("Box"       ))\
			x(BoxLine    ,= HashI("BoxLine"   ))\
			x(BoxList    ,= HashI("BoxList"   ))\
			x(FrustumWH  ,= HashI("FrustumWH" ))\
			x(FrustumFA  ,= HashI("FrustumFA" ))\
			x(Sphere     ,= HashI("Sphere"    ))\
			x(CylinderHR ,= HashI("CylinderHR"))\
			x(ConeHA     ,= HashI("ConeHA"    ))\
			x(Mesh       ,= HashI("Mesh"      ))\
			x(ConvexHull ,= HashI("ConvexHull"))\
			x(Model      ,= HashI("Model"     ))\
			x(Group      ,= HashI("Group"     ))\
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
			x(Txfm               ,= HashI("Txfm"              ))\
			x(O2W                ,= HashI("O2W"               ))\
			x(M4x4               ,= HashI("M4x4"              ))\
			x(M3x3               ,= HashI("M3x3"              ))\
			x(Pos                ,= HashI("Pos"               ))\
			x(Up                 ,= HashI("Up"                ))\
			x(Direction          ,= HashI("Direction"         ))\
			x(Quat               ,= HashI("Quat"              ))\
			x(Rand4x4            ,= HashI("Rand4x4"           ))\
			x(RandPos            ,= HashI("RandPos"           ))\
			x(RandOri            ,= HashI("RandOri"           ))\
			x(Euler              ,= HashI("Euler"             ))\
			x(Scale              ,= HashI("Scale"             ))\
			x(Transpose          ,= HashI("Transpose"         ))\
			x(Inverse            ,= HashI("Inverse"           ))\
			x(Normalise          ,= HashI("Normalise"         ))\
			x(Orthonormalise     ,= HashI("Orthonormalise"    ))\
			x(Colour             ,= HashI("Colour"            ))\
			x(Solid              ,= HashI("Solid"             ))\
			x(Facets             ,= HashI("Facets"            ))\
			x(CornerRadius       ,= HashI("CornerRadius"      ))\
			x(RandColour         ,= HashI("RandColour"        ))\
			x(ColourMask         ,= HashI("ColourMask"        ))\
			x(Animation          ,= HashI("Animation"         ))\
			x(Style              ,= HashI("Style"             ))\
			x(Period             ,= HashI("Period"            ))\
			x(Velocity           ,= HashI("Velocity"          ))\
			x(AngVelocity        ,= HashI("AngVelocity"       ))\
			x(Axis               ,= HashI("Axis"              ))\
			x(Hidden             ,= HashI("Hidden"            ))\
			x(Wireframe          ,= HashI("Wireframe"         ))\
			x(Delimiters         ,= HashI("Delimiters"        ))\
			x(Clear              ,= HashI("Clear"             ))\
			x(Camera             ,= HashI("Camera"            ))\
			x(LookAt             ,= HashI("LookAt"            ))\
			x(Align              ,= HashI("Align"             ))\
			x(Aspect             ,= HashI("Aspect"            ))\
			x(FovX               ,= HashI("FovX"              ))\
			x(FovY               ,= HashI("FovY"              ))\
			x(Fov                ,= HashI("Fov"               ))\
			x(Near               ,= HashI("Near"              ))\
			x(Far                ,= HashI("Far"               ))\
			x(AbsoluteClipPlanes ,= HashI("AbsoluteClipPlanes"))\
			x(Orthographic       ,= HashI("Orthographic"      ))\
			x(Lock               ,= HashI("Lock"              ))\
			x(Coloured           ,= HashI("Coloured"          ))\
			x(Width              ,= HashI("Width"             ))\
			x(Smooth             ,= HashI("Smooth"            ))\
			x(Param              ,= HashI("Param"             ))\
			x(Texture            ,= HashI("Texture"           ))\
			x(Video              ,= HashI("Video"             ))\
			x(Divisions          ,= HashI("Divisions"         ))\
			x(Layers             ,= HashI("Layers"            ))\
			x(Wedges             ,= HashI("Wedges"            ))\
			x(ViewPlaneZ         ,= HashI("ViewPlaneZ"        ))\
			x(Verts              ,= HashI("Verts"             ))\
			x(Normals            ,= HashI("Normals"           ))\
			x(Colours            ,= HashI("Colours"           ))\
			x(TexCoords          ,= HashI("TexCoords"         ))\
			x(Lines              ,= HashI("Lines"             ))\
			x(Faces              ,= HashI("Faces"             ))\
			x(Tetra              ,= HashI("Tetra"             ))\
			x(GenerateNormals    ,= HashI("GenerateNormals"   ))\
			x(BakeTransform      ,= HashI("BakeTransform"     ))\
			x(Step               ,= HashI("Step"              ))\
			x(Addr               ,= HashI("Addr"              ))\
			x(Filter             ,= HashI("Filter"            ))\
			x(Range              ,= HashI("Range"             ))\
			x(Specular           ,= HashI("Specular"          ))\
			x(CastShadow         ,= HashI("CastShadow"        ))
		PR_DEFINE_ENUM2(EKeyword, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Enums

		// Simple animation styles
		#define PR_ENUM(x)\
			x(NoAnimation)\
			x(PlayOnce)\
			x(PlayReverse)\
			x(PingPong)\
			x(PlayContinuous)
		PR_DEFINE_ENUM1(EAnimStyle, PR_ENUM);
		#undef PR_ENUM

		// Flags for partial update of a model
		#define PR_ENUM(x)\
			x(None       ,= 0)\
			x(All        ,= ~0U)\
			x(Name       ,= 1 << 0)\
			x(Model      ,= 1 << 1)\
			x(Transform  ,= 1 << 2)\
			x(Children   ,= 1 << 3)\
			x(Colour     ,= 1 << 4)\
			x(ColourMask ,= 1 << 5)\
			x(Wireframe  ,= 1 << 6)\
			x(Visibility ,= 1 << 7)\
			x(Animation  ,= 1 << 8)\
			x(StepData   ,= 1 << 9)
		PR_DEFINE_ENUM2_FLAGS(EUpdateObject, PR_ENUM);
		#undef PR_ENUM
		
		#pragma endregion

		#pragma region Types

		// An instance type for line drawer stock objects
		#define PR_RDR_INST(x)\
			x(pr::m4x4          ,m_i2w   ,pr::rdr::EInstComp::I2WTransform)\
			x(pr::rdr::ModelPtr ,m_model ,pr::rdr::EInstComp::ModelPtr    )
		PR_RDR_DEFINE_INSTANCE(StockInstance, PR_RDR_INST);
		#undef PR_RDR_INST

		// An instance type for object bounding boxes
		#define PR_RDR_INST(x)\
			x(pr::m4x4            ,m_i2w    ,pr::rdr::EInstComp::I2WTransform   )\
			x(pr::rdr::ModelPtr   ,m_model  ,pr::rdr::EInstComp::ModelPtr       )
		PR_RDR_DEFINE_INSTANCE(BBoxInstance, PR_RDR_INST);
		#undef PR_RDR_INST

		// An instance for passing to the renderer
		// A renderer instance type for the body
		// Note: don't use 'm_i2w' to control the object transform, use m_o2p in the LdrObject instead
		#define PR_RDR_INST(x)\
			x(pr::m4x4            ,m_i2w    ,pr::rdr::EInstComp::I2WTransform   )\
			x(pr::rdr::ModelPtr   ,m_model  ,pr::rdr::EInstComp::ModelPtr       )\
			x(pr::Colour32        ,m_colour ,pr::rdr::EInstComp::TintColour32   )\
			x(pr::rdr::SKOverride ,m_sko    ,pr::rdr::EInstComp::SortkeyOverride)\
			x(pr::rdr::BSBlock    ,m_bsb    ,pr::rdr::EInstComp::BSBlock        )\
			x(pr::rdr::DSBlock    ,m_dsb    ,pr::rdr::EInstComp::DSBlock        )\
			x(pr::rdr::RSBlock    ,m_rsb    ,pr::rdr::EInstComp::RSBlock        )
		PR_RDR_DEFINE_INSTANCE(RdrInstance, PR_RDR_INST);
		#undef PR_RDR_INST

		// Attributes (with defaults) for an LdrObject
		struct ObjectAttributes
		{
			ELdrObject   m_type;     // Object type
			string32     m_name;     // Name of the object
			pr::Colour32 m_colour;   // Base colour of the object
			bool         m_instance; // True if an instance should be created

			ObjectAttributes() :m_type(ELdrObject::Unknown) ,m_name("unnamed") ,m_colour(pr::Colour32White) ,m_instance(true) {}
			ObjectAttributes(ELdrObject type) :m_type(type), m_name("unnamed") ,m_colour(pr::Colour32White) ,m_instance(true) {}
			ObjectAttributes(ELdrObject type, char const* name) :m_type(type), m_name(name) ,m_colour(pr::Colour32White) ,m_instance(true) {}
			ObjectAttributes(ELdrObject type, char const* name, pr::Colour32 colour) :m_type(type), m_name(name) ,m_colour(colour) ,m_instance(true) {}
			ObjectAttributes(ELdrObject type, char const* name, pr::Colour32 colour, bool instance) :m_type(type), m_name(name) ,m_colour(colour) ,m_instance(instance) {}
		};

		// Mesh creation data
		using MeshCreationData = pr::rdr::MeshCreationData;

		// Info on how to animate a ldr object
		struct Animation
		{
			EAnimStyle m_style;
			float      m_period;       // Seconds
			pr::v4     m_velocity;     // Linear velocity of the animation in m/s
			pr::v4     m_ang_velocity; // Angular velocity of the animation in rad/s

			Animation()
				:m_style(EAnimStyle::NoAnimation)
				,m_period(0)
				,m_velocity(pr::v4Zero)
				,m_ang_velocity(pr::v4Zero)
			{}

			// Return a transform representing the offset
			// added by this object at time 'time_s'
			pr::m4x4 Step(float time_s) const
			{
				if (m_style == EAnimStyle::NoAnimation) return pr::m4x4Identity;

				float t = 0.0f;
				switch (m_style)
				{
				default:break;
				case EAnimStyle::PlayOnce:          t = time_s < m_period ? time_s : m_period; break;
				case EAnimStyle::PlayReverse:       t = time_s < m_period ? m_period - time_s : 0.0f; break;
				case EAnimStyle::PingPong:          t = pr::Fmod(time_s, 2.0f*m_period) >= m_period ? m_period - pr::Fmod(time_s, m_period) : pr::Fmod(time_s, m_period); break;
				case EAnimStyle::PlayContinuous:    t = time_s; break;
				}

				return m4x4::Transform(m_ang_velocity*t, m_velocity*t + pr::v4Origin);
			}
		};

		// Step data for an ldr object
		struct LdrObjectStepData
		{
			typedef pr::chain::Link<LdrObjectStepData> Link;
			std::string m_code;
			Link m_link;
			LdrObjectStepData() { m_link.init(this); }
			bool empty() const  { return m_code.empty(); }
		};

		#pragma endregion

		// A line drawer object
		struct LdrObject
			:RdrInstance
			,pr::RefCount<LdrObject>
		{
			// Note: try not to use the RdrInstance members for things other than rendering
			// they can temporarily have different models/transforms/etc during rendering of
			// object bounding boxes etc.
			pr::m4x4          m_o2p;           // Object to parent transform (or object to world if this is a top level object)
			ELdrObject        m_type;          // Object type
			LdrObject*        m_parent;        // The parent of this object, 0 for top level instances.
			ObjectCont        m_child;         // A container of pointers to child instances
			string32          m_name;          // A name for the object
			GUID              m_context_id;    // The id of the context this instance was created in
			pr::Colour32      m_base_colour;   // The original colour of this object
			pr::uint          m_colour_mask;   // A bit mask for applying the base colour to child objects
			Animation         m_anim;          // Animation data
			LdrObjectStepData m_step;          // Step data for the object
			BBoxInstance      m_bbox_instance; // Used for rendering the bounding box for this instance
			bool              m_instanced;     // False if this instance should never be drawn (it's used for instancing only)
			bool              m_visible;       // True if the instance should be rendered
			bool              m_wireframe;     // True if this object is drawn in wireframe
			pr::UserData      m_user_data;     // User data

			LdrObject(ObjectAttributes const& attr, LdrObject* parent, pr::Guid const& context_id);
			~LdrObject();

			// Return the type and name of this object
			string32 TypeAndName() const;

			// Recursively add this object and its children to a scene
			// Note, LdrObject does not inherit Evt_UpdateScene, because child LdrObjects need to be
			// added using the parents transform. Any app containing LdrObjects should handle the scene
			// render event and then call 'AddToScene' on all of the root objects only
			void AddToScene(pr::rdr::Scene& scene, float time_s = 0.0f, pr::m4x4 const* p2w = &pr::m4x4Identity);

			// Recursively add the bounding box instance for this object using 'bbox_model'
			// located and scaled to the transform and box of this object
			void AddBBoxToScene(pr::rdr::Scene& scene, pr::rdr::ModelPtr bbox_model, float time_s = 0.0f, pr::m4x4 const* p2w = &pr::m4x4Identity);

			// Apply an operation on this object or any of its child objects that match 'name'
			// If 'name' is null, then 'func' is applied to this object only
			// If 'name' is "", then 'func' is applied to this object and all children recursively
			// Otherwise, 'func' is applied to all child objects that match name.
			// If 'name' begins with '#' then the remainder of the name is treated as a regular expression
			// 'func' should have a signature: 'bool func(pr::ldr::LdrObject* obj);'
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
						Apply(func, name, child.m_ptr);
				}
				else
				{
					if ((name[0] == '#' && std::regex_match(std::string(obj->m_name.c_str()), std::regex(&name[1]))) || obj->m_name == name)
					{
						if (!func(obj))
							return false;
					}
					for (auto& child : obj->m_child)
						Apply(func, name, child.m_ptr);
				}
				return true;
			}

			// Get the first child object of this object that matches 'name' (see Apply)
			LdrObjectPtr Child(char const* name) const;

			// Get/Set the object to world transform of this object or the first child object matching 'name' (see Apply)
			// Note, it is more efficient to set O2P.
			pr::m4x4 O2W(char const* name = nullptr) const;
			void O2W(pr::m4x4 const& o2w, char const* name = nullptr);

			// Get/Set the object to parent transform of this object or child objects matching 'name' (see Apply)
			pr::m4x4 O2P(char const* name = nullptr) const;
			void O2P(pr::m4x4 const& o2p, char const* name = nullptr);

			// Get/Set the visibility of this object or child objects matching 'name' (see Apply)
			bool Visible(char const* name = nullptr);
			void Visible(bool visible, char const* name = nullptr);

			// Get/Set the render mode for this object or child objects matching 'name' (see Apply)
			bool Wireframe(char const* name = nullptr);
			void Wireframe(bool wireframe, char const* name = nullptr);

			// Get/Set the colour of this object or child objects matching 'name' (see Apply)
			// For 'Get', the colour of the first object to match 'name' is returned
			// For 'Set', the object base colour is not changed, only the tint colour = tint
			pr::Colour32 Colour(bool base_colour, char const* name = nullptr) const;
			void Colour(pr::Colour32 colour, pr::uint mask, char const* name = nullptr);

			// Restore the colour to the initial colour for this object or child objects matching 'name' (see Apply)
			void ResetColour(char const* name = nullptr);

			// Set the texture on this object or child objects matching 'name' (see Apply)
			// Note for difference mode drawlist management, if the object is currently in
			// one or more drawlists (i.e. added to a scene) it will need to be removed and
			// re-added so that the sort order is correct.
			void SetTexture(pr::rdr::Texture2DPtr tex, char const* name = nullptr);

			// Return the bounding box for this object in model space
			// To convert this to parent space multiply by 'm_o2p'
			// e.g. BBoxMS() for "*Box { 1 2 3 *o2w{*rand} }" will return bb.m_centre = origin, bb.m_radius = (1,2,3)
			template <typename Pred> pr::BBox BBoxMS(bool include_children, Pred pred, float time_s = 0.0f, pr::m4x4 const* p2w = &pr::m4x4Identity) const
			{
				auto i2w = *p2w * m_anim.Step(time_s);

				// Start with the bbox for this object
				pr::BBox bbox = pr::BBoxReset;
				if (m_model && pred(*this)) // Get the bbox from the graphics model
				{
					auto bb = i2w * m_model->m_bbox;
					if (!bb.empty()) pr::Encompass(bbox, bb);
				}
				if (include_children) // Add the bounding boxes of the children
				{
					for (auto& child : m_child)
					{
						auto c2w = i2w * child->m_o2p;
						auto cbbox = child->BBoxMS(include_children, pred, time_s, &c2w);
						if (!cbbox.empty()) pr::Encompass(bbox, cbbox);
					}
				}
				return bbox;
			}
			pr::BBox BBoxMS(bool include_children) const
			{
				return BBoxMS(include_children, [](LdrObject const&){ return true; });
			}

			// Return the bounding box for this object in world space.
			// If this is a top level object, this will be equivalent to 'm_o2p * BBoxMS()'
			// If not then, then the returned bbox will be transformed to the top level object space
			template <typename Pred> pr::BBox BBoxWS(bool include_children, Pred pred, float time_s = 0.0f) const
			{
				// Get the combined o2w transform;
				pr::m4x4 o2w = m_o2p;
				for (pr::ldr::LdrObject* parent = m_parent; parent; parent = parent->m_parent)
					o2w = parent->m_o2p * parent->m_anim.Step(time_s) * o2w;

				return BBoxMS(include_children, pred, time_s, &o2w);
			}
			pr::BBox BBoxWS(bool include_children) const
			{
				return BBoxWS(include_children, [](LdrObject const&){ return true; });
			}

			// Add/Remove 'child' as a child of this object
			void AddChild(LdrObjectPtr child);
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

		#pragma region Events

		// An ldr object has been modified
		struct Evt_LdrObjectChg
		{
			LdrObjectPtr m_obj; // The object that was changed.
			Evt_LdrObjectChg(LdrObjectPtr obj) :m_obj(obj) {}
		};

		#if 0
		// A number of objects are about to be added
		struct Evt_AddBegin
		{};

		// The last object in a group has been added
		struct Evt_AddEnd
		{
			int m_first, m_last;    // Index of the first and last object added
			Evt_AddEnd(int first, int last) :m_first(first) ,m_last(last) {}
		};

		// All objects removed from the object manager
		struct Evt_DeleteAll
		{};

		// An ldr object has been added
		struct Evt_LdrObjectAdd
		{
			LdrObjectPtr m_obj;     // The object that was added.
			Evt_LdrObjectAdd(LdrObjectPtr obj) :m_obj(obj) {}
		};

		// An ldr object is about to be deleted
		struct Evt_LdrObjectDelete
		{
			LdrObject* m_obj;       // The object to be deleted. Note, not a ref ptr because this event is only sent when the ref count = 0
			Evt_LdrObjectDelete(LdrObject* obj) :m_obj(obj) {}
		};

		// An object with step code has been created
		struct Evt_LdrObjectStepCode
		{
			LdrObjectPtr m_obj;     // The object containing step code
			Evt_LdrObjectStepCode(LdrObjectPtr obj) :m_obj(obj) {}
		};

		// A camera description has been read
		struct Evt_LdrSetCamera
		{
			// Bit mask of set fields
			enum EField
			{
				C2W     = 1 << 0,
				Focus   = 1 << 1,
				Align   = 1 << 2,
				Aspect  = 1 << 3,
				FovY    = 1 << 4,
				Near    = 1 << 5,
				Far     = 1 << 6,
				AbsClip = 1 << 7,
				Ortho   = 1 << 8,
			};
			pr::Camera m_cam;
			size_t m_set_fields;
			Evt_LdrSetCamera() :m_cam() ,m_set_fields() {}
		};

		// App commands read from the script
		struct Evt_LdrAppCommands
		{
			bool m_clear;
			bool m_wireframe;
		};
		#endif

		#pragma endregion

		// LdrObject Creation functions *********************************************

		// The results of parsing ldr script
		struct ParseResult
		{
			typedef std::unordered_map<size_t, pr::rdr::ModelPtr> ModelLookup;

			// Bit mask of set camera fields
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
				AbsClip = 1 << 7,
				Ortho   = 1 << 8,
			};

			ObjectCont  m_def_objects; // Objects container that is used if none is provided
			ModelLookup m_models;      // A lookup map for models based on hashed object name
			ObjectCont& m_objects;     // Reference to the objects container to fill
			pr::Camera  m_cam;         // Camera description has been read
			ECamField   m_cam_fields;  // Bitmask of fields in 'm_cam' that were given in the camera description
			bool        m_clear;       // True if '*Clear' was read in the script
			bool        m_wireframe;   // True if '*Wireframe' was read in the script
			
			ParseResult()
				:ParseResult(m_def_objects)
			{}
			ParseResult(ObjectCont& cont)
				:m_def_objects()
				,m_objects(cont)
				,m_cam()
				,m_cam_fields()
				,m_clear()
				,m_wireframe()
			{}

		private:
			ParseResult(ParseResult const&);
			ParseResult& operator=(ParseResult const&);
		};
		inline ParseResult::ECamField& operator |= (ParseResult::ECamField& lhs, ParseResult::ECamField rhs)
		{
			return lhs = static_cast<ParseResult::ECamField>(int(lhs) | int(rhs));
		}
		inline int operator & (ParseResult::ECamField lhs, ParseResult::ECamField rhs)
		{
			return int(lhs) & int(rhs);
		}

		// Parse the ldr script in 'reader' adding the results to 'out'
		// If 'async' is true, a progress dialog is displayed and parsing is done in a background thread.
		void Parse(
			pr::Renderer& rdr,                // The renderer to create models for
			pr::script::Reader& reader,       // The source of the script
			ParseResult& out,                 // The results of parsing the script
			bool async = true,                // True if parsing should be done in a background thread
			pr::Guid const& context_id = GuidZero // The context id to assign to each created object
			);

		// Parse ldr script from a text file
		// 'include_paths' is a comma/semicolon separated list of include paths to use to resolve #include directives (or nullptr)
		inline void ParseFile(
			pr::Renderer& rdr,                     // The renderer to create models for
			char const* filename,                  // The file containing the ldr script
			ParseResult& out,                      // The results of parsing the script
			bool async = true,                     // True if parsing should be done in a background thread
			pr::Guid const& context_id = GuidZero) // The context id to assign to each created object
		{
			pr::script::FileSrc<> src(filename);
			pr::script::Reader reader(src);
			Parse(rdr, reader, out, async, context_id);
		}

		// Parse ldr script from a string
		// 'include_paths' is a comma/semicolon separated list of include "paths" to use to resolve #include directives (or nullptr)
		template <typename Char>
		inline void ParseString(
			pr::Renderer& rdr,                     // The reader to create models for
			Char const* ldr_script,                // The literal string containing the script
			ParseResult& out,                      // The results of parsing the script
			bool async = true,                     // True if parsing should be done in a background thread
			pr::Guid const& context_id = GuidZero) // The context id to assign to each created object
		{
			pr::script::Ptr<Char const*> src(ldr_script);
			pr::script::Reader reader(src);
			Parse(rdr, reader, out, async, context_id);
		}

		// Callback function for editing a dynamic model
		// This callback is intentionally low level, providing the whole model for editing.
		// Remember to update the bounding box, vertex and index ranges, and regenerate nuggets.
		typedef void (__stdcall *EditObjectCB)(pr::rdr::ModelPtr model, void* ctx, pr::Renderer& rdr);

		// Create an ldr object from creation data.
		LdrObjectPtr Create(
			pr::Renderer& rdr,                      // The reader to create models for
			ObjectAttributes attr,                  // Object attributes to use with the created object
			MeshCreationData const& cdata,          // Model creation data
			pr::Guid const& context_id = GuidZero); // The context id to assign to the object

		// Create an ldr object using a callback to populate the model data.
		// Objects created by this method will have dynamic usage and are suitable for updating every frame via the 'Edit' function.
		LdrObjectPtr CreateEditCB(
			pr::Renderer& rdr,                      // The reader to create models for
			ObjectAttributes attr,                  // Object attributes to use with the created object
			int vcount,                             // The number of verts to create the model with
			int icount,                             // The number of indices to create the model with
			int ncount,                             // The number of nuggets to create the model with
			EditObjectCB edit_cb,                   // The callback function, called after the model is created, to populate the model data
			void* ctx,                              // Callback user context
			pr::Guid const& context_id = GuidZero); // The context id to assign to the object

		// Modify the geometry of an LdrObject
		void Edit(pr::Renderer& rdr, LdrObjectPtr object, EditObjectCB edit_cb, void* ctx);

		// Update 'object' with info from 'desc'. 'keep' describes the properties of 'object' to update
		void Update(pr::Renderer& rdr, LdrObjectPtr object, pr::script::Reader& reader, EUpdateObject flags = EUpdateObject::All);

		// Remove all objects from 'objects' that have a context id matching one in 'doomed' and not in 'excluded'
		// If 'doomed' is 0, all are assumed doomed. If 'excluded' is 0, none are assumed excluded
		// 'excluded' is considered after 'doomed' so if any context ids are in both arrays, they will be excluded.
		void Remove(ObjectCont& objects, pr::Guid const* doomed, std::size_t dcount, pr::Guid const* excluded, std::size_t ecount);

		// Remove 'obj' from 'objects'
		void Remove(ObjectCont& objects, pr::ldr::LdrObjectPtr obj);

		// Parse an ldr transform description accumulatively
		// 'o2w' should be a valid initial transform
		// Parse the source data in 'reader' using the same syntax
		// as we use for ldr object '*o2w' transform descriptions.
		// This function is inline so that external code can use the Ldr
		// transform syntax without dependence on renderer functions
		inline pr::m4x4& ParseLdrTransform(pr::script::Reader& reader, pr::m4x4& o2w)
		{
			assert(pr::IsFinite(o2w) && "A valid 'o2w' must be passed to this function as it pre-multiplies the transform with the one read from the script");
			pr::m4x4 p2w = pr::m4x4Identity;

			reader.SectionStart();
			for (EKeyword kw; reader.NextKeywordH(kw);)
			{
				switch (kw)
				{
				default:
					{
						reader.ReportError(pr::script::EResult::UnknownToken);
						break;
					}
				case EKeyword::M4x4:
					{
						m4x4 o2w_ = m4x4Identity;
						reader.Matrix4x4S(o2w_);
						if (o2w_.w.w != 1)
						{
							reader.ReportError(pr::script::EResult::UnknownValue, "M4x4 must be an affine transform with: w.w == 1");
							break;
						}
						p2w = o2w_ * p2w;
						break;
					}
				case EKeyword::M3x3:
					{
						m4x4 m = m4x4Identity;
						reader.Matrix3x3S(m.rot);
						p2w = m * p2w;
						break;
					}
				case EKeyword::Pos:
					{
						m4x4 m = m4x4Identity;
						reader.Vector3S(m.pos, 1.0f);
						p2w = m * p2w;
						break;
					}
				case EKeyword::Align:
					{
						int axis_id;
						pr::v4 direction;
						reader.SectionStart();
						reader.Int(axis_id, 10);
						reader.Vector3(direction, 0.0f);
						reader.SectionEnd();

						v4 axis = AxisId(axis_id);
						if (IsZero3(axis))
						{
							reader.ReportError(pr::script::EResult::UnknownValue, "axis_id must one of ±1, ±2, ±3");
							break;
						}

						p2w = m4x4::Transform(axis, direction, v4Origin) * p2w;
						break;
					}
				case EKeyword::Quat:
					{
						pr::quat q;
						reader.Vector4S(q.xyzw);
						p2w = m4x4::Transform(q, v4Origin) * p2w;
						break;
					}
				case EKeyword::Rand4x4:
					{
						float radius;
						pr::v4 centre;
						reader.SectionStart();
						reader.Vector3(centre, 1.0f);
						reader.Real(radius);
						reader.SectionEnd();
						p2w = pr::Random4x4(pr::g_Rand(), centre, radius) * p2w;
						break;
					}
				case EKeyword::RandPos:
					{
						float radius;
						pr::v4 centre;
						reader.SectionStart();
						reader.Vector3(centre, 1.0f);
						reader.Real(radius);
						reader.SectionEnd();
						p2w = m4x4::Translation(Random3(g_Rand(), centre, radius, 1.0f)) * p2w;
						break;
					}
				case EKeyword::RandOri:
					{
						m4x4 m = m4x4Identity;
						m.rot = pr::Random3x4(pr::g_Rand());
						p2w = m * p2w;
						break;
					}
				case EKeyword::Euler:
					{
						pr::v4 angles;
						reader.Vector3S(angles, 0.0f);
						p2w = m4x4::Transform(pr::DegreesToRadians(angles.x), pr::DegreesToRadians(angles.y), pr::DegreesToRadians(angles.z), pr::v4Origin) * p2w;
						break;
					}
				case EKeyword::Scale:
					{
						pr::v4 scale;
						reader.SectionStart();
						reader.Real(scale.x);
						if (reader.IsSectionEnd())
							scale.z = scale.y = scale.x;
						else
						{
							reader.Real(scale.y);
							reader.Real(scale.z);
						}
						reader.SectionEnd();
						p2w = m4x4::Scale(scale.x, scale.y, scale.z, v4Origin) * p2w;
						break;
					}
				case EKeyword::Transpose:
					{
						p2w = pr::Transpose4x4(p2w);
						break;
					}
				case EKeyword::Inverse:
					{
						p2w = pr::IsOrthonormal(p2w) ? pr::InvertFast(p2w) : pr::Invert(p2w);
						break;
					}
				case EKeyword::Normalise:
					{
						p2w.x = pr::Normalise3(p2w.x);
						p2w.y = pr::Normalise3(p2w.y);
						p2w.z = pr::Normalise3(p2w.z);
						break;
					}
				case EKeyword::Orthonormalise:
					{
						p2w = pr::Orthonorm(p2w);
						break;
					}
				}
			}
			reader.SectionEnd();

			// Pre-multiply the object to world transform
			o2w = p2w * o2w;
			PR_INFO_IF(PR_DBG, o2w.w.w != 1.0f, "o2w.w.w != 1.0f - non orthonormal transform");
			return o2w;
		}

		// Parse the source data in 'reader' using the same syntax
		// as we use for ldr object '*o2w' transform descriptions.
		// The source should begin with '{' and end with '}', i.e. *o2w { ... } with the *o2w already read
		inline pr::m4x4 ParseLdrTransform(pr::script::Reader& reader)
		{
			pr::m4x4 o2w = pr::m4x4Identity;
			ParseLdrTransform(reader, o2w);
			return o2w;
		}

		// Generate a scene that demos the supported object types and modifiers.
		std::wstring CreateDemoScene();
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr
{
	namespace unittests
	{
		PRUnitTest(pr_linedrawer_ldr_object)
		{
			// Check the hash values are correct
			auto hasher  = [](wchar_t const* s) { return pr::script::Reader::StaticHashKeyword(s, false); };
			auto on_fail = [](char const* m) { PR_FAIL(m); };
			pr::CheckHashEnum<pr::ldr::EKeyword  , wchar_t>(hasher, on_fail);
			pr::CheckHashEnum<pr::ldr::ELdrObject, wchar_t>(hasher, on_fail);
		}
	}
}
#endif
