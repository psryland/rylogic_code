//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#include "renderer/utility/stdafx.h"
#include "pr/renderer/models/types.h"
#include "pr/renderer/packages/package.h"
#include "pr/renderer/renderer/renderer.h"
#include "pr/renderer/utility/globalfunctions.h"

using namespace pr;
using namespace pr::rdr;

// Check that required dlls to run the renderer are available
// Throws RdrException if not
void pr::rdr::CheckDependencies()
{
	struct ErrMode
	{
		DWORD m_old_mode;
		ErrMode() :m_old_mode(::SetErrorMode(SEM_FAILCRITICALERRORS)) {}
		~ErrMode() { ::SetErrorMode(m_old_mode); }
	} auto_error_mode;

	HMODULE handle;

	handle = ::LoadLibraryA("d3dcompiler_42.dll");
	if (handle == 0) throw RdrException(pr::rdr::EResult::DependencyMissing, "Dependent dll 'd3dcompiler_42.dll' could not be loaded. Please download and install the DirectX 9.0 End User Runtime from the Microsoft website.");
	else FreeLibrary(handle);
	
	handle = ::LoadLibraryA("d3dx9_42.dll");
	if (handle == 0) throw RdrException(pr::rdr::EResult::DependencyMissing, "Dependent dll 'd3dx9_42.dll' could not be loaded. Please download and install the DirectX 9.0 End User Runtime from the Microsoft website.");
	else FreeLibrary(handle);
}
	
// Return the monitor associated with the device
HMONITOR pr::rdr::GetMonitor(D3DPtr<IDirect3DDevice9> const& d3d_device)
{
	D3DPtr<IDirect3D9> d3d;
	D3DDEVICE_CREATION_PARAMETERS params;
	Throw(d3d_device->GetCreationParameters(&params));
	Throw(d3d_device->GetDirect3D(&d3d.m_ptr));
	return d3d->GetAdapterMonitor(params.AdapterOrdinal);
}
			
// Get the Vrange from an Irange in an index buffer
model::Range pr::rdr::GetVRange(model::Range const& i_range, rdr::Index const* ibuffer)
{
	model::Range v_range = {0, 0};

	// Determine the v_range 
	rdr::Index const* ib     = ibuffer + i_range.m_begin;
	rdr::Index const* ib_end = ib      + i_range.size();
	if (ib != ib_end)
	{
		std::size_t first_v = *ib;
		std::size_t last_v  = *ib;
		for (; ib != ib_end; ++ib)
		{
			if (*ib < first_v) first_v = *ib;
			if (*ib > last_v ) last_v  = *ib;
		}
		v_range.set(first_v, last_v);
	}
	return v_range;
}
	
// Return the number of bits per pixel for a given format
uint pr::rdr::BytesPerPixel(D3DFORMAT format)
{
#	pragma region BBPTable
	struct BBPTable
	{
		pr::uint8 m_bpp[120];
		BBPTable() :m_bpp()
		{
			m_bpp[D3DFMT_R8G8B8        ] = 3;
			m_bpp[D3DFMT_A8R8G8B8      ] = 4;
			m_bpp[D3DFMT_X8R8G8B8      ] = 4;
			m_bpp[D3DFMT_R5G6B5        ] = 2;
			m_bpp[D3DFMT_X1R5G5B5      ] = 2;
			m_bpp[D3DFMT_A1R5G5B5      ] = 2;
			m_bpp[D3DFMT_A4R4G4B4      ] = 2;
			m_bpp[D3DFMT_R3G3B2        ] = 1;
			m_bpp[D3DFMT_A8            ] = 1;
			m_bpp[D3DFMT_A8R3G3B2      ] = 2;
			m_bpp[D3DFMT_X4R4G4B4      ] = 2;
			m_bpp[D3DFMT_A2B10G10R10   ] = 4;
			m_bpp[D3DFMT_A8B8G8R8      ] = 4;
			m_bpp[D3DFMT_X8B8G8R8      ] = 4;
			m_bpp[D3DFMT_G16R16        ] = 4;
			m_bpp[D3DFMT_A2R10G10B10   ] = 4;
			m_bpp[D3DFMT_A16B16G16R16  ] = 8;
			m_bpp[D3DFMT_A8P8          ] = 2;
			m_bpp[D3DFMT_P8            ] = 1;
			m_bpp[D3DFMT_L8            ] = 1;
			m_bpp[D3DFMT_A8L8          ] = 2;
			m_bpp[D3DFMT_A4L4          ] = 1;
			m_bpp[D3DFMT_V8U8          ] = 2;
			m_bpp[D3DFMT_L6V5U5        ] = 2;
			m_bpp[D3DFMT_X8L8V8U8      ] = 4;
			m_bpp[D3DFMT_Q8W8V8U8      ] = 4;
			m_bpp[D3DFMT_V16U16        ] = 4;
			m_bpp[D3DFMT_A2W10V10U10   ] = 4;
			m_bpp[D3DFMT_D16_LOCKABLE  ] = 2;
			m_bpp[D3DFMT_D32           ] = 4;
			m_bpp[D3DFMT_D15S1         ] = 2;
			m_bpp[D3DFMT_D24S8         ] = 4;
			m_bpp[D3DFMT_D24X8         ] = 4;
			m_bpp[D3DFMT_D24X4S4       ] = 4;
			m_bpp[D3DFMT_D16           ] = 2;
			m_bpp[D3DFMT_D32F_LOCKABLE ] = 4;
			m_bpp[D3DFMT_D24FS8        ] = 4;
			m_bpp[D3DFMT_L16           ] = 2;
			m_bpp[D3DFMT_INDEX16       ] = 2;
			m_bpp[D3DFMT_INDEX32       ] = 4;
			m_bpp[D3DFMT_Q16W16V16U16  ] = 8;
			m_bpp[D3DFMT_R16F          ] = 2;
			m_bpp[D3DFMT_G16R16F       ] = 4;
			m_bpp[D3DFMT_A16B16G16R16F ] = 8;
			m_bpp[D3DFMT_R32F          ] = 4;
			m_bpp[D3DFMT_G32R32F       ] = 8;
			m_bpp[D3DFMT_A32B32G32R32F ] = 16;
			m_bpp[D3DFMT_CxV8U8        ] = 2;
		}
		pr::uint operator[](int format) const
		{
			PR_ASSERT(PR_DBG_RDR, format < sizeof(m_bpp), "Unknown d3dformat");
			PR_ASSERT(PR_DBG_RDR, m_bpp[format] > 0, "Unknown d3dformat");
			return m_bpp[format];
		}
	};
#	pragma endregion
	static BBPTable bpp;
	return bpp[format];
}
	
// Return a multisampling level based on a quality and the capabilities of the hardware
D3DMULTISAMPLE_TYPE pr::rdr::GetAntiAliasingLevel(D3DPtr<IDirect3D9> d3d, const DeviceConfig& config, D3DFORMAT format, EQuality::Type quality)
{
	switch (quality)
	{
	case EQuality::High:   if (Succeeded(d3d->CheckDeviceMultiSampleType(config.m_adapter_index, config.m_device_type, format, config.m_windowed, D3DMULTISAMPLE_16_SAMPLES, 0))) return D3DMULTISAMPLE_16_SAMPLES;
	                       if (Succeeded(d3d->CheckDeviceMultiSampleType(config.m_adapter_index, config.m_device_type, format, config.m_windowed, D3DMULTISAMPLE_9_SAMPLES , 0))) return D3DMULTISAMPLE_9_SAMPLES;
	case EQuality::Medium: if (Succeeded(d3d->CheckDeviceMultiSampleType(config.m_adapter_index, config.m_device_type, format, config.m_windowed, D3DMULTISAMPLE_4_SAMPLES , 0))) return D3DMULTISAMPLE_4_SAMPLES;
	                       if (Succeeded(d3d->CheckDeviceMultiSampleType(config.m_adapter_index, config.m_device_type, format, config.m_windowed, D3DMULTISAMPLE_2_SAMPLES , 0))) return D3DMULTISAMPLE_2_SAMPLES;
	case EQuality::Low:    if (Succeeded(d3d->CheckDeviceMultiSampleType(config.m_adapter_index, config.m_device_type, format, config.m_windowed, D3DMULTISAMPLE_NONE      , 0))) return D3DMULTISAMPLE_NONE;
	                       throw RdrException(EResult::NoMultiSamplingTypeSupported, "No multi sample type (including none) is supported on this graphics adapter");
	}
	return D3DMULTISAMPLE_NONE;
}
	
// Configure a texture filter to a particular quality level based on the capabilities of the hardware
void pr::rdr::SetTextureFilter(TextureFilter& filter, const D3DCAPS9& caps, EQuality::Type quality)
{
	// Set the texture filter levels
	switch (quality)
	{
	case EQuality::High:   if (caps.TextureFilterCaps & D3DPTFILTERCAPS_MAGFGAUSSIANQUAD ) { filter.m_mag = D3DTEXF_GAUSSIANQUAD; break; }
	                       if (caps.TextureFilterCaps & D3DPTFILTERCAPS_MAGFPYRAMIDALQUAD) { filter.m_mag = D3DTEXF_PYRAMIDALQUAD; break; }
	                       if (caps.TextureFilterCaps & D3DPTFILTERCAPS_MAGFANISOTROPIC  ) { filter.m_mag = D3DTEXF_ANISOTROPIC; break; }
	case EQuality::Medium: if (caps.TextureFilterCaps & D3DPTFILTERCAPS_MAGFLINEAR       ) { filter.m_mag = D3DTEXF_LINEAR; break; }
	case EQuality::Low:                                                                    { filter.m_mag = D3DTEXF_POINT; break; }
	}
	switch (quality)
	{
	case EQuality::High:
	case EQuality::Medium: if (caps.TextureFilterCaps & D3DPTFILTERCAPS_MIPFLINEAR)        { filter.m_mip = D3DTEXF_LINEAR; break; }
	case EQuality::Low:                                                                    { filter.m_mip = D3DTEXF_POINT; break; }
	}
	switch (quality)
	{
	case EQuality::High:   if (caps.TextureFilterCaps & D3DPTFILTERCAPS_MINFGAUSSIANQUAD) { filter.m_min = D3DTEXF_GAUSSIANQUAD; break; }
	                       if (caps.TextureFilterCaps & D3DPTFILTERCAPS_MINFPYRAMIDALQUAD){ filter.m_min = D3DTEXF_PYRAMIDALQUAD; break; }
	                       if (caps.TextureFilterCaps & D3DPTFILTERCAPS_MINFANISOTROPIC)  { filter.m_min = D3DTEXF_ANISOTROPIC; break; }
	case EQuality::Medium: if (caps.TextureFilterCaps & D3DPTFILTERCAPS_MINFLINEAR)       { filter.m_min = D3DTEXF_LINEAR; break; }
	case EQuality::Low:                                                                   { filter.m_min = D3DTEXF_POINT; break; }
	}
}

// Set the render states in 'rsb' suitable for alpha blending
void pr::rdr::SetAlphaRenderStates(rs::Block& rsb, bool on, uint blend_op, uint src, uint dest)
{
	if (on)
	{
		rsb.SetRenderState(D3DRS_CULLMODE         ,D3DCULL_NONE);
		rsb.SetRenderState(D3DRS_ZWRITEENABLE     ,FALSE);
		rsb.SetRenderState(D3DRS_ALPHABLENDENABLE ,TRUE);
		rsb.SetRenderState(D3DRS_BLENDOP          ,blend_op);
		rsb.SetRenderState(D3DRS_SRCBLEND         ,src);
		rsb.SetRenderState(D3DRS_DESTBLEND        ,dest);
	}
	else
	{
		rsb.ClearRenderState(D3DRS_CULLMODE);
		rsb.ClearRenderState(D3DRS_ZWRITEENABLE);
		rsb.ClearRenderState(D3DRS_ALPHABLENDENABLE);
		rsb.ClearRenderState(D3DRS_BLENDOP);
		rsb.ClearRenderState(D3DRS_SRCBLEND);
		rsb.ClearRenderState(D3DRS_DESTBLEND);
	}
}
	
// Create a material from a 'pr::Material'
pr::rdr::Material pr::rdr::LoadMaterial(Renderer& rdr, pr::Material const& material, pr::GeomType geom_type)
{
	// Select an effect appropriate for the vertex format being used.
	pr::rdr::Material mat = rdr.m_mat_mgr.GetMaterial(geom_type);
	
	// Load the textures of this material
	if (!material.m_texture.empty())
		mat.m_diffuse_texture = rdr.m_mat_mgr.CreateTexture(AutoId, material.m_texture[0].m_filename.c_str());
	
	return mat;
}
	
// Create a model from a 'pr::Mesh'
pr::rdr::ModelPtr pr::rdr::LoadMesh(Renderer& rdr, pr::Mesh const& mesh)
{
	// Create the model
	model::Settings settings;
	settings.m_vertex_type = vf::GetTypeFromGeomType(mesh.m_geom_type);
	settings.m_Vcount      = mesh.m_vertex.size();
	settings.m_Icount      = mesh.m_face.size() * 3;
	pr::rdr::ModelPtr mdl = rdr.m_mdl_mgr.CreateModel(settings);
	
	model::VLock vlock;
	model::ILock ilock;
	
	// Copy the vertices and indices into the model
	// Read the vertex buffer
	mdl->m_bbox.reset();
	vf::iterator vb = mdl->LockVBuffer(vlock);
	for (TVertCont::const_iterator v = mesh.m_vertex.begin(), v_end = mesh.m_vertex.end(); v != v_end; ++v)
	{
		Encompase(mdl->m_bbox, v->m_vertex);
		vb->set(*v); ++vb;
	}
	
	// Read the index buffer
	Index* ib = mdl->LockIBuffer(ilock);
	for (TFaceCont::const_iterator f = mesh.m_face.begin(), f_end = mesh.m_face.end(); f != f_end; ++f)
	{
		*ib = f->m_vert_index[0]; ++ib;
		*ib = f->m_vert_index[1]; ++ib;
		*ib = f->m_vert_index[2]; ++ib;
	}
	
	// Register the materials
	pr::Array<Material, 25> materials;
	for (TMaterialCont::const_iterator m = mesh.m_material.begin(), m_end = mesh.m_material.end(); m != m_end; ++m)
	{
		Material mat = LoadMaterial(rdr, *m, mesh.m_geom_type);
		materials.push_back(mat);
	}
	
	// Set the materials on the model
	TFaceCont::const_iterator fbegin = mesh.m_face.begin();
	TFaceCont::const_iterator fend   = mesh.m_face.end();
	if (fbegin != fend)
	{
		// If the model doesn't contain any materials use a default material for the whole model
		if (materials.empty())
		{
			Material mat = rdr.m_mat_mgr.GetMaterial(mesh.m_geom_type);
			mdl->SetMaterial(mat, model::EPrimitive::TriangleList, true);
		}
		
		// Otherwise, determine the ranges of faces that use each material
		else
		{
			for (TFaceCont::const_iterator face = fbegin; face != fend;)
			{
				std::size_t first_mat_index = pr::Clamp<std::size_t>(face->m_mat_index, 0, materials.size() - 1);
				size_t iidx = (face - fbegin) * 3;
				
				// Find the range of contiguous verts and indices that use 'first_mat_index'
				model::Range vrange, irange = {iidx, iidx + 3};
				Encompase(vrange, face->m_vert_index[0]);
				Encompase(vrange, face->m_vert_index[1]);
				Encompase(vrange, face->m_vert_index[2]);
				for (++face; face != fend && face->m_mat_index == first_mat_index; ++face)
				{
					irange.m_end += 3;
					Encompase(vrange, face->m_vert_index[0]);
					Encompase(vrange, face->m_vert_index[1]);
					Encompase(vrange, face->m_vert_index[2]);
				}
				
				if (!irange.empty())
					mdl->SetMaterial(materials[first_mat_index], model::EPrimitive::TriangleList, false, &vrange, &irange);
			}
		}
	}
	return mdl;
}
	
