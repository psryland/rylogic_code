#include "forward.h"
#include "player.h"

using namespace allkeys;

namespace allkeys
{
	// Global instances
	std::vector<std::unique_ptr<Player>> g_players;

	// Convert a handle to a synth instance pointer
	inline Player* player_ptr(JNIEnv* env, jobject obj)
	{
		return get<Player*>(env, obj, "player");
	}
}

// Create a player instance
extern "C" JNIEXPORT player_handle_t JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_createPlayer(JNIEnv* env, jobject obj, synth_handle_t synth)
{
	return TryCatch<player_handle_t>([=]
	{
		g_players.emplace_back(new Player(*reinterpret_cast<Synth*>(synth)));
		return reinterpret_cast<player_handle_t>(g_players.back().get());
	}, "Failed to create player");
}

// Destroy the player instance
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_destroyPlayer(JNIEnv* env, jobject obj)
{
	TryCatch([=]
	{
		auto player = player_ptr(env, obj);
		auto it = std::remove_if(g_players.begin(), g_players.end(), [player](const auto& p) { return p.get() == player; });
		g_players.erase(it, g_players.end());
	}, "Failed to destroy player");
}

// Get the player status
extern "C" JNIEXPORT jint JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_getStatus(JNIEnv* env, jobject obj)
{
	return TryCatch<jint>([=]
	{
		return static_cast<jint>(player_ptr(env, obj)->Status());
	}, "Failed to get player status");
}

// Start the player
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_startPlayer(JNIEnv* env, jobject obj)
{
	TryCatch([=]
	{
		player_ptr(env, obj)->Play();
	}, "Failed to start player");
}

// Pause the player
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_pausePlayer(JNIEnv* env, jobject obj)
{
	TryCatch([=]
	{
		player_ptr(env, obj)->Pause();
	}, "Failed to stop player");
}

// Set the player loop mode
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_loopMode(JNIEnv* env, jobject obj, jboolean enabled)
{
	TryCatch([=]
	{
		player_ptr(env, obj)->Loop(enabled);
	}, "Failed to set player loop");
}

// Seek the player
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_seekTo(JNIEnv* env, jobject obj, jint time_ms)
{
	TryCatch([=]
	{
		player_ptr(env, obj)->Seek(time_ms);
	}, "Failed to seek player");
}

// Get/Set tempo of playback
extern "C" JNIEXPORT jint JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_tempoBPM(JNIEnv* env, jobject obj)
{
	return TryCatch<jint>([=]
	{
		return player_ptr(env, obj)->TempoBPM();
	}, "Failed to get player tempo", 0);
}

// Get/Set tempo of playback
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_tempoSet(JNIEnv* env, jobject obj, jint tempo_type, jdouble tempo)
{
	TryCatch([=]
	{
		player_ptr(env, obj)->Tempo(static_cast<fluid_player_set_tempo_type>(tempo_type), tempo);
	}, "Failed to set player tempo");
}

// Add midi data from memory
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_addMidiData(JNIEnv* env, jobject obj, jbyteArray data)
{
	TryCatch([=]
	{
		jni_bytearray midi_data(env, data);
		player_ptr(env, obj)->Add(midi_data);
	}, "Failed to add midi data");
}

// Add midi data by file path
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidPlayer_addMidiFile(JNIEnv* env, jobject obj, jstring file_path)
{
	TryCatch([=]
	{
		jni_string path(env, file_path);
		player_ptr(env, obj)->Add(path);
	}, "Failed to add midi file");
}