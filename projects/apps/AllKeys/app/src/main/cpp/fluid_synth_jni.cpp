#include "forward.h"
#include "synth.h"

using namespace allkeys;

namespace allkeys
{
	// Global instances
	std::vector<std::unique_ptr<Synth>> g_synths;

	// Convert a handle to a synth instance pointer
	inline Synth* synth_ptr(JNIEnv* env, jobject obj)
	{
		return get<Synth*>(env, obj, "synth");
	}
//	bool drivers_registered = []()
//	{
//		static char const* drivers[] = { "alsa", "jack", "portaudio" };
//		fluid_audio_driver_register(drivers);
//		return true;
//	}();
}

// Create a synth instance
extern "C" JNIEXPORT synth_handle_t JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_createSynth(JNIEnv* env, jobject obj)
{
	return TryCatch<synth_handle_t>([]
	{
		g_synths.emplace_back(new Synth());
		return reinterpret_cast<synth_handle_t>(g_synths.back().get());
	}, "Failed to create synth");
}

// Destroy the synth instance
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_destroySynth(JNIEnv* env, jobject obj)
{
	TryCatch([=]
	{
		auto synth = synth_ptr(env, obj);
		auto it = std::remove_if(g_synths.begin(), g_synths.end(), [synth](const auto& s) { return s.get() == synth; });
		g_synths.erase(it, g_synths.end());
	}, "Failed to destroy synth");
}

// Load a soundfont
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_loadSoundFont(JNIEnv* env, jobject obj, jstring sf_path)
{
	TryCatch([=]
	{
		jni_string path(env, sf_path);
		synth_ptr(env, obj)->LoadSoundFont(path);
	}, "Loading soundfont failed");
}

extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_playNote(JNIEnv* env, jobject obj, jshort channel, jshort key, jshort velocity)
{
	TryCatch([=]
	{
		synth_ptr(env, obj)->NoteOn(midi_channel_t(channel), midi_key_t(key), midi_velocity_t(velocity));
	}, "Failed to play note");
}

extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_stopNote(JNIEnv* env, jobject obj, jshort channel, jshort key)
{
	TryCatch([=]
	{
		synth_ptr(env, obj)->NoteOff(midi_channel_t(channel), midi_key_t(key));
	}, "Failed to stop note");
}

// Get/Set Master gain
extern "C" JNIEXPORT jfloat  JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_masterGainGet(JNIEnv* env, jobject obj)
{
	return TryCatch<jfloat>([=]
	{
		return synth_ptr(env, obj)->MasterGain();
	}, "Failed to get master gain", 0.0f);
}
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_masterGainSet(JNIEnv* env, jobject obj, jfloat gain)
{
	TryCatch([=]
	{
		synth_ptr(env, obj)->MasterGain(gain);
	}, "Failed to set master gain");
}

// Stop all sounds
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_allSoundsOff(JNIEnv* env, jobject obj, jshort channel)
{
	TryCatch([=]
	{
		synth_ptr(env, obj)->AllSoundsOff(midi_channel_t(channel));
	}, "Failed to stop all sounds");
}

// Stop all notes
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_allNotesOff(JNIEnv* env, jobject obj, jshort channel)
{
	TryCatch([=]
	{
		synth_ptr(env, obj)->AllNotesOff(midi_channel_t(channel));
	}, "Failed to stop all notes");
}

// Change the program for a channel
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSynth_programChange(JNIEnv* env, jobject obj, jshort channel, jint program)
{
	TryCatch([=]
	{
		synth_ptr(env, obj)->ProgramChange(midi_channel_t(channel), program);
	}, "Failed to change program");
}
