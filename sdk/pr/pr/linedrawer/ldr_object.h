//***************************************************************************************************
// Ldr Object
//  Copyright © Rylogic Ltd 2009
//***************************************************************************************************
	
#pragma region KEYWORDS
#ifndef LDR_OBJECT
#define LDR_OBJECT(name, hashvalue)
#endif
#ifndef LDR_KEYWORD
#define LDR_KEYWORD(keyword, hashvalue)
#endif

// Ldr object types
LDR_OBJECT(Unknown          ,0x062170b2)
LDR_OBJECT(Line             ,0x10d28008)
LDR_OBJECT(LineD            ,0x07512a2a)
LDR_OBJECT(LineList         ,0x0e9c633b)
LDR_OBJECT(LineBox          ,0x11d1a8c4)
LDR_OBJECT(Spline           ,0x1f45c36e)
LDR_OBJECT(Circle           ,0x015ed657)
LDR_OBJECT(Ellipse          ,0x1dc7f472)
LDR_OBJECT(Matrix3x3        ,0x1bd46252)
LDR_OBJECT(Triangle         ,0x118ad8f6)
LDR_OBJECT(Quad             ,0x083b7f24)
LDR_OBJECT(Plane            ,0x0ed56051)
LDR_OBJECT(Box              ,0x11b87d89)
LDR_OBJECT(BoxLine          ,0x11d4daa7)
LDR_OBJECT(BoxList          ,0x1481a6af)
LDR_OBJECT(FrustumWH        ,0x02f1f573)
LDR_OBJECT(FrustumFA        ,0x14adfea2)
LDR_OBJECT(Sphere           ,0x010cf039)
LDR_OBJECT(SphereRxyz       ,0x146866ff)
LDR_OBJECT(CylinderHR       ,0x0f3d4e07)
LDR_OBJECT(ConeHR           ,0x14362856)
LDR_OBJECT(ConeHA           ,0x1f093110)
LDR_OBJECT(Mesh             ,0x07fb0d2b)
LDR_OBJECT(ConvexHull       ,0x0a93b3d5)
LDR_OBJECT(Group            ,0x1c1c3d90)
LDR_OBJECT(Instance         ,0x061978f8)
LDR_OBJECT(Custom           ,0x1a1023c5)

// Ldr script keywords
LDR_KEYWORD(O2W             ,0x1d20a7ba)
LDR_KEYWORD(M4x4            ,0x1788ecfa)
LDR_KEYWORD(M3x3            ,0x0aef28f5)
LDR_KEYWORD(Pos             ,0x13930daf)
LDR_KEYWORD(Up              ,0x1e72eb5a)
LDR_KEYWORD(Direction       ,0x0fca4076)
LDR_KEYWORD(Quat            ,0x1de5b1fd)
LDR_KEYWORD(Rand4x4         ,0x02becb08)
LDR_KEYWORD(RandPos         ,0x0456bf23)
LDR_KEYWORD(RandOri         ,0x185629c3)
LDR_KEYWORD(Euler           ,0x150894fc)
LDR_KEYWORD(Scale           ,0x1837069d)
LDR_KEYWORD(Transpose       ,0x12fafef6)
LDR_KEYWORD(Inverse         ,0x01ca4f54)
LDR_KEYWORD(Normalise       ,0x01693558)
LDR_KEYWORD(Orthonormalise  ,0x1c2a9e41)
LDR_KEYWORD(Colour          ,0x08b2c176)
LDR_KEYWORD(RandColour      ,0x0cdef959)
LDR_KEYWORD(ColourMask      ,0x05dc4ca1)
LDR_KEYWORD(Animation       ,0x004c5336)
LDR_KEYWORD(Style           ,0x0c0c447d)
LDR_KEYWORD(Period          ,0x166be380)
LDR_KEYWORD(Velocity        ,0x13c33323)
LDR_KEYWORD(AngVelocity     ,0x1572a9b4)
LDR_KEYWORD(Axis            ,0x05a50e20)
LDR_KEYWORD(Hidden          ,0x16ade487)
LDR_KEYWORD(Wireframe       ,0x067b0d73)
LDR_KEYWORD(Delimiters      ,0x084f5b30)
LDR_KEYWORD(Clear           ,0x045518bd)
LDR_KEYWORD(Camera          ,0x19028d2f)
LDR_KEYWORD(Lock            ,0x0a040f55)
LDR_KEYWORD(Coloured        ,0x078194a5)
LDR_KEYWORD(Param           ,0x090da184)
LDR_KEYWORD(Texture         ,0x126f7a1b)
LDR_KEYWORD(Video           ,0x1121909f)
LDR_KEYWORD(Divisions       ,0x1a7f5e28)
LDR_KEYWORD(Layers          ,0x0ea88b62)
LDR_KEYWORD(Wedges          ,0x0e4a1c99)
LDR_KEYWORD(Verts           ,0x1b15f488)
LDR_KEYWORD(Normals         ,0x061bcc50)
LDR_KEYWORD(Colours         ,0x1e9214e3)
LDR_KEYWORD(TexCoords       ,0x096ec538)
LDR_KEYWORD(Lines           ,0x18abd83b)
LDR_KEYWORD(Faces           ,0x14903115)
LDR_KEYWORD(Tetra           ,0x092750d0)
LDR_KEYWORD(GenerateNormals ,0x1c991230)
LDR_KEYWORD(Step            ,0x0ad1d27d)
LDR_KEYWORD(Addr            ,0x0a215e87)
LDR_KEYWORD(Filter          ,0x183f6f0c)
#undef LDR_OBJECT
#undef LDR_KEYWORD
#pragma endregion
	
#ifndef PR_LDR_OBJECT_H
#define PR_LDR_OBJECT_H

#ifndef PR_DBG_LDROBJMGR
#define PR_DBG_LDROBJMGR PR_DBG
#endif

#include <string>
#include "pr/common/min_max_fix.h"
#include "pr/common/array.h"
#include "pr/common/hash.h"
#include "pr/common/refcount.h"
#include "pr/common/refptr.h"
#include "pr/maths/maths.h"
#include "pr/str/prstdstring.h"
#include "pr/script/reader.h"
#include "pr/renderer/renderer.h"
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
		
		namespace ELdrObject
		{
			enum Type
			{
				#define LDR_OBJECT(name, hashvalue) name = hashvalue,
				#include "ldr_object.h"
			};
			inline char const* ToString(Type type)
			{
				switch (type)
				{
				default: return "unknown ldr object type";
				#define LDR_OBJECT(name, hashvalue) case name: return #name;
				#include "ldr_object.h"
				}
			}
		}
		namespace EKeyword
		{
			enum Type
			{
				#define LDR_KEYWORD(name, hashvalue) name = hashvalue,
				#include "ldr_object.h"
			};
			inline char const* ToString(Type type)
			{
				switch (type)
				{
				default: return "unknown object modifier";
				#define LDR_KEYWORD(name, hashvalue) case name: return #name;
				#include "ldr_object.h"
				}
			}
		}
		namespace EObjectBounds
		{
			enum Type {All, Selected, Visible };
		}
		namespace EAnimStyle
		{
			enum Type {NoAnimation, PlayOnce, PlayReverse, PingPong, PlayContinuous, NumberOf };
		}
		
		// An instance type for line drawer stock objects
		PR_RDR_DECLARE_INSTANCE_TYPE2
		(
			StockInstance,
			pr::rdr::ModelPtr ,m_model ,pr::rdr::instance::ECpt_ModelPtr,
			pr::m4x4          ,m_i2w   ,pr::rdr::instance::ECpt_I2WTransform
		);
		
		// An instance for passing to the renderer
		PR_RDR_DECLARE_INSTANCE_TYPE5
		(
			// Note: don't use 'm_i2w' to control the object transform, use m_o2p in the LdrObject instead
			RdrInstance,
			pr::rdr::ModelPtr           ,m_model  ,pr::rdr::instance::ECpt_ModelPtr,
			pr::m4x4                    ,m_i2w    ,pr::rdr::instance::ECpt_I2WTransform,
			pr::rdr::sort_key::Override ,m_sko    ,pr::rdr::instance::ECpt_SortkeyOverride,
			pr::rdr::rs::Block          ,m_rsb    ,pr::rdr::instance::ECpt_RenderState,
			pr::Colour32                ,m_colour ,pr::rdr::instance::ECpt_TintColour32
		);
		
		// Attributes (with defaults) for an LdrObject
		struct ObjectAttributes
		{
			ELdrObject::Type m_type;     // Object type
			std::string      m_name;     // Name of the object
			pr::Colour32     m_colour;   // Base colour of the object
			bool             m_instance; // True if an instance should be created
			
			ObjectAttributes() :m_type(ELdrObject::Unknown) ,m_name("unnamed") ,m_colour(pr::Colour32White) ,m_instance(true) {}
			ObjectAttributes(ELdrObject::Type type) :m_type(type), m_name("unnamed") ,m_colour(pr::Colour32White) ,m_instance(true) {}
			ObjectAttributes(ELdrObject::Type type, char const* name) :m_type(type), m_name(name) ,m_colour(pr::Colour32White) ,m_instance(true) {}
			ObjectAttributes(ELdrObject::Type type, char const* name, pr::Colour32 colour) :m_type(type), m_name(name) ,m_colour(colour) ,m_instance(true) {}
			ObjectAttributes(ELdrObject::Type type, char const* name, pr::Colour32 colour, bool instance) :m_type(type), m_name(name) ,m_colour(colour) ,m_instance(instance) {}
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
			EAnimStyle::Type m_style;
			float            m_period;       // Seconds
			pr::v4           m_velocity;     // Linear velocity of the animation in m/s
			pr::v4           m_ang_velocity; // Angular velocity of the animation in rad/s
			
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
		struct LdrObject :RdrInstance ,pr::RefCount<LdrObject>
		{
			// Note: try not to use the RdrInstance members for things other than rendering
			// they can temporarily have different models/transforms/etc during rendering of
			// object bounding boxes etc.
			pr::m4x4            m_o2p;          // Object to parent transform (or object to world if this is a top level object)
			ELdrObject::Type    m_type;         // Object type
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
			
			// Recursively add this object and its child to a viewport
			void AddToViewport(pr::rdr::Viewport& viewport, float time_s = 0.0f, pr::m4x4 const* p2w = &pr::m4x4Identity);
			
			// Recursively add this object using 'bbox_model' instead of its
			// actual model, located and scaled to the transform and box of this object
			void AddBBoxToViewport(pr::rdr::Viewport& viewport, pr::rdr::ModelPtr bbox_model, float time_s = 0.0f, pr::m4x4 const* p2w = &pr::m4x4Identity);
			
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
		pr::ldr::LdrObjectPtr Add(pr::Renderer& rdr, pr::ldr::ObjectAttributes attr, m4x4 const& o2w, int icount, int vcount, pr::uint16 const* index, pr::v4 const* verts, pr::v4 const* normals, pr::Colour32 const* colours, pr::v2 const* tex_coords, pr::rdr::model::EPrimitive::Type prim_type, pr::ldr::ContextId context_id = DefaultContext);
		
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
	
#endif
