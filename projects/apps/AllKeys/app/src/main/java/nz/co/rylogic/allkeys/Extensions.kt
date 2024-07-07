package nz.co.rylogic.allkeys

import android.app.Activity
import android.content.Context
import android.content.res.AssetManager
import android.os.Debug
import androidx.appcompat.app.AppCompatDelegate
import androidx.preference.MultiSelectListPreference
import java.io.BufferedReader
import java.io.File
import java.io.FileOutputStream
import java.io.InputStreamReader

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
	val settings = (this.application as AllKeysApp).settings
	when  (settings.theme) {
		EThemes.System -> AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_FOLLOW_SYSTEM)
		EThemes.Light -> AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_NO)
		EThemes.Dark -> AppCompatDelegate.setDefaultNightMode(AppCompatDelegate.MODE_NIGHT_YES)
	}
}

// Access the app settings
val Context.settings: Settings
	get() = (this.applicationContext as AllKeysApp).settings

// Check if an asset exists
fun AssetManager.exists(assetName: String): Boolean
{
	return try
	{
		this.open(assetName).close()
		true
	}
	catch (e: Exception)
	{
		false
	}
}

// Copy an asset to local storage (if not done already) and return its File object
fun Context.localFile(assetName: String): File
{
	val file = File(this.filesDir, assetName)
	if (!file.exists() || Debug.isDebuggerConnected())
	{
		this.assets.open(assetName).use { input ->
			FileOutputStream(file).use { output ->
				input.copyTo(output)
			}
		}
	}
	return file
}

// Read an asset file to a string
fun Context.readAssetFileToString(fileName: String): String
{
	val inputStream = this.assets.open(fileName)
	val bufferedReader = BufferedReader(InputStreamReader(inputStream))
	return bufferedReader.use { it.readText() }
}

// Map a list of notes to a range, keeping the relative intervals
// 'notes' are a list of chromatic intervals from the root note
fun mapToRange(root: Short, notes: List<Int>, range: IntRange): List<Short>
{
	var shift = 0
	val n = root + notes.first()
	while (n + shift < range.first) { shift += 12 }
	while (n + shift >= range.last) { shift -= 12 }
	return notes.map { (root + it + shift).toShort() }
}
