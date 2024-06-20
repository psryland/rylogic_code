package nz.co.rylogic.allkeys

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import com.google.android.material.floatingactionbutton.FloatingActionButton

class ActivityMain : AppCompatActivity()
{
	override fun onCreate(savedInstanceState: Bundle?)
	{
		setThemeFromSettings()
		super.onCreate(savedInstanceState)
		setContentView(R.layout.activity_main)

		// Navigate to the settings fragment
		findViewById<FloatingActionButton>(R.id.fab_menu).setOnClickListener {
			startActivity(Intent(this, ActivitySettings::class.java))
		}

		if (savedInstanceState == null)
		{
			supportFragmentManager
				.beginTransaction()
				.replace(R.id.fragment_main, FragmentMain())
				.commit()
		}
	}
}