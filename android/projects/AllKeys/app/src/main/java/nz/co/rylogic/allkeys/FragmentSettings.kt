package nz.co.rylogic.allkeys

import android.content.Context
import android.content.SharedPreferences
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.preference.ListPreference
import androidx.preference.MultiSelectListPreference
import androidx.preference.PreferenceCategory
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

		// Initialize the enabled states
		val metronomeAccent = findPreference<ListPreference>(Settings.METRONOME_ACCENT) as ListPreference
		metronomeAccent.isEnabled = mSettings.metronomeSounds
		val metronomeClick = findPreference<ListPreference>(Settings.METRONOME_CLICK) as ListPreference
		metronomeClick.isEnabled = mSettings.metronomeSounds

		// Initialize the summary from the selected setting
		val keys = findPreference<MultiSelectListPreference>(Settings.SELECTED_KEYS) as MultiSelectListPreference
		keys.setSummaryFrom(mSettings.keyGroupsSelected.toHashSet(), "\n")

		// Initialize the summary from the selected setting
		val chords = findPreference<MultiSelectListPreference>(Settings.SELECTED_CHORDS) as MultiSelectListPreference
		chords.setSummaryFrom(mSettings.chordsSelected.toHashSet(), " ")
		chords.entryValues = mSettings.chordsAll
		chords.entries = mSettings.chordsAll

		// Initialize the summary
		val bpb = findPreference<SeekBarPreference>(Settings.BEATS_PER_BAR) as SeekBarPreference
		bpb.setSummary(mSettings.beatsPerBar.toString())
		val bpc = findPreference<SeekBarPreference>(Settings.BARS_PER_CHORD) as SeekBarPreference
		bpc.setSummary(mSettings.barsPerChord.toString())
	}

	override fun onSharedPreferenceChanged(prefs: SharedPreferences?, key: String?)
	{
		when (key)
		{
			Settings.SELECTED_KEYS ->
			{
				val keys = findPreference<MultiSelectListPreference>(Settings.SELECTED_KEYS) as MultiSelectListPreference
				keys.setSummaryFrom(mSettings.keyGroupsSelected.toHashSet(), "\n")
			}
			Settings.SELECTED_CHORDS ->
			{
				val chords = findPreference<MultiSelectListPreference>(Settings.SELECTED_CHORDS) as MultiSelectListPreference
				chords.setSummaryFrom(mSettings.chordsSelected.toHashSet(), " ")
			}
			Settings.CUSTOM_CHORDS ->
			{
				val chords = findPreference<MultiSelectListPreference>(Settings.SELECTED_CHORDS) as MultiSelectListPreference
				chords.entries = mSettings.chordsAll
				chords.entryValues = mSettings.chordsAll
			}
			Settings.METRONOME_SOUNDS ->
			{
				val metronomeAccent = findPreference<ListPreference>(Settings.METRONOME_ACCENT) as ListPreference
				metronomeAccent.isEnabled = mSettings.metronomeSounds
				val metronomeClick = findPreference<ListPreference>(Settings.METRONOME_CLICK) as ListPreference
				metronomeClick.isEnabled = mSettings.metronomeSounds
			}
			Settings.BEATS_PER_BAR ->
			{
				val beatsPerBar = findPreference<SeekBarPreference>(Settings.BEATS_PER_BAR) as SeekBarPreference
				beatsPerBar.setSummary(mSettings.beatsPerBar.toString())
			}
			Settings.BARS_PER_CHORD ->
			{
				val barsPerChord = findPreference<SeekBarPreference>(Settings.BARS_PER_CHORD) as SeekBarPreference
				barsPerChord.setSummary(mSettings.barsPerChord.toString())
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
