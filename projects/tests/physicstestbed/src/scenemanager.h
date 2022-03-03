//******************************************
// SceneManager
//******************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"
#include "PhysicsTestbed/Terrain.h"
#include "PhysicsTestbed/Static.h"
#include "PhysicsTestbed/Prop.h"
#include "PhysicsTestbed/Graphics.h"
#include "PhysicsTestbed/Transients.h"
#include "PhysicsTestbed/ParseOutput.h"
#include "PhysicsTestbed/CollisionCallBacks.h"

class SceneManager
{
public:
	SceneManager(PhysicsEngine* engine);
	~SceneManager();
	void PrePhysicsStep();
	void Step(float step_size);
	void UpdateTransients();
	void Clear();
	void ClearTerrain();
	void ClearStatics();
	void ClearProps();
	void ClearGraphics();
	void ClearContacts();
	void ClearImpulses();
	void ClearRayCasts();
	void ClearGravityFields();
	void ClearDrag();
	void AddToScene			(parse::Output const& output);
	void AddGraphics		(parse::Gfx const& gfx);
	void AddTerrain			(parse::Terrain const& terrain);
	Static* AddStatic		(parse::Output const& output, parse::Static const& statik);
	Prop*	AddPhysicsObject(parse::Output const& output, parse::PhysObj const& phys);
	Prop*	AddMultibody	(parse::Output const& output, parse::Multibody const& multi, Prop* parent = 0);
	void AddMaterial		(parse::Material const& material);
	void AddGravityField	(parse::Gravity const& grav);
	void AddDrag			(float drag);
	void AddContact(const pr::v4& pt, const pr::v4& norm);
	void AddImpulse(const pr::v4& pt, const pr::v4& impulse);
	void AddRayCast(const pr::v4& start, const pr::v4& end);
	void CreateBox();
	void CreateCylinder();
	void CreateSphere();
	void CreatePolytope();
	void CreateDeformableMesh();
	void CastRay(bool apply_impulse);
	void TerrainSampler(bool show);
	void ViewStateUpdate();
	void DeleteObject(pr::ldr::ObjectHandle object);
	void EnsureFreePhysicsObject();
	void ExportScene(const char* filename, bool physics_scene);
	Prop* GetPropFromPhysObj(PhysObj const* obj);
	void PstCollisionCallBack(col::Data const& col_data);

private:
	PhysicsEngine*		m_physics_engine;
	TTerrain			m_terrain;			// The terrain data
	TStatic				m_static;			// The static instances
	TProp				m_prop;				// The dynamic instances
	TGraphics			m_graphics;			// Non physical instances
	TContact			m_contact;
	TImpulse			m_impulse;
	TRayCast			m_raycast;
	pr::ldr::ObjectHandle m_ldr_terrain_sampler;
	pr::BoundingBox		m_world_bounds;
	float				m_drag;
	float				m_scale;
};

