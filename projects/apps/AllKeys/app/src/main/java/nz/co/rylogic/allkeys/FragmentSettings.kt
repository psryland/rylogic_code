package nz.co.rylogic.allkeys

import android.content.Context
import android.content.SharedPreferences
import android.os.Bundle
import android.webkit.WebView
import androidx.appcompat.app.AlertDialog
import androidx.preference.DropDownPreference
import androidx.preference.ListPreference
import androidx.preference.MultiSelectListPreference
import androidx.preference.Preference
import androidx.preference.PreferenceFragmentCompat
import androidx.preference.PreferenceManager
import androidx.preference.SeekBarPreference
import androidx.preference.SwitchPreference

// A simple [Fragment] subclass as the second destination in the navigation.
class FragmentSettings : PreferenceFragmentCompat(), SharedPreferences.OnSharedPreferenceChangeListener
{
	private lateinit var mContext: Context
	private lateinit var mSettings: Settings

	override fun onCreate(savedInstanceState: Bundle?)
	{
		mContext = context as Context
		mSettings = mContext.settings
		super.onCreate(savedInstanceState)
		PreferenceManager.getDefaultSharedPreferences(mContext).registerOnSharedPreferenceChangeListener(this)
	}

	override fun onDestroy()
	{
		PreferenceManager.getDefaultSharedPreferences(mContext).unregisterOnSharedPreferenceChangeListener(this)
		super.onDestroy()
	}

	override fun onCreatePreferences(savedInstanceState: Bundle?, rootKey: String?)
	{
		// Create the settings layout from the resources
		setPreferencesFromResource(R.xml.settings_ui, rootKey)

		// Initialize the controls
		for (field in mSettings.fields)
			onSharedPreferenceChanged(null, field)

		// Add click listener for credits
		val credits = findPreference<Preference>("credits")!!
		credits.setOnPreferenceClickListener {
			val builder = AlertDialog.Builder(mContext, R.style.Theme_AllKeys_Dialog)
			builder.setTitle("Credits")
			builder.setView(R.layout.credits)
			builder.setPositiveButton("OK") { dialog, _ -> dialog.dismiss() }
			builder.create().also { dialog ->
				dialog.show()
				val wb = dialog.findViewById<WebView>(R.id.webView)
				wb?.loadDataWithBaseURL(null, mContext.readAssetFileToString("credits.html"), "text/html", "utf-8", null)
			}
			true
		}
	}

	override fun onSharedPreferenceChanged(prefs: SharedPreferences?, key: String?)
	{
		when (key)
		{
			Settings.GENRE ->
			{
				val control = findPreference<ListPreference>(Settings.GENRE)!!
				control.entries = mSettings.genresAll.toTypedArray()
				control.entryValues = mSettings.genresAll.toTypedArray()
			}
			Settings.SELECTED_KEYS ->
			{
				val control = findPreference<MultiSelectListPreference>(Settings.SELECTED_KEYS)!!
				control.entries = mSettings.keyGroupsAll.toTypedArray()
				control.entryValues = mSettings.keyGroupsAll.toTypedArray()
				control.setSummaryFrom(mSettings.keyGroupsSelected.toHashSet(), "\n")
			}
			Settings.SELECTED_CHORDS ->
			{
				val control = findPreference<MultiSelectListPreference>(Settings.SELECTED_CHORDS)!!
				control.entries = mSettings.chordsAll.toTypedArray()
				control.entryValues = mSettings.chordsAll.toTypedArray()
				control.setSummaryFrom(mSettings.chordsSelected.toHashSet(), " ")
			}
			Settings.NEXT_CHORD_MODE ->
			{
				val control = findPreference<DropDownPreference>(Settings.NEXT_CHORD_MODE)!!
				control.entryValues = ENextChordMode.values().map { it.name }.toTypedArray()
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
			Settings.ROOT_NOTE_SOUNDS ->
			{
				findPreference<SeekBarPreference>(Settings.ROOT_NOTE_VOLUME)?.isVisible = mSettings.rootNoteSounds
				findPreference<DropDownPreference>(Settings.ROOT_NOTE_INSTRUMENT)?.isVisible = mSettings.rootNoteSounds
				findPreference<SwitchPreference>(Settings.ROOT_NOTE_WALKING)?.isVisible = mSettings.rootNoteSounds
			}
			Settings.ROOT_NOTE_INSTRUMENT ->
			{
				val control = findPreference<DropDownPreference>(Settings.ROOT_NOTE_INSTRUMENT)!!
				control.entryValues = ERootNoteInstruments.values().map { it.name }.toTypedArray()
			}
			Settings.CHORD_COMP_SOUNDS ->
			{
				findPreference<SeekBarPreference>(Settings.CHORD_COMP_VOLUME)?.isVisible = mSettings.chordCompSounds
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
