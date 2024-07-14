package nz.co.rylogic.allkeys

import android.content.Intent
import android.os.Bundle
import androidx.appcompat.app.AlertDialog
import androidx.appcompat.app.AppCompatActivity
import com.google.android.material.floatingactionbutton.FloatingActionButton
import java.io.File
import kotlin.system.exitProcess

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