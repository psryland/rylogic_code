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
	//   Input → Finalise
	//
	// Future tasks (Physics, AI, Particles, etc.) will be inserted
	// between Input and Finalise, with their own dependency edges.
	enum class StepTaskId : int
	{
		Input,       // Process player input, update movement intent
		Finalise,    // Barrier: commit all state snapshots
		Count,
	};

	// Render phase task graph.
	// Dependency DAG:
	//   PrepareFrame → Skybox  ─┐
	//   PrepareFrame → Ocean   ─┼→ Submit
	//   PrepareFrame → Terrain ─┘
	//
	// Skybox, Ocean, and Terrain run in parallel after PrepareFrame.
	// Submit waits for all three before presenting the frame.
	//
	// Thread safety: scene.AddInstance() is NOT thread-safe.
	// Per-system tasks prepare shader constant buffers only.
	// Submit does the actual AddInstance calls serially.
	enum class RenderTaskId : int
	{
		PrepareFrame,  // NewFrame, ClearDrawlists, read state snapshots
		Skybox,        // Skybox rendering
		Ocean,         // Ocean shader CB update + AddToScene
		Terrain,       // Terrain shader CB update + AddToScene
		Submit,        // scene.Render + RenderUI + Present
		Count,
	};
}
