#include "SpaceTrucker/src/forward.h"
#include "SpaceTrucker/src/settings.h"
#include "SpaceTrucker/src/dx_state.h"
#include "SpaceTrucker/src/settings_ui.h"
#include "SpaceTrucker/src/trade_db.h"

using namespace std::placeholders;

namespace st
{
	// ToDo
	// -Scan the netLog file for the current system/body player location
	// -Create a worker thread that looks at your current location and reports the best trade routes.
	// -Capture the dx front buffer and clip out the area containing trade data, and also the station name
	// -OCR the trade data and station name
	//   -do this last, use manually added data to start with to test everything else
	// -Check the station name matches the current system/body, or update if not known yet
	// -Toggle on/off with global key shortcut
	// -Option for auto show trades vs. manual searches
	// -Allow full sql searches
	// -Separate the capturing of trade data and database updating/searching so that data could be updated
	//  from other sources, or could be uploaded to an online db

	// behaviour:
	//  run this program on the other monitor
	//  first time - at the trade screen "configure" the app
	//    - screen grab, draw the capture areas on the image to show where to capture (maybe this can be hard coded)
	//  arrive at a station and open the trade view
	//  hit key shortcut to enable capturing
	//  (could scan the station name area and enable when a sensible result is found, disable when not)
	//    - app captures screens (every 1s or whatever)
	//    - OCR's the text and updates the database
	//    - best trades, or manual searching used
	//  

	struct Main :Form<Main>
	{
		Settings m_settings;
		Button   m_btn_capture;
		DxState  m_dx;
		TradeDB m_db;

		enum { IDC_BTN_CAPTURE = 1000 };
		Main()
			:Form<Main>(_T("Space Trucker"), ApplicationMainWindow, CW_USEDEFAULT, CW_USEDEFAULT, 300, 300, DefaultFormStyle, DefaultFormStyleEx, IDM_MENU)
			,m_settings()
			,m_dx(m_settings)
			,m_btn_capture(_T("Capture Screen"), 3, 3, 100, 20, IDC_BTN_CAPTURE, m_hwnd, this)
			,m_db("ed.db")
		{
			m_db.UseDummyData();
			m_btn_capture.Click += std::bind(&Main::CaptureScreen, this, _1, _2);
		}

		// Main menu handler
		bool HandleMenu(WORD menu_id) override
		{
			switch (menu_id)
			{
			default:
				return false;
			case ID_FILE_OPTIONS:
				ShowOptions();
				return true;
			case ID_FILE_EXIT:
				Close();
				return true;
			}
		}

		// Capture the front buffer
		void CaptureScreen(Button&,EmptyArgs const&)
		{
			//ID3D11Texture2D* pSurface;
			//HRESULT hr = m_swapChain->GetBuffer( 0, __uuidof( ID3D11Texture2D ), reinterpret_cast< void** >( &pSurface ) );
			//if( pSurface )
			//{
			//	const int width = static_cast<int>(m_window->Bounds.Width * m_dpi / 96.0f);
			//	const int height = static_cast<int>(m_window->Bounds.Height * m_dpi / 96.0f);
			//	unsigned int size = width * height;
			//	if( m_captureData )
			//	{
			//		freeFramebufferData( m_captureData );
			//	}
			//	m_captureData = new unsigned char[ width * height * 4 ];

			//	ID3D11Texture2D* pNewTexture = NULL;

			//	D3D11_TEXTURE2D_DESC description;
			//	pSurface->GetDesc( &description );
			//	description.BindFlags = 0;
			//	description.CPUAccessFlags = D3D11_CPU_ACCESS_READ | D3D11_CPU_ACCESS_WRITE;
			//	description.Usage = D3D11_USAGE_STAGING;

			//	HRESULT hr = m_d3dDevice->CreateTexture2D( &description, NULL, &pNewTexture );
			//	if( pNewTexture )
			//	{
			//		m_d3dContext->CopyResource( pNewTexture, pSurface );
			//		D3D11_MAPPED_SUBRESOURCE resource;
			//		unsigned int subresource = D3D11CalcSubresource( 0, 0, 0 );
			//		HRESULT hr = m_d3dContext->Map( pNewTexture, subresource, D3D11_MAP_READ_WRITE, 0, &resource );
			//		//resource.pData; // TEXTURE DATA IS HERE

			//		const int pitch = width << 2;
			//		const unsigned char* source = static_cast< const unsigned char* >( resource.pData );
			//		unsigned char* dest = m_captureData;
			//		for( int i = 0; i < height; ++i )
			//		{
			//			memcpy( dest, source, width * 4 );
			//			source += pitch;
			//			dest += pitch;
			//		}

			//		m_captureSize = size;
			//		m_captureWidth = width;
			//		m_captureHeight = height;

			//		return;
			//	}

			//	freeFramebufferData( m_captureData );
			//}
		}

		// Display the options dialog
		void ShowOptions()
		{
			SettingsUI ui(m_settings);
			ui.ShowDialog(this);
		}
	};
}

// WinMain
int __stdcall _tWinMain(HINSTANCE, HINSTANCE, LPTSTR, int)
{
	pr::wingui::InitCtrls();

	st::Main main;
	pr::wingui::MessageLoop loop;
	return loop.Run();
}
