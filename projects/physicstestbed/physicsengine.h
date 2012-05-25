//******************************************
// PhysicsEngine
//******************************************
#pragma once

#include "PhysicsTestbed/Forwards.h"
#include "pr/linedrawer/plugininterface.h"
#include "PhysicsTestbed/Static.h"
#include "PhysicsTestbed/CollisionCallBacks.h"

enum EPhysObjType
{
	EPhysObjType_Dynamic,
	EPhysObjType_Static,
	EPhysObjType_Terrain,
	EPhysObjType_Keyframed,
};
enum EMotionType
{
	EMotionType_Static,
	EMotionType_Ballistic,
	EMotionType_Sleeping,
	EMotionType_InfiniteMass,
	EMotionType_Animated,
};

// Structure for constructing and maintaining a physics engine (reflections or rylogic)
struct PhysicsEnginePrivate;
class PhysicsEngine
{
public:
	PhysicsEngine();
	~PhysicsEngine();

	void			Sync();
	void			Step();
	void			SetTimeStep(float step_size_in_seconds);
	unsigned int	GetFrameNumber() const;
	void			Clear();
	std::size_t		GetMaxObject() const;
	std::size_t		GetNumObjects() const;
	void			SetMaterial(const parse::Material& material);
	void			AddGravityField(const parse::Gravity& gravity);
	void			ClearGravityFields();
	void			SetDefaultTerrain();
	void			SetTerrain(const parse::Terrain& terrain);
	void			GetTerrainDimensions(float& terr_x, float& terr_z, float& terr_w, float& terr_d);
	void			SampleTerrain(pr::v4 const& point, float& height, pr::v4& normal);
	void			CastRay(const pr::v4& point, const pr::v4& direction, float& intercept, pr::v4& normal, PhysObj*& hit_object, pr::uint& prim_id);
	std::string		CreateTerrainSampler(pr::v4 const& point);

	// Static objects
	void			ClearStaticSceneData();
	void			CreateStaticCollisionModel(parse::Model const& model, CollisionModel& col_model, std::string& ldr_string);
	void			RebuildStaticScene(TStatic const& statics, pr::BoundingBox const& world_bounds);

	// Object functions
	void					CreateCollisionModel	(parse::Model const& model, CollisionModel& col_model);
	void					CreatePhysicsObject		(parse::PhysObj const& phys, CollisionModel const& col_model, void* user_data, PhysObj*& phys_obj);
	void					DeletePhysicsObject		(PhysObj*& phys_obj);
	void					CreateDeformableModel	(parse::Deformable const& deformable, DeformableModel& def_model);
	void					CreateSkeleton			(parse::Skeleton const& skeleton, CollisionModel const& col_model, Skeleton& skel);
	static pr::m4x4			ObjectToWorld			(PhysObj const* phys_obj);
	static pr::v4			ObjectGetVelocity		(PhysObj const* phys_obj);
	static pr::v4			ObjectGetVelocity		(PhysObj const* phys_obj, pr::v4 const& ws_point);
	static pr::v4			ObjectGetAngVelocity	(PhysObj const* phys_obj);
	static pr::v4			ObjectGetAngMomentum	(PhysObj const* phys_obj);
	static pr::BoundingBox	ObjectGetWSBbox			(PhysObj const* phys_obj);
	static pr::BoundingBox	ObjectGetOSBbox			(PhysObj const* phys_obj);
	static void*			ObjectGetPreColData		(PhysObj const* phys_obj);
	static void*			ObjectGetPstColData		(PhysObj const* phys_obj);
	static float			ObjectGetMass			(PhysObj const* phys_obj);
	static pr::m3x3			ObjectGetOSInertia		(PhysObj const* phys_obj);
	static pr::m3x3			ObjectGetWSInvInertia	(PhysObj const* phys_obj);
	static EPhysObjType		ObjectGetPhysObjType	(PhysObj const* phys_obj);
	static void				ObjectRestingContacts	(PhysObj const* phys_obj, pr::v4* contacts, pr::uint max_contacts, pr::uint& count);
	static bool				ObjectIsSleeping		(PhysObj const* phys_obj);
	static void				SetObjectToWorld		(PhysObj*		phys_obj, pr::m4x4 const& o2w);
	static void				ObjectSetGravity		(PhysObj*       phys_obj);
	static void				ObjectSetVelocity		(PhysObj*		phys_obj, pr::v4 const& vel);
	static void				ObjectSetAngVelocity	(PhysObj*		phys_obj, pr::v4 const& ang_vel);
	static void				ObjectWakeUp			(PhysObj*		phys_obj);
	static void				ObjectApplyImpulse		(PhysObj*		phys_obj, pr::v4 const& ws_impulse, pr::v4 const& ws_pos);
	static void				ObjectSetColModel		(PhysObj*		phys_obj, CollisionModel const& col_model, pr::m4x4 const& o2w);

	// Deformables
	static bool				Deform				(CollisionModel& col_model, PhysObj const* objA, PhysObj const* objB, col::Contact const& ct);
	static void				DeformableTransform	(DeformableModel& deform, pr::m4x4 const& transform);
	static void				DeformableImpact	(DeformableModel& deform, pr::v4 const& point, pr::v4 const& normal, pr::v4 const& delta_vel);
	static bool				DeformableEvolve	(DeformableModel& deform, float step_size, bool to_equilibrium);
	static void				DeformableDecompose	(DeformableModel& deform, CollisionModel& col_model);
	static void				SkeletonDeform		(Skeleton& skel, pr::v4 const& ms_point, pr::v4 const& ms_norm, pr::v4 const& ms_deltavel);
	static bool				SkeletonEvolve		(Skeleton& skel, float step_size);
	static void				SkeletonMorphCM		(Skeleton const& skel, CollisionModel& col_model);
	
	// Multibody functions
	static void				MultiAttach			(PhysObj*		phys_obj, PhysObj* parent, parse::Multibody const& multi_info);
	static void				MultiBreak			(PhysObj*		phys_obj);

	// Ldr Helper functions
	static std::string		MakeLdrString			(std::string const& name, pr::Colour32 colour, CollisionModel const& col_model);
	static std::string		MakeLdrString			(std::string const& name, pr::Colour32 colour, DeformableModel const& model, bool show_velocity);
	static void				MakeLdrObjectDeformable	(pr::rdr::Model* model, pr::BoundingBox& bbox, void* user_data, pr::rdr::MaterialManager& mat_mgr);
	static std::string		MakeLdrString			(std::string const& name, pr::Colour32 colour, Skeleton const& skeleton);
	static void				MakeLdrObject			(pr::rdr::Model* model, pr::BoundingBox& bbox, void* user_data, pr::rdr::MaterialManager& mat_mgr);

private:
	PhysicsEnginePrivate* m_data;
	unsigned int m_frame_number;
};
