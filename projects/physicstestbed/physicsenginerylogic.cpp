//******************************************
// PhysicsEngine
//******************************************

#include "PhysicsTestbed/Stdafx.h"

#if PHYSICS_ENGINE==RYLOGIC_PHYSICS

#define PH_LDR_OUTPUT 1

#include "pr/common/stdset.h"
#include "physicstestbed/physicsengine.h"
#include "physicstestbed/parseoutput.h"
#include "physicstestbed/collisionmodel.h"
#include "physicstestbed/deformablemodel.h"
#include "physicstestbed/collisioncallbacks.h"

using namespace pr;

namespace pr
{
	namespace ph
	{
		// Physics object
		struct PhysicsObject : ph::Rigidbody
		{
			PhysicsObject(parse::PhysObj const& phys, CollisionModel const& col_model, void* user_data, ph::Engine* engine)
			{
				ph::RigidbodySettings rb_settings;
				rb_settings.m_shape				= col_model.m_shape;
				rb_settings.m_mass_properties	.set(col_model.m_inertia_tensor, v4Origin, phys.m_mass != 0.0f ? phys.m_mass : col_model.m_mass);
				rb_settings.m_object_to_world	= phys.m_object_to_world;
				rb_settings.m_lin_velocity		= phys.m_velocity;
				rb_settings.m_ang_velocity		= phys.m_ang_velocity;
				rb_settings.m_user_data			= user_data;
				rb_settings.m_flags				= ERBFlags_PreCol | ERBFlags_PstCol;
				rb_settings.m_name				= phys.m_name.c_str();
				Rigidbody::Create(rb_settings);

				m_engine = engine;
				m_engine->Register(*this);
			}
			Engine* m_engine;
		};
		typedef std::set<PhysicsObject*> TPhysObj;

		inline Rigidbody const& GetRB(PhysObj const* obj) { return *static_cast<ph::Rigidbody const*>(obj); }
		inline Rigidbody&		GetRB(PhysObj*		 obj) { return *static_cast<ph::Rigidbody*      >(obj); }

		// Collision data interface ************************************
		struct ColData : col::Data
		{
			ColData(ph::Rigidbody const& rbA, ph::Rigidbody const& rbB, ph::ContactManifold const& manifold)
			{
				m_objA = &rbA;
				m_objB = &rbB;
				m_info = &manifold;
			}
			pr::uint NumContacts() const
			{
				return m_info->Size();
			}
			col::Contact GetContact(int obj_index, int contact_index) const
			{
				Contact const& contact = (*m_info)[contact_index];
				if( obj_index == 0 )
				{
					return col::Contact(contact.m_pointA, contact.m_normal, v4Zero, v4Zero, 0);
				/*float sign = -obj_index*2.0f+1.0f;
				return Contact(	mav4_to_v4(phCollisionGetCollisionPointWS (*m_info, obj_index)),
								mav4_to_v4(phCollisionGetCollisionNormalWS(*m_info, obj_index)),
								sign*mav4_to_v4(m_info->ws_impulse),
								mav4_to_v4(phCollisionDeltaVelocityWS(*m_info, obj_index)),
								phCollisionGetPrimId(*m_info, obj_index));*/
				}
				else
				{
					return col::Contact(contact.m_pointB, -contact.m_normal, v4Zero, v4Zero, 0);
				}
			}
		};
	}//namespace ph
}//namespace pr

TPreCollCB g_pre_coll_cb;
TPstCollCB g_pst_coll_cb;

struct PhysicsEnginePrivate : pr::ph::IGravity, pr::ph::IMaterial, pr::ph::IPreCollisionObserver, pr::ph::IPstCollisionObserver
{
	enum { MaxObjects = 100 };
	PhysicsEnginePrivate()
	:m_broadphase()
	,m_terrain()
	,m_engine(&m_broadphase, &m_terrain, this, this)
	,m_material(ph::Material::make(0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f, 0.0f)) // Not set here... Look in ParseOutput.h
	,m_num_objects(0)
	,m_time_step(0.0f)
	{
		ph::RegisterMaterials(this);
		ph::RegisterGravityField(this);
	}

	// Return a physics material from an id
	ph::Material const& GetMaterial(std::size_t) const { return m_material; }

	// Return the 'acceleration' due to gravity at 'position'
	v4 GravityField(v4 const& position) const
	{
		pr::v4 diff, grav = pr::v4Zero;
		for( parse::TGravity::const_iterator i = m_gravity.begin(), i_end = m_gravity.end(); i != i_end; ++i )
		{
			switch( i->m_type )
			{
			default: PR_ASSERT(1, false); break;
			case parse::Gravity::EType_Radial:
				diff = i->m_centre - position;
				if( !FEqlZero3(diff) ) grav += i->m_strength * Normalise3(diff);
				break;
			case parse::Gravity::EType_Directional:
				grav += i->m_direction * i->m_strength;
				break;
			}
		}
		return grav;
	}

	// Returns the potential energy of 'position' in the gravity field
	float GravityPotential(v4 const& position) const
	{
		return -Dot3(GravityField(position), position);
	}

	//void ConvertCollisionData(ph::Rigidbody const& rbA, ph::Rigidbody const& rbB, ph::ContactManifold const& manifold, col::Data& col_data)
	//{
	//	col_data.m_objA = &rbA;
	//	col_data.m_objB = &rbB;
	//	col_data.m_num_contacts = 0;
	//	for( uint i = 0; i != manifold.Size(); ++i )
	//	{
	//		ph::Contact const& cnt = manifold[i];
	//		col::Contact contactA, contactB;
	//		contactA.m_ws_point		=  cnt.m_pointA;
	//		contactB.m_ws_point		=  cnt.m_pointB;
	//		contactA.m_ws_normal	=  cnt.m_normal;
	//		contactB.m_ws_normal	= -cnt.m_normal;
	//		contactA.m_ws_impulse	=  v4Zero;
	//		contactB.m_ws_impulse	= -v4Zero;
	//		contactA.m_prim_id		= 0;
	//		contactB.m_prim_id		= 0;
	//		col_data.m_contactsA.push_back(contactA);
	//		col_data.m_contactsB.push_back(contactB);
	//		col_data.m_num_contacts++;
	//	}
	//}
	// Collision watch
	bool NotifyPreCollision(ph::Rigidbody const& rbA, ph::Rigidbody const& rbB, ph::ContactManifold& manifold)
	{
		ph::ColData col_data(rbA, rbB, manifold);
		bool collide = true;
		for( TPreCollCB::iterator fn = g_pre_coll_cb.begin(), fn_end = g_pre_coll_cb.end(); fn != fn_end; ++fn )
		{
			collide &= (*fn)(col_data);
		}
		return collide;
	}
	void NotifyPstCollision(ph::Rigidbody const& rbA, ph::Rigidbody const& rbB, ph::ContactManifold const& manifold)
	{
		ph::ColData col_data(rbA, rbB, manifold);
		for( TPstCollCB::iterator fn = g_pst_coll_cb.begin(), fn_end = g_pst_coll_cb.end(); fn != fn_end; ++fn )
		{
			(*fn)(col_data);
		}
	}

	//ph::BPBruteForce	m_broadphase;		// The broadphase system to use
	ph::BPSweepAndPrune	m_broadphase;		// The broadphase system to use
	//ph::TerrainImplicitSurf m_terrain;
	ph::TerrainPlane	m_terrain;			// The terrain system for the world
	ph::Engine			m_engine;			// This is the physics world
	ph::Material		m_material;			// The material to use
	std::size_t			m_num_objects;
	parse::TGravity		m_gravity;			// Sources of gravity
	float				m_time_step;
};

struct PolytopeBuilder : pr::tetramesh::IPolytopeGenerator
{
	typedef std::vector<v4>					TVerts;
	typedef std::vector<ph::ShapePolyFace>	TFaces;
	ph::ShapeBuilder*	m_shape_builder;
	TVerts				m_verts;
	TFaces				m_faces;

	PolytopeBuilder(ph::ShapeBuilder& shape_builder) : m_shape_builder(&shape_builder) {}
	void BeginPolytope()
	{
		m_verts.clear();
		m_faces.clear();
	}
	void AddPolytopeVert(v4 const& vertex)
	{
		m_verts.push_back(vertex);
	}
	void AddPolytopeFace(tetramesh::VIndex a, tetramesh::VIndex b, tetramesh::VIndex c)
	{
		ph::ShapePolyFace face = {static_cast<ph::PolyIdx>(a), static_cast<ph::PolyIdx>(b), static_cast<ph::PolyIdx>(c), 0};
		m_faces.push_back(face);
	}
	void EndPolytope()
	{
		ph::ShapePolytopeHelper	poly_helper;
		m_shape_builder->AddShape(poly_helper.set(&m_verts[0], m_verts.size(), &m_faces[0], m_faces.size(), m4x4Identity, 0, ph::EShapeFlags_None));
	}
};

ph::TPhysObj g_objects;	// The physics objects

// Construction
PhysicsEngine::PhysicsEngine()
:m_data(new PhysicsEnginePrivate())
{
	m_frame_number = 0;
	SetMaterial(parse::Material());
}

// Destruction
PhysicsEngine::~PhysicsEngine()
{
	delete m_data;
}

// Bring recently added objects up to date in the physics engine. (reflections only)
void PhysicsEngine::Sync()
{
}

// Advance the physics engine
void PhysicsEngine::Step()
{
	PR_PROFILE_FRAME_BEGIN;
	
	++m_frame_number;
	m_data->m_engine.Step(m_data->m_time_step);
	
	PR_PROFILE_FRAME_END;
	PR_PROFILE_OUTPUT(120);
}

// Set the step size 
void PhysicsEngine::SetTimeStep(float step_size_in_seconds)
{
	m_data->m_time_step = step_size_in_seconds;
}

// Return the current frame number
unsigned int PhysicsEngine::GetFrameNumber() const
{
	return m_frame_number;
}

// Empty the registered model lists, and quad trees
void PhysicsEngine::Clear()
{
	m_frame_number = 0;
}

// Return the maximum number of objects allowed in the physics engine
std::size_t PhysicsEngine::GetMaxObject() const
{
	return PhysicsEnginePrivate::MaxObjects; // No real limit actually
}

// Return the number of physics objects in the physics engine
std::size_t PhysicsEngine::GetNumObjects() const
{
	return m_data->m_num_objects;
}

// Set the physics material
void PhysicsEngine::SetMaterial(const parse::Material& material)
{
	m_data->m_material.m_density				= material.m_density;
	m_data->m_material.m_static_friction		= material.m_static_friction;
	m_data->m_material.m_dynamic_friction		= material.m_dynamic_friction;
	m_data->m_material.m_rolling_friction		= material.m_rolling_friction;
	m_data->m_material.m_elasticity				= material.m_elasticity;
	m_data->m_material.m_tangential_elasticity	= material.m_tangential_elasiticity;
	m_data->m_material.m_tortional_elasticity	= material.m_tortional_elasticity;
}

// Add a gravity source to the engine
void PhysicsEngine::AddGravityField(const parse::Gravity& gravity)
{
	m_data->m_gravity.push_back(gravity);
}
void PhysicsEngine::ClearGravityFields()
{
	m_data->m_gravity.clear();
}

// Set the terrain to use the default terrain plane
void PhysicsEngine::SetDefaultTerrain()
{}

// Set the terrain to use the data contained in 'terrain'
void PhysicsEngine::SetTerrain(parse::Terrain const& terrain)
{
	terrain; // ToDo
}

// Return the dimensions of the terrain area
void PhysicsEngine::GetTerrainDimensions(float& terr_x, float& terr_z, float& terr_w, float& terr_d)
{
	terr_x = -10.0f;
	terr_z = -10.0f;
	terr_w = 20.0f;
	terr_d = 20.0f;
}

// Sample the terrain at a point
void PhysicsEngine::SampleTerrain(pr::v4 const& point, float& height, pr::v4& normal)
{
	pr::ph::terrain::Sample sample;
	sample.m_point = point;
	sample.m_radius = 0.0f;
	pr::ph::terrain::ResultHelper<1> result;
	m_data->m_terrain.CollideSpheresFtr(&sample, 1, result);
	if( result.m_num_results > 0 )
	{
		height = result.m_result[0].m_terrain_point.y;
		normal = result.m_result[0].m_normal;
	}
	else
	{
		height = point.y;
		normal = pr::v4YAxis;
	}
}

// Cast a ray in the physics engine
void PhysicsEngine::CastRay(pr::v4 const& point, pr::v4 const& direction, float& intercept, pr::v4& normal, PhysObj*& hit_object, pr::uint& prim_id)
{
	//{// HACK
	//	struct TestCase { v4 s, e; float thk; int result; }; // result: 0=false, 1=true, 2=don'tcare
	//	TestCase test_cases[] =
	//	{
	//		{{-1.0f,  0.0f, 0.0f, 1.0f},  {1.0f, 0.0f, 0.0f, 1.0f}, 0.0f, 1},
	//		{{-1.0f,  0.0f, 0.0f, 1.0f}, {-2.0f, 0.0f, 0.0f, 1.0f}, 0.0f, 1},
	//		{{ 0.6f,  0.55f, 0.0f, 1.0f}, {-0.6f, 0.55f, 0.0f, 1.0f}, 0.0f, 0},
	//		{{-0.6f,  0.49f, 0.0f, 1.0f}, {0.6f, 0.49f, 0.0f, 1.0f}, 0.0f, 1},
	//		{{ 0.0f,  0.0f, 0.0f, 1.0f}, {0.0f, 1.0f, 0.0f, 1.0f}, 0.0f, 1},
	//		{{ 0.0f,  0.0f, 0.0f, 1.0f}, {1.0f, 0.0f, 0.0f, 1.0f}, 0.0f, 1},
	//		{{ 0.0f,  0.0f, 0.0f, 1.0f}, {1.0f, 1.0f, 1.0f, 1.0f}, 0.0f, 1},
	//		{{ 0.0f,  0.0f, 0.0f, 1.0f}, {-1.0f, -1.0f, -1.0f, 1.0f}, 0.0f, 1},
	//		{{-1.0f, -1.0f, -1.0f, 1.0f}, { 0.0f,  0.0f, 0.0f, 1.0f}, 0.0f, 1},
	//		{{-0.5f,  0.49f, -0.5f, 1.0f}, {0.5f, 0.49f, 0.5f, 1.0f}, 0.0f, 1},
	//		{{-0.5f,  0.49f,  0.5f, 1.0f}, {0.5f, 0.49f, -0.5f, 1.0f}, 0.0f, 1},
	//		{{-0.5f, -0.5f, -0.5f, 1.0f}, {0.5f, 0.5f, 0.5f, 1.0f}, 0.0f, 2},
	//		{{-0.6f, -0.6f, -0.6f, 1.0f}, {0.6f, 0.6f, 0.6f, 1.0f}, 0.0f, 2},
	//		{{-0.5f,  0.5f, -0.5f, 1.0f}, {0.5f, 0.5f, 0.5f, 1.0f}, 0.0f, 2},
	//		{{-0.5f,  0.5f, 0.5f, 1.0f}, {0.5f,  0.5f, 0.5f, 1.0f}, 0.0f, 2},
	//		{{ 0.5001f,  0.499f, 0.499f, 1.0f}, {0.499f, 0.499f, 0.5001f, 1.0f}, 0.0f, 1},
	//		{{ 0.499f,  0.5001f, 0.499f, 1.0f}, {0.5001f, 0.5001f, 0.5001f, 1.0f}, 0.0f, 0},
	//		{{-0.6f,  0.499f, 0.499f, 1.0f}, {0.5f,  0.500f, 0.500f, 1.0f}, 0.0f, 1},
	//		{{-0.6f,  0.5001f, 0.5001f, 1.0f}, {0.5f,  0.500f, 0.500f, 1.0f}, 0.0f, 0},
	//		{{-0.6f,  0.5f, 0.5f, 1.0f}, {0.5f,  0.5f, 0.5f, 1.0f}, 0.0f, 2},
	//	};
	//	int num_test_cases = sizeof(test_cases) / sizeof(TestCase);
	//	for( int i = 0; i != num_test_cases; ++i )
	//	{
	//		TestCase const& test = test_cases[i];
	//		pr::ph::RayCastResult result;
	//		pr::ph::Ray ray;
	//		ray.m_point     = v4::make(0.359253f,   0.304604f,  1.322510f, 1.0f);
	//		ray.m_direction = v4::make(-0.312110f, -0.211668f, -0.926167f, 0.0f);
	//		ray.m_thickness = 0.15f;
	//		//ray.m_point     = test.s;
	//		//ray.m_direction = test.e - test.s;
	//		//ray.m_thickness = test.thk;
	//		pr::ph::PhysicsObject* phys_obj = *g_objects.begin();
	//		pr::ph::ShapePolytope& poly = *pr::ph::shape_cast<pr::ph::ShapePolytope>(phys_obj->m_base.m_shape);
	//		if( pr::ph::RayCast(ray, poly, result) )
	//		{
	//			PR_ASSERT(PR_DBG_PHYSICS, test.result == 2 || test.result == 1);
	//			intercept = result.m_t0;
	//			normal	  = result.m_normal;
	//		}
	//		else
	//		{
	//			PR_ASSERT(PR_DBG_PHYSICS, test.result == 2 || test.result == 0);
	//			intercept = 1.0f;
	//		}
	//	}
	//	return;
	//}

	pr::ph::Ray ray;
	ray.m_point     = point;
	ray.m_direction = direction;
	ray.m_thickness = 0.0f;
	pr::ph::RayVsWorldResult result;

	// Test a ray against the world
	if( m_data->m_engine.RayCast(ray, result) )
	{
		intercept	= result.m_intercept;
		normal		= result.m_normal;
		hit_object	= (PhysObj*)result.m_object;
		prim_id		= 0;//result.m_shape;
	}
	else
	{
		intercept	= 1.0f;
		hit_object	= 0;
		prim_id		= 0;
	}
}

// Create a graphic for the terrain sampler
std::string PhysicsEngine::CreateTerrainSampler(pr::v4 const& point)
{point;
	return "";
}

// Clear the static scene data
void PhysicsEngine::ClearStaticSceneData()
{
}

// Create a collision model in the 
void PhysicsEngine::CreateStaticCollisionModel(parse::Model const& model, CollisionModel& col_model, std::string& ldr_string)
{model;col_model;ldr_string;
}

void PhysicsEngine::RebuildStaticScene(TStatic const& statics, pr::BoundingBox const& world_bounds)
{statics;world_bounds;
}

// Create a collision model. Creates 'col_model' and modifies 'model' as if
// it was a description of the model given in inertial frame
void PhysicsEngine::CreateCollisionModel(parse::Model const& model, CollisionModel& col_model)
{
	ph::ShapeBuilder builder;
	for( parse::TPrim::const_iterator p = model.m_prim.begin(), p_end = model.m_prim.end(); p != p_end; ++p )
	{
		parse::Prim const& prim = *p;
		switch( prim.m_type )
		{
		case parse::Prim::EType_Sphere:
			builder.AddShape(ph::ShapeSphere::make(prim.m_radius.x, prim.m_prim_to_model, 0, ph::EShapeFlags_None));
			break;
		case parse::Prim::EType_Cylinder:
			builder.AddShape(ph::ShapeCylinder::make(prim.m_radius.x, prim.m_radius.y, prim.m_prim_to_model, 0, ph::EShapeFlags_None));
			break;
		case parse::Prim::EType_Box:
			builder.AddShape(ph::ShapeBox::make(prim.m_radius, prim.m_prim_to_model, 0, ph::EShapeFlags_None));
			break;
		case parse::Prim::EType_Polytope:
			{
				ph::ShapePolytopeHelper helper;
				builder.AddShape(helper.set(&prim.m_vertex[0], prim.m_vertex.size(), prim.m_prim_to_model, 0, ph::EShapeFlags_None));
			}break;
		case parse::Prim::EType_PolytopeExplicit:
			{
				ph::ShapePolytopeHelper helper;
				parse::TIndices::const_iterator i = prim.m_face.begin();
				std::vector<pr::ph::ShapePolyFace> faces(prim.m_face.size() / 3);
				for( std::vector<pr::ph::ShapePolyFace>::iterator f = faces.begin(), f_end = faces.end(); f != f_end; ++f )
				{
					f->m_index[0] = static_cast<pr::ph::PolyIdx>(*i++);
					f->m_index[1] = static_cast<pr::ph::PolyIdx>(*i++);
					f->m_index[2] = static_cast<pr::ph::PolyIdx>(*i++);
				}
				builder.AddShape(helper.set(&prim.m_vertex[0], prim.m_vertex.size(), &faces[0], faces.size(), prim.m_prim_to_model, 0, ph::EShapeFlags_None));
			}break;
		case parse::Prim::EType_Triangle:
			builder.AddShape(ph::ShapeTriangle::make(prim.m_vertex[0], prim.m_vertex[1], prim.m_vertex[2], prim.m_prim_to_model, 0, ph::EShapeFlags_None));
			break;
		default:PR_ASSERT(PR_DBG_COMMON, false);
		}
	}

	v4 model_to_CoMframe;
	ph::MassProperties mp;
	col_model.m_shape = builder.BuildShape(col_model.m_buffer, mp, model_to_CoMframe);
	col_model.m_inertia_tensor		= mp.m_os_inertia_tensor;
	col_model.m_model_to_CoMframe	.identity();
	col_model.m_model_to_CoMframe.pos = v4Origin - model_to_CoMframe; // (the point (0,0,0) in model space becomes -model_to_CoMframe in inertial frame)
	col_model.m_CoMframe_to_model	= GetInverseFast(col_model.m_model_to_CoMframe);
	col_model.m_ms_bbox				= col_model.m_shape->m_bbox;
	col_model.m_mass				= mp.m_mass;
}

// Create a deformable collision model
void PhysicsEngine::CreateDeformableModel(parse::Deformable const& deformable, DeformableModel& def_model)
{deformable;def_model;
	//using namespace tetramesh;
	//using namespace deformable;

	//std::size_t num_verts = deformable.m_vertex.size();
	//std::size_t num_tetra = deformable.m_tetras.size() / 4;
	//std::vector<DefVData>	vdata(num_verts);

	//// Set up per vert data
	//for( std::vector<DefVData>::iterator i = vdata.begin(), i_end = vdata.end(); i != i_end; ++i )
	//{
	//	i->m_max_displacement = maths::float_max;
	//}

	//def_model.m_model = &deformable::Create(num_verts, &deformable.m_vertex[0], &vdata[0], num_tetra, &deformable.m_tetras[0], def_model.m_buffer);
}

// Return a dynamic physics object
void PhysicsEngine::CreatePhysicsObject(parse::PhysObj const& phys, CollisionModel const& col_model, void* user_data, PhysObj*& phys_obj)
{
	ph::PhysicsObject* obj = new ph::PhysicsObject(phys, col_model, user_data, &m_data->m_engine);
	g_objects.insert(obj);
	phys_obj = obj;
	++m_data->m_num_objects;
}

// Delete a physics object
void PhysicsEngine::DeletePhysicsObject(PhysObj*& phys_obj)
{
	ph::PhysicsObject* obj = static_cast<ph::PhysicsObject*>(phys_obj);
	ph::TPhysObj::iterator i = g_objects.find(obj);
	if( i != g_objects.end() )
	{
		g_objects.erase(i);
		delete obj;
	}
	phys_obj = 0;
	--m_data->m_num_objects;
}

// Make a ldr string representation of a collision model
std::string PhysicsEngine::MakeLdrString(std::string const& name, pr::Colour32 colour, CollisionModel const& col_model)
{
	std::string str;
	ldr::PhShape(name.c_str(), colour, *col_model.m_shape, m4x4Identity, str);
	return str;
}

// Make a custom line drawer object for a deformable
void PhysicsEngine::MakeLdrObjectDeformable(pr::rdr::Model* model, pr::BoundingBox& bbox, void* user_data, pr::rdr::MaterialManager&)
{
	model;bbox;user_data;
}

// Make a custom line drawer object
void PhysicsEngine::MakeLdrObject(pr::rdr::Model* model, pr::BoundingBox& bbox, void* user_data, pr::rdr::MaterialManager&)
{
	model; bbox; user_data; 
}

// Return the object to world transform for a physics object
pr::m4x4 PhysicsEngine::ObjectToWorld(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).ObjectToWorld();
}

// Return the velocity of a physics object
pr::v4 PhysicsEngine::ObjectGetVelocity(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).Velocity();
}

// Return the angular velocity of a physics object
pr::v4 PhysicsEngine::ObjectGetAngVelocity(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).AngVelocity();
}

// Return the anguluar momentum of a physics object
pr::v4 PhysicsEngine::ObjectGetAngMomentum(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).AngMomentum();
}

// Return the ws bounding box for a physics object
pr::BoundingBox	PhysicsEngine::ObjectGetWSBbox(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).BBoxWS();
}

// Return the os bounding box for a physics object
pr::BoundingBox	PhysicsEngine::ObjectGetOSBbox(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).BBoxOS();
}

// Return the pre-collision call back data
void* PhysicsEngine::ObjectGetPreColData(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).UserData();
}

// Return the post-collision call back data
void* PhysicsEngine::ObjectGetPstColData(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).UserData();
}

// Return the mass of a physics object
float PhysicsEngine::ObjectGetMass(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).Mass();
}

// Return the object space
pr::m3x3 PhysicsEngine::ObjectGetOSInertia(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).InertiaOS();
}

// Return the motion type of an object
EPhysObjType PhysicsEngine::ObjectGetPhysObjType(PhysObj const* phys_obj)
{
	ph::Rigidbody const& rb = ph::GetRB(phys_obj);
	switch( rb.Type() )
	{
	case ph::ERigidbody_Dynamic:		return EPhysObjType_Dynamic;
	case ph::ERigidbody_Static:			return EPhysObjType_Static;
	case ph::ERigidbody_Terrain:		return EPhysObjType_Terrain;
	default: PR_ASSERT(1, false);		return EPhysObjType_Dynamic;
	}
}

// Return the points of resting contact
void PhysicsEngine::ObjectRestingContacts(PhysObj const* phys_obj, pr::v4* contacts, pr::uint, pr::uint& count)
{
	ph::GetRB(phys_obj).RestingContacts(contacts, count);
}

// Return true if an object is asleep
bool PhysicsEngine::ObjectIsSleeping(PhysObj const* phys_obj)
{
	return ph::GetRB(phys_obj).SleepState();
}

// Set the object to world transform
void PhysicsEngine::SetObjectToWorld(PhysObj* phys_obj, pr::m4x4 const& o2w)
{
	ph::GetRB(phys_obj).SetObjectToWorld(o2w);
}

// Set the gravity vector for a physics object
void PhysicsEngine::ObjectSetGravity(PhysObj*)
{
	// Do nothing, the gravity is updated via call back
}

// Set the velocity of a physics object
void PhysicsEngine::ObjectSetVelocity(PhysObj* phys_obj, pr::v4 const& vel)
{
	ph::GetRB(phys_obj).SetVelocity(vel);
}

// Set the ang velocity of a physics object
void PhysicsEngine::ObjectSetAngVelocity(PhysObj* phys_obj, pr::v4 const& ang_vel)
{
	ph::GetRB(phys_obj).SetAngVelocity(ang_vel);
}

// Wake a physics object up
void PhysicsEngine::ObjectWakeUp(PhysObj* phys_obj)
{
	ph::GetRB(phys_obj).SetSleepState(false);
}

// Apply an impulse to a physics object
void PhysicsEngine::ObjectApplyImpulse(PhysObj* phys_obj, pr::v4 const& ws_impulse, pr::v4 const& ws_pos)
{
	ph::GetRB(phys_obj).ApplyWSImpulse(ws_impulse, ws_pos - ph::GetRB(phys_obj).Position());
}

// Update the collision model of a physics object
void PhysicsEngine::ObjectSetColModel(PhysObj* phys_obj, CollisionModel const& col_model, m4x4 const& o2w)
{
	//pr::ph::MassProperties mp;
	//mp.m_centre_of_mass		= pr::v4Zero;
	//mp.m_mass					= col_model.m_mass;
	//mp.m_os_inertia_tensor	= col_model.m_inertia_tensor;
	//ph::SetCollisionShape(ph::GetRB(phys_obj), col_model.m_shape, col_model.m_ms_bbox, mp);
	ph::GetRB(phys_obj).SetCollisionShape(col_model.m_shape, o2w);
}

//// Decompose a deformable mesh
//void PhysicsEngine::Decompose(DeformableModel& deform, CollisionModel& col_model, float convex_tolerance)
//{
//	ph::ShapeBuilder	shape_builder;
//	PolytopeBuilder		poly_builder(shape_builder);
//	tetramesh::Decompose(deform.m_model->m_tetra_mesh, poly_builder, convex_tolerance);
//
//	v4 model_to_CoMframe;
//	ph::MassProperties mp;
//	col_model.m_shape = shape_builder.BuildShape(col_model.m_buffer, mp, model_to_CoMframe);
//
//	col_model.m_inertia_tensor		= mp.m_os_inertia_tensor;
//	col_model.m_model_to_CoMframe	.Identity();
//	col_model.m_model_to_CoMframe.pos = v4Origin - model_to_CoMframe; // (the point (0,0,0) in model space becomes -model_to_CoMframe in inertial frame)
//	col_model.m_CoMframe_to_model	= col_model.m_model_to_CoMframe.GetInverseFast();
//	col_model.m_ms_bbox				= col_model.m_shape->m_bbox;
//	col_model.m_mass				= mp.m_mass;
//}

//// Deform a deformable mesh
//void PhysicsEngine::Deform(DeformableModel& deform, m4x4 const& shape, float plasticity, float min_volume)
//{
//	deformable::Deform(*deform.m_model, shape, plasticity, min_volume);
//}
//
//// Deform a collision model
//bool PhysicsEngine::Deform(CollisionModel& col_model, pr::uint prim_id, pr::v4 const& point, pr::v4 const& normal, pr::v4 const& impulse)
//{
//	col_model;prim_id;point;normal;impulse;
//	return false;
//}

void PhysicsEngine::DeformableTransform(DeformableModel& deform, pr::m4x4 const& transform)
{
	deform;transform;
}

void PhysicsEngine::DeformableImpact(DeformableModel& deform, pr::v4 const& point, pr::v4 const& normal, pr::v4 const& delta_vel)
{
	deform;point;normal;delta_vel;
}
bool PhysicsEngine::DeformableEvolve(DeformableModel& deform, float step_size, bool to_equilibrium)
{
	deform;step_size;to_equilibrium;
	return true;
}
void PhysicsEngine::DeformableDecompose(DeformableModel& deform, CollisionModel& col_model)
{
	deform;col_model;
}

// Create a multibody by attaching 'phys_obj' to 'parent'
// if 'parent' is 0, then phys_obj is the root of a multibody
void PhysicsEngine::MultiAttach(PhysObj* phys_obj, PhysObj* parent, parse::Multibody const& multi_info)
{
	phys_obj;parent;multi_info;
}

// Break all the links in a multibody leaving rigid bodies
void PhysicsEngine::MultiBreak(PhysObj* phys_obj)
{
	phys_obj;
}

//// Return all of the points of collision for a pair
//pr::uint PhysicsEngine::ColGetCollisionPointsWS(ColInfo const* collision_info, pr::v4* point, pr::v4* normal, pr::uint max_points)
//{
//	ph::ContactManifold const& manifold = *static_cast<ph::ContactManifold const*>(collision_info);
//	uint num_points = pr::Minimum(manifold.Size(), max_points);
//	for( pr::uint i = 0; i != num_points; ++i )
//	{
//		ph::Contact const& contact = manifold[i];
//		point[i]  = contact.m_pointA;
//		normal[i] = contact.m_normal;
//	}
//	return num_points;
//}

//// Return the elasticity used in resolving the collision
//float PhysicsEngine::ColGetElasticity(int, ColInfo const* /*collision_info*/)
//{
//	//ph::ContactManifold const& manifold = *static_cast<ph::ContactManifold const*>(collision_info);
//	return 0.0f;//manifold[0].m_elasticity_n;
//}

//// Return the major axis that cylinder use
//int PhysicsEngine::GetCylinderMajorAxis()
//{
//	return 2;
//}

#endif
