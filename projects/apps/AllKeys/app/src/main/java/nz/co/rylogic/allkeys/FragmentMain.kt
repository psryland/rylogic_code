package nz.co.rylogic.allkeys

import android.content.Context
import android.content.SharedPreferences
import android.graphics.drawable.GradientDrawable
import android.os.Bundle
import android.os.Handler
import android.os.Looper
import android.util.Log
import android.view.LayoutInflater
import android.view.View
import android.view.ViewGroup
import android.view.WindowManager
import android.widget.GridLayout
import android.widget.SeekBar
import androidx.core.content.ContextCompat
import androidx.fragment.app.Fragment
import androidx.preference.PreferenceManager
import nz.co.rylogic.allkeys.databinding.ButtonLayoutBinding
import nz.co.rylogic.allkeys.databinding.ChordLayoutBinding
import nz.co.rylogic.allkeys.databinding.FragmentMainBinding
import java.beans.PropertyChangeEvent
import java.beans.PropertyChangeListener
import kotlin.math.absoluteValue
import kotlin.random.Random

// A simple [Fragment] subclass as the default destination in the navigation.
class FragmentMain : Fragment(), PropertyChangeListener, SharedPreferences.OnSharedPreferenceChangeListener
{
	companion object
	{
		private val CHANNEL_ROOT_NOTES = FluidSynth.EMidiChannel.Ch1
		private val CHANNEL_COMPING = FluidSynth.EMidiChannel.Ch2
		private val CHANNEL_METRONOME = FluidSynth.EMidiChannel.Ch10
	}

	// App context
	private lateinit var mContext: Context
	private lateinit var mSettings: Settings

	// This property is only valid between onCreateView and  onDestroyView.
	private lateinit var mBinding: FragmentMainBinding

	// References to the beat indicators
	private var mBeats: List<GridLayout> = listOf()

	// The thing that changes the chord
	private lateinit var mUpdater: Updater

	// Colours for the beat indicators
	private lateinit var mBeat0: GradientDrawable
	private lateinit var mBeat1: GradientDrawable

	// For playing root note sounds
	private var mFluidSynth: FluidSynth = FluidSynth()
	private var mFluidSequencer: FluidSequencer = FluidSequencer(mFluidSynth)
	private var mFluidPlayer: FluidPlayer = FluidPlayer(mFluidSynth)
	private var mStartTime: Long = 0

	override fun onCreate(savedInstanceState: Bundle?)
	{
		mContext = context as Context
		mSettings = mContext.settings
		super.onCreate(savedInstanceState)
		mBeat0 = ContextCompat.getDrawable(requireContext(), R.drawable.beat0) as GradientDrawable
		mBeat1 = ContextCompat.getDrawable(requireContext(), R.drawable.beat1) as GradientDrawable

		// Listen for changes to the settings
		PreferenceManager.getDefaultSharedPreferences(mContext).registerOnSharedPreferenceChangeListener(this)

		// Create the synth
		mFluidSynth.loadSoundFont(mContext.localFile("allkeys.sf3"))
		mFluidSynth.programChange(CHANNEL_ROOT_NOTES, mSettings.rootNoteInstrument.value)
		mFluidSynth.programChange(CHANNEL_COMPING, EInstruments.Piano.value)
		mFluidSynth.programChange(CHANNEL_METRONOME, EInstruments.DrumKit.value)
		mFluidSynth.masterGain = 2.0f

		// Create the updater
		mUpdater = Updater(context as Context, mSettings, Handler(Looper.getMainLooper()))
		mUpdater.addPropertyChangeListener(this)
	}

	override fun onDestroy()
	{
		PreferenceManager.getDefaultSharedPreferences(context as Context).unregisterOnSharedPreferenceChangeListener(this)
		super.onDestroy()
		soundsOff()
		mFluidPlayer.close()
		mFluidSequencer.close()
		mFluidSynth.close()
	}

	override fun onCreateView(inflater: LayoutInflater, container: ViewGroup?, savedInstanceState: Bundle?): View?
	{
		return inflater.inflate(R.layout.fragment_main, container, false)
	}

	override fun onViewCreated(view: View, savedInstanceState: Bundle?)
	{
		super.onViewCreated(view, savedInstanceState)
		mBinding = FragmentMainBinding.bind(view)
		val tempoLayout = mBinding.tempoLayout
		val buttonLayout = mBinding.buttonLayout

		// Get the beat indicators
		mBeats = listOf(tempoLayout.beat0, tempoLayout.beat1, tempoLayout.beat2, tempoLayout.beat3, tempoLayout.beat4, tempoLayout.beat5, tempoLayout.beat6)

		// Attach a listener to the tempo slider
		tempoLayout.labelTempo.text = getString(R.string.tempo_bpm, mSettings.tempo)
		tempoLayout.sliderSpeed.progress = mSettings.tempo
		tempoLayout.sliderSpeed.setOnSeekBarChangeListener(object : SeekBar.OnSeekBarChangeListener {
			override fun onProgressChanged(sb: SeekBar, progress: Int, from: Boolean) { mSettings.tempo = progress }
			override fun onStartTrackingTouch(p0: SeekBar?) {}
			override fun onStopTrackingTouch(p0: SeekBar?) {}
		})

		// Buttons
		buttonLayout.buttonStart.setOnClickListener {
			if (mUpdater.runMode == ERunMode.Stopped)
				mUpdater.runMode = ERunMode.Continuous
			else
				mUpdater.runMode = ERunMode.Stopped
		}
		buttonLayout.buttonNext.setOnClickListener {
			mUpdater.runMode = ERunMode.Stopped
			mUpdater.runMode = ERunMode.StepOne
		}

		mUpdater.reset()
	}

	override fun onPause()
	{
		mUpdater.runMode = ERunMode.Stopped
		activity?.window?.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
		soundsOff()
		super.onPause()
	}

	override fun onSharedPreferenceChanged(prefs: SharedPreferences?, key: String?)
	{
		when (key)
		{
			Settings.TEMPO ->
			{
				val tempoLayout = mBinding.tempoLayout
				tempoLayout.labelTempo.text = getString(R.string.tempo_bpm, mSettings.tempo)
				mUpdater.runMode = ERunMode.Stopped
			}
			Settings.ROOT_NOTE_INSTRUMENT ->
			{
				mFluidSynth.programChange(CHANNEL_ROOT_NOTES, mSettings.rootNoteInstrument.ordinal)
			}
			Settings.BEATS_PER_BAR ->
			{
				soundsOff()
				updateBeatIndicators(0)
				mUpdater.reset()
			}
			Settings.SELECTED_KEYS ->
			{
				mUpdater.reset()
			}
			Settings.SELECTED_CHORDS ->
			{
				mUpdater.reset()
			}
			else ->
			{
			}
		}
	}

	override fun propertyChange(prop: PropertyChangeEvent?)
	{
		val chordLayout = mBinding.chordLayout
		val buttonLayout = mBinding.buttonLayout

		when (prop?.propertyName)
		{
			// Start/Stop running
			Updater.NOTIFY_RUN_MODE ->
			{
				// Update the UI
				updateChordText(chordLayout)
				updateNextChord(chordLayout, 0, 0)
				updateBeatIndicators(0)
				updateButtons(buttonLayout)

				// Stop sounds
				if (mUpdater.runMode == ERunMode.Stopped)
					soundsOff()

				// Update the screen on flag
				if (mUpdater.runMode == ERunMode.Continuous)
					activity?.window?.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
				else
					activity?.window?.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)

				// Record the start time on every run mode change
				mStartTime = mFluidSequencer.time

				// Start sounds for continuous mode
				if (mUpdater.runMode == ERunMode.Continuous)
				{
					//// Read swing0.mid into a byte array
					//val midiData = resources.openRawResource(R.raw.swing0).readBytes()
					//mFluidPlayer.addData(midiData)
					//mFluidPlayer.setTempo(EFluidPlayerTempoType.ExternalBPM, mSettings.tempo.toDouble())
					//mFluidPlayer.loop(true)
					//mFluidPlayer.play()
				}
			}

			// Chord change
			Updater.NOTIFY_CHORD ->
			{
				// Update the chord text
				updateChordText(chordLayout)
			}

			// Beat change
			Updater.NOTIFY_BEAT ->
			{
				val beat = mUpdater.beat
				val bar = mUpdater.bar

				// Update the beat indicators
				updateBeatIndicators(beat)

				// Update the 'next chord' visibility
				updateNextChord(chordLayout, beat, bar)

				// Play the metronome sound on each beat
				if (mSettings.metronomeSounds)
				{
					renderMetronome(beat)
				}

				// Play the root note
				if (mSettings.rootNoteSounds && beat == 0)
				{
					// Generate the walking bass line
					if (mUpdater.runMode == ERunMode.Continuous && mSettings.rootNoteWalking)
						renderWalk()
					else
						renderSingleNote()
				}

				// Play the comping
				if (mSettings.chordCompSounds && beat == 0)
				{
					if (mUpdater.runMode == ERunMode.Continuous)
						renderComping()
					else
						renderSingleComp()
				}
			}
		}
	}

	// Update the button text
	private fun updateButtons(buttonLayout: ButtonLayoutBinding)
	{
		if (mUpdater.runMode == ERunMode.Continuous)
			buttonLayout.buttonStart.setText(R.string.stop)
		else
			buttonLayout.buttonStart.setText(R.string.start)
	}

	// Update the beat indicators
	private fun updateBeatIndicators(beat: Int)
	{
		for (i in mBeats.indices)
		{
			mBeats[i].background = if (i == beat) mBeat1 else mBeat0
			mBeats[i].visibility = if (i < mSettings.beatsPerBar) View.VISIBLE else View.GONE
		}
	}

	// Update the chord text
	private fun updateChordText(chordLayout: ChordLayoutBinding)
	{
		chordLayout.textChord.text = getString(R.string.key_and_chord, mUpdater.key, mUpdater.chord)
		chordLayout.textChordNext.text = getString(R.string.key_and_chord, mUpdater.keyNext, mUpdater.chordNext)
	}

	// Update the 'next chord' visibility
	private fun updateNextChord(chordLayout: ChordLayoutBinding, beat: Int, bar: Int)
	{
		when (mSettings.nextChordMode)
		{
			ENextChordMode.Hidden -> chordLayout.textChordNext.visibility =
				View.GONE
			ENextChordMode.OneBeatBefore -> chordLayout.textChordNext.visibility =
				if (beat >= mSettings.beatsPerBar - 1 && bar == mSettings.barsPerChord - 1)
					View.VISIBLE else View.INVISIBLE
			ENextChordMode.TwoBeatsBefore -> chordLayout.textChordNext.visibility =
				if (beat >= mSettings.beatsPerBar - 2 && bar == mSettings.barsPerChord - 1)
					View.VISIBLE else View.INVISIBLE
			ENextChordMode.BarBefore -> chordLayout.textChordNext.visibility =
				if (bar >= mSettings.barsPerChord - 1)
					View.VISIBLE else View.INVISIBLE
			ENextChordMode.Always -> chordLayout.textChordNext.visibility =
				View.VISIBLE
		}
	}

	// Generate chord accompaniment
	private fun renderComping()
	{
		val vamp = mUpdater.vamp
		var voice = mUpdater.voice
		val changeChordWeight = 0.3

		var time = (mStartTime + mUpdater.beatCount * mUpdater.beatPeriodMs).toDouble()
		for (step in vamp.steps)
		{
			val duration = mUpdater.beatPeriodMs * step.duration
			val velocity = mSettings.chordCompVolume * step.velocity

			// Sequence the notes
			if (step.key == 'c')
			{
				// Change the voice sometimes
				if (Random.nextDouble() < changeChordWeight * step.duration)
					voice = mUpdater.voice

				Log.d("Voice", "Selected voice ${voice.name}")

				for (note in voice.lh.union(voice.rh))
				{
					FluidEvent().note(CHANNEL_COMPING.value, note, (127 * velocity).toInt().toShort(), duration.toInt()).use {
						mFluidSequencer.queue(it, time.toLong(), true)
					}
				}
			}
			time += duration
		}
	}

	// Generate a single comping chord
	private fun renderSingleComp()
	{
		// Chords play more notes at once so tend to be louder
		// In the genre file, comping chords are around 0.5 to 0.7
		val chordVolumeScalar = 0.6

		val voice = mUpdater.voice
		val volume = mSettings.chordCompVolume * chordVolumeScalar
		val duration = mUpdater.beatPeriodMs * mSettings.beatsPerBar

		for (note in voice.lh.union(voice.rh))
		{
			FluidEvent().note(CHANNEL_COMPING.value, note, (127 * volume).toInt().toShort(), duration).use {
				mFluidSequencer.queue(it, 0, false)
			}
		}
	}

	// Generate one bars worth of bass line
	private fun renderWalk()
	{
		val c = mUpdater.key
		val n = mUpdater.keyNext
		val walk = mUpdater.walk
		val scale = mUpdater.scale

		var time = (mStartTime + mUpdater.beatCount * mUpdater.beatPeriodMs).toDouble()
		for (step in walk.steps)
		{
			// Note length and velocity
			val volume = mSettings.rootNoteVolume * step.velocity
			val duration = mUpdater.beatPeriodMs * step.duration

			// Convert the scale degree to a MIDI note
			val root = when (step.key) { 'c' -> c; 'n' -> n; else -> "" }
			val key = if (root.isNotEmpty()) scaleDegreeToMidiNote(root, step.scaleDegree, step.accidental, scale) else null

			// Sequence the notes
			if (key != null)
			{
				val note = mapToRange(key, listOf(0), 28..96).first()
				FluidEvent().note(CHANNEL_ROOT_NOTES.value, note, (127*volume).toInt().toShort(), duration.toInt()).use {
					mFluidSequencer.queue(it, time.toLong(), true)
				}
			}
			time += duration
		}
	}

	// Generate a single root note for a bar
	private fun renderSingleNote()
	{
		val key = mSettings.keyRootNotes[mUpdater.key] ?: 0
		val volume = mSettings.rootNoteVolume
		val duration = mUpdater.beatPeriodMs * mSettings.beatsPerBar
		FluidEvent().note(CHANNEL_ROOT_NOTES.value, key, (127 * volume).toInt().toShort(), duration).use {
			mFluidSequencer.queue(it, 0, false)
		}
	}

	// Generate the metronome sound
	private fun renderMetronome(beat: Int)
	{
		// Accent the first beat
		val sound = if (beat == 0) mSettings.metronomeAccent else mSettings.metronomeClick
		val volume = mSettings.metronomeVolume * (if (beat == 0) 1.0 else 0.9)
		val duration = mUpdater.beatPeriodMs

		FluidEvent().note(CHANNEL_METRONOME.value, sound.value, (127 * volume).toInt().toShort(), duration).use {
			if (mUpdater.runMode == ERunMode.Continuous)
			{
				val time = mStartTime + mUpdater.beatCount * mUpdater.beatPeriodMs
				mFluidSequencer.queue(it, time, true)
			}
			else
			{
				mFluidSequencer.queue(it, 0, false)
			}
		}
	}

	// Convert a scale degree to a midi note
	private fun scaleDegreeToMidiNote(key: String, scaleDegree: Int, accidental: Int, scale: Scale): Short?
	{
		// Get the base midi key for 'key'
		val note = mSettings.keyRootNotes[key] ?: return null

		// Convert the scale degree to a 0-based interval. This means 7 == octave
		val scaleDegree0 = scaleDegree.absoluteValue - 1

		// Get the octave of the scale degree (assumes 7 note scales)
		val octave = scaleDegree0 / 7
		val interval = scaleDegree0 % 7

		// Convert the scale note to a chromatic index
		val chromaticIndex = if (scaleDegree > 0)
		{
			val scaleIndex = mScaleMapping[scale.notes.size]?.get(interval) ?: 0
			scale.notes[scaleIndex] + octave * 12
		}
		else // -2 == 7, -4 == 5, -6 == 3, -8 == 1
		{
			val scaleIndex = mScaleMapping[scale.notes.size]?.get(7 - interval) ?: 0
			scale.notes[scaleIndex] - (1+octave) * 12
		}

		// Get the midi note
		return (note + accidental + chromaticIndex).toShort()
	}

	private fun soundsOff()
	{
		mFluidPlayer.stop()
		mFluidSequencer.flush(FluidEvent.EType.FLUID_SEQ_NOTE)
		mFluidSynth.allNotesOff(FluidSynth.EMidiChannel.ChAll)
	}

	// 'scaleDegree' assumes a 7-note scale. Map the scale degree to an index in the actual scale
	private val mScaleMapping: Map<Int, List<Int>> = mapOf(
		1 to listOf(0, 0, 0, 0, 0, 0, 0),
		2 to listOf(0, 0, 0, 0, 1, 1, 1),
		3 to listOf(0, 0, 0, 1, 1, 1, 2),
		4 to listOf(0, 0, 1, 1, 2, 2, 3),
		5 to listOf(0, 1, 1, 2, 3, 3, 4),
		6 to listOf(0, 1, 2, 3, 4, 4, 5),
		7 to listOf(0, 1, 2, 3, 4, 5, 6),
		8 to listOf(0, 1, 2, 4, 5, 6, 7),
	)
}
