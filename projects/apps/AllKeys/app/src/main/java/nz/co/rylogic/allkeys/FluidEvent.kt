package nz.co.rylogic.allkeys

import java.io.Closeable

class FluidEvent : Closeable
{
	enum class EType(val value: Int)
	{
		FLUID_SEQ_NOTE(0),		/**< Note event with duration */
		FLUID_SEQ_NOTEON(1),		/**< Note on event */
		FLUID_SEQ_NOTEOFF(2),		/**< Note off event */
		FLUID_SEQ_ALLSOUNDSOFF(3),	/**< All sounds off event */
		FLUID_SEQ_ALLNOTESOFF(4),	/**< All notes off event */
		FLUID_SEQ_BANKSELECT(5),		/**< Bank select message */
		FLUID_SEQ_PROGRAMCHANGE(6),	/**< Program change message */
		FLUID_SEQ_PROGRAMSELECT(7),	/**< Program select message */
		FLUID_SEQ_PITCHBEND(8),		/**< Pitch bend message */
		FLUID_SEQ_PITCHWHEELSENS(9),	/**< Pitch wheel sensitivity set message @since 1.1.0 was misspelled previously */
		FLUID_SEQ_MODULATION(10),		/**< Modulation controller event */
		FLUID_SEQ_SUSTAIN(11),		/**< Sustain controller event */
		FLUID_SEQ_CONTROLCHANGE(12),	/**< MIDI control change event */
		FLUID_SEQ_PAN(13),		/**< Stereo pan set event */
		FLUID_SEQ_VOLUME(14),		/**< Volume set event */
		FLUID_SEQ_REVERBSEND(15),		/**< Reverb send set event */
		FLUID_SEQ_CHORUSSEND(16),		/**< Chorus send set event */
		FLUID_SEQ_TIMER(17),		/**< Timer event (useful for giving a callback at a certain time) */
		FLUID_SEQ_CHANNELPRESSURE(18),    /**< Channel aftertouch event @since 1.1.0 */
		FLUID_SEQ_KEYPRESSURE(19),        /**< Polyphonic aftertouch event @since 2.0.0 */
		FLUID_SEQ_SYSTEMRESET(20),        /**< System reset event @since 1.1.0 */
		FLUID_SEQ_UNREGISTERING(21),      /**< Called when a sequencer client is being unregistered. @since 1.1.0 */
		FLUID_SEQ_SCALE(22),              /**< Sets a new time scale for the sequencer @since 2.2.0 */
	}

	companion object
	{
		init
		{
			System.loadLibrary("fluidsynth")
			System.loadLibrary("fluidsynth_jni")
		}
	}

	private var event: Long = createEvent()

	override fun close()
	{
		if (event != 0L) destroyEvent()
		event = 0L
	}

	fun note(channel: Short, key: Short, velocity: Short, duration: Int): FluidEvent
	{
		setNote(channel, key, velocity, duration)
		return this
	}

	fun noteOn(channel: Short, key: Short, velocity: Short): FluidEvent
	{
		setNoteOn(channel, key, velocity)
		return this
	}

	fun noteOff(channel: Short, key: Short): FluidEvent
	{
		setNoteOff(channel, key)
		return this
	}

	private external fun createEvent(): Long
	private external fun destroyEvent()
	private external fun setNote(channel: Short, key: Short, velocity: Short, duration: Int)
	private external fun setNoteOn(channel: Short, key: Short, velocity: Short)
	private external fun setNoteOff(channel: Short, key: Short)
}
