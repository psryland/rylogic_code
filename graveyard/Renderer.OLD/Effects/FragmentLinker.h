//***************************************************************************
//
//	A base class for shader fragments
//
//***************************************************************************
#ifndef PR_RDR_FRAGMENT_LINKER_H
#define PR_RDR_FRAGMENT_LINKER_H

#include "PR/Common/StdVector.h"
#include "PR/Common/D3DPtr.h"
#include "PR/Renderer/Effects/EffectBase.h"
#include "PR/Renderer/Forward.h"

namespace pr
{
	namespace rdr
	{
        namespace effect
		{
			class FragmentLinker
			{
			public:
				FragmentLinker();
				virtual ~FragmentLinker(){}	// Do not call Release() here

			protected:
				bool	Create(Renderer* renderer);
				void	Release();

				// This should really create shader objects that handle their own releasing...
				bool	BuildVertexShader(const std::vector<D3DXHANDLE>& fragment, D3DPtr<IDirect3DVertexShader9>& vertex_shader);
				bool	BuildPixelShader (const std::vector<D3DXHANDLE>& fragment, D3DPtr<IDirect3DPixelShader9>&  pixel_shader );

				// Overrides
				// Return a filename containing shader fragments or 0 to stop
				// adding fragments. Index increases after each call to this method.
				virtual const char* GetFragmentFilename(uint index) = 0;
				// Add fragment handles to 'm_fragment'. Return true if successful
				virtual bool		GetFragmentHandles() = 0;

			private:
				bool	AddFragments(const char* fragment_filename);
			
			protected:
				D3DPtr<ID3DXFragmentLinker>	m_fragment_linker;
				D3DPtr<ID3DXBuffer>			m_fragment_buffer;
				D3DPtr<ID3DXBuffer>			m_fragment_compile_errors;
				Renderer*					m_renderer;
				std::vector<D3DXHANDLE>		m_fragment;
			};
		}//namespace effect
	}//namespace rdr
}//namespace pr

#endif//PR_RDR_FRAGMENT_LINKER_H
