package nz.co.rylogic.allkeys

import android.os.Bundle
import androidx.preference.MultiSelectListPreference
import androidx.preference.PreferenceFragmentCompat

// A simple [Fragment] subclass as the second destination in the navigation.
class FragmentSettings : PreferenceFragmentCompat()
{
	override fun onCreatePreferences(savedInstanceState: Bundle?, rootKey: String?)
	{
		// Create the settings layout from the resources
		setPreferencesFromResource(R.xml.settings, rootKey)

		// Update the summary from the selected setting
		val keys = findPreference<MultiSelectListPreference>("selected_keys")
		if (keys != null)
		{
			keys.setSummaryFrom(keys.values as HashSet<String>, "\n")
			keys.setOnPreferenceChangeListener { preference, newValue: Any? ->
				@Suppress("UNCHECKED_CAST")
				(preference as MultiSelectListPreference).setSummaryFrom(newValue as? HashSet<String>, "\n")
				true
			}
		}

		// Update the summary from the selected setting
		val chords = findPreference<MultiSelectListPreference>("selected_chords_std")
		if (chords != null)
		{
			chords.setSummaryFrom(chords.values as HashSet<String>, " ")
			chords.setOnPreferenceChangeListener { preference, newValue: Any? ->
				@Suppress("UNCHECKED_CAST")
				(preference as MultiSelectListPreference).setSummaryFrom(newValue as? HashSet<String>, " ")
				true
			}
		}
	}
}
