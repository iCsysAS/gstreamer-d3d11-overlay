using System;
using System.Linq;
using GLib;
using Gst;
using Gst.Video;
using Thread = System.Threading.Thread;

namespace GStreamerD3D.Samples.WPF.D3D11 {
	public class Playback {
		private static readonly log4net.ILog _log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public delegate void OnDrawReceivedEventHandler(Element videoSink, GLib.SignalArgs args);

		private Pipeline _pipeline;
		private MainLoop _mainLoop;
		private Thread _mainGlibThread;
		private Element _uriDecodeBin, _depay, _avdec, _audioConvert, _videoConvert;
		private Element _audioSink, _videoSink;
		private VideoOverlayAdapter _adapter;
		private IntPtr _handle;

		private const bool _enableAudio = false;
		private const string _videoDecoder = "d3d11h264dec"; // This decoder will reduce CPU usage significantly

		private bool _enableOverlay;
		private bool _rtsp;
		private string _pipelineString;
		private string _source = "rtsp://10.13.37.243/live_stream";

		public static string udpPipelineString = "udpsrc port=5600 ! application/x-rtp, media=(string)video, clock-rate=(int)90000, encoding-name=(string)H264, payload=(int)96  ! rtph264depay ! avdec_h264 max-threads=12 ! d3d11videosink name=videosink";

		public Playback(IntPtr hwnd, bool rtsp = true, string pipeline = "", string gstDebug = "") {
			_handle = hwnd;
			_enableOverlay = true;
			_rtsp = rtsp;
			_pipelineString = pipeline;

			if (!String.IsNullOrEmpty(gstDebug)) {
				Environment.SetEnvironmentVariable("GST_DEBUG", gstDebug);
			}

			InitGst();

			if (String.IsNullOrEmpty(_pipelineString))
            {
				CreatePipeline();
            }
            else
            {
				CreatePipelineFromString();
			}

			var ret = _pipeline.SetState(State.Playing);

			if (ret == StateChangeReturn.Failure) {
				Log("Unable to set the pipeline to the playing state.", LogLevelFlags.Error);
				return;
			}
		}

		public OnDrawReceivedEventHandler OnDrawSignalReceived;

		public void InitGst() {
			Gst.Application.Init();
			GtkSharp.GstreamerSharp.ObjectManager.Initialize();
			_mainLoop = new MainLoop();
			_mainGlibThread = new Thread(_mainLoop.Run);
			_mainGlibThread.Start();
		}

		private void CreatePipeline() {
			Log("Initializing Pipeline..", LogLevelFlags.Debug);
			_pipeline = new Pipeline("pipeline0");
            _pipeline.Bus.EnableSyncMessageEmission();
            _pipeline.Bus.AddSignalWatch();
			_pipeline.Bus.Message += OnBusMessage;
			_pipeline.AutoFlushBus = true;

			if (!_enableOverlay) {
				_pipeline.Bus.SyncMessage += OnBusSyncMessage;
			}

			CreateElements();


			if (_enableAudio) {
				_pipeline.Add(_uriDecodeBin, _audioConvert, _videoConvert, _audioSink, _videoSink);

				if (!_audioConvert.Link(_audioSink)) {
					Log("Audio sink could not be linked", LogLevelFlags.Error);
					return;
				}
			}
			else {
				_pipeline.Add(_uriDecodeBin, _depay, _avdec, _videoConvert, _videoSink);
			}

			if (!_videoConvert.Link(_videoSink)) {
				Log("Video sink could not be linked", LogLevelFlags.FlagFatal);
				return;
			}

			if (!_rtsp) {
				_uriDecodeBin.Link(_depay);
				_depay.Link(_avdec);
				_avdec.Link(_videoConvert);
				_videoConvert.Link(_videoSink);
			}
		}

		private void CreatePipelineFromString()
		{

			var pipeline = Parse.Launch(_pipelineString);
			_pipeline = pipeline as Pipeline;

			foreach (Element el in _pipeline.IterateElements())
			{
				if (el.Name == "videosink")
				{
					el["draw-on-shared-texture"] = true;
					el.Connect("begin-draw", VideoSink_OnBeginDraw);
				}
			}
			Console.WriteLine(_pipeline);

			_pipeline.Bus.Message += OnBusMessage;
			_pipeline.AutoFlushBus = true;

			if (!_enableOverlay)
			{
				_pipeline.Bus.SyncMessage += OnBusSyncMessage;
			}
		}

		private void OnPadAdded(object sender, PadAddedArgs args) {
			var src = (Element)sender;
			var newPad = args.NewPad;

			var newPadCaps = newPad.CurrentCaps;
			var newPadStruct = newPadCaps.GetStructure(0);
			var newPadType = newPadStruct.Name;

			if (newPadType.StartsWith("audio/x-raw")) {
				Pad sinkPad = _audioConvert.GetStaticPad("sink");
				Log($"Received new pad '{newPad.Name}' from '{src.Name}':", LogLevelFlags.Debug);

				if (sinkPad.IsLinked) {
					Log("We are already linked, ignoring", LogLevelFlags.Warning);
					return;
				}

				var ret = newPad.Link(sinkPad);

				if (ret != PadLinkReturn.Ok) {
					Log($"Type is {newPadType} but link failed", LogLevelFlags.Error);
				}

				sinkPad.Dispose();
			}
			else if (newPadType.StartsWith("video/x-raw")) {
				Pad sinkPad = _videoConvert.GetStaticPad("sink");
				Log($"Received new pad '{newPad.Name}' from '{src.Name}':", LogLevelFlags.Debug);

				if (sinkPad.IsLinked) {
					Log("We are already linked, ignoring", LogLevelFlags.Warning);
					return;
				}

				var ret = newPad.Link(sinkPad);

				if (ret != PadLinkReturn.Ok) {
					Log($"Type is {newPadType} but link failed", LogLevelFlags.Error);
				}

				sinkPad.Dispose();
			}
			else {
				var success = newPad.Link(_depay.GetStaticPad("sink"));
				Log($"Link: {success}", LogLevelFlags.Message);

				Log($"Linking depay: {_depay.Link(_avdec)}", LogLevelFlags.Message);

				Log($"Linking _avdec: {_avdec.Link(_videoConvert)}", LogLevelFlags.Message);


				//				Log($"It has type '{newPadType}' which is not raw audio or video. Ignoring.", LogLevelFlags.Debug);
			}

			newPadCaps.Dispose();
			newPadStruct.Dispose();
			newPad.Dispose();
		}

		private void OnBusSyncMessage(object o, SyncMessageArgs sargs) {
			Message msg = sargs.Message;

			if (!Gst.Video.Global.IsVideoOverlayPrepareWindowHandleMessage(msg)) {
				msg.Dispose();
				return;
			}

			Element src = msg.Src as Element;

			if (src == null) {
				msg.Dispose();
				return;
			}

			try {
				src["force-aspect-ratio"] = true;
			}
			catch (PropertyNotFoundException ex) {
				Console.WriteLine(ex.Message, ex);
			}

			Element overlay = (_pipeline)?.GetByInterface(VideoOverlayAdapter.GType);

			_adapter = new VideoOverlayAdapter(overlay.Handle);
			_adapter.WindowHandle = _handle;
			_adapter.HandleEvents(true);

			msg?.Dispose();
			overlay?.Dispose();
			src?.Dispose();
		}

		private static bool FilterVisFeatures(PluginFeature feature) {
			if (!(feature is ElementFactory)) {
				return false;
			}

			var factory = (ElementFactory)feature;

			return (factory.GetMetadata(Gst.Constants.ELEMENT_METADATA_KLASS).ToLower().Contains("codec/decoder")) || (factory.GetMetadata(Gst.Constants.ELEMENT_METADATA_KLASS).ToLower().Contains("sink/video"));
		}
		private void SetPrimaryDecoder(string decoderName) {
			var registry = Gst.Registry.Get();
			var pluginList = registry.FeatureFilter(FilterVisFeatures, false);

			// First we find the current primary decoders
			var primaryPlugins = pluginList.Where(plugin => plugin.Rank == (uint)Rank.Primary);

			// And then downrank them 
			foreach (var plugin in primaryPlugins) {
				--plugin.Rank;
			}

			// Then we set our plugin to the primary rank
			PluginFeature d3d11Plugin = pluginList.FirstOrDefault(plugin => plugin.Name == decoderName);

			if (d3d11Plugin != null) {
				d3d11Plugin.Rank = (uint)Rank.Primary;
			}
		}
		protected bool CreateElements() {
			try {
				_videoSink = ElementFactory.Make("d3d11videosink", "d3d11videosink0");

				if (_enableOverlay) {
					_videoSink["draw-on-shared-texture"] = true;
					_videoSink.Connect("begin-draw", VideoSink_OnBeginDraw);
				}

				//SetPrimaryDecoder(_videoDecoder);

				_videoConvert = ElementFactory.Make("videoconvert", "videoconvert0");

				if (_videoSink == null) {
					Log($"Could not locate Direct3D11", LogLevelFlags.Error);
					_videoSink = ElementFactory.Make("autovideosink", "autovideosink0");
				}

				if (_enableAudio) {
					_audioConvert = ElementFactory.Make("audioconvert", "audioConvert");
					_audioSink = ElementFactory.Make("autoaudiosink", "audioSink");
				}
				if (_rtsp) {
					_uriDecodeBin = ElementFactory.Make("rtspsrc", "source");
					_uriDecodeBin["location"] = _source;
					_uriDecodeBin["latency"] = 0;

					_depay = ElementFactory.Make("rtph264depay", "depay");
					Log($"Linking rtspsrc: {_uriDecodeBin.Link(_depay)}", LogLevelFlags.Message);

					_avdec = ElementFactory.Make("avdec_h264", "avdec");
					//_avdec.PadAdded += OnPadAdded;

					_uriDecodeBin.PadAdded += OnPadAdded;
                }
                else {
					_uriDecodeBin = ElementFactory.Make("udpsrc", "source");
					_uriDecodeBin["port"] = 5600;
					var caps = Gst.Global.CapsFromString("application/x-rtp, media=(string)video, clock-rate=(int)90000, encoding-name=(string)H264, payload=(int)96");
					_uriDecodeBin["caps"] = caps;

					var acaps = _uriDecodeBin.GetStaticPad("src").AllowedCaps;

					_depay = ElementFactory.Make("rtph264depay", "depay");

					_avdec = ElementFactory.Make("avdec_h264", "avdec");
					_avdec["max-threads"] = 12;
				}
			}
			catch (Exception ex) {
				Log(ex.ToString(), LogLevelFlags.Critical, ex);
				return false;
			}

			return true;
		}

		private void OnBusMessage(object o, MessageArgs margs) {
			try {

				Message message = margs.Message;

				switch (message.Type) {
					case MessageType.Eos:

						Log("Replaying stream...", LogLevelFlags.Info);

						var ret = _pipeline.SetState(Gst.State.Ready);

						if (ret == StateChangeReturn.Async)
							ret = _pipeline.GetState(out var state, out var pending, Gst.Constants.SECOND * 10L);

						if (ret == StateChangeReturn.Success) {
							ret = _pipeline.SetState(Gst.State.Playing);
							if (ret == StateChangeReturn.Async)
								ret = _pipeline.GetState(out var state, out var pending, Gst.Constants.SECOND * 10L);
						}
						break;
					case MessageType.Error:
						message.ParseError(out GException err, out string debug);
						if (debug == null) {
							debug = "none";
						}
						Log($"Error! Bus message: {debug}", LogLevelFlags.Error, err);
						if (err.Code == 3) {
							System.Windows.Application.Current.Dispatcher.InvokeShutdown();
						}
						break;
				}

			}
			catch (Exception ex) {
				Log("Bus message error.", LogLevelFlags.Error, ex);
			}
		}

		private void VideoSink_OnBeginDraw(object o, GLib.SignalArgs args) {
			OnDrawSignalReceived?.Invoke((Element)o, args);
		}

		public void Log(string message, LogLevelFlags logLevel, Exception exception = null) {
			try {
				switch (logLevel) {
					case LogLevelFlags.Debug:
						_log.Debug(message, exception);
						break;

					case LogLevelFlags.Info:
						_log.Info(message, exception);
						break;

					case LogLevelFlags.Warning:
						_log.Warn(message, exception);
						break;

					case LogLevelFlags.Error:
						_log.Error(message, exception);
						break;

					case LogLevelFlags.FlagFatal:
						_log.Fatal(message, exception);
						break;

					default:
						_log.Info(message, exception);
						break;
				}
			}
			catch (Exception ex) {
				_log.Error($"Log error: {ex.Message}", ex);
			}
		}

		internal void Cleanup() {
			try {
				Log("Cleaning up pipeline..", LogLevelFlags.Info);

				_adapter?.HandleEvents(false);
				_videoSink?.Disconnect("begin-draw", VideoSink_OnBeginDraw);

				if (_pipeline != null) {
					if (_pipeline.Bus != null)
                    {
						_pipeline.Bus.Message -= OnBusMessage;
						_pipeline.Bus.SyncMessage -= OnBusSyncMessage;
                    }
				}

				if (_uriDecodeBin != null) {
					_uriDecodeBin.PadAdded -= OnPadAdded;
				}

				_pipeline?.SetState(State.Null);
				_pipeline?.Dispose();
				_mainLoop?.Quit();

				GC.Collect();
				GC.WaitForPendingFinalizers();
			}
			catch (Exception ex) {
				Log("Failed to cleanup resources", LogLevelFlags.Error, ex);
			}
		}
	}
}