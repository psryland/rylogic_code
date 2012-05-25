//**********************************************************************
//
//	Configure.
//
//**********************************************************************
// A collection of global functions for configuring the renderer.
#ifndef RDR_CONFIGURE_H
#define RDR_CONFIGURE_H

#include "PR/Common/PRTypes.h"
#include "PR/Common/PRAssert.h"
#include "PR/Common/D3DPtr.h"
#include "PR/Renderer/D3DHeaders.h"
#include "PR/Renderer/RendererAssertEnable.h"
#include "PR/Renderer/Forward.h"
#include "PR/Renderer/Errors.h"

namespace pr
{
	template <typename T>
	T Zero() { T t; memset(&t, 0, sizeof(T)); return t; }

	namespace rdr
	{
		// A device with which to initialise d3d
		struct DeviceConfig
		{
			uint				m_adapter_index;	// The ordinal of the adapter this device belongs to
			D3DDEVTYPE			m_device_type;		// The type of device to create
			D3DCAPS9			m_caps;				// Capabilities of this device
			uint				m_behavior;			// Hardware / Software / Mixed vertex processing
			D3DDISPLAYMODE		m_display_mode;		// The screen size, format, and refresh rate
			bool				m_windowed;			// True if this is a config for windowed moded
		};

		DeviceConfig GetDefaultDeviceConfig(bool windowed);
		DeviceConfig AutoDetectDeviceConfig(D3DDISPLAYMODE wanted_display_mode, bool windowed, D3DDEVTYPE device_type = D3DDEVTYPE_HAL, bool software_vertex_processing = false);

		// Display modes supported by an adapter
		struct DisplayModeIter
		{
			DisplayModeIter(const pr::rdr::Adapter* adapter, uint mode_index, uint last_mode_index);

			D3DDISPLAYMODE		operator * () const	{ return m_display_mode; }
			D3DDISPLAYMODE*		operator ->()		{ return &m_display_mode; }
			DisplayModeIter&	operator ++();
			DisplayModeIter&	operator --();
			bool IsValid();

			const Adapter*		m_adapter;
			uint				m_mode_index;
			uint				m_last_mode_index;
			D3DDISPLAYMODE		m_display_mode;
		};
		inline bool operator == (const DisplayModeIter& lhs, const DisplayModeIter& rhs) { return lhs.m_adapter == rhs.m_adapter && lhs.m_mode_index == rhs.m_mode_index; }
		inline bool operator != (const DisplayModeIter& lhs, const DisplayModeIter& rhs) { return !(lhs == rhs); }
		inline bool operator <  (const D3DDISPLAYMODE& lhs, const D3DDISPLAYMODE& rhs)
		{
			if( lhs.Format != rhs.Format ) return lhs.Format < rhs.Format;
			if( lhs.Width  != rhs.Width  ) return lhs.Width  < rhs.Width;
			if( lhs.Height != rhs.Height ) return lhs.Height < rhs.Height;
			return lhs.RefreshRate < rhs.RefreshRate;
		}
		
		// Adapters on the current system
		class Adapter
		{
		public:
			Adapter(D3DPtr<IDirect3D9> d3d, uint adapter_index);

			uint			GetOrdinal() const					{ return m_adapter_index; }
			void			SetDeviceType(D3DDEVTYPE dev_type)	{ m_device_type = dev_type; }
			void			SetFormat(D3DFORMAT format)			{ m_format = format; m_max_modes = m_d3d->GetAdapterModeCount(m_adapter_index, m_format); }
			void			SetWindowed(bool windowed)			{ m_windowed = windowed; }
			void			SetSoftwareVertexProcessing(bool on){ m_software_vp = on; }
			D3DDISPLAYMODE	GetCurrentDisplayMode() const		{ D3DDISPLAYMODE mode; m_d3d->GetAdapterDisplayMode(m_adapter_index, &mode); return mode; }
			
			DisplayModeIter ModeBegin() const					{ return DisplayModeIter(this,           0, m_max_modes); }
			DisplayModeIter ModeEnd  () const					{ return DisplayModeIter(this, m_max_modes, m_max_modes); }

			DeviceConfig	GetDeviceConfig(const D3DDISPLAYMODE& display_mode) const;

		private:
			friend struct pr::rdr::DisplayModeIter;
			D3DPtr<IDirect3D9>		m_d3d;					// The d3d interface
			uint					m_adapter_index;		// The ordinal for this adapter
			D3DDEVTYPE				m_device_type;			// Device type, normally D3DDEVTYPE_HAL
			D3DFORMAT				m_format;				// The format of the display and back buffer the client wants
			bool					m_windowed;				// True if the client wants a windowed mode device
			bool					m_software_vp;			// True if the client wants software vertex processing
			uint					m_max_modes;			// The maximum number of display modes for the current 'm_format'
			D3DADAPTER_IDENTIFIER9	m_identifier;			// Info about the driver for this adapter
		};

		// An object representing the current system
		class System
		{
		public:
			System();
			uint	GetAdapterCount() const		{ return m_d3d->GetAdapterCount(); }
			Adapter GetAdapter(uint i) const	{ PR_ASSERT(PR_DBG_RDR, i < GetAdapterCount()); return Adapter(m_d3d, i); }

		private:			
			D3DPtr<IDirect3D9>		m_d3d;				// The d3d interface
		};
	}//namespace rdr
}//namespace pr

#endif//RDR_CONFIGURE_H
