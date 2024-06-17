package nz.co.rylogic.allkeys

import android.content.Context
import android.util.AttributeSet
import androidx.preference.SeekBarPreference

class SeekBarPreferenceRange(context: Context, attrs: AttributeSet) : SeekBarPreference(context, attrs)
{
	private val mMin = attrs.getAttributeIntValue("http://schemas.android.com/apk/res/android", "min", 0)
	private val mMax = attrs.getAttributeIntValue("http://schemas.android.com/apk/res/android", "max", 1)

	init
	{
		max = mMax
		min = mMin
	}
}
