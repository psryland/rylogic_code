//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

// Manager for creating/destroying vertex declarations

#pragma once
#ifndef PR_RDR_VERTEX_FORMAT_MANAGER_H
#define PR_RDR_VERTEX_FORMAT_MANAGER_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/vertexformats/vertexformat.h"

namespace pr
{
	namespace rdr
	{
		class VertexFormatManager
			:public pr::events::IRecv<pr::rdr::Evt_DeviceLost>
			,public pr::events::IRecv<pr::rdr::Evt_DeviceRestored>
		{
			D3DPtr<IDirect3DDevice9>            m_d3d_device;
			D3DPtr<IDirect3DVertexDeclaration9> m_vd[vf::EVertType::NumberOf];
			
			void OnEvent(pr::rdr::Evt_DeviceLost const&);
			void OnEvent(pr::rdr::Evt_DeviceRestored const&);
			
		public:
			VertexFormatManager(D3DPtr<IDirect3DDevice9> d3d_device);
			~VertexFormatManager();
			
			D3DPtr<IDirect3DVertexDeclaration9> GetVertexDeclaration(vf::Type type) const { return m_vd[type]; };
		};
	}
}

#endif
