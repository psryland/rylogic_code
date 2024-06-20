package nz.co.rylogic.allkeys

import android.content.Context
import android.os.Handler
import java.beans.PropertyChangeSupport
import kotlin.math.absoluteValue

class Updater(context: Context, handler: Handler) :Runnable, PropertyChangeSupport(handler)
{
	companion object
	{
		const val NOTIFY_RESET = "reset"
		const val NOTIFY_CHORD = "chord"
		const val NOTIFY_BAR = "bar"
		const val NOTIFY_BEAT = "beat"
		const val NOTIFY_RUN_MODE = "run_mode"
	}

	class KeyChordPair(val key: Int, val chord: Int)
	class Scale(val name: String, val notes: List<Int>)
	class Walk(val steps: List<String>)

	private val mContext: Context = context
	private lateinit var mSettings: Settings
	private val mHandler: Handler = handler
	private var mKeys: List<String> = listOf()
	private var mChords: List<String> = listOf()
	private var mOrder: MutableList<KeyChordPair> = mutableListOf()
	private var mWalk: Walk = Walk(listOf())
	private var mScale: Scale = Scale("unknown", List(7) { 0 })
	private var mRunMode: ERunMode = ERunMode.Stopped

	// Careful! Beat and Bar are index of the current beat/bar.
	// They only advance just before the next beat/bar.
	private var mBeat: Int = -1
	private var mBar: Int = 0

	// Map from chord type to scale options
	private val mScales: MutableMap<String, MutableList<Scale>> = mutableMapOf()

	// Map from bar length to walking pattern
	private val mWalks: MutableMap<Int, MutableList<Walk>> = mutableMapOf()

	// Constructor
	init
	{
		val scales = mContext.resources.getStringArray(R.array.scales)
		for (scale in scales)
		{
			val nameScale = scale.split(":")
			val name = nameScale[0]
			val notes = nameScale[1].split(",")
			mScales.getOrPut(name) { mutableListOf() }.add(Scale(name, notes.map { it.toInt() }))
		}

		val walks = mContext.resources.getStringArray(R.array.walks)
		for (walk in walks)
		{
			val steps = walk.split(",")
			mWalks.getOrPut(steps.size) { mutableListOf() }.add(Walk(steps))
		}
	}

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
	private val scale: Scale
		get() = mScale

	// Return a walk to use for the current bar
	private val walk: Walk
		get() = mWalk

	// Return the next root note to play
	val rootNote: Int
		get() = if (mSettings.rootNoteWalking) renderWalk() else keyToNoteIndex(key)

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

			val prev = mRunMode
			mRunMode = value
			mHandler.removeCallbacks(this)
			run()

			notifyUpdated(NOTIFY_RUN_MODE, prev, value)
		}

	// Snapshot the settings
	fun reset(settings: Settings)
	{
		mRunMode = ERunMode.Stopped

		// Reset state
		mSettings = settings
		mKeys = settings.keys
		mChords = settings.chords

		// Reset the order to empty
		mOrder = mutableListOf()

		// Select the first
		resetBeat()
		nextChord()
		nextBar()

		notifyUpdated(NOTIFY_RESET)
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
		// We need the current and next in the list at all times
		if (mOrder.size > 2)
		{
			mOrder.removeFirst()
		}
		else
		{
			val order: MutableList<KeyChordPair> = mutableListOf()
			for (i in mKeys.indices)
			{
				for (j in mChords.indices)
				{
					val pair = KeyChordPair(i, j)
					if (pair in mOrder) continue
					order.add(pair)
				}
			}
			order.shuffle()
			mOrder.addAll(order)
		}

		// Choose a scale to use for this chord
		mScale = mScales[chord]?.random() ?: Scale("unknown", List(7){0})

		// Notify of new values
		notifyUpdated(NOTIFY_CHORD)
	}

	// Advance to the next bar
	private fun nextBar()
	{
		// Generate a walking bass line for the next bar
		mWalk = mWalks[mSettings.beatsPerBar]?.random() ?: generateWalk(mSettings.beatsPerBar)

		// Notify of a new bar
		notifyUpdated(NOTIFY_BAR)
	}

	override fun run()
	{
		if (runMode == ERunMode.Stopped)
		{
			return
		}
		if (runMode == ERunMode.StepOne)
		{
			nextChord()
			resetBeat()
		}

		// Time for next bar?
		if (++mBeat == mSettings.beatsPerBar)
		{
			if (++mBar == mSettings.barsPerChord)
			{
				nextChord()
				mBar = 0
			}
			nextBar()
			mBeat = 0
		}

		assert(mBeat >= 0 && mBeat < mSettings.beatsPerBar)
		assert(mBar >= 0 && mBar < mSettings.barsPerChord)

		// Signal a beat
		notifyUpdated(NOTIFY_BEAT, null, mBeat)

		// Schedule the next call
		if (runMode == ERunMode.Continuous)
		{
			val periodS = 60.0 / mSettings.tempo // seconds / beat
			mHandler.postDelayed(this, (periodS * 1000).toLong())
		}
		if (runMode == ERunMode.StepOne)
		{
			runMode = ERunMode.Stopped
		}
	}

	private fun notifyUpdated(prop: String = "", oldValue: Any? = null, newValue: Any? = null)
	{
		firePropertyChange(prop, oldValue, newValue)
	}

	private fun renderWalk(): Int
	{
		/* Play the scale
		val i = mBar * mSettings.beatsPerBar + mBeat
		val nOfs = scale.notes[i % scale.notes.size]
		return (keyToNoteIndex(key) + nOfs) % 12
		*/

		// Beat out of range...
		if (mBeat < 0 || mBeat >= walk.steps.size)
			return keyToNoteIndex(key)

		// Invalid step in walk
		val step = walk.steps[mBeat]
		if (step.length < 2)
			return keyToNoteIndex(key)

		// Get the base key (either current or next)
		val k = when (step[0])
		{
			'c' -> keyToNoteIndex(key)
			'n' -> keyToNoteIndex(keyNext)
			else -> 0
		}

		// Is it raised or lowered?
		val accidental = when (step[1])
		{
			'+' -> 1
			'-' -> -1
			else -> 0
		}

		// Get the scale degree
		val kOfs = step.substring(1 + accidental.absoluteValue).toInt() - 1 // Because tonic is 1

		// Get the chromatic index of the scale note (clamped to [0,11]
		val idx = (scale.notes[kOfs % scale.notes.size] + accidental).coerceIn(0,11)

		// Get the note index relative to 'k'
		val degree = (k + idx) % 12
		return degree
	}

	private fun keyToNoteIndex(key: String): Int
	{
		return when (key)
		{
			"G" -> 0
			"G♯" -> 1
			"A♭" -> 1
			"A" -> 2
			"A♯" -> 3
			"B♭" -> 3
			"B" -> 4
			"B♯" -> 5
			"C♭" -> 4
			"C" -> 5
			"C♯" -> 6
			"D♭" -> 6
			"D" -> 7
			"D♯" -> 8
			"E♭" -> 8
			"E" -> 9
			"E♯" -> 10
			"F♭" -> 9
			"F" -> 10
			"F♯" -> 11
			"G♭" -> 11
			else -> 0
		}
	}

	private fun generateWalk(length: Int): Walk
	{
		val walk = mutableListOf<String>()
		for (i in 0 until length - 1)
			walk.add("c${i+1}")

		walk.add("n-2")
		return Walk(walk)
	}
}