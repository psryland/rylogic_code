//*******************************************************************************************
// LineDrawer
//	(c)opyright 2002 Rylogic Limited
//*******************************************************************************************
#ifndef LDR_OBJECTS_H
#define LDR_OBJECTS_H

#if USE_NEW_PARSER

#include "LineDrawer/Source/Forward.h"
#include "LineDrawer/Objects/LdrInstance.h"
#include "LineDrawer/Objects/AnimationData.h"

enum ELdrObject
{
	ELdrObject_Unknown,
	#define LDR_OBJECT(identifier, hash) ELdrObject_##identifier,
	#include "LineDrawer/Objects/LdrObjects.inc"
	ELdrObject_Custom,
	ELdrObject_NumberOf
};

enum ELdrObjectException
{
	ELdrObjectException_SyntaxError,
	ELdrObjectException_ValueOutOfRange,
	ELdrObjectException_FailedToCreateRdrModel
};

// Converts a script object type enum to a string
char const* ToString(ELdrObject type);

// Base class for all line drawer objects
struct LdrObject
{
	std::string			m_name;							// An identifier for the object
	Colour32			m_base_colour;					// The original colour of the object when created
	BoundingBox			m_bbox;							// Local space Bounding box 
	ELdrObject			m_type;							// The ldr script object type
	m4x4				m_object_to_parent;				// An offset transform from this object to its parent
	LdrObject*			m_parent;						// The parent of this object
	TLdrObjectPtrVec	m_child;						// Child objects of this object
	bool				m_enabled;						// True if this object is visible
	bool				m_wireframe;					// True if we're drawing this object in wireframe
	LdrInstance			m_instance;						// An instance of this model
	HTREEITEM			m_tree_item;					// Location in the DataManagerGUI tree control
	int					m_list_item;					// Location in the DataManagerGUI list control
	AnimationData		m_animation;					// Data used to animate this object
	void*				m_user_data;					// User data pointer for plugins
	LineDrawer*			m_ldr;							// Reference to the main object

	LdrObject(LineDrawer& ldr, std::string const& name, Colour32 base_colour);
	virtual				~LdrObject();
	m4x4				ObjectToWorld() const;
	BoundingBox			BBox(bool include_children) const;
	BoundingBox			WorldSpaceBBox(bool include_children) const;
	virtual void		Render(rdr::Viewport& viewport, m4x4 const& parent_object_to_world);
	virtual void		Cycle(bool on);					// Enable cycling through child objects. Only applies to groups.
	void				SetEnable(bool enabled, bool recursive);
	void				SetWireframe(bool wireframe, bool recursive);
	void				SetColour(Colour32 colour, bool recursive, bool mask);
	void				SetAlpha(bool on, bool recursive);

protected:
	rdr::ModelManager&		ModelMgr() const;
	rdr::MaterialManager&	MatMgr() const;
};

// A collection of other objects, with the ability to cycle through the children
struct TGroup : public LdrObject
{
	enum EMode { EMode_StartEnd = 0, EMode_EndStart, EMode_PingPong, EMode_NumberOf };
	bool	m_cycle;
	EMode	m_mode;
	uint	m_start_time;
	uint	m_ms_per_frame;
	
	TGroup(LineDrawer& ldr, std::string const& name, Colour32 colour)
	:LdrObject(ldr, name, colour)
	,m_cycle(false)
	,m_mode(EMode_StartEnd)
	,m_start_time(0)
	,m_ms_per_frame(1000)
	{}
	void CreateRenderObject();
	void Cyclic(bool on);
	void Render(rdr::Viewport& viewport, m4x4 const& parent_object_to_world);
};

// An array of points
struct TPoints : public LdrObject
{
	TPoints(LineDrawer& ldr, std::string const& name, Colour32 colour) :LdrObject(ldr, name, colour) {}
	void CreateRenderObject(pr::TVecCont const& points);
};

// An array of lines
struct TLines : public LdrObject
{
	TLines(LineDrawer& ldr, std::string const& name, Colour32 colour) :LdrObject(ldr, name, colour) {}
	void CreateRenderObject(TVecCont const& points, TColour32Cont const& colours);
};

// An array of triangles
struct TTriangles : public LdrObject
{
	TTriangles(LineDrawer& ldr, std::string const& name, Colour32 colour) :LdrObject(ldr, name, colour) {}
	void CreateRenderObject(pr::TVertexCont const& verts, pr::GeomType geom_type, std::string const& texture);
};

// An array of boxes
struct TBoxes : public LdrObject
{
	TBoxes(LineDrawer& ldr, std::string const& name, Colour32 colour) :LdrObject(ldr, name, colour) {}
	void CreateRenderObject(pr::TVecCont const& points);
};

// A cyliner
struct TCylinder : public LdrObject
{
	TCylinder(LineDrawer& ldr, std::string const& name, Colour32 colour) :LdrObject(ldr, name, colour) {}
	void CreateRenderObject(float height, float radiusX, float radiusZ, uint wedges, uint layers);
};

// A sphere
struct TSphere : public LdrObject
{
	TSphere(LineDrawer& ldr, std::string const& name, Colour32 colour) :LdrObject(ldr, name, colour) {}
	void CreateRenderObject(float radiusX, float radiusY, float radiusZ, uint divisions, std::string const& texture);
};

// A mesh
struct TMesh : public LdrObject
{
	TMesh(LineDrawer& ldr, std::string const& name, Colour32 colour) :LdrObject(ldr, name, colour)  {}
	void CreateRenderObject(TVertexCont const& verts, TIndexCont const& indices, pr::GeomType geom_type, bool generate_normals, bool line_list);
	void CreateRenderObject(pr::Mesh const& mesh);
};

// A custom created object
struct TCustom : public LdrObject
{
//	TCustom(LineDrawer& ldr, std::string const& name, Colour32 colour) :LdrObject(ldr, name, colour) {}//{ PR_ASSERT_STR(PR_DBG_LDR, false, "This object isn't designed to be constructed this way"); }
	explicit TCustom(LineDrawer& ldr, pr::ldr::CustomObjectData const& settings);
};

#endif//USE_NEW_PARSER
#endif//LDR_OBJECTS_H
