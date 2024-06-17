package nz.co.rylogic.allkeys

import android.content.Context
import android.os.Handler
import java.beans.PropertyChangeSupport

class Updater(context: Context, handler: Handler) :Runnable, PropertyChangeSupport(handler)
{
	companion object
	{
		const val NOTIFY_CHORD = "chord"
		const val NOTIFY_BEAT = "beat"
		const val RUNNING = "running"
	}

	private val mContext: Context = context
	private lateinit var mSettings: Settings
	private val mHandler: Handler = handler
	private var mRunning: Boolean = false
	private var mKeys: List<String> = listOf()
	private var mChords: List<String> = listOf()
	private var mKeysOrder: MutableList<Int> = mutableListOf()
	private var mChordsOrder: MutableList<Int> = mutableListOf()
	private var mBeat: Int = 0

	// Snapshot the settings
	fun reset(settings: Settings)
	{
		this.mBeat = 0
		this.mSettings = settings

		// Snapshot the settings into lists
		mKeys = settings.keys
		mChords = settings.chords

		// Reset the order to empty
		mKeysOrder = mutableListOf()
		mChordsOrder = mutableListOf()

		// Select the first
		next()
	}

	// The current 'key'
	val key: String
		get() = if (mKeysOrder.size > 0) mKeys[mKeysOrder[0]] else mContext.resources.getString(R.string.no_key)

	// The current 'chord'
	val chord: String
		get() = if (mChordsOrder.size > 0) mChords[mChordsOrder[0]] else mContext.resources.getString(R.string.no_chord)

	// The next 'key'
	val keyNext: String
		get() = if (mKeysOrder.size > 1) mKeys[mKeysOrder[1]] else mContext.resources.getString(R.string.no_key)

	// The current 'chord'
	val chordNext: String
		get() = if (mChordsOrder.size > 1) mChords[mChordsOrder[1]] else mContext.resources.getString(R.string.no_chord)

	// True if the updater is running
	var running
		get() = mRunning
		set(value)
		{
			if (mRunning == value) return
			mRunning = value
			mHandler.removeCallbacks(this)
			if (mRunning) run()
			notifyUpdated(RUNNING, !value, value)
		}

	// Advance to the next chord
	fun next(stop:Boolean = true)
	{
		fun adv(list: MutableList<Int>, size: Int)
		{
			// We need the current and next in the list at all times
			if (list.size > 1)
			{
				list.removeFirst()
			}
			if (list.size <= 1)
			{
				list.addAll(0 until size)
				list.shuffle()
			}
		}

		// Update the chord
		adv(mKeysOrder, mKeys.size)
		adv(mChordsOrder, mChords.size)
		mBeat = 0

		// Notify of new values
		running = !stop
		notifyUpdated(NOTIFY_CHORD)
		notifyUpdated(NOTIFY_BEAT, null, mBeat)

	}

	override fun run()
	{
		if (!running)
			return

		// Time for next chord?
		if (mBeat == mSettings.beatsPerBar * mSettings.barsPerChord)
			next(false)
		else
			notifyUpdated(NOTIFY_BEAT, null, mBeat)

		// Beat played
		++mBeat

		// Schedule the next call
		val periodS = 60.0 / mSettings.tempo // seconds / beat
		mHandler.postDelayed(this, (periodS * 1000).toLong())
	}

	private fun notifyUpdated(prop:String = "", oldValue:Any? = null, newValue:Any? = null)
	{
		firePropertyChange(prop, oldValue, newValue)
	}
}