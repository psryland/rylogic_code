package nz.co.rylogic.allkeys

import androidx.preference.MultiSelectListPreference

// Split strings and remove empty values
fun CharSequence.splitIgnoreEmpty(vararg delimiters: String): List<String>
{
	return this
		.split(*delimiters)
		.filter { it.isNotEmpty() }
}

// Set the summary for a MultiSelectListPreference
fun MultiSelectListPreference.setSummaryFrom(values:HashSet<String>?, sep:String)
{
	this.summary = (values ?: this.values)
		.map { this.findIndexOfValue(it) }
		.filter { it != -1 }
		.joinToString(sep) { this.entries[it] }
}