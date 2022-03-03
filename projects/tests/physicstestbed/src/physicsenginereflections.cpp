//******************************************
// PhysicsEngine
//******************************************

#include "PhysicsTestbed/Stdafx.h"

#if PHYSICS_ENGINE==REFLECTIONS_PHYSICS

#include "pr/filesys/filesys.h"
#include "pr/filesys/fileex.h"
#include "Shared/Source/CrossPlatform/Terrain/TerrainV2.h"
#include "Shared/Source/CrossPlatform/Terrain/TerrainSearch.h"
#include "Shared/Source/CrossPlatform/Terrain/Terrain3D.h"
#include "PhysicsTestbed/PhysicsEngine.h"
#include "PhysicsTestbed/PhysicsTestbed.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "PhysicsTestbed/CollisionModel.h"
#include "PhysicsTestbed/DeformableModel.h"
#include "PhysicsTestbed/Skeleton.h"

#define PHYSICS_BUILD // hack so we can draw these
#include "Physics/Deformation/PHdeformableMeshData.h"
#include "Physics/Model/PHprimitive.h"
#include "Physics/Deformation/PHtetrameshInterface.h"
#include "Physics/Utility/PHbyteData.h"
#include "Physics/Utility/PHdebug.h"
#undef PHYSICS_BUILD

using namespace pr;

#include "TaskScheduler/SETTINGS.h"
RI_TASK_SCHEDULER_SETTINGS(5, 65536)

// Collision data interface ************************************
namespace col
{
	struct ColData : Data
	{
		ColData(PHobject* objA, PHobject* objB, PHcollisionFrameInfo* info)
		{
			m_objA = objA;
			m_objB = objB;
			m_info = info;
		}
		pr::uint NumContacts() const
		{
			return 1;
		}
		Contact GetContact(int obj_index, int) const
		{
			float sign = -obj_index*2.0f+1.0f;
			return Contact(	mav4_to_v4(phCollisionGetCollisionPointWS (*m_info, obj_index)),
							mav4_to_v4(phCollisionGetCollisionNormalWS(*m_info, obj_index)),
							sign*mav4_to_v4(m_info->ws_impulse),
							mav4_to_v4(phCollisionDeltaVelocityWS(*m_info, obj_index)),
							phCollisionGetPrimId(*m_info, obj_index));
		}
	};
}

// Collision call backs ************************************
TPreCollCB g_pre_coll_cb;
TPstCollCB g_pst_coll_cb;
// [in]  'objA' - an object involved in the collision. Corresponding to object[0] in the collision frame info
// [in]  'objB' - an object involved in the collision. Corresponding to object[1] in the collision frame info
// [in]  'info' - Information about the collision.
// Return 'PH_collide' to allow the collision resolution to continue, 'PH_doNotCollide' to ignore the collision
// Note: objB may be null if the collision is caused by a non-physical object (e.g. an explosion)
// Note: 'info' may be null when objA and objB are in collision groups with 'ECollisionGroupResponse_Detect'
PHstatus ReflectionsPreCollisionCallBack(PHobject* objA, PHobject* objB, const PHcollisionFrameInfo* info, PHcollisionFrameInfo* collisionFrameInfoOut)
{
	*collisionFrameInfoOut = *info;

	PHstatus result = PH_collide;
	col::ColData col_data(objA, objB, collisionFrameInfoOut);
	for( TPreCollCB::iterator fn = g_pre_coll_cb.begin(), fn_end = g_pre_coll_cb.end(); fn != fn_end; ++fn )
	{
		if( !(*fn)(col_data) ) result = PH_doNotCollide;
	}
	return result;
}

// [in]  'objA' - an object involved in the collision. Corresponding to object[0] in the collision frame info
// [in]  'objB' - an object involved in the collision. Corresponding to object[1] in the collision frame info
// [in]  'info' - Information about the collision
// [in]  'impulse' - The world space impulse applied to objA (-impulse is applied to objB)
// Note: objB may be null if the collision is caused by a non-physical object (e.g. an explosion)
void ReflectionsPstCollisionCallBack(PHobject* objA, PHobject* objB, PHcollisionFrameInfo& info, PHv4ref)
{
	col::ColData col_data(objA, objB, &info);
	for( TPstCollCB::iterator fn = g_pst_coll_cb.begin(), fn_end = g_pst_coll_cb.end(); fn != fn_end; ++fn )
	{
		(*fn)(col_data);
	}
}

// Materials ************************************
PHmaterialData g_material_data;
void MaterialCallBack(PHint, PHmaterialData& material_data)
{
	material_data = g_material_data;
}

// Gravity sources ************************************
struct GravSources
{
	parse::TGravity m_gravity;				// Sources of gravity
	PHv4 GetGravity(PHv4 const& position) const
	{
		PHv4 diff, grav; grav.setZero();
		for( parse::TGravity::const_iterator i = m_gravity.begin(), i_end = m_gravity.end(); i != i_end; ++i )
		{
			switch( i->m_type )
			{
			default: PR_ASSERT(1, false); break;
			case parse::Gravity::EType_Radial:
				diff = v4_to_mav4(i->m_centre) - position;
				if( !diff.isZero() ) grav += i->m_strength * (diff.getNormal3(diff), diff);
				break;
			case parse::Gravity::EType_Directional:
				grav += v4_to_mav4(i->m_direction) * i->m_strength;
				break;
			}
		}
		return grav;
	}	
} g_grav_sources;

// Terrain call back ************************************
void ReflectionsDefaultTerrainCallBack(PHv4ref, PHterrainSample& terrain_sample)
{
	terrain_sample.m_height			= 0.0f;
	terrain_sample.m_material_index = 0;
	terrain_sample.m_normal			.set(0.0f, 1.0f, 0.0f, 0.0f);
	terrain_sample.m_water_height	= -500.0f;
}

// Read thd terrain data
terrain::Header const* g_terrain;
void ReflectionsTerrainCallBack(PHv4ref position, PHterrainSample& terrain_sample)
{
	terrain::SingleHeightLookup query(position[1]);
	terrain::Query(*g_terrain, position[0], position[2], query);
	terrain_sample.m_height			= query.m_height;
	terrain_sample.m_water_height	= query.m_water_height;
	terrain_sample.m_material_index	= query.m_material_id;
	terrain_sample.m_normal			= query.m_plane;
	terrain_sample.m_normal			.setW0();
}

// Test terrain for the 4-terrains system for DriverWii
terrain::Header const* g_terrain3D[4];
void ReflectionsTerrain3DCallBack(PHv4ref position, PHuint projection_mask, PHterrain3DSample& terrain_sample)
{
	#if RI_PH_DEBUG_TERRAIN
	std::string str;
	ldr::Box("query_point", "FFFFFFFF", mav4_to_v4(position), 0.2f, str);
	char const* colour[4] = {"FFFF0000", "FF00FF00", "FF0000FF", "FFFFFF00"};
	#endif

	terrain_sample.m_point			= position;
	terrain_sample.m_down			.setAsDirection(0.0f, -1.0f, 0.0f);
	terrain_sample.m_surface_point	.setAsPosition(position[0], Terrain_DefaultHeight, position[2]);
	terrain_sample.m_surface_normal	.setAsDirection(0.0f, 1.0f, 0.0f);
	terrain_sample.m_depth			= Terrain_DefaultHeight - position[1];
	terrain_sample.m_material_index	= 0;
	terrain_sample.m_projection		= 0;

	float nearest = MA_FLT_MAX;
	for (int r = 0; r != 1; ++r) // for each region
	{
		// Bounding box test the query point for being within the region 
		const float terrain_bbox_tolerance = 10.0f;
		if (!terrain::PointIsWithin(*g_terrain3D[0], position[0], position[2], terrain_bbox_tolerance))
			continue;

		for( int j = 0; j != terrain::NumProjections; ++j )
		{
			// Only consider projections specified in the mask
			if (((1 << j) & projection_mask) == 0) continue;

			// Find the region position in projected region space
			MAv4 pos = terrain::ProjectionTransform(j) * position;

			terrain::SingleHeightLookup query(pos[1]);
			terrain::Query(*g_terrain3D[j], pos[0], pos[2], query);
			float dist = query.m_query_height - query.m_height;

			RI_S_ASSERTSONLY(RI_PH_DEBUG_TERRAIN, ldr::Line(FmtS("result_%d", j), colour[j], mav4_to_v4(position), mav4_to_v4(position - dist * terrain::ProjectionTransformInv(j)[1]), str));
			RI_S_ASSERTSONLY(RI_PH_DEBUG_TERRAIN, ldr::Box(FmtS("result_%d", j), colour[j], mav4_to_v4(position - dist * terrain::ProjectionTransformInv(j)[1]), 0.2f, str));

			const float Terrain_Thickness = 4.0f;
			if (dist > nearest || dist < -Terrain_Thickness || query.m_height == Terrain_DefaultHeight)
				continue;
			
			// Record the nearest terrain point
			nearest = dist;
			PHv4 dir = terrain::ProjectionTransformInv(j)[1];
			PHv4 surf_norm = query.m_plane; surf_norm.setW0();
			surf_norm = terrain::ProjectionTransformInv(j) * surf_norm;

			terrain_sample.m_point			= position;
			terrain_sample.m_down			= -dir;
			terrain_sample.m_surface_normal	= surf_norm;
			terrain_sample.m_surface_point	= position - dist * dir;
			terrain_sample.m_depth			= -dist;
			terrain_sample.m_material_index = query.m_material_id;
			terrain_sample.m_projection		= 1 << j;
		}
	}
	RI_S_ASSERTSONLY(RI_PH_DEBUG_TERRAIN, pr::StringToFile(str, "D:/deleteme/terrain_query.pr_script"));
}

// Physics engine ************************************
struct PhysicsEnginePrivate : BPinstanceState
{
	PhysicsEnginePrivate()
	{
		// Create the physics engine
		m_engine_info.m_max_objects					= 100;
		m_engine_info.m_max_multibody_objects		= 100;
		m_engine_info.m_max_spooled_regions			= 1;
		m_engine_info.m_max_bodies_per_multibody	= 25;
		m_engine_info.m_max_outward_links_per_link	= 8;
		m_engine_info.m_memory_size					= phEngineSizeOf(m_engine_info);
		m_engine_info.m_memory						= (PHmem*)_aligned_malloc(m_engine_info.m_memory_size, 16);
		m_engine_info.m_time_step					= Testbed().m_controls.GetStepSize();
		m_engine									= phEngineCreate(m_engine_info);
		m_world_bounds								.Set(v4Origin, v4::make(100.0f, 100.0f, 100.0f, 0.0f));
		m_static_data_registered					= false;

		g_material_data.friction	= 0.5f;
		g_material_data.elasticity	= 0.6f;
		g_material_data.eT			= 1.0f;
	}
	~PhysicsEnginePrivate()
	{
		phEngineKill(*m_engine);
		_aligned_free(m_engine_info.m_memory);
	}

	// BPinstanceState interface
	const PHstaticInstance* GetInstancePointer(PHuint instance_index) const { return reinterpret_cast<const PHstaticInstance*>(m_instance_data[instance_index]); }
	PHm4 GetInstanceToWorld(PHuint instance_index) const		{ return m4x4_to_mam4(m_instance_data[instance_index]->InstToWorld()); }
	bool IsSmashable(PHuint) const								{ return false; }
	bool IsAnimateable(PHuint) const							{ return false; }
	bool IsCollidable(PHuint) const								{ return true; }
	PHuint GetCollisionGroup(PHuint) const						{ return ECollisionGroup_Static; }

	PHengineInfo		m_engine_info;
	PHengine*			m_engine;
	unsigned int		m_frame_number;
	pr::BoundingBox		m_world_bounds;	

	// Static scene data
	enum { ArbitraryModelListSize = 10000 };
	typedef std::vector<Static const*> TInstanceData;
	typedef ri::type_traits::aligned_buffer_generator_with_size<ArbitraryModelListSize, PHcollision::ModelAlignment>::type TModelListBuffer;
	TInstanceData		m_instance_data;
	PHmodelBuilder		m_model_builder;
	//TBinaryData			m_quad_tree_data;
	PHbyteData<128>		m_quad_tree_data;
	//TBinaryData			m_model_list_data;
	ri::task::CLockable<TModelListBuffer> m_model_list_data;
	ri::task::CWritePtr<TModelListBuffer> m_model_list_wlock;
	BPstatics*			m_statics_quad_tree;
	PHmodelList*		m_statics_model_list;
	bool				m_static_data_registered;
};

// Construction
PhysicsEngine::PhysicsEngine()
:m_data(new PhysicsEnginePrivate())
{
	m_frame_number = 0;

	phEngineSetPreCollisionCallBack	(*m_data->m_engine, ReflectionsPreCollisionCallBack);
	phEngineSetPostCollisionCallBack(*m_data->m_engine, ReflectionsPstCollisionCallBack);
	phEngineSetTerrainCallBacks		(*m_data->m_engine, ReflectionsDefaultTerrainCallBack, 0);
	phEngineSetMaterialCallBack		(*m_data->m_engine, MaterialCallBack);
}

// Destruction
PhysicsEngine::~PhysicsEngine()
{
	delete m_data;
}

// Bring the engine up to date after adding objects
void PhysicsEngine::Sync()
{
	phEngineSync(*m_data->m_engine);
}

// Advance the physics engine
void PhysicsEngine::Step()
{
	PR_PROFILE_FRAME_BEGIN;

	++m_frame_number;
	phEngineRun(*m_data->m_engine, phEngineGetTimeStep(*m_data->m_engine));
	
	PR_PROFILE_FRAME_END;
	PR_PROFILE_OUTPUT(120);
}

// Set the step size
void PhysicsEngine::SetTimeStep(float step_size_in_seconds)
{
	phEngineSetTimeStep(*m_data->m_engine, step_size_in_seconds);
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
	return phEngineMaxDynamicObjects(*m_data->m_engine);
}

// Return the number of physics objects in the physics engine
std::size_t PhysicsEngine::GetNumObjects() const
{
	return phEngineNumDynamicObjects(*m_data->m_engine);
}

// Set the physics material
void PhysicsEngine::SetMaterial(const parse::Material& material)
{
	g_material_data.friction	= material.m_static_friction;
	g_material_data.elasticity	= material.m_elasticity;
	g_material_data.eT			= material.m_tangential_elasiticity + 1.0f;
}

// Add a gravity source to the engine
void PhysicsEngine::AddGravityField(const parse::Gravity& gravity)
{
	g_grav_sources.m_gravity.push_back(gravity);
}
void PhysicsEngine::ClearGravityFields()
{
	g_grav_sources.m_gravity.clear();
}

// Setup default terrain
void PhysicsEngine::SetDefaultTerrain()
{
	phEngineSetTerrainCallBacks(*m_data->m_engine, ReflectionsDefaultTerrainCallBack, 0);
}

// Setup terrain based on the data in 'terrain
void PhysicsEngine::SetTerrain(const parse::Terrain& terrain)
{
	if( terrain.m_type == parse::Terrain::EType_Reflections_2D )
	{
		static TBinaryData s_terrain_buffer;
		s_terrain_buffer.resize(0);
		pr::FileToBuffer(terrain.m_data.c_str(), s_terrain_buffer);
		g_terrain = reinterpret_cast<terrain::Header const*>(&s_terrain_buffer[0]);
		g_terrain3D[0] = 0;

		// Load the terrain thd file and change the call back function pointer
		phEngineSetTerrainCallBacks(*m_data->m_engine, ReflectionsTerrainCallBack, 0);
	}
	else if( terrain.m_type == parse::Terrain::EType_Reflections_3D )
	{
		static TBinaryData s_terrain_buffer;
		s_terrain_buffer.resize(0);
		pr::FileToBuffer(terrain.m_data.c_str(), s_terrain_buffer);
		g_terrain3D[0] = reinterpret_cast<terrain::Header const*>(&s_terrain_buffer[0]);
		g_terrain3D[1] = reinterpret_cast<terrain::Header const*>(reinterpret_cast<ri::uint8_t const*>(g_terrain3D[0]) + g_terrain3D[0]->m_data_size);
		g_terrain3D[2] = reinterpret_cast<terrain::Header const*>(reinterpret_cast<ri::uint8_t const*>(g_terrain3D[1]) + g_terrain3D[1]->m_data_size);
		g_terrain3D[3] = reinterpret_cast<terrain::Header const*>(reinterpret_cast<ri::uint8_t const*>(g_terrain3D[2]) + g_terrain3D[2]->m_data_size);
		g_terrain = 0;

		// Load the terrain thd file and change the call back function pointer
		phEngineSetTerrainCallBacks(*m_data->m_engine, ReflectionsTerrain3DCallBack);
	}
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
	PHterrainSample sample;
	phEngineTerrainCallBack(*m_data->m_engine)(v4_to_mav4(point), sample);
	height = sample.m_height;
	normal = mav4_to_v4(sample.m_normal);
}

// Cast a ray in the physics engine
void PhysicsEngine::CastRay(const pr::v4& point, const pr::v4& direction, float& intercept, pr::v4& normal, PhysObj*& hit_object, pr::uint& prim_id)
{
	PHlineToWorldTestObject line;
	line.m_point			= v4_to_mav4(point);
	line.m_direction		= v4_to_mav4(direction);
	line.m_length			= 1.0f;
	line.m_number_of_segments = 10;
	line.m_line_mask[PHlineToWorldTestObject::Want] = ELineCheckMask_All;

	PHlineToWorldCollisionParams params;
	params.m_engine			= m_data->m_engine;
	params.m_lines			= &line;
	params.m_num_lines		= 1;
	params.m_geometry_expansion = 0.0f;

	PHlineCollResult col;
	PHlineToWorldCollisionResults results;
	results.m_results		= &col;
	results.m_max_results	= 1;
	phLineVsWorldCollision(params, results);

	intercept	= col.m_intercept;
	normal		= mav4_to_v4(col.m_collision_normal);
	hit_object	= col.m_dynamic_object;
	prim_id		= col.m_prim_id;
}

// Create a graphic for the terrain sampler
std::string PhysicsEngine::CreateTerrainSampler(pr::v4 const& point)
{
	std::string str;
	if (g_terrain)
	{
		terrain::SingleHeightLookup query(point[1]);
		terrain::Query(*g_terrain, point[0], point[2], query);
		str = "*Group terrain_sampler FFFFFFFF {";
		str += Fmt("*Box query FFFFFFFF {0.2 0.2 0.2 *Position {%f %f %f}}\n", float(point[0]), float(point[1]), float(point[2]));
		str += Fmt("*Box surface FF0000FF {0.2 0.2 0.2 *Position {%f %f %f}}\n", float(point[0]), float(query.m_height), float(point[2]));
		str += Fmt("*LineD normal FF0000FF {%f %f %f %f %f %f}}\n", float(point[0]), float(query.m_height), float(point[2]), float(query.m_plane[0]), float(query.m_plane[1]), float(query.m_plane[2]));
		str += "}\n";
	}
	else if (g_terrain3D[0])
	{
		str = "*Group terrain_sampler FFFFFFFF {";
		str += Fmt("*Box query FFFFFFFF {0.2 0.2 0.2 *Position {%f %f %f}}\n", point.x, point.y, point.z);
		char const* colour[4] = {"FFFF0000", "FF00FF00", "FF0000FF", "FFFFFF00"};
		PHv4 position = v4_to_mav4(point);
		for( int i = 0; i != terrain::NumProjections; ++i )
		{
			MAv4 pos = terrain::ProjectionTransform(i) * position;
			terrain::SingleHeightLookup query(pos[1]);
			terrain::Query(*g_terrain3D[i], pos[0], pos[2], query);
			float dist = query.m_query_height - query.m_height;

			PHv4 dir = terrain::ProjectionTransformInv(i)[1];
			PHv4 surf_point	= position - dist * dir;
		
			PHv4 surf_norm = query.m_plane; surf_norm.setW0();
			surf_norm = terrain::ProjectionTransformInv(i) * surf_norm;

			ldr::Line (FmtS("result_%d", i), colour[i], point, mav4_to_v4(surf_point), str);
			ldr::Box  (FmtS("result_%d", i), colour[i], mav4_to_v4(surf_point), 0.2f, str);
			ldr::LineD(FmtS("result_%d_normal", i), colour[i], mav4_to_v4(surf_point), mav4_to_v4(surf_norm), str);
		}
		str += "}\n";
	}
	return str;
}

PHmodelReference AddModelToModelList(PHmodelBuilder& builder, parse::Model const& model, bool centre_of_mass_frame, PHm4& model_to_comframe, PHv4& inertia, PHreal& mass, PHreal density)
{
	// Create a model list containing a collision model for 'model'
	builder.beginModel();
	for( parse::TPrim::const_iterator p = model.m_prim.begin(), p_end = model.m_prim.end(); p != p_end; ++p )
	{
		parse::Prim const& prim = *p;
		switch( prim.m_type )
		{
		default: PR_ASSERT(PR_DBG_COMMON, false); break;
		case parse::Prim::EType_Box:
			builder.addSolidBox(v4_to_mav4(prim.m_radius), m4x4_to_mam4(prim.m_prim_to_model), 0);
			break;
		case parse::Prim::EType_Cylinder:
			builder.addSolidCylinder(v4_to_mav4(prim.m_radius), m4x4_to_mam4(prim.m_prim_to_model), 0);
			break;
		case parse::Prim::EType_Sphere:
			builder.addSolidSphere(v4_to_mav4(prim.m_radius), m4x4_to_mam4(prim.m_prim_to_model), 0);
			break;
		case parse::Prim::EType_Polytope:
			builder.beginPolytope(m4x4_to_mam4(prim.m_prim_to_model), 0);
			for( std::size_t i = 0; i != prim.m_vertex.size(); ++i )
				builder.addPolytopeVertex(v4_to_mav4(prim.m_vertex[i]));
			builder.endPolytope();
			break;
		case parse::Prim::EType_PolytopeExplicit:
			{
				builder.beginExplicitPolytope(m4x4_to_mam4(prim.m_prim_to_model), 0);

				for( parse::TPoints::const_iterator i = prim.m_vertex.begin(), i_end = prim.m_vertex.end(); i != i_end; ++i )
					builder.addExplicitPolytopeVertex(v4_to_mav4(*i));

				parse::TIndices::const_iterator i = prim.m_face.begin();
				for( std::size_t f = 0; f != prim.m_face.size() / 3; ++f )
				{
					PHpolyIndex i0 = static_cast<PHpolyIndex>(*i++);
					PHpolyIndex i1 = static_cast<PHpolyIndex>(*i++);
					PHpolyIndex i2 = static_cast<PHpolyIndex>(*i++);
					builder.addExplicitPolytopeFace(i0, i1, i2);
				}

				builder.endExplicitPolytope();
			}break;
		}
	}
	if( centre_of_mass_frame )
	{
		builder.moveToCentreOfMassFrame(model_to_comframe, inertia, mass, density);
	}
	return builder.endModel();
}
PHmodelReference AddModelToModelList(PHmodelBuilder& builder, parse::Model const& model)
{
	PHm4 model_to_comframe;
	PHv4 inertia;
	PHreal mass;
	return AddModelToModelList(builder, model, false, model_to_comframe, inertia, mass, 1.0f);
}

// Reset the static scene data
void PhysicsEngine::ClearStaticSceneData()
{
	// Unregister any previous data
	if( m_data->m_static_data_registered )
	{
		phUnregisterModelList       (*m_data->m_engine, *m_data->m_statics_model_list, 0);
		phUnRegisterRegionBroadphase(*m_data->m_engine, *m_data->m_statics_quad_tree);
		m_data->m_static_data_registered = false;
	}

	m_data->m_model_builder.clear();
	m_data->m_instance_data.clear();
	m_data->m_quad_tree_data.clear();
	//m_data->m_model_list_data.clear();
	m_data->m_model_list_wlock = 0;
	m_data->m_statics_model_list = 0;
	m_data->m_statics_quad_tree = 0;
}

// Add a collision model to model list and return the collision model
void PhysicsEngine::CreateStaticCollisionModel(parse::Model const& model, CollisionModel& col_model, std::string& ldr_string)
{
	col_model.m_shape_id = AddModelToModelList(m_data->m_model_builder, model);

	// This is returning a pointer into the model builder, don't store this pointer
	CollisionModel tmp;
	tmp.m_shape = &m_data->m_model_builder.getCollisionModel(col_model.m_shape_id);
	ldr_string = MakeLdrString(model.m_name, model.m_colour, tmp);
}

// This should be called after adding static objects to the scene
void PhysicsEngine::RebuildStaticScene(TStatic const& statics, pr::BoundingBox const& world_bounds)
{
	// Unregister any previous data
	if( m_data->m_static_data_registered )
	{
		phUnregisterModelList       (*m_data->m_engine, *m_data->m_statics_model_list, 0);
		phUnRegisterRegionBroadphase(*m_data->m_engine, *m_data->m_statics_quad_tree);
		m_data->m_static_data_registered = false;
	}

	// Nothing to do if there aren't any statics
	if( statics.empty() )
		return;

	// Export the new static models list
	TBinaryData	model_list_data;
	m_data->m_model_builder.createModelList(model_list_data);

	// Hold a writelock on the model buffer to keep the task schedular happy
	m_data->m_model_list_wlock = m_data->m_model_list_data.GetWritePtr();

	// Copy the model list data into the locked memory buffer
	PR_ASSERT_STR(PR_DBG_COMMON, model_list_data.size() <= PhysicsEnginePrivate::ArbitraryModelListSize, "Static models to large for the fixed buffer");
	memcpy(m_data->m_model_list_wlock.get_ptr(), &model_list_data[0], model_list_data.size());

	// Resolve and register 
	m_data->m_statics_model_list = phResolveModels(m_data->m_model_list_wlock.get_ptr());
	phRegisterModelList(*m_data->m_engine, *m_data->m_statics_model_list, 0);

	// Create and register a static quad tree
	BPstaticQuadTreeBuilder qt_builder;
	v4 centre = world_bounds.Centre();
	v4 radius = world_bounds.Radius();
	float region_size = Maximum(radius.x, radius.z);
	qt_builder.Initialise(ri::GlobalRegionId(), centre.x-region_size, centre.z-region_size, 2.0f*region_size);

	// Add each instance to the quad tree
	m_data->m_instance_data.clear();
	for( TStatic::const_iterator i = statics.begin(), i_end = statics.end(); i != i_end; ++i )
	{
		Static const& statik = *(i->second);

		// Add the static to the instance data
		PHuint inst_index = static_cast<PHuint>(m_data->m_instance_data.size());
		m_data->m_instance_data.push_back(&statik);

		// Add a static object to the broadphase quad tree
		BPstaticObject static_object;
		static_object.m_collision_model_ref = statik.ColModel().m_shape_id;
		static_object.m_graphic_instance_ref = inst_index;
		PHv4  point  = v4_to_mav4(statik.Bounds().Centre());
		float radius = statik.Bounds().Diametre() * 0.5f;
		if( !qt_builder.AddStaticObject(static_object, point, radius) )
		{
			PR_ASSERT_STR(PR_DBG_COMMON, false, "Failed to add a static instance to the quad tree");
		}
	}

	//set a lookup table that maps the local model table ids packed into the static objects to global model table ids.
	//global model table ids are simply superregion ids (in D4) or region ids (in D5).
	PHint container_id_map[1] = {0};
	qt_builder.SetModelListIDs(container_id_map, 1);

	// 'buffer' will be resized to contain the package data on return
	if( !qt_builder.CreatePackedData(m_data->m_quad_tree_data) )
	{
		PR_ASSERT_STR(PR_DBG_COMMON, false, "Failed to create the scene data");
	}

	// Resolve the quad tree data and register it with the physics
	m_data->m_statics_quad_tree = phResolveQuadTree(&m_data->m_quad_tree_data[0]);
	phRegisterRegionBroadphase(*m_data->m_engine, *m_data->m_statics_quad_tree, *m_data);
	m_data->m_static_data_registered = true;
}

// Create a collision model
void PhysicsEngine::CreateCollisionModel(parse::Model const& model, CollisionModel& col_model)
{
	// Handle empty models, they are used for multibody joints
	if( model.m_prim.empty() )
	{
		col_model.m_shape = 0;
		col_model.m_CoMframe_to_model.Identity();
		col_model.m_model_to_CoMframe.Identity();
		col_model.m_inertia_tensor.Identity();
		col_model.m_mass = 1.0f;
		col_model.m_ms_bbox.Unit();
		return;
	}

	// Create a model list containing a collision model for 'model'
	PHmodelBuilder builder;
	PHreal mass;
	PHv4 inertia;
	PHm4 model_to_CoMframe;
	PHmodelReference model_ref = AddModelToModelList(builder, model, true, model_to_CoMframe, inertia, mass, 1.0f);

	// Create the model list
	TBinaryData model_buffer;
	builder.createModelList(model_buffer);
	PHmodelList* model_list = phResolveModels(&model_buffer[0]);
	PHcollisionModel* cm = &phGetCollisionModel(*model_list, model_ref);

	// Copy the model into the lockable model buffer
	PR_ASSERT_STR(PR_DBG_COMMON, phColModelSize(*cm) <= col_model.m_buffer.Size, "Model too big for fixed size buffer");
	phColModelClone(*cm, &col_model.m_buffer.model(), col_model.m_buffer.Size);
	col_model.m_shape = &col_model.m_buffer.model();

	// We're going to modify 'model' so that it looks like it was
	// given in inertial frame this means model_to_CoMframe (and i2m)
	// are not needed when dealing with model transforms (i.e. no model_to_CoMframe etc)
	// They are needed for things that use the model however, i.e. phys.m_model_to_world
	// needs to be adjusted
	col_model.m_model_to_CoMframe	= mam4_to_m4x4(model_to_CoMframe);
	col_model.m_CoMframe_to_model	= col_model.m_model_to_CoMframe.GetInverseFast();
	col_model.m_inertia_tensor.x.x	= inertia[0];
	col_model.m_inertia_tensor.y.y	= inertia[1];
	col_model.m_inertia_tensor.z.z	= inertia[2];
	col_model.m_mass				= mass;
}

// Return a dynamic physics object
void PhysicsEngine::CreatePhysicsObject(parse::PhysObj const& phys, CollisionModel const& col_model, void* user_data, PhysObj*& phys_obj)
{
	PHobjectInfo object_info;
	object_info.m_engine			= m_data->m_engine;
	object_info.m_cmodel			= col_model.m_shape;
	object_info.m_mass				= phys.m_mass != 0.0f ? phys.m_mass : col_model.m_mass;
	object_info.m_os_mass_tensor	= PHv4::make(col_model.m_inertia_tensor.x.x, col_model.m_inertia_tensor.y.y, col_model.m_inertia_tensor.z.z, 0.0f);
	object_info.m_object_to_world	= m4x4_to_mam4(phys.m_object_to_world);
	object_info.m_gravity			= v4_to_mav4(phys.m_gravity);
	object_info.m_velocity			= v4_to_mav4(phys.m_velocity);
	object_info.m_ang_velocity		= v4_to_mav4(phys.m_ang_velocity);
	object_info.m_pre_coll_cb_data	= user_data;
	object_info.m_post_coll_cb_data	= user_data;
	phys_obj						= phObjectCreate(object_info); PR_ASSERT(1, phys_obj);
	phObjectSetName(*phys_obj, phys.m_name.c_str());
}

// Delete a physics object
void PhysicsEngine::DeletePhysicsObject(PhysObj*& phys_obj)
{
	if( phys_obj ) phObjectKill(*phys_obj);
	phys_obj = 0;
}

// Create a deformable collision model
void PhysicsEngine::CreateDeformableModel(parse::Deformable const& deformable, DeformableModel& def_model)
{
	std::vector<PHv4>		verts;
	std::vector<PHdefTetra>	tetra;
	std::vector<PHdefStrut> strut;

	std::size_t total_verts		= deformable.m_tmesh_verts.size() + deformable.m_smesh_verts.size() +  deformable.m_anchors.size();
	std::size_t total_tetra		= deformable.m_tetras.size()/4;
	std::size_t total_strut		= deformable.m_springs.size()/2 + deformable.m_beams.size()/2;

	PHdefParams params;
	params.m_num_tmesh_verts	= (PHuint)deformable.m_tmesh_verts.size();
	params.m_num_spring_verts	= (PHuint)deformable.m_smesh_verts.size();
	params.m_num_anchor_verts	= (PHuint)deformable.m_anchors.size();
	params.m_num_tetra			= (PHuint)deformable.m_tetras.size() / 4;
	params.m_num_springs		= (PHuint)deformable.m_springs.size() / 2;
	params.m_num_beams			= (PHuint)deformable.m_beams.size() / 2;
	params.m_verts				= total_verts != 0 ? (verts.resize(total_verts), &verts[0]) : 0;
	params.m_tetra				= total_tetra != 0 ? (tetra.resize(total_tetra), &tetra[0]) : 0;
	params.m_strut				= total_strut != 0 ? (strut.resize(total_strut), &strut[0]) : 0;
	params.m_spring_constant	= deformable.m_spring_constant;
	params.m_damping_constant	= deformable.m_damping_constant;
	params.m_sprain_percentage	= deformable.m_sprain_percentage;

	// Copy vert data
	PHv4* v = const_cast<PHv4*>(params.m_verts);
	for( parse::TPoints::const_iterator i = deformable.m_tmesh_verts.begin(), i_end = deformable.m_tmesh_verts.end(); i != i_end; ++i, ++v )
		*v = v4_to_mav4(*i);
	for( parse::TPoints::const_iterator i = deformable.m_smesh_verts.begin(), i_end = deformable.m_smesh_verts.end(); i != i_end; ++i, ++v )
		*v = v4_to_mav4(*i);
	for( parse::TPoints::const_iterator i = deformable.m_anchors.begin(), i_end = deformable.m_anchors.end(); i != i_end; ++i, ++v )
		*v = v4_to_mav4(*i);
	
	// Copy tetra data
	PHdefTetra* t = const_cast<PHdefTetra*>(params.m_tetra);
	for( parse::TIndices::const_iterator i = deformable.m_tetras.begin(), i_end = deformable.m_tetras.end(); i != i_end; ++t )
	{
		t->m_index[0] = pr::value_cast<PHuint>(*i++);
		t->m_index[1] = pr::value_cast<PHuint>(*i++);
		t->m_index[2] = pr::value_cast<PHuint>(*i++);
		t->m_index[3] = pr::value_cast<PHuint>(*i++);
	}

	// Copy strut data
	PHdefStrut* s = const_cast<PHdefStrut*>(params.m_strut);
	for( parse::TIndices::const_iterator i = deformable.m_springs.begin(), i_end = deformable.m_springs.end(); i != i_end; ++s )
	{
		s->m_index0 = pr::value_cast<PHuint>(*i++);
		s->m_index1 = pr::value_cast<PHuint>(*i++);
	}
	for( parse::TIndices::const_iterator i = deformable.m_beams.begin(), i_end = deformable.m_beams.end(); i != i_end; ++s )
	{
		s->m_index0 = pr::value_cast<PHuint>(*i++);
		s->m_index1 = pr::value_cast<PHuint>(*i++);
	}

	// Auto generate if things are missing
	std::vector<PHv4>		verts2;
	std::vector<PHdefStrut> strut2;
	if( params.m_num_spring_verts == 0 && params.m_num_anchor_verts == 0 &&
		params.m_num_springs == 0 && params.m_num_beams == 0 )
	{
		phDeformableGenerateSpringData(params, verts2, strut2, MA_FLT_MAX, 200);
	}

	// Build the data
	if( phDeformableBuildData(params, def_model.m_buffer) == 0 )
	{
		ldrErrorReport(pr::Fmt("Failed to create the deformable data. Reason: %s", phGetErrorString()).c_str());
		return;
	}

	// Resolve the deformable mesh into a runtime ready one
	PHdeformable::Data* mesh_data = phResolveDeformable(&def_model.m_buffer[0]);
	
	// Transform the deformable into com frame
	PHreal mass;
	PHv4 inertia;
	PHm4 model_to_CoMframe;
	phDeformableFixCoordFrame(*mesh_data, model_to_CoMframe, inertia, mass, 1.0f);

	// Create an instance of the deformable mesh
	def_model.m_model = &def_model.m_model_buffer;
	phDeformableCreateInstance(*mesh_data, *def_model.m_model);

	// Record the mass properties
	def_model.m_model_to_CoMframe		= mam4_to_m4x4(model_to_CoMframe);
	def_model.m_CoMframe_to_model		= def_model.m_model_to_CoMframe.GetInverseFast();
	def_model.m_inertia_tensor.x.x		= inertia[0];
	def_model.m_inertia_tensor.y.y		= inertia[1];
	def_model.m_inertia_tensor.z.z		= inertia[2];
	def_model.m_mass					= mass;
}

// Return the object to world transform for a physics object
pr::m4x4 PhysicsEngine::ObjectToWorld(PhysObj const* phys_obj)
{
	return mam4_to_m4x4(phObjectToWorld(*phys_obj));
}

// Set the object to world transform
void PhysicsEngine::SetObjectToWorld(PhysObj* phys_obj, pr::m4x4 const& o2w)
{
	phObjectSetObjectToWorld(*phys_obj, m4x4_to_mam4(o2w));
}

// Set the gravity vector for a physics object
void PhysicsEngine::ObjectSetGravity(PhysObj* phys_obj)
{
	PHv4 grav = g_grav_sources.GetGravity(phObjectPosition(*phys_obj));
	phObjectSetGravity(*phys_obj, grav);
}

// Set the velocity of a physics object
void PhysicsEngine::ObjectSetVelocity(PhysObj* phys_obj, pr::v4 const& vel)
{
	phObjectSetVelocity(*phys_obj, v4_to_mav4(vel));
}

// Set the ang velocity of a physics object
void PhysicsEngine::ObjectSetAngVelocity(PhysObj* phys_obj, pr::v4 const& ang_vel)
{
	phObjectSetAngularVelocity(*phys_obj, v4_to_mav4(ang_vel));
}

// Wake a physics object up
void PhysicsEngine::ObjectWakeUp(PhysObj* phys_obj)
{
	phObjectWakeUp(*phys_obj);
}

// Apply an impulse to a physics object
void PhysicsEngine::ObjectApplyImpulse(PhysObj* phys_obj, pr::v4 const& ws_impulse, pr::v4 const& ws_pos)
{
	PHm4ref o2w = phObjectToWorld(*phys_obj);
	phObjectApplyImpulse(*phys_obj, v4_to_mav4(ws_impulse), v4_to_mav4(ws_pos) - o2w[3]);
}

// Update the collision model of a physics object
void PhysicsEngine::ObjectSetColModel(PhysObj* phys_obj, CollisionModel const& col_model, m4x4 const& o2w)
{
	phObjectSetCollisionModel(*phys_obj, col_model.m_shape);
	phObjectSetObjectToWorld (*phys_obj, m4x4_to_mam4(o2w));
}

//// Deform a collision model.
//// Assumes the collision model belongs to object A
//bool PhysicsEngine::Deform(CollisionModel& col_model, PhysObj const* objA, PhysObj const* objB, col::Contact const& ct)
//{
//	PHobject const& objectA = *objA;
//	PHobject const& objectB = *objB;
//
//	PHprimitive* prim = 0;
//	PHcollisionModel const* cmB = phObjectCollisionModel(objectB);
//	if( cmB )
//	{
//
//	}
//	else
//	{
//		static PHcollision::Box dummy;
//
//	}
//
//	PHcollisionModel& cmA = *col_model.m_shape;
//
//	struct Contact
//	{
//		pr::v4		m_ws_point;
//		pr::v4		m_ws_normal;
//		pr::v4		m_ws_impulse;
//		pr::uint	m_prim_id;
//	};
//
//	return phColModelDeform(
//		cmA,		// The collision model to be deformed
//		PHprimitive const& prim,	// The colliding primitive
//		PHm4ref prim_to_cm,			// A transform that puts the colliding primitive in collision model space
//		PHv4ref impulse,			// The collision impulse
//		PHv4ref normal				// The collision normal
//		);
//	return false;
//}

// Transform the deformable into a different space
void PhysicsEngine::DeformableTransform(DeformableModel& deform, pr::m4x4 const& transform)
{
	phDeformableTransform(*deform.m_model, m4x4_to_mam4(transform));
}

// Respond to a collision
void PhysicsEngine::DeformableImpact(DeformableModel& deform, pr::v4 const& point, pr::v4 const& normal, pr::v4 const& impulse)
{
	phDeformableImpact(*deform.m_model, v4_to_mav4(point), v4_to_mav4(normal), v4_to_mav4(impulse));
}

// Evolve the state of the deformable.
// Returns true if the deformable has changed shape
bool PhysicsEngine::DeformableEvolve(DeformableModel& deform, float step_size, bool to_equilibrium)
{
	return phDeformableEvolve(*deform.m_model, step_size, to_equilibrium);
}

// Decompose a deformable mesh into a collision model
void PhysicsEngine::DeformableDecompose(DeformableModel& deform, CollisionModel& col_model)
{
	PHcollision::ModelBuffer<10000> model;
	if( phDeformableDecompose(*deform.m_model, model, deform.m_convex_tolerance) )
	{
		phColModelClone(model, &col_model.m_buffer.model(), col_model.m_buffer.Size);
		col_model.m_shape = &col_model.m_buffer.model();
		
		col_model.m_model_to_CoMframe	= deform.m_model_to_CoMframe;
		col_model.m_CoMframe_to_model	= deform.m_CoMframe_to_model;
		col_model.m_inertia_tensor		= deform.m_inertia_tensor;
		col_model.m_mass				= deform.m_mass;
	}
}

//// Deform a skeleton in response to an impulse
//void PhysicsEngine::SkeletonDeform(Skeleton& skel, pr::v4 const& ms_point, pr::v4 const& ms_norm, pr::v4 const& ms_deltavel)
//{
//	phSkeletonDeform(*skel.m_inst, v4_to_mav4(ms_point), v4_to_mav4(ms_norm), v4_to_mav4(ms_deltavel));
//}
//
//// Evolve the state of a skeleton over time
//bool PhysicsEngine::SkeletonEvolve(Skeleton& skel, float step_size)
//{
//	return phSkeletonEvolve(*skel.m_inst, step_size);
//}
//
//// Use a rigidbody skeleton to deform a collision model
//void PhysicsEngine::SkeletonMorphCM(Skeleton const& skel, CollisionModel& col_model)
//{
//	phSkeletonMorphCM(*skel.m_inst, *skel.m_ref_cm, *col_model.m_shape);
//}

// Attach a multibody
void PhysicsEngine::MultiAttach(PhysObj* phys_obj, PhysObj* parent, parse::Multibody const& multi_info)
{
	PHjointFrame ps_frame, os_frame;
	ps_frame.m_position		= v4_to_mav4(multi_info.m_ps_attach.x);
	ps_frame.m_orientation	= v4_to_mav4(multi_info.m_ps_attach.y);
	ps_frame.m_zero			= v4_to_mav4(multi_info.m_ps_attach.z);
	os_frame.m_position		= v4_to_mav4(multi_info.m_os_attach.x);
	os_frame.m_orientation	= v4_to_mav4(multi_info.m_os_attach.y);
	os_frame.m_zero			= v4_to_mav4(multi_info.m_os_attach.z);

	EMultibodyJointType jtype = EMultibodyJointType_Floating;
	if( multi_info.m_joint_type == 1 ) jtype = EMultibodyJointType_Revolute;
	if( multi_info.m_joint_type == 2 ) jtype = EMultibodyJointType_Prismatic;

	PHobject& object = *phys_obj;
	if( !parent )
	{
		PHmulti* multi = phMultiCreate(object);
		if( jtype == EMultibodyJointType_Floating )
		{
			PHm4 o2w = phObjectToWorld(object);
			o2w[3]  -= phObjectToWorld(object) * os_frame.m_position;
			//o2w[3][0] = fmod(o2w[3][0], 10.0f);
			//o2w[3][2] = fmod(o2w[3][2], 10.0f);
			phObjectSetObjectToWorld(object, o2w);
			phObjectSetVelocity(object, v4_to_mav4(multi_info.m_velocity));
			phObjectSetAngularVelocity(object, v4_to_mav4(multi_info.m_ang_velocity));
		}
		else
		{
			// Adjust the ps_frame by the object to world of 'object'
			PHv4 position = ps_frame.m_position; position.setW1();
			ps_frame.m_position		= phObjectToWorld(object) * position;
			ps_frame.m_orientation	= phObjectToWorld(object) * ps_frame.m_orientation;
			ps_frame.m_zero			= phObjectToWorld(object) * ps_frame.m_zero;
			phMultiFixToWorld(*multi, os_frame, ps_frame, jtype);
		}
	}
	else
	{
		PHobject& parent_obj = *parent;
		phMultiAttachObject(object, parent_obj, os_frame, ps_frame, jtype);
	}
	phMultiSetSpringConstants(object, multi_info.m_joint_spring, multi_info.m_joint_damping, 3);
	phMultiSetJointPosition(object, multi_info.m_pos);
	phMultiSetJointVelocity(object, multi_info.m_vel);
	phMultiSetJointLimits  (object, multi_info.m_lower_limit, multi_info.m_upper_limit, 3);
	phMultiSetLimitRestitution(object, multi_info.m_restitution);
}

// Break all the links in a multibody leaving rigid bodies
void PhysicsEngine::MultiBreak(PhysObj* phys_obj)
{
	PHobject& object = *phys_obj;
	phMultiBreak(phMultiGet(object));
}

// Return the velocity of a physics object
pr::v4 PhysicsEngine::ObjectGetVelocity(PhysObj const* phys_obj)
{
	return mav4_to_v4(phObjectVelocity(*phys_obj));
}

// Return the angular velocity of a physics object
pr::v4 PhysicsEngine::ObjectGetAngVelocity(PhysObj const* phys_obj)
{
	return mav4_to_v4(phObjectAngularVelocity(*phys_obj));
}

// Return the velocity of a point on the physics object
pr::v4 PhysicsEngine::ObjectGetVelocity(PhysObj const* phys_obj, pr::v4 const& ws_point)
{
	PHobject const& object = *phys_obj;
	PHv4 pt = v4_to_mav4(ws_point) - phObjectPosition(object);
	return mav4_to_v4(phObjectVelocity(object, pt));
}

// Return the anguluar momentum of a physics object
pr::v4 PhysicsEngine::ObjectGetAngMomentum(PhysObj const* phys_obj)
{
	return mav4_to_v4(phObjectAngularMomentum(*phys_obj));
}

// Return the ws bounding box for a physics object
pr::BoundingBox	PhysicsEngine::ObjectGetWSBbox(PhysObj const* phys_obj)
{
	PHv4 lower = phBoundsLower(*phys_obj);
	PHv4 upper = phBoundsUpper(*phys_obj);
	return pr::BoundingBox::make(mav4_to_v4((lower + upper) / 2.0f), mav4_to_v4((upper - lower) / 2.0f));
}

// Return the os bounding box for a physics object
pr::BoundingBox	PhysicsEngine::ObjectGetOSBbox(PhysObj const* phys_obj)
{
	PHcollisionModel const* col_model = phObjectCollisionModel(*phys_obj);
	if( !col_model ) return pr::BBoxZero;
	PHv4 lower, upper;
	phPrimitiveBounds(phColModelBoundingPrim(*col_model), lower, upper, PHm4::Id());
	return pr::BoundingBox::make(mav4_to_v4((lower + upper) / 2.0f), mav4_to_v4((upper - lower) / 2.0f));
}

// Return the pre-collision call back data
void* PhysicsEngine::ObjectGetPreColData(PhysObj const* phys_obj)
{
	return phObjectPreCollisionCallBackData(*phys_obj);
}

// Return the post-collision call back data
void* PhysicsEngine::ObjectGetPstColData(PhysObj const* phys_obj)
{
	return phObjectPostCollisionCallBackData(*phys_obj);
}

// Return the mass of a physics object
float PhysicsEngine::ObjectGetMass(PhysObj const* phys_obj)
{
	return phObjectMass(*phys_obj);
}

// Return the inertia tensor for a physics object
pr::m3x3 PhysicsEngine::ObjectGetOSInertia(PhysObj const* phys_obj)
{
	PHv4 vec = phObjectOSMassTensor(*phys_obj);
	m3x3 inertia = m3x3Identity;
	inertia.x.x = vec[0];
	inertia.y.y = vec[1];
	inertia.z.z = vec[2];
	return inertia;
}

// Return the inverse world space inertia tensor
pr::m3x3 PhysicsEngine::ObjectGetWSInvInertia(PhysObj const* phys_obj)
{
	return mam3_to_m3x3(phObjectWSInverseMassTensor(*phys_obj));
}

// Return the motion type of an object
EPhysObjType PhysicsEngine::ObjectGetPhysObjType(PhysObj const* phys_obj)
{
	switch( phObjectMotionType(*phys_obj) )
	{
	default:
	case EPhysicsMotionType_Ballistic:		return EPhysObjType_Dynamic;
	case EPhysicsMotionType_Static:			return EPhysObjType_Static;
	case EPhysicsMotionType_Sleeping:		return EPhysObjType_Static;
	case EPhysicsMotionType_Terrain:		return EPhysObjType_Terrain;
	case EPhysicsMotionType_InfiniteMass:	return EPhysObjType_Static;
	case EPhysicsMotionType_Animated:		return EPhysObjType_Static;
	}
}

// Return the resting contact points for a physics object
void PhysicsEngine::ObjectRestingContacts(PhysObj const* phys_obj, pr::v4* contacts, pr::uint max_contacts, pr::uint& count)
{
	PHv4* points = (PHv4*)_alloca(max_contacts * sizeof(PHv4));
	phObjectRestingContactPoints(*phys_obj, points, count);
	for( pr::uint i = 0; i != count; ++i )
	{
		PR_ASSERT(1, points[i][3] == 1.0f);
		contacts[i] = mav4_to_v4(points[i]);
	}
}

// Return true if an object is asleep
bool PhysicsEngine::ObjectIsSleeping(PhysObj const* phys_obj)
{
	return phObjectMotionType(*phys_obj) == EPhysicsMotionType_Sleeping;
}

// Make a ldr string representation of a collision model
std::string PhysicsEngine::MakeLdrString(std::string const& name, pr::Colour32 colour, CollisionModel const& col_model)
{
	std::string str;
	ldr::GroupStart(name.c_str(), str);
	if( col_model.m_shape )
	{
		PHcollisionModel const& cm = *col_model.m_shape;
		std::string col = Fmt("%X", colour.m_aarrggbb).c_str();
		//if( phColModelPrimCount(cm) > 1 )
		//{
		//	PHprimitive const& prim = phColModelBoundingPrim(cm);
		//	PHm4 p2w  = phPrimitivePrimToModel(prim);
		//	PHv4 dims = phPrimitiveDimensions(prim);
		//	ldr::Box("box", col.c_str(), mam4_to_m4x4(p2w), mav4_to_v4(dims));
		//	ldr::Nest();
		//	ldr::g_output->Print("*Wireframe");
		//	ldr::UnNest();
		//}
		for( PHprimitiveCIter i = phColModelPrimBegin(cm), i_end = phColModelPrimEnd(cm); i != i_end; ++i )
		{
			PHprimitive const& prim = *i;
			PHm4 p2w  = phPrimitivePrimToModel(prim);
			PHv4 dims = phPrimitiveDimensions(prim);
			switch( phPrimitiveType(prim) )
			{
			case EPHprimitive_Box:		ldr::Box       ("box", col.c_str(), mam4_to_m4x4(p2w), mav4_to_v4(dims), str); break;
			case EPHprimitive_Cylinder:	ldr::CylinderHR("cyl", col.c_str(), mam4_to_m4x4(p2w), dims[0], dims[1], str); break;
			case EPHprimitive_Sphere:	ldr::Sphere    ("sph", col.c_str(), mav4_to_v4(p2w[3]), dims[0], str); break;
			case EPHprimitive_Polytope:
				{
					PHuint	vert_count	= phPolytopeGetVertCount(phPrimitiveAsPolytope(prim));
					PHv4*	verts		= PR_ALLOCA(PHv4, vert_count);
					phPolytopeGenerateVerts(phPrimitiveAsPolytope(prim), verts, verts + vert_count);
					ldr::Polytope ("ply", col.c_str(), m4x4Identity, (pr::v4 const*)verts, (pr::v4 const*)(verts + vert_count), str);
				}break;
			}
		}
	}
	ldr::GroupEnd(str);
	return str;
}

// Update a ldr model for the skeleton
void PhysicsEngine::MakeLdrObject(pr::rdr::Model* model, pr::BoundingBox& bbox, void* user_data, pr::rdr::MaterialManager& mat_mgr)
{
	using namespace pr::rdr;
	using namespace pr::rdr::model;

	CollisionModel const& col_model = *static_cast<CollisionModel const*>(user_data);
	if( !col_model.m_shape ) return;
	PHcollisionModel const& cm = *col_model.m_shape;
	
	MLock mlock(model);
	rdr::Material mat = mat_mgr.GetDefaultMaterial();
	mat.m_effect = mat_mgr.GetEffect(EEffect_xyzlittint);

	model->DeleteRenderNuggets();
	model->SetName(col_model.m_name.c_str());

	{// Add the bounding box
	}

	// Add the primitives
	for( PHprimitiveCIter i = phColModelPrimBegin(cm), i_end = phColModelPrimEnd(cm); i != i_end; ++i )
	{
		PHprimitive const& prim = *i;
		PHm4 p2w  = phPrimitivePrimToModel(prim);
		PHv4 dims = phPrimitiveDimensions(prim);
		switch( phPrimitiveType(prim) )
		{
		case EPHprimitive_Box:		Box 		 (mlock, mav4_to_v4(dims)         , mam4_to_m4x4(p2w)      , Colour32White, &mat); break;
		case EPHprimitive_Cylinder:	CylinderHRxRz(mlock, dims[1], dims[0], dims[0], mam4_to_m4x4(p2w), 1, 3, Colour32White, &mat); break;
		case EPHprimitive_Sphere:	SphereRxRyRz (mlock, dims[0], dims[0], dims[0], mav4_to_v4(p2w[3]),   3, Colour32White, &mat); break;
		case EPHprimitive_Polytope:
			{
				PHpolytope const& poly = phPrimitiveAsPolytope(prim);

				PHuint vert_count = phPolytopeGetVertCount(poly);
				PHuint face_count = 2 * vert_count;
				PHuint*	    Faces = PR_ALLOCA(PHuint, 3*face_count), *f_in  = Faces;
				rdr::Index* faces = reinterpret_cast<rdr::Index*>(Faces) , *f_out = faces; // Alias this punk
				phPolytopeGenerateFaces(poly, Faces, Faces + 3*face_count, face_count);
				for( PHuint i = 0; i != 3*face_count; ++i )
					*f_out++ = pr::value_cast<rdr::Index>(*f_in++);

				Range v_range, i_range;
				model::Mesh(mlock, EPrimitiveType_TriangleList, f_out - faces, vert_count, faces, (pr::v4 const*)poly.vert_begin(), 0, 0, 0, mam4_to_m4x4(p2w), Colour32White, &mat, &v_range, &i_range);
				GenerateNormals(mlock, &v_range, &i_range);
			}break;
		}
	}

	bbox.Reset();
	for( vf::iterator vb = mlock.m_vlock.m_ptr, vb_end = vb + mlock.m_vrange.m_first; vb != vb_end; ++vb )
		Encompase(bbox, v4::make(vb->vertex(), 1.0f));
}

// Update a ldr model for the Deformable
void PhysicsEngine::MakeLdrObjectDeformable(pr::rdr::Model* model, pr::BoundingBox& bbox, void* user_data, pr::rdr::MaterialManager& mat_mgr)
{
	using namespace pr::rdr;
	using namespace pr::rdr::model;

	DeformableModel const& deform = *static_cast<DeformableModel const*>(user_data);
	if( !deform.m_model ) return;
	PHdeformable::InstancePriv const& dmesh = *deform.m_model;
	
	MLock mlock(model);
	rdr::Material mat = mat_mgr.GetDefaultMaterial();

	model->DeleteRenderNuggets();
	model->SetName(deform.m_name.c_str());

	{// Add the anchor points
		mat.m_effect = mat_mgr.GetEffect(EEffect_xyzlitpvc);
		PHuint num_anchors = dmesh.m_mesh_data->m_num_verts - dmesh.m_mesh_data->m_num_moveable_verts;
		pr::rdr::model::BoxList(mlock, 0.02f, (pr::v4 const*)dmesh.anchor_verts(), num_anchors, deform.m_anchor_colour, &mat);
	}

	{// Add the tetra edges
		PHuint num_edges = phTetrameshGetEdges(dmesh, 0, 0, true);
		PHuint* idx		 = PR_ALLOCA(PHuint, num_edges * 2);
		pr::v4* edges	 = PR_ALLOCA(pr::v4, num_edges * 2);
		pr::v4* edge	 = edges;	 
		phTetrameshGetEdges(dmesh, idx, idx + num_edges * 2, true);
		for( PHuint i = 0; i != num_edges; ++i )
		{
			*edge++ = mav4_to_v4(dmesh.m_verts[*idx++]);
			*edge++ = mav4_to_v4(dmesh.m_verts[*idx++]);
		}
		mat.m_effect = mat_mgr.GetEffect(EEffect_xyzpvc);
		pr::rdr::model::Line(mlock, edges, num_edges, pr::Colour32::make(0xFF0000FF), &mat);
	}

	{// Add the beams
		PHuint num_beams = dmesh.m_mesh_data->m_num_struts - dmesh.m_mesh_data->m_num_springs;
		pr::v4* lines = PR_ALLOCA(pr::v4, num_beams * 2);
		pr::v4* line = lines;
		for( PHuint i = 0; i != num_beams; ++i )
		{
			PHdeformable::Strut const& beam = dmesh.beams()[i];
			*line++ = mav4_to_v4(dmesh.m_verts[beam.m_index[0]]);
			*line++ = mav4_to_v4(dmesh.m_verts[beam.m_index[1]]);
		}
		mat.m_effect = mat_mgr.GetEffect(EEffect_xyzpvc);
		pr::rdr::model::Line(mlock, lines, num_beams, deform.m_beam_colour, &mat);
	}

	{// Add the springs
		PHuint num_springs = dmesh.m_mesh_data->m_num_springs;
		pr::v4* lines = PR_ALLOCA(pr::v4, num_springs * 2);
		pr::v4* line = lines;
		for( PHuint i = 0; i != num_springs; ++i )
		{
			PHdeformable::Strut const& spring = dmesh.springs()[i];
			*line++ = mav4_to_v4(dmesh.m_verts[spring.m_index[0]]);
			*line++ = mav4_to_v4(dmesh.m_verts[spring.m_index[1]]);
		}
		mat.m_effect = mat_mgr.GetEffect(EEffect_xyzpvc);
		pr::rdr::model::Line(mlock, lines, num_springs, deform.m_spring_colour, &mat);
	}

	if( deform.m_show_velocity )
	{// Add the velocities
		pr::v4* points		= PR_ALLOCA(pr::v4, dmesh.m_mesh_data->m_num_verts);
		pr::v4* directions	= PR_ALLOCA(pr::v4, dmesh.m_mesh_data->m_num_verts);
		pr::v4* point		= points;
		pr::v4* direction	= directions;
		for( PHuint i = 0; i != dmesh.m_mesh_data->m_num_verts; ++i )
		{
			*point++		= mav4_to_v4(dmesh.m_verts[i]);
			*direction++	= mav4_to_v4(dmesh.m_velocity[i]);
		}
		mat.m_effect = mat_mgr.GetEffect(EEffect_xyzpvc);
		pr::rdr::model::LineD(mlock, points, directions, dmesh.m_mesh_data->m_num_verts, deform.m_velocity_colour, &mat);
	}

	bbox.Reset();
	for( vf::iterator vb = mlock.m_vlock.m_ptr, vb_end = vb + mlock.m_vrange.m_first; vb != vb_end; ++vb )
		Encompase(bbox, v4::make(vb->vertex(), 1.0f));
}

#endif



//// Append a model list to the statics collision model list
//// Returns the base index
//int PhysicsEngine::AppendToStatics(PHmodelListEx& model_list)
//{
//	int base_index = m_statics_ml.GetML() != 0 ? phModelsCount(*m_statics_ml.GetML()) : 0;
//
//	// Unregister the old list
//	if( m_statics_ml.GetML() ) phUnregisterModelList(*m_engine, *m_statics_ml.GetML(), 0);
//
//	// Append the new model list to our one
//	m_statics_ml.Add(model_list);
//
//	// Register the new model list in slot 0
//	if( m_statics_ml.GetML() ) phRegisterModelList(*m_engine, *m_statics_ml.GetML(), 0);
//
//	// Return the collision model index
//	return base_index;
//}

//// Make a linedrawer string for a deformable
//std::string PhysicsEngine::MakeLdrString(std::string const& name, pr::Colour32 colour, DeformableModel const& model, bool show_velocity)
//{
//	PHdeformable::InstancePriv const& dmesh = *model.m_model;
//	
//	std::string str = pr::Fmt("*Group %s FFFFFFFF {\n", name.c_str());
//	if( model.m_model )
//	{
//		std::string col = Fmt("%X", colour.m_aarrggbb);
//		
//		str += "*BoxList anchors FFFF0000 { *Size 0.02\n";
//		for( PHuint i = 0; i != dmesh.m_mesh_data->m_num_anchors; ++i )
//		{
//			pr::v4 pt = mav4_to_v4(dmesh.anchors()[i]);
//			str += pr::Fmt("%f %f %f\n", pt.x ,pt.y ,pt.z);
//		}
//		str += "}\n";
//
//		str += "*Line beams FFFF0000 {\n";
//		for( PHuint i = 0; i != dmesh.m_mesh_data->m_num_beams; ++i )
//		{
//			PHdeformable::Strut const& beam = dmesh.beams()[i];
//			pr::v4 p0 = mav4_to_v4(dmesh.verts()[beam.m_i0]);
//			pr::v4 p1 = mav4_to_v4(dmesh.verts()[beam.m_i1]);
//			str += pr::Fmt("%f %f %f  %f %f %f\n", p0.x ,p0.y ,p0.z ,p1.x ,p1.y ,p1.z);
//		}
//		str += "}\n";
//
//		str += pr::Fmt("*Line springs %s {\n", col.c_str());
//		for( PHuint i = 0; i != dmesh.m_mesh_data->m_num_springs; ++i )
//		{
//			PHdeformable::Strut const& spring = dmesh.springs()[i];
//			pr::v4 p0 = mav4_to_v4(dmesh.verts()[spring.m_i0]);
//			pr::v4 p1 = mav4_to_v4(dmesh.verts()[spring.m_i1]);
//			str += pr::Fmt("%f %f %f  %f %f %f\n", p0.x ,p0.y ,p0.z ,p1.x ,p1.y ,p1.z);
//		}
//		str += "}\n";
//	
//		if( show_velocity )
//		{
//			str += "*LineD velocities FFFFFF00 {\n";
//			for( PHuint i = 0; i != dmesh.m_mesh_data->m_num_verts; ++i )
//			{
//				pr::v4 pt = mav4_to_v4(dmesh.verts()[i]);
//				pr::v4 vel = mav4_to_v4(dmesh.velocity()[i]);
//				str += pr::Fmt("%f %f %f  %f %f %f\n", pt.x ,pt.y ,pt.z, vel.x ,vel.y ,vel.z);
//			}
//			str += "}\n";
//		}
//	}
//	str += "}\n";
//	return str;
//}

//// Make a linedrawer string for a skeleton
//std::string PhysicsEngine::MakeLdrString(std::string const& name, pr::Colour32 colour, Skeleton const& skeleton)
//{
//	PHskeleton::Inst const& skel = *skeleton.m_inst;
//	std::string col = Fmt("%X", colour.m_aarrggbb);
//	
//	std::string str;
//	ldr::StringOutput out(str);
//	str =  "*Mesh " + name + " " + col + " {\n";
//	str += "*Verts {";
//	for( PHuint i = 0; i != skel.m_model->m_num_anchors; ++i )
//	{
//		PHskeleton::Anchor const& a = skel.anchor()[i];
//		str += pr::Fmt("%f %f %f ", a.m_pos[0], a.m_pos[1], a.m_pos[2]);
//	}
//	str += "}\n";
//	str += "*Lines {";
//	for( PHuint i = 0; i != skel.m_model->m_num_struts; ++i )
//	{
//		PHskeleton::Strut const& s = skel.m_model->strut()[i];
//		str += pr::Fmt("%d %d ", s.m_i0, s.m_i1);
//	}
//	str += "}\n";
//	//str += "*LineD vel FFFF0000 {\n";
//	//for( PHuint i = 0; i != skel.m_model->m_num_zones*4; ++i )
//	//{
//	//	PHskeleton::Anchor const& a = skel.anchor()[i];
//	//	str += pr::Fmt("%f %f %f %f %f %f\n" ,a.m_pos[0] ,a.m_pos[1] ,a.m_pos[2] ,a.m_vel[0] ,a.m_vel[1] ,a.m_vel[2]);
//	//}
//	//str += "}\n";
//	str += "}\n";
//	return str;
//}


	//// Build a quad tree for the statics
	//PR_ASSERT(1, m_world_bounds.SizeX() == m_world_bounds.SizeZ());
	//BPstaticQuadTreeBuilder builder;
	//builder.Initialise(100000, 0, 0, m_world_bounds.Lower().x, m_world_bounds.Lower().z, 1, m_world_bounds.SizeX());
	//for( TModel::const_iterator i = m_statics.begin(), i_end = m_statics.end(); i != i_end; ++i )
	//{
	//	const Model& model = *i;
	//	PHushort model_index = static_cast<PHushort>(i - m_statics.begin());
	//	PHcollisionModel& cm = phGetCollisionModel(*m_statics_ml, model_index);

	//	builder.AddStaticObject(BPstaticObject::make(model_index, 0, model_index), v4_to_mav4(model.m_model_to_world.pos), cm.boundingRadius);
	//}

	//builder.CreatePackedData(m_statics_qt_buffer);
	//m_statics_qt = phResolveQuadTree(&m_statics_qt_buffer[0]);
	//phRegisterRegionBroadphase(*m_engine, *m_statics_qt, m_inst_state);

	//// Create instance data for the statics
	//m_inst.resize(m_statics.size());
	//m_inst_data_header.m_instances			= &m_inst[0];
	//m_inst_data_header.m_region_id			= ri::GlobalRegionId();
	//m_inst_data_header.m_num_instances		= static_cast<ri::uint16_t>(m_inst.size());
	//m_inst_data_header.m_physics_only		= m_inst_data_header.m_num_instances;
	//m_inst_data_header.m_anim_static		= m_inst_data_header.m_num_instances;
	//m_inst_data_header.m_smashable			= m_inst_data_header.m_num_instances;
	//m_inst_data_header.m_anim_smashable		= m_inst_data_header.m_num_instances;
	//m_inst_data_header.m_non_physical		= m_inst_data_header.m_num_instances;
	//m_inst_data_header.m_anim_non_physical	= m_inst_data_header.m_num_instances;
	//TInstData::iterator inst = m_inst.begin();
	//for( TModel::const_iterator i = m_statics.begin(), i_end = m_statics.end(); i != i_end; ++i )
	//{
	//	const Model& model = *i;
	//	inst->SetTransform(m4x4_to_mam4(model.m_model_to_world));
	//	inst->SetModelHandle(PI_HANDLE(0, 0));
	//	inst->SetNextGroupInstanceIndex(0);
	//	inst->SetInstanceType(static_inst::EType_Building);
	//	++inst;
	//}

//// Create the skeleton data. Then make an instance of the skeleton
//void PhysicsEngine::CreateSkeleton(parse::Skeleton const& skeleton, CollisionModel const& col_model, Skeleton& skel)
//{
//	PHcollisionModel const& cm = *col_model.m_shape;
//
//	// Allocate the model data buffer in 'skel'
//	PHuint num_anchors	= skeleton.m_anchor.size();
//	PHuint num_struts	= skeleton.m_strut.size() / 2;
//	PHuint model_data_size = PHskeleton::ModelSize(num_anchors, num_struts);
//	PHuint model_data_align = ri::type_traits::alignment_of<PHm4>::value;
//	skel.alloc_skel_data(model_data_size, model_data_align);
//		
//	// Convert types
//	PHv4* anchors = PR_ALLOCA(PHv4, num_anchors);
//	for( PHuint i = 0; i != num_anchors; ++i )
//		anchors[i] = v4_to_mav4(col_model.m_model_to_CoMframe * skeleton.m_anchor[i]);
//
//	// Create the skeleton model data
//	skel.m_model = &phSkeletonBuildModelData(
//		skel.m_skel_data_buffer,
//		model_data_size,
//		num_anchors,
//		anchors,
//		num_struts,
//		&skeleton.m_strut[0]);
//
//	// Allocate the instance data buffer in 'skel'
//	PHuint inst_data_size = PHskeleton::InstSize(*skel.m_model);
//	PHuint inst_data_align = ri::type_traits::alignment_of<PHv4>::value;
//	skel.alloc_skel_inst(inst_data_size, inst_data_align);
//
//	// Create the skeleton instance
//	skel.m_inst = &phSkeletonCreateInstance(
//		skel.m_skel_inst_buffer,
//		inst_data_size,
//		*skel.m_model);
//
//	// Make a copy of the original collision model
//	PHuint cm_size = phColModelSize(cm);
//	PHuint cm_align = PHcollision::ModelAlignment;
//	skel.alloc_ref_cm(cm_size, cm_align);
//	skel.m_ref_cm = reinterpret_cast<PHcollisionModel*>(skel.m_ref_cm_buffer);
//	phColModelClone(cm, skel.m_ref_cm, cm_size);
//
//	skel.m_in_use = true;
//}
