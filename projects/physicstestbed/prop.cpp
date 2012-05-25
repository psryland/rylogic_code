//******************************************
// Prop
//******************************************

#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/Prop.h"
#include "PhysicsTestbed/PhysicsEngine.h"
#include "PhysicsTestbed/PhysicsTestbed.h"

Prop::Prop()
:m_created_time(GetTickCount())
,m_valid(false)
,m_displaying_resting_contacts(false)
,m_object(0)
,m_parent(0)
,m_col_model()
{}

Prop::~Prop()
{
	if( IsMultibody() ) BreakMultibody();
	Testbed().m_physics_engine.DeletePhysicsObject(m_object);
}

// Return the object to world for the prop
pr::m4x4 Prop::I2W() const
{
	return PhysicsEngine::ObjectToWorld(m_object);
}

// Set the gravity vector
void Prop::ApplyGravity()
{
	PhysicsEngine::ObjectSetGravity(m_object);
}

// Apply drag to the objects velocity
void Prop::ApplyDrag(float drag)
{
	if( drag == 0.0f ) return;
	PhysicsEngine::ObjectSetVelocity   (m_object, PhysicsEngine::ObjectGetVelocity   (m_object) * (1.0f - drag));
	PhysicsEngine::ObjectSetAngVelocity(m_object, PhysicsEngine::ObjectGetAngVelocity(m_object) * (1.0f - drag));
}

// Attach this prop to another one to make a multibody
void Prop::MultiAttach(parse::Multibody const& multi, Prop* parent)
{
	PR_ASSERT_STR(1, m_parent == 0, "Already a multibody");
	PR_ASSERT_STR(1, !parent || parent->m_parent != 0, "Not a multibody");

	if( !parent )
	{
		PhysicsEngine::MultiAttach(m_object, 0, multi);
		m_parent = this;
	}
	else
	{
		PhysicsEngine::MultiAttach(m_object, parent->m_object, multi);
		m_parent = parent;
		parent->m_children.push_back(this);
	}
	UpdateGraphics();
}

// Return true if this prop is part of a multibody
bool Prop::IsMultibody() const
{
	return m_parent != 0;
}

// Break the multibody that this prop is part of
void Prop::BreakMultibody()
{
	PR_ASSERT(1, IsMultibody());
	PhysicsEngine::MultiBreak(m_object);

	// Find the root of the multi
	Prop* root = m_parent;
	while( root->m_parent != root )
		root = root->m_parent;

	// Go through all links clearing children and parent pointers
	std::vector<Prop*> parts;
	parts.push_back(root);
	while( !parts.empty() )
	{
		Prop* link = parts.back(); parts.pop_back();
		parts.insert(parts.end(), link->m_children.begin(), link->m_children.end());
		link->m_parent = 0;
		link->m_children.clear();
	}
}

// Update the view state for props
void Prop::ViewStateUpdate()
{
	// Display sleeping objects as semi transparent
	m_prop_ldr.SetSemiTransparent(Testbed().m_state.m_show_sleeping && PhysicsEngine::ObjectIsSleeping(m_object));

	// Display a ws bounding box around the prop
	m_ldr_ws_bbox.UpdateGfx("*Box ws_bbox FF0000FF { 1 1 1 *Wireframe }", Testbed().m_state.m_show_ws_bounding_boxes);

	// Display an os bounding box around the prop
	m_ldr_os_bbox.UpdateGfx("*Box os_bbox FF0000FF { 1 1 1 *Wireframe }", Testbed().m_state.m_show_os_bounding_boxes);

	// Display a velocity vector
	m_ldr_velocity.UpdateGfx("*Line velocity FFFF0000 { 0 0 0 0 0 1 }", Testbed().m_state.m_show_velocity);
	
	// Display an ang velocity vector
	m_ldr_ang_vel.UpdateGfx("*Line ang_vel FF00FF00 { 0 0 0 0 0 1 }", Testbed().m_state.m_show_ang_velocity);

	// Display an ang momentum vector
	m_ldr_ang_mom.UpdateGfx("*Line ang_mom FF0000FF { 0 0 0 0 0 1 }", Testbed().m_state.m_show_ang_momentum);

	// Display the centre of mass of the prop
	m_ldr_com.UpdateGfx("*Matrix3x3 centre_of_mass FFFFFFFF { 0.1 0 0  0 0.1 0  0 0 0.1 }", Testbed().m_state.m_show_centre_of_mass);

	// Display the inertia tensor for the prop
	pr::m3x3 inertia = PhysicsEngine::ObjectGetOSInertia(m_object);
	m_ldr_inertia.UpdateGfx(
		pr::Fmt("*Matrix3x3 inertia FFFFFFFF { %f %f %f  %f %f %f  %f %f %f }"
		,inertia.x.x ,inertia.x.y ,inertia.x.z
		,inertia.y.x ,inertia.y.y ,inertia.y.z
		,inertia.z.x ,inertia.z.y ,inertia.z.z)
		,Testbed().m_state.m_show_inertia);

	// Display any resting contact points for the prop
	if( Testbed().m_state.m_show_resting_contacts && !m_displaying_resting_contacts )
	{
		std::string ldr_str = "*Box resting_contact 00000000 { 0.02 0.02 0.02 }";
		m_ldr_resting_contact[0].UpdateGfx(ldr_str, true);
		m_ldr_resting_contact[1].UpdateGfx(ldr_str, true);
		m_ldr_resting_contact[2].UpdateGfx(ldr_str, true);
		m_ldr_resting_contact[3].UpdateGfx(ldr_str, true);
		m_displaying_resting_contacts = true;
	}
	else if( !Testbed().m_state.m_show_resting_contacts && m_displaying_resting_contacts )
	{
		m_ldr_resting_contact[0].Render(false);
		m_ldr_resting_contact[1].Render(false);
		m_ldr_resting_contact[2].Render(false);
		m_ldr_resting_contact[3].Render(false);
		m_displaying_resting_contacts = false;
	}

	// Now update the transforms
	UpdateGraphics();
}

// Update the transforms for the graphics of this prop
void Prop::UpdateGraphics()
{
	pr::m4x4 o2w = I2W();
	m_prop_ldr.UpdateO2W(o2w);

	if( Testbed().m_state.m_show_sleeping )
	{
		m_prop_ldr.SetSemiTransparent(PhysicsEngine::ObjectIsSleeping(m_object));
	}

	if( m_ldr_velocity )
	{
		pr::v4 vel = PhysicsEngine::ObjectGetVelocity(m_object);
		float fvel = Length3(vel);
		pr::v4 vel_n = (fvel > pr::maths::tiny) ? vel / fvel : pr::v4::make(0.0f, 0.0f, 1.0f, 0.0f);

		pr::m4x4 i2w = pr::OrientationFromDirection4x4(vel_n, 2) * pr::Scale4x4(fvel, fvel, fvel, pr::v4Origin);
		i2w.pos = o2w.pos;
		m_ldr_velocity.UpdateO2W(i2w);
	}
	if( m_ldr_ang_vel )
	{
		pr::v4 ang_vel = PhysicsEngine::ObjectGetAngVelocity(m_object);
		float fang_vel = Length3(ang_vel);
		pr::v4 ang_vel_n = (fang_vel > pr::maths::tiny) ? ang_vel / fang_vel : pr::v4::make(0.0f, 0.0f, 1.0f, 0.0f);

		pr::m4x4 i2w = pr::OrientationFromDirection4x4(ang_vel_n, 2) * pr::Scale4x4(fang_vel, fang_vel, fang_vel, pr::v4Origin);
		i2w.pos = o2w.pos;
		m_ldr_ang_vel.UpdateO2W(i2w);
	}
	if( m_ldr_ang_mom )
	{
		pr::v4 ang_mom = PhysicsEngine::ObjectGetAngMomentum(m_object);
		float fang_mom = Length3(ang_mom);
		pr::v4 ang_mom_n = (fang_mom > pr::maths::tiny) ? ang_mom / fang_mom : pr::v4::make(0.0f, 0.0f, 1.0f, 0.0f);

		pr::m4x4 i2w = pr::OrientationFromDirection4x4(ang_mom_n, 2) * pr::Scale4x4(fang_mom, fang_mom, fang_mom, pr::v4Origin);
		i2w.pos = o2w.pos;
		m_ldr_ang_mom.UpdateO2W(i2w);
	}
	if( m_ldr_ws_bbox )
	{
		pr::BoundingBox bbox = PhysicsEngine::ObjectGetWSBbox(m_object);
		pr::m4x4 bbox2w = pr::m4x4Identity;
		bbox2w.x.x = bbox.m_radius.x * 2.0f;
		bbox2w.y.y = bbox.m_radius.y * 2.0f;
		bbox2w.z.z = bbox.m_radius.z * 2.0f;
		bbox2w.pos = bbox.m_centre;
		m_ldr_ws_bbox.UpdateO2W(bbox2w);
	}
	if( m_ldr_os_bbox )
	{
		pr::BoundingBox bbox = PhysicsEngine::ObjectGetOSBbox(m_object);
		pr::m4x4 bbox2o = pr::m4x4Identity;
		bbox2o.x.x = bbox.m_radius.x * 2.0f;
		bbox2o.y.y = bbox.m_radius.y * 2.0f;
		bbox2o.z.z = bbox.m_radius.z * 2.0f;
		bbox2o.pos = bbox.m_centre;
		pr::m4x4 bbox2w = PhysicsEngine::ObjectToWorld(m_object) * bbox2o;
		m_ldr_os_bbox.UpdateO2W(bbox2w);
	}
	if( m_ldr_com )
	{
		m_ldr_com.UpdateO2W(PhysicsEngine::ObjectToWorld(m_object));
	}
	if( m_ldr_inertia )
	{
		m_ldr_inertia.UpdateO2W(PhysicsEngine::ObjectToWorld(m_object));
	}
	if( m_displaying_resting_contacts )
	{
		pr::v4 contact[4];
		pr::Colour32 colour[4] = {pr::Colour32Red, pr::Colour32Green, pr::Colour32Blue, pr::Colour32Yellow};
		pr::uint i = 0, count;
		PhysicsEngine::ObjectRestingContacts(m_object, contact, 4, count);
		for( ; i != count; ++i )
		{
			ldrSetObjectColour  (m_ldr_resting_contact[i], colour[i]);
			ldrSetObjectPosition(m_ldr_resting_contact[i], contact[i]);
		}
		for( ; i != 4; ++i )
		{
			ldrSetObjectColour(m_ldr_resting_contact[i], pr::Colour32Zero);
		}		
	}
}

