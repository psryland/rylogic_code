//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
#include "renderer11/util/stdafx.h"
#include "pr/renderer11/textures/text_manager.h"
#include "pr/renderer11/render/sortkey.h"

#pragma comment(lib, "dwrite.lib")
#pragma comment(lib, "d2d1.lib")

namespace pr
{
	namespace rdr
	{
		//// Gets a dx10.1 device from a dx11 device
		//D3DPtr<ID3D10Device1> D3d10Device1(D3DPtr<ID3D11Device>& device)
		//{
		//	// D2D is required for DirectWrite, but only supports Dx10.1 on windows 7
		//	// see: http://xboxforums.create.msdn.com/forums/t/103939.aspx
		//	auto hd3d10_1 = LoadLibraryW(L"d3d10_1.dll");

		//	// Get adapter of the current D3D11 device. Our D3D10 device will run on the same adapter.
		//	D3DPtr<IDXGIDevice> dxgi_device;
		//	D3DPtr<IDXGIAdapter> adapter;
		//	pr::Throw(device->QueryInterface<IDXGIDevice>(&dxgi_device.m_ptr));
		//	pr::Throw(dxgi_device->GetAdapter(&adapter.m_ptr));

		//	// Get address of the function D3D10CreateDevice1 dynamically.
		//	typedef HRESULT (WINAPI* FN_D3D10CreateDevice1)(IDXGIAdapter *pAdapter, D3D10_DRIVER_TYPE DriverType, HMODULE Software, UINT Flags, D3D10_FEATURE_LEVEL1 HardwareLevel, UINT SDKVersion, ID3D10Device1 **ppDevice );
		//	FN_D3D10CreateDevice1 CreateDevice10_1 = (FN_D3D10CreateDevice1)GetProcAddress(hd3d10_1, "D3D10CreateDevice1");

		//	// Call D3D10CreateDevice1 dynamically.
		//	D3DPtr<ID3D10Device1> dx10_device;
		//	pr::Throw(CreateDevice10_1(adapter.m_ptr, D3D10_DRIVER_TYPE_HARDWARE, NULL, D3D10_CREATE_DEVICE_BGRA_SUPPORT|D3D10_CREATE_DEVICE_DEBUG, D3D10_FEATURE_LEVEL_10_1, D3D10_1_SDK_VERSION, &dx10_device.m_ptr));
		//}

		TextManager::TextManager(MemFuncs& mem, D3DPtr<ID3D11Device>& device, D3DPtr<ID3D10Device1>& device10_1, TextureManager& tex_mgr)
			:m_alex_text(Allocator<Text>(mem))
			,m_device(device)
			,m_device10_1(device10_1)
			,m_tex_mgr(tex_mgr)
		{
			// Create the d2d factory
			pr::Throw(D2D1CreateFactory(D2D1_FACTORY_TYPE_SINGLE_THREADED, &m_d2dfactory.m_ptr));

			// Create the dwrite factory
			pr::Throw(DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, __uuidof(IDWriteFactory), reinterpret_cast<IUnknown**>(&m_dwfactory.m_ptr)));

			// Create the font table
			for (EFont font : EFont::Members())
			{
				FontPtr format;
				pr::Throw(m_dwfactory->CreateTextFormat(
					font.ToWString(),           // Font family name.
					nullptr,                    // Font collection (NULL sets it to use the system font collection).
					DWRITE_FONT_WEIGHT_REGULAR, //
					DWRITE_FONT_STYLE_NORMAL,   //
					DWRITE_FONT_STRETCH_NORMAL, //
					10.0f,                      //
					L"en-us",
					&format.m_ptr));
				pr::Throw(format->SetTextAlignment(DWRITE_TEXT_ALIGNMENT_CENTER));
				pr::Throw(format->SetParagraphAlignment(DWRITE_PARAGRAPH_ALIGNMENT_CENTER));
				m_fonts.push_back(format);
			}
		}

		// Create a layout object for some text
		// 'sx','sy' are the size of the bounding rectangle in which to layout
		TextLayoutPtr TextManager::CreateLayout(std::wstring text, EFont font, size_t sx, size_t sy)
		{
			// Create the layout
			TextLayoutPtr layout;
			pr::Throw(m_dwfactory->CreateTextLayout(text.c_str(), text.size(), m_fonts[font].m_ptr, static_cast<float>(sx), static_cast<float>(sy), &layout.m_ptr));
			return layout;
		}

		// Create a new text instance
		// sx,sy are the max size of the text texture
		TextPtr TextManager::CreateText(std::wstring text, TextLayoutPtr layout)
		{
			TextPtr inst = m_alex_text.New(this, text, layout);
			inst->m_id = MakeId(inst.m_ptr);
			return inst;
		}

		// Create text texture instance with default layout and size 'sx','sy'
		TextPtr TextManager::CreateText(std::wstring text, EFont font, size_t sx, size_t sy)
		{
			auto layout = CreateLayout(text, font, sx, sy);
			return CreateText(text, layout);
		}

		// Delete a text instance.
		void TextManager::Delete(Text* text)
		{
			if (text == nullptr) return;
			m_alex_text.Delete(text);
		}
	}
}
