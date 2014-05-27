//***************************************************************************************************
// Ldr Object
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#pragma once

#ifndef PR_LDR_OBJECT_H
#define PR_LDR_OBJECT_H

#ifndef PR_DBG_LDROBJMGR
#define PR_DBG_LDROBJMGR PR_DBG
#endif

#include <string>
#include <regex>
#include "pr/macros/enum.h"
#include "pr/common/min_max_fix.h"
#include "pr/common/array.h"
#include "pr/common/hash.h"
#include "pr/common/new.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"
#include "pr/maths/maths.h"
#include "pr/str/prstdstring.h"
#include "pr/script/reader.h"
#include "pr/renderer11/instance.h"
#include "pr/linedrawer/ldr_forward.h"

// Forward declare the tree control item.
struct _TREEITEM;
typedef struct _TREEITEM *HTREEITEM;

namespace pr
{
	namespace ldr
	{
		// Forwards
		struct ObjectAttributes;
		typedef pr::RefPtr<LdrObject> LdrObjectPtr;
		typedef pr::Array<LdrObjectPtr, 8> ObjectCont;
		typedef pr::string<char, 32> string32;

		static HTREEITEM const INVALID_TREE_ITEM =  0;
		static int const       INVALID_LIST_ITEM = -1;

		#pragma region Ldr object types
		#define PR_ENUM(x)\
			x(Unknown          ,= 0x062170b2)\
			x(Line             ,= 0x10d28008)\
			x(LineD            ,= 0x07512a2a)\
			x(LineStrip        ,= 0x0702D776)\
			x(LineBox          ,= 0x11d1a8c4)\
			x(Grid             ,= 0x08D503AF)\
			x(Spline           ,= 0x1f45c36e)\
			x(Arrow            ,= 0x0A85DC58)\
			x(Circle           ,= 0x015ed657)\
			x(Rect             ,= 0x12E8C1AD)\
			x(Matrix3x3        ,= 0x1bd46252)\
			x(Triangle         ,= 0x118ad8f6)\
			x(Quad             ,= 0x083b7f24)\
			x(Plane            ,= 0x0ed56051)\
			x(Ribbon           ,= 0x00000100)\
			x(Box              ,= 0x11b87d89)\
			x(BoxLine          ,= 0x11d4daa7)\
			x(BoxList          ,= 0x1481a6af)\
			x(FrustumWH        ,= 0x02f1f573)\
			x(FrustumFA        ,= 0x14adfea2)\
			x(Sphere           ,= 0x010cf039)\
			x(CylinderHR       ,= 0x0f3d4e07)\
			x(ConeHA           ,= 0x1f093110)\
			x(Mesh             ,= 0x07fb0d2b)\
			x(ConvexHull       ,= 0x0a93b3d5)\
			x(Group            ,= 0x1c1c3d90)\
			x(Instance         ,= 0x061978f8)\
			x(DirectionalLight ,= 0x1744E3AC)\
			x(PointLight       ,= 0x01058AFF)\
			x(SpotLight        ,= 0x0C406795)\
			x(Custom           ,= 0x1a1023c5)
		PR_DEFINE_ENUM2(ELdrObject, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Ldr script keywords
		#define PR_ENUM(x)\
			x(O2W                ,= 0x1d20a7ba)\
			x(M4x4               ,= 0x1788ecfa)\
			x(M3x3               ,= 0x0aef28f5)\
			x(Pos                ,= 0x13930daf)\
			x(Up                 ,= 0x1e72eb5a)\
			x(Direction          ,= 0x0fca4076)\
			x(Quat               ,= 0x1de5b1fd)\
			x(Rand4x4            ,= 0x02becb08)\
			x(RandPos            ,= 0x0456bf23)\
			x(RandOri            ,= 0x185629c3)\
			x(Euler              ,= 0x150894fc)\
			x(Scale              ,= 0x1837069d)\
			x(Transpose          ,= 0x12fafef6)\
			x(Inverse            ,= 0x01ca4f54)\
			x(Normalise          ,= 0x01693558)\
			x(Orthonormalise     ,= 0x1c2a9e41)\
			x(Colour             ,= 0x08b2c176)\
			x(Solid              ,= 0x0B6477F6)\
			x(Facets             ,= 0x1C8FE1ED)\
			x(CornerRadius       ,= 0x1C7A230A)\
			x(RandColour         ,= 0x0cdef959)\
			x(ColourMask         ,= 0x05dc4ca1)\
			x(Animation          ,= 0x004c5336)\
			x(Style              ,= 0x0c0c447d)\
			x(Period             ,= 0x166be380)\
			x(Velocity           ,= 0x13c33323)\
			x(AngVelocity        ,= 0x1572a9b4)\
			x(Axis               ,= 0x05a50e20)\
			x(Hidden             ,= 0x16ade487)\
			x(Wireframe          ,= 0x067b0d73)\
			x(Delimiters         ,= 0x084f5b30)\
			x(Clear              ,= 0x045518bd)\
			x(Camera             ,= 0x19028d2f)\
			x(LookAt             ,= 0x1B6B2D38)\
			x(Align              ,= 0x0E12A77C)\
			x(Aspect             ,= 0x1003E8FA)\
			x(FovX               ,= 0x19374FBB)\
			x(FovY               ,= 0x136802CE)\
			x(Fov                ,= 0x1AF2491D)\
			x(Near               ,= 0x1E14CA9C)\
			x(Far                ,= 0x05214BAB)\
			x(AbsoluteClipPlanes ,= 0x026E4C7B)\
			x(Orthographic       ,= 0x1EA1DC13)\
			x(Lock               ,= 0x0a040f55)\
			x(Coloured           ,= 0x078194a5)\
			x(Width              ,= 0x190A34C3)\
			x(Smooth             ,= 0x0EF3B8A6)\
			x(Param              ,= 0x090da184)\
			x(Texture            ,= 0x126f7a1b)\
			x(Video              ,= 0x1121909f)\
			x(Divisions          ,= 0x1a7f5e28)\
			x(Layers             ,= 0x0ea88b62)\
			x(Wedges             ,= 0x0e4a1c99)\
			x(ViewPlaneZ         ,= 0x053FC43C)\
			x(Verts              ,= 0x1b15f488)\
			x(Normals            ,= 0x061bcc50)\
			x(Colours            ,= 0x1e9214e3)\
			x(TexCoords          ,= 0x096ec538)\
			x(Lines              ,= 0x18abd83b)\
			x(Faces              ,= 0x14903115)\
			x(Tetra              ,= 0x092750d0)\
			x(GenerateNormals    ,= 0x1c991230)\
			x(Step               ,= 0x0ad1d27d)\
			x(Addr               ,= 0x0a215e87)\
			x(Filter             ,= 0x183f6f0c)\
			x(Range              ,= 0x0529B2DD)\
			x(Specular           ,= 0x15DF6A5F)\
			x(CastShadow         ,= 0x10C1337D)
		PR_DEFINE_ENUM2(EKeyword, PR_ENUM);
		#undef PR_ENUM
		#pragma endregion

		#pragma region Enums

		// Modes for bounding groups of objects
		#define PR_ENUM(x)\
			x(All)\
			x(Selected)\
			x(Visible)
		PR_DEFINE_ENUM1(EObjectBounds, PR_ENUM);
		#undef PR_ENUM

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

				return pr::Rotation4x4(m_ang_velocity*t, m_velocity*t + pr::v4Origin);
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

		// Interface for user data
		struct ILdrUserData { virtual ~ILdrUserData() {} };

		// A map from custom id to user data
		typedef std::unordered_map<size_t, std::unique_ptr<ILdrUserData>> UserData;

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
			ContextId         m_context_id;    // The id of the context this instance was created in
			pr::Colour32      m_base_colour;   // The original colour of this object
			pr::uint          m_colour_mask;   // A bit mask for applying the base colour to child objects
			Animation         m_anim;          // Animation data
			LdrObjectStepData m_step;          // Step data for the object
			BBoxInstance      m_bbox_instance; // Used for rendering the bounding box for this instance
			bool              m_instanced;     // False if this instance should never be drawn (it's used for instancing only)
			bool              m_visible;       // True if the instance should be rendered
			bool              m_wireframe;     // True if this object is drawn in wireframe
			UserData          m_user_data;     // User assigned data

			// Predicate for matching this object by context id
			struct MatchId
			{
				ContextId m_id;
				MatchId(ContextId id) :m_id(id) {}
				bool operator()(LdrObject const& obj) const { return obj.m_context_id == m_id; }
				bool operator()(LdrObject const* obj) const { return obj && obj->m_context_id == m_id; }
			};

			LdrObject(ObjectAttributes const& attr, pr::ldr::LdrObject* parent, pr::ldr::ContextId context_id);
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
			template <typename TFunc> void Apply(TFunc func, char const* name = nullptr, LdrObject* obj = nullptr)
			{
				if (obj == nullptr)
				{
					obj = this;
				}
				if (name == nullptr)
				{
					func(obj);
				}
				else if (name[0] == '\0')
				{
					func(obj);
					for (auto& child : obj->m_child)
						Apply(func, name, child.m_ptr);
				}
				else
				{
					if ((name[0] == '#' && std::regex_match(std::string(obj->m_name.c_str()), std::regex(&name[1]))) || obj->m_name == name)
						func(obj);
					for (auto& child : obj->m_child)
						Apply(func, name, child.m_ptr);
				}
			}

			// Set the visibility of this object or child objects matching 'name' (see Apply)
			void Visible(bool visible, char const* name = nullptr);

			// Set the render mode for this object or child objects matching 'name' (see Apply)
			void Wireframe(bool wireframe, char const* name = nullptr);

			// Set the colour of this object or child objects matching 'name' (see Apply)
			void SetColour(pr::Colour32 colour, pr::uint mask, char const* name = nullptr);

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
					if (bb.IsValid()) pr::Encompass(bbox, bb);
				}
				if (include_children) // Add the bounding boxes of the children
				{
					for (auto& child : m_child)
					{
						auto c2w = i2w * child->m_o2p;
						auto cbbox = child->BBoxMS(include_children, pred, time_s, &c2w);
						if (cbbox.IsValid()) pr::Encompass(bbox, cbbox);
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

			// True if this object contains user data matching 'id'
			bool HasUserData(size_t id) const { return m_user_data.count(id) != 0; }

			// Convenience accessor to user data
			template <typename T> T const& UserData(size_t id) const { return *m_user_data[id].get(); }
			template <typename T> T&       UserData(size_t id)       { return *m_user_data[id].get(); }

			// Called when there are no more references to this object
			static void RefCountZero(RefCount<LdrObject>* doomed);
			long AddRef() const;
			long Release() const;
		};

		#pragma region Events

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

		// An ldr object has been modified
		struct Evt_LdrObjectChg
		{
			LdrObjectPtr m_obj;     // The object that was changed.
			Evt_LdrObjectChg(LdrObjectPtr obj) :m_obj(obj) {}
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

		// Called when one or more objects have changed state
		struct Evt_Refresh
		{
			LdrObjectPtr m_obj;     // The object that has changed. If null, then more than one object has changed
			Evt_Refresh() :m_obj(0) {}
			Evt_Refresh(LdrObjectPtr obj) :m_obj(obj) {}
		};

		//// Callback progress event used during parsing
		//struct Evt_LdrProgress
		//{
		//	int m_count;            // -1 for unknown
		//	int m_total;            // -1 for unknown
		//	char const* m_desc;     // A brief description of the operation that progress is for
		//	bool m_allow_cancel;    // True if it's ok to cancel this operation
		//	LdrObjectPtr m_obj;     // An object near the current progress point
		//	Evt_LdrProgress(int count, int total, char const* desc, bool allow_cancel, LdrObjectPtr obj) :m_count(count) ,m_total(total) ,m_desc(desc) ,m_allow_cancel(allow_cancel) ,m_obj(obj) {}
		//};

		// Event fired from the UI when the selected object changes
		struct Evt_LdrObjectSelectionChanged
		{};

		// Sent by the object manager ui whenever its settings have changed
		struct Evt_SettingsChanged
		{};

		#pragma endregion

		// LdrObject Creation functions *********************************************

		// Callback function for editing a dynamic model
		// This callback is intentionally low level, providing the whole model for editing.
		// Remember to update the bounding box, vertex and index ranges, and regenerate nuggets.
		typedef void (__stdcall *EditObjectCB)(pr::rdr::ModelPtr model, void* ctx, pr::Renderer& rdr);

		// Add the ldr objects described in 'reader' to 'objects'.
		// If 'async' is true, a progress dialog is displayed and adding is done in a background thread.
		// Raises 'Evt_LdrObjectAdd' events for each root level object added.
		void Add(pr::Renderer& rdr, pr::script::Reader& reader, pr::ldr::ObjectCont& objects, pr::ldr::ContextId context_id = DefaultContext, bool async = true);

		// Add models/instances from a text file containing linedrawer script
		inline void AddFile(pr::Renderer& rdr, char const* filename, pr::ldr::ObjectCont& objects, pr::ldr::ContextId context_id = DefaultContext, bool async = true, pr::script::IErrorHandler* script_error_handler = 0, pr::script::IEmbeddedCode* lua_code_handler = 0)
		{
			pr::script::Reader reader;
			pr::script::FileSrc src(filename);
			if (script_error_handler) reader.ErrorHandler() = script_error_handler;
			if (lua_code_handler)     reader.CodeHandler() = lua_code_handler;
			reader.AddSource(src);
			Add(rdr, reader, objects, context_id, async);
		}

		// Add models/instances from a string of linedrawer script
		// Returns the number of top level objects added
		inline void AddString(pr::Renderer& rdr, char const* ldr_script, pr::ldr::ObjectCont& objects, pr::ldr::ContextId context_id = DefaultContext, bool async = true, pr::script::IErrorHandler* script_error_handler = 0, pr::script::IEmbeddedCode* lua_code_handler = 0)
		{
			pr::script::Loc    loc("ldr_string", 0, 0);
			pr::script::PtrSrc src(ldr_script, &loc);
			pr::script::Reader reader;
			if (script_error_handler) reader.ErrorHandler() = script_error_handler;
			if (lua_code_handler)     reader.CodeHandler() = lua_code_handler;
			reader.AddSource(src);
			Add(rdr, reader, objects, context_id, async);
		}

		// Add a custom object
		pr::ldr::LdrObjectPtr Add(
			pr::Renderer& rdr,
			pr::ldr::ObjectAttributes attr,
			pr::rdr::EPrim prim_type,
			int icount,
			int vcount,
			pr::uint16 const* indices,
			pr::v4 const* verts,
			int ccount = 0,                         // 0, 1, or vcount
			pr::Colour32 const* colours = nullptr,  // nullptr, 1, or vcount colours
			int ncount = 0,                         // 0, 1, or vcount
			pr::v4 const* normals = nullptr,        // nullptr or a pointer to vcount normals
			pr::v2 const* tex_coords = nullptr,     // nullptr or a pointer to vcount tex coords
			pr::ldr::ContextId context_id = DefaultContext);

		// Add a custom object via callback
		// Objects created by this method will have dynamic usage and are suitable for updating every frame
		// They are intended to be used with the 'Edit' function.
		pr::ldr::LdrObjectPtr Add(pr::Renderer& rdr, pr::ldr::ObjectAttributes attr, int icount, int vcount, EditObjectCB edit_cb, void* ctx, pr::ldr::ContextId context_id = DefaultContext);

		// Update 'object' with info from 'desc'. 'keep' describes the properties of 'object' to update
		void Update(pr::Renderer& rdr, LdrObjectPtr object, char const* desc, EUpdateObject flags = EUpdateObject::All);

		// Modify the geometry of an LdrObject
		void Edit(pr::Renderer& rdr, LdrObjectPtr object, EditObjectCB edit_cb, void* ctx);

		// Remove all objects from 'objects' that have a context id matching one in 'doomed' and not in 'excluded'
		// If 'doomed' is 0, all are assumed doomed. If 'excluded' is 0, none are assumed excluded
		// 'excluded' is considered after 'doomed' so if any context ids are in both arrays, they will be excluded.
		void Remove(pr::ldr::ObjectCont& objects, pr::ldr::ContextId const* doomed, std::size_t dcount, pr::ldr::ContextId const* excluded, std::size_t ecount);

		// Remove 'obj' from 'objects'
		void Remove(pr::ldr::ObjectCont& objects, pr::ldr::LdrObjectPtr obj);

		// Parse the source data in 'reader' using the same syntax
		// as we use for ldr object '*o2w' transform descriptions.
		// The source should begin with '{' and end with '}', i.e. *o2w { ... } with the *o2w already read
		pr::m4x4 ParseLdrTransform(pr::script::Reader& reader);

		// Generate a scene that demos the supported object types and modifers.
		std::string CreateDemoScene();
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
			auto hasher  = [](char const* s) { return pr::hash::HashLwr(s); };
			auto on_fail = [](char const* m) { PR_FAIL(m); };
			pr::CheckHashEnum<pr::ldr::EKeyword  >(hasher, on_fail);
			pr::CheckHashEnum<pr::ldr::ELdrObject>(hasher, on_fail);
		}
	}
}
#endif

#endif
