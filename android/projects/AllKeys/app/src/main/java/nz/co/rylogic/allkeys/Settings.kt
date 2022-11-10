package nz.co.rylogic.allkeys

import android.content.Context
import android.content.SharedPreferences
import androidx.preference.PreferenceManager

class Settings(context: Context)
{
	companion object
	{
		const val SELECTED_KEYS = "selected_keys"
		const val SELECTED_CHORDS = "selected_chords"
		const val CUSTOM_CHORDS = "custom_chords"
		const val PLAY_ROOT_NOTE = "play_root_note"
	}

	private val mSettings: SharedPreferences = PreferenceManager.getDefaultSharedPreferences(context)
	private val mKeysDefaults = context.resources.getStringArray(R.array.key_groups_defaults)
	private val mChordsDefaults = context.resources.getStringArray(R.array.chords_defaults)
	private val mChordsStd = context.resources.getStringArray(R.array.chords)

	// The list of individual key names
	val keys: List<String>
		get()
		{
			return keyGroupsSelected
				.map { it.splitIgnoreEmpty(" ", ",") }
				.flatten()
		}

	// The list of selected key groups
	var keyGroupsSelected: Set<String>
		get()
		{
			return mSettings.getStringSet(SELECTED_KEYS, null) ?: mKeysDefaults.toSet()
		}
		set(value)
		{
			var editor = mSettings.edit()
			editor.putStringSet(SELECTED_KEYS, value)
			editor.commit()
		}

	// The list of all chord names (standard + custom combined)
	val chordsAll: Array<String>
		get()
		{
			return mChordsStd.union(chordsCustom).toTypedArray()
		}

	// The list of selected chord names
	val chords: List<String>
		get()
		{
			return chordsSelected.toList()
		}

	// The selected chords
	var chordsSelected: Set<String>
		get()
		{
			return mSettings.getStringSet(SELECTED_CHORDS, null) ?: mChordsDefaults.toSet()
		}
		set(value)
		{
			var editor = mSettings.edit()
			editor.putStringSet(SELECTED_CHORDS, value)
			editor.commit()
		}

	// The list of custom chord names
	private var chordsCustom: List<String>
		get()
		{
			return (mSettings.getString(CUSTOM_CHORDS, null) ?: "")
				.splitIgnoreEmpty(" ", ",")
				.toList()
		}
		set(value)
		{
			var editor = mSettings.edit()
			editor.putString(CUSTOM_CHORDS, value.joinToString(" "))
			editor.commit()
		}

	// True if the tonic should be played with each chord change
	val playRootNote:Boolean
		get()
		{
			return mSettings.getBoolean(PLAY_ROOT_NOTE, false)
		}

	// Number of beats per bar
	val beatsPerBar:Int
		get()
		{
			return 4
		}

	// The number of bars to hold each chord for
	val barsPerChord:Int
		get()
		{
			return 2
		}
}