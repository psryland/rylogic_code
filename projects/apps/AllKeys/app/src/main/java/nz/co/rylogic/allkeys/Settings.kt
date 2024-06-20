package nz.co.rylogic.allkeys

import android.content.Context
import android.content.SharedPreferences
import androidx.preference.PreferenceManager

class Settings(context: Context)
{
	companion object
	{
		// These constants are used to match the preference controls to saved values
		const val TEMPO = "tempo"
		const val SELECTED_KEYS = "selected_keys"
		const val SELECTED_CHORDS = "selected_chords"
		const val CUSTOM_CHORDS = "custom_chords"
		const val NEXT_CHORD_MODE = "next_chord_mode"
		const val ROOT_NOTE_SOUNDS = "root_note_sounds"
		const val ROOT_NOTE_VOLUME = "root_note_volume"
		const val ROOT_NOTE_INSTRUMENT = "root_note_instrument"
		const val ROOT_NOTE_WALKING = "root_note_walking"
		const val METRONOME_SOUNDS = "metronome_sounds"
		const val METRONOME_VOLUME = "metronome_volume"
		const val METRONOME_ACCENT = "metronome_accent"
		const val METRONOME_CLICK = "metronome_click"
		const val BEATS_PER_BAR = "beats_per_bar"
		const val BARS_PER_CHORD = "bars_per_chord"
		const val THEME = "theme"
	}

	private val mSettings: SharedPreferences = PreferenceManager.getDefaultSharedPreferences(context)
	private val mKeysDefaults = context.resources.getStringArray(R.array.key_groups_defaults)
	private val mChordsDefaults = context.resources.getStringArray(R.array.chords_defaults)
	private val mChordsStd = context.resources.getStringArray(R.array.chords)

	init
	{
		// This is needed because the settings UI accesses the 'SharedPreferences' object directly.
		// We need to ensure all settings exist or the Preferences stuff will crash.
		val editor = mSettings.edit()
		editor.putInt(TEMPO, tempo)
		editor.putStringSet(SELECTED_KEYS, keyGroupsSelected)
		editor.putStringSet(SELECTED_CHORDS, chordsSelected)
		editor.putString(CUSTOM_CHORDS, chordsCustom.joinToString(" "))
		editor.putString(NEXT_CHORD_MODE, nextChordMode.toString())
		editor.putBoolean(ROOT_NOTE_SOUNDS, rootNoteSounds)
		editor.putInt(ROOT_NOTE_VOLUME, rootNoteVolume)
		editor.putString(ROOT_NOTE_INSTRUMENT, rootNoteInstrument.toString())
		editor.putBoolean(ROOT_NOTE_WALKING, rootNoteWalking)
		editor.putBoolean(METRONOME_SOUNDS, metronomeSounds)
		editor.putInt(METRONOME_VOLUME, metronomeVolume)
		editor.putString(METRONOME_ACCENT, metronomeAccent.toString())
		editor.putString(METRONOME_CLICK, metronomeClick.toString())
		editor.putInt(BEATS_PER_BAR, beatsPerBar)
		editor.putInt(BARS_PER_CHORD, barsPerChord)
		editor.putString(THEME, theme.toString())
		editor.apply()
	}

	// Return the settings field names
	val fields: Set<String>
		get() = mSettings.all.keys

	// The last selected tempo value
	var tempo: Int
		get()
		{
			return mSettings.getInt(TEMPO, 120)
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putInt(TEMPO, value)
			editor.apply()
		}

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
			val editor = mSettings.edit()
			editor.putStringSet(SELECTED_KEYS, value)
			editor.apply()
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
			val editor = mSettings.edit()
			editor.putStringSet(SELECTED_CHORDS, value)
			editor.apply()
		}

	// The list of custom chord names
	private var chordsCustom: List<String>
		get()
		{
			return (mSettings.getString(CUSTOM_CHORDS, null) ?: "")
				.splitIgnoreEmpty(",")
				.map { it.trim() }
				.toList()
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putString(CUSTOM_CHORDS, value.joinToString(","))
			editor.apply()
		}

	// How to display the next chord
	var nextChordMode: ENextChordMode
		get()
		{
			return ENextChordMode.valueOf(mSettings.getString(NEXT_CHORD_MODE, null) ?: ENextChordMode.BarBefore.toString())
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putString(NEXT_CHORD_MODE, value.toString())
			editor.apply()
		}

	// True if the tonic should be played with each chord change
	var rootNoteSounds:Boolean
		get()
		{
			return mSettings.getBoolean(ROOT_NOTE_SOUNDS, true)
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putBoolean(ROOT_NOTE_SOUNDS, value)
			editor.apply()
		}

	// Relative volume of the root notes
	var rootNoteVolume:Int
		get()
		{
			return mSettings.getInt(ROOT_NOTE_VOLUME, 80)
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putInt(ROOT_NOTE_VOLUME, value)
			editor.apply()
		}

	// The instrument to use for the root notes
	var rootNoteInstrument:EInstrument
		get()
		{
			return EInstrument.valueOf(mSettings.getString(ROOT_NOTE_INSTRUMENT, null) ?: EInstrument.Piano.toString())
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putString(ROOT_NOTE_INSTRUMENT, value.toString())
			editor.apply()
		}

	// True if the bass should walk
	var rootNoteWalking:Boolean
		get()
		{
			return mSettings.getBoolean(ROOT_NOTE_WALKING, true)
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putBoolean(ROOT_NOTE_WALKING, value)
			editor.apply()
		}

	// True if the metronome should play sounds
	var metronomeSounds:Boolean
		get()
		{
			return mSettings.getBoolean(METRONOME_SOUNDS, true)
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putBoolean(METRONOME_SOUNDS, value)
			editor.apply()
		}

	// Relative volume of the metronome
	var metronomeVolume:Int
		get()
		{
			return mSettings.getInt(METRONOME_VOLUME, 80)
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putInt(METRONOME_VOLUME, value)
			editor.apply()
		}

	// The accented click sound to use
	var metronomeAccent: EMetronomeSounds
		get()
		{
			return EMetronomeSounds.valueOf(mSettings.getString(METRONOME_ACCENT, null) ?: EMetronomeSounds.Clave.toString())
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putString(METRONOME_ACCENT, value.toString())
			editor.apply()
		}

	// The click sound to use
	var metronomeClick: EMetronomeSounds
		get()
		{
			return EMetronomeSounds.valueOf(mSettings.getString(METRONOME_CLICK, null) ?: EMetronomeSounds.Clave.toString())
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putString(METRONOME_CLICK, value.toString())
			editor.apply()
		}

	// Number of beats per bar
	var beatsPerBar:Int
		get()
		{
			return mSettings.getInt(BEATS_PER_BAR, 4)
		}
		set(value)
		{
			val bpb = when
			{
				value < 1 -> 1
				value > 7 -> 7
				else -> value
			}
			val editor = mSettings.edit()
			editor.putInt(BEATS_PER_BAR, bpb)
			editor.apply()
		}

	// The number of bars to hold each chord for
	var barsPerChord:Int
		get()
		{
			return mSettings.getInt(BARS_PER_CHORD, 1)
		}
		set(value)
		{
			val bpc = when
			{
				value < 1 -> 1
				value > 8 -> 8
				else -> value
			}
			val editor = mSettings.edit()
			editor.putInt(BARS_PER_CHORD, bpc)
			editor.apply()
		}

	// The user's theme preference
	var theme:EThemes
		get()
		{
			return EThemes.valueOf(mSettings.getString(THEME, null) ?: EThemes.System.toString())
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putString(THEME, value.toString())
			editor.apply()
		}
}