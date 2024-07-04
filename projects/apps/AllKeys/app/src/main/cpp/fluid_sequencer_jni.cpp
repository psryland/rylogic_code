#include "forward.h"
#include "sequencer.h"

using namespace allkeys;

namespace allkeys
{
	// Global instances
	std::vector<std::unique_ptr<Sequencer>> g_sequencers;

	// Convert a handle to a synth instance pointer
	inline Sequencer* seq_ptr(JNIEnv* env, jobject obj)
	{
		return get<Sequencer*>(env, obj, "seq");
	}
}

extern "C" JNIEXPORT seq_handle_t JNICALL Java_nz_co_rylogic_allkeys_FluidSequencer_createSequencer(JNIEnv* env, jobject obj, synth_handle_t synth)
{
	return TryCatch<seq_handle_t>([=]
	{
		g_sequencers.emplace_back(new Sequencer(*reinterpret_cast<Synth*>(synth), "Sequencer"));
		return reinterpret_cast<seq_handle_t>(g_sequencers.back().get());
	}, "Failed to create sequencer");
}

extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSequencer_destroySequencer(JNIEnv* env, jobject obj)
{
	TryCatch([=]
	{
		auto seq = seq_ptr(env, obj);
		auto it = std::remove_if(g_sequencers.begin(), g_sequencers.end(), [seq](const auto& s) { return s.get() == seq; });
		g_sequencers.erase(it, g_sequencers.end());
	}, "Failed to destroy sequencer");
}

extern "C" JNIEXPORT jlong JNICALL Java_nz_co_rylogic_allkeys_FluidSequencer_tick(JNIEnv* env, jobject obj)
{
	return TryCatch([=]
	{
		return static_cast<jlong>(seq_ptr(env, obj)->Tick());
	}, "Failed to process sequencer", 0LL);
}

extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSequencer_queueEvent(JNIEnv* env, jobject obj, jobject event, jlong delay, jboolean absolute)
{
	TryCatch([=]
	{
		auto ev = get<fluid_event_t*>(env, event, "event");
		seq_ptr(env, obj)->Queue(ev, milliseconds_t(delay), absolute);
	}, "Failed to queue event");
}

extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidSequencer_flush(JNIEnv* env, jobject obj, jint event_type)
{
	TryCatch([=]
	{
		seq_ptr(env, obj)->Flush(static_cast<fluid_seq_event_type>(event_type));
	}, "Failed to flush events from the sequencer");
}