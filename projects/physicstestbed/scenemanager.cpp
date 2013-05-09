//******************************************
// SceneManager
//******************************************
#pragma once

#include "PhysicsTestbed/Stdafx.h"
#include "pr/linedrawer/plugininterface.h"
#include "PhysicsTestbed/SceneManager.h"
#include "PhysicsTestbed/PhysicsTestbed.h"
#include "PhysicsTestbed/Parser.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "PhysicsTestbed/PhysicsEngine.h"
#include "PhysicsTestbed/ShapeGenParams.h"
#include "PhysicsTestbed/PropRigidbody.h"
#include "PhysicsTestbed/PropDeformable.h"
#include "PhysicsTestbed/CollisionCallBacks.h"

using namespace pr;

void SceneManagerPstCollisionCallBack(col::Data const& col_data)
{
	Testbed().m_scene_manager.PstCollisionCallBack(col_data);
}

// Constructor
SceneManager::SceneManager(PhysicsEngine* engine)
:m_physics_engine(engine)
,m_ldr_terrain_sampler(ldr::InvalidObjectHandle)
,m_scale(1.0f)
{
	RegisterPstCollisionCB(SceneManagerPstCollisionCallBack, true);
	Clear();
}

SceneManager::~SceneManager()
{
	Clear();
}

// Update the scene before the physics is stepped
void SceneManager::PrePhysicsStep()
{
	// Update the graphics for the props
	for( TProp::iterator i = m_prop.begin(), i_end = m_prop.end(); i != i_end; ++i )
	{
		Prop& prop = *i->second;
		prop.ApplyDrag(m_drag);
		prop.ApplyGravity();
	}
}

// Update the scene
void SceneManager::Step(float step_size)
{
	// Update the graphics for the props
	for( TProp::iterator i = m_prop.begin(), i_end = m_prop.end(); i != i_end; ++i )
	{
		Prop& prop = *i->second;
		prop.Step(step_size);
	}
}

// Update the transient objects
void SceneManager::UpdateTransients()
{
	Testbed().PushHookState(EHookType_DeleteObjects, false);

	float scale = Testbed().m_state.m_scale * 0.00999f + 0.00001f;
	bool re_scale = m_scale != scale;
	m_scale = scale;

	for( TImpulse::iterator i = m_impulse.begin(), i_end = m_impulse.end(); i != i_end; )
	{
		TImpulse::iterator j = i++;
		if( re_scale )
		{
			(*j)->Recreate(m_scale);
		}
		if( !(*j)->Step(m_physics_engine->GetFrameNumber()) )
		{
			CWImpulse* imp = *j;
			m_impulse.erase(j);
			delete imp;
		}
	}
	for( TContact::iterator i = m_contact.begin(), i_end = m_contact.end(); i != i_end; )
	{
		TContact::iterator j = i++;
		if( re_scale )
		{
			(*j)->Recreate(m_scale);
		}
		if( !(*j)->Step(m_physics_engine->GetFrameNumber()) )
		{
			CWContact* contact = *j;		
			m_contact.erase(j);
			delete contact;
		}
	}
	for( TRayCast::iterator i = m_raycast.begin(), i_end = m_raycast.end(); i != i_end; )
	{
		TRayCast::iterator j = i++;
		if( !(*j)->Step(m_physics_engine->GetFrameNumber()) )
		{
			RayCast* ray = *j;		
			m_raycast.erase(j);
			delete ray;
		}
	}

	// Show the terrain sampler
	TerrainSampler(Testbed().m_state.m_show_terrain_sampler);

	Testbed().PopHookState(EHookType_DeleteObjects);
}

// Unregister LineDrawer objects and delete any physics objects
void SceneManager::Clear()
{
	Testbed().PushHookState(EHookType_DeleteObjects, false);
	m_world_bounds.reset();
	TerrainSampler(false);
	ClearTerrain();
	ClearStatics();
	ClearProps();
	ClearGraphics();
	ClearContacts();
	ClearImpulses();
	ClearRayCasts();
	ClearGravityFields();
	ClearDrag();
	Testbed().PopHookState(EHookType_DeleteObjects);
}

// Free the terrain
void SceneManager::ClearTerrain()
{
	while( !m_terrain.empty() )
	{
		Terrain* terrain = m_terrain.begin()->second;
		m_terrain.erase(m_terrain.begin());
		delete terrain;
	}
}

// Free the statics
void SceneManager::ClearStatics()
{
	m_physics_engine->ClearStaticSceneData();
	while( !m_static.empty() )
	{
		Static* statik = m_static.begin()->second;
		m_static.erase(m_static.begin());
		delete statik;
	}
}

// Free the dynamics
void SceneManager::ClearProps()
{
	while( !m_prop.empty() )
	{
		Prop* prop = m_prop.begin()->second;
		m_prop.erase(m_prop.begin());
		delete prop;
	}
}

// Free graphics
void SceneManager::ClearGraphics()
{
	while( !m_graphics.empty() )
	{
		Graphics* gfx = m_graphics.begin()->second;
		m_graphics.erase(m_graphics.begin());
		delete gfx;
	}
}

// Delete all contact graphics
void SceneManager::ClearContacts()
{
	for( TContact::iterator i = m_contact.begin(), i_end = m_contact.end(); i != i_end; ++i )
		delete *i;
	m_contact.clear();
}

// Delete all impulse graphics
void SceneManager::ClearImpulses()
{
	for( TImpulse::iterator i = m_impulse.begin(), i_end = m_impulse.end(); i != i_end; ++i )
		delete *i;
	m_impulse.clear();
}

// Delete all ray cast graphics
void SceneManager::ClearRayCasts()
{
	for( TRayCast::iterator i = m_raycast.begin(), i_end = m_raycast.end(); i != i_end; ++i )
		delete *i;
	m_raycast.clear();
}

// Delete all gravity fields
void SceneManager::ClearGravityFields()
{
	m_physics_engine->ClearGravityFields();
}

void SceneManager::ClearDrag()
{
	m_drag = 0.0f;
}

// Add line drawer objects and physics objects from 'output' to the scene
void SceneManager::AddToScene(const parse::Output& output)
{
	Encompase(m_world_bounds, output.m_world_bounds);

	// Add any graphics objects to the scene
	for( parse::TGfx::const_iterator i = output.m_graphics.begin(), i_end = output.m_graphics.end(); i != i_end; ++i )
		AddGraphics(*i);

	// Add new terrain objects to the scene
	for( parse::TTerrain::const_iterator i = output.m_terrain.begin(), i_end = output.m_terrain.end(); i != i_end; ++i )
		AddTerrain(*i);
	if( output.m_terrain.empty() ) AddTerrain(parse::Terrain());

	// Add new static objects to the scene
	for( parse::TStatic::const_iterator i = output.m_statics.begin(), i_end = output.m_statics.end(); i != i_end; ++i )
		AddStatic(output, *i);
	m_physics_engine->RebuildStaticScene(m_static, m_world_bounds);

	// Add new physics objects to the scene
	for( parse::TPhysObj::const_iterator i = output.m_phys_obj.begin(), i_end = output.m_phys_obj.end(); i != i_end; ++i )
		AddPhysicsObject(output, *i);
	
	// Add new multibody physics objects to the scene
	for( parse::TMultibody::const_iterator i = output.m_multis.begin(), i_end = output.m_multis.end(); i != i_end; ++i )
		AddMultibody(output, *i);

	// Add any gravity fields
	for( parse::TGravity::const_iterator i = output.m_gravity.begin(), i_end = output.m_gravity.end(); i != i_end; ++i )
		AddGravityField(*i);
	
	// Add any drag
	AddDrag(output.m_drag);

	// Add a physics material
	AddMaterial(output.m_material);

	// Bring the engine up to date
	m_physics_engine->Sync();
}

// Add a non physical object to the scene
void SceneManager::AddGraphics(const parse::Gfx& gfx)
{
	Graphics* g = new Graphics(gfx);
	m_graphics[g->m_ldr] = g;
}

// Add terrain data to the scene
void SceneManager::AddTerrain(parse::Terrain const& terrain)
{
	Terrain* t = new Terrain(terrain, *m_physics_engine);
	m_terrain[t->m_ldr] = t;
}

// Add static physics objects to the scene
Static* SceneManager::AddStatic(parse::Output const& output, parse::Static const& statik)
{
	Static* s = new Static(output, statik, *m_physics_engine);
	m_static[s->m_ldr] = s;
	return s;
}

// Add rigid body physics object to the scene
Prop* SceneManager::AddPhysicsObject(parse::Output const& output, parse::PhysObj const& phys)
{
	if( phys.m_by_name_only )
		return 0;

	EnsureFreePhysicsObject();
	Prop* p = 0;
	switch( phys.m_model_type )
	{
	case parse::EObjectType_Model:		p = new PropRigidbody (output, phys, *m_physics_engine); break;
	case parse::EObjectType_Deformable:	p = new PropDeformable(output, phys, *m_physics_engine); break;
	default: break;
	}
	if( p )
	{
		if( !p->m_valid )
		{
			delete p;
			return 0;
		}
		PR_ASSERT_STR(PR_DBG_COMMON, m_prop.find(p->m_prop_ldr.m_ldr) == m_prop.end(), "The ldr object for this prop is not unique");	
		m_prop[p->m_prop_ldr.m_ldr] = p;
	}
	return p;
}

// Add multibody physics object to the scene
Prop* SceneManager::AddMultibody(parse::Output const& output, parse::Multibody const& multi, Prop* parent)
{
	// Override properties
	parse::PhysObj phys = output.m_phys_obj[multi.m_phys_obj_index];
	phys.m_by_name_only = false;
	if( !multi.m_name.empty() )							phys.m_name				= multi.m_name;
	if( multi.m_object_to_world != pr::m4x4Identity )	phys.m_object_to_world	= multi.m_object_to_world;
	if( !IsZero3(multi.m_gravity) )						phys.m_gravity			= multi.m_gravity;
	if( !IsZero3(multi.m_velocity) )					phys.m_velocity			= multi.m_velocity;
	if( !IsZero3(multi.m_ang_velocity) )				phys.m_ang_velocity		= multi.m_ang_velocity;
	if( multi.m_colour != pr::Colour32Black )			phys.m_colour			= multi.m_colour;

	Prop* prop = AddPhysicsObject(output, phys);
	prop->MultiAttach(multi, parent);
	
	// Add the children
	for( parse::TMultibody::const_iterator i = multi.m_joints.begin(), i_end = multi.m_joints.end(); i != i_end; ++i )
	{
		AddMultibody(output, *i, prop);
	}
	return prop;
}

// Set the physics material to use
void SceneManager::AddMaterial(const parse::Material& material)
{
	m_physics_engine->SetMaterial(material);
}

// Add a gravity field
void SceneManager::AddGravityField(parse::Gravity const& grav)
{
	m_physics_engine->AddGravityField(grav);
}

// Set the drag percentage to use
void SceneManager::AddDrag(float drag)
{
	m_drag = drag;
}

// Add a contact graphic
void SceneManager::AddContact(const pr::v4& pt, const pr::v4& norm)
{
	m_contact.push_back(new CWContact(pt, norm, m_scale, m_physics_engine->GetFrameNumber()));
}

// Add an impulse graphic
void SceneManager::AddImpulse(const pr::v4& pt, const pr::v4& impulse)
{
	m_impulse.push_back(new CWImpulse(pt, impulse, m_scale, m_physics_engine->GetFrameNumber()));
}

// Add a ray cast graphic
void SceneManager::AddRayCast(const pr::v4& start, const pr::v4& end)
{
	m_raycast.push_back(new RayCast(start, end, m_physics_engine->GetFrameNumber()));
}


// Generate a random box
void SceneManager::CreateBox()
{
	pr::v4 pos = ldrGetFocusPoint();
	std::string src = pr::Fmt(
		"*PhysicsObject "
		"{ "
			"*Model { *Box { *Random { %1.1f %1.1f %1.1f %1.1f %1.1f %1.1f} *RandomColour } } "
			"*RandomTransform { %f %f %f 0 } "
		"} "
		,ShapeGen().m_box_min_dim.x ,ShapeGen().m_box_min_dim.y ,ShapeGen().m_box_min_dim.z
		,ShapeGen().m_box_max_dim.x ,ShapeGen().m_box_max_dim.y ,ShapeGen().m_box_max_dim.z
		,pos.x ,pos.y ,pos.z);

	Parser parser;
	if( parser.Load(src.c_str(), src.size()) )
	{
		AddPhysicsObject(parser.m_output, parser.m_output.m_phys_obj.front());
		ldrRender();
	}
}

// Generate a random cylinder
void SceneManager::CreateCylinder()
{
	pr::v4 pos = ldrGetFocusPoint();
	std::string src = pr::Fmt(
		"*PhysicsObject "
		"{ "
			"*Model { *Cylinder { *Random { %1.1f %1.1f %1.1f %1.1f } *RandomColour } } "
			"*RandomTransform { %f %f %f 0 } "
		"} "
		,ShapeGen().m_cyl_min_height ,ShapeGen().m_cyl_min_radius
		,ShapeGen().m_cyl_max_height ,ShapeGen().m_cyl_max_radius
		,pos.x ,pos.y ,pos.z);

	Parser parser;
	if( parser.Load(src.c_str(), src.size()) )
	{
		AddPhysicsObject(parser.m_output, parser.m_output.m_phys_obj.front());
		ldrRender();
	}
}

// Generate a random sphere
void SceneManager::CreateSphere()
{
	pr::v4 pos = ldrGetFocusPoint();
	std::string src = pr::Fmt(
		"*PhysicsObject "
		"{ "
			"*Model { *Sphere { *Random { %1.1f %1.1f } *RandomColour } } "
			"*RandomTransform { %f %f %f 0 } "
		"} "
		,ShapeGen().m_sph_min_radius ,ShapeGen().m_sph_max_radius
		,pos.x ,pos.y ,pos.z);

	Parser parser;
	if( parser.Load(src.c_str(), src.size()) )
	{
		AddPhysicsObject(parser.m_output, parser.m_output.m_phys_obj.front());
		ldrRender();
	}
}

// Generate a random polytope
void SceneManager::CreatePolytope()
{
	pr::v4 pos = ldrGetFocusPoint();
	std::string src = pr::Fmt(
		"*PhysicsObject "
		"{ "
			"*Model { *Polytope { *Random { %d %1.1f %1.1f %1.1f %1.1f %1.1f %1.1f } *RandomColour } } "
			"*RandomTransform { %f %f %f 0 } "
		"} "
		,ShapeGen().m_ply_vert_count
		,ShapeGen().m_ply_min_dim.x ,ShapeGen().m_ply_min_dim.y ,ShapeGen().m_ply_min_dim.z
		,ShapeGen().m_ply_max_dim.x ,ShapeGen().m_ply_max_dim.y ,ShapeGen().m_ply_max_dim.z
		,pos.x ,pos.y ,pos.z);

	Parser parser;
	if( parser.Load(src.c_str(), src.size()) )
	{
		AddPhysicsObject(parser.m_output, parser.m_output.m_phys_obj.front());
		ldrRender();
	}
}

// Generate a random deformable mesh
void SceneManager::CreateDeformableMesh()
{
	pr::v4 pos = ldrGetFocusPoint();
	std::string src = pr::Fmt(
		"*PhysicsObject "
		"{ "
			"*Deformable { *Verts { } *Struts { } *RandomColour } "	//*Random { %d %1.1f %1.1f %1.1f %1.1f %1.1f %1.1f } 
			"*RandomTransform { %f %f %f 0 } "
		"} "
		//,ShapeGen().m_ply_vert_count
		//,ShapeGen().m_ply_min_dim.x ,ShapeGen().m_ply_min_dim.y ,ShapeGen().m_ply_min_dim.z
		//,ShapeGen().m_ply_max_dim.x ,ShapeGen().m_ply_max_dim.y ,ShapeGen().m_ply_max_dim.z
		,pos.x ,pos.y ,pos.z);

	Parser parser;
	if( parser.Load(src.c_str(), src.size()) )
	{
		//AddPhysicsObject(parser.m_output, parser.m_output.m_phys_obj.front());
		ldrRender();
	}
}

struct RayHitData : col::Data
{
	pr::v4 m_point;
	pr::v4 m_normal;
	pr::v4 m_impulse;
	pr::uint m_prim_id;
	RayHitData()										{ m_objA = 0; m_objB = 0; m_info = 0; }
	pr::uint	 NumContacts() const					{ return 1; }
	col::Contact GetContact(int obj_index, int) const	{ float sign = -obj_index*2.0f+1.0f; return col::Contact(m_point, sign*m_normal, sign*m_impulse, sign*pr::v4Zero, m_prim_id); }
};

// Cast a ray from the camera to the focus point
void SceneManager::CastRay(bool apply_impulse)
{
	pr::m4x4 c2w = ldrGetCameraToWorld();
	pr::v4	 ray = ldrGetFocusPoint() - c2w.pos;
	
	float intercept;
	RayHitData col_data;
	PhysObj* hit_object;
	m_physics_engine->CastRay(c2w.pos, ray, intercept, col_data.m_normal, hit_object, col_data.m_prim_id);
	AddRayCast(c2w.pos, c2w.pos + ray * intercept);
	if( intercept != 1.0f )
	{
		col_data.m_point = c2w.pos + ray * intercept;
		AddContact(col_data.m_point, col_data.m_normal);
		if( apply_impulse && hit_object )
		{
			// Apply an impulse that will change the velocity
			static float deltav = 5.0f;
			float strength = PhysicsEngine::ObjectGetMass(hit_object) * deltav;
			col_data.m_impulse = strength * Normalise3(ray);
			col_data.m_objA = hit_object;
			PhysicsEngine::ObjectApplyImpulse(hit_object, col_data.m_impulse, col_data.m_point);
			GetPropFromPhysObj(hit_object)->OnCollision(col_data);
		}
	}
}

// Update the terrain sampler graphic
void SceneManager::TerrainSampler(bool show)
{
	if (m_ldr_terrain_sampler != ldr::InvalidObjectHandle)
	{
		ldrUnRegisterObject(m_ldr_terrain_sampler);
		m_ldr_terrain_sampler = ldr::InvalidObjectHandle;
	}
	if (show)
	{
		std::string str = m_physics_engine->CreateTerrainSampler(ldrGetFocusPoint());
		m_ldr_terrain_sampler = ldrRegisterObject(str.c_str(), str.size());
	}
}
	
// Turn on/off displaying of bounding boxes for props
void SceneManager::ViewStateUpdate()
{
	for( TProp::iterator p = m_prop.begin(), p_end = m_prop.end(); p != p_end; ++p )
	{
		p->second->ViewStateUpdate();
	}
}

// Delete an object from the scene
void SceneManager::DeleteObject(pr::ldr::ObjectHandle object)
{
	// Delete a terrain object
	TTerrain::iterator t = m_terrain.find(object);
	if( t != m_terrain.end() )
	{
		Terrain* terr = t->second;
		m_terrain.erase(t);
		delete terr;
		return;
	}

	// Delete a static object
	TStatic::iterator s = m_static.find(object);
	if( s != m_static.end() )
	{
		Static* statik = s->second;
		m_static.erase(s);
		delete statik;
		// Rememeber to call rebuild static scene
		return;
	}

	// Delete a prop
	TProp::iterator p = m_prop.find(object);
	if( p != m_prop.end() )
	{
		Prop* prop = p->second;
		m_prop.erase(p);
		delete prop;
		return;
	}

	// Delete a graphics object
	TGraphics::iterator g = m_graphics.find(object);
	if( g != m_graphics.end() )
	{
		Graphics* gfx = g->second;
		m_graphics.erase(g);
		delete gfx;
		return;
	}
}

// If we're out of physics objects delete the oldest
void SceneManager::EnsureFreePhysicsObject()
{
	if( m_prop.size() < m_physics_engine->GetMaxObject() ) return;

	TProp::iterator p = m_prop.begin(), p_end = m_prop.end(), oldest = p;
	for( ; p != p_end; ++p )
	{
		if( p->second->m_created_time < oldest->second->m_created_time )
			oldest = p;
	}
	Prop* doomed = oldest->second;
	m_prop.erase(oldest);
	delete doomed;
}

// Save the current scene to a line script file
// If 'physics_scene' is true the scene is saved as physics objects
// If 'physics_scene' is false the scene is saved as normal linedrawer scene description
void SceneManager::ExportScene(const char* filename, bool physics_scene)
{
	std::string tmp_filename = pr::FmtS("%s.tmp", filename);
	{ // scope for file
		pr::Handle file = FileOpen(tmp_filename.c_str(), pr::EFileOpen_Writing);
		if (file == INVALID_HANDLE_VALUE)
		{
			//MessageBox(Testbed().m_controls.GetSafeHwnd(), pr::Fmt("Failed to open file: %s", filename).c_str(), "Export Error", MB_ICONEXCLAMATION|MB_OK);
			return;
		}
		
		// Save the camera location
		if( !physics_scene )
		{
			pr::ldr::CameraData cam = ldrGetCameraData();
			std::string str = pr::Fmt(
				"*Camera "
				"{ "
					"*Position %f %f %f "
					"*LookAt %f %f %f "
					"*Up %f %f %f "
					"*FOV %f "
					"*Aspect %f "
					"*Near %f "
					"*Far %f "
				"}\n"
				,cam.m_camera_position.x ,cam.m_camera_position.y ,cam.m_camera_position.z
				,cam.m_lookat_centre.x   ,cam.m_lookat_centre.y   ,cam.m_lookat_centre.z
				,cam.m_camera_up.x       ,cam.m_camera_up.y       ,cam.m_camera_up.z
				,cam.m_fov
				,cam.m_aspect
				,cam.m_near
				,cam.m_far);
			FileWrite(file, str.c_str(), DWORD(str.size()));
		}

		// Save terrain
		for( TTerrain::iterator t = m_terrain.begin(), t_end = m_terrain.end(); t != t_end; ++t )
		{
			t->second->ExportTo(file, physics_scene);
		}

		// Save props
		for( TProp::iterator p = m_prop.begin(), p_end = m_prop.end(); p != p_end; ++p )
		{
			p->second->ExportTo(file, physics_scene);
		}

		// Save statics

		// Save graphics
	}

	// Double buffer the export file
	CopyFile(tmp_filename.c_str(), filename, FALSE);
	DeleteFile(tmp_filename.c_str());
}

// Converts a PhysObj into a PropBase
Prop* SceneManager::GetPropFromPhysObj(PhysObj const* obj)
{
	if( !obj ) return 0;
	return static_cast<Prop*>(PhysicsEngine::ObjectGetPreColData(obj));
}

// Post-collision call back
void SceneManager::PstCollisionCallBack(col::Data const& col_data)
{
	Prop* propA = GetPropFromPhysObj(col_data.m_objA);
	Prop* propB = GetPropFromPhysObj(col_data.m_objB);
	if( propA ) propA->OnCollision(col_data);
	if( propB )	propB->OnCollision(col_data);
}

