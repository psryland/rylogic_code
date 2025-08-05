package nz.co.rylogic.allkeys

import android.content.Intent
import android.os.Bundle
import android.view.View
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import androidx.core.view.ViewCompat
import androidx.core.view.WindowCompat
import androidx.core.view.WindowInsetsCompat
import androidx.core.view.updatePadding
import com.google.android.material.floatingactionbutton.FloatingActionButton
import kotlin.system.exitProcess

class ActivityMain : AppCompatActivity()
{
	override fun onCreate(savedInstanceState: Bundle?)
	{
		setThemeFromSettings()
		super.onCreate(savedInstanceState)

		// Enable edge-to-edge display
		WindowCompat.setDecorFitsSystemWindows(window, false)

		setContentView(R.layout.activity_main)

		// Adjust size to account for system bars
		ViewCompat.setOnApplyWindowInsetsListener(findViewById<View>(R.id.main_layout)) { view, windowInsets ->
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

		// Navigate to the settings fragment
		findViewById<FloatingActionButton>(R.id.fab_menu).setOnClickListener {
			startActivity(Intent(this, ActivitySettings::class.java))
		}

		if (savedInstanceState == null)
		{
			val app: AllKeysApp = application as AllKeysApp
			supportFragmentManager
				.beginTransaction()
				.replace(R.id.fragment_main, FragmentMain())
				.commit()


			if (app.settings.genresAll.isEmpty())
			{
				val dlg = AlertDialog.Builder(this)
					.setTitle("No valid genres found")
					.setMessage("No valid genres were found in '${app.settings.genreDirectory}'. Please reinstall.")
					.setPositiveButton("OK") { dialog, _ ->
						dialog.dismiss()
						exitProcess(1)
					}
					.create()

				dlg.setCancelable(false)
				dlg.setCanceledOnTouchOutside(false)
				dlg.show()
			}

		}
	}
}