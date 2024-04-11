//***********************************************************************
// BlitzSearch
//  Copyright (c) Rylogic Ltd 2024
//***********************************************************************
#include "src/forward.h"
#include "src/settings.h"
#include "src/ui/main_ui.h"

int __stdcall wWinMain(_In_ HINSTANCE, _In_opt_ HINSTANCE, _In_ LPWSTR, _In_ int)
{
	try
	{
		// Load the settings
		blitzsearch::Settings settings;
		
		blitzsearch::MainUI main_ui;
		main_ui.Show();
		return ui::MessageLoop().Run();
	}
	catch (std::exception const& ex)
	{
		OutputDebugStringA("Died: ");
		OutputDebugStringA(ex.what());
		OutputDebugStringA("\n");
		return -1;
	}
}
