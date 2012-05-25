//*******************************************************************************************
//
// A class to interpret strings into LdrObject objects
// and past them on to the data manager
//
//*******************************************************************************************
#include "Stdafx.h"
#if USE_OLD_PARSER
#include "pr/filesys/filesys.h"
#include "pr/storage/xfile/xfile.h"
#include "LineDrawer/Source/LineDrawer.h"
#include "LineDrawer/Source/FileLoader.h"
#include "LineDrawer/Objects/StringParser.h"

// Constructor
StringParser::StringParser(LineDrawer* linedrawer)
:m_linedrawer(linedrawer)
{
	m_loader.SetDelimiters(",;");
	m_loader.ThrowExceptions(false);
	Clear();
}

// Destructor
StringParser::~StringParser()
{
	Clear();
}

// Delete any objects left in our store
void StringParser::Clear()
{
	for( TLdrObjectPtrVec::iterator iter = m_store.begin(), iter_end = m_store.end(); iter != iter_end; ++iter )
	{
		delete *iter;
	}
	m_store.clear();
	m_view_mask.reset();
	m_global_wireframe_mode = -1;
}

// Parse data contained in the file loader
bool StringParser::Parse(FileLoader& file_loader)
{
	file_loader.ClearWatchFiles();

	std::string data;
	for( TLdrFileVec::iterator f = file_loader.m_file.begin(), f_end = file_loader.m_file.end(); f != f_end; ++f )
	{
		LdrFile& file = *f;
		SetWindowText(m_linedrawer->m_window_handle, Fmt("LineDrawer - Parsing file: \"%s\" ... Please wait", file.m_name.c_str()).c_str());

		// This file needs watching
		file_loader.AddFileToWatch(file.m_name.c_str());

		// Get the file data
		data.resize(0);
		if( !file.GetData(data) )
		{
			m_linedrawer->m_error_output.Error(Fmt("FileLoader: Failed to load %s", file.m_name.c_str()).c_str());
			//m_linedrawer->RemoveRecentFile(file.m_name.c_str());
			continue;
		}

		// Add the file's path to the include paths in the loader
		m_loader.AddIncludePath(pr::filesys::GetDirectory(file.m_name));

		// Parse it
		Parse(data.c_str(), data.size());

		// Clear the include paths
		m_loader.ClearIncludePaths();

		// Add any includes files for watching as well
		for( pr::script::TPaths::const_iterator i = m_loader.GetIncludedFiles().begin(), i_end = m_loader.GetIncludedFiles().end(); i != i_end; ++i )
		{
			file_loader.AddFileToWatch(i->c_str());
		}
	}

	file_loader.m_refresh_pending = false;
	return true;
}

// Parse a string
bool StringParser::Parse(const char* string, std::size_t length)
{
	m_loader.IgnoreMissingIncludes(m_linedrawer->m_user_settings.m_ignore_missing_includes);

	// Parse the script
	script::EResult load_result = m_loader.LoadFromString(string, length);
	if( Failed(load_result) )
	{
		LineDrawer::Get().m_error_output.Error((Fmt("Parse error: ") + ToString(load_result)).c_str());
		//PR_ASSERT(PR_DBG_LDR, false);
		return false;
	}

	// Remember when parsing started
	m_parse_start_time = (uint)GetTickCount();

	// Parse the data
	bool success = true;
	std::string keyword;
	while( success && m_loader.GetKeyword(keyword) )			
	{
		success = ParseCommon(keyword, 0);
	}
	LineDrawer::Get().SetProgress(0, 0, "");
	return success;
}

//*******************************************************************************************
// Private methods
//*****
// Recursively parse the data.
bool StringParser::ParseCommon(const std::string& keyword, LdrObject* parent_object)
{
	// Update the progress bar
	if( !LineDrawer::Get().SetProgress((uint)m_loader.GetPosition(), (uint)m_loader.GetDataLength(), Fmt("Parsing object: %s", keyword.c_str()).c_str(), (uint)(GetTickCount() - m_parse_start_time)) )
	{	return false; }

	// Get the type of object 'keyword' represents
	eType type = GetLDObjectType(keyword.c_str());
	if( type != tUnknown )
	{
		// If it's a known type, parse it.
		LdrObject* object = ParseObject(type);
		if( !object ) return false;

		if( parent_object )
		{
			parent_object->m_child.push_back(object);
			object->m_parent = parent_object;
		}
		else
		{
			object->m_parent = 0;
			m_store.push_back(object);
		}
		return true;
	}

	// Otherwise it might represent an object modifier for 'parent_object'
	else if( parent_object )
	{
		if( str::EqualNoCase(keyword, "Transform") )
		{
			m4x4 object_to_parent;
			if( !ParseTransform(object_to_parent) )			{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the transform for type %s",			 GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			parent_object->m_object_to_parent = object_to_parent;
			return true;
		}
		else if( str::EqualNoCase(keyword, "RandomTransform") )
		{
			v4 centre;
			float range;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for random transform in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.ExtractVector3(centre, 1.0f) )	{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the random transform for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.ExtractFloat(range) )				{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the random transform for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for random transform in type %s",		GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			parent_object->m_object_to_parent = m4x4Random(centre, range);
		}
		else if( str::EqualNoCase(keyword, "Position") )
		{
			v4 position;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for position in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.ExtractVector3(position, 1.0f) )	{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the position for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for position in type %s",		GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			parent_object->m_object_to_parent.pos = position;
			return true;
		}
		else if( str::EqualNoCase(keyword, "RandomPosition") )
		{
			v4 centre;
			float range;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for random position in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.ExtractVector3(centre, 1.0f) )	{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the random position for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.ExtractFloat(range) )				{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the random position for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for random position in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			parent_object->m_object_to_parent.pos = v4Random3(centre, range, 1.0f);
			return true;
		}
		else if( str::EqualNoCase(keyword, "Direction") )
		{
			uint axis;
			v4 direction;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for direction in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.ExtractUInt(axis, 10) )			{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the direction axis for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }			
			if( !m_loader.ExtractVector3(direction, 0.0f) )	{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the direction for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for direction in type %s",		GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			m3x3 orientation; OrientationFromDirection(orientation, direction, axis);
			cast_m3x3(parent_object->m_object_to_parent) = orientation;
			return true;
		}
		else if( str::EqualNoCase(keyword, "Orientation") )
		{
			Quat orientation;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for orientation in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.ExtractQuaternion(orientation) )	{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the orientation for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for orientation in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			m3x3 object_to_parent = m3x3::make(orientation);
			cast_m3x3(parent_object->m_object_to_parent) = object_to_parent;
			return true;
		}
		else if( str::EqualNoCase(keyword, "RandomOrientation") )
		{
			cast_m3x3(parent_object->m_object_to_parent) = m3x3Random();
			return true;
		}
		else if( str::EqualNoCase(keyword, "Euler") )
		{
			v4 euler_angles;
			if( !m_loader.FindSectionStart() )					{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for euler in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.ExtractVector3(euler_angles, 0.0f) )	{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the euler for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.FindSectionEnd() )					{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for euler in type %s",		GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			euler_angles.x = DegreesToRadians(euler_angles.x);
			euler_angles.y = DegreesToRadians(euler_angles.y);
			euler_angles.z = DegreesToRadians(euler_angles.z);
			m4x4 object_to_parent = m4x4::make(euler_angles.x, euler_angles.y, euler_angles.z, v4Origin);
			cast_m3x3(parent_object->m_object_to_parent) = cast_m3x3(object_to_parent);
			return true;
		}
		else if( str::EqualNoCase(keyword, "Scale") )
		{
			v4 scale;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for scale in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.ExtractVector3(scale, 0.0f) )		{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the scale for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for scale in type %s",		GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			parent_object->m_object_to_parent.x *= scale.x;
			parent_object->m_object_to_parent.y *= scale.y;
			parent_object->m_object_to_parent.z *= scale.z;
			return true;
		}
		else if( str::EqualNoCase(keyword, "Animation") )
		{
			AnimationData anim_data;
			if( !m_loader.FindSectionStart() )	{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for the animation data in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !ParseAnimation(anim_data) )	{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the animation data for type %s",					GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			if( !m_loader.FindSectionEnd() )	{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for the animation data in type %s",	GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			parent_object->m_animation = anim_data;
			return true;
		}
		else if( str::EqualNoCase(keyword, "Hidden") )
		{
			parent_object->SetEnable(false, true);
			return true;
		}
		else if( str::EqualNoCase(keyword, "Wireframe") )
		{
			parent_object->SetWireframe(true, true);
			return true;
		}
		else if( str::EqualNoCase(keyword, "Colour") )
		{
			Colour32 col;
			if( !m_loader.ExtractUInt(col.m_aarrggbb, 16) )	{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the colour override for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			parent_object->SetColour(col, true, false);
			return true;
		}
		else if( str::EqualNoCase(keyword, "RandomColour") )
		{
			parent_object->SetColour(Colour32RandomRGB(), true, false);
			return true;
		}
		else if( str::EqualNoCase(keyword, "ColourMask") )
		{
			Colour32 col;
			if( !m_loader.ExtractUInt(col.m_aarrggbb, 16) )	{ m_linedrawer->m_error_output.Error(Fmt("Error while reading the colour mask for type %s",				GetLDObjectTypeString(parent_object->GetSubType())).c_str()); return false; }
			parent_object->SetColour(col, true, true);
			return true;
		}
	}
	else if( str::EqualNoCase(keyword, "Camera") )
	{
		if( !ParseCamera() ) { m_linedrawer->m_error_output.Error("Failed to read Camera data"); return false; }
		return true;
	}
	else if( str::EqualNoCase(keyword, "Lock") )
	{
		if( !ParseLocks() ) { m_linedrawer->m_error_output.Error("Failed to read *Lock"); return false; }
		return true;
	}
	else if( str::EqualNoCase(keyword, "Delimiters") )
	{
		std::string delim;
		if( !m_loader.ExtractCString(delim) ) { m_linedrawer->m_error_output.Error("Error white reading delimiters"); return false; }
		m_loader.SetDelimiters(delim);
	}
	else if( str::EqualNoCase(keyword, "GlobalWireframeMode") )
	{
		if( !ParseGlobalWireframeMode() ) { m_linedrawer->m_error_output.Error("Failed to read GlobalWireframeMode data"); return false; }
		return true;
	}
	m_linedrawer->m_error_output.Error(Fmt("Unknown keyword found in source '%s'", keyword.c_str()).c_str());
	return false;
}

//*****
// Parse an object
LdrObject* StringParser::ParseObject(eType object_type)
{
	// Extract a name and colour for this object
	std::string name;
	if( !m_loader.ExtractIdentifier(name) ) { m_linedrawer->m_error_output.Error(Fmt("Type %s does not have a valid name", GetLDObjectTypeString(object_type)).c_str()); return 0; }
	uint colour_uint;
	if( !m_loader.ExtractUInt(colour_uint, 16) ) { m_linedrawer->m_error_output.Error(Fmt("Type %s does not have a valid colour", GetLDObjectTypeString(object_type)).c_str()); return 0; }
	Colour32 colour; colour = colour_uint;

	LdrObject* object = 0;
	if( !m_loader.FindSectionStart() ) { m_linedrawer->m_error_output.Error(Fmt("Unable to find the section for type %s", GetLDObjectTypeString(object_type)).c_str()); return 0; }
	std::string section = m_loader.CopySection();
	switch( object_type )
	{
		#define DECLARE_OBJECT(subtype, type) case t##subtype: \
		{ \
			object = new T##type(object_type, name, colour, section); \
			if( !Parse##subtype(static_cast<T##type*>(object)) ) { delete object; object = 0; return 0; } \
		}break;
		DECLARE_OBJECT(Point,			Point);
		DECLARE_OBJECT(Line,			Line);
		DECLARE_OBJECT(LineD,			Line);
		DECLARE_OBJECT(LineNL,			Line);
		DECLARE_OBJECT(LineList,		Line);
		DECLARE_OBJECT(Rectangle,		Line);
		DECLARE_OBJECT(RectangleLU,		Line);
		DECLARE_OBJECT(RectangleWHZ,	Line);
		DECLARE_OBJECT(CircleR,			Line);
		DECLARE_OBJECT(CircleRxRyZ,		Line);
		DECLARE_OBJECT(Triangle,		Triangle);
		DECLARE_OBJECT(Quad,			Quad);
		DECLARE_OBJECT(QuadLU,			Quad);
		DECLARE_OBJECT(QuadWHZ,			Quad);
		DECLARE_OBJECT(BoxLU,			Box);
		DECLARE_OBJECT(BoxWHD,			Box);
		DECLARE_OBJECT(BoxList,			Box);
		DECLARE_OBJECT(CylinderHR,		Cylinder);
		DECLARE_OBJECT(CylinderHRxRy,	Cylinder);
		DECLARE_OBJECT(SphereR,			Sphere);
		DECLARE_OBJECT(SphereRxRyRz,	Sphere);
		DECLARE_OBJECT(Polytope,		Polytope);
		DECLARE_OBJECT(FrustumWHNF,		Frustum);
		DECLARE_OBJECT(FrustumATNF,		Frustum);
		DECLARE_OBJECT(GridWH,			Grid);
		DECLARE_OBJECT(SurfaceWHD,		Surface);
		DECLARE_OBJECT(Matrix3x3,		Matrix);
		DECLARE_OBJECT(Matrix4x4,		Matrix);
		DECLARE_OBJECT(Mesh,			Mesh);
		DECLARE_OBJECT(File,			File);
		DECLARE_OBJECT(Group,			Group);
		DECLARE_OBJECT(GroupCyclic,		GroupCyclic);
		#undef DECLARE_OBJECT
	};
	if( !m_loader.FindSectionEnd() ) { m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for type %s", GetLDObjectTypeString(object_type)).c_str()); delete object; return 0; }
	return object;
}

// Read a camera view
bool StringParser::ParseCamera()
{
	if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error("Unable to find the section start for camera"); return false; }
	
	// Set defaults
	m_view_mask.reset();
	m_view = CameraView();
	m_view.SetAspect(m_linedrawer->GetClientArea());

	std::string keyword;
	while( m_loader.GetKeyword(keyword) )
	{
		if( str::EqualNoCase(keyword, "Position") )
		{
			if( !m_loader.FindSectionStart() )								{ m_linedrawer->m_error_output.Error("Section start missing from *Position in *Camera"); return false; }
			if( !m_loader.ExtractVector3(m_view.m_camera_position, 1.0f) )	{ m_linedrawer->m_error_output.Error("Error while reading camera position"); return false; }
			if( !m_loader.FindSectionEnd() )								{ m_linedrawer->m_error_output.Error("Section end missing from *Position in *Camera"); return false; }
			m_view_mask[ViewMask::PositionX] = true;
			m_view_mask[ViewMask::PositionY] = true;
			m_view_mask[ViewMask::PositionZ] = true;
		}
		else if( str::EqualNoCase(keyword, "Up") )
		{
			if( !m_loader.FindSectionStart() )								{ m_linedrawer->m_error_output.Error("Section start missing from *Up in *Camera"); return false; }
			if( !m_loader.ExtractVector3(m_view.m_camera_up, 0.0f) )		{ m_linedrawer->m_error_output.Error("Error while reading camera up direction"); return false; }
			if( !m_loader.FindSectionEnd() )								{ m_linedrawer->m_error_output.Error("Section end missing from *Up in *Camera"); return false; }
			m_view_mask[ViewMask::UpX] = true;
			m_view_mask[ViewMask::UpY] = true;
			m_view_mask[ViewMask::UpZ] = true;
		}
		else if( str::EqualNoCase(keyword, "PositionX") )
		{
			if( !m_loader.ExtractFloat(m_view.m_camera_position.x) )		{ m_linedrawer->m_error_output.Error("Error while reading camera position x"); return false; }
			m_view_mask[ViewMask::PositionX] = true;
		}
		else if( str::EqualNoCase(keyword, "PositionY") )
		{
			if( !m_loader.ExtractFloat(m_view.m_camera_position.y) )		{ m_linedrawer->m_error_output.Error("Error while reading camera position y"); return false; }
			m_view_mask[ViewMask::PositionY] = true;
		}
		else if( str::EqualNoCase(keyword, "PositionZ") )
		{
			if( !m_loader.ExtractFloat(m_view.m_camera_position.z) )		{ m_linedrawer->m_error_output.Error("Error while reading camera position z"); return false; }
			m_view_mask[ViewMask::PositionZ] = true;
		}
		else if( str::EqualNoCase(keyword, "LookAt") )
		{
			if( !m_loader.FindSectionStart() )								{ m_linedrawer->m_error_output.Error("Section start missing from *LookAt in *Camera"); return false; }
			if( !m_loader.ExtractVector3(m_view.m_lookat_centre, 1.0f) )	{ m_linedrawer->m_error_output.Error("Error while reading camera look at position"); return false; }
			if( !m_loader.FindSectionEnd() )								{ m_linedrawer->m_error_output.Error("Section end missing from *LookAt in *Camera"); return false; }
			m_view_mask[ViewMask::LookAt] = true;
		}
		else if( str::EqualNoCase(keyword, "FOV") )
		{
			if( !m_loader.ExtractFloat(m_view.m_fov) )						{ m_linedrawer->m_error_output.Error("Error while reading camera field of view"); return false; }
			m_view_mask[ViewMask::FOV] = true;
		}
		else if( str::EqualNoCase(keyword, "Aspect") )
		{
			if( !m_loader.ExtractFloat(m_view.m_aspect) )					{ m_linedrawer->m_error_output.Error("Error while reading camera aspect ratio"); return false; }
			m_view_mask[ViewMask::Aspect] = true;
		}
		else if( str::EqualNoCase(keyword, "Near") )
		{
			if( !m_loader.ExtractFloat(m_view.m_near) )						{ m_linedrawer->m_error_output.Error("Error while reading camera near clip plane"); return false; }
			m_view_mask[ViewMask::Near] = true;
		}
		else if( str::EqualNoCase(keyword, "Far") )
		{
			if( !m_loader.ExtractFloat(m_view.m_far) )						{ m_linedrawer->m_error_output.Error("Error while reading camera far clip plane"); return false; }
			m_view_mask[ViewMask::Far] = true;
		}
		else if( str::EqualNoCase(keyword, "AlignX") )
		{
			m_view_mask[ViewMask::AlignX] = true;
		}
		else if( str::EqualNoCase(keyword, "AlignY") )
		{
			m_view_mask[ViewMask::AlignY] = true;
		}
		else if( str::EqualNoCase(keyword, "AlignZ") )
		{
			m_view_mask[ViewMask::AlignZ] = true;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Unknown keyword '%s' given in camera", keyword.c_str()).c_str());
			return false;
		}
	}

	if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error("Unable to find the section end for camera"); return false; }
	return true;
}

// Read Lock settings
bool StringParser::ParseLocks()
{
	if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error("Unable to find the section start for axis Lock"); return false; }
	
	// Set defaults
	m_locks.reset();
	std::string keyword;
	while( m_loader.GetKeyword(keyword) )
	{
		if     ( str::EqualNoCase(keyword, "TransX") )			{ m_locks[LockMask::TransX] = true; }
		else if( str::EqualNoCase(keyword, "TransY") )			{ m_locks[LockMask::TransY] = true; }
		else if( str::EqualNoCase(keyword, "TransZ") )			{ m_locks[LockMask::TransZ] = true; }
		else if( str::EqualNoCase(keyword, "RotX") )			{ m_locks[LockMask::RotX] = true; }
		else if( str::EqualNoCase(keyword, "RotY") )			{ m_locks[LockMask::RotY] = true; }
		else if( str::EqualNoCase(keyword, "RotZ") )			{ m_locks[LockMask::RotZ] = true; }
		else if( str::EqualNoCase(keyword, "Zoom") )			{ m_locks[LockMask::Zoom] = true; }
		else if( str::EqualNoCase(keyword, "CameraRelative") )	{ m_locks[LockMask::CameraRelative] = true; }
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Unknown keyword '%s' found in *Lock", keyword.c_str()).c_str());
			return false;
		}
	}

	if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error("Unable to find the section end for *Lock"); return false; }
	return true;
}

// Read a setting for the global wireframe mode
bool StringParser::ParseGlobalWireframeMode()
{
	if( !m_loader.ExtractInt(m_global_wireframe_mode, 10) )	{ m_linedrawer->m_error_output.Error("Error while reading global wireframe mode"); return false; }
	if( m_global_wireframe_mode < -1 || m_global_wireframe_mode > 2 ) 
	{
		m_linedrawer->m_error_output.Error("Invalid global wireframe mode");
		m_global_wireframe_mode = -1;
		return false;
	}
	return true;
}

//*****
// Read a transform
bool StringParser::ParseTransform(m4x4& transform)
{
	if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error("Unable to find the section start for tranform"); return false; }
	if( !m_loader.Extractm4x4(transform) )			{ m_linedrawer->m_error_output.Error("Error while reading the transform data"); return false; }
	
	std::string keyword;
	while( m_loader.GetKeyword(keyword) )
	{
		if( str::EqualNoCase(keyword, "Transpose") )
		{
			Transpose4x4(transform);
		}
		else if( str::EqualNoCase(keyword, "Inverse") )
		{
			Inverse(transform);
		}
		else if( str::EqualNoCase(keyword, "Orthonormalise") )
		{
			Orthonormalise(transform);
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Unknown operation '%s' specified in transform", keyword.c_str()).c_str());
			return false;
		}
	}

	if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error("Unable to find the section end for tranform"); return false; }
	return true;
}

//*****
// Read a list of points
bool StringParser::ParsePoint(TPoint* point)
{
	v4 pt;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(pt, 1.0f) )
		{
			point->m_point.push_back(pt);
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, point) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in Point '%s'", point->m_name.c_str()).c_str());
			return false;
		}
	}
	if( point->m_point.empty() ) return true;
	point->CreateRenderObject();
	return true;
}

// Parse the common elements of a line
bool StringParser::ParseLineCommon(TLine* line, bool& normalise)
{
	std::string keyword;
	if( m_loader.GetKeyword(keyword) )
	{
		if( str::EqualNoCase(keyword, "Normalise") )
		{
			normalise = true;
		}
		else if( str::EqualNoCase(keyword, "Parametric") )
		{
			float t0 = 0.0f, t1 = 1.0f;
			m_loader.ExtractFloat(t0);
			m_loader.ExtractFloat(t1);
			if( line->m_point.size() < 2 )
			{
				m_linedrawer->m_error_output.Error(Fmt("Syntax error found in line '%s'. *Parametric applies to the previous line only", line->m_name.c_str()).c_str());
				return false;
			}
			v4& a = line->m_point[line->m_point.size() - 2];
			v4& b = line->m_point[line->m_point.size() - 1];
			v4 dir = b - a;
			b = a + t1 * dir;
			a = a + t0 * dir;
		}
		else if( !ParseCommon(keyword, line) ) return false;
	}
	return true;
}

// Read a list of lines
bool StringParser::ParseLine(TLine* line)
{
	v4 start, end;
	bool normalise = false;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(start, 1.0f) && m_loader.ExtractVector3(end, 1.0f) )
		{
			line->m_point.push_back(start);
			line->m_point.push_back(end);
		}
		else if( m_loader.IsKeyword() )
		{
			if( !ParseLineCommon(line, normalise) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in Line '%s'", line->m_name.c_str()).c_str());
			return false;
		}
	}
	if( line->m_point.empty() ) return true;
	if( normalise )
	{
		for( TPointVec::iterator p = line->m_point.begin(), p_end = line->m_point.end(); p != p_end; p += 2 )
		{	*(p+1) = GetNormal3(*(p+1) - *p); }
	}
	line->CreateRenderObject();
	return true;
}

//*****
// Read a list of lines
bool StringParser::ParseLineD(TLine* line)
{
	v4 start, direction;
	bool normalise = false;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(start, 1.0f) && m_loader.ExtractVector3(direction, 0.0f) )
		{
			line->m_point.push_back(start);
			line->m_point.push_back(start + direction);
		}
		else if( m_loader.IsKeyword() )
		{
			if( !ParseLineCommon(line, normalise) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in LineD '%s'", line->m_name.c_str()).c_str());
			return false;
		}
	}
	if( line->m_point.empty() ) return true;
	if( normalise )
	{
		for( TPointVec::iterator p = line->m_point.begin(), p_end = line->m_point.end(); p != p_end; p += 2 )
		{	*(p+1) = GetNormal3(*(p+1) - *p); }
	}
	line->CreateRenderObject();
	return true;
}

// Read a list of lines
bool StringParser::ParseLineNL(TLine* line)
{
	float length;
	v4 start, normal;
	bool normalise = false;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(start, 1.0f) && m_loader.ExtractVector3(normal, 0.0f) && m_loader.ExtractFloat(length) )
		{
			line->m_point.push_back(start);
			line->m_point.push_back(start + normal * length);
		}
		else if( m_loader.IsKeyword() )
		{
			if( !ParseLineCommon(line, normalise) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in LineNL '%s'", line->m_name.c_str()).c_str());
			return false;
		}
	}

	if( line->m_point.empty() ) return true;
	if( normalise )
	{
		for( TPointVec::iterator p = line->m_point.begin(), p_end = line->m_point.end(); p != p_end; p += 2 )
		{	*(p+1) = GetNormal3(*(p+1) - *p); }
	}
	line->CreateRenderObject();
	return true;
}

// Read a list of points as a connected line list
bool StringParser::ParseLineList(TLine* line)
{
	v4 pt;
	bool normalise = false;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(pt, 1.0f) )
		{
			if( !line->m_point.empty() ) line->m_point.push_back(pt);
			line->m_point.push_back(pt);
		}
		else if( m_loader.IsKeyword() )
		{
			if( !ParseLineCommon(line, normalise) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in LineList '%s'", line->m_name.c_str()).c_str());
			return false;
		}
	}

	if( line->m_point.empty() ) return true;
	line->m_point.pop_back();
	//line->m_point.push_back(line->m_point.front()); ?? 
	if( normalise )
	{
		for( TPointVec::iterator p = line->m_point.begin(), p_end = line->m_point.end(); p != p_end; p += 2 )
		{	*(p+1) = GetNormal3(*(p+1) - *p); }
	}
	line->CreateRenderObject();
	return true;
}

// Read a list of rectangles
bool StringParser::ParseRectangle(TLine* line)
{
	v4 pt[4];
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(pt[0], 1.0f) &&
			m_loader.ExtractVector3(pt[1], 1.0f) &&
			m_loader.ExtractVector3(pt[2], 1.0f) &&
			m_loader.ExtractVector3(pt[3], 1.0f) )
		{
			line->m_point.push_back(pt[0]);
			line->m_point.push_back(pt[1]);
			line->m_point.push_back(pt[1]);
			line->m_point.push_back(pt[2]);
			line->m_point.push_back(pt[2]);
			line->m_point.push_back(pt[3]);
			line->m_point.push_back(pt[3]);
			line->m_point.push_back(pt[0]);
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, line) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in Rectangle '%s'", line->m_name.c_str()).c_str());
			return false;
		}
	}
	if( line->m_point.empty() ) return true;
	line->CreateRenderObject();
	return true;
}

// Read a list of rectangles.
bool StringParser::ParseRectangleLU(TLine* line)
{
	v4 lower, upper;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(lower, 1.0f) && m_loader.ExtractVector3(upper, 1.0f) )
		{
			line->m_point.push_back(lower);
			line->m_point.push_back(v4::make(upper.x, lower.y, upper.z, 1.0f));
			line->m_point.push_back(v4::make(upper.x, lower.y, upper.z, 1.0f));
			line->m_point.push_back(upper);
			line->m_point.push_back(upper);
			line->m_point.push_back(v4::make(lower.x, upper.y, lower.z, 1.0f));
			line->m_point.push_back(v4::make(lower.x, upper.y, lower.z, 1.0f));
			line->m_point.push_back(lower);
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, line) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in RectangleLU '%s'", line->m_name.c_str()).c_str());
			return false;
		}
	}
	if( line->m_point.empty() ) return true;
	line->CreateRenderObject();
	return true;
}

// Read a list of rectangles.
bool StringParser::ParseRectangleWHZ(TLine* line)
{
	v4 dim;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(dim, 1.0f) )
		{	
			line->m_point.push_back(v4::make(-dim.x, -dim.y,  dim.z, 1.0f));
			line->m_point.push_back(v4::make(-dim.x,  dim.y,  dim.z, 1.0f));
			line->m_point.push_back(v4::make(-dim.x,  dim.y,  dim.z, 1.0f));
			line->m_point.push_back(v4::make( dim.x,  dim.y,  dim.z, 1.0f));
			line->m_point.push_back(v4::make( dim.x,  dim.y,  dim.z, 1.0f));
			line->m_point.push_back(v4::make( dim.x, -dim.y,  dim.z, 1.0f));
			line->m_point.push_back(v4::make( dim.x, -dim.y,  dim.z, 1.0f));
			line->m_point.push_back(v4::make(-dim.x, -dim.y,  dim.z, 1.0f));
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, line) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in RectangleWHZ '%s'", line->m_name.c_str()).c_str());
			return false;
		}
	}
	if( line->m_point.empty() ) return true;
	line->CreateRenderObject();
	return true;
}

// Read a list of circles.
bool StringParser::ParseCircleR(TLine* line)
{
	v4 pt;
	float radius;
	uint divisions = 50;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractFloat(radius) )
		{
			float da = 2.0f * maths::pi / divisions;
			for( uint t = 0; t != divisions; ++t )
			{
				pt.set(Cos(t * da) * radius, Sin(t * da) * radius, 0.0f, 1.0f);
				if( !line->m_point.empty() ) { line->m_point.push_back(pt); }
				line->m_point.push_back(pt);
			}
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "Divisions") )
			{
				if( !m_loader.ExtractUInt(divisions, 10) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read divisions for CircleR '%s'", line->m_name.c_str()).c_str()); }
			}
			else if( !ParseCommon(keyword, line) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in CircleR '%s'", line->m_name.c_str()).c_str());
			return false;
		}
	}
	if( line->m_point.empty() ) return true;
	line->m_point.push_back(line->m_point.front());
	line->CreateRenderObject();
	return true;
}

// Read a list of circles.
bool StringParser::ParseCircleRxRyZ(TLine* line)
{
	v4 pt, radius;
	uint divisions = 50;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(radius, 1.0f) )
		{
			float da = 2.0f * maths::pi / divisions;
			for( uint t = 0; t != divisions; ++t )
			{
				pt.set(Cos(t * da) * radius.x, Sin(t * da) * radius.y, radius.z, 1.0f);
				if( !line->m_point.empty() )	{ line->m_point.push_back(pt); }
				line->m_point.push_back(pt);
			}
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "Divisions") )
			{
				if( !m_loader.ExtractUInt(divisions, 10) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read divisions for CircleR '%s'", line->m_name.c_str()).c_str()); }
			}
			else if( !ParseCommon(keyword, line) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in CircleRxRyZ '%s'", line->m_name.c_str()).c_str());
			return false;
		}
	}
	if( line->m_point.empty() ) return true;
	line->m_point.push_back(line->m_point.front());
	line->CreateRenderObject();
	return true;
}

// Read a list of triangles.
bool StringParser::ParseTriangle(TTriangle* tri)
{
	v4 point[3];
	uint vertex_colour[3];
	bool vertex_colours = false;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( !vertex_colours &&
			m_loader.ExtractVector3(point[0], 1.0f) &&
			m_loader.ExtractVector3(point[1], 1.0f) &&
			m_loader.ExtractVector3(point[2], 1.0f) )
		{
			tri->m_point.push_back(point[0]);
			tri->m_point.push_back(point[1]);
			tri->m_point.push_back(point[2]);
		}
		else if( vertex_colours &&
			m_loader.ExtractVector3(point[0], 1.0f) && m_loader.ExtractUInt(vertex_colour[0], 16) &&
			m_loader.ExtractVector3(point[1], 1.0f) && m_loader.ExtractUInt(vertex_colour[1], 16) &&
			m_loader.ExtractVector3(point[2], 1.0f) && m_loader.ExtractUInt(vertex_colour[2], 16) )
		{
			tri->m_point.push_back(point[0]); tri->m_vertex_colour.push_back(Colour32::make(vertex_colour[0]));
			tri->m_point.push_back(point[1]); tri->m_vertex_colour.push_back(Colour32::make(vertex_colour[1]));
			tri->m_point.push_back(point[2]); tri->m_vertex_colour.push_back(Colour32::make(vertex_colour[2]));
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "VertexColoured") )
			{
				vertex_colours = true;
			}
			else if( !ParseCommon(keyword, tri) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in Triangle '%s'", tri->m_name.c_str()).c_str());
			return false;
		}
	}
	if( tri->m_point.empty() ) return true;
	tri->CreateRenderObject();
	return true;
}

//*****
// Read a list of quads.
bool StringParser::ParseQuad(TQuad* quad)
{
	v4 point[4];
	uint vertex_colour[4];
	bool vertex_colours = false;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( !vertex_colours &&
			m_loader.ExtractVector3(point[0], 1.0f) &&
			m_loader.ExtractVector3(point[1], 1.0f) &&
			m_loader.ExtractVector3(point[2], 1.0f) &&
			m_loader.ExtractVector3(point[3], 1.0f) )
		{
			quad->m_point.push_back(point[0]);
			quad->m_point.push_back(point[1]);
			quad->m_point.push_back(point[2]);
			quad->m_point.push_back(point[3]);
		}
		else if( vertex_colours &&
			m_loader.ExtractVector3(point[0], 1.0f) && m_loader.ExtractUInt(vertex_colour[0], 16) &&
			m_loader.ExtractVector3(point[1], 1.0f) && m_loader.ExtractUInt(vertex_colour[1], 16) &&
			m_loader.ExtractVector3(point[2], 1.0f) && m_loader.ExtractUInt(vertex_colour[2], 16) &&
			m_loader.ExtractVector3(point[3], 1.0f) && m_loader.ExtractUInt(vertex_colour[3], 16) )
		{
			quad->m_point.push_back(point[0]);	quad->m_vertex_colour.push_back(Colour32::make(vertex_colour[0]));
			quad->m_point.push_back(point[1]);	quad->m_vertex_colour.push_back(Colour32::make(vertex_colour[1]));
			quad->m_point.push_back(point[2]);	quad->m_vertex_colour.push_back(Colour32::make(vertex_colour[2]));
			quad->m_point.push_back(point[3]);	quad->m_vertex_colour.push_back(Colour32::make(vertex_colour[3]));
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "VertexColoured") )
			{
				vertex_colours = true;
			}
			else if( str::EqualNoCase(keyword, "Texture") )
			{
				if( !m_loader.ExtractString(quad->m_texture) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read texture for quad '%s'", quad->m_name.c_str()).c_str()); }
			}
			else if( !ParseCommon(keyword, quad) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in Quad '%s'", quad->m_name.c_str()).c_str());
			return false;
		}
	}
	if( quad->m_point.empty() ) return true;
	quad->CreateRenderObject();
	return true;
}

//*****
// Read a list of quads.
bool StringParser::ParseQuadLU(TQuad* quad)
{
	v4 lower, upper;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(lower, 1.0f) && m_loader.ExtractVector3(upper, 1.0) )
		{
			quad->m_point.push_back(lower);
			quad->m_point.push_back(v4::make(lower.x, upper.y, upper.z, 1.0f));
			quad->m_point.push_back(upper);
			quad->m_point.push_back(v4::make(upper.x, lower.y, lower.z, 1.0f));
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "Texture") )
			{
				if( !m_loader.ExtractString(quad->m_texture) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read texture for quad '%s'", quad->m_name.c_str()).c_str()); }
			}
			else if( !ParseCommon(keyword, quad) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in QuadLU '%s'", quad->m_name.c_str()).c_str());
			return false;
		}
	}
	if( quad->m_point.empty() ) return true;
	quad->CreateRenderObject();
	return true;
}

//*****
// Read a list of quads.
bool StringParser::ParseQuadWHZ(TQuad* quad)
{
	v4 dim;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(dim, 1.0f) )
		{
			quad->m_point.push_back(v4::make(-dim.x, -dim.y,  dim.z, 1.0f));
			quad->m_point.push_back(v4::make(-dim.x,  dim.y,  dim.z, 1.0f));
			quad->m_point.push_back(v4::make( dim.x,  dim.y,  dim.z, 1.0f));
			quad->m_point.push_back(v4::make( dim.x, -dim.y,  dim.z, 1.0f));
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "Texture") )
			{
				if( !m_loader.ExtractString(quad->m_texture) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read texture for quad '%s'", quad->m_name.c_str()).c_str()); }
			}
			else if( !ParseCommon(keyword, quad) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in QuadWHZ '%s'", quad->m_name.c_str()).c_str());
			return false;
		}
	}
	if( quad->m_point.empty() ) return true;
	quad->CreateRenderObject();
	return true;
}

//*****
// Read a list of boxs.
bool StringParser::ParseBoxLU(TBox* box)
{
	v4 lower, upper;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(lower, 1.0f) && m_loader.ExtractVector3(upper, 1.0f) )
		{
			box->m_point.push_back(lower);
			box->m_point.push_back(v4::make(lower.x, upper.y, lower.z, 1.0f));
			box->m_point.push_back(v4::make(upper.x, lower.y, lower.z, 1.0f));
			box->m_point.push_back(v4::make(upper.x, upper.y, lower.z, 1.0f));
			box->m_point.push_back(v4::make(upper.x, lower.y, upper.z, 1.0f));
			box->m_point.push_back(upper);
			box->m_point.push_back(v4::make(lower.x, lower.y, upper.z, 1.0f));
			box->m_point.push_back(v4::make(lower.x, upper.y, upper.z, 1.0f));
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, box) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in BoxLU '%s'", box->m_name.c_str()).c_str());
			return false;
		}
	}
	if( box->m_point.empty() ) return true;
	box->CreateRenderObject();
	return true;
}

//*****
// Read a list of boxes.
bool StringParser::ParseBoxWHD(TBox* box)
{
	v4 dim;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(dim, 0.0f) )
		{
			dim /= 2.0f;
			box->m_point.push_back(v4::make(-dim.x, -dim.y, -dim.z, 1.0f));
			box->m_point.push_back(v4::make(-dim.x,  dim.y, -dim.z, 1.0f));
			box->m_point.push_back(v4::make( dim.x, -dim.y, -dim.z, 1.0f));
			box->m_point.push_back(v4::make( dim.x,  dim.y, -dim.z, 1.0f));
			box->m_point.push_back(v4::make( dim.x, -dim.y,  dim.z, 1.0f));
			box->m_point.push_back(v4::make( dim.x,  dim.y,  dim.z, 1.0f));
			box->m_point.push_back(v4::make(-dim.x, -dim.y,  dim.z, 1.0f));
			box->m_point.push_back(v4::make(-dim.x,  dim.y,  dim.z, 1.0f));
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, box) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in BoxWHD '%s'", box->m_name.c_str()).c_str());
			return false;
		}
	}
	if( box->m_point.empty() ) return true;
	box->CreateRenderObject();
	return true;
}

//*****
// Read a list of square boxes.
bool StringParser::ParseBoxList(TBox* box)
{
	v4 pos;
	float size = 0.01f;
	bool constant_size = false;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( (constant_size || m_loader.ExtractFloat(size)) && m_loader.ExtractVector3(pos, 1.0f) )
		{
			box->m_point.push_back(v4::make(pos.x - size, pos.y - size, pos.z - size, 1.0f));
			box->m_point.push_back(v4::make(pos.x - size, pos.y + size, pos.z - size, 1.0f));
			box->m_point.push_back(v4::make(pos.x + size, pos.y - size, pos.z - size, 1.0f));
			box->m_point.push_back(v4::make(pos.x + size, pos.y + size, pos.z - size, 1.0f));
			box->m_point.push_back(v4::make(pos.x + size, pos.y - size, pos.z + size, 1.0f));
			box->m_point.push_back(v4::make(pos.x + size, pos.y + size, pos.z + size, 1.0f));
			box->m_point.push_back(v4::make(pos.x - size, pos.y - size, pos.z + size, 1.0f));
			box->m_point.push_back(v4::make(pos.x - size, pos.y + size, pos.z + size, 1.0f));
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "Size") )
			{
				if( !m_loader.ExtractFloat(size) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read 'Size' for BoxList '%s'", box->m_name.c_str()).c_str()); }
				constant_size = true;
			}
			else if( !ParseCommon(keyword, box) ) return false;
			
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in BoxList '%s'", box->m_name.c_str()).c_str());
			return false;
		}
	}
	if( box->m_point.empty() ) return true;
	box->CreateRenderObject();
	return true;
}

//*****
// Read a list of cylinders
bool StringParser::ParseCylinderHR(TCylinder* cylinder)
{
	v4 dim;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractFloat(dim.x) && m_loader.ExtractFloat(dim.y) )
		{
			dim.z = dim.y;
			dim.w = 1.0f;
			cylinder->m_point.push_back(dim);
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, cylinder) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in CylinderHR '%s'", cylinder->m_name.c_str()).c_str());
			return false;
		}
	}
	if( cylinder->m_point.empty() ) return true;
	cylinder->CreateRenderObject();
	return true;
}

//*****
// Read a list of cylinders.
bool StringParser::ParseCylinderHRxRy(TCylinder* cylinder)
{
	v4 dim;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(dim, 1.0f) )
		{
			cylinder->m_point.push_back(dim);
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, cylinder) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in CylinderHRxRy '%s'", cylinder->m_name.c_str()).c_str());
			return false;
		}
	}
	if( cylinder->m_point.empty() ) return true;
	cylinder->CreateRenderObject();
	return true;
}

//*****
// Read a list of spheres.
bool StringParser::ParseSphereR(TSphere* sphere)
{
	v4 dim;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractFloat(dim.x) )
		{
			dim.y = dim.x;
			dim.z = dim.x;
			dim.w = 1.0f;
			sphere->m_point.push_back(dim);
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "Divisions") )
			{
				if( !m_loader.ExtractUInt(sphere->m_divisions, 10) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read divisions for sphere '%s'", sphere->m_name.c_str()).c_str()); }
			}
			else if( str::EqualNoCase(keyword, "Texture") )
			{
				if( !m_loader.ExtractString(sphere->m_texture) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read texture for sphere '%s'", sphere->m_name.c_str()).c_str()); }
			}
			else if( !ParseCommon(keyword, sphere) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in SphereR '%s'", sphere->m_name.c_str()).c_str());
			return false;
		}
	}
	if( sphere->m_point.empty() ) return true;
	sphere->CreateRenderObject();
	return true;
}

//*****
// Read a list of spheres.
bool StringParser::ParseSphereRxRyRz(TSphere* sphere)
{
	v4 dim;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(dim, 1.0f) )
		{
			sphere->m_point.push_back(dim);
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "Divisions") )
			{
				if( !m_loader.ExtractUInt(sphere->m_divisions, 10) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read divisions for sphere '%s'", sphere->m_name.c_str()).c_str()); }
			}
			else if( str::EqualNoCase(keyword, "Texture") )
			{
				if( !m_loader.ExtractString(sphere->m_texture) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read texture for sphere '%s'", sphere->m_name.c_str()).c_str()); }
			}
			else if( !ParseCommon(keyword, sphere) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in SphereRxRyRz '%s'", sphere->m_name.c_str()).c_str());
			return false;
		}
	}
	if( sphere->m_point.empty() ) return true;
	sphere->CreateRenderObject();
	return true;
}

//*****
// Read a polytope
bool StringParser::ParsePolytope(TPolytope* polytope)
{
	v4 point;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(point, 1.0f) )
		{
			polytope->m_point.push_back(point);
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, polytope) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in Polytope '%s'", polytope->m_name.c_str()).c_str());
			return false;
		}
	}
	if( polytope->m_point.empty() ) return true;
	polytope->CreateRenderObject();
	return true;
}

//*****
// Read a frustum.
bool StringParser::ParseFrustumWHNF(TFrustum* frustum)
{
	float width, height, Near, Far;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractFloat(width) &&
			m_loader.ExtractFloat(height) &&
			m_loader.ExtractFloat(Near) &&
			m_loader.ExtractFloat(Far) )
		{
			width /= 2.0f;
			height /= 2.0f;
			float Width = width * Far / Near;
			float Height = height * Far / Near;

			frustum->m_point.push_back(v4::make(-width, -height, Near, 1.0f));
			frustum->m_point.push_back(v4::make(-width,  height, Near, 1.0f));
			frustum->m_point.push_back(v4::make( width, -height, Near, 1.0f));
			frustum->m_point.push_back(v4::make( width,  height, Near, 1.0f));
			frustum->m_point.push_back(v4::make( Width, -Height, Far,  1.0f));
			frustum->m_point.push_back(v4::make( Width,  Height, Far,  1.0f));
			frustum->m_point.push_back(v4::make(-Width, -Height, Far,  1.0f));
			frustum->m_point.push_back(v4::make(-Width,  Height, Far,  1.0f));
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, frustum) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in FrustumWHNF '%s'", frustum->m_name.c_str()).c_str());
			return false;
		}
	}
	if( frustum->m_point.empty() ) return true;
	frustum->CreateRenderObject();
	return true;
}

//*****
// Read a frustum.
bool StringParser::ParseFrustumATNF(TFrustum* frustum)
{
	float alpha, theta, Near, Far;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractFloat(alpha) &&
			m_loader.ExtractFloat(theta) &&
			m_loader.ExtractFloat(Near) &&
			m_loader.ExtractFloat(Far) )
		{
			float width		= 2.0f * Near * Tan(DegreesToRadians(alpha / 2.0f));
			float height	= 2.0f * Near * Tan(DegreesToRadians(theta / 2.0f));
			float Width		= width  * Far / Near;
			float Height	= height * Far / Near; 

			frustum->m_point.push_back(v4::make(-width, -height, Near, 1.0f));
			frustum->m_point.push_back(v4::make(-width,  height, Near, 1.0f));
			frustum->m_point.push_back(v4::make( width, -height, Near, 1.0f));
			frustum->m_point.push_back(v4::make( width,  height, Near, 1.0f));
			frustum->m_point.push_back(v4::make( Width, -Height, Far,  1.0f));
			frustum->m_point.push_back(v4::make( Width,  Height, Far,  1.0f));
			frustum->m_point.push_back(v4::make(-Width, -Height, Far,  1.0f));
			frustum->m_point.push_back(v4::make(-Width,  Height, Far,  1.0f));
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, frustum) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in FrustumATNF '%s'", frustum->m_name.c_str()).c_str());
			return false;
		}
	}
	if( frustum->m_point.empty() ) return true;
	frustum->CreateRenderObject();
	return true;
}

//*****
// Read a grid.
bool StringParser::ParseGridWH(TGrid* grid)
{
	float width, height;
	uint div_w, div_h;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractFloat(width) && m_loader.ExtractFloat(height) &&
			m_loader.ExtractUInt(div_w, 10) && m_loader.ExtractUInt(div_h, 10) )
		{	
			grid->m_point.push_back(v4::make(div_w + 1.0f, div_h + 1.0f, 0.0f, 1.0f));
			
			for( uint h = 0; h <= div_h; ++h )
			{
				for( uint w = 0; w <= div_w; ++w )
				{
					grid->m_point.push_back(v4::make(w * width / float(div_w), h * height / float(div_h), 0.0f, 1.0f));
				}
			}			
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, grid) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in GridWH '%s'", grid->m_name.c_str()).c_str());
			return false;
		}
	}
	if( grid->m_point.empty() ) return true;
	grid->CreateRenderObject();
	return true;
}
	
//*****
// Read a grid.
bool StringParser::ParseSurfaceWHD(TSurface* surface)
{
	uint width, height;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractUInt(width, 10) && m_loader.ExtractUInt(height, 10) )
		{	
			surface->m_point.push_back(v4::make((float)width, (float)height, 0.0f, 1.0f));
			v4 point;
			for( uint h = 0; h < height; ++h )
			{
				for( uint w = 0; w < width; ++w )
				{
					if( !m_loader.ExtractVector3(point, 1.0f) ) { m_linedrawer->m_error_output.Error(Fmt("Insufficient data for SurfaceWHD '%s'", surface->m_name.c_str()).c_str()); return false; }
					surface->m_point.push_back(point);
				}
			}			
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, surface) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in SurfaceWHD '%s'", surface->m_name.c_str()).c_str());
			return false;
		}
	}
	if( surface->m_point.empty() ) return true;
	surface->CreateRenderObject();
	return true;
}

//*****
// Read a 3x3 matrix
bool StringParser::ParseMatrix3x3(TMatrix* matrix)
{
	v4 x_axis, y_axis, z_axis;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector3(x_axis, 0.0f) &&
			m_loader.ExtractVector3(y_axis, 0.0f) &&
			m_loader.ExtractVector3(z_axis, 0.0f) )
		{
			matrix->m_point.push_back(x_axis);
			matrix->m_point.push_back(y_axis);
			matrix->m_point.push_back(z_axis);
			matrix->m_point.push_back(v4Origin);
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "Transpose") )
			{
				if( matrix->m_point.size() >= 4 )
				{
					m3x3 mat;
					matrix->m_point.pop_back();
					mat.z = matrix->m_point.back();	matrix->m_point.pop_back();
					mat.y = matrix->m_point.back();	matrix->m_point.pop_back();
					mat.x = matrix->m_point.back();	matrix->m_point.pop_back();
					Transpose(mat);
					matrix->m_point.push_back(mat.x);
					matrix->m_point.push_back(mat.y);
					matrix->m_point.push_back(mat.z);
					matrix->m_point.push_back(v4Origin);
				}
			}
			else if( str::EqualNoCase(keyword, "Inverse") )
			{
				if( matrix->m_point.size() >= 4 )
				{
					m3x3 mat;
					matrix->m_point.pop_back();
					mat.z = matrix->m_point.back();	matrix->m_point.pop_back();
					mat.y = matrix->m_point.back();	matrix->m_point.pop_back();
					mat.x = matrix->m_point.back();	matrix->m_point.pop_back();
					Inverse(mat);
					matrix->m_point.push_back(mat.x);
					matrix->m_point.push_back(mat.y);
					matrix->m_point.push_back(mat.z);
					matrix->m_point.push_back(v4Origin);
				}
			}
			else if( !ParseCommon(keyword, matrix) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in Matrix3x3 '%s'", matrix->m_name.c_str()).c_str());
			return false;
		}
	}
	if( matrix->m_point.empty() ) return true;
	matrix->CreateRenderObject();
	return true;
}

//*****
// Read a 4x4 matrix
bool StringParser::ParseMatrix4x4(TMatrix* matrix)
{
	v4 x_axis, y_axis, z_axis, pos;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractVector4(x_axis) &&
			m_loader.ExtractVector4(y_axis) &&
			m_loader.ExtractVector4(z_axis) &&
			m_loader.ExtractVector4(pos) )
		{
			matrix->m_point.push_back(x_axis);
			matrix->m_point.push_back(y_axis);
			matrix->m_point.push_back(z_axis);
			matrix->m_point.push_back(pos);
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "Transpose") )
			{
				if( matrix->m_point.size() >= 4 )
				{
					m4x4 mat;
					mat.w = matrix->m_point.back();	matrix->m_point.pop_back();
					mat.z = matrix->m_point.back();	matrix->m_point.pop_back();
					mat.y = matrix->m_point.back();	matrix->m_point.pop_back();
					mat.x = matrix->m_point.back();	matrix->m_point.pop_back();
					Transpose4x4(mat);
					matrix->m_point.push_back(mat.x);
					matrix->m_point.push_back(mat.y);
					matrix->m_point.push_back(mat.z);
					matrix->m_point.push_back(mat.w);
				}
			}
			else if( str::EqualNoCase(keyword, "Inverse") )
			{
				if( matrix->m_point.size() >= 4 )
				{
					m4x4 mat;
					mat.w = matrix->m_point.back();	matrix->m_point.pop_back();
					mat.z = matrix->m_point.back();	matrix->m_point.pop_back();
					mat.y = matrix->m_point.back();	matrix->m_point.pop_back();
					mat.x = matrix->m_point.back();	matrix->m_point.pop_back();
					Inverse(mat);
					matrix->m_point.push_back(mat.x);
					matrix->m_point.push_back(mat.y);
					matrix->m_point.push_back(mat.z);
					matrix->m_point.push_back(mat.w);
				}
			}
			if( !ParseCommon(keyword, matrix) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in Matrix4x4 '%s'", matrix->m_name.c_str()).c_str());
			return false;
		}
	}
	if( matrix->m_point.empty() ) return true;
	matrix->CreateRenderObject();
	return true;
}

//*****
// Read a mesh.
bool StringParser::ParseMesh(TMesh* mesh)
{
	bool generate_normals = false;
	std::string keyword;
	while( m_loader.GetKeyword(keyword) )
	{
		if( str::EqualNoCase(keyword, "Verts") )
		{
			v4 vert;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for vertices in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
			while( !m_loader.IsSectionEnd() )
			{
				if( !m_loader.ExtractVector3(vert, 1.0f) )	{ m_linedrawer->m_error_output.Error(Fmt("Incomplete vertex found in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
				mesh->m_point.push_back(vert);
			}
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for vertices in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
		}
		else if( str::EqualNoCase(keyword, "Normals") )
		{
			v4 norm;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for normals in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
			while( !m_loader.IsSectionEnd() )
			{
				if( !m_loader.ExtractVector3(norm, 0.0f) )	{ m_linedrawer->m_error_output.Error(Fmt("Incomplete normal found in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
				mesh->m_normal.push_back(norm);
			}
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for normals in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
		}
		else if( str::EqualNoCase(keyword, "Faces") )
		{
			uint i0, i1, i2;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for indices in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
			while( !m_loader.IsSectionEnd() )
			{
				if( !m_loader.ExtractUInt(i0, 10) ||
					!m_loader.ExtractUInt(i1, 10) ||
					!m_loader.ExtractUInt(i2, 10) )		{ m_linedrawer->m_error_output.Error(Fmt("Incomplete face found in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
				mesh->m_index.push_back(value_cast<uint16>(i0));
				mesh->m_index.push_back(value_cast<uint16>(i1));
				mesh->m_index.push_back(value_cast<uint16>(i2));
			}
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for indices in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
		}
		else if( str::EqualNoCase(keyword, "Lines") )
		{
			uint i0, i1;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for indices in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
			while( !m_loader.IsSectionEnd() )
			{
				if( !m_loader.ExtractUInt(i0, 10) ||
					!m_loader.ExtractUInt(i1, 10) )			{ m_linedrawer->m_error_output.Error(Fmt("Incomplete line found in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
				mesh->m_index.push_back(value_cast<uint16>(i0));
				mesh->m_index.push_back(value_cast<uint16>(i1));
			}
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for indices in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
			mesh->m_line_list = true;
		}
		else if( str::EqualNoCase(keyword, "Tetra") )
		{
			uint i0, i1, i2, i3;
			if( !m_loader.FindSectionStart() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section start for indices in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
			while( !m_loader.IsSectionEnd() )
			{
				if( !m_loader.ExtractUInt(i0, 10) ||
					!m_loader.ExtractUInt(i1, 10) ||
					!m_loader.ExtractUInt(i2, 10) ||
					!m_loader.ExtractUInt(i3, 10) )			{ m_linedrawer->m_error_output.Error(Fmt("Incomplete tetra found in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }

				mesh->m_index.push_back(value_cast<uint16>(i0));
				mesh->m_index.push_back(value_cast<uint16>(i1));
				mesh->m_index.push_back(value_cast<uint16>(i2));
				mesh->m_index.push_back(value_cast<uint16>(i0));
				mesh->m_index.push_back(value_cast<uint16>(i2));
				mesh->m_index.push_back(value_cast<uint16>(i3));
				mesh->m_index.push_back(value_cast<uint16>(i0));
				mesh->m_index.push_back(value_cast<uint16>(i3));
				mesh->m_index.push_back(value_cast<uint16>(i1));
				mesh->m_index.push_back(value_cast<uint16>(i3));
				mesh->m_index.push_back(value_cast<uint16>(i2));
				mesh->m_index.push_back(value_cast<uint16>(i1));
			}
			if( !m_loader.FindSectionEnd() )				{ m_linedrawer->m_error_output.Error(Fmt("Unable to find the section end for indices in mesh '%s'", mesh->m_name.c_str()).c_str()); return false; }
		}
		else if( str::EqualNoCase(keyword, "GenerateNormals") )
		{
			generate_normals = true;
		}
		else if( ParseCommon(keyword, mesh) )
		{}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in Mesh '%s'", mesh->m_name.c_str()).c_str());
			return false;
		}
	}
	if( mesh->m_point.empty() || mesh->m_index.empty() ) return true;
	mesh->m_generate_normals = generate_normals || mesh->m_normal.size() != mesh->m_point.size();
	mesh->CreateRenderObject();
	return true;
}

//*****
// Load a geometry file from disc
bool StringParser::ParseFile(TFile* file)
{
	std::string name;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		std::string filename;
		if( m_loader.ExtractString(filename) )
		{
			name = pr::filesys::GetFiletitle(filename);
			std::string extn = pr::filesys::GetExtension(filename);

			// If the file is an x file
			if( str::EqualNoCase(extn, "x") )
			{
				if( Failed(xfile::Load(filename.c_str(), file->m_geometry)) )
				{
					m_linedrawer->m_error_output.Error(Fmt("Failed to load X File: %s", filename.c_str()).c_str());
					return false;
				}
			}
			else
			{
				m_linedrawer->m_error_output.Error(Fmt("File format not supported. Failed to load File: %s", file->m_name.c_str()).c_str());
				return false;
			}

			if( file->m_geometry.m_frame.empty() ) { m_linedrawer->m_error_output.Error(Fmt("File %s contains no geometry", file->m_name.c_str()).c_str()); return false; }
		}
		else if( m_loader.GetKeyword(keyword) )
		{
			if( str::EqualNoCase(keyword, "GenerateNormals") )
			{
				file->m_generate_normals = true;
			}
			else if( str::EqualNoCase(keyword, "Frame") )
			{
				if( !m_loader.ExtractUInt(file->m_frame_number, 10) ) { m_linedrawer->m_error_output.Error(Fmt("Failed to read 'Frame' keyword in File: %s", file->m_name.c_str()).c_str()); return false; }
			}
			else if( !ParseCommon(keyword, file) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in File '%s'", file->m_name.c_str()).c_str());
			return false;
		}
	}
	if( file->m_frame_number != Clamp<uint>(file->m_frame_number, 0, (uint)(file->m_geometry.m_frame.size() - 1)) )
	{
		m_linedrawer->m_error_output.Error(Fmt("Frame number %d does not exist in File '%s'", file->m_frame_number, file->m_name.c_str()).c_str());
		return false;
	}
	file->m_name = name + file->m_geometry.m_frame[file->m_frame_number].m_name;
	file->CreateRenderObject();
	return true;
}

//*****
// Read a collection of other things.
bool StringParser::ParseGroup(TGroup* group)
{
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, group) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in Group '%s'", group->m_name.c_str()).c_str());
			return false;
		}
	}
	group->SetColour(group->m_instance.m_colour, true, true);
	group->CreateRenderObject();
	return true;
}

//*****
// Read a collection of things to cycle thru.
bool StringParser::ParseGroupCyclic(TGroupCyclic* group_cyclic)
{
	uint style;
	float fps;
	std::string keyword;
	while( !m_loader.IsSectionEnd() )
	{
		if( m_loader.ExtractUInt(style, 10) && m_loader.ExtractFloat(fps) )
		{
			if( fps == 0.0f ) fps = 1.0f;
			group_cyclic->m_style			= static_cast<TGroupCyclic::Style>(style);
			group_cyclic->m_ms_per_frame	= static_cast<uint>(1000.0f / fps);
		}
		if( m_loader.GetKeyword(keyword) )
		{
			if( !ParseCommon(keyword, group_cyclic) ) return false;
		}
		else
		{
			m_linedrawer->m_error_output.Error(Fmt("Syntax error found in GroupCyclic '%s'", group_cyclic->m_name.c_str()).c_str());
			return false;
		}
	}
	if( group_cyclic->m_child.empty() ) return true;
	group_cyclic->CreateRenderObject();
	return true;
}

//*****
// Parse the animation data object modifier
bool StringParser::ParseAnimation(AnimationData& animation)
{
	// Extract the style
	uint style;
	if( !m_loader.ExtractUInt(style, 10) ) { return false; }
	if( style >= AnimationData::NumberOf ) { return false; }

	// Extract period
	float period;
	if( !m_loader.ExtractFloat(period) ) { return false; }

	// Extract linear velocity
	v4 velocity;
	if( !m_loader.ExtractVector3(velocity, 0.0f) ) { return false; }

	// Extract rotation axis
	v4 axis;
	if( !m_loader.ExtractVector3(axis, 0.0f) ) { return false; }

	// Extract angular speed
	float ang_speed;
	if( !m_loader.ExtractFloat(ang_speed) ) { return false; }

	animation.m_style			= static_cast<AnimationData::Style>(style);
	animation.m_period			= period;
	animation.m_velocity		= velocity;
	animation.m_rotation_axis	= Normalise3IfNonZero(axis);
	animation.m_angular_speed	= ang_speed;
	return true;
}
#endif//USE_OLD_PARSER