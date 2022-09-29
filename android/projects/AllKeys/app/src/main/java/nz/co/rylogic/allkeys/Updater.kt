package nz.co.rylogic.allkeys

import android.content.SharedPreferences
import android.os.Handler
import java.beans.PropertyChangeSupport

class Updater(handler: Handler) :Runnable, PropertyChangeSupport(handler)
{
	private val mHandler: Handler = handler
	private var mKeys: List<String> = listOf()
	private var mChords: List<String> = listOf()
	private var mKeysOrder: MutableList<Int> = mutableListOf()
	private var mChordsOrder: MutableList<Int> = mutableListOf()

	// Snapshot the settings
	fun reset(sp: SharedPreferences, period:Double)
	{
		this.period = period

		// Snapshot the settings into lists
		mKeys = (sp.getStringSet("selected_keys", null) ?: setOf())
			.map { it.splitIgnoreEmpty(" ", ",") }
			.flatten()
		mChords = (sp.getStringSet("selected_chords_std", null) ?: setOf())
			.union((sp.getString("selected_chords_additional", null) ?: "")
			.splitIgnoreEmpty(" ", ","))
			.toList()

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

	// The update period in seconds
	var period: Double
		get() = mPeriod
		set(value) { mPeriod = value }
	private var mPeriod: Double = 0.1

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

		// Notify of new values
		running = !stop
		notifyUpdated()
	}

	override fun run()
	{
		if (!running)
			return

		// Advance
		next(false)

		// Schedule the next call
		val periodMS = (period * 1000L).toLong()
		mHandler.postDelayed(this, periodMS)
	}

	private fun notifyUpdated()
	{
		firePropertyChange("", null, null)
	}
}