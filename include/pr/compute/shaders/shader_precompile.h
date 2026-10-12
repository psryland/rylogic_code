//*********************************************
// Compute
//  Copyright (c) Rylogic Ltd 2026
//*********************************************
#pragma once
#include "pr/compute/forward.h"
#include "pr/compute/shaders/shader_cache.h"

namespace pr::compute
{
	// One runtime-compiled shader that can be compiled ahead of its first use.
	// 'm_compile' must be the same function the owning pipeline uses to compile the shader, so that both produce the same cache key.
	// It must be safe to call from any thread.
	struct ShaderRecipe
	{
		using CompileFn = std::function<std::vector<uint8_t>(shader_cache::IShaderCache* cache)>;

		std::string m_name;
		CompileFn m_compile;
	};

	// Reports progress on the calling thread as 'completed' of 'total' recipes. Return false to cancel the remaining recipes.
	using ShaderPrecompileProgress = std::function<bool(int completed, int total)>;

	// Compile 'recipes' into 'cache' using up to 'thread_count' worker threads, blocking until they finish or are cancelled.
	// 'progress' is called on the calling thread once at the start and after each completed recipe.
	// Cancellation stops new recipes from starting and waits for running ones. The first compile error is rethrown after all workers stop.
	void PrecompileShaders(std::span<ShaderRecipe const> recipes, shader_cache::IShaderCache& cache, int thread_count, ShaderPrecompileProgress const& progress);
}
