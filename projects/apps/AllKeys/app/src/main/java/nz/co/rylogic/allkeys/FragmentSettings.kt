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
import androidx.preference.SwitchPreference

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
		for (field in mSettings.fields)
			onSharedPreferenceChanged(null, field)
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
				val control = findPreference<MultiSelectListPreference>(Settings.SELECTED_CHORDS)!!
				control.entries = mSettings.chordsAll
				control.entryValues = mSettings.chordsAll
				control.setSummaryFrom(mSettings.chordsSelected.toHashSet(), " ")
			}

			Settings.NEXT_CHORD_MODE ->
			{
				val control = findPreference<DropDownPreference>(Settings.NEXT_CHORD_MODE)!!
				control.entryValues = ENextChordMode.values().map { it.name }.toTypedArray()
			}

			Settings.ROOT_NOTE_SOUNDS ->
			{
				findPreference<SeekBarPreference>(Settings.ROOT_NOTE_VOLUME)?.isVisible = mSettings.rootNoteSounds
				findPreference<DropDownPreference>(Settings.ROOT_NOTE_INSTRUMENT)?.isVisible = mSettings.rootNoteSounds
				findPreference<SwitchPreference>(Settings.ROOT_NOTE_WALKING)?.isVisible = mSettings.rootNoteSounds
			}

			Settings.ROOT_NOTE_INSTRUMENT ->
			{
				val control = findPreference<DropDownPreference>(Settings.ROOT_NOTE_INSTRUMENT)!!
				control.entryValues = EInstrument.values().map { it.name }.toTypedArray()
			}
			Settings.METRONOME_SOUNDS ->
			{
				findPreference<SeekBarPreference>(Settings.METRONOME_VOLUME)?.isVisible = mSettings.metronomeSounds
				findPreference<ListPreference>(Settings.METRONOME_ACCENT)?.isVisible = mSettings.metronomeSounds
				findPreference<ListPreference>(Settings.METRONOME_CLICK)?.isVisible = mSettings.metronomeSounds
			}
			Settings.METRONOME_ACCENT ->
			{
				val control = findPreference<DropDownPreference>(Settings.METRONOME_ACCENT)!!
				control.entryValues = EMetronomeSounds.values().map { it.name }.toTypedArray()
			}
			Settings.METRONOME_CLICK ->
			{
				val control = findPreference<DropDownPreference>(Settings.METRONOME_CLICK)!!
				control.entryValues = EMetronomeSounds.values().map { it.name }.toTypedArray()
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
				val control = findPreference<DropDownPreference>(Settings.THEME)!!
				control.entryValues = EThemes.values().map { it.name }.toTypedArray()
				if (prefs != null)
					activity?.recreate()
			}
			else ->
			{
			}
		}
	}
}
