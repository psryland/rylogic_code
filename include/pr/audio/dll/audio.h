//***************************************************************************************************
// Audio
//  Copyright (c) Rylogic Ltd 2017
//***************************************************************************************************
#pragma once

#ifdef AUDIO_EXPORTS
#define AUDIO_API __declspec(dllexport)
#else
#define AUDIO_API __declspec(dllimport)
#endif

#include <windows.h>
#include <xaudio2.h>

#ifdef AUDIO_EXPORTS
	namespace pr
	{
		namespace audio {}
	}
	using AudioContext = unsigned char*;
#else
	using AudioContext = void*;
#endif

extern "C"
{
	// Forward declarations
	using Audio_ReportErrorCB = void (__stdcall *)(void* ctx, wchar_t const* msg);

	// Initialise/shutdown the dll
	AUDIO_API AudioContext __stdcall Audio_Initialise(Audio_ReportErrorCB initialise_error_cb, void* ctx);
	AUDIO_API void         __stdcall Audio_Shutdown(AudioContext context);
	AUDIO_API void         __stdcall Audio_GlobalErrorCBSet(Audio_ReportErrorCB error_cb, void* ctx, BOOL add);

	// Wave Banks
	AUDIO_API void         __stdcall Audio_WaveBankCreateMidiInstrument(char const* bank_name, wchar_t const* root_dir, wchar_t const* xwb_filepath, wchar_t const* xml_instrument_filepath);

	// Misc
	AUDIO_API void         __stdcall Audio_PlayFile(wchar_t const* filepath);
}
