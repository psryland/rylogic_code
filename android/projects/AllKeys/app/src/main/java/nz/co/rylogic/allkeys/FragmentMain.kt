package nz.co.rylogic.allkeys

import android.content.Context
import android.content.SharedPreferences
import android.graphics.Color
import android.graphics.drawable.ColorDrawable
import android.media.MediaPlayer
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.GridLayout
import android.widget.SeekBar
import androidx.fragment.app.Fragment
import androidx.preference.PreferenceManager
import nz.co.rylogic.allkeys.databinding.FragmentMainBinding
import java.beans.PropertyChangeEvent
import java.beans.PropertyChangeListener
import java.util.*

// A simple [Fragment] subclass as the default destination in the navigation.
class FragmentMain : Fragment(), PropertyChangeListener
{
	companion object
	{
		const val MIN_TEMPO = 40.0
		const val MAX_TEMPO = 300.0
		final val BEAT0 = ColorDrawable(R.color.metronome_beat0)
		final val BEAT1 = ColorDrawable(R.color.metronome_beat1)
	}

	// This property is only valid between onCreateView and  onDestroyView.
	private val binding get() = mBinding!!
	private var mBinding: FragmentMainBinding? = null

	// References to the beat indicators
	private var mBeats: List<GridLayout> = listOf()

	// App settings
	private val settings get() = mSettings!!
	private var mSettings: Settings? = null

	// The thing that changes the chord
	private var mUpdater :Updater = Updater(Handler(Looper.getMainLooper()))

	// For playing root notes
	private var mMediaPlayerRootNotes :MediaPlayer? = null

	override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View
	{
		mSettings = Settings(context as Context)
		mBinding = FragmentMainBinding.inflate(inflater, container, false)
		mBeats = listOf(binding.beat1, binding.beat2, binding.beat3, binding.beat4, binding.beat5, binding.beat6, binding.beat7)
		mUpdater.addPropertyChangeListener(this)
		return binding.root
	}

	override fun onDestroyView()
	{
		super.onDestroyView()
		mBinding = null
	}

	override fun onViewCreated(view: View, savedInstanceState: Bundle?)
	{
		super.onViewCreated(view, savedInstanceState)

		fun tempo(): Double = 1.0 * binding.sliderSpeed.progress//(1.0 * binding.sliderSpeed.progress / binding.sliderSpeed.max) * (MAX_TEMPO - MIN_TEMPO) + MIN_TEMPO
		binding.sliderSpeed.progress = 120
		binding.labelTempo.text = "-- Tempo: ${tempo()} bpm --"
		for (i in mBeats.indices)
		{
			mBeats[i].background = if (i == 0) BEAT1 else BEAT0
			mBeats[i].visibility = if (settings.beatsPerBar > i) View.VISIBLE else View.GONE
		}
		binding.sliderSpeed.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
			override fun onProgressChanged(sb:SeekBar, progress:Int, from:Boolean) {
				mUpdater.running = false
				mUpdater.tempo = tempo()
				binding.labelTempo.text = "-- Tempo: ${tempo()} bpm --"
			}
			override fun onStartTrackingTouch(p0: SeekBar?) {}
			override fun onStopTrackingTouch(p0: SeekBar?) {}
		})
		binding.buttonNext.setOnClickListener{
			mUpdater.next()
		}
		binding.buttonStart.setOnClickListener {
			if (mUpdater.running)
			{
				mUpdater.running = false
			}
			else
			{
				mUpdater.reset(settings, tempo())
				mUpdater.running = true
			}
		}

		mUpdater.reset(settings, tempo())
	}

	override fun onPause()
	{
		mUpdater.running = false
		super.onPause()
	}

	override fun propertyChange(prop: PropertyChangeEvent?)
	{
		// Chord change
		if (prop?.propertyName == Updater.NOTIFY_CHORD)
		{
			// Update the chord text
			binding.textChord.text = "${mUpdater.key} ${mUpdater.chord}"
		}

		// Update metronome
		if (prop?.propertyName == Updater.NOTIFY_BEAT)
		{
			var beat = (prop.newValue as Int) % settings.beatsPerBar
			for (i in mBeats.indices)
			{
				mBeats[i].background = if (i == beat) BEAT1 else BEAT0

			}

			// Play the root note on beat 1
			if (settings.playRootNote && beat == 1)
			{
				mMediaPlayerRootNotes?.release()
				var mp = MediaPlayer.create(context, R.raw.a440)
				mp.setOnCompletionListener { mp.release() }
				mp.start()
				mMediaPlayerRootNotes = mp
			}
		}

		// Update the button text
		if (mUpdater.running)
			binding.buttonStart.setText(R.string.stop)
		else
			binding.buttonStart.setText(R.string.start)
	}
}
