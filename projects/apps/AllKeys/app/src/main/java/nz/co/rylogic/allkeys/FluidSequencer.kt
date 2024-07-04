package nz.co.rylogic.allkeys

import java.io.Closeable

class FluidSequencer(synth: FluidSynth) : Closeable
{
	companion object
	{
		init
		{
			System.loadLibrary("fluidsynth")
			System.loadLibrary("fluidsynth_jni")
		}
	}

	private var seq: Long = createSequencer(synth.handle)

	// Release this synth
	override fun close()
	{
		if (seq != 0L) destroySequencer()
		seq = 0L
	}

	// Get the current sequencer clock time
	val time: Long
		get() = tick()

	// Queue an event
	fun queue(event: FluidEvent, delay: Long, absolute: Boolean)
	{
		queueEvent(event, delay, absolute)
	}

	fun flush(eventType: FluidEvent.EType)
	{
		flush(eventType.value)
	}

	// Sequencer
	private external fun createSequencer(synth: Long): Long
	private external fun destroySequencer()
	private external fun tick(): Long
	private external fun queueEvent(event: FluidEvent, delay: Long, absolute: Boolean)
	private external fun flush(eventType: Int)
}