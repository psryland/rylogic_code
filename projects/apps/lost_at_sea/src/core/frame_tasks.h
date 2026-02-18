//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2025
//************************************
// Task IDs for the Step and Render task graphs.
// Each enum defines the tasks in its respective graph and the
// implicit signal namespace for inter-task dependencies.
#pragma once

namespace las
{
	// Step phase task graph.
	// Dependency DAG:
	//   Input → Physics → Finalise
	//
	// Physics steps the ship (and future rigid bodies) between input and finalise.
	enum class StepTaskId : int
	{
		Input,       // Process player input, update movement intent
		Physics,     // Step rigid bodies and ocean-surface constraints
		Finalise,    // Barrier: commit all state snapshots
		Count,
	};

	// Render phase task graph.
	// Dependency DAG:
	//   PrepareFrame → Skybox        ─┐
	//   PrepareFrame → Ocean         ─┤
	//   PrepareFrame → DistantOcean  ─┼→ Submit
	//   PrepareFrame → Terrain       ─┤
	//   PrepareFrame → Ship          ─┘
	//
	// Skybox, Ocean, DistantOcean, Terrain, and Ship run in parallel after PrepareFrame.
	// Submit waits for all before presenting the frame.
	//
	// Thread safety: scene.AddInstance() is NOT thread-safe.
	// Per-system tasks prepare shader constant buffers only.
	// Submit does the actual AddInstance calls serially.
	enum class RenderTaskId : int
	{
		PrepareFrame,    // NewFrame, ClearDrawlists, read state snapshots
		Skybox,          // Skybox rendering
		Ocean,           // Near ocean shader CB update
		DistantOcean,    // Distance ocean shader CB update
		Terrain,         // Terrain shader CB update
		Ship,            // Ship instance transform update
		Submit,          // scene.Render + RenderUI + Present
		Count,
	};
}
