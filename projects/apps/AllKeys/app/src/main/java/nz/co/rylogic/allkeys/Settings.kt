package nz.co.rylogic.allkeys

import android.content.Context
import android.content.SharedPreferences
import android.util.Log
import android.widget.Toast
import androidx.preference.PreferenceManager
import com.google.gson.GsonBuilder
import com.google.gson.JsonDeserializationContext
import com.google.gson.JsonDeserializer
import com.google.gson.JsonElement
import com.google.gson.annotations.SerializedName
import com.google.gson.reflect.TypeToken
import java.io.File
import java.lang.reflect.Type
import kotlin.math.absoluteValue

class Settings(context: Context) : SharedPreferences.OnSharedPreferenceChangeListener
{
	companion object
	{
		// These constants are used to match the preference controls to saved values
		const val GENRE = "genre"
		const val SELECTED_KEYS = "selected_keys"
		const val SELECTED_CHORDS = "selected_chords"
		const val NEXT_CHORD_MODE = "next_chord_mode"
		const val METRONOME_SOUNDS = "metronome_sounds"
		const val METRONOME_VOLUME = "metronome_volume"
		const val METRONOME_ACCENT = "metronome_accent"
		const val METRONOME_CLICK = "metronome_click"
		const val ROOT_NOTE_SOUNDS = "root_note_sounds"
		const val ROOT_NOTE_VOLUME = "root_note_volume"
		const val ROOT_NOTE_INSTRUMENT = "root_note_instrument"
		const val ROOT_NOTE_WALKING = "root_note_walking"
		const val CHORD_COMP_SOUNDS = "chord_comp_sounds"
		const val CHORD_COMP_VOLUME = "chord_comp_volume"
		const val BEATS_PER_BAR = "beats_per_bar"
		const val BARS_PER_CHORD = "bars_per_chord"
		const val TEMPO = "tempo"
		const val THEME = "theme"
	}

	private val mSettings: SharedPreferences = PreferenceManager.getDefaultSharedPreferences(context)
	private val mKeyGroups = context.resources.getStringArray(R.array.key_groups)
	private val mGenres: MutableList<Genre> = mutableListOf()

	init
	{
		mSettings.registerOnSharedPreferenceChangeListener(this)
		val genreDir = "genres"

		// Ensure that all embedded 'genres' are copied to the app's 'genres' directory
		val genresDir = File(context.filesDir, genreDir)
		if (!genresDir.exists())
		{
			genresDir.mkdirs()
		}
		if (!genresDir.isDirectory)
		{
			throw Exception("Genres directory is not a directory")
		}
		for (file in context.assets.list(genreDir) ?: arrayOf())
		{
			context.localFile("$genreDir/$file")
		}

		// Create a json parser
		val gson = GsonBuilder()
			.registerTypeAdapter(WalkStep::class.java, WalkStepDeserializer())
			.registerTypeAdapter(VampStep::class.java, VampStepDeserializer())
			.create()

		// Get the type name that each file should map to
		val dataType = object : TypeToken<Genre>()
		{}

		// Search the 'genres' directory for genre json files
		for (file in genresDir.walkTopDown())
		{
			if (!file.isFile || (file.extension != "json" && file.extension != "jsonc"))
				continue

			try
			{
				val json = file.readText()
				val genre = gson.fromJson(json, dataType).validate(context)
				mGenres.add(genre)
			}
			catch (e: Exception)
			{
				// Add a logcat message
				Log.e("Settings", "Failed to parse genre file: ${file.name}", e)

				// Add a toast message
				Toast.makeText(context, "Failed to parse genre file: ${file.name}", Toast.LENGTH_SHORT).show()
			}
		}

		// This is needed because the settings UI accesses the 'SharedPreferences' object directly.
		// We need to ensure all settings exist or the Preferences stuff will crash.
		val editor = mSettings.edit()
		editor.putString(GENRE, genreSelected)
		editor.putStringSet(SELECTED_KEYS, keyGroupsSelected)
		editor.putStringSet(SELECTED_CHORDS, chordsSelected)
		editor.putString(NEXT_CHORD_MODE, nextChordMode.toString())
		editor.putBoolean(CHORD_COMP_SOUNDS, chordCompSounds)
		editor.putInt(CHORD_COMP_VOLUME, (chordCompVolume * 100).toInt())
		editor.putBoolean(ROOT_NOTE_SOUNDS, rootNoteSounds)
		editor.putInt(ROOT_NOTE_VOLUME, (rootNoteVolume * 100).toInt())
		editor.putString(ROOT_NOTE_INSTRUMENT, rootNoteInstrument.toString())
		editor.putBoolean(ROOT_NOTE_WALKING, rootNoteWalking)
		editor.putBoolean(METRONOME_SOUNDS, metronomeSounds)
		editor.putInt(METRONOME_VOLUME, (metronomeVolume * 100).toInt())
		editor.putString(METRONOME_ACCENT, metronomeAccent.toString())
		editor.putString(METRONOME_CLICK, metronomeClick.toString())
		editor.putInt(BEATS_PER_BAR, beatsPerBar)
		editor.putInt(BARS_PER_CHORD, barsPerChord)
		editor.putInt(TEMPO, tempo)
		editor.putString(THEME, theme.toString())
		editor.apply()
	}

	// Unregister the listener
	fun close()
	{
		mSettings.unregisterOnSharedPreferenceChangeListener(this)
	}

	// Handle settings self-consistency
	override fun onSharedPreferenceChanged(prefs: SharedPreferences?, key: String?)
	{
		when (key)
		{
			GENRE ->
			{
				// Remove chords that aren't in this genre
				val available = chordsAll.toSet()
				chordsSelected = chordsSelected.filter { available.contains(it) }.toSet()

				// If there are no selected chords, set defaults
				if (chordsSelected.isEmpty())
					chordsSelected = chordsAll.take(3).toSet()
			}
			else ->
			{
			}
		}
	}

	// Map from key to MIDI root notes
	val keyRootNotes: Map<String, Short> = mapOf(
		"G" to 31, "G♯" to 32, "A♭" to 32, "A" to 33, "A♯" to 34, "B♭" to 34, "B" to 35, "C♭" to 35,
		"C" to 36, "B♯" to 36, "C♯" to 37, "D♭" to 37, "D" to 38, "D♯" to 39, "E♭" to 39, "E" to 40,
		"F♭" to 40, "F" to 41, "E♯" to 41, "F♯" to 42, "G♭" to 42,
	)
	fun midiKeyToNote(key: Short, flats: Boolean = true): String
	{
		// C-1 = 0, C4 = 60
		val note = key % 12
		val octave = (key / 12) - 1
		val noteNamesSharps = arrayOf("C", "C♯", "D", "D♯", "E", "F", "F♯", "G", "G♯", "A", "A♯", "B")
		val noteNamesFlats = arrayOf("C", "D♭", "D", "E♭", "E", "F", "G♭", "G", "A♭", "A", "B♭", "B")
		val noteName = if (flats) noteNamesFlats[note] else noteNamesSharps[note]
		return "${noteName}${octave}"
	}

	// Return the settings field names
	val fields: Set<String>
		get() = mSettings.all.keys

	// Return all available genre names
	val genresAll: List<String>
		get() = mGenres.map { it.name }

	// The selected playback genre
	var genreSelected: String
		get() = mSettings.getString(GENRE, null) ?: mGenres.firstOrNull()?.name ?: "Swing"
		set(value)
		{
			val editor = mSettings.edit()
			editor.putString(GENRE, value)
			editor.apply()
		}

	// Return the playback genre
	val genre: Genre
		get() = mGenres.find { it.name.equals(genreSelected, true) } ?: Genre()

	// All available key groups
	val keyGroupsAll: List<String>
		get() = mKeyGroups.toList()

	// The list of selected key groups
	var keyGroupsSelected: Set<String>
		get() = mSettings.getStringSet(SELECTED_KEYS, null) ?:setOf(mKeyGroups.first())
		set(value)
		{
			val editor = mSettings.edit()
			editor.putStringSet(SELECTED_KEYS, value)
			editor.apply()
		}

	// The list of individual key names
	val keys: List<String>
		get() = keyGroupsSelected
			.map { it.split(",") }.flatten()
			.map { it.trim() }
			.filter { it.isNotEmpty() }

	// The list of all chord names
	val chordsAll: List<String>
		get() = genre.chords.keys.toList()

	// The list of individual selected chords
	var chordsSelected: Set<String>
		get() = mSettings.getStringSet(SELECTED_CHORDS, null) ?: setOf()
		set(value)
		{
			val editor = mSettings.edit()
			editor.putStringSet(SELECTED_CHORDS, value)
			editor.apply()
		}

	// The list of individual chord names
	val chords: List<String>
		get() = chordsSelected
			.map { it.trim() }
			.filter { it.isNotEmpty() }

	// How to display the next chord
	var nextChordMode: ENextChordMode
		get() = ENextChordMode.valueOf(mSettings.getString(NEXT_CHORD_MODE, null) ?: ENextChordMode.BarBefore.toString())
		set(value)
		{
			val editor = mSettings.edit()
			editor.putString(NEXT_CHORD_MODE, value.toString())
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
	var metronomeVolume: Double
		get()
		{
			return mSettings.getInt(METRONOME_VOLUME, 80) * 0.01
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putInt(METRONOME_VOLUME, (value * 100).toInt())
			editor.apply()
		}

	// The accented click sound to use
	var metronomeAccent: EMetronomeSounds
		get()
		{
			return EMetronomeSounds.valueOf(mSettings.getString(METRONOME_ACCENT, null) ?: EMetronomeSounds.WoodblockHigh.toString())
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
			return EMetronomeSounds.valueOf(mSettings.getString(METRONOME_CLICK, null) ?: EMetronomeSounds.WoodblockMed.toString())
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
	var rootNoteVolume: Double
		get()
		{
			return mSettings.getInt(ROOT_NOTE_VOLUME, 80) * 0.01
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putInt(ROOT_NOTE_VOLUME, (value * 100).toInt())
			editor.apply()
		}

	// The instrument to use for the root notes
	var rootNoteInstrument:ERootNoteInstruments
		get()
		{
			return ERootNoteInstruments.valueOf(mSettings.getString(ROOT_NOTE_INSTRUMENT, null) ?: ERootNoteInstruments.AcousticBass.toString())
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
			return mSettings.getBoolean(ROOT_NOTE_WALKING, false)
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putBoolean(ROOT_NOTE_WALKING, value)
			editor.apply()
		}

	// True if chords accompaniment should play sounds
	var chordCompSounds:Boolean
		get()
		{
			return mSettings.getBoolean(CHORD_COMP_SOUNDS, false)
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putBoolean(CHORD_COMP_SOUNDS, value)
			editor.apply()
		}

	// Relative volume of the chords
	var chordCompVolume: Double
		get()
		{
			return mSettings.getInt(CHORD_COMP_VOLUME, 60) * 0.01
		}
		set(value)
		{
			val editor = mSettings.edit()
			editor.putInt(CHORD_COMP_VOLUME, (value * 100).toInt())
			editor.apply()
		}

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


data class Genre(
	// The name of this style
	val name: String = "",

	// Available scales
	var scales: Map<String, List<Int>> = mapOf(),

	// Available chords and associated voicings/scales
	var chords: Map<String, Chord> = mapOf(),

	// Walking bass line patterns
	var walks: Map<String, List<List<WalkStep>>> = mapOf(),

	// Chord vamp patterns
	var vamps: Map<String, List<List<VampStep>>> = mapOf(),

	// Drum loops
	var rhythms: List<DrumLoop> = listOf()
)
{
	// Check for invalid data
	fun validate(context: Context): Genre
	{
		for (scale in scales)
		{
			@Suppress("SENSELESS_COMPARISON")
			if (scale.value == null || scale.value.isEmpty())
				throw Exception("Empty scale in $name.scales[${scale.key}]")
			if (scale.value.any { it < 0 || it > 11 })
				throw Exception("Invalid scale degree in $name.scale[${scale.key}]")
		}
		for (walk in walks)
		{
			@Suppress("SENSELESS_COMPARISON")
			if (walk.value == null)
				throw Exception("Empty walk in $name.walks[${walk.key}]")

			for (step in walk.value.withIndex())
			{
				@Suppress("SENSELESS_COMPARISON")
				if (step.value == null || step.value.isEmpty())
					throw Exception("Empty walk in $name.walks[${walk.key}][${step.index}]")
				if (step.value.any { it.key != 'c' && it.key != 'n' && it.key != '_' })
					throw Exception("Invalid key in $name.walks[${walk.key}][${step.index}]")
				if (step.value.any { it.accidental < -1 || it.accidental > 1 })
					throw Exception("Invalid accidental in $name.walks[${walk.key}][${step.index}]")
				if (step.value.any { it.velocity < 0.0 || it.velocity > 1.0 })
					throw Exception("Invalid velocity in $name.walks[${walk.key}][${step.index}]")
			}
		}
		for (vamp in vamps)
		{
			@Suppress("SENSELESS_COMPARISON")
			if (vamp.value == null)
				throw Exception("Empty vamp in $name.vamps[${vamp.key}]")

			for (step in vamp.value.withIndex())
			{
				@Suppress("SENSELESS_COMPARISON")
				if (step.value == null || step.value.isEmpty())
					throw Exception("Empty vamp in $name.vamps[${vamp.key}][${step.index}]")
				if (step.value.any { it.key != 'c' && it.key != '_' })
					throw Exception("Invalid key in $name.vamps[${vamp.key}][${step.index}]")
				if (step.value.any { it.velocity < 0.0 || it.velocity > 1.0 })
					throw Exception("Invalid velocity in $name.vamps[${vamp.key}][${step.index}]")
			}
		}
		for (rhythm in rhythms.withIndex())
		{
			@Suppress("SENSELESS_COMPARISON")
			if (rhythm.value == null)
				throw Exception("Empty vamp in $name.vamps[${rhythm.index}]")
			if (rhythm.value.name.isEmpty())
				throw Exception("Empty name in $name.rhythms[${rhythm.index}]")
			if (rhythm.value.midi.isEmpty())
				throw Exception("Empty midi in $name.rhythms[${rhythm.value.name}]")
			if (!context.assets.exists(rhythm.value.midi) && !File(context.filesDir, rhythm.value.midi).exists())
				throw Exception("Midi file not found in $name.rhythms[${rhythm.value.name}]")
			if (rhythm.value.beatsPerBar < 1 || rhythm.value.beatsPerBar > 7)
				throw Exception("Invalid beats per bar in $name.rhythms[${rhythm.value.name}]")
			if (rhythm.value.numberOfBars < 1 || rhythm.value.numberOfBars > 8)
				throw Exception("Invalid number of bars in $name.rhythms[${rhythm.value.name}]")
		}
		return this
	}

	// Return a list of walk options for the given bar length
	fun walks(barLength: Int): List<List<WalkStep>>
	{
		var available = walks["$barLength-beat"]
		if (available == null)
		{
			fun isTransition(walk: List<WalkStep>): Boolean = walk.any { it.key == 'n' }
			available = when (barLength)
			{
				5 ->
				{
					val end = walks(2)
					val beg = walks(3).filter { !isTransition(it) }.shuffled()
					beg.map { it + end.random() }
				}
				6 ->
				{
					val end = walks(3)
					val beg = walks(3).filter { !isTransition(it) }.shuffled()
					beg.map { it + end.random() }
				}
				7 ->
				{
					val end = walks(3)
					val beg = walks(4).filter { !isTransition(it) }.shuffled()
					beg.map { it + end.random() }
				}
				else ->
				{
					listOf()
				}
			}
			walks = walks + ("$barLength-beat" to available)
		}
		return available
	}

	// Return a list of vamp options for the given bar length
	fun vamps(barLength: Int): List<List<VampStep>>
	{
		var available = vamps["$barLength-beat"]
		if (available == null)
		{
			available = when (barLength)
			{
				5 ->
				{
					val end = vamps(2)
					val beg = vamps(3).shuffled()
					beg.map { it + end.random() }
				}
				6 ->
				{
					val end = vamps(3)
					val beg = vamps(3).shuffled()
					beg.map { it + end.random() }
				}
				7 ->
				{
					val end = vamps(3)
					val beg = vamps(4).shuffled()
					beg.map { it + end.random() }
				}
				else ->
				{
					listOf()
				}
			}
			vamps = vamps + ("$barLength-beat" to available)
		}
		return available
	}
}

data class Chord(
	// Scales that can be used with the voicing
	val scales: List<String> = listOf(),

	// The LH/RH notes in this voicing
	val voicings: List<Voicing> = listOf(),
)

data class Voicing(
	// Name of the voiding
	val name: String = "",

	// Left hand notes
	val lh: List<Int> = listOf(),

	// Right hand notes
	val rh: List<Int> = listOf(),
)

data class WalkStep(
	// 'c' for current chord, 'n' for next chord, '_' for rest
	val key: Char,

	// Whether the scale degree is raised or lowered
	val accidental: Int,

	// The scale degree to play
	val scaleDegree: Int,

	// How long to play the note for (relative to a quarter note)
	val duration: Double,

	// How loud to play the note
	val velocity: Double
)

data class VampStep(
	// 'c' for current chord, '_' for rest
	val key: Char,

	// How long to play the chord for (relative to a quarter note)
	val duration: Double,

	// How loud to play the chord
	val velocity: Double
)

data class DrumLoop(
	// UI name for the loop
	val name: String = "",

	// The drum loop data
	val midi: String = "",

	// The time signature of the loop
	@SerializedName("beats-per-bar")
	val beatsPerBar: Int = 4,

	// The length of the loop in bars
	@SerializedName("number-of-bars")
	val numberOfBars: Int = 1,

	// True if the loop is swung
	val swung: Boolean = false
)

class WalkStepDeserializer : JsonDeserializer<WalkStep>
{
	override fun deserialize(json: JsonElement, typeOfT: Type, context: JsonDeserializationContext): WalkStep {
		return json.asString.split(":").let {
			// The "c1" part of the step
			val note: String = it[0]

			// Get the base key (either [c]current, [n]next, or [_]rest)
			val key: Char = note[0]

			// Is it raised or lowered?
			val accidental = when (note[1]) { 's' -> 1; 'b' -> -1; else -> 0 }

			// Get the scale degree from 'baseKey'
			val scaleDegree = note.substring(1 + accidental.absoluteValue).toInt()

			// The length of the note
			val duration = it[1].toDouble() * 0.01

			// The velocity of the note
			val velocity = it[2].toDouble() * 0.01

			WalkStep(key, accidental, scaleDegree, duration, velocity)
		}
	}
}
class VampStepDeserializer : JsonDeserializer<VampStep>
{
	override fun deserialize(json: JsonElement, typeOfT: Type, context: JsonDeserializationContext): VampStep {
		return json.asString.split(":").let {
			// The "c" part of the step
			val note: String = it[0]

			// Get the base key (either [c]current, or [_]rest)
			val key: Char = note[0]

			// The length of the chord
			val duration = it[1].toDouble() * 0.01

			// The velocity of the chord
			val velocity = it[2].toDouble() * 0.01

			VampStep(key, duration, velocity)
		}
	}
}
