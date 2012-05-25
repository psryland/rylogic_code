//***************************************
// Physics Testbed Test Code
//***************************************

#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/PhysicsEngine.h"
#include "PhysicsTestbed/PropDeformable.h"
#include "PhysicsTestbed/Parser.h"
#include "PhysicsTestbed/PhysicsTestbed.h"

using namespace pr;

bool g_use_ldr_strings = false;

PropDeformable::PropDeformable(parse::Output const& output, parse::PhysObj const& phys, PhysicsEngine& engine)
:m_engine(&engine)
,m_phys(phys)
,m_deform()
,m_deformed(false)
,m_generate_col_models(true)
{
	parse::Deformable const& deformable = output.m_deformables[phys.m_model_index];
	if( m_phys.m_colour == pr::Colour32Black ) m_phys.m_colour = deformable.m_colour;
	m_generate_col_models = deformable.m_generate_col_models;

	// Create the deformable mesh
	engine.CreateDeformableModel(deformable, m_deform);
	m_deform.m_name				= m_phys.m_name + "_dmg_model";
	m_deform.m_spring_colour	= deformable.m_springs_colour;
	m_deform.m_beam_colour		= deformable.m_beams_colour;
	m_deform.m_show_velocity	= Testbed().m_state.m_show_velocity;
	m_deform.m_convex_tolerance	= deformable.m_convex_tolerance;
	if( m_deform.m_model == 0 ) 
		return;

	// Generate a collision model from the deformable mesh
	PhysicsEngine::DeformableDecompose(m_deform, m_col_model);
	m_col_model.m_name			= m_phys.m_name;
	m_col_model.m_colour		= m_phys.m_colour;
	m_phys.m_object_to_world	= m_phys.m_object_to_world * m_col_model.m_CoMframe_to_model;
	
	// Create a physics object
	engine.CreatePhysicsObject(m_phys, m_col_model, this, m_object); PR_ASSERT(PR_DBG_COMMON, m_object);
	if( !m_object )
		return;

	// The prop is now valid
	m_valid = true;

	if( !g_use_ldr_strings )
	{// Create a graphic for the prop
		pr::ldr::CustomObjectData settings;
		settings.m_name				= m_phys.m_name;
		settings.m_colour			= m_phys.m_colour;
		settings.m_num_verts		= 2000;
		settings.m_num_indices		= 10000;
		settings.m_geom_type		= pr::geometry::EType_Vertex | pr::geometry::EType_Normal;
		settings.m_user_data		= &m_col_model;
		settings.m_create_func		= PhysicsEngine::MakeLdrObject;
		m_prop_ldr					= ldrRegisterCustomObject(settings);
	}
	else
	{
		//m_prop_ldr.UpdateGfx(PhysicsEngine::MakeLdrString(m_phys.m_name, m_phys.m_colour, m_col_model));
	}

	if( !g_use_ldr_strings )
	{// Create a graphic for the deformable mesh
		pr::ldr::CustomObjectData settings;
		settings.m_name				= m_deform.m_name;
		settings.m_colour			= pr::Colour32White;
		settings.m_num_verts		= 5000;//should work this out proper like. m_deformable.m_vertex.size() + m_deformable.m_anchors.size();
		settings.m_num_indices		= 5000;//should work this out proper like. m_deformable.m_springs.size() + m_deformable.m_beams.size();
		settings.m_geom_type		= pr::geometry::EType_Vertex | pr::geometry::EType_Normal | pr::geometry::EType_Colour;
		settings.m_user_data		= &m_deform;
		settings.m_create_func		= PhysicsEngine::MakeLdrObjectDeformable;
		m_skel_ldr					= ldrRegisterCustomObject(settings);
	}
	else
	{
		//m_skel_ldr.UpdateGfx(PhysicsEngine::MakeLdrString(m_deform.m_name, m_deform.m_spring_colour, m_deform, Testbed().m_state.m_show_velocity));
	}

	ViewStateUpdate();
}

// Update the rendering of deformable prop specific graphics
void PropDeformable::UpdateGraphics()
{
	m4x4 i2w = I2W(); //i2w.pos += v4::make(2.0f, 0, 0, 0);
	m_skel_ldr.UpdateO2W(i2w);
	Prop::UpdateGraphics();
}

void PropDeformable::Step(float step_size)
{
	// If the prop is flagged as stationary reset it's transform to the initial position
	if( m_phys.m_stationary )
	{
		PhysicsEngine::SetObjectToWorld(m_object, m_phys.m_object_to_world);
		PhysicsEngine::ObjectSetVelocity(m_object, pr::v4Zero);
		PhysicsEngine::ObjectSetAngVelocity(m_object, pr::v4Zero);
	}

	bool deformed = PhysicsEngine::DeformableEvolve(m_deform, step_size, false);
	
	// Nothing to do unless we've been deformed
	if( deformed )
	{
		// Update the skeleton graphic
		m_deform.m_show_velocity = Testbed().m_state.m_show_velocity;
		if( !g_use_ldr_strings )
		{
			ldrEditObject(m_skel_ldr, PhysicsEngine::MakeLdrObjectDeformable, &m_deform);
		}
		else
		{
			//m_skel_ldr.UpdateGfx(PhysicsEngine::MakeLdrString(m_deform.m_name, m_deform.m_spring_colour, m_deform, Testbed().m_state.m_show_velocity));
		}
	}

	if( deformed && m_generate_col_models )
	{
		// Re-create the collision model from the deformable mesh
		PhysicsEngine::DeformableDecompose(m_deform, m_col_model);

		// Update the collision model and the object transform
		PhysicsEngine::ObjectSetColModel(m_object, m_col_model, I2W());

		// Update the line drawer objects
		if( !g_use_ldr_strings )
		{
			ldrEditObject(m_prop_ldr, PhysicsEngine::MakeLdrObject, &m_col_model);
		}
		else
		{
			//m_prop_ldr.UpdateGfx(PhysicsEngine::MakeLdrString(m_phys.m_name, m_phys.m_colour, m_col_model), true);
		}
	}
	UpdateGraphics();
}

// If something hits the prop
void PropDeformable::OnCollision(col::Data const& col_data)
{
	//return;

	// Transform from world space to deformable model space
	pr::m4x4 w2dm = GetInverseFast(I2W());
			
	for( pr::uint i = 0; i != col_data.NumContacts(); ++i )
	{
		col::Contact ct = col_data.GetContact(m_object == col_data.m_objB, i);
		pr::v4 ms_point		= w2dm * ct.m_ws_point;
		pr::v4 ms_norm		= w2dm * ct.m_ws_normal;
		pr::v4 ms_impulse	= w2dm * ct.m_ws_impulse / PhysicsEngine::ObjectGetMass(m_object); // normalise the impulse to make it independent of mass

		// Sets the velocities in the verts around the collision point
		PhysicsEngine::DeformableImpact(m_deform, ms_point, ms_norm, ms_impulse);
	}
}

// Save a prop to a ldr script file
void PropDeformable::ExportTo(pr::Handle& file, bool physics_scene) const
{
	if( physics_scene )
	{}
	else
	{
		parse::PhysObj phys = m_phys;
		phys.m_object_to_world = I2W();
		std::string str = PhysicsEngine::MakeLdrString(m_phys.m_name, m_phys.m_colour, m_col_model);
		FileWrite(file, str.c_str(), DWORD(str.size()));
	}
}


//void PropDeformable::Step2(float step_size)
//{
//	// Nothing to do unless we've been deformed
//	if( !m_deformed )
//	{
//		UpdateGraphics();
//		return;
//	}
//	m_deformed = false;
//
//	// Record the current orientation of the physics object so that
//	// when we update the transform it still has the same orientation
//	// 'i' = inertial, 'w' = world, 'm' = model
//	m4x4 iold_to_w = I2W();
//	m4x4 m_to_iold = m_col_model.m_model_to_CoMframe;
//
//	// Re-create the collision model from the deformable mesh
//	PhysicsEngine::Decompose(m_deform, m_col_model, m_convex_tolerance);
//
//	//// Generate the collision model
//	//m_engine->CreateCollisionModel(m_deformable.m_model, m_col_model);
//
//	pr::m4x4 inew_to_m = m_col_model.m_CoMframe_to_model;
//	pr::m4x4 inew_to_w = iold_to_w * m_to_iold * inew_to_m;
//
//	// Update the collision model and the object transform
//	PhysicsEngine::ObjectSetColModel(m_object, m_col_model, inew_to_w);
//
//	// Register a new line drawer object
//	m_prop_ldr.UpdateGfx(PhysicsEngine::MakeLdrString(m_phys.m_name, m_colour, m_col_model));
//	UpdateGraphics();
//}
//
//// If something hits the prop
//// Deform the deformable mesh in response to the impulse
//// Regenerate the ldr for the deformable mesh
//// Generate a new polytope from the deformable mesh
//void PropDeformable::OnCollision2(col::Data const& col_data)
//{
//	static float plasticity = 0.5f;
//	for( pr::uint i = 0; i != col_data.NumContacts(); ++i )
//	{
//		col::Contact ct = col_data.GetContact(m_object == col_data.m_objB, i);
//		pr::v4 const& ws_point		= ct.m_ws_point;
//		pr::v4 const& ws_norm		= ct.m_ws_normal;
//		pr::v4 const& ws_impulse	= ct.m_ws_impulse;
//	
//		// Calculate the size of the deformation using:
//		// str_coeff = strength co-efficient
//		// imp = impulse / str_coeff;
//		// t = imp^2 / (str_coeff + imp^2); // Don't simplify this to 'impulse^2 / (str_coeff^3 + impulse^2)' as impulse^2 is a massive number
//		// dent_size = object_size * (3.0f - 2.0f * t) * t * t;
//		float impulse_len = ws_impulse.Length3();
//		float imp = impulse_len / m_strength;
//		float t = Sqr(imp) / (m_strength + Sqr(imp));
//		float dent_size = (3.0f - 2.0f * t) * Sqr(t);
//
//		static float threshold = 0.05f;
//		if( dent_size > threshold )
//		{
//			BoundingBox bbox = PhysicsEngine::ObjectGetOSBbox(m_object);
//			dent_size *= 0.5f * bbox.Diametre();
//
//			// Create a shape matrix in world space
//			pr::m4x4 shape;
//			if( FEql(pr::Abs(ws_norm.y), 1.0f, maths::tiny) )
//			{
//				shape.x = dent_size * v4ZAxis;
//				shape.y = dent_size * v4XAxis;
//			}
//			else
//			{
//				v4 tmp_x = pr::Cross3(pr::v4YAxis, ws_norm).GetNormal3();
//				shape.x = dent_size * tmp_x;
//				shape.y = dent_size * Cross3(ws_norm, tmp_x);
//			}
//			shape.z   = (dent_size / impulse_len) * ws_impulse;
//			shape.pos = ws_point;
//
//			// Transform from world space to deformable model space
//			pr::m4x4 w2dm = m_col_model.m_CoMframe_to_model * I2W().GetInverseFast();
//			shape = w2dm * shape;
//
//			PhysicsEngine::Deform(m_deform, shape, plasticity, 0.001f);
//			m_deformed = true;
//		}
//	}
//}
//
