package nz.co.rylogic.allkeys

import android.os.Handler
import java.beans.PropertyChangeSupport

class Updater(handler: Handler) :Runnable, PropertyChangeSupport(handler)
{
	companion object
	{
		const val NOTIFY_CHORD = "chord"
		const val NOTIFY_BEAT = "beat"
	}

	private val mHandler: Handler = handler
	private var mKeys: List<String> = listOf()
	private var mChords: List<String> = listOf()
	private var mKeysOrder: MutableList<Int> = mutableListOf()
	private var mChordsOrder: MutableList<Int> = mutableListOf()
	private var mBeatsPerBar: Int = 4
	private var mBarsPerChord: Int = 2
	private var mBeat: Int = 0

	// Snapshot the settings
	fun reset(settings: Settings, tempo:Double)
	{
		this.tempo = tempo
		this.mBeat = 0
		this.mBeatsPerBar = settings.beatsPerBar
		this.mBarsPerChord = settings.barsPerChord

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
		get() = if (mKeysOrder.size != 0) mKeys[mKeysOrder.last()] else "---"

	// The current 'chord'
	val chord: String
		get() = if (mChordsOrder.size != 0) mChords[mChordsOrder.last()] else "---"

	// True if the updater is running
	var running
		get() = mRunning
		set(value)
		{
			if (mRunning == value) return
			mRunning = value
			mHandler.removeCallbacks(this)
			if (mRunning) run()
			notifyUpdated()
		}
	private var mRunning: Boolean = false

	// The rate to change chords (in 8x beats/min)
	var tempo: Double
		get() = mTempo
		set(value) { mTempo = value }
	private var mTempo: Double = 120.0

	// Advance to the next chord
	fun next(stop:Boolean = true)
	{
		fun adv(list: MutableList<Int>, size: Int)
		{
			if (list.size != 0)
			{
				list.removeLast()
			}
			if (list.size == 0)
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
	}

	override fun run()
	{
		if (!running)
			return

		// Time for next chord?
		if (mBeat == mBeatsPerBar * mBarsPerChord)
			next(false)

		// Notify metronome
		notifyUpdated(NOTIFY_BEAT,null,mBeat)

		// Beat played
		++mBeat

		// Schedule the next call
		var periodS = 60.0 / tempo // seconds / beat
		mHandler.postDelayed(this, (periodS * 1000).toLong())
	}

	private fun notifyUpdated(prop:String = "", oldValue:Any? = null, newValue:Any? = null)
	{
		firePropertyChange(prop, oldValue, newValue)
	}
}