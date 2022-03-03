//******************************************
// PropRigidbody
//******************************************
#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/PropRigidbody.h"
#include "PhysicsTestbed/Parser.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "PhysicsTestbed/PhysicsEngine.h"
#include "PhysicsTestbed/TestbedState.h"
#include "PhysicsTestbed/PhysicsTestbed.h"

#define PR_DBG_SKEL 1

PropRigidbody::PropRigidbody(parse::Output const& output, parse::PhysObj const& phys, PhysicsEngine& engine)
:m_phys(phys)
,m_model()
{
	if( m_phys.m_model_index != 0xFFFFFFFF )	m_model = output.m_models[phys.m_model_index];
	if( m_phys.m_colour == pr::Colour32Black )	m_phys.m_colour = m_model.m_colour;
	if( m_phys.m_colour == pr::Colour32Black && !m_model.m_prim.empty() )	m_phys.m_colour = m_model.m_prim.begin()->m_colour;

	// Create a collision model for the model
	engine.CreateCollisionModel(m_model, m_col_model);
	m_col_model.m_name			= m_phys.m_name;
	m_col_model.m_colour		= m_phys.m_colour;
	m_phys.m_object_to_world	= m_phys.m_object_to_world * m_col_model.m_CoMframe_to_model;
	if( !m_col_model.m_shape )
		return;
	
	// Create a physics object
	engine.CreatePhysicsObject(m_phys, m_col_model, this, m_object);
	if( !m_object )
		return;

	// The prop is now valid
	m_valid = true;

	m_prop_ldr.UpdateGfx(PhysicsEngine::MakeLdrString(m_col_model.m_name, m_col_model.m_colour, m_col_model));
	
	//// Create a skeleton for the object if provided
	//if( m_model.m_skel.has_data() )
	//{
	//	engine.CreateSkeleton(m_model.m_skel, m_col_model, m_skel);

	//	m_skel.m_render_skel	= m_model.m_skel.m_render;
	//	if( m_skel.m_render_skel )
	//	{
	//		pr::ldr::CustomObjectData settings;
	//		settings.m_name = "skeleton";
	//		settings.m_colour = m_model.m_skel.m_colour;
	//		settings.m_num_verts = m_model.m_skel.m_anchor.size();
	//		settings.m_num_indices = m_model.m_skel.m_strut.size();
	//		settings.m_geom_type = pr::geometry::EType_Vertex;
	//		settings.m_primitive_type = pr::rdr::model::EPrimitiveType_LineList;
	//		settings.m_user_data = m_skel.m_inst;
	//		settings.m_create_func = PhysicsEngine::MakeLdrObject;
	//		m_skel.m_gfx.m_ldr = ldrRegisterCustomObject(settings);

	//		pr::m4x4 o2w = PhysicsEngine::ObjectToWorld(m_object);
	//		m_skel.m_gfx.UpdateO2W(o2w);
	//	}
	//}

	ViewStateUpdate();
}

// Step the prop, not really much to do, the physics is doing it
void PropRigidbody::Step(float)
{
	// If the prop is flagged as stationary reset it's transform to the initial position
	if( m_phys.m_stationary )
	{
		PhysicsEngine::SetObjectToWorld(m_object, m_phys.m_object_to_world);
		PhysicsEngine::ObjectSetVelocity(m_object, pr::v4Zero);
		PhysicsEngine::ObjectSetAngVelocity(m_object, pr::v4Zero);
	}

	//PhysicsEngine::ObjectWakeUp(m_object);
	
	//if( m_skel.m_in_use )
	//{
	//	if( PhysicsEngine::SkeletonEvolve(m_skel, step_size) )
	//	{
	//		//PhysicsEngine::SkeletonMorphCM(m_skel, m_col_model);
	//		//m_prop_ldr.UpdateGfx(PhysicsEngine::MakeLdrString(m_phys.m_name, m_phys.m_colour, m_col_model));

	//		if( m_skel.m_render_skel )
	//			ldrEditObject(m_skel.m_gfx.m_ldr, PhysicsEngine::MakeLdrObject, m_skel.m_inst);
	//			//m_skel.m_gfx.UpdateGfx(PhysicsEngine::MakeLdrString("skeleton", m_skel.m_colour_dmg, m_skel));
	//	}

	//	pr::m4x4 o2w = PhysicsEngine::ObjectToWorld(m_object);
	//	m_skel.m_gfx.UpdateO2W(o2w);
	//}

	UpdateGraphics();
}

// On collision for a rigid body
void PropRigidbody::OnCollision(col::Data const&)
{
	UpdateGraphics();

	//int obj_index = m_object == col_data.m_objB;

	//for( pr::uint i = 0; i != col_data.m_num_contacts; ++i )
	//{
	//	col::Contact const& ct = is_objA ? col_data.m_contactsA[i] : col_data.m_contactsB[i];
	//	PhysicsEngine::Deform(m_col_model, ct.m_prim_id, ms_point, ms_norm, ms_impulse);
	//}
	
	//if( m_skel.m_in_use )
	//{
	//	for( pr::uint i = 0; i != col_data.NumContacts(); ++i )
	//	{
	//		col::Contact ct = col_data.GetContact(obj_index, i);

	//		pr::m4x4 w2o = PhysicsEngine::ObjectToWorld(m_object).GetInverseFast();
	//		pr::v4 ms_point		= w2o * ct.m_ws_point;
	//		pr::v4 ms_norm		= w2o * ct.m_ws_normal;
	//		pr::v4 ms_deltavel	= w2o * ct.m_ws_delta_vel;
	//		
	//		// Should really be using the change in relative velocity, that's independent of mass then
	//		static float scale = 0.05f;
	//		ms_deltavel = w2o * ct.m_ws_impulse * scale;

	//		PhysicsEngine::SkeletonDeform(m_skel, ms_point, ms_norm, ms_deltavel);
	//	}
	//}
}

// Save a prop to a ldr script file
void PropRigidbody::ExportTo(pr::Handle& file, bool physics_scene) const
{
	if( physics_scene )
	{
		// TODO
		//std::string str;
		//str +=
		//	"*PhysicsObject\n"
		//	"{\n"
		//	"   \n";
		//FileWrite(file, str.c_str(), str.size());
	}
	else
	{
		parse::PhysObj phys = m_phys;
		phys.m_object_to_world = PhysicsEngine::ObjectToWorld(m_object);
		std::string str = PhysicsEngine::MakeLdrString(m_col_model.m_name, m_col_model.m_colour, m_col_model);
		FileWrite(file, str.c_str(), DWORD(str.size()));
	}
}

