//*******************************************************************************************
//
// A class to interpret strings into LdrObject objects
// and past them on to the data manager
//
//*******************************************************************************************
#ifndef STRINGPARSER_H
#define STRINGPARSER_H

#if USE_OLD_PARSER

#include "pr/common/StdVector.h"
#include "pr/common/PRScript.h"
#include "LineDrawer/Objects/ObjectTypes.h"
#include "LineDrawer/Source/Forward.h"
#include "LineDrawer/Source/CameraView.h"
#include "LineDrawer/Source/LockMask.h"

class StringParser
{
public:
	StringParser(LineDrawer* linedrawer);
	~StringParser();
	void	Clear();
	bool	Parse(FileLoader& file_loader);
	bool	Parse(const char* string, std::size_t length);
	uint	GetNumObjects() const			{ return (uint)m_store.size(); }
	LdrObject*	GetObject(uint i)				{ PR_ASSERT(PR_DBG_LDR, i < GetNumObjects()); LdrObject* to_return = m_store[i]; m_store[i] = 0; return to_return; }
	pr::script::TPaths const& GetIncludedFiles() const { return m_loader.GetIncludedFiles(); }

	// Optionals
	bool	ContainsGlobalWireframeMode() const		{ return m_global_wireframe_mode != -1; }
	int		GetGlobalWireframeMode() const			{ return m_global_wireframe_mode; }
	LockMask GetLockMask() const					{ return m_locks; }
	ViewMask GetViewMask() const					{ return m_view_mask; }
	CameraView const& GetView() const				{ return m_view; }
	//int		CameraAlign() const						{ return m_view_align; }

private:
	bool	ParseCommon				(const std::string& keyword, LdrObject* parent_object);
	LdrObject*	ParseObject				(eType type);
	bool	ParseCamera				();
	bool	ParseLocks				();
	bool	ParseGlobalWireframeMode();
	bool	ParseTransform			(m4x4& transform);
	bool	ParsePoint				(TPoint* point);
	bool	ParseLine				(TLine* line);
	bool	ParseLineD				(TLine* line);
	bool	ParseLineNL				(TLine* line);
	bool	ParseLineList			(TLine* line);
	bool	ParseLineCommon			(TLine* line, bool& normalise);
	bool	ParseRectangle			(TLine* line);
	bool	ParseRectangleLU		(TLine* line);
	bool	ParseRectangleWHZ		(TLine* line);
	bool	ParseCircleR			(TLine* line);
	bool	ParseCircleRxRyZ		(TLine* line);
	bool	ParseTriangle			(TTriangle* tri);
	bool	ParseQuad				(TQuad* quad);
	bool	ParseQuadLU				(TQuad* quad);
	bool	ParseQuadWHZ			(TQuad* quad);
	bool	ParseBoxLU				(TBox* box);
	bool	ParseBoxWHD				(TBox* box);
	bool	ParseBoxList			(TBox* box);
	bool	ParseCylinderHR			(TCylinder* cylinder);
	bool	ParseCylinderHRxRy		(TCylinder* cylinder);
	bool	ParseSphereR			(TSphere* sphere);
	bool	ParseSphereRxRyRz		(TSphere* sphere);
	bool	ParsePolytope			(TPolytope* polytope);
	bool	ParseFrustumWHNF		(TFrustum* frustum);
	bool	ParseFrustumATNF		(TFrustum* frustum);
	bool	ParseGridWH				(TGrid* grid);
	bool	ParseSurfaceWHD			(TSurface* surface);
	bool	ParseMatrix3x3			(TMatrix* matrix);
	bool	ParseMatrix4x4			(TMatrix* matrix);
	bool	ParseMesh				(TMesh* mesh);
	bool	ParseFile				(TFile* file);
	bool	ParseGroup				(TGroup* group);
	bool	ParseGroupCyclic		(TGroupCyclic* group_cyclic);
	bool	ParseAnimation			(AnimationData& animation);

private:
	LineDrawer*		m_linedrawer;		// Mr. LineDrawer
	ScriptLoader	m_loader;			// Used to read the source data
	TLdrObjectPtrVec		m_store;			// The parsed objects
	uint			m_parse_start_time;	// The time at which parsing started

	// Optionals
	ViewMask		m_view_mask;				// A mask of bits that where set in the view
	CameraView		m_view;						// The view contained in the source
	//int				m_view_align;				// 0-no align, 1-alignX, 2-alignY, 3-alignZ
	int				m_global_wireframe_mode;	// -1=not set, 0=solid, 1=wireframe, 2=solid+wire
	LockMask		m_locks;					// 
};

#endif//USE_OLD_PARSER
#endif//STRINGPARSER_H