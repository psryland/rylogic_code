package nz.co.rylogic.allkeys

import android.content.Context
import android.content.SharedPreferences
import android.os.Bundle
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import androidx.preference.MultiSelectListPreference
import androidx.preference.PreferenceFragmentCompat
import androidx.preference.PreferenceManager

// A simple [Fragment] subclass as the second destination in the navigation.
class FragmentSettings : PreferenceFragmentCompat(), SharedPreferences.OnSharedPreferenceChangeListener
{
	override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View
	{
		var view = super.onCreateView(inflater, container, savedInstanceState)
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
		setPreferencesFromResource(R.xml.settings, rootKey)

		var settings = Settings(context as Context)

		// Update the summary from the selected setting
		val keys = findPreference<MultiSelectListPreference>(Settings.SELECTED_KEYS) as MultiSelectListPreference
		keys.setSummaryFrom(settings.keyGroupsSelected.toHashSet(), "\n")

		// Update the summary from the selected setting
		val chords = findPreference<MultiSelectListPreference>(Settings.SELECTED_CHORDS) as MultiSelectListPreference
		chords.entries = settings.chordsAll
		chords.entryValues = settings.chordsAll
		chords.setSummaryFrom(settings.chordsSelected.toHashSet(), " ")
	}

	override fun onSharedPreferenceChanged(prefs: SharedPreferences?, key: String?)
	{
		when (key)
		{
			Settings.SELECTED_KEYS ->
			{
				val keys = findPreference<MultiSelectListPreference>(Settings.SELECTED_KEYS) as MultiSelectListPreference
				keys.setSummaryFrom(keys.values as HashSet<String>, "\n")
			}
			Settings.SELECTED_CHORDS ->
			{
				val chords = findPreference<MultiSelectListPreference>(Settings.SELECTED_CHORDS) as MultiSelectListPreference
				chords.setSummaryFrom(chords.values as HashSet<String>, " ")
			}
			Settings.CUSTOM_CHORDS ->
			{
				var settings = Settings(context as Context)
				val chords = findPreference<MultiSelectListPreference>(Settings.SELECTED_CHORDS) as MultiSelectListPreference
				chords.entries = settings.chordsAll
				chords.entryValues = settings.chordsAll
			}
			else ->
			{
			}
		}
	}
}
