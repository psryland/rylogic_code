package nz.co.rylogic.allkeys

import android.content.Context
import kotlin.jvm.Throws

class FluidSynth
{
	companion object
	{
		init
		{
			System.loadLibrary("fluidsynth")
			System.loadLibrary("fluidsynth_jni")
		}
	}

	private var handle: Long = createSynth()

	// Clean up the synth
	@Throws(Throwable::class)
	protected fun finalize()
	{
		release()
	}

	// Release this synth
	fun release()
	{
		if (handle == 0L) return
		destroySynth(handle)
		handle = 0L
	}

	// Get/Set the master gain
	var masterGain: Float
		get() = masterGainGet(handle)
		set(value) { masterGainSet(handle, value) }

	// Load a sound font from the assets folder
	fun loadSoundFont(context: Context, assetName: String)
	{
		val file = copyAssetToFile(context, assetName)
		loadSoundFont(handle, file.absolutePath)
	}

	// Play/Stop a note
	fun playNote(channel: Int, key: Int, velocity: Int)
	{
		playNote(handle, channel, key, velocity)
	}
	fun stopNote(channel: Int, key: Int)
	{
		stopNote(handle, channel, key)
	}

	// Interop functions
	private external fun createSynth(): Long
	private external fun destroySynth(handle: Long)
	private external fun loadSoundFont(handle: Long, soundFontPath: String)
	private external fun masterGainGet(handle: Long): Float
	private external fun masterGainSet(handle: Long, gain: Float)
	private external fun playNote(handle: Long, channel: Int, key: Int, velocity: Int)
	private external fun stopNote(handle: Long, channel: Int, key: Int)
}
