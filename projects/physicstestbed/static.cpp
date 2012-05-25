//******************************************
// Static
//******************************************
#include "PhysicsTestbed/Stdafx.h"
#include "PhysicsTestbed/Static.h"
#include "PhysicsTestbed/Parser.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "PhysicsTestbed/PhysicsEngine.h"

Static::Static(parse::Output const& output, parse::Static const& statik, PhysicsEngine& engine)
:m_static(statik)
{
	PR_ASSERT(1, m_static.m_model_index != 0xFFFFFFFF);
	parse::Model model = output.m_models[m_static.m_model_index];
	if( m_static.m_colour != pr::Colour32Black )							model.m_colour = m_static.m_colour;
	if( model.m_colour    == pr::Colour32Black && !model.m_prim.empty() )	model.m_colour = model.m_prim.begin()->m_colour;

	// Create a collision model and a ldr string for it
	std::string str;
	engine.CreateStaticCollisionModel(model, m_col_model, str);

	// Register the line drawer object
	//std::string str = PhysicsEngine::MakeLdrString(m_model.m_name, m_model.m_colour, col_model);
	m_ldr = ldrRegisterObject(str.c_str(), str.size());
	ldrSetObjectUserData(m_ldr, this);
	ldrSetObjectTransform(m_ldr, InstToWorld());
}

Static::~Static()
{
	ldrUnRegisterObject(m_ldr);
}
