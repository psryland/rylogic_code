//*******************************************************************************************
//
//	This class is a container for the stuff to be drawn
//
//*******************************************************************************************
#ifndef DATAMANAGER_H
#define DATAMANAGER_H

#include "pr/common/StdVector.h"
#include "pr/renderer/renderer.h"
#include "LineDrawer/Objects/ObjectTypes.h"
#include "LineDrawer/Objects/ObjectState.h"
#include "LineDrawer/Objects/LdrObjects.h"
#include "LineDrawer/GUI/DataManagerGUI.h"

class DataManager
{
public:
	DataManager();
	~DataManager();

	// GUI methods
	void		CreateGUI();
	void		ShowGUI();

	// Data methods
	void		AddObject(LdrObject* object, LdrObject* after = 0);
	void		DeleteObject(LdrObject* object);
	void		Clear();
	void		SaveObjectStates (TObjectState& state, TLdrObjectPtrVec const& objects);
	void		ApplyObjectStates(TObjectState const& state, TLdrObjectPtrVec& objects);
	void		SaveObjectStates (TObjectState& state)		{ SaveObjectStates(state, m_data); }
	void		ApplyObjectStates(TObjectState const& state){ ApplyObjectStates(state, m_data); }

	void		SaveToFile(const char* filename);
	uint		GetNumObjects() const						{ return (uint)m_data.size(); }
	LdrObject*	GetObject(uint i)							{ PR_ASSERT(PR_DBG_LDR, i < GetNumObjects()); return m_data[i]; }
	void		SetObjectName(uint i, const char* name)		{ PR_ASSERT(PR_DBG_LDR, i < GetNumObjects()); m_data[i]->m_name = name; }
	LdrObject*	GetSelectedObject() const					{ return m_last_selected_object; }
	float		GetAutoClearTime() const					{ return m_auto_clear_time / 1000.0f; }
	void		SetAutoClearTime(float sec)					{ m_auto_clear_time = static_cast<uint>(sec * 1000.0f); }
	
	void		SetObjectCyclic(bool start);
	void		SelectNone();
	void		SelectNearest(v4 const& point);
	void		SelectNext();
	void		SelectPrev();
	void		Select(const v2& point);
	void		Render(rdr::Viewport& viewport);

	/// Public members
	BoundingBox	m_bbox;

private:
	DataManagerGUI		m_gui;
	TLdrObjectPtrVec	m_data;
	uint				m_auto_clear_time;
	uint				m_last_add_object_time;
	LdrObject*			m_last_selected_object;
	std::size_t			m_index_of_last_selected;
};

#endif//DATAMANAGER_H
