//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/textures/text.h"
#include "pr/renderer11/textures/text_manager.h"
#include "pr/renderer11/textures/image.h"

namespace pr
{
	namespace rdr
	{
		// To<D2D_POINT_2F>
		template <typename TFrom> struct Convert<D2D_POINT_2F,TFrom>
		{
			static D2D_POINT_2F To(pr::v2 const& pos) { D2D_POINT_2F pt = {pos.x, pos.y}; return pt; }
		};

		// To<D2D_RECT_F>
		template <typename TFrom> struct Convert<D2D_RECT_F,TFrom>
		{
			static D2D_RECT_F To(pr::IRect const& r) { D2D_RECT_F rt = {float(r.m_min.x), float(r.m_min.y), float(r.m_max.x), float(r.m_max.y)}; return rt; }
			static D2D_RECT_F To(pr::FRect const& r) { D2D_RECT_F rt = {r.m_min.x, r.m_min.y, r.m_max.x, r.m_max.y}; return rt; }
		};

		Image Img(TextLayoutPtr const& layout)
		{
			DWRITE_TEXT_METRICS metrics;
			pr::Throw(layout->GetMetrics(&metrics));
			auto sx = static_cast<size_t>(::ceil(metrics.layoutWidth ));
			auto sy = static_cast<size_t>(::ceil(metrics.layoutHeight));
			return Image::make(sx, sy, nullptr, DXGI_FORMAT_B8G8R8A8_UNORM);
		}
		TextureDesc TDesc(TextLayoutPtr const& layout)
		{
			DWRITE_TEXT_METRICS metrics;
			pr::Throw(layout->GetMetrics(&metrics));
			auto sx = static_cast<size_t>(::ceil(metrics.layoutWidth ));
			auto sy = static_cast<size_t>(::ceil(metrics.layoutHeight));
			TextureDesc desc(sx, sy, 0, DXGI_FORMAT_B8G8R8A8_UNORM);
			desc.MipLevels = 1;
			desc.ArraySize = 1;
			desc.SampleDesc.Count = 1;
			desc.SampleDesc.Quality = 0;
			desc.Usage     = D3D11_USAGE_DEFAULT;
			desc.BindFlags = D3D11_BIND_SHADER_RESOURCE | D3D11_BIND_RENDER_TARGET;
			desc.MiscFlags = D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX;
			return desc;
		}

		Text::Text(TextManager* mgr, std::wstring text, TextLayoutPtr layout, pr::Colour const& colour)
			:Texture2D(&mgr->m_tex_mgr, Img(layout), TDesc(layout), SamplerDesc(), 0)
			,m_text(text)
			,m_layout(layout)
			,m_colour(colour)
			,m_mgr(mgr)
			,m_brush()
			,m_options()
			,m_rt()
		{
			// Get the keyed mutex for dx11
			pr::Throw(m_tex->QueryInterface(__uuidof(IDXGIKeyedMutex), (void**)&m_keyed_mutex11.m_ptr));

			// Get the shared handle needed to open the shared texture in D3D10.1
			HANDLE sharedHandle11;
			{
				// Get DXGI surface from texture
				D3DPtr<IDXGISurface1> surf;
				pr::Throw(m_tex->QueryInterface(__uuidof(IDXGISurface1), (void**)&surf.m_ptr));

				// Get the resource from the surface
				D3DPtr<IDXGIResource> sharedResource11;
				pr::Throw(surf->QueryInterface(__uuidof(IDXGIResource), (void**)&sharedResource11.m_ptr));

				// Get the handle from the resource
				pr::Throw(sharedResource11->GetSharedHandle(&sharedHandle11));
			}

			/// Open the surface for the shared texture in D3D10.1
			{
				D3DPtr<IDXGISurface1> sharedSurface10;
				pr::Throw(mgr->m_device10_1->OpenSharedResource(sharedHandle11, __uuidof(IDXGISurface1), (void**)(&sharedSurface10.m_ptr)));

				// Get the keyed mutex for dx10
				pr::Throw(sharedSurface10->QueryInterface(__uuidof(IDXGIKeyedMutex), (void**)&m_keyed_mutex10.m_ptr));

				// Create a d2d1 render target from the shared resource
				D2D1_RENDER_TARGET_PROPERTIES props = {};
				props.type = D2D1_RENDER_TARGET_TYPE_HARDWARE;
				props.pixelFormat = D2D1::PixelFormat(DXGI_FORMAT_UNKNOWN, D2D1_ALPHA_MODE_PREMULTIPLIED);
				pr::Throw(mgr->m_d2dfactory->CreateDxgiSurfaceRenderTarget(sharedSurface10.m_ptr, &props, &m_rt.m_ptr));
			}

			// Create a solid color brush to draw something with
			pr::Throw(m_rt->CreateSolidColorBrush(pr::To<D2D1_COLOR_F>(colour), &m_brush.m_ptr));

			RenderText();
		}

		// Render a string into the texture
		void Text::RenderText(std::wstring text, pr::Colour const& colour)
		{
			enum SyncKey { dx11, dx10, };
			m_text = text;
			m_colour = colour;

			m_keyed_mutex11->ReleaseSync(SyncKey::dx11);               // Release the D3D 11 Device
			pr::Throw(m_keyed_mutex10->AcquireSync(SyncKey::dx10, 5)); // Use D3D10.1 device

			m_brush->SetColor(pr::To<D2D1_COLOR_F>(m_colour)); // Set the brush color D2D will use to draw with

			m_rt->BeginDraw();
			m_rt->Clear(D2D1::ColorF(1.0f, 1.0f, 1.0f, 1.0f)); // Clear D2D Background

			//m_rt->FillRectangle(To<D2D1_RECT_F>(pr::IRectUnit), m_brush.m_ptr);
			//m_rt->DrawTextLayout(To<D2D_POINT_2F>(m_t2s.pos.xy()), m_layout.m_ptr, m_brush.m_ptr, m_options); // Draw the Text
			pr::Throw(m_rt->EndDraw());

			m_keyed_mutex10->ReleaseSync(SyncKey::dx10);               // Release the D3D10.1 Device
			pr::Throw(m_keyed_mutex11->AcquireSync(SyncKey::dx11, 5)); // Use the D3D11 Device
		}

		// Refcounting cleanup function
		void Text::Delete()
		{
			m_mgr->Delete(this);
		}
	}
}
