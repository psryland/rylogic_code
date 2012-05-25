//************************************************************************************
//
//	D3DInstance
//
//************************************************************************************
// Creates a d3d interface and device for use when Direct Graphics is not needed

#ifndef PR_D3D_INSTANCE_H
#define PR_D3D_INSTANCE_H

#include <d3d9.h>
#include <d3dx9.h>
#include <dxerr9.h>
#include "pr/common/D3DHelperFunctions.h"

namespace pr
{
	class D3DInstance
	{
		D3DInstance(LPDIRECT3D9 d3d_interface = 0) : m_interface_needs_releasing(false)
		{
			if( d3d_interface )
			{
				m_d3d_interface = d3d_interface;
			}
			else
			{
				m_d3d_interface = Direct3DCreate9(D3D_SDK_VERSION);
				if( m_d3d_interface == 0 ) return;
				m_interface_needs_releasing = true;
			}

			D3DPRESENT_PARAMETERS present_parameters;
			ZeroMemory(&present_parameters, sizeof(present_parameters));
			present_parameters.Windowed			= TRUE;
			present_parameters.SwapEffect		= D3DSWAPEFFECT_DISCARD;
			present_parameters.BackBufferFormat = D3DFMT_UNKNOWN;

			m_d3d_interface->CreateDevice(D3DADAPTER_DEFAULT, D3DDEVTYPE_REF, 0,
										D3DCREATE_SOFTWARE_VERTEXPROCESSING,
										&present_parameters, &m_d3d_device);
		}
		~D3DInstance()
		{
			if( m_d3d_device ) m_d3d_device->Release();
			if( m_interface_needs_releasing && m_d3d_interface ) m_d3d_interface->Release();
		}

		LPDIRECT3D9			Interface() { return m_d3d_interface;	}
		LPDIRECT3DDEVICE9	Device()	{ return m_d3d_device;		}

	private:
		LPDIRECT3D9				m_d3d_interface;
		LPDIRECT3DDEVICE9		m_d3d_device;
		bool					m_interface_needs_releasing;
	};
}//namespace pr

#endif//PR_D3D_INSTANCE_H
