//***************************************************************************
//
//	Instance
//
//***************************************************************************
//
// Definition of the instance base class and built in instances for the renderer
//
// Usage:
//	Client code can use the instance structs provided here or derive there own from
//	InstanceBase. If custom instances are used in conjunction with custom shaders
//	dynamic casts should be used to down-cast the instance struct to the appropriate type.
//
#ifndef PR_RDR_INSTANCE_H
#define PR_RDR_INSTANCE_H

#include "PR/Renderer/Forward.h"
#include "PR/Renderer/RenderState.h"
#include "PR/Renderer/Materials/MaterialMap.h"
#include "PR/Renderer/Models/RenderableBase.h"

namespace pr
{
	namespace rdr
	{
		// This is the type that the renderer deals with. 
		struct InstanceBase
		{
			virtual Material				GetMaterial(uint mat_index) const		{ return m_model->m_material_map[mat_index]; }
			virtual m4x4					GetInstanceToWorld() const				= 0;
			virtual const m4x4*				GetModelToRoot() const					{ return m_model->m_model_to_root; }
			virtual const m4x4*				GetCameraToScreen() const				{ return m_model->m_camera_to_screen; }
			virtual const RenderStateBlock*	GetRenderStates() const					{ return 0; }
			virtual vf::Type				GetVertexType() const					{ return m_model->m_vertex_type; }
			
			RenderableBase*	m_model; // The model this is an instance of
		};

		//****************************************************************************
		// Mix-in classes
		struct Txfm
		{
			const m4x4& GetI2W() const { return m_instance_to_world; }
			m4x4 m_instance_to_world;
		};
		struct ShrdTxfm
		{
			const m4x4& GetI2W() const { return *m_instance_to_world; }
			const m4x4* m_instance_to_world;
		};
		struct RdrStates
		{
			RenderStateBlock m_render_state;
		};
		struct MatMap
		{
			MaterialMap m_material_map;
		};
		
		//****************************************************************************
		// Actual instance structs.
		// These must be resident in the client code during the render call

		//*****
		// A regular instance with an instance to world transform
		struct Instance : InstanceBase, Txfm
		{
			virtual m4x4					GetInstanceToWorld() const			{ return (GetModelToRoot()) ? (*GetModelToRoot() * GetI2W()) : (GetI2W()); }
		};

		//*****
		// An instance with a pointer to an instance to world transform
		struct ShrdTxfmInstance : InstanceBase, ShrdTxfm
		{
			virtual m4x4					GetInstanceToWorld() const			{ return (GetModelToRoot()) ? (*GetModelToRoot() * GetI2W()) : (GetI2W()); }
		};

		//*****
		// An instance with it's own render states
		struct RSInstance : InstanceBase, Txfm, RdrStates
		{
			virtual m4x4					GetInstanceToWorld() const			{ return (GetModelToRoot()) ? (*GetModelToRoot() * GetI2W()) : (GetI2W()); }
			virtual const RenderStateBlock* GetRenderStates() const				{ return &m_render_state; }
		};

		//*****
		// An instance with a material that is changed dynamically
		struct RTMatInstance : InstanceBase, Txfm, MatMap
		{
			virtual m4x4					GetInstanceToWorld() const			{ return (GetModelToRoot()) ? (*GetModelToRoot() * GetI2W()) : (GetI2W()); }
			virtual Material				GetMaterial(uint mat_index) const	{ return m_material_map[mat_index]; }
		};

		//*****
		// An instance with a material that is changed dynamically
		// and a pointer to an instance to world transform
		struct RTMatShrdTxfmInstance : InstanceBase, ShrdTxfm, MatMap
		{
			virtual m4x4					GetInstanceToWorld() const			{ return (GetModelToRoot()) ? (*GetModelToRoot() * GetI2W()) : (GetI2W()); }
			virtual Material				GetMaterial(uint mat_index) const	{ return m_material_map[mat_index]; }
		};

		//*****
		// An instance with it's own render states and a runtime material
		struct RTMatRSInstance : InstanceBase, Txfm, RdrStates, MatMap
		{
			virtual m4x4					GetInstanceToWorld() const			{ return (GetModelToRoot()) ? (*GetModelToRoot() * m_instance_to_world) : (m_instance_to_world); }
			virtual const RenderStateBlock*	GetRenderStates() const				{ return &m_render_state; }
			virtual Material				GetMaterial(uint mat_index) const	{ return m_material_map[mat_index]; }
		};

	}//namespace rdr
}//namespace pr

#endif//PR_RDR_INSTANCE_H
