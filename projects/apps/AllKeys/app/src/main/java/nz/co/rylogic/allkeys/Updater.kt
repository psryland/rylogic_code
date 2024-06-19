package nz.co.rylogic.allkeys

import android.content.Context
import android.os.Handler
import java.beans.PropertyChangeSupport

enum class ERunMode
{
	Stopped,
	StepOne,
	Continuous,
}

class Updater(context: Context, handler: Handler) :Runnable, PropertyChangeSupport(handler)
{
	companion object
	{
		const val NOTIFY_CHORD = "chord"
		const val NOTIFY_BEAT = "beat"
		const val RUN_MODE = "run_mode"
	}

	class KeyChordPair(val key: Int, val chord: Int)

	private val mContext: Context = context
	private lateinit var mSettings: Settings
	private val mHandler: Handler = handler
	private var mKeys: List<String> = listOf()
	private var mChords: List<String> = listOf()
	private var mOrder: MutableList<KeyChordPair> = mutableListOf()
	private var mRunMode: ERunMode = ERunMode.Stopped
	private var mBeat: Int = 0

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

	// True to run continuously
	var runMode: ERunMode
		get() = mRunMode
		set(value)
		{
			if (runMode == value)
				return

			val prev = mRunMode;
			mRunMode = value
			mHandler.removeCallbacks(this)
			run()

			notifyUpdated(RUN_MODE, prev, value)
		}

	// Snapshot the settings
	fun reset(settings: Settings)
	{
		mRunMode = ERunMode.Stopped

		// Reset state
		mSettings = settings
		mKeys = settings.keys
		mChords = settings.chords
		mBeat = 0

		// Reset the order to empty
		mOrder = mutableListOf()

		// Select the first
		next()
	}

	// Advance to the next chord
	fun next()
	{
		// We need the current and next in the list at all times
		if (mOrder.size > 2)
		{
			mOrder.removeFirst()
		}
		else
		{
			val order:MutableList<KeyChordPair> = mutableListOf()
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

		// Notify of new values
		notifyUpdated(NOTIFY_CHORD)
	}

	override fun run()
	{
		if (runMode == ERunMode.Stopped)
			return

		// Time for next chord?
		if (mBeat == mSettings.beatsPerBar * mSettings.barsPerChord)
		{
			next()
			mBeat = 0
		}

		// Beat played
		notifyUpdated(NOTIFY_BEAT, null, mBeat)
		++mBeat

		// Schedule the next call
		if (runMode == ERunMode.Continuous)
		{
			val periodS = 60.0 / mSettings.tempo // seconds / beat
			mHandler.postDelayed(this, (periodS * 1000).toLong())
		}
		if (runMode == ERunMode.StepOne)
		{
			runMode = ERunMode.Stopped
			mBeat = 0
		}
	}

	private fun notifyUpdated(prop:String = "", oldValue:Any? = null, newValue:Any? = null)
	{
		firePropertyChange(prop, oldValue, newValue)
	}
}