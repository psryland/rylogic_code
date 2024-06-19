package nz.co.rylogic.allkeys

import android.content.Context
import android.content.SharedPreferences
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.preference.DropDownPreference
import androidx.preference.ListPreference
import androidx.preference.MultiSelectListPreference
import androidx.preference.PreferenceFragmentCompat
import androidx.preference.PreferenceManager
import androidx.preference.SeekBarPreference

// A simple [Fragment] subclass as the second destination in the navigation.
class FragmentSettings : PreferenceFragmentCompat(), SharedPreferences.OnSharedPreferenceChangeListener
{
	private lateinit var mSettings: Settings

	override fun onCreate(savedInstanceState: Bundle?)
	{
		mSettings = Settings(context as Context)
		super.onCreate(savedInstanceState)
	}

	override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View
	{
		val view = super.onCreateView(inflater, container, savedInstanceState)
		PreferenceManager.getDefaultSharedPreferences(context as Context).registerOnSharedPreferenceChangeListener(this)
		return view
	}

	override fun onDestroyView()
	{
		PreferenceManager.getDefaultSharedPreferences(context as Context).unregisterOnSharedPreferenceChangeListener(this)
		super.onDestroyView()
	}

	override fun onCreatePreferences(savedInstanceState: Bundle?, rootKey: String?)
	{
		// Create the settings layout from the resources
		setPreferencesFromResource(R.xml.settings_ui, rootKey)

		// Initialize the controls
		val keys = listOf(
			Settings.SELECTED_KEYS,
			Settings.SELECTED_CHORDS,
			Settings.CUSTOM_CHORDS,
			Settings.ROOT_NOTE_SOUNDS,
			Settings.ROOT_NOTE_VOLUME,
			Settings.ROOT_NOTE_INSTRUMENT,
			Settings.METRONOME_SOUNDS,
			Settings.METRONOME_VOLUME,
			Settings.METRONOME_ACCENT,
			Settings.METRONOME_CLICK,
			Settings.BEATS_PER_BAR,
			Settings.BARS_PER_CHORD,
		)
		for (key in keys)
			onSharedPreferenceChanged(null, key)
	}

	override fun onSharedPreferenceChanged(prefs: SharedPreferences?, key: String?)
	{
		when (key)
		{
			Settings.SELECTED_KEYS ->
			{
				findPreference<MultiSelectListPreference>(Settings.SELECTED_KEYS)?.setSummaryFrom(mSettings.keyGroupsSelected.toHashSet(), "\n")
			}
			Settings.SELECTED_CHORDS ->
			{
				findPreference<MultiSelectListPreference>(Settings.SELECTED_CHORDS)?.setSummaryFrom(mSettings.chordsSelected.toHashSet(), " ")
			}
			Settings.CUSTOM_CHORDS ->
			{
				val chords = findPreference<MultiSelectListPreference>(Settings.SELECTED_CHORDS)!!
				chords.entries = mSettings.chordsAll
				chords.entryValues = mSettings.chordsAll
				chords.setSummaryFrom(mSettings.chordsSelected.toHashSet(), " ")
			}
			Settings.ROOT_NOTE_SOUNDS ->
			{
				findPreference<SeekBarPreference>(Settings.ROOT_NOTE_VOLUME)?.isVisible = mSettings.rootNoteSounds
				findPreference<DropDownPreference>(Settings.ROOT_NOTE_INSTRUMENT)?.isVisible = mSettings.rootNoteSounds
			}
			Settings.METRONOME_SOUNDS ->
			{
				findPreference<SeekBarPreference>(Settings.METRONOME_VOLUME)?.isVisible = mSettings.metronomeSounds
				findPreference<ListPreference>(Settings.METRONOME_ACCENT)?.isVisible = mSettings.metronomeSounds
				findPreference<ListPreference>(Settings.METRONOME_CLICK)?.isVisible = mSettings.metronomeSounds
			}
			Settings.BEATS_PER_BAR ->
			{
				findPreference<SeekBarPreference>(Settings.BEATS_PER_BAR)?.setSummary(mSettings.beatsPerBar.toString())
			}
			Settings.BARS_PER_CHORD ->
			{
				findPreference<SeekBarPreference>(Settings.BARS_PER_CHORD)?.setSummary(mSettings.barsPerChord.toString())
			}
			Settings.THEME ->
			{
				activity?.recreate()
			}
			else ->
			{
			}
		}
	}
}
