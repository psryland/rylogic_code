//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************

#include "pr/audio/forward.h"
#include "pr/audio/audio.h"
#include "pr/audio/dll/audio.h"
#include "audio/dll/context.h"

using namespace pr::audio;

#ifdef _MANAGED
#pragma managed(push, off)
#endif
HINSTANCE g_hInstance;
BOOL APIENTRY DllMain(HINSTANCE hInstance, DWORD ul_reason_for_call, LPVOID)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH: g_hInstance = hInstance; break;
	case DLL_PROCESS_DETACH: g_hInstance = nullptr; break;
	case DLL_THREAD_ATTACH:  break;
	case DLL_THREAD_DETACH:  break;
	}
	return TRUE;
}
#ifdef _MANAGED
#pragma managed(pop)
#endif

static Context* g_ctx = nullptr;
static Context& Dll()
{
	if (g_ctx) return *g_ctx;
	throw std::exception("Audio not initialised");
}

// Default error callback
void __stdcall DefaultErrorCB(void*, wchar_t const* msg)
{
	std::wcerr << msg << std::endl;
}

// Lock and catch
#define DllLockGuard LockGuard lock(Dll().m_mutex)
#define CatchAndReport(func_name, ret)\
	catch (std::exception const& ex) { Dll().ReportError(L"" ## #func_name ## " failed. ", ex); }\
	catch (...)                      { Dll().ReportError(L"" ## #func_name ## " failed."); }\
	return ret

// Initialise the dll *****************************************************************************

// Initialise calls are reference counted and must be matched with Shutdown calls
// 'initialise_error_cb' is used to report dll initialisation errors only (i.e. it isn't stored)
// Note: this function is not thread safe, avoid race calls
AUDIO_API AudioContext __stdcall Audio_Initialise(Audio_ReportErrorCB initialise_error_cb, void* ctx)
{
	try
	{
		initialise_error_cb = initialise_error_cb ? initialise_error_cb : DefaultErrorCB;

		// Create the dll context on the first call
		if (g_ctx == nullptr)
			g_ctx = new Context();

		// Generate a unique handle per Initialise call, used to match up with Shutdown calls
		static AudioContext context = nullptr;
		g_ctx->m_inits.insert(++context);
		return context;
	}
	catch (std::exception const& e)
	{
		if (initialise_error_cb) initialise_error_cb(ctx, pr::FmtS(L"Failed to initialise Audio.\n%S\n", e.what()));
		return nullptr;
	}
	catch (...)
	{
		if (initialise_error_cb) initialise_error_cb(ctx, L"Failed to initialise Audio.\nAn unknown exception occurred\n");
		return nullptr;
	}
}
AUDIO_API void __stdcall Audio_Shutdown(AudioContext context)
{
	if (!g_ctx) return;

	g_ctx->m_inits.erase(context);
	if (!g_ctx->m_inits.empty())
		return;

	delete g_ctx;
	g_ctx = nullptr;
}

// Add/Remove a global error callback.
// Note: The callback function can be called in a worker thread context if errors occur during LoadScriptSource
AUDIO_API void __stdcall Audio_GlobalErrorCBSet(Audio_ReportErrorCB error_cb, void* ctx, BOOL add)
{
	try
	{
		if (add)
			Dll().OnError += {ctx, error_cb};
		else
			Dll().OnError -= {ctx, error_cb};
	}
	CatchAndReport(Audio_GlobalErrorCBSet,);
}

// Wave Banks *************************************************************************************

// Create a wave bank of sounds for a MIDI instrument
AUDIO_API void __stdcall Audio_WaveBankCreateMidiInstrument(char const* bank_name, wchar_t const* root_dir, wchar_t const* xwb_filepath, wchar_t const* xml_instrument_filepath)
{
	try
	{
		CreateMidiInstrumentWaveBank(bank_name, root_dir, xwb_filepath, xml_instrument_filepath);
	}
	CatchAndReport(Audio_WaveBankCreateMidiInstrument,);
}

// Misc *******************************************************************************************

// Play an audio file
AUDIO_API void __stdcall Audio_PlayFile(wchar_t const* filepath)
{
	try
	{
		Dll().m_audio.PlaySynchronous(filepath);
	}
	CatchAndReport(Audio_PlayFile,);
}

