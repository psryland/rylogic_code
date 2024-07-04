package nz.co.rylogic.allkeys

import android.content.Context
import android.os.Handler
import android.util.Log
import androidx.core.os.postDelayed
import java.beans.PropertyChangeSupport
import kotlin.math.absoluteValue
import kotlin.random.Random

data class KeyChordPair(val key: Int, val chord: Int)
data class Scale(val name: String, val notes: List<Int>)
data class Voice(val name: String, val lh: List<Short>, val rh: List<Short>)
data class Walk(val index: Int, val steps: List<WalkStep>)
data class Vamp(val index: Int, val steps: List<VampStep>)
class DrumPattern(val name: String, val midiData: ByteArray)

class Updater(context: Context, settings: Settings, handler: Handler) :Runnable, PropertyChangeSupport(handler)
{
	companion object
	{
		const val NOTIFY_CHORD = "chord"
		const val NOTIFY_BAR = "bar"
		const val NOTIFY_BEAT = "beat"
		const val NOTIFY_RUN_MODE = "run_mode"
	}

	private val mContext: Context = context
	private val mSettings: Settings = settings
	private val mHandler: Handler = handler
	private var mKeys: List<String> = listOf()
	private var mChords: List<String> = listOf()
	private var mOrder: MutableList<KeyChordPair> = mutableListOf()
	private var mWalk: Walk = Walk(-1, listOf())
	private var mVamp: Vamp = Vamp(-1, listOf())
	private var mScale: Scale = Scale("---", listOf(0))
	private var mVoices: List<Voice> = listOf()
	private var mRunMode: ERunMode = ERunMode.Stopped
	private var mStartTime: Long = 0
	private var mBeatCount: Int = 0

	// Careful! Beat and Bar are index of the current beat/bar.
	// They only advance just before the next beat/bar.
	private var mBeat: Int = -1
	private var mBar: Int = 0

	// The current 'key'
	val key: String
		get() = if (mOrder.size > 0) mKeys[mOrder[0].key] else mContext.resources.getString(R.string.no_key)

	// The current 'chord'
	val chord: String
		get() = if (mOrder.size > 0) mChords[mOrder[0].chord] else mContext.resources.getString(R.string.no_chord)

	// The next 'key'
	val keyNext: String
		get() = if (mOrder.size > 1) mKeys[mOrder[1].key] else mContext.resources.getString(R.string.no_key)

	// The current 'chord'
	val chordNext: String
		get() = if (mOrder.size > 1) mChords[mOrder[1].chord] else mContext.resources.getString(R.string.no_chord)

	// Return a scale to use for the current chord
	val scale: Scale
		get() = mScale

	// Return a chord voicing suitable for the current chord
	val voice: Voice
		get() = mVoices.randomOrNull() ?: Voice("silent", listOf(), listOf())

	// Return a walk to use for the current bar
	val walk: Walk
		get() = mWalk

	// Return a vamp to use for the current bar
	val vamp: Vamp
		get() = mVamp

	// The current beat
	val beat: Int
		get() = mBeat

	// The current bar
	val bar: Int
		get() = mBar

	// True to run continuously
	var runMode: ERunMode
		get() = mRunMode
		set(value)
		{
			if (runMode == value)
				return

			mHandler.removeCallbacks(this)

			val prev = mRunMode
			mRunMode = value

			// Reset the beat count
			mBeatCount = 0
			mStartTime = System.currentTimeMillis()
			resetBeat()

			// Notify of the mode change
			notifyUpdated(NOTIFY_RUN_MODE, prev, mRunMode)

			// Start running
			if (mRunMode != ERunMode.Stopped)
				run()
		}

	// The duration of one beat
	val beatPeriodMs: Int
		get() = (60.0 / mSettings.tempo * 1000).toInt()

	// The number of elapsed beats
	val beatCount: Int
		get() = mBeatCount

	// Chooses a new chord, random order, and resets to bar 0
	fun reset()
	{
		runMode = ERunMode.Stopped

		// Reset state
		mKeys = mSettings.keys
		mChords = mSettings.chords

		// Reset the order to empty
		mOrder = mutableListOf()

		// Select the first
		mBar = 0
		mBeat = 0
		nextChord()
		nextBar()
		resetBeat()

		// Reset is called once on startup, simulate the first 'runMode' change
		notifyUpdated(NOTIFY_RUN_MODE, null, mRunMode)
	}

	// Set the beat back to bar/beat 0
	private fun resetBeat()
	{
		mBeat = -1
		mBar = 0
	}

	// Advance to the next chord
	private fun nextChord()
	{
		if (mOrder.size != 0)
			mOrder.removeFirst()

		// We need the current and next in the list at all times
		if (mOrder.size < 2)
		{
			val order: MutableList<KeyChordPair> = mutableListOf()
			for (i in mKeys.indices)
			{
				for (j in mChords.indices)
				{
					if (mKeys[i] == key && mChords[j] == chord) continue
					if (mKeys[i] == keyNext && mChords[j] == chordNext) continue
					order.add(KeyChordPair(i, j))
				}
			}
			order.shuffle()
			mOrder.addAll(order)
		}

		// Choose a scale/voice for this chord
		mScale = chooseScale(chord)
		mVoices = chooseVoices(key, chord)

		// Notify of a new chord
		notifyUpdated(NOTIFY_CHORD)
	}

	// Advance to the next bar
	private fun nextBar()
	{
		// Choose walk/vamp for this bar
		mWalk = chooseWalk()
		mVamp = chooseVamp()

		// Notify of a new bar
		notifyUpdated(NOTIFY_BAR)
	}

	// Advance to the next beat
	private fun nextBeat()
	{
		// Time for next bar?
		if (++mBeat == mSettings.beatsPerBar)
		{
			mBeat = 0
			if (++mBar == mSettings.barsPerChord)
			{
				mBar = 0
				nextChord()
			}
			nextBar()
		}

		assert(mBeat >= 0 && mBeat < mSettings.beatsPerBar)
		assert(mBar >= 0 && mBar < mSettings.barsPerChord)

		// Signal a beat
		notifyUpdated(NOTIFY_BEAT, null, mBeat)

		++mBeatCount
	}

	// Runnable interface
	override fun run()
	{
		when (runMode)
		{
			ERunMode.Stopped -> {}
			ERunMode.StepOne ->
			{
				resetBeat()
				nextChord()
				nextBeat()
			}
			ERunMode.Continuous ->
			{
				// Want the beats to be exactly on time so the period of this call needs to be
				// shorter than the tempo. We want the callback to be a few milliseconds before
				// the next beat.
				nextBeat()

				// Schedule the next beat
				val mBias = 10
				val nextBeatTime = mStartTime + mBeatCount * beatPeriodMs
				val delayMs = (nextBeatTime - System.currentTimeMillis() - mBias).coerceAtLeast(0L)
				mHandler.postDelayed(this, delayMs)
			}
		}
	}

	// Notify listeners of an update
	private fun notifyUpdated(prop: String = "", oldValue: Any? = null, newValue: Any? = null)
	{
		firePropertyChange(prop, oldValue, newValue)
	}

	// Choose a scale to use for the given chord
	private fun chooseScale(chordName: String): Scale
	{
		// Find the chord
		val chord = mSettings.genre.chords[chordName] ?: Chord()

		// Choose a scale suitable for this chord
		val scale = chord.scales.randomOrNull() ?: return Scale("", listOf(0))

		// Apply the chromatic offset to the scale notes
		return scale.split(":").let {
			val name = it[0]
			val ofs = it[1].toInt()
			val notes = mSettings.genre.scales[name] ?: listOf(0)
			Scale(name, notes.map { n -> (n + ofs) % 12 }.sorted())
		}
	}

	// Choose a chord voicing to use for the given chord
	private fun chooseVoices(key: String, chordName: String): List<Voice>
	{
		// Get the base midi key for 'key'
		val rootNote = mSettings.keyRootNotes[key]

		// Find the chord
		val chord = mSettings.genre.chords[chordName]

		if (rootNote == null || chord == null || chord.voicings.isEmpty())
			return listOf(Voice("silent", listOf(), listOf()))

		fun mapToRange(root: Short, notes: List<Int>, range: IntRange): List<Short>
		{
			var shift = 0
			val n = root + notes.first()
			while (n + shift < range.first) { shift += 12 }
			while (n + shift >= range.last) { shift -= 12 }
			return notes.map { (root + it + shift).toShort() }
		}

		// Map the voicings into a nice range for the key
		val voices: MutableList<Voice> = mutableListOf()
		for (voicing in chord.voicings)
		{
			// Get the chord notes and map them into a nice range
			val lhNotes = mapToRange(rootNote, voicing.lh, IntRange(48, 96))
			val rhNotes = mapToRange(rootNote, voicing.rh, IntRange(lhNotes.last().toInt(), 96))

			// Add the voicing to the list
			voices.add(Voice(voicing.name, lhNotes, rhNotes))
		}
		return voices
	}

	// Choose the walk pattern to use for the next bar
	private fun chooseWalk() :Walk
	{
		// Heuristics:
		//  - Want to pick a different walk to the current one
		//  - Want to start near the last note of the previous walk
		//  - Want to transition if the 'keyNext' is not the same as 'key'
		//  - Prefer not to have the same note twice in a row
		//  - Prefer the walk not to start on the same note as the last walk
		//  - Strongly favour patterns that start on c1 for the first bar
		//  - Want some random variation in the selected walk
		val walks = mSettings.genre.walks(mSettings.beatsPerBar)

		val firstNote = if (mWalk.steps.isNotEmpty()) mWalk.steps.first().scaleDegree else 8
		val lastNote = if (mWalk.steps.isNotEmpty()) mWalk.steps.last().scaleDegree else 1
		val isTransition = mBar == mSettings.barsPerChord - 1

		// Assign weights to each walk option (smaller is better)
		val weights = walks.mapIndexed { i: Int, walk:List<WalkStep> ->
			val sameWalk = if (i == mWalk.index) 2.0 else 0.0
			val distance = (walk[0].scaleDegree - lastNote).absoluteValue * 0.5
			val transition = if (walk.any{ it.key == 'n' } == isTransition) 0.0 else 3.0
			val sameNote = if (walk[0].scaleDegree == lastNote) 1.0 else 0.0
			val sameStartNote = if (walk[0].scaleDegree == firstNote) 1.0 else 0.0
			val startOnRoot = (if (walk[0].scaleDegree != 1) 1.0 else 0.0) * if (mBar == 0) 5.0 else 1.0
			val random = Random.nextDouble()
			sameWalk + distance + transition + sameNote + sameStartNote + startOnRoot + random
		}

		// Choose the walk with the smallest weight
		val idx = weights.withIndex().minByOrNull { it.value }?.index ?: 0

		// Log the walk index
		Log.d("Walk", "Selected walk $idx. Weights: ${weights.joinToString(",")}")

		return if (walks.isNotEmpty()) Walk(idx, walks[idx]) else Walk(-1, listOf())
	}

	// Choose the vamp pattern to use for the next bar
	private fun chooseVamp() :Vamp
	{
		// Heuristics:
		//  - Want to pick a different vamp to the current one
		//  - Favour patterns that start on the first beat of a new chord
		//  - Want to transition if the 'keyNext' is not the same as 'key'
		//  - Want some random variation in the selected vamp
		val vamps = mSettings.genre.vamps(mSettings.beatsPerBar)
		val isTransition = mBar == mSettings.barsPerChord - 1
		val isStart = mBar == 0 && mBeat == 0

		// Assign weights to each vamp option (smaller is better)
		val weights = vamps.mapIndexed { i: Int, vamp:List<VampStep> ->
			val sameVamp = if (i == mVamp.index) 2.0 else 0.0
			val chordStart = if (isStart && vamp[0].key != 'c') 4.0 else 0.0
			val transition = if (vamp.any{ it.key == 'n' } == isTransition) 0.0 else 3.0
			val random = Random.nextDouble()
			sameVamp + chordStart + transition + random
		}

		// Choose the vamp with the smallest weight
		val idx = weights.withIndex().minByOrNull { it.value }?.index ?: 0

		// Log the vamp index
		Log.d("Vamp", "Selected vamp $idx. Weights: ${weights.joinToString(",")}")

		return if (vamps.isNotEmpty()) Vamp(idx, vamps[idx]) else Vamp(-1, listOf())
	}
}