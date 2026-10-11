//*********************************************
// Physics Engine
//  Copyright (C) Rylogic Ltd 2026
//*********************************************
#if PR_UNITTESTS
#include "pr/common/unittests.h"
#include "src/unittests/shared_gpu.h"

namespace pr::physics::tests
{
	// Check the resource lifetime guarantees of command list recording.
	PRUnitTestClass(GpuTests)
	{
		// A resource released by its owner while recorded work uses it must stay alive until the GPU has finished that work.
		PRUnitTestMethod(UsedResourceAliveUntilJobCompletes, Quick)
		{
			// Record work that uses a resource. Repeated use within one recording must not add more references.
			auto& gpu = SharedTestGpu();
			auto& job = gpu.m_job;
			auto desc = compute::ResDesc::Buf<uint32_t>(16, {}).usage(compute::EUsage::UnorderedAccess);
			auto res = gpu.CreateResource(desc, job.m_cmd_list, "GpuTests:Used");
			job.m_barriers.Transition(res.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
			job.m_barriers.Commit();
			job.m_cmd_list.Use(res.get());

			// Hold a raw reference so the reference count can be read without touching a released object.
			auto* raw = res.get();
			raw->AddRef();

			// Releasing the owner's reference must leave the command list holding exactly one reference.
			res = nullptr;
			PR_EXPECT(raw->AddRef() == 3 && raw->Release() == 2);

			// 'Close' hands the reference to the allocator pool, and resetting for the next recording after the GPU wait releases it.
			job.Run();
			PR_EXPECT(raw->Release() == 0);
		}
	};
}
#endif
