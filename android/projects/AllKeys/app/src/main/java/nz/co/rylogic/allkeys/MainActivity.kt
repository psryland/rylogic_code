package nz.co.rylogic.allkeys

import android.os.Bundle
import androidx.appcompat.app.AppCompatActivity
import androidx.navigation.findNavController
import androidx.navigation.ui.AppBarConfiguration
import androidx.navigation.ui.navigateUp
import android.view.Menu
import android.view.MenuItem
import androidx.navigation.NavController
import androidx.navigation.fragment.NavHostFragment
import androidx.navigation.ui.setupActionBarWithNavController
import androidx.preference.PreferenceManager
import nz.co.rylogic.allkeys.databinding.ActivityMainBinding
import java.util.Locale

class MainActivity : AppCompatActivity()
{
	private lateinit var mNavController: NavController
	private lateinit var mAppBarConfiguration: AppBarConfiguration
	private lateinit var mBinding: ActivityMainBinding

	override fun onCreate(savedInstanceState: Bundle?)
	{
		setAppTheme()
		super.onCreate(savedInstanceState)

		// Get the binding data from the main activity XML
		mBinding = ActivityMainBinding.inflate(layoutInflater)
		setContentView(mBinding.root)
		setSupportActionBar(mBinding.toolbar)

		// Setup the navigation
		val navHostFragment = supportFragmentManager.findFragmentById(R.id.nav_host_fragment_content_main) as NavHostFragment
		mNavController = navHostFragment.navController
		mNavController.addOnDestinationChangedListener { _, destination, _ ->
			if (destination.id == R.id.FragmentSettings)
				mBinding.toolbar.menu.clear()
			else
				menuInflater.inflate(R.menu.menu_main, mBinding.toolbar.menu)
		}

		// Configure the app bar
		mAppBarConfiguration = AppBarConfiguration(mNavController.graph)// setOf(R.id.FragmentMain, R.id.FragmentSettings)
		setupActionBarWithNavController(mNavController, mAppBarConfiguration)
	}

	override fun onCreateOptionsMenu(menu: Menu): Boolean
	{
		// Inflate the menu; this adds items to the action bar if it is present.
		menuInflater.inflate(R.menu.menu_main, menu)
		return true
	}

	override fun onOptionsItemSelected(item: MenuItem): Boolean
	{
		// Handle action bar item clicks here. The action bar will
		// automatically handle clicks on the Home/Up button, so long
		// as you specify a parent activity in AndroidManifest.xml.
		return when (item.itemId)
		{
			R.id.action_settings -> {
				findNavController(R.id.nav_host_fragment_content_main).navigate(R.id.action_Main_to_Settings)
				return true
			}
			else -> super.onOptionsItemSelected(item)
		}
	}

	override fun onSupportNavigateUp(): Boolean
	{
		//mAppBarConfiguration
		return mNavController.navigateUp() || super.onSupportNavigateUp()
	}

	private fun setAppTheme()
	{
		val settings = Settings(this)
		when  (settings.theme) {
			"Light" -> setTheme(R.style.Theme_AllKeys_Light)
			"Dark" -> setTheme(R.style.Theme_AllKeys_Dark)
			else -> setTheme(R.style.Theme_AllKeys)
		}
	}
}