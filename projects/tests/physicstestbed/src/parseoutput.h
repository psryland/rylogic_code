//******************************************
// Parse Output
//******************************************
#pragma once

#include "pr/common/Fmt.h"
#include "pr/common/StdString.h"
#include "pr/common/StdVector.h"
#include "pr/maths/maths.h"
#include "pr/geometry/colour.h"

// Structures produced by the parser after reading one or more source files
namespace parse
{
	enum EObjectType
	{
		EObjectType_None = 0,
		EObjectType_Position,
		EObjectType_Direction,
		EObjectType_Transform,
		EObjectType_Velocity,
		EObjectType_AngVelocity,
		EObjectType_Gravity,
		EObjectType_Mass,
		EObjectType_Name,
		EObjectType_ByName,
		EObjectType_Colour,
		EObjectType_DisableRender,
		EObjectType_Stationary,
		EObjectType_Gfx,
		EObjectType_Terrain,
		EObjectType_Material,
		EObjectType_GravityField,
		EObjectType_Drag,
		EObjectType_Model,
		EObjectType_ModelByName,
		EObjectType_Deformable,
		EObjectType_DeformableByName,
		EObjectType_StaticObject,
		EObjectType_PhysicsObject,
		EObjectType_PhysObjByName,
		EObjectType_Multibody,
		EObjectType_Unknown,
	};

	typedef std::vector<pr::v4> TPoints;
	typedef std::vector<pr::uint> TIndices;

	struct Gfx
	{
		std::string				m_ldr_str;			// String describing the graphics object
	};
	typedef std::vector<Gfx> TGfx;

	struct Terrain
	{
		enum EType
		{
			EType_None,
			EType_Reflections_2D,
			EType_Reflections_3D
		};
		Terrain() { clear(); }
		void clear()
		{
			m_ldr_str = "";
			m_data = "";
			m_colour = 0x8000A000;
		}
		EType					m_type;				// The type of terrain used
		std::string				m_ldr_str;			// Ldr script representing the terrain
		std::string				m_data;				// Terrain data
		pr::Colour32			m_colour;			// Colour to draw the terrain
	};
	typedef std::vector<Terrain> TTerrain;

	struct Material
	{
		Material() { clear(); }
		void clear()
		{
			m_density					= 2.0f;
			m_static_friction			= 0.5f;
			m_dynamic_friction			= 0.5f;
			m_rolling_friction			= 0.0f;
			m_elasticity				= 0.8f;
			m_tangential_elasiticity	= 0.0f;
			m_tortional_elasticity		= -1.0f;
		}
		float m_density;
		float m_static_friction;
		float m_dynamic_friction;
		float m_rolling_friction;
		float m_elasticity;
		float m_tangential_elasiticity;
		float m_tortional_elasticity;
	};

	struct Gravity
	{
		Gravity() : m_type(EType_Directional), m_direction(-pr::v4YAxis), m_strength(10.0f) {}
		enum EType { EType_Radial, EType_Directional };
		EType m_type;
		union {
		pr::v4 m_direction;
		pr::v4 m_centre;
		};
		float m_strength;
	};
	typedef std::vector<Gravity> TGravity;

	struct Prim
	{
		enum EType { EType_Box, EType_Cylinder, EType_Sphere, EType_Polytope, EType_PolytopeExplicit, EType_Triangle };
		Prim() { clear(); }
		void clear()
		{
			m_type			= EType_Box;
			m_radius		.set(1.0f, 1.0f, 1.0f, 0.0f);
			m_vertex		.clear();
			m_face			.clear();
			m_anchor		.clear();
			m_prim_to_model	.identity();
			m_colour		= pr::Colour32White;
			m_bbox			.reset();
		}
		EType					m_type;				// Box, Cylinder, Sphere, Polytope
		pr::v4					m_radius;			// Radius of the primitive
		TPoints					m_vertex;			// Verts used if this is a polytope
		TIndices				m_face;				// Faces of the mesh if this is a PolytopeExplicit
		TPoints					m_anchor;			// Anchor points for this primitive
		pr::m4x4				m_prim_to_model;	// Primitive to model transform
		pr::Colour32			m_colour;			// Colour of the primitive
		pr::BoundingBox			m_bbox;				// Primitive space bounding box
	};
	typedef std::vector<Prim> TPrim;

	struct Skeleton
	{
		Skeleton()
		:m_anchor()
		,m_strut()
		,m_colour(pr::Colour32White)
		,m_render(true)
		{}
		bool has_data() const						{ return !m_anchor.empty() && !m_strut.empty(); }
		TPoints					m_anchor;			// Anchor points for the primitives
		TIndices				m_strut;			// Edges between anchors forming a grid skeleton for this model
		pr::Colour32			m_colour;			// Colour of the skeleton
		bool					m_render;			// True if we should render the skeleton
	};
	typedef std::vector<Skeleton> TSkeleton;

	struct Model
	{
		Model()
		:m_model_to_world(pr::m4x4Identity)
		,m_name("model")
		,m_colour(pr::Colour32Black)
		,m_bbox(pr::BBoxReset)
		{}
		bool has_data() const						{ return !m_prim.empty(); }
		TPrim					m_prim;				// Primitives that this model is constructed from
		pr::m4x4				m_model_to_world;	// Model to world (statics) or model to instance (dynamics) transform
		std::string				m_name;				// Name of the collision model
		pr::Colour32			m_colour;			// A colour for the model
		pr::BoundingBox			m_bbox;				// Model space bounding box
		Skeleton				m_skel;				// A skeleton for the model
	};
	typedef std::vector<Model> TModel;

	struct Deformable
	{
		Deformable()
		:m_model_to_world(pr::m4x4Identity)
		,m_name("deformable")
		,m_colour(pr::Colour32White)
		,m_bbox(pr::BBoxReset)
		,m_springs_colour(pr::Colour32Blue)
		,m_beams_colour(pr::Colour32Red)
		,m_spring_constant(1.0f)
		,m_damping_constant(0.0f)
		,m_sprain_percentage(-1.0f)
		,m_convex_tolerance(0.1f)
		,m_generate_col_models(true)
		{}
		bool has_data() const	{ return !m_tmesh_verts.empty() || !m_smesh_verts.empty() || !m_anchors.empty(); }
		TPoints					m_tmesh_verts;		// The verts of the tetramesh
		TPoints					m_smesh_verts;		// The verts that are part of the spring mesh only
		TPoints					m_anchors;			// The verts that are fixed in model space
		TIndices				m_tetras;			// The tetrahedra of the deformable
		TIndices				m_springs;			// Spring connections between verts
		TIndices				m_beams;			// Rigid connections between verts
		pr::m4x4				m_model_to_world;	// Model to world (statics) or model to instance (dynamics) transform
		std::string				m_name;				// Name of the deformable
		pr::Colour32			m_colour;			// Colour of the deformable
		pr::BoundingBox			m_bbox;				// Bounding box for the deformable
		pr::Colour32			m_springs_colour;	// The colour to render the springs;
		pr::Colour32			m_beams_colour;		// The colour to render the beams;
		float					m_spring_constant;	// The spring constant for the springs
		float					m_damping_constant;	// The damping constant for the springs
		float					m_sprain_percentage;// The percentage limit before the spring rest length changes
		float					m_convex_tolerance;	// The tolerance to use when decomposing the mesh
		bool					m_generate_col_models;// True if the deformable should be decomposed and a new collision model generated
	};
	typedef std::vector<Deformable> TDeformable;

	struct Static
	{
		Static()
		:m_name("static_object")
		,m_model_index(0xFFFFFFFF)
		,m_inst_to_world(pr::m4x4Identity)
		,m_colour(pr::Colour32Black)
		,m_bbox(pr::BBoxReset)
		{}
		std::string				m_name;				// A name for the static
		std::size_t				m_model_index;		// The index of the collision model
		pr::m4x4				m_inst_to_world;	// Instance to world transform
		pr::Colour32			m_colour;			// Colour to override the model with
		pr::BoundingBox			m_bbox;				// Bounding box for the static
	};
	typedef std::vector<Static> TStatic;

	struct PhysObj
	{
		static int Id() { static int id = 0; return ++id; }
		PhysObj()
		:m_name(pr::Fmt("physics_object_%d", Id()))
		,m_model_type(EObjectType_None)
		,m_object_to_world(pr::m4x4Identity)
		,m_gravity(pr::v4Zero)
		,m_velocity(pr::v4Zero)
		,m_ang_velocity(pr::v4Zero)
		,m_mass(0.0f)
		,m_colour(pr::Colour32Black)
		,m_bbox(pr::BBoxReset)
		,m_by_name_only(false)
		,m_stationary(false)
		{}
		std::string				m_name;				// Name for the physics object
		EObjectType				m_model_type;		// Either EObjectType_None, EObjectType_Model or EObjectType_Deformable
		std::size_t				m_model_index;		// The index of the collision model
		pr::m4x4				m_object_to_world;	// Object to world transform
		pr::v4					m_gravity;			// Gravity
		pr::v4					m_velocity;			// Initial linear velocity
		pr::v4					m_ang_velocity;		// Initial angular velocity
		float					m_mass;				// Mass of the object
		pr::Colour32			m_colour;			// Colour to override the model/deformable with
		pr::BoundingBox			m_bbox;				// Bounding box for the physics object
		bool					m_by_name_only;		// True if we don't want to create one of these (only use it by name)
		bool					m_stationary;		// True if we want to hold the physics object at the start position
	};
	typedef std::vector<PhysObj> TPhysObj;

	struct Multibody
	{
		static int Id() { static int id = 0; return ++id; }
		Multibody()
		:m_name(pr::Fmt("multibody_%d", Id()))
		,m_phys_obj_index(0xFFFFFFFF)
		,m_object_to_world(pr::m4x4Identity)
		,m_ps_attach(pr::m3x3::make(pr::v4Zero, pr::v4YAxis, pr::v4ZAxis))
		,m_os_attach(pr::m3x3::make(pr::v4Zero, pr::v4YAxis, pr::v4ZAxis))
		,m_gravity(pr::v4Zero)
		,m_velocity(pr::v4Zero)
		,m_ang_velocity(pr::v4Zero)
		,m_colour(pr::Colour32Black)
		,m_bbox(pr::BBoxReset)
		,m_joint_type(0)
		,m_pos(0.0f)
		,m_vel(0.0f)
		,m_lower_limit(-pr::maths::float_max)
		,m_upper_limit(pr::maths::float_max)
		,m_restitution(1.0f)
		,m_joint_zero(0.0f)
		,m_joint_spring(0.0f)
		,m_joint_damping(0.0f)
		{}
		std::string				m_name;				// A name for the multi
		std::size_t				m_phys_obj_index;	// The index of the physics object attached by this joint
		pr::m4x4				m_object_to_world;	// Object to world for the base of the multi, overrides the physics object
		pr::m3x3				m_ps_attach;		// x=point, y=axis, z=zero for the attachment point (in parent space)
		pr::m3x3				m_os_attach;		// x=point, y=axis, z=zero for the attachment point (in object space)
		pr::v4					m_gravity;			// Gravity, overrides the physics object
		pr::v4					m_velocity;			// Initial linear velocity, overrides the physics object
		pr::v4					m_ang_velocity;		// Initial angular velocity, overrides the physics object
		pr::Colour32			m_colour;			// Colour, overrides the physics object
		pr::BoundingBox			m_bbox;				// Bounding box for the multi
		int						m_joint_type;		// Joint type 0=floating, 1=revolute 2=prismatic
		float					m_pos;				// Joint position
		float					m_vel;				// Joint velocity
		float					m_lower_limit;		// Lower joint limit
		float					m_upper_limit;		// Upper joint limit
		float					m_restitution;		// Joint restitution
		float					m_joint_zero;		// Zero point for the joint
		float					m_joint_spring;		// Joint spring force
		float					m_joint_damping;	// Joint damping
		std::vector<Multibody>	m_joints;			// Children of the multi
	};
	typedef std::vector<Multibody> TMultibody;

	// The parser modifies one of these objects
	struct Output
	{
		Output() { Clear(); }
		void Clear()
		{
			m_graphics.clear();
			m_terrain.clear();
			m_material.clear();
			m_gravity.clear();
			m_drag = 0.0f;
			m_models.clear();
			m_deformables.clear();
			m_statics.clear();
			m_phys_obj.clear();
			m_multis.clear();
			m_world_bounds.reset();
		}
		
		TGfx					m_graphics;		// Non-physical objects
		TTerrain				m_terrain;		// Terrain object
		Material				m_material;		// The physics material to use for everything
		TGravity				m_gravity;		// Gravity sources
		float					m_drag;			// Drag to apply to moving objects
		TModel					m_models;		// Models
		TDeformable				m_deformables;	// Deformables
		TStatic					m_statics;		// Static physics objects
		TPhysObj				m_phys_obj;		// Dynamic physics objects
		TMultibody				m_multis;		// Multi body objects
		pr::BoundingBox			m_world_bounds;	// A bounding box for all objects in the scene
	};

}//namespace parse

