package nz.co.rylogic.allkeys

import android.app.Activity
import androidx.appcompat.app.AppCompatDelegate
import androidx.preference.MultiSelectListPreference

// Split strings and remove empty values
fun CharSequence.splitIgnoreEmpty(vararg delimiters: String): List<String>
{
	return this
		.split(*delimiters)
		.filter { it.isNotEmpty() }
}

// Set the summary for a MultiSelectListPreference
fun MultiSelectListPreference.setSummaryFrom(values:HashSet<String>, sep:String)
{
	this.summary = values
		.map { this.findIndexOfValue(it) }
		.filter { it != -1 }
		.joinToString(sep) { this.entries[it] }
}

// Set the activity theme based on the user's settings
fun Activity.setThemeFromSettings()
{
	val settings = Settings(this)
	when  (settings.theme) {
		EThemes.System -> AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_FOLLOW_SYSTEM)
		EThemes.Light -> AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_NO)
		EThemes.Dark -> AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES)
	}
}