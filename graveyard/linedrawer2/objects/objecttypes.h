//*******************************************************************************************
//
//	This header contains the things that can be drawn
//
//*******************************************************************************************
#ifndef OBJECTTYPES_H
#define OBJECTTYPES_H

#if USE_OLD_PARSER

#include "pr/common/StdVector.h"
#include "pr/maths/maths.h"
#include "pr/geometry/geometry.h"
#include "pr/renderer/renderer.h"
#include "LineDrawer/Source/Forward.h"
#include "LineDrawer/Objects/LdrInstance.h"
#include "LineDrawer/Objects/AnimationData.h"
#include "pr/linedrawer/customobjectdata.h"

typedef std::vector<LdrObject*>	TLdrObjectPtrVec;
typedef std::vector<v4>			TPointVec;
typedef std::vector<uint16>		TIndexVec;
typedef std::vector<Colour32>	TColourVec;

//*****
// The supported drawable types
enum eType
{
	tUnknown,
	tPoint,
	tLine,
		tLineD,
		tLineNL,
		tLineList,
		tRectangle,
		tRectangleLU,
		tRectangleWHZ,
		tCircleR,
		tCircleRxRyZ,
	tTriangle,
	tQuad,
		tQuadLU,
		tQuadWHZ,
	tBox,
		tBoxLU,
		tBoxWHD,
		tBoxList,
	tCylinder,
		tCylinderHR,
		tCylinderHRxRy,
	tSphere,
		tSphereR,
		tSphereRxRyRz,
	tCapsule,
		tCapsuleHR,
		tCapsuleHRxRy,
	tPolytope,
	tFrustum,
		tFrustumWHNF,
		tFrustumATNF,
	tGrid,
		tGridWH,
	tSurface,
		tSurfaceWHD,
	tMatrix,
		tMatrix3x3,
		tMatrix4x4,
	tMesh,
	tFile,
	tGroup,
	tGroupCyclic,
	tCustom,
	NUMBER_OF_TYPES
};
inline eType GetLDObjectType(const char* type_name)
{
	if( _stricmp(type_name, "POINT")		== 0 )	return tPoint;
	if( _stricmp(type_name, "LINE")			== 0 )	return tLine;
	if( _stricmp(type_name, "LINED")		== 0 )	return tLineD;
	if( _stricmp(type_name, "LINENL")		== 0 )	return tLineNL;
	if( _stricmp(type_name, "LINELIST")		== 0 )	return tLineList;
	if( _stricmp(type_name, "RECTANGLE")	== 0 )	return tRectangle;
	if( _stricmp(type_name, "RECTANGLELU")	== 0 )	return tRectangleLU;
	if( _stricmp(type_name, "RECTANGLEWHZ")	== 0 )	return tRectangleWHZ;
	if( _stricmp(type_name, "CIRCLER")		== 0 )	return tCircleR;
	if( _stricmp(type_name, "CIRCLERXRYZ")	== 0 )	return tCircleRxRyZ;
	if( _stricmp(type_name, "TRIANGLE")		== 0 )	return tTriangle;
	if( _stricmp(type_name, "QUAD")			== 0 )	return tQuad;
	if( _stricmp(type_name, "QUADLU")		== 0 )	return tQuadLU;
	if( _stricmp(type_name, "QUADWHZ")		== 0 )	return tQuadWHZ;
	if( _stricmp(type_name, "BOXLU")		== 0 )	return tBoxLU;
	if( _stricmp(type_name, "BOXWHD")		== 0 )	return tBoxWHD;
	if( _stricmp(type_name, "BOXLIST")		== 0 )	return tBoxList;
	if( _stricmp(type_name, "CYLINDERHR")	== 0 )	return tCylinderHR;
	if( _stricmp(type_name, "CYLINDERHRXRY")== 0 )	return tCylinderHRxRy;
	if( _stricmp(type_name, "SPHERER")		== 0 )	return tSphereR;
	if( _stricmp(type_name, "SPHERERXRYRZ")	== 0 )	return tSphereRxRyRz;
	if( _stricmp(type_name, "CAPSULEHR")	== 0 )	return tCapsuleHR;
	if( _stricmp(type_name, "CAPSULEHRXRY")	== 0 )	return tCapsuleHRxRy;
	if( _stricmp(type_name, "POLYTOPE")		== 0 )	return tPolytope;
	if( _stricmp(type_name, "FRUSTUM")		== 0 )	return tFrustum;
	if( _stricmp(type_name, "FRUSTUMWHNF")	== 0 )	return tFrustumWHNF;
	if( _stricmp(type_name, "FRUSTUMATNF")	== 0 )	return tFrustumATNF;
	if( _stricmp(type_name, "GRID")			== 0 )	return tGrid;
	if( _stricmp(type_name, "GRIDWH")		== 0 )	return tGridWH;
	if( _stricmp(type_name, "SURFACE")		== 0 )	return tSurface;
	if( _stricmp(type_name, "SURFACEWHD")	== 0 )	return tSurfaceWHD;
	if( _stricmp(type_name, "MATRIX")		== 0 )	return tMatrix;
	if( _stricmp(type_name, "MATRIX3X3")	== 0 )	return tMatrix3x3;
	if( _stricmp(type_name, "MATRIX4X4")	== 0 )	return tMatrix4x4;
	if( _stricmp(type_name, "MESH")			== 0 )	return tMesh;
	if( _stricmp(type_name, "FILE")			== 0 )	return tFile;
	if( _stricmp(type_name, "GROUP")		== 0 )	return tGroup;
	if( _stricmp(type_name, "GROUPCYCLIC")	== 0 )	return tGroupCyclic;
	if( _stricmp(type_name, "CUSTOM")		== 0 )	return tCustom;
	return tUnknown;
}
inline const char* GetLDObjectTypeString(eType type)
{
	switch( type )
	{
	case tPoint:			return "Point";
	case tLine:				return "Line";
	case tLineD:			return "LineD";
	case tLineNL:			return "LineNL";
	case tLineList:			return "LineList";
	case tRectangle:		return "Rectangle";
	case tRectangleLU:		return "RectangleLU";
	case tRectangleWHZ:		return "RectangleWHZ";
	case tCircleR:			return "CircleR";
	case tCircleRxRyZ:		return "CircleRxRyZ";
	case tTriangle:			return "Triangle";
	case tQuad:				return "Quad";
	case tQuadLU:			return "QuadLU";
	case tQuadWHZ:			return "QuadWHZ";
	case tBox:				return "Box";
	case tBoxLU:			return "BoxLU";
	case tBoxWHD:			return "BoxWHD";
	case tBoxList:			return "BoxList";
	case tCylinder:			return "Cylinder";
	case tCylinderHR:		return "CylinderHR";
	case tCylinderHRxRy:	return "CylinderHRxRy";
	case tSphere:			return "Sphere";
	case tSphereR:			return "SphereR";
	case tSphereRxRyRz:		return "SphereRxRyRz";
	case tCapsule:			return "Capsule";
	case tCapsuleHR:		return "CapsuleHR";
	case tCapsuleHRxRy:		return "CapsuleHRxRy";
	case tPolytope:			return "Polytope";
	case tFrustum:			return "Frustum";
	case tFrustumWHNF:		return "FrustumWHNF";
	case tFrustumATNF:		return "FrustumATNF";
	case tGrid:				return "Grid";
	case tGridWH:			return "GridWH";
	case tSurface:			return "Surface";
	case tSurfaceWHD:		return "SurfaceWHD";
	case tMatrix:			return "Matrix";
	case tMatrix3x3:		return "Matrix3x3";
	case tMatrix4x4:		return "Matrix4x4";
	case tMesh:				return "Mesh";
	case tFile:				return "File";
	case tGroup:			return "Group";
	case tGroupCyclic:		return "GroupCyclic";
	case tCustom:			return "Custom";
	case tUnknown:			return "Unknown";
	default:				return "Unknown";
	}
}

//*****
// A virtual base class for all things that can be drawn
struct LdrObject
{
	LdrObject(eType sub_type, const std::string& name, Colour32 colour, const std::string& source);
	virtual ~LdrObject();
	virtual eType		GetType() const = 0;
	virtual void		CreateRenderObject() = 0;
	
	eType				GetSubType() const				{ return m_sub_type; }
	std::string			GetSourceString() const			{ return m_source_string; }
	virtual bool		SetCyclic(bool start);
	void				SetEnable(bool enabled, bool recursive);
	void				SetWireframe(bool wireframe, bool recursive);
	void				SetAlpha(bool on, bool recursive);
	void				SetColour(Colour32 colour, bool recursive, bool mask);
	BoundingBox			BBox(bool including_children = true) const;
	BoundingBox			WorldSpaceBBox(bool including_children = true) const;
	m4x4				ObjectToWorld() const;
	void				SetAnimationOffset(m4x4& animation_offset);
	virtual void		Render(rdr::Viewport& viewport, const m4x4& parent_object_to_world = m4x4Identity);
	    	
	TPointVec			m_point;						// The points that make up the object
	LdrInstance			m_instance;						// An instance of this model
	eType				m_sub_type;						// The sub type within eType
	std::string			m_name;							// An identifier for the object
	Colour32			m_base_colour;					// The original colour of the object when created
	BoundingBox			m_bbox;							// Local space Bounding box 
	AnimationData		m_animation;					// Data used to animate this object
	m4x4				m_object_to_parent;				// An offset transform from this object to its parent
	LdrObject*			m_parent;						// The parent of this object
	TLdrObjectPtrVec	m_child;						// Child objects of this object
	TColourVec			m_vertex_colour;				// Vertex colours
	bool				m_enabled;						// True if this object is visible
	bool				m_wireframe;					// True if we're drawing this object in wireframe
	std::string			m_source_string;				// The string used to generate this object
	HTREEITEM			m_tree_item;					// Location in the DataManagerGUI tree control
	int					m_list_item;					// Location in the DataManagerGUI list control
	void*				m_user_data;					// User data pointer for plugins
};
typedef LdrObject LdrObject;

// An array of points
struct TPoint : public LdrObject
{
	TPoint(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{}
	eType				GetType() const	{ return tPoint; }
	void				CreateRenderObject();
};

// An array of lines
struct TLine : public LdrObject
{
	TLine(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{}
	eType				GetType() const { return tLine; }
	void				CreateRenderObject();
};

//*****
// A Triangle
struct TTriangle : public LdrObject
{
	TTriangle(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{}
	eType				GetType() const { return tTriangle; }
	void				CreateRenderObject();
};

//*****
// A Quad
struct TQuad : public LdrObject
{
	TQuad(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	,m_texture("")
	{}
	eType				GetType() const { return tQuad; }
	void				CreateRenderObject();
	std::string			m_texture;
};

//*****
// A box
struct TBox : public LdrObject
{
	TBox(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{}
	eType				GetType() const { return tBox; }
	void				CreateRenderObject();
};

//*****
// A cyliner
struct TCylinder : public LdrObject
{
	TCylinder(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	,m_wedges(40)
	,m_layers(1)
	{}
	eType				GetType() const { return tCylinder; }
	void				CreateRenderObject();
	uint	m_wedges;	// The number of radial divisions
	uint	m_layers;	// The number of lengthwise divisions
};

//*****
// A sphere
struct TSphere : public LdrObject
{
	TSphere(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	,m_divisions(3)
	,m_texture("")
	{}
	eType				GetType() const { return tSphere; }
	void				CreateRenderObject();
	uint		m_divisions;
	std::string	m_texture;
};

//*****
// A capsule
struct TCapsule : public LdrObject
{
	TCapsule(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	,m_wedges(40)
	,m_layers(1)
	{}
	eType				GetType() const { return tCapsule; }
	void				CreateRenderObject();
	uint	m_wedges;	// The number of radial divisions
	uint	m_layers;	// The number of lengthwise divisions
};

//*****
// A polytope
struct TPolytope : public LdrObject
{
	TPolytope(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{}
	eType				GetType() const { return tPolytope; }
	void				CreateRenderObject();
};

//*****
// A Frustum
struct TFrustum : public LdrObject
{
	TFrustum(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{}
	eType				GetType() const { return tFrustum; }
	void				CreateRenderObject();
};

//*****
// A Grid
struct TGrid : public LdrObject
{
	TGrid(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{}
	eType				GetType() const { return tGrid; }
	void				CreateRenderObject();
};

//*****
// A Surface
struct TSurface : public LdrObject
{
	TSurface(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{}
	eType				GetType() const { return tSurface; }
	void				CreateRenderObject();
};

//*****
// A Matrix
struct TMatrix : public LdrObject
{
	TMatrix(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{}
	eType				GetType() const	{ return tMatrix; }
	void				CreateRenderObject();
};

//*****
// A mesh
struct TMesh : public LdrObject
{
	TMesh(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	,m_generate_normals(false)
	,m_line_list(false)
	{}
	eType				GetType() const { return tMesh; }
	void				CreateRenderObject();
	TIndexVec			m_index;
	TPointVec			m_normal;
	bool				m_generate_normals;
	bool				m_line_list;
};

//*****
// X file, ASE file, etc
struct TFile : public LdrObject
{
	TFile(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	,m_geometry()
	,m_generate_normals(false)
	,m_frame_number(0)
	{}
	eType				GetType() const { return tFile; }
	void				CreateRenderObject();
	Geometry			m_geometry;
	bool				m_generate_normals;
	uint				m_frame_number;
};

//*****
// A collection of any or all of the above
struct TGroup : public LdrObject
{
	TGroup(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{}
	eType				GetType() const { return tGroup; }
	void				CreateRenderObject();
};

//*****
// A collection of animation frames
struct TGroupCyclic : public LdrObject
{
	TGroupCyclic(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	,m_ms_per_frame(1000)
	,m_start_time(0)
	,m_cycling(false)
	{}
	enum Style { START_END, END_START, PING_PONG };

	eType				GetType() const { return tGroupCyclic; }
	void				Render(rdr::Viewport& viewport, const m4x4& parent_object_to_world = m4x4Identity);
	void				CreateRenderObject();
	bool				SetCyclic(bool start);
	
	Style				m_style;
	uint				m_ms_per_frame;
	uint				m_start_time;
	bool				m_cycling;
};

//*****
// A custom object with model data modified via call back function
struct TCustom : public LdrObject
{
	TCustom(eType sub_type, const std::string& name, Colour32 colour, const std::string& section)
	:LdrObject(sub_type, name, colour, section)
	{ PR_ASSERT_STR(PR_DBG_LDR, false, "This object isn't designed to be constructed this way"); }
	eType				GetType() const { return tCustom; }
	void				CreateRenderObject() {}
	explicit TCustom(LineDrawer& ldr, pr::ldr::CustomObjectData const& settings);
};

#endif//USE_OLD_PARSER
#endif//OBJECTTYPES_H
