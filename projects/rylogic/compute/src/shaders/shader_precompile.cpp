//*********************************************
// Compute
//  Copyright (c) Rylogic Ltd 2026
//*********************************************
#include "pr/compute/shaders/shader_precompile.h"

namespace pr::compute
{
	// Compile 'recipes' into 'cache' using up to 'thread_count' worker threads.
	void PrecompileShaders(std::span<ShaderRecipe const> recipes, shader_cache::IShaderCache& cache, int thread_count, ShaderPrecompileProgress const& progress)
	{
		// Workers claim recipes in order and report completions to the calling thread, which owns progress reporting and cancellation.
		auto const total = static_cast<int>(recipes.size());
		auto mutex = std::mutex{};
		auto changed = std::condition_variable{};
		auto next = 0;
		auto completed = 0;
		auto running = 0;
		auto cancelled = false;
		auto failure = std::exception_ptr{};
		auto worker = [&]
		{
			// Each worker compiles one recipe at a time until none remain, or until the caller cancels or a compile fails.
			for (;;)
			{
				int index;
				{
					std::lock_guard lock(mutex);
					if (cancelled || failure || next == total)
						break;

					index = next++;
				}

				// Compile outside the lock. Each compile uses its own DXC instance, and the cache serialises access to each key.
				auto error = std::exception_ptr{};
				try
				{
					recipes[index].m_compile(&cache);
				}
				catch (...)
				{
					error = std::current_exception();
				}

				// Record the result and wake the reporting thread.
				{
					std::lock_guard lock(mutex);
					if (error && !failure)
						failure = error;

					++completed;
				}
				changed.notify_all();
			}

			// Let the reporting thread know when the last worker has stopped.
			{
				std::lock_guard lock(mutex);
				--running;
			}
			changed.notify_all();
		};

		// Start no more workers than there are recipes. The progress call before starting lets the caller cancel at no cost.
		auto const workers = std::clamp(thread_count, 1, std::max(total, 1));
		if (!progress(0, total))
			return;

		auto threads = std::vector<std::jthread>{};
		threads.reserve(workers);
		running = workers;
		for (int i = 0; i != workers; ++i)
			threads.emplace_back(worker);

		// Report each new completion until every worker has stopped. Progress is called without the lock so it can take its time.
		auto reported = 0;
		for (;;)
		{
			int done;
			bool stopped;
			{
				std::unique_lock lock(mutex);
				changed.wait(lock, [&] { return completed != reported || running == 0; });
				done = completed;
				stopped = running == 0;
			}
			if (done != reported)
			{
				reported = done;
				if (!progress(done, total))
				{
					std::lock_guard lock(mutex);
					cancelled = true;
				}
			}
			if (stopped)
				break;
		}
		threads.clear();

		// Report a compile failure only after all workers have stopped using the recipes and cache.
		if (failure)
			std::rethrow_exception(failure);
	}
}
