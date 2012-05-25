//******************************************
// CollisionModel
//******************************************
#pragma once
#ifdef USE_REFLECTIONS_ENGINE
	#include "Physics/Include/Physics.h"
	#include "Physics/Include/PhysicsDev.h"
//	#include "Physics/Deformation/PHrigidbodySkeleton.h"
#else//USE_REFLECTIONS_ENGINE
#endif//USE_REFLECTIONS_ENGINE
#include "PhysicsTestbed/Ldr.h"
#include "PhysicsTestbed/CollisionModel.h"

struct Skeleton
{
	void*			m_skel_data_buffer;		// A buffer to hold the skeleton const data
	void*			m_skel_inst_buffer;		// A buffer to hold the skeleton instance
	void*			m_ref_cm_buffer;		// A buffer to hold the reference collision model
	bool			m_in_use;				// True if this skeleton has data
	Ldr				m_gfx;					// Graphic for the skeleton
	bool			m_render_skel;			// True if we should draw the skeleton

	#ifdef USE_REFLECTIONS_ENGINE
	//PHskeleton::Model*	m_model;				// The skeleton model (contains const data)
	//PHskeleton::Inst*	m_inst;					// The skeleton instance (contains mutable data)
	//PHcollisionModel*	m_ref_cm;				// The un-deformed collision model
	#else//USE_REFLECTIONS_ENGINE
	#endif//USE_REFLECTIONS_ENGINE

	Skeleton()
	:m_skel_data_buffer(0)
	,m_skel_inst_buffer(0)
	,m_ref_cm_buffer(0)
	,m_in_use(false)
	{}
	~Skeleton()
	{
		_aligned_free(m_skel_data_buffer);
		_aligned_free(m_skel_inst_buffer);
		_aligned_free(m_ref_cm_buffer);
	}
	void alloc_skel_data(std::size_t size, std::size_t align)	{ m_skel_data_buffer	= _aligned_malloc(size, align); }
	void alloc_skel_inst(std::size_t size, std::size_t align)	{ m_skel_inst_buffer	= _aligned_malloc(size, align); }
	void alloc_ref_cm	(std::size_t size, std::size_t align)	{ m_ref_cm_buffer		= _aligned_malloc(size, align); }
};
