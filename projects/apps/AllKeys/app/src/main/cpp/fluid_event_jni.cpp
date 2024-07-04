#include "forward.h"
#include "event.h"

using namespace allkeys;

namespace allkeys
{
	// Global instances
	struct EventDeleter { void operator ()(fluid_event_t* ev) { delete_fluid_event(ev); } };
	std::vector<std::unique_ptr<fluid_event_t, EventDeleter>> g_events;

	// Convert a handle to an instance pointer
	inline fluid_event_t* event_ptr(JNIEnv* env, jobject obj)
	{
		return get<fluid_event_t*>(env, obj, "event");
	}
}

// Create an event instance
extern "C" JNIEXPORT event_handle_t JNICALL Java_nz_co_rylogic_allkeys_FluidEvent_createEvent(JNIEnv* env, jobject obj)
{
	return TryCatch<event_handle_t>([]
	{
		g_events.emplace_back(new_fluid_event());
		return reinterpret_cast<event_handle_t>(g_events.back().get());
	}, "Failed to create event");
}

// Destroy the event instance
extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidEvent_destroyEvent(JNIEnv* env, jobject obj)
{
	TryCatch([=]
	{
		auto event = event_ptr(env, obj);
		auto it = std::remove_if(g_events.begin(), g_events.end(), [event](const auto& s) { return s.get() == event; });
		g_events.erase(it, g_events.end());
	}, "Failed to destroy event");
}

extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidEvent_setNote(JNIEnv* env, jobject obj, jshort channel, jshort key, jshort velocity, jint duration)
{
	TryCatch([=]
	{
		auto event = event_ptr(env, obj);
		fluid_event_note(event, channel, key, velocity, duration);
	}, "Failed to create note event");
}

extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidEvent_setNoteOn(JNIEnv* env, jobject obj, jshort channel, jshort key, jshort velocity)
{
	TryCatch([=]
	{
		auto event = event_ptr(env, obj);
		fluid_event_noteon(event, channel, key, velocity);
	}, "Failed to create note on event");
}

extern "C" JNIEXPORT void JNICALL Java_nz_co_rylogic_allkeys_FluidEvent_setNoteOff(JNIEnv* env, jobject obj, jshort channel, jshort key)
{
	TryCatch([=]
	{
		auto event = event_ptr(env, obj);
		fluid_event_noteoff(event, channel, key);
	}, "Failed to create note off event");
}