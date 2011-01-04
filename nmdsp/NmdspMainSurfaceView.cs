using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Media;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Commons.Music.Midi;
using Commons.Music.Midi.Player;

using Timer = System.Timers.Timer;

namespace nmdsp
{
	class NmdspMainView : SurfaceView, ISurfaceHolderCallback
	{
		NmdspMainActivity activity;
		Timer update_timer;
		long time;
		const int color_background = unchecked ((int) 0xFF000008);
		const int color_white_key = unchecked ((int) 0xFFAaAaAa);
		const int color_basic_stroke = unchecked ((int) 0xFF000000);
		const int color_black_key = unchecked ((int) 0xFF000000);
		const int color_black_key_edge = unchecked ((int) 0xFFFfFfFf);
		const int color_keyon = unchecked ((int) 0xFFFfFf00);
		const int color_aftertouch = unchecked ((int) 0xFFFf8000);
		const int color_bend = unchecked ((int) 0xFF0080Ff);
		const int color_hold = unchecked ((int) 0xFF0080C0);
		const int color_bright = unchecked ((int) 0xFFFfFfE0);
		const int color_usual = unchecked ((int) 0xFF3060C0);
		const int color_dark = unchecked ((int) 0xFF1830C0);
		const int color_hidden = unchecked ((int) 0xFF000030);
		const int color_ch_base = unchecked ((int) color_bright);
		const int color_ch_colored = unchecked ((int) color_usual);
		const int color_ch_dark = unchecked ((int) color_dark);
		const int color_ch_hidden = unchecked ((int) color_hidden);
		const int color_ch_text_colored = unchecked ((int) color_ch_colored);
		const int color_ch_text_base = unchecked ((int) color_ch_base);
		const int color_ch_text_dark = unchecked ((int) color_ch_dark);
		const int color_ch_text_hidden = unchecked ((int) color_ch_hidden);
		int all_keys, key_lower_bound, key_higher_bound;
		bool small_screen;
		int channels;
		int key_width;
		int key_height;
		float blackKeyWidth;
		float blackKeyHeight;
		int ch_height;
		int text_height;
		int play_info_section_width;
		String[] ch_types;
		Canvas canvas;
		BitmapDrawable bitmap_drawable;
		Paint paint = new Paint();
		bool needs_redraw;

		public NmdspMainView(IntPtr handle)
			: base (handle)
		{
		}

		public NmdspMainView(Context context)
			: base (context)
		{
			activity = (NmdspMainActivity) context;
			key_lower_bound = 24;
			key_higher_bound = 128;
			all_keys = key_higher_bound - key_lower_bound;
			channels = 16;
			key_width = 7;
			key_height = 16;
			blackKeyWidth = (float)(key_width * 0.4 + 1);
			blackKeyHeight = key_height / 2;
			ch_height = 32;
			text_height = 8;
			play_info_section_width = 200;
			ch_types = new String[] { "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI", "MIDI" };

			Holder.AddCallback(this);
			Focusable = true;
			RequestFocus();
		}

#if false
		// JetPlayer.OnJetEventListener implementation

		public void OnJetEvent(JetPlayer player, short segment, byte track, byte channel, byte controller, byte value)
		{
			// TODO Auto-generated method stub
			switch (controller) {
			case (byte) 0x90: // note on
				break;
			case (byte) 0x80: // note off
				break;
			}
		}

		public void OnJetNumQueuedSegmentUpdate(JetPlayer player, int nbSegments)
		{
			// nothing to do
		}

		public void OnJetPauseUpdate(JetPlayer player, int paused)
		{
			// nothing to do yet
		}

		public void onJetUserIdUpdate(JetPlayer player, int userId, int repeatCount)
		{
			// nothing to do
		}
#endif

		public override bool OnTouchEvent(MotionEvent e)
		{
			int x = (int) e.GetX();
			int y = (int) e.GetY();
			switch (e.Action) {
			case MotionEventActions.Down:
				if (play_button.Contains(x, y))
					ProcessPlay();
				else if (pause_button.Contains(x, y))
					ProcessPause();
				else if (stop_button.Contains(x, y))
					ProcessStop();
				else if (ff_button.Contains(x, y))
					ProcessFastForward();
				else if (rew_button.Contains(x, y))
					ProcessRewind();
				else if (load_button.Contains(x, y))
					ProcessLoad();
				break;
			}
			// TODO Auto-generated method stub
			return base.OnTouchEvent(e);
		}

		public void SurfaceChanged(ISurfaceHolder holder, int format, int width,
				int height) {
			// TODO Auto-generated method stub
			
		}

		public void SurfaceCreated(ISurfaceHolder holder)
		{
			// Init.
			Rect rect = Holder.SurfaceFrame;
			Bitmap bmp = Bitmap.CreateBitmap(rect.Width(), rect.Height(), Bitmap.Config.Argb4444);
			bitmap_drawable = new BitmapDrawable(bmp);
			canvas = new Canvas(bmp);
			paint.SetStyle(Paint.Style.Fill);
			paint.Color = color_background;
			canvas.DrawRect(canvas.ClipBounds, paint);

			// initialize display size
			if (rect.Width() < 600) {
				small_screen = true;
				key_lower_bound = 24;
				key_higher_bound = 96;
				all_keys = 96 - 24;
			}

			for (int i = 0; i < channels; i = i + 1)
				SetupChannelInfo (i);
			for (int i = 0; i < channels; i = i + 1)
				SetupKeyboard (i);

			SetupParameterVisualizer ();
			SetupPlayerStatusPanel ();
			//addPlayTimeStatusPanel ();
			//addSpectrumAnalyzerPanel ();
			//addKeyonMeterPanel ();
			
			Canvas c = Holder.LockCanvas();
			c.DrawBitmap(bmp, new Matrix(), paint);
			Holder.UnlockCanvasAndPost(c);

			update_timer = new Timer () { Interval = 100 };
			update_timer.Elapsed += delegate {
				if (!needs_redraw)
					return;
				UpdateView();
				};
			update_timer.Start();
		}
		
		void UpdateView()
		{
			Canvas c = Holder.LockCanvas();
			if (c != null) {
				c.DrawBitmap(bitmap_drawable.Bitmap, new Matrix (), paint);
				needs_redraw = false;
				Holder.UnlockCanvasAndPost(c);
			}
		}

		void SetupChannelInfo (int channel)
		{
			float yText1 = GetChannelYPos (channel) + text_height;
			float yText2 = GetChannelYPos (channel) + text_height * 2;
			paint.Color = color_ch_text_colored;
			paint.TextSize = 16;
			canvas.DrawText ("" + (channel + 1), 35, yText2, paint); // FIXME: nf(x,2)
			paint.Color = color_ch_text_colored;
			paint.TextSize = 8;
			canvas.DrawText (ch_types [channel], 0, yText1, paint);
			paint.Color = color_ch_text_base;
			canvas.DrawText ("TRACK.", 0, yText2, paint);
			paint.SetStyle (Paint.Style.Stroke);
			/*
			paint.setColor (color_ch_colored);
			canvas.drawLine (340, getChannelYPos (channel) + 2, 360, getChannelYPos (channel) + text_height - 2, paint);
			paint.setStyle (Paint.Style.FILL);
			paint.setColor (color_ch_text_colored);
			canvas.drawText ("" + 1000, 364, getChannelYPos (channel) + text_height, paint); // FIXME: nf(x,5)
			paint.setStyle (Paint.Style.FILL);
			paint.setColor (color_ch_text_base);
			canvas.drawText ("M:--------", 340, yText2, paint);
			*/
		}

		float GetChannelYPos (int channel)
		{
			return channel * ch_height;
		}
		
		void SetupKeyboard (int channel)
		{
			int octaves = all_keys / 12;
			for (int octave = 0; octave < octaves; octave = octave + 1)
				DrawOctave (channel, octave);
		}
		
		void DrawOctave(int channel, int octave)
		{
			float x = octave * key_width * 7;
			float y = GetChannelYPos (channel) + ch_height - key_height;
			//ProcessingApplication.Current.pushMatrix (); // user_code
			//var h = ProcessingApplication.Current.Host; // user_code
			// user_code
			for (int n = 0; n < 12; n++)
			{
				if (!IsWhiteKey (n))
					continue;
				int k = key_to_keyboard_idx [n];
				// user_code
				paint.StrokeJoin = Paint.Join.Round;
				paint.StrokeWidth = 1;
				paint.SetStyle(Paint.Style.Fill);
				paint.Color = color_white_key;
				Rect rect = new Rect ();
				rect.Left = (int) (x + k * key_width);
				rect.Top = (int) y;
				rect.Right = rect.Left + key_width;
				rect.Bottom = rect.Top + key_height;
				canvas.DrawRect (rect, paint);
				paint.SetStyle(Paint.Style.Stroke);
				paint.Color = color_basic_stroke;
				canvas.DrawRect (rect, paint);
				// /user_code
				//key_rectangles [channel, octave * 12 + n] = (Rectangle) h.Children.Last ();
			}
			// user_code
			//var wh = ProcessingApplication.Current.Host;
			//ProcessingApplication.Current.popMatrix ();
			//ProcessingApplication.Current.Host.Children.Remove (wh);
			//white_key_panel.Children.Add (wh);

			//ProcessingApplication.Current.pushMatrix ();
			//h = ProcessingApplication.Current.Host;
			// /user_code

			paint.StrokeJoin = Paint.Join.Bevel;
			paint.StrokeWidth = 1;
			for (int n = 0; n < 12; n++)
			{
				if (IsWhiteKey (n))
					continue;
				paint.SetStyle(Paint.Style.Fill);
				paint.Color = color_black_key;
				int k = key_to_keyboard_idx [n];
				// custom_code
				if (k != 2 && k != 6) {
					int blackKeyStartX = (int) (x + (k + 0.8) * key_width);
					Rect rect = new Rect ();
					rect.Left = blackKeyStartX;
					rect.Top = (int) (y + 1);
					rect.Right = (int) (rect.Left + blackKeyWidth);
					rect.Bottom = (int) (rect.Top + blackKeyHeight);
					canvas.DrawRect (rect, paint);
					// /user_code
					//key_rectangles [channel, octave * 12 + n] = (Rectangle) h.Children.Last ();
					float bottom = y + blackKeyHeight + 1;
					paint.SetStyle(Paint.Style.Stroke);
					paint.Color = color_black_key_edge;
					canvas.DrawLine(blackKeyStartX + 1, bottom, blackKeyStartX + blackKeyWidth - 1, bottom, paint);
				}
			}
			// user_code
			//var bh = ProcessingApplication.Current.Host;
			//ProcessingApplication.Current.popMatrix ();
			//ProcessingApplication.Current.Host.Children.Remove (bh);
			//black_key_panel.Children.Add (bh);
			// /user_code
		}

		int [] key_to_keyboard_idx = {0, 0, 1, 1, 2, 3, 3, 4, 4, 5, 5, 6};
		bool IsWhiteKey (int note)
		{
			switch (note % 12) {
			case 0: case 2: case 4: case 5: case 7: case 9: case 11:
				return true;
			default:
				return false;
			}
		}

		int GetKeyIndexForNote (int value)
		{
			int note = value - key_lower_bound;
			if (note < 0 || note < key_lower_bound || key_higher_bound < note)
				return -1;
			return note;
		}

		public void SurfaceDestroyed(ISurfaceHolder holder)
		{
			update_timer.Stop ();
		}
		
		Rect GetRect (int x, int y, float width, float height)
		{
			Rect rect = new Rect ();
			rect.Left = x;
			rect.Top = y;
			rect.Right = (int) (x + width);
			rect.Bottom = (int) (y + height);
			return rect;
		}

		int left_base;
		Rect play_button, pause_button, stop_button,
			ff_button, rew_button, load_button;

		PlayerStatusPanel player_status_panel;
		PlayTimeStatusPanel play_time_status_panel;
		ParameterVisualizerPanel [] parameter_visualizers;
		//SpectrumAnalyzerPanel spectrum_analyzer_panel;
		//KeyonMeterPanel keyon_meter_panel;

		void SetupParameterVisualizer ()
		{
			parameter_visualizers = new ParameterVisualizerPanel [16];
			for (int i = 0; i < parameter_visualizers.Length; i++) {
				paint.Color = unchecked ((int) 0xFF60A0Ff);
				paint.SetStyle(Paint.Style.Fill);
				canvas.Translate(0, 8);
				canvas.DrawText("VOL", 80, i * 32, paint);
				canvas.DrawText("EXP", 130, i * 32, paint);
				canvas.DrawText("RSD", 80, i * 32 + 8, paint);
				canvas.DrawText("CSD", 130, i * 32 + 8, paint);
				canvas.DrawText("DSD", 180, i * 32 + 8, paint);
				if (!small_screen) {
					canvas.DrawText("H", 220, i * 32, paint);
					canvas.DrawText("P", 220, i * 32 + 8, paint);
					canvas.DrawText("So", 240, i * 32, paint);
					canvas.DrawText("SP", 240, i * 32 + 8, paint);
				}
				canvas.Translate(0, -8);

				ParameterVisualizerPanel p = new ParameterVisualizerPanel ();
				p.location = new Point (80, i * 32);
				parameter_visualizers [i] = p;
			}
		}
		
		void SetupPlayerStatusPanel()
		{
			left_base= small_screen ? 280 : 400;
			int size = small_screen ? 24 : 32;
			paint.TextSize = size;
			play_button = GetRect (left_base + 50, 50 + 20, paint.MeasureText("Play"), size);
			pause_button = GetRect (left_base + 50 + size * 3, 50 + 20, paint.MeasureText("Pause"), size);
			stop_button = GetRect (left_base + 100 + size * 3, 50 + 20, paint.MeasureText("Stop"), size);
			ff_button = GetRect (left_base + 50, 50 + 55, paint.MeasureText("FF"), size);
			rew_button = GetRect (left_base + 50 + size * 3, 50 + 55, paint.MeasureText("Rew"), size);
			load_button = GetRect (left_base + 100 + size * 3, 50 + 55, paint.MeasureText("Load"), size);

			paint.Color = color_dark;
			paint.SetStyle(Paint.Style.Fill);
			var paint2 = new Paint(paint);
			paint2.SetStyle(Paint.Style.Stroke);
			canvas.DrawText("Play", play_button.Left, play_button.Bottom, paint);
			canvas.DrawRect(play_button, paint2);
			canvas.DrawText("Pause", pause_button.Left, pause_button.Bottom, paint);
			canvas.DrawRect(pause_button, paint2);
			canvas.DrawText("Stop", stop_button.Left, stop_button.Bottom, paint);
			canvas.DrawRect(stop_button, paint2);
			canvas.DrawText("FF", ff_button.Left, ff_button.Bottom, paint);
			canvas.DrawRect(ff_button, paint2);
			canvas.DrawText("Rew", rew_button.Left, rew_button.Bottom, paint);
			canvas.DrawRect(rew_button, paint2);
			canvas.DrawText("Load", load_button.Left, load_button.Bottom, paint);
			canvas.DrawRect(load_button, paint2);
		}

		MediaPlayer media_player;
		MidiPlayer midi_player;
		FileInfo midifile;

		void DrawCommon (String s)
		{
			lock (paint) {
				int initX = small_screen ? 300 : 400;
				paint.SetStyle (Paint.Style.FillAndStroke);
				paint.Color = color_background;
				canvas.DrawRect(initX, 110, 400, 140, paint);
				paint.Color = color_dark;
				paint.TextSize = 16;
				canvas.DrawText(s, initX, 140, paint);
				this.needs_redraw = true;
			}
		}
		internal void ProcessPlay()
		{
			DrawCommon("XXXXX");
			if (smf_music == null)
				return;
			if (media_player == null) {
				try {
					media_player = new MediaPlayer ();
					media_player.SetDataSource(jetfile.FullName);
					media_player.Prepare();
				} catch (IOException ex) {
					media_player = null;
					DrawCommon ("failed to load SMF");
					return;
				}
			}
			if (midi_player == null) {
				midi_player = new MidiPlayer (smf_music);
				midi_player.Finished += delegate { StopViews(); };
				midi_player.MessageReceived += HandleSmfMessage;
			}
			// This state check is not necessary for MidiPlayer, 
			// but for JetPlayer (which does not expose state). 
			if (midi_player.State != PlayerState.Playing) {
				midi_player.PlayAsync();
				media_player.Start();
			}
			DrawCommon ("PLAY");
		}

		internal void ProcessPause()
		{
			if (midi_player == null)
				return;
			midi_player.PauseAsync();
			if (media_player != null)
				media_player.Pause();
			DrawCommon ("PAUSE");
		}
		internal void ProcessStop()
		{
			if (midi_player == null)
				return;
			midi_player.Dispose();
			midi_player = null;
			if (media_player != null) {
				media_player.Stop();
				media_player = null;
			}
			DrawCommon ("STOP");
		}
		internal void ProcessFastForward()
		{
			DrawCommon ("not supported yet");
		}
		internal void ProcessRewind()
		{
			DrawCommon ("not supported yet");
		}

		internal void ProcessLoad()
		{
			Intent intent = new Intent("org.openintents.action.PICK_FILE");
			activity.StartActivityForResult(intent, 1);
		}

		//MidiPlayerCallback callback = this;
		SmfMusic smf_music;
		FileInfo jetfile;

		internal void LoadFileAsync(FileInfo file)
		{
			FileInfo prevfile = midifile;
			if (file == prevfile || file == null)
				return;
			midifile = file;
			new Thread ((ThreadStart) delegate {
				try {
					DrawCommon ("Loading " + midifile.Name);
					UpdateView();
					jetfile = midifile;
					using (var fs = File.OpenRead(midifile.FullName))
					{
						SmfReader r = new SmfReader(fs);
						r.Parse();
						smf_music = r.Music;
						smf_music = SmfTrackMerger.Merge(smf_music);
						DrawCommon("Loaded");
					}
				} catch (SmfParserException ex) {
					DrawCommon ("Parse error " + ex);
				} catch (IOException ex) {
					DrawCommon ("I/O error " + ex);
				}
			}).Start();
		}

		void StopViews()
		{
			// initialize keyboard
			for (int i = 0; i < channels; i++)
				this.SetupKeyboard(i);
		}
		
		void DrawNoteOnOff(SmfMessage m)
		{
			int note = GetKeyIndexForNote (m.Msb);
			if (note < 0)
				return; // out of range
			int octave = note / 12;
			int key = note % 12;
			int channel = m.Channel;
			bool isKeyOn = m.MessageType == SmfMessage.NoteOn && m.Lsb != 0;

			float x = octave * key_width * 7;
			float y = GetChannelYPos (channel) + ch_height - key_height;
			int k = key_to_keyboard_idx [key];
			if (IsWhiteKey (key)) {
				paint.Color = (isKeyOn ? color_keyon : color_white_key);
				canvas.DrawCircle(x + k * key_width + 3, y + 12, 2, paint);
				//keyon_meter_panel.ProcessKeyOn (m.Channel, m.Msb, m.Lsb);
				//spectrum_analyzer_panel.ProcessKeyOn (m.Channel, m.Msb, m.Lsb);
			} else {
				paint.Color = (isKeyOn ? color_keyon : color_black_key);
				int blackKeyStartX = (int) (x + (k + 0.8) * key_width);
				canvas.DrawCircle(blackKeyStartX + 2, y + 1 + 5, 1, paint);
			}
			needs_redraw = true;
		}

		void HandleSmfMessage(SmfMessage m)
		{
			switch (m.MessageType) {
			case SmfMessage.NoteOn:
			case SmfMessage.NoteOff:
				DrawNoteOnOff (m);
				break;
				/*
			case SmfMessage.Program:
				keyon_meter_panel.SetProgram (m.Channel, m.Msb);
				break;
			case SmfMessage.CC:
				switch (m.Msb) {
				case SmfCC.BankSelect:
					keyon_meter_panel.SetBank (m.Channel, m.Lsb, true);
					break;
				case SmfCC.BankSelectLsb:
					keyon_meter_panel.SetBank (m.Channel, m.Lsb, false);
					break;
				case SmfCC.Pan:
					keyon_meter_panel.SetPan (m.Channel, m.Lsb);
					break;
				case SmfCC.Volume:
					parameter_visualizers [m.Channel].Volume.SetValue (m.Lsb);
					break;
				case SmfCC.Expression:
					parameter_visualizers [m.Channel].Expression.SetValue (m.Lsb);
					break;
				case SmfCC.Rsd:
					parameter_visualizers [m.Channel].Rsd.SetValue (m.Lsb);
					break;
				case SmfCC.Csd:
					parameter_visualizers [m.Channel].Csd.SetValue (m.Lsb);
					break;
				case SmfCC.Hold:
					parameter_visualizers [m.Channel].Hold.Value = (m.Lsb > 63);
					if (m.Lsb < 64 && key_rectangles != null) { // reset held keys to nothing
						for (int i = 0; i < 128; i++) {
							note = getKeyIndexForNote (i);
							if (note < 0)
								continue;
							var rect = key_rectangles [m.Channel, note];
							if (rect == null)
								continue;
							if (((SolidColorBrush) rect.Fill).Color == color_hold)
								key_rectangles [m.Channel, note].Fill = IsWhiteKey (i) ? brush_white_key : brush_black_key;
						}
					}
					break;
				case SmfCC.PortamentoSwitch:
					parameter_visualizers [m.Channel].PortamentoSwitch.Value = (m.Lsb > 63);
					break;
				case SmfCC.Sostenuto:
					parameter_visualizers [m.Channel].Sostenuto.Value = (m.Lsb > 63);
					break;
				case SmfCC.SoftPedal:
					parameter_visualizers [m.Channel].SoftPedal.Value = (m.Lsb > 63);
					break;
				}
				break;
			case SmfMessage.Meta:
				switch (m.MetaType) {
				case SmfMetaType.TimeSignature:
					play_time_status_panel.SetTimeMeterValues (m.Data);
					break;
				case SmfMetaType.Tempo:
					foreach (var view in player_status_views)
						view.ProcessChangeTempo ((int) ((60.0 / SmfMetaType.GetTempo (m.Data)) * 1000000.0));
					break;
				}
				break;
				*/
			}
		}
	}

	[Activity(MainLauncher = true)]
	public class NmdspMainActivity : Activity
	{
		NmdspMainView view;

		public NmdspMainActivity ()
		{
		}

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			view = new NmdspMainView(this);
			SetContentView(view);
		}

		protected override void OnPause()
		{
			base.OnPause();
			view.ProcessPause();
		}

		protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
		{
			base.OnActivityResult(requestCode, resultCode, data);
			if (resultCode == Result.Ok && data != null)
			{
				String filename = data.DataString;
				if (filename == null)
					return;
				if (filename.StartsWith("file://"))
					filename = filename.Substring(7); // remove URI prefix

				this.view.LoadFileAsync(new FileInfo(filename));
			}
		}
	}

	class ParameterVisualizerPanel : BitmapDrawable
	{
		public ParameterVisualizerPanel ()
		{
			paint = new Paint ();
			paint.TextSize = 7;
			paint.Color = unchecked ( (int) 0xFF60A0FF );
			paint.SetStyle(Paint.Style.Fill);
		}
		
		public Point location;
		public Paint paint;
	}

	class PlayerStatusPanel : BitmapDrawable
	{
		public PlayerStatusPanel ()
		{
		}
	}
	
	abstract class VisualItem : BitmapDrawable
	{
	}

	class NumericVisualItem : VisualItem
	{
	}

	class PlayTimeStatusPanel : BitmapDrawable
	{
		public PlayTimeStatusPanel ()
		{
		}
	}
}

