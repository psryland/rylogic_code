//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/sampler/sampler.h"
#include "pr/view3d-12/sampler/sampler_desc.h"
#include "pr/view3d-12/resource/resource_manager.h"
#include "pr/view3d-12/resource/descriptor_store.h"

namespace pr::rdr12
{
	Sampler::Sampler(ResourceManager& mgr, SamplerDesc const& desc)
		:RefCounted<Sampler>()
		,m_mgr(&mgr)
		,m_id(desc.m_id == AutoId ? MakeId(this) : desc.m_id)
		,m_samp()
		,m_name(desc.m_name)
	{
		m_samp = m_mgr->m_descriptor_store.Create(desc.m_sdesc);
	}
	Sampler::~Sampler()
	{
		OnDestruction(*this, EmptyArgs());

		// Release the sampler resource
		if (m_samp)
			m_mgr->m_descriptor_store.Release(m_samp);
	}

	// Access the renderer
	Renderer& Sampler::rdr() const
	{
		return m_mgr->rdr();
	}

	// Ref counting clean up function
	void Sampler::RefCountZero(RefCounted<Sampler>* doomed)
	{
		auto sam = static_cast<Sampler*>(doomed);
		sam->Delete();
	}
	void Sampler::Delete()
	{
		m_mgr->Delete(this);
	}
}
