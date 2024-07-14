package nz.co.rylogic.allkeys

import java.io.Closeable

enum class EFluidPlayerStatus(val value: Int)
{
	Ready(0),           /**< Player is ready */
	Playing(1),         /**< Player is currently playing */
	Stopping(2),        /**< Player is stopping, but hasn't finished yet (currently unused) */
	Done(3),             /**< Player is finished playing */
}

enum class EFluidPlayerTempoType(val value: Int)
{
	Internal(0),      /**< Use midi file tempo set in midi file (120 bpm by default). Multiplied by a factor */
	ExternalBPM(1),  /**< Set player tempo in bpm, supersede midi file tempo */
	ExternalMidi(2), /**< Set player tempo in us per quarter note, supersede midi file tempo */
};

class FluidPlayer(synth: FluidSynth) : Closeable
{
	companion object
	{
		init
		{
			System.loadLibrary("fluidsynth")
			System.loadLibrary("fluidsynth_jni")
		}
	}

	private var player: Long = createPlayer(synth.handle)

	// Release the player
	override fun close()
	{
		if (player != 0L) destroyPlayer()
		player = 0L
	}

	// Get the player status
	val status: EFluidPlayerStatus
		get() = EFluidPlayerStatus.values()[getStatus()]

	// Player controls
	fun play()
	{
		startPlayer()
	}
	fun stop()
	{
		pausePlayer()
	}
	fun seek(timeMs: Int)
	{
		seekTo(timeMs)
	}
	fun loop(enabled: Boolean)
	{
		loopMode(enabled)
	}

	// Get/Set tempo of playback
	val tempoBPM: Int
		get() = tempoBPM()
	fun setTempo(tempoType: EFluidPlayerTempoType, tempo: Double)
	{
		tempoSet(tempoType.value, tempo)
	}

	// Add a midi file from memory
	fun addData(midiData: ByteArray)
	{
		addMidiData(midiData)
	}

	// Add a midi file by file path
	fun addFile(midiPath: String)
	{
		addMidiFile(midiPath)
	}

	private external fun createPlayer(synth: Long): Long
	private external fun destroyPlayer()
	private external fun getStatus(): Int
	private external fun startPlayer()
	private external fun pausePlayer()
	private external fun seekTo(timeMs: Int)
	private external fun loopMode(enabled: Boolean)
	private external fun tempoBPM(): Int
	private external fun tempoSet(tempoType: Int, tempo: Double)
	private external fun addMidiData(data: ByteArray)
	private external fun addMidiFile(path: String)
}