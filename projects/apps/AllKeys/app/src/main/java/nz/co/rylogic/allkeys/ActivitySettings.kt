package nz.co.rylogic.allkeys

import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.ViewCompat
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.updatePadding

class ActivitySettings : AppCompatActivity()
{
	override fun onCreate(savedInstanceState: Bundle?)
	{
		setThemeFromSettings()
		super.onCreate(savedInstanceState)

		// Enable edge-to-edge display
		WindowCompat.setDecorFitsSystemWindows(window, false)

		setContentView(R.layout.activity_settings)

		// Adjust size to account for system bars
		ViewCompat.setOnApplyWindowInsetsListener(findViewById<View>(R.id.settings_layout)) { view, windowInsets ->
			val insets = windowInsets.getInsets(WindowInsetsCompat.Type.systemBars())

			// Apply the insets as padding to the view.
			view.updatePadding(
				left = insets.left,
				top = insets.top,
				right = insets.right,
				bottom = insets.bottom
			)

			// Return CONSUMED if you've handled the insets
			WindowInsetsCompat.CONSUMED
		}

		setSupportActionBar(findViewById(R.id.toolbar))
		supportActionBar?.setDisplayHomeAsUpEnabled(true)

		if (savedInstanceState == null)
		{
			val app: AllKeysApp = application as AllKeysApp
			supportFragmentManager
				.beginTransaction()
				.replace(R.id.fragment_settings, FragmentSettings())
				.commit()
		}
	}

	override fun onSupportNavigateUp(): Boolean
	{
		finish()
		return true
	}
}