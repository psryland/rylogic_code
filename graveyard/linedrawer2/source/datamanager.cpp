//*********************************************
// Line Drawer
//	(C)opyright Rylogic Limited 2007
//*********************************************
#include "Stdafx.h"
#include "pr/filesys/fileex.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/Source/DataManager.h"

DataManager::DataManager()
:m_bbox(BBoxUnit)
,m_gui()
,m_data()
,m_auto_clear_time(0)
,m_last_add_object_time(0)
,m_last_selected_object(0)
,m_index_of_last_selected(0)
{}

DataManager::~DataManager()
{
	Clear();
}

// Create the data manager window
void DataManager::CreateGUI()
{
	m_gui.Create(DataManagerGUI::IDD, (CWnd*)LineDrawer::Get().m_line_drawer_GUI);
}

// Show the data manager window
void DataManager::ShowGUI()
{
	m_gui.SetWindowPos((CWnd*)LineDrawer::Get().m_line_drawer_GUI, 0,0,0,0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
}

// Add an object to our data
void DataManager::AddObject(LdrObject* object, LdrObject* after)
{
	// Do auto clear
	DWORD now = GetTickCount();
	if( m_auto_clear_time > 0 && now - m_last_add_object_time > m_auto_clear_time )
	{
		Clear();
	}

	// Grow the bounding box
	if( m_data.empty() ) m_bbox.reset();
	BoundingBox bbox = object->BBox();
	if( bbox != BBoxReset )
	{
		PR_ASSERT_STR(PR_DBG_LDR, bbox.m_centre + v4ZAxis != bbox.m_centre, "BoundingBox too distant from origin");
		PR_ASSERT_STR(PR_DBG_LDR, bbox.m_radius + (v4One-v4Origin) != bbox.m_radius, "BoundingBox too large");
		Encompase(m_bbox, bbox);
	}

	// If an object to insert after has been provided, get the list that it is in and insert after it
	if( after )
	{
		TLdrObjectPtrVec& list = (after->m_parent == 0) ? (m_data) : (object->m_parent->m_child);
		
		// Find 'after' in the list
		TLdrObjectPtrVec::iterator i = list.begin(), i_end = list.end();
		for( ; i != i_end && *i != after; ++i ) {}
		PR_ASSERT_STR(PR_DBG_LDR, i != i_end, "Object 'after' is not in the data manager.");

		// Insert after 'after'
		m_gui.Add(object, *i);
		list.insert(i, object);
	}
	// Otherwise just add to the end of the root list
	else
	{
		m_gui.Add(object, !m_data.empty() ? m_data.back() : 0);
		m_data.push_back(object);
	}

	// Remember this event
	m_last_add_object_time = GetTickCount();
}

//// Replace 'object' with an object created using 'source_string'
//void DataManager::UpdateObject(LdrObject*& object, const char* source_string, uint length)
//{
//	StringParser& string_parser = LineDrawer::Get().m_string_parser;
//	string_parser.Clear();
//	if( string_parser.Parse(source_string, length) && string_parser.GetNumObjects() > 0 )
//	{
//		LdrObject* new_object		= string_parser.GetObject(0);
//		new_object->m_enabled	= object->m_enabled;
//		new_object->m_parent	= object->m_parent;
//		new_object->m_wireframe	= object->m_wireframe;
//
//		// Replace 'object' with 'new_object' in the data list 'object' is in
//		TLdrObjectPtrVec& list = (object->m_parent == 0) ? (m_data) : (object->m_parent->m_child);
//
//		TLdrObjectPtrVec::iterator i = list.begin(), i_end = list.end();
//		for( ; i != i_end; ++i )
//		{
//			if( *i == object ) break;
//		}
//		PR_ASSERT_STR(PR_DBG_LDR, i != i_end, "Object not found for updating.");
//
//		// Tell the GUI to replace 'object' with 'new_object'
//		m_gui.Add(new_object, object);
//		m_gui.Delete(object);
//		//m_gui.Update(object, new_object);
//
//		// Replace 'object'
//		*i = new_object;
//		delete object;
//		object = new_object;
//	}
//}

// Remove an object from the data
void DataManager::DeleteObject(LdrObject* object)
{
	// Delete 'object' from the data list it's in
	TLdrObjectPtrVec& list = (object->m_parent == 0) ? (m_data) : (object->m_parent->m_child);

	// Find 'object'
	TLdrObjectPtrVec::iterator i = list.begin(), i_end = list.end();
	for( ; i != i_end && *i != object; ++i ) {}
	if( i != i_end )
	{
		// Remove it from the data manager list
		list.erase(i);

		// Tell the GUI that the object has been deleted
		m_gui.Delete(object);

		// Tell the plugin manager that the object has been deleted
		LineDrawer::Get().m_plugin_manager.DeleteObject(object);

		// Delete the object
		delete object;
	}
}

// Clear all of our data
void DataManager::Clear()
{
	// Clear the GUI
	m_gui.Clear();

	// Delete all of the objects
	uint start_delete_time = (uint)GetTickCount();
	uint num_objects = (uint)m_data.size();
	while( !m_data.empty() )
	{
		// Display the progress box if deleting takes more than 2 seconds
		std::string progress_msg = "Clearing data: " + m_data.front()->m_name;
		LineDrawer::Get().SetProgress(num_objects - (uint)m_data.size(), num_objects, progress_msg.c_str(), GetTickCount() - start_delete_time);

		// Delete object may call into a plugin that can cause other objects to be deleted.
		// Assume 'm_data' is modified by this call.
		DeleteObject(m_data.front());
	}
	LineDrawer::Get().SetProgress(0, 0, "");

	m_data.clear();
	m_bbox.unit();
	m_last_add_object_time = GetTickCount();
	m_last_selected_object = 0;
	m_index_of_last_selected = 0;
}

// Save the state of all currently loaded objects
void DataManager::SaveObjectStates(TObjectState& state, TLdrObjectPtrVec const& objects)
{
	for( TLdrObjectPtrVec::const_iterator i = objects.begin(), i_end = objects.end(); i != i_end; ++i )
	{
		LdrObject const& obj = **i;
		
		pr::CRC hash = Crc(obj.m_name.c_str(), obj.m_name.size());
		TObjectState::iterator state_iter = state.find(hash);
		if( state_iter == state.end() )
		{
			ObjectState& obj_state = state[hash];
			obj_state.m_wireframe = obj.m_wireframe;
			obj_state.m_enabled	= obj.m_enabled;
			//obj_state.m_colour = obj.m_instance.m_colour;
			obj_state.m_valid = true;
		}
		else
		{
			// Name clash, don't persist state
			ObjectState& obj_state = state_iter->second;
			obj_state.m_valid = false;
		}
		SaveObjectStates(state, obj.m_child);
	}
}

// Restore object states
void DataManager::ApplyObjectStates(TObjectState const& state, TLdrObjectPtrVec& objects)
{
	for( TLdrObjectPtrVec::const_iterator i = objects.begin(), i_end = objects.end(); i != i_end; ++i )
	{
		LdrObject& obj = **i;
		
		pr::CRC hash = Crc(obj.m_name.c_str(), obj.m_name.size());
		TObjectState::const_iterator state_iter = state.find(hash);
		if( state_iter != state.end() )
		{
			ObjectState const& obj_state = state_iter->second;
			if( obj_state.m_valid )
			{
				obj.SetWireframe(obj_state.m_wireframe, false);
				obj.SetEnable(obj_state.m_enabled, false);
				//obj.SetColour(obj_state.m_colour, false, false);
			}
		}
		ApplyObjectStates(state, obj.m_child);
	}
}

// Save the scene to a file
void DataManager::SaveToFile(const char* filename)
{
	PR_ASSERT(PR_DBG_LDR, filename);
	pr::Handle file = FileOpen(filename, pr::EFileOpen_Writing);
	if (file == INVALID_HANDLE_VALUE) { LineDrawer().Get().m_error_output.Error(Fmt("Failed to open file: %s", filename).c_str()); return; }

	for( TLdrObjectPtrVec::iterator i = m_data.begin(), i_end = m_data.end(); i != i_end; ++i )
	{
		std::string source = (*i)->GetSourceString();
		str::Replace(source, "\r\n", "\n");
		FileWrite(file, source.c_str(), DWORD(source.length()));
	}
}

//*****
// Start/Stop any animations. Returns the state of animations
void DataManager::SetObjectCyclic(bool start)
{
	for( TLdrObjectPtrVec::iterator i = m_data.begin(), i_end = m_data.end(); i != i_end; ++i )
	{
		(*i)->SetCyclic(start);
	}
}

//*****
// Select nothing in the scene
void DataManager::SelectNone()
{
	m_last_selected_object = 0;
	m_gui.SelectNone();
}

//*****
// Select the object nearest to 'point'
void DataManager::SelectNearest(v4 const& point)
{
	float dist = maths::float_max;
	for( TLdrObjectPtrVec::iterator i = m_data.begin(), i_end = m_data.end(); i != i_end; ++i )
	{
		float d = Length3Sq((*i)->ObjectToWorld().pos - point);
		if( d < dist )
		{
			dist = d;
			m_last_selected_object = *i;
		}
	}
}

//*****
// Select the next object in the data list
void DataManager::SelectNext()
{
	if( m_data.empty() ) return;
	TLdrObjectPtrVec::iterator iter = std::find(m_data.begin(), m_data.end(), m_last_selected_object);
	if( iter == m_data.end() || iter == m_data.end() - 1 )
		m_last_selected_object = *m_data.begin();
	else
		m_last_selected_object = *(++iter);
}
void DataManager::SelectPrev()
{
	if( m_data.empty() ) return;
	TLdrObjectPtrVec::iterator iter = std::find(m_data.begin(), m_data.end(), m_last_selected_object);
	if( iter == m_data.end() || iter == m_data.begin() )
		m_last_selected_object = *(--m_data.end());
	else
		m_last_selected_object = *(--iter);
}

//*****
// Select an object at the screen space co-ord 'point'
void DataManager::Select(const v2& point)
{
	Camera& camera	= LineDrawer::Get().m_navigation_manager.m_camera;
	v4 world_point	= camera.ScreenToWorld(v4::make(point, 1.0f, 1.0f));
	v4 camera_point = camera.GetPosition();
	Line3 select_vector = Line3::make(camera_point, (world_point - camera_point));

	//Debug code for drawing the select vector
	//std::string spot = Fmt("*BoxWHD spot FFFFFF00 { 0.01 0.01 0.01 *Position {%f %f %f} *LineD ray FFFFFF00 {0 0 0 %f %f %f} }"
	//,select_vector.m_point.x ,select_vector.m_point.y ,select_vector.m_point.z
	//,select_vector.m_line.x ,select_vector.m_line.y ,select_vector.m_line.z);
	//LineDrawer::Get().RefreshFromString(spot.c_str(), (uint)spot.size(), false, false);

	// Allow for orthographic projections
	if( !camera.Is3D() )
	{
		v4 forward = camera.GetForward() * Dot3(camera.GetForward(), select_vector.m_line);
		select_vector.m_point = select_vector.m_line - forward;
		select_vector.m_line  = forward;
	}

	// Find an object whose bounding box intersects this vector
	m_gui.SelectNone();
	for( std::size_t i = 0, i_end = m_data.size(); i != i_end; ++i )
	{
		std::size_t index = (i + 1 + m_index_of_last_selected) % m_data.size();
		LdrObject* object = m_data[index];
		
		if( !object->m_enabled ) continue;
		Line3 select_vector_os = GetInverseFast(object->ObjectToWorld()) * select_vector;
		if( IsIntersection(object->m_bbox, select_vector_os) )
		{
			m_gui.SelectObject(object);
			m_last_selected_object	 = object;
			m_index_of_last_selected = index;
			return;
		}
	}
}

//*****
// Draw all of our contained objects
void DataManager::Render(rdr::Viewport& viewport)
{
	//bool animation_on		= LineDrawer::Get().m_animation_on;
	//NavigationManager& nav	= LineDrawer::Get().m_navigation_manager;

	for( TLdrObjectPtrVec::iterator i = m_data.begin(), i_end = m_data.end(); i != i_end; ++i )
	{
		//if( nav.IsVisible((*i)->WorldSpaceBBox()) )
		{
			(*i)->Render(viewport);
		}
	}
}
