package nz.co.rylogic.allkeys

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity

class ActivitySettings : AppCompatActivity()
{
	override fun onCreate(savedInstanceState: Bundle?)
	{
		setThemeFromSettings()
		super.onCreate(savedInstanceState)
		setContentView(R.layout.activity_settings)
		setSupportActionBar(findViewById(R.id.toolbar))
		supportActionBar?.setDisplayHomeAsUpEnabled(true)

		if (savedInstanceState == null)
		{
			supportFragmentManager
				.beginTransaction()
				.replace(R.id.settings, FragmentSettings())
				.commit()
		}
	}

	override fun onSupportNavigateUp(): Boolean
	{
		finish()
		return true
	}
}