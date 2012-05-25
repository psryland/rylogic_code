//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_MATERIAL_EFFECTS_FRAGMENTS_H
#define PR_RDR_MATERIAL_EFFECTS_FRAGMENTS_H

#include "pr/renderer/types/forward.h"
#include "pr/renderer/configuration/iallocator.h"
#include "pr/renderer/materials/textures/texturefilter.h"

namespace pr
{
	namespace rdr
	{
		namespace effect
		{
			// A buffer for generating the shader text in
			typedef pr::string<char, 16384, false> ShaderBuffer;

			namespace frag
			{
				// A buffer for accumulating shader fragments
				// Note: static size of 256 bytes. If more memory is required then the
				// effect buffer should be constructed with an IAllocator pointer.
				typedef pr::Array<pr::uint8, 256, false> Buffer;

				// Function pointer for setting the parameters for each shader fragment
				typedef void (*SetParametersFunc)(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const& viewport, DrawListElement const& dle);

				// Fragment type ids
				namespace EFrag { enum Type { Txfm, Tinting, PVC, Texture2D, EnvMap, Lighting, SMap, Terminator, NumberOf, }; }
	
				// Fragment header - all effect fragments must begin with one of these
				struct Header
				{
					pr::uint          m_size;
					EFrag::Type       m_type;
					SetParametersFunc SetParameters;
					
					template <typename Frag> static Header make() { Header hdr = {sizeof(Frag), static_cast<EFrag::Type>(Frag::Type), Frag::SetParameters}; return hdr; }
					void SetHandles(D3DPtr<ID3DXEffect> effect);
					void AddTo(Desc& desc) const;
					void Variables(ShaderBuffer& data) const;
					void Functions(ShaderBuffer& data) const;
					void VSfragment(ShaderBuffer& data, int vs_idx) const;
					void PSfragment(ShaderBuffer& data, int ps_idx) const;
				};
	
				// object to world transforms
				struct Txfm
				{
					enum { Type = EFrag::Txfm };
					Header     m_header;
					D3DXHANDLE m_object_to_world;
					D3DXHANDLE m_norm_to_world;
					D3DXHANDLE m_object_to_screen;
					D3DXHANDLE m_world_to_camera;
					D3DXHANDLE m_camera_to_world;
					D3DXHANDLE m_camera_to_screen;

					Txfm();
					void SetHandles(D3DPtr<ID3DXEffect> effect);
					void AddTo(Desc& desc) const;
					static void Variables(void const* fragment, ShaderBuffer& data);
					static void Functions(void const* fragment, ShaderBuffer& data);
					static void VSfragment(void const* fragment, ShaderBuffer& data, int vs_idx);
					static void PSfragment(void const* fragment, ShaderBuffer& data, int ps_idx);
					static void SetParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const& viewport, DrawListElement const& dle);
				};
	
				// object colour tinting
				struct Tinting
				{
					enum { Type = EFrag::Tinting };
					enum EStyle { EStyle_Tint, EStyle_Tint_x_Diff };

					Header m_header;
					int m_tint_index;
					EStyle m_style;
					D3DXHANDLE m_tint_colour;

					Tinting(int tint_index, EStyle style);
					void SetHandles(D3DPtr<ID3DXEffect> effect);
					void AddTo(Desc& desc) const;
					static void Variables(void const* fragment, ShaderBuffer& data);
					static void VSfragment(void const* fragment, ShaderBuffer& data, int vs_idx);
					static void PSfragment(void const* fragment, ShaderBuffer& data, int ps_idx);
					static void SetParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const& viewport, DrawListElement const& dle);
				};
	
				// per vertex colouring
				struct PVC
				{
					enum { Type = EFrag::PVC };
					enum EStyle { EStyle_PVC, EStyle_PVC_x_Diff };

					Header m_header;
					EStyle m_style;

					PVC(EStyle style);
					void AddTo(Desc& desc) const;
					static void VSfragment(void const* fragment, ShaderBuffer& data, int vs_idx);
					static void PSfragment(void const* fragment, ShaderBuffer& data, int ps_idx);
					static void SetParameters(void const*, D3DPtr<ID3DXEffect>, Viewport const&, DrawListElement const&) {}
				};
	
				// object texturing
				struct Texture2D
				{
					enum { Type = EFrag::Texture2D };
					enum EStyle { EStyle_Tex, EStyle_Tex_x_Diff };
					
					Header                   m_header;
					int                      m_tex_index;
					EStyle                   m_style;
					D3DXHANDLE               m_texture;
					D3DXHANDLE               m_tex_to_surf;
					D3DXHANDLE               m_mip_filter;
					D3DXHANDLE               m_min_filter;
					D3DXHANDLE               m_mag_filter;
					D3DXHANDLE               m_addrU;
					D3DXHANDLE               m_addrV;
					
					Texture2D(int tex_index, EStyle style);
					void SetHandles(D3DPtr<ID3DXEffect> effect);
					void AddTo(Desc& desc) const;
					static void Variables(void const* fragment, ShaderBuffer& data);
					static void VSfragment(void const* fragment, ShaderBuffer& data, int vs_idx);
					static void PSfragment(void const* fragment, ShaderBuffer& data, int ps_idx);
					static void SetParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const& viewport, DrawListElement const& dle);
				};
	
				// environment map
				struct EnvMap
				{
					enum { Type = EFrag::EnvMap };
					
					Header     m_header;
					D3DXHANDLE m_texture;

					EnvMap();
					void SetHandles(D3DPtr<ID3DXEffect> effect);
					void AddTo(Desc& desc) const;
					static void Variables(void const* fragment, ShaderBuffer& data);
					static void Functions(void const* fragment, ShaderBuffer& data);
					static void PSfragment(void const* fragment, ShaderBuffer& data, int ps_idx);
					static void SetParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const& viewport, DrawListElement const& dle);
				};
	
				// object lighting
				struct Lighting
				{
					enum { Type = EFrag::Lighting };
					
					Header       m_header;
					int          m_light_count;        // The maximum number of lights supported
					int          m_caster_count;       // The maximum number of lights that cast shadows
					bool         m_specular;
					D3DXHANDLE   m_light_type;
					D3DXHANDLE   m_ws_light_position;
					D3DXHANDLE   m_ws_light_direction;
					D3DXHANDLE   m_light_ambient;
					D3DXHANDLE   m_light_diffuse;
					D3DXHANDLE   m_light_specular;
					D3DXHANDLE   m_specular_power;
					D3DXHANDLE   m_spot_inner_cosangle;
					D3DXHANDLE   m_spot_outer_cosangle;
					D3DXHANDLE   m_spot_range;
					D3DXHANDLE   m_world_to_smap;
					D3DXHANDLE   m_cast_shadows;
					D3DXHANDLE   m_smap_frust;
					D3DXHANDLE   m_smap_frust_dim;
					D3DXHANDLE   m_smap[MaxShadowCasters];

					Lighting(int light_count, int caster_count, bool specular);
					void SetHandles(D3DPtr<ID3DXEffect> effect);
					void AddTo(Desc& desc) const;
					static void Variables(void const* fragment, ShaderBuffer& data);
					static void Functions(void const* fragment, ShaderBuffer& data);
					static void PSfragment(void const* fragment, ShaderBuffer& data, int px_idx);
					static void SetParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, Viewport const& viewport, DrawListElement const& dle);
				};
	
				// shadow map
				struct SMap
				{
					enum { Type = EFrag::SMap };
					enum { TexSize = 1024 };

					Header     m_header;
					D3DXHANDLE m_object_to_world;
					D3DXHANDLE m_world_to_smap;
					D3DXHANDLE m_ws_smap_plane;
					D3DXHANDLE m_smap_frust_dim;
					D3DXHANDLE m_light_type;
					D3DXHANDLE m_ws_light_position;
					D3DXHANDLE m_ws_light_direction;
					
					SMap();
					void SetHandles(D3DPtr<ID3DXEffect> effect);
					void AddTo(Desc& desc) const;
					static void Variables(void const* fragment, ShaderBuffer& data);
					static void Functions(void const* fragment, ShaderBuffer& data);
					static void VSfragment(void const* fragment, ShaderBuffer& data, int vs_idx);
					static void PSfragment(void const* fragment, ShaderBuffer& data, int ps_idx);
					static void SetParameters(void const*, D3DPtr<ID3DXEffect>, Viewport const&, DrawListElement const&) {}
					static bool CreateProjection(int face, pr::Frustum const& frust, pr::m4x4 const& c2w, Light const& light, pr::m4x4& w2s);
					static bool SetSceneParameters(void const* fragment, D3DPtr<ID3DXEffect> effect, int pass, pr::Frustum const& frust, m4x4 const& c2w, Light const& light);
					static void SetObjectToWorld(void const* fragment, D3DPtr<ID3DXEffect> effect, pr::m4x4 const& o2w);
				};

				// The terminating effect fragment
				struct Terminator
				{
					enum { Type = EFrag::Terminator };

					Header m_header;

					Terminator() :m_header(Header::make<Terminator>()) {}
					static void SetParameters(void const*, D3DPtr<ID3DXEffect>, Viewport const&, DrawListElement const&) {}
				};
	
				// Helper casting functions
				inline pr::uint8 const*                     byte_cast(void const*   ptr) { return static_cast<pr::uint8 const*>(ptr); }
				inline pr::uint8*                           byte_cast(void*         ptr) { return static_cast<pr::uint8*>      (ptr); }
				inline Header const*                        hdr_cast (void const*   ptr) { return static_cast<Header const*>   (ptr); }
				inline Header*                              hdr_cast (void*         ptr) { return static_cast<Header *>        (ptr); }
				template <typename Frag> inline Frag const* frag_cast(void const*   ptr) { return static_cast<Frag const*>     (ptr); }
				template <typename Frag> inline Frag*       frag_cast(void*         ptr) { return static_cast<Frag*>           (ptr); }
	
				// Iteration access to the fragments
				inline Header const* Begin(void const* fragment_list)              { return hdr_cast(fragment_list)->m_type != EFrag::Terminator ? hdr_cast(fragment_list) : 0; }
				inline Header*       Begin(void*       fragment_list)              { return hdr_cast(fragment_list)->m_type != EFrag::Terminator ? hdr_cast(fragment_list) : 0; }
				inline Header const* Inc(Header const* fragment)                   { return Begin(byte_cast(fragment) + fragment->m_size); }
				inline Header*       Inc(Header*       fragment)                   { return Begin(byte_cast(fragment) + fragment->m_size); }
				inline Header const* IncUnique(Header const* frag, pr::uint& seen) { seen = pr::SetBits(seen, 1<<frag->m_type, true); Header const* hdr; for (hdr=Inc(frag); hdr && AllSet(seen, 1<<hdr->m_type); hdr=Inc(hdr)){} return hdr; }
				inline Header*       IncUnique(Header*       frag, pr::uint& seen) { seen = pr::SetBits(seen, 1<<frag->m_type, true); Header*       hdr; for (hdr=Inc(frag); hdr && AllSet(seen, 1<<hdr->m_type); hdr=Inc(hdr)){} return hdr; }
	
				// Return a pointer to a particular fragment type
				template <typename Frag> inline Frag const* Find(Header const* frag) { Header const* f; for (f=Begin(frag); f && f->m_type!=Frag::Type; f=Inc(f)){} return frag_cast<Frag>(f); }
				template <typename Frag> inline Frag*       Find(Header*       frag) { Header*       f; for (f=Begin(frag); f && f->m_type!=Frag::Type; f=Inc(f)){} return frag_cast<Frag>(f); }
			}

			// Shader parameter semantics
			namespace ESemantic
			{
				enum Type { Position, Color0, Color1, Color2, Color3, Depth, TexCoord0, TexCoord1, TexCoord2, TexCoord3, TexCoord4, NumberOf };
				char const* ToString(Type semantic);
			}

			// Object containing a description of the effect
			struct Desc
			{
				struct Member
				{
					ESemantic::Type m_channel;
					string32 m_type, m_name, m_init, m_chnl;
					bool valid() const                              { return !m_name.empty(); }
					bool operator ==(ESemantic::Type channel) const { return m_channel == channel; }
					bool operator < (Member const& rhs) const       { return m_channel < rhs.m_channel; }
					char const* decl() const                        { return pr::FmtS("	%-8s %-8s :%s;\n" ,m_type.c_str() ,m_name.c_str() ,m_chnl.c_str()); }
					char const* init() const                        { return pr::FmtS("	Out.%-8s = %s;\n" ,m_name.c_str() ,m_init.c_str()); }
				};
				typedef pr::Array<Member, 4, false> MemberCont;
				struct Struct
				{
					MemberCont m_member;

					void Add(ESemantic::Type channel, char const* type, char const* name, char const* init);
					void decl(ShaderBuffer& data) const;
					void init(ShaderBuffer& data) const;
				};
				typedef pr::Array<Struct, 3, false> StructCont;
				struct Shader
				{
					int               m_in_idx;          // Shader input structure index
					int               m_out_idx;         // Shader output structure index
					int               m_version;         // The version of the shader
					string256         m_sig;             // Shader function signiture
				};
				typedef pr::Array<Shader, 1, false> ShaderCont;
				struct Pass
				{
					int               m_vs_idx;          // Index of the vertex shader to use in this pass
					int               m_ps_idx;          // Index of the pixel shader to use in this pass
					string256         m_vs_params;       // Values to pass to the vertex shader function
					string256         m_ps_params;       // Values to pass to the pixel shader function
					string256         m_rdr_states;      // Render states set within the pass
					Pass    (int vs_idx = 0, int ps_idx = 0) { set(vs_idx, ps_idx); }
					void set(int vs_idx = 0, int ps_idx = 0) { m_vs_idx = vs_idx; m_ps_idx = ps_idx; }
				};
				typedef pr::Array<Pass, 2, false> PassCont;
				struct Technique
				{
					PassCont          m_pass;
				};
				typedef pr::Array<Technique, 1, false> TechCont;

				int                   m_vs_version;      // The maximum vertex shader version supported by this hardware
				int                   m_ps_version;      // The maximum pixel shader version supported by this hardware
				RdrId                 m_effect_id;       // Unique identifier for this effect
				frag::Buffer          m_buf;             // Buffer to collect shader fragments
				
				StructCont            m_vsout;           // VS output structures
				StructCont            m_psout;           // PS output structures
				ShaderCont            m_vs;              // Vertex shader functions
				ShaderCont            m_ps;              // Pixel shader functions
				TechCont              m_tech;            // Techniques

				// Constructor
				// It is valid to pass 0 for the allocator, in which case you will
				// get an assert if you exceed the static sizes of the containers
				Desc(D3DPtr<IDirect3DDevice9> d3d_device = 0);

				// Reset the Desc
				void Reset(int tech_count = 1, int vs_count = 1, int ps_count = 1, int vsout_count = 1, int psout_count = 1);

				// Add an effect fragment to the effect description
				template <typename Frag> void Add(Frag const& frag) { return Add(reinterpret_cast<frag::Header const&>(frag)); }
				void Add(frag::Header const& frag);

				// Compile the effect into a block of text
				void GenerateText(ShaderBuffer& data) const;
			};
		}
	}
}

#endif
