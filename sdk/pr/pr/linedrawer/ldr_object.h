//***************************************************************************************************
// Ldr Object
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************

#pragma once
#ifndef PR_LDR_OBJECT_H
#define PR_LDR_OBJECT_H

#ifndef PR_DBG_LDROBJMGR
#define PR_DBG_LDROBJMGR PR_DBG
#endif

#include <string>
#include "pr/macros/enum.h"
#include "pr/common/min_max_fix.h"
#include "pr/common/array.h"
#include "pr/common/hash.h"
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

		static HTREEITEM const INVALID_TREE_ITEM =  0;
		static int const       INVALID_LIST_ITEM = -1;

		// Ldr object types
		#define PR_ENUM(x)\
			x(Unknown          ,= 0x062170b2)\
			x(Line             ,= 0x10d28008)\
			x(LineD            ,= 0x07512a2a)\
			x(LineList         ,= 0x0e9c633b)\
			x(LineBox          ,= 0x11d1a8c4)\
			x(Spline           ,= 0x1f45c36e)\
			x(Circle           ,= 0x015ed657)\
			x(Ellipse          ,= 0x1dc7f472)\
			x(Matrix3x3        ,= 0x1bd46252)\
			x(Triangle         ,= 0x118ad8f6)\
			x(Quad             ,= 0x083b7f24)\
			x(Plane            ,= 0x0ed56051)\
			x(Box              ,= 0x11b87d89)\
			x(BoxLine          ,= 0x11d4daa7)\
			x(BoxList          ,= 0x1481a6af)\
			x(FrustumWH        ,= 0x02f1f573)\
			x(FrustumFA        ,= 0x14adfea2)\
			x(Sphere           ,= 0x010cf039)\
			x(SphereRxyz       ,= 0x146866ff)\
			x(CylinderHR       ,= 0x0f3d4e07)\
			x(ConeHR           ,= 0x14362856)\
			x(ConeHA           ,= 0x1f093110)\
			x(Mesh             ,= 0x07fb0d2b)\
			x(ConvexHull       ,= 0x0a93b3d5)\
			x(Group            ,= 0x1c1c3d90)\
			x(Instance         ,= 0x061978f8)\
			x(Custom           ,= 0x1a1023c5)
		PR_DEFINE_ENUM2(ELdrObject, PR_ENUM);
		#undef PR_ENUM

		// Ldr script keywords
		#define PR_ENUM(x)\
			x(O2W             ,= 0x1d20a7ba)\
			x(M4x4            ,= 0x1788ecfa)\
			x(M3x3            ,= 0x0aef28f5)\
			x(Pos             ,= 0x13930daf)\
			x(Up              ,= 0x1e72eb5a)\
			x(Direction       ,= 0x0fca4076)\
			x(Quat            ,= 0x1de5b1fd)\
			x(Rand4x4         ,= 0x02becb08)\
			x(RandPos         ,= 0x0456bf23)\
			x(RandOri         ,= 0x185629c3)\
			x(Euler           ,= 0x150894fc)\
			x(Scale           ,= 0x1837069d)\
			x(Transpose       ,= 0x12fafef6)\
			x(Inverse         ,= 0x01ca4f54)\
			x(Normalise       ,= 0x01693558)\
			x(Orthonormalise  ,= 0x1c2a9e41)\
			x(Colour          ,= 0x08b2c176)\
			x(RandColour      ,= 0x0cdef959)\
			x(ColourMask      ,= 0x05dc4ca1)\
			x(Animation       ,= 0x004c5336)\
			x(Style           ,= 0x0c0c447d)\
			x(Period          ,= 0x166be380)\
			x(Velocity        ,= 0x13c33323)\
			x(AngVelocity     ,= 0x1572a9b4)\
			x(Axis            ,= 0x05a50e20)\
			x(Hidden          ,= 0x16ade487)\
			x(Wireframe       ,= 0x067b0d73)\
			x(Delimiters      ,= 0x084f5b30)\
			x(Clear           ,= 0x045518bd)\
			x(Camera          ,= 0x19028d2f)\
			x(Lock            ,= 0x0a040f55)\
			x(Coloured        ,= 0x078194a5)\
			x(Param           ,= 0x090da184)\
			x(Texture         ,= 0x126f7a1b)\
			x(Video           ,= 0x1121909f)\
			x(Divisions       ,= 0x1a7f5e28)\
			x(Layers          ,= 0x0ea88b62)\
			x(Wedges          ,= 0x0e4a1c99)\
			x(Verts           ,= 0x1b15f488)\
			x(Normals         ,= 0x061bcc50)\
			x(Colours         ,= 0x1e9214e3)\
			x(TexCoords       ,= 0x096ec538)\
			x(Lines           ,= 0x18abd83b)\
			x(Faces           ,= 0x14903115)\
			x(Tetra           ,= 0x092750d0)\
			x(GenerateNormals ,= 0x1c991230)\
			x(Step            ,= 0x0ad1d27d)\
			x(Addr            ,= 0x0a215e87)\
			x(Filter          ,= 0x183f6f0c)
		PR_DEFINE_ENUM2(EKeyword, PR_ENUM);
		#undef PR_ENUM

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

		// An instance type for line drawer stock objects
		#define PR_RDR_INST(x)\
			x(pr::m4x4            ,m_i2w    ,pr::rdr::EInstComp::I2WTransform   )\
			x(pr::rdr::ModelPtr   ,m_model  ,pr::rdr::EInstComp::ModelPtr       )
		PR_RDR_DEFINE_INSTANCE(StockInstance, PR_RDR_INST);
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
			std::string  m_name;     // Name of the object
			pr::Colour32 m_colour;   // Base colour of the object
			bool         m_instance; // True if an instance should be created

			ObjectAttributes() :m_type(ELdrObject::Unknown) ,m_name("unnamed") ,m_colour(pr::Colour32White) ,m_instance(true) {}
			ObjectAttributes(ELdrObject type) :m_type(type), m_name("unnamed") ,m_colour(pr::Colour32White) ,m_instance(true) {}
			ObjectAttributes(ELdrObject type, char const* name) :m_type(type), m_name(name) ,m_colour(pr::Colour32White) ,m_instance(true) {}
			ObjectAttributes(ELdrObject type, char const* name, pr::Colour32 colour) :m_type(type), m_name(name) ,m_colour(colour) ,m_instance(true) {}
			ObjectAttributes(ELdrObject type, char const* name, pr::Colour32 colour, bool instance) :m_type(type), m_name(name) ,m_colour(colour) ,m_instance(instance) {}
		};

		// UI data for an ldr object
		struct LdrObjectUIData
		{
			HTREEITEM m_tree_item;
			int       m_list_item;
			LdrObjectUIData();
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

		// A line drawer object
		struct LdrObject
			:RdrInstance
			,pr::RefCount<LdrObject>
		{
			// Note: try not to use the RdrInstance members for things other than rendering
			// they can temporarily have different models/transforms/etc during rendering of
			// object bounding boxes etc.
			pr::m4x4            m_o2p;          // Object to parent transform (or object to world if this is a top level object)
			ELdrObject          m_type;         // Object type
			LdrObject*          m_parent;       // The parent of this object, 0 for top level instances.
			ObjectCont          m_child;        // A container of pointers to child instances
			std::string         m_name;         // A name for the object
			ContextId           m_context_id;   // The id of the context this instance was created in
			pr::Colour32        m_base_colour;  // The original colour of this object
			pr::uint            m_colour_mask;  // A bit mask for applying the base colour to child objects
			Animation           m_anim;         // Animation data
			LdrObjectUIData     m_uidata;       // Data required to find this object in the ui
			LdrObjectStepData   m_step;         // Step data for the object
			bool                m_instanced;    // False if this instance should never be drawn (it's used for instancing only)
			bool                m_visible;      // True if the instance should be rendered
			bool                m_wireframe;    // True if this object is drawn in wireframe
			void*               m_user_data;    // Custom data

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
			std::string TypeAndName() const;

			// Recursively add this object and its children to a scene
			// Note, LdrObject does not inherit Evt_SceneRender, because child LdrObjects need to be
			// added using the parents transform. Any app containing LdrObjects should handle the scene
			// render event and then call 'AddToScene' on all of the root objects only
			void AddToScene(pr::rdr::Scene& scene, float time_s = 0.0f, pr::m4x4 const* p2w = &pr::m4x4Identity);

			// ToDo, do this in a way that will actually work
			//// Recursively add this object using 'bbox_model' instead of its
			//// actual model, located and scaled to the transform and box of this object
			//void AddBBoxToViewport(pr::rdr::Viewport& viewport, pr::rdr::ModelPtr bbox_model, float time_s = 0.0f, pr::m4x4 const* p2w = &pr::m4x4Identity);

			// Change visibility for this object
			void Visible(bool visible, bool include_children);

			// Change the render mode for this object
			void Wireframe(bool wireframe, bool include_children);

			// Change the colour of this object
			void SetColour(pr::Colour32 colour, pr::uint mask, bool include_children);

			// Return the bounding box for this object in model space
			// To convert this to parent space multiply by 'm_o2p'
			// e.g. BBoxMS() for "*Box { 1 2 3 *o2w{*rand} }" will return bb.m_centre = origin, bb.m_radius = (1,2,3)
			template <typename Pred> pr::BoundingBox BBoxMS(bool include_children, Pred pred) const
			{
				// Add the bbox for this object
				pr::BoundingBox bbox = pr::BBoxReset;
				if (m_model && pred(*this)) // Get the bbox from the graphics model
				{
					pr::BoundingBox const& bb = m_model->m_bbox;
					if (bb.IsValid()) pr::Encompase(bbox, bb);
				}
				if (include_children) // Add the bounding boxes of the children
				{
					for (ObjectCont::const_iterator i = m_child.begin(), iend = m_child.end(); i != iend; ++i)
					{
						LdrObjectPtr const& child = *i;
						pr::BoundingBox child_bbox = child->BBoxMS(include_children, pred);
						if (child_bbox.IsValid()) pr::Encompase(bbox, child->m_o2p * child_bbox);
					}
				}
				return bbox;
			}
			pr::BoundingBox BBoxMS(bool include_children) const
			{
				struct All { bool operator()(LdrObject const&) const {return true;} };
				return BBoxMS(include_children, All());
			}

			// Return the bounding box for this object in world space.
			// If this is a top level object, this will be equivalent to 'm_o2p * BBoxMS()'
			// If not then, then the returned bbox will be transformed to the top level object space
			template <typename Pred> pr::BoundingBox BBoxWS(bool include_children, Pred pred) const
			{
				pr::BoundingBox bbox = BBoxMS(include_children, pred);
				if (bbox.IsValid())
				{
					bbox = m_o2p * bbox;
					for (pr::ldr::LdrObject* parent = m_parent; parent; parent = parent->m_parent)
						bbox = parent->m_o2p * bbox;
				}
				return bbox;
			}
			pr::BoundingBox BBoxWS(bool include_children) const
			{
				struct All { bool operator()(LdrObject const&) const {return true;} };
				return BBoxWS(include_children, All());
			}

			// Called when there are no more references to this object
			static void RefCountZero(RefCount<LdrObject>* doomed);
			long AddRef() const;
			long Release() const;
		};

		// Events Types *************************************

		struct Evt_AddBegin {};     // A number of objects are about to be added
		struct Evt_AddEnd           // The last object in a group has been added
		{
			int m_first, m_last;    // Index of the first and last object added
			Evt_AddEnd(int first, int last) :m_first(first) ,m_last(last) {}
		};
		struct Evt_LdrObjectAdd     // An ldr object has been added
		{
			LdrObjectPtr m_obj;     // The object that was added.
			Evt_LdrObjectAdd(LdrObjectPtr obj) :m_obj(obj) {}
		};
		struct Evt_LdrObjectChg     // An ldr object has been modified
		{
			LdrObjectPtr m_obj;     // The object that was changed.
			Evt_LdrObjectChg(LdrObjectPtr obj) :m_obj(obj) {}
		};
		struct Evt_DeleteAll {};    // All objects removed from the object manager
		struct Evt_LdrObjectDelete
		{
			LdrObject* m_obj;       // The object to be deleted. Note, not a ref ptr because this event is only sent when the ref count = 0
			Evt_LdrObjectDelete(LdrObject* obj) :m_obj(obj) {}
		};
		struct Evt_LdrObjectStepCode
		{
			LdrObjectPtr m_obj;     // The object containing step code
			Evt_LdrObjectStepCode(LdrObjectPtr obj) :m_obj(obj) {}
		};
		struct Evt_Refresh          // Called when one or more objects have changed state
		{
			LdrObjectPtr m_obj;     // The object that has changed. If null, then more than one object has changed
			Evt_Refresh() :m_obj(0) {}
			Evt_Refresh(LdrObjectPtr obj) :m_obj(obj) {}
		};
		struct Evt_LdrProgress
		{
			int m_count;            // -1 for unknown
			int m_total;            // -1 for unknown
			char const* m_desc;     // A brief description of the operation that progress is for
			bool m_allow_cancel;    // True if it's ok to cancel this operation
			LdrObjectPtr m_obj;     // An object near the current progress point
			Evt_LdrProgress(int count, int total, char const* desc, bool allow_cancel, LdrObjectPtr obj) :m_count(count) ,m_total(total) ,m_desc(desc) ,m_allow_cancel(allow_cancel) ,m_obj(obj) {}
		};
		struct Evt_LdrObjectSelectionChanged {}; // Event fired from the UI when the selected object changes
		struct Evt_SettingsChanged {}; // Sent by the object manager ui whenever its settings have changed

		// LdrObject Creation functions *********************************************

		// Callback function for editing an existing model
		typedef void (__stdcall *EditObjectCB)(pr::rdr::ModelPtr model, void* ctx, pr::Renderer& rdr);

		// Add the ldr objects described in 'reader' to 'objects'
		// Note: this is done as a background thread while a progrss dialog is displayed
		void Add(pr::Renderer& rdr, pr::script::Reader& reader, pr::ldr::ObjectCont& objects, pr::ldr::ContextId context_id = DefaultContext, bool async = true);

		// Add models/instances from a text file containing linedrawer script
		inline void AddFile(pr::Renderer& rdr, char const* filename, pr::ldr::ObjectCont& objects, pr::ldr::ContextId context_id = DefaultContext, bool async = true, pr::script::IErrorHandler* script_error_handler = 0, pr::script::IEmbeddedCode* lua_code_handler = 0)
		{
			pr::script::Reader reader;
			pr::script::FileSrc src(filename);
			reader.ErrorHandler() = script_error_handler;
			reader.CodeHandler() = lua_code_handler;
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
			reader.ErrorHandler() = script_error_handler;
			reader.CodeHandler() = lua_code_handler;
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
			pr::v4 const* normals = nullptr,        // nullptr or a pointer to vcount normals
			pr::v2 const* tex_coords = nullptr,     // nullptr or a pointer to vcount tex coords
			pr::ldr::ContextId context_id = DefaultContext);

		// Add a custom object via callback
		pr::ldr::LdrObjectPtr Add(pr::Renderer& rdr, pr::ldr::ObjectAttributes attr, int icount, int vcount, EditObjectCB edit_cb, void* ctx, pr::ldr::ContextId context_id = DefaultContext);

		// Remove all objects from 'objects' that have a context id matching one in 'doomed' and not in 'excluded'
		// If 'doomed' is 0, all are assumed doomed. If 'excluded' is 0, none are assumed excluded
		// 'excluded' is considered after 'doomed' so if any context ids are in both arrays, they will be excluded.
		void Remove(pr::ldr::ObjectCont& objects, pr::ldr::ContextId const* doomed, std::size_t dcount, pr::ldr::ContextId const* excluded, std::size_t ecount);

		// Remove 'obj' from 'objects'
		void Remove(pr::ldr::ObjectCont& objects, pr::ldr::LdrObjectPtr obj);

		// Modify the geometry of an LdrObject
		void Edit(pr::Renderer& rdr, LdrObjectPtr object, EditObjectCB edit_cb, void* ctx);

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
