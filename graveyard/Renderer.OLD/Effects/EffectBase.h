//***************************************************************************
//
//	A base class for classes that wrap effect files
//
//***************************************************************************
// Usage:
//	Client code should derive an effect wrapper class from this base class
//	for each effect it requires. The effect wrapper class should have access
//	methods to set parameters that they require during rendering (e.g. Renderer*)
//

#ifndef PR_RDR_EFFECT_BASE_H
#define PR_RDR_EFFECT_BASE_H

#include "PR/Common/StdString.h"
#include "PR/Common/D3DPtr.h"
#include "PR/Common/Chain.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
        namespace effect
		{
			class Base : public pr::chain::link<Base, RendererEffectChain>
			{
			public:
				Base();

				// Returns an array of render state types that this effect uses. Specifying
				// a render state type here prevents it being set by the render state manager
				virtual void	GetRenderStates(const D3DRENDERSTATETYPE*& rs, uint& num_rs) const { rs; num_rs = 0; }

				// Called before BeginPass is called for this effect. This would be the place 
				// to call ApplyParameterBlock if the effect has one
				virtual void	PrePass() {}

				// Called before DIP is called on a draw list element and before its states are
				// added to the render state manager. Return true if changes where made
				virtual bool	MidPass(const Viewport&, const DrawListElement&) { return false; }

				// Called after EndPass has been called for this effect
				virtual void	PostPass() {}

				// Create/Release the effect. Note: 'Create/ReCreate' must not throw.
				bool			Create(const char* filename, D3DPtr<IDirect3DDevice9> d3d_device, D3DPtr<ID3DXEffectPool> effect_pool);
				bool			ReCreate(D3DPtr<IDirect3DDevice9> d3d_device, D3DPtr<ID3DXEffectPool> effect_pool);
				void			Release();

				const char*		GetFilename() const						{ return m_name.c_str(); }
				HRESULT			Begin(uint* num_passes, uint flags)		{ PR_ASSERT(PR_DBG_RDR, m_effect); return m_effect->Begin(num_passes, flags); }
				HRESULT			BeginPass(uint pass)					{ PR_ASSERT(PR_DBG_RDR, m_effect); return m_effect->BeginPass(pass); }
				HRESULT			EndPass()								{ PR_ASSERT(PR_DBG_RDR, m_effect); return m_effect->EndPass(); }
				HRESULT			End()									{ PR_ASSERT(PR_DBG_RDR, m_effect); return m_effect->End(); }
				HRESULT			CommitChanges()							{ PR_ASSERT(PR_DBG_RDR, m_effect); return m_effect->CommitChanges(); }

				uint16			m_id;

			protected:
				bool			GetValidTechniques();
				virtual void	GetParameterHandles() = 0;

			protected:
				typedef std::vector<D3DXHANDLE>	TTechniqueContainer;
				
				std::string					m_name;
				D3DPtr<IDirect3DDevice9>	m_d3d_device;
				D3DPtr<ID3DXEffect>			m_effect;
				D3DPtr<ID3DXBuffer>			m_compile_errors;
				TTechniqueContainer			m_technique;
			};

			#ifdef PR_DEBUG_SHADERS
			static const DWORD g_shader_flags = 0 | D3DXSHADER_DEBUG | D3DXSHADER_SKIPOPTIMIZATION;
			#else//PR_DEBUG_SHADERS
			static const DWORD g_shader_flags = 0;
			#endif//PR_DEBUG_SHADERS
		}//namespace effect
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_EFFECT_BASE_H
