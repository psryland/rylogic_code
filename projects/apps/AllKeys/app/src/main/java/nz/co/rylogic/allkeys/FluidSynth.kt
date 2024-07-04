package nz.co.rylogic.allkeys

import java.io.Closeable
import java.io.File

class FluidSynth : Closeable
{
	// Midi channels
	enum class EMidiChannel(val value: Short)
	{
		Ch1(0),
		Ch2(1),
		Ch3(2),
		Ch4(3),
		Ch5(4),
		Ch6(5),
		Ch7(6),
		Ch8(7),
		Ch9(8),
		Ch10(9),
		Ch11(10),
		Ch12(11),
		Ch13(12),
		Ch14(13),
		Ch15(14),
		Ch16(15),
		ChAll(16),
	}

	companion object
	{
		init
		{
			System.loadLibrary("fluidsynth")
			System.loadLibrary("fluidsynth_jni")
		}
	}

	private var synth: Long = createSynth()

	// Release this synth
	override fun close()
	{
		if (synth != 0L) destroySynth()
		synth = 0L
	}

	// Get/Set the master gain
	var masterGain: Float
		get() = masterGainGet()
		set(value) { masterGainSet(value) }

	// Load a sound font from the assets folder
	fun loadSoundFont(soundFont: File)
	{
		loadSoundFont(soundFont.absolutePath)
	}

	// Stop all sounds immediately
	fun allSoundsOff(channel: EMidiChannel)
	{
		allSoundsOff(channel.value)
	}

	// Stop all notes on 'channel' using Release events
	fun allNotesOff(channel: EMidiChannel)
	{
		allNotesOff(channel.value)
	}

	// Play/Stop a note
	fun playNote(channel: EMidiChannel, key: Short, velocity: Short)
	{
		playNote(channel.value, key, velocity)
	}
	fun stopNote(channel: EMidiChannel, key: Short)
	{
		stopNote(channel.value, key)
	}

	// Change the program
	fun programChange(channel: EMidiChannel, program: Int)
	{
		programChange(channel.value, program)
	}

	// The fluidsynth handle
	val handle: Long
		get() = synth

	// Synth
	private external fun createSynth(): Long
	private external fun destroySynth()
	private external fun loadSoundFont(soundFontPath: String)
	private external fun masterGainGet(): Float
	private external fun masterGainSet(gain: Float)
	private external fun allSoundsOff(channel: Short)
	private external fun allNotesOff(channel: Short)
	private external fun playNote(channel: Short, key: Short, velocity: Short)
	private external fun stopNote(channel: Short, key: Short)
	private external fun programChange(channel: Short, program: Int)
}
