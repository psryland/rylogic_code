package nz.co.rylogic.allkeys

import android.content.Context
import android.graphics.drawable.ColorDrawable
import android.media.SoundPool
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.widget.GridLayout
import android.widget.SeekBar
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import nz.co.rylogic.allkeys.databinding.FragmentMainBinding
import java.beans.PropertyChangeEvent
import java.beans.PropertyChangeListener

// A simple [Fragment] subclass as the default destination in the navigation.
class FragmentMain : Fragment(), PropertyChangeListener
{
	// Colours for the beat indicators
	private lateinit var mBeat0: ColorDrawable
	private lateinit var mBeat1: ColorDrawable

	// This property is only valid between onCreateView and  onDestroyView.
	private lateinit var mBinding: FragmentMainBinding

	// References to the beat indicators
	private var mBeats: List<GridLayout> = listOf()

	// App settings
	private lateinit var mSettings: Settings

	// The thing that changes the chord
	private lateinit var mUpdater: Updater

	// For playing root note sounds
	private lateinit var mSoundPoolNotes: SoundPool
	private var mSoundRootNotes: List<Int> = listOf()

	// For playing metronome sounds
	private lateinit var mSoundPoolClicks: SoundPool
	private var mSoundClicks: Map<String, Int> = mapOf()

	// The sound if of the root note to play
	private var mSoundRootNote: Int = 0

	override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View
	{
		mSettings = Settings(context as Context)
		mUpdater = Updater(context as Context, Handler(Looper.getMainLooper()))

		mSoundPoolNotes = SoundPool.Builder().setMaxStreams(1).build()
		mSoundPoolNotes.setOnLoadCompleteListener { _, sampleId, status ->
			if (status != 0)
			{
				// Write error to logcat
				android.util.Log.e("AllKeys", "Failed to load sound $sampleId")
			}
		}
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.g1, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.ab1, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.a2, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.bb2, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.b2, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.c2, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.db2, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.d2, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.eb2, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.e2, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.f2, 1)
		mSoundRootNotes += mSoundPoolNotes.load(context, R.raw.gb2, 1)

		mSoundPoolClicks = SoundPool.Builder().setMaxStreams(1).build()
		mSoundPoolClicks.setOnLoadCompleteListener { _, sampleId, status ->
			if (status != 0)
			{
				// Write error to logcat
				android.util.Log.e("AllKeys", "Failed to load sound $sampleId")
			}
		}
		mSoundClicks += "Click" to mSoundPoolClicks.load(context, R.raw.click0, 1)
		mSoundClicks += "CowBell" to mSoundPoolClicks.load(context, R.raw.click0, 1)
		mSoundClicks += "WoodBlock" to mSoundPoolClicks.load(context, R.raw.click0, 1)

		mBinding = FragmentMainBinding.inflate(inflater, container, false)
		mBeat0 = ColorDrawable(ContextCompat.getColor(requireContext(), R.color.metronome_beat0))
		mBeat1 = ColorDrawable(ContextCompat.getColor(requireContext(), R.color.metronome_beat1))

		val tempoLayout = mBinding.tempoLayout
		mBeats = listOf(tempoLayout.beat0,tempoLayout.beat1, tempoLayout.beat2, tempoLayout.beat3, tempoLayout.beat4, tempoLayout.beat5, tempoLayout.beat6)
		mUpdater.addPropertyChangeListener(this)
		return mBinding.root
	}

	override fun onDestroyView()
	{
		super.onDestroyView()
		mSoundPoolNotes.release()
		mSoundPoolClicks.release()
	}

	override fun onViewCreated(view: View, savedInstanceState: Bundle?)
	{
		super.onViewCreated(view, savedInstanceState)

		// Update the tempo slider
		val tempoLayout = mBinding.tempoLayout
		tempoLayout.labelTempo.text = getString(R.string.tempo_bpm, mSettings.tempo)
		tempoLayout.sliderSpeed.progress = mSettings.tempo
		tempoLayout.sliderSpeed.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener
		{
			override fun onProgressChanged(sb: SeekBar, progress: Int, from: Boolean)
			{
				mSettings.tempo = progress
				tempoLayout.labelTempo.text = getString(R.string.tempo_bpm, mSettings.tempo)
			}

			override fun onStartTrackingTouch(p0: SeekBar?)
			{
			}

			override fun onStopTrackingTouch(p0: SeekBar?)
			{
			}
		})

		// Update the beat indicators
		for (i in mBeats.indices)
		{
			mBeats[i].background = if (i == 0) mBeat1 else mBeat0
			mBeats[i].visibility = if (i < mSettings.beatsPerBar) View.VISIBLE else View.GONE
		}

		// Buttons
		val buttonLayout = mBinding.buttonLayout
		buttonLayout.buttonNext.setOnClickListener {
			mUpdater.next()
		}
		buttonLayout.buttonStart.setOnClickListener {
			if (mUpdater.running)
			{
				mUpdater.running = false
			}
			else
			{
				mUpdater.reset(mSettings)
				mUpdater.running = true
			}
		}

		// Initialise the updater
		mUpdater.reset(mSettings)
	}

	override fun onPause()
	{
		mUpdater.running = false
		super.onPause()
	}

	override fun propertyChange(prop: PropertyChangeEvent?)
	{
		val chordLayout  = mBinding.chordLayout
		val buttonLayout = mBinding.buttonLayout

		// Start/Stop running
		if (prop?.propertyName == Updater.RUNNING)
		{
			// Update the button text
			if (mUpdater.running)
			{
				buttonLayout.buttonStart.setText(R.string.stop)
			}
			else
			{
				buttonLayout.buttonStart.setText(R.string.start)
				mSoundPoolNotes.autoPause()
				mSoundPoolClicks.autoPause()
			}
		}

		// Chord change
		if (prop?.propertyName == Updater.NOTIFY_CHORD)
		{
			// Update the chord text
			chordLayout.textChord.text = getString(R.string.key_and_chord, mUpdater.key, mUpdater.chord)
			chordLayout.textChordNext.text = getString(R.string.key_and_chord, mUpdater.keyNext, mUpdater.chordNext)

			// Update the root note in the media player
			if (mSettings.playRootNote)
			{
				mSoundRootNote = when (mUpdater.key)
				{
					"G" -> mSoundRootNotes[0]
					"G♯" -> mSoundRootNotes[1]
					"A♭" -> mSoundRootNotes[1]
					"A" -> mSoundRootNotes[2]
					"A♯" -> mSoundRootNotes[3]
					"B♭" -> mSoundRootNotes[3]
					"B" -> mSoundRootNotes[4]
					"B♯" -> mSoundRootNotes[5]
					"C♭" -> mSoundRootNotes[4]
					"C" -> mSoundRootNotes[5]
					"C♯" -> mSoundRootNotes[6]
					"D♭" -> mSoundRootNotes[6]
					"D" -> mSoundRootNotes[7]
					"D♯" -> mSoundRootNotes[8]
					"E♭" -> mSoundRootNotes[8]
					"E" -> mSoundRootNotes[9]
					"E♯" -> mSoundRootNotes[10]
					"F♭" -> mSoundRootNotes[9]
					"F" -> mSoundRootNotes[10]
					"F♯" -> mSoundRootNotes[11]
					"G♭" -> mSoundRootNotes[11]
					else -> 0
				}
			}
		}

		// Update metronome
		if (prop?.propertyName == Updater.NOTIFY_BEAT)
		{
			val beat = (prop.newValue as Int) % mSettings.beatsPerBar

			// Update the visuals for each beat
			for (i in mBeats.indices)
			{
				mBeats[i].background = if (i == beat) mBeat1 else mBeat0
			}

			// Play the metronome sound on each beat
			if (mSettings.metronomeSounds)
			{
				if (beat == 0)
					mSoundPoolClicks.play(mSoundClicks[mSettings.metronomeAccent] ?: 0, 1.0f, 1.0f, 0, 0, 1.0f)
				else
					mSoundPoolClicks.play(mSoundClicks[mSettings.metronomeClick] ?: 0, 0.7f, 0.7f, 0, 0, 1.0f)
			}

			// Play the root note on beat 1
			if (mSettings.playRootNote && beat == 0)
			{
				mSoundPoolNotes.play(mSoundRootNote, 1.0f, 1.0f, 0, 0, 1.0f)
			}

			// Update the button text
			if (mUpdater.running)
				buttonLayout.buttonStart.setText(R.string.stop)
			else
				buttonLayout.buttonStart.setText(R.string.start)
		}
	}
}
