//*********************************************
// Physics engine
//  Copyright (c) Rylogic Ltd 2006
//*********************************************

#pragma once
#ifndef PR_PH_CONSTRAINT_ACCUMULATOR_H
#define PR_PH_CONSTRAINT_ACCUMULATOR_H

#include "pr/common/allocator.h"
#include "pr/physics/types/forward.h"
#include "pr/physics/solver/constraint.h"

namespace pr
{
	namespace ph
	{
		// This object resolves collisions for the scene.
		//
		class ConstraintAccumulator
		{
			enum { ConstraintSetMappingSize = 256 };
			AllocFunction     m_Allocate;                      // Custom allocator for constraint buffer memory
			DeallocFunction   m_Deallocate;                    // Custom deallocator for constraint buffer memory
			Engine&           m_engine;                        // A reference to the engine for pre/post collision call backs
			uint8_t           m_map[ConstraintSetMappingSize]; // A table for forming constraint sets. Last entry (256) reserved for NoConstraintSet
			uint8_t           m_num_sets;                      // The number of constraint sets added so far
			uint8_t*          m_buffer;                        // The buffer of constraint blocks and constraints
			uint8_t*          m_buffer_end;                    // The end of the allocated buffer
			uint8_t*          m_buffer_ptr;                    // A pointer to the next free byte in the buffer
			ConstraintBlock** m_pairs;                         // Pointers to constraint blocks used for sorting
			uint32_t          m_num_pairs;                     // The number of constraint blocks added to the buffer and the length of the 'm_pairs' array
			uint32_t          m_max_pairs;                     // The maximum number of constraint blocks the 'm_pairs' array can contain
			float             m_step_size;                     // The time step (in seconds) we are solving for

			struct SolverParams
			{
				ConstraintAccumulator*	m_this;
				ConstraintBlock**		m_first;
				ConstraintBlock**		m_last;
			};
			ConstraintAccumulator(ConstraintAccumulator const&);				// no copying
			ConstraintAccumulator& operator =(ConstraintAccumulator const&);	// no copying
			ConstraintBlock&	AllocateConstraints(Rigidbody& rbA, Rigidbody& rbB, uint32_t num_constraints);
			void				SetMaterialProperties(Constraint& cons, uint32_t mat_idA, uint32_t mat_idB) const;
			void				SetCollisionMatrix(Constraint& cons, Rigidbody const& rbA, Rigidbody const& rbB, int mass_mask = 3) const;
			static void			SolveConstraintSet(void* context);
			void				SolveConstraintBlock(ConstraintBlock& pair, bool shock_propagation) const;
			uint32_t				CalculateDesiredVelocities(ConstraintBlock& pair, bool shock_propagation) const;

		public:
			ConstraintAccumulator(Engine& engine, pr::AllocFunction allocate, pr::DeallocFunction deallocate);
			~ConstraintAccumulator();

			void SetBufferSize(std::size_t constraint_buffer_size_in_bytes);
			void BeginFrame(float elapsed_seconds);
			void AddContact(Rigidbody& rbA, Rigidbody& rbB, ContactManifold& manifold);
			void Solve();
		};
	}
}

#endif
