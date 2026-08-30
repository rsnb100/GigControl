using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TelldusWrapper;
using CannedBytes.Midi;
using Rug.Osc;
using System.Threading;


namespace DMXServer
{
    public partial class MainForm : Form
    {

        #region Globals

        delegate void SetTextCallback(string text);
        MidiReceiver receiver;
        MidiOutPort midiOutPort;
        public VComWrapper dmxOutputDevice;
        public List<XElement> midiMappings;
        public List<XElement> xOscMappings = new List<XElement>();
        List<xFixture> xFixtures = new List<xFixture>();
        List<xPatch> xPatches = new List<xPatch>();
        List<xRewrite> xRewrites = new List<xRewrite>();
        public List<xScene> xScenes = new List<xScene>();
        public List<xChase> xChases = new List<xChase>();
        public int maxChannels = 300;
        public byte[] LiveLevels;
        public byte[] OverrideLevels;
        public List<ControllerState> controllerStates = new List<ControllerState>();
        List<OscControllerState> oscControllerStates = new List<OscControllerState>();
        int timerInterval = 10;
        public List<Label> dmxLabels = new List<Label>();
        public DMXLevelsForm form2;
        public bool dmxRunning = false;
        string fixtureFile = "fixtures.xml";
        string patchFile = "patches.xml";
        public string midiMappingsFile = "midimappings.xml";
        string oscMappingsFile = "oscmappings*.xml";
        public string webMappingsFile = "webmappings.xml";
        string sceneFile = "scenes*.xml";
        string chaseFile = "chases*.xml";
        public int tempo = 300;
        public DateTime lastTap = DateTime.MinValue;
        public ArtNet.Engine ArtEngine;
        string artNetBroadcastIP = "172.17.201.255";
        bool enableArtNet = false;
        static OscReceiver oscReceiver;
        static Thread oscThread;
        public int oscPort = 8000;
        List<string> vlcIPs = new List<string>() { "192.168.1.198" };
        string X32IP = "192.168.1.32";
        int X32Port = 10024;
        int X32TimeFactor = 5;
        public uDMX udmx = new uDMX();
        SceneForm sceneForm;
        DMXController dmxController;
        bool debug = true;
        int x32TimerInterval = 500;
        bool hazeActive = true;
        int hazeStep = 0;
        string hazeLevelAddress = "/1/haze";
        string hazeTimeAddress = "/1/hazetime";
        bool oscResendLoopback = true;
        List<string> vlcExtensions = new List<string>() { "*.mov", "*.mp4", "*.mpg" };
        List<VlcFileInfo> vlcFiles = new List<VlcFileInfo>();
        string vlcBrowsePath = @"e$";
        string vlcSelectedFile = "";
        MidiMappingsForm midiForm;
        string ReaperIP = "127.0.0.1";
        string ReaperPort = "8001";
        IPAddress ReaperControlIp = IPAddress.Parse("192.168.1.229");
        int ReaperControlPort = 9000;
        IPAddress OSCLoopbackIP = IPAddress.Parse("192.168.1.30");
        int OSCLoopbackPort = 7700;
        List<IPAddress> OscClients = new List<IPAddress>();
        public List<xState> xStates = new List<xState>();
        public string statesFile = "states.xml";
        public OscMessage lastOscMessage;
        List<AllOffTime> AllOffTimes = new List<AllOffTime>();
        bool BaseSceneActive = false;
        string BaseScene = null;
        DateTime x32TapLast = DateTime.MinValue;
        public string currentShow = "";
        string textBuffer = "";
        bool storeState = false;
        int selectedState = 0;
        bool assignState = false;
        bool unassignState = false;
        bool shift = false;
        bool caps = false;
        //OpenDMX od = new OpenDMX();
        
        #endregion Globals


        #region Entry points

        public MainForm()
        {
            InitializeComponent();

            LiveLevels = new byte[maxChannels];
            OverrideLevels = new byte[maxChannels];
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetupForm();
            ReadAllFiles();
            if (enableArtNet)
                StartArtNet();

            cbBaseScene.Items.Clear();
            foreach (var scene in xScenes.OrderBy(a => a.name))
            {
                cbBaseScene.Items.Add(scene.name);
            }

            cbBaseScene.SelectedItem = BaseScene;
            cbBaseSceneActive.Checked = BaseSceneActive;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            try
            {

                SaveSettings(currentShow);

                StopMidi();
                StopDMX();
                StopOSC();
                udmx.Dispose();
                StopArtNet();
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        // Detect all numeric characters at the form level and consume 1, 
        // 4, and 7. Note that Form.KeyPreview must be set to true for this
        // event handler to be called.
        void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            MessageBox.Show("Form.KeyPress: '" +
                    e.KeyChar.ToString() + "' pressed.");

            if (e.KeyChar >= 48 && e.KeyChar <= 57)
            {
                MessageBox.Show("Form.KeyPress: '" +
                    e.KeyChar.ToString() + "' pressed.");

                switch (e.KeyChar)
                {
                    case (char)49:
                    case (char)52:
                    case (char)55:
                        MessageBox.Show("Form.KeyPress: '" +
                            e.KeyChar.ToString() + "' consumed.");
                        e.Handled = true;
                        break;
                }
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            StartMidi();
            StartDMX();
            StartOSC();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopMidi();
            StopDMX();
            StopOSC();
        }

        private void btnReload_Click(object sender, EventArgs e)
        {
            Reload();
        }

        private void ddlShows_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (currentShow == ddlShows.SelectedItem.ToString())
                return;

            SaveSettings(currentShow);

            currentShow = ddlShows.SelectedItem.ToString();

            StopMidi();
            StopDMX();
            StopOSC();

            LoadSettings(currentShow);
            Reload();

            //StartMidi();
            //StartDMX();
            //StartOSC();

            SaveGlobalSettings();
        }

        private void btnOutputs_Click(object sender, EventArgs e)
        {
            ShowLevels();
        }

        private void btnScenes_Click(object sender, EventArgs e)
        {
            if (sceneForm != null)
                if (sceneForm.Visible)
                    return;

            sceneForm = new SceneForm(this);
            sceneForm.Show();

        }

        private void btnController_Click(object sender, EventArgs e)
        {
            StopOSC();
            if (dmxController != null)
                if (dmxController.Visible)
                    return;

            //Reload();

            dmxController = new DMXController(this);
            dmxController.FormClosed += DmxController_FormClosed;
            dmxController.Show();
            //StartOSC();
        }

        private void btnStates_Click(object sender, EventArgs e)
        {
            var StatesForm = new StatesForm(this);
            StatesForm.ShowDialog();
        }

        private void DmxController_FormClosed(object sender, FormClosedEventArgs e)
        {
            StartOSC();
        }

        #endregion Entry points


        #region DMX

        private void StartArtNet()
        {
            try
            {
                ArtEngine = new ArtNet.Engine("MidiDMX", "");
                ArtEngine.BroadcastAddress = artNetBroadcastIP;
                ArtEngine.Start();
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void StopArtNet()
        {

            if (ArtEngine != null && ArtEngine.Running)
                ArtEngine.Pause();
        }

        private void timerDMX_Tick(object sender, EventArgs e)
        {
            try {
                if (dmxController != null && dmxController.Visible)
                    return;

                ResetLiveLevels();

                // add base scene here
                if (cbBaseSceneActive.Checked && cbBaseScene.SelectedItem != null)
                    RunScene((string)cbBaseScene.SelectedItem, 1);

                foreach (var scene in xScenes)
                {
                    if (scene.active)
                        RunScene(scene.name, 1);
                }

                foreach (var chase in xChases)
                {
                    if (chase.active)
                        RunChase(chase.name);
                }

                foreach (var state in xStates)
                {
                    if (state.active)
                        RunState(state.name);
                }

                ProgRunScene();

                SetMidiControllers();
                SetOscControllers();

                SetOverrideLevels();

                SendDMX();
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

        }

        private void SendDMX()
        {
            if (form2 != null)
            {
                for (int i = 0; i < maxChannels; i++)
                    dmxLabels[i].Text = LiveLevels[i].ToString();
            }

            if (ArtEngine != null && ArtEngine.Running)
                ArtEngine.SendDMX(0, LiveLevels, LiveLevels.Length);

            if (cbUDMX.Checked)
                SendUDMX();

            //if (od.status == FT_STATUS.FT_OK)
            //{
            //    od.buffer = LiveLevels;
            //    od.writeData();
            //}

            if (dmxOutputDevice == null) return;
            if (dmxOutputDevice.IsOpen == false) return;
            if (dmxOutputDevice.m_port.BytesToWrite > 0) return;

            if (dmxRunning)
            {
                dmxOutputDevice.sendDMXPacketRequest(LiveLevels);
            }



        }

        public void SendUDMX()
        {

            if (udmx.IsOpen)
            {
                udmx.SetChannelRange(0, LiveLevels);
            }
        }

        private void StartDMX()
        {
            if (dmxOutputDevice.initPro((string)cbDmx.SelectedItem))
                dmxOutputDevice.sendGetWidgetParametersRequest((ushort)0);

            //try
            //{
            //    od.start();                                            //find and connect to device (first found if multiple)
            //    if (od.status == FT_STATUS.FT_DEVICE_NOT_FOUND)       //update status
            //        OutputText("No Enttec Open USB Device Found");
            //    else if (od.status == FT_STATUS.FT_OK)
            //        OutputText("Found Open DMX on USB");
            //    else
            //        OutputText("Error Opening Open DMX Device");
            //}
            //catch (Exception exp)
            //{
            //    OutputText(exp.Message);
            //    OutputText("Error Connecting to Enttec Open USB Device");

            //}


            timerDMX.Interval = timerInterval;
            //if ((string)cbDmx.SelectedItem != "DMX Disabled")
                timerDMX.Enabled = true;

            cbDmx.Enabled = false;

        }

        private void StopDMX()
        {
            if (dmxOutputDevice == null) return;
            if (dmxOutputDevice.IsOpen)
            {
                dmxOutputDevice.detatchPro();
                OutputText("** DMX closed. **");
            }

            //try
            //{
            //    od.stop();
            //    OutputText("Open DMX stopped");
            //}
            //catch (Exception ex)
            //{
            //    OutputText("Error stopping Open DMX: " + ex.Message);
            //}



            timerDMX.Enabled = false;

            cbDmx.Enabled = true;
            dmxRunning = false;
        }

        void dmx_WidgetParametersReceived(object sender, WidgetParameterArgs e)
        {
            OutputText("** Enttec DMX connected. **");
            dmxRunning = true;
        }

        void dmx_SerialNumberReceived(object sender, SerialNumberArgs e)
        {
            throw new NotImplementedException();
        }

        public void ResetLiveLevels()
        {
            for (int i = 0; i < LiveLevels.Count(); i++)
                LiveLevels[i] = 0;
        }

        private void SetOverrideLevels()
        {
            for (int i = 0; i < OverrideLevels.Count(); i++)
                if (OverrideLevels[i] > 0)
                    LiveLevels[i] = OverrideLevels[i];
        }

        private void RunScene(string sceneName, double multiplier)
        {
            try
            {
                var scene = (from S in xScenes
                             where S.name == sceneName
                             select S).First();

                foreach (var function in scene.functions)
                {
                    try
                    {
                        string patchName = function.patch;
                        string functionName = function.function;
                        byte value = function.value;

                        value = (byte)(value * multiplier);

                        int dmxChannel = channelFromFunction(patchName, functionName).Value;

                        if (functionName.ToLower().Contains("pan") || functionName.ToLower().Contains("tilt"))
                            LiveLevels[dmxChannel] = value;
                        else
                            LiveLevels[dmxChannel] = System.Math.Max(value, LiveLevels[dmxChannel]);


                    }
                    catch (Exception ex)
                    {
                        OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void RunState(string stateName)
        {
            var state = xStates.First(a => a.name == stateName);

            if (state.tempo != null)
                tempo = 60000 / state.tempo.Value;

            foreach (var scene in state.scenes)
                RunScene(scene, 1);

            foreach (var chase in state.chases)
                RunChase(chase);
        }

        int? channelFromFunction(string patchName, string functionName)
        {
            try
            {
                int dmxChannel = 0;

                var patch = (from P in xPatches
                             where P.name == patchName
                             select P).First();

                int address = patch.address;

                var fixture = patch.fixture;

                var channel = (from C in fixture.channels
                               where C.function == functionName
                               select C).Single();


                int offset = channel.offset;
                dmxChannel = address + offset - 1;

                return dmxChannel;
            }
            catch
            {
                return null;
            }
        }

        private void RunChase(string chaseName)
        {
            try
            {
                DateTime now = DateTime.Now;

                var chase = (from C in xChases
                             where C.name == chaseName
                             select C).First();

                var steps = from S in chase.steps
                            select S;

                var step = steps.First();

                var chaseState = xChases.First(a => a.name == chaseName);

                if (chaseState.tap)
                    chaseState.hold = tempo;

                if (chaseState.lastChange == null)
                {
                    chaseState.lastChange = now;
                }
                else
                {
                    if (chaseState.lastChange.Value.AddMilliseconds(chaseState.hold) < now)
                    {
                        chaseState.lastChange = now;
                        chaseState.lastStep = chaseState.currentStep;
                        chaseState.currentStep++;
                        if (chaseState.currentStep >= chaseState.steps.Count())
                            chaseState.currentStep = 0;

                    }

                    step = steps.ElementAt(chaseState.currentStep);

                }

                if (chaseState.lastStep == null)
                {
                    RunStep(chaseState, step, now);
                }
                else
                {
                    xChaseStep lastStep = steps.ElementAt(chaseState.lastStep.Value);
                    RunNextStep(chaseState, step, lastStep, now);
                }
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void RunStep(xChase chaseState, xChaseStep step, DateTime now)
        {
            double multiplier = 1;
            if (chaseState.lastChange.Value.AddMilliseconds(chaseState.fadein) > now)
            {
                double difference = (now - chaseState.lastChange.Value).TotalMilliseconds;

                multiplier = difference / chaseState.fadein;
            }

            var functions = from F in step.functions
                            select F;

            foreach (var function in functions)
            {
                try
                {
                    string patchName = function.patch;
                    string functionName = function.function;
                    byte value = function.value;

                    value = (byte)(value * multiplier);

                    int dmxChannel = channelFromFunction(patchName, functionName).Value;

                    if (functionName.ToLower().Contains("pan") || functionName.ToLower().Contains("tilt"))
                        LiveLevels[dmxChannel] = value;
                    else
                        LiveLevels[dmxChannel] = System.Math.Max(value, LiveLevels[dmxChannel]);
                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }

            var scenes = from S in step.scenes
                         select S;

            foreach (string scene in scenes)
            {
                RunScene(scene, multiplier);
            }
        }

        private void RunNextStep(xChase chaseState, xChaseStep step, xChaseStep lastStep, DateTime now)
        {
            double multiplier = 1;
            if (chaseState.lastChange.Value.AddMilliseconds(chaseState.fadein) > now)
            {
                double difference = (now - chaseState.lastChange.Value).TotalMilliseconds;

                multiplier = difference / chaseState.fadein;
            }


            var functions = from F in step.functions
                            select F;

            var lastFunctions = from F in lastStep.functions
                                select F;

            foreach (var function in functions)
            {
                try
                {
                    string patchName = function.patch;
                    string functionName = function.function;
                    byte value = function.value;
                    byte lastValue = 0;
                    try
                    {
                        var lastFunction = lastFunctions.Where(a => a.patch == patchName && a.function == functionName);
                        if (lastFunction.Count() > 0)
                            lastValue = lastFunction.First().value;
                    }
                    catch (Exception ex)
                    {
                        OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    }

                    value = (byte)((value - lastValue) * multiplier + lastValue);

                    //lblDebug.Text = string.Format("{0} {1} {2}", lastValue, value, multiplier);

                    int dmxChannel = channelFromFunction(patchName, functionName).Value;

                    if (functionName.ToLower().Contains("pan") || functionName.ToLower().Contains("tilt"))
                        LiveLevels[dmxChannel] = value;
                    else
                        LiveLevels[dmxChannel] = System.Math.Max(value, LiveLevels[dmxChannel]);
                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }

            var scenes = from S in step.scenes
                         select S;

            foreach (string scene in scenes)
                RunScene(scene, multiplier);

            var lastScenes = from S in lastStep.scenes
                             select S;

            foreach (string lastScene in lastScenes)
                RunScene(lastScene, 1 - multiplier);
        }

        private void SetMidiControllers()
        {
            foreach (var controller in controllerStates)
            {
                if (controller.value == 0)
                    continue;

                var mappings = from M in midiMappings
                               where M.Elements("maptype").First().Value == "controller"
                               && M.Element("midichannel").Value == controller.midiChannel.ToString()
                               && M.Element("controller").Value == controller.controller.ToString()
                               select M;

                foreach (var mapping in mappings)
                {
                    try
                    {
                        int? dmxChannel = null;

                        XElement dmxChannelName = mapping.Element("dmxchannel");
                        XElement patch = mapping.Element("patch");
                        XElement function = mapping.Element("function");
                        XElement scene = mapping.Element("scene");

                        if (dmxChannelName != null)
                            dmxChannel = byte.Parse(dmxChannelName.Value);

                        if (patch != null && function != null)
                            dmxChannel = channelFromFunction(patch.Value, function.Value);

                        if (dmxChannel != null)
                        {
                            LiveLevels[dmxChannel.Value] = System.Math.Max((byte)(controller.value * 2.01), LiveLevels[dmxChannel.Value]);
                        }

                        if (scene != null)
                        {
                            double multiplier = controller.value / 127.0;
                            RunScene(scene.Value, multiplier);
                        }

                    }
                    catch (Exception ex)
                    {
                        OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    }
                }

            }
        }

        private void SetOscControllers()
        {
            foreach (var controller in oscControllerStates)
            {
                if (controller.value == 0)
                    continue;

                var mappings = from M in xOscMappings
                               where M.Elements("maptype").First().Value == "controller"
                               && M.Element("address").Value == controller.address
                               select M;

                foreach (var mapping in mappings)
                {
                    try
                    {
                        int? dmxChannel = null;

                        XElement dmxChannelName = mapping.Element("dmxchannel");
                        XElement patch = mapping.Element("patch");
                        XElement function = mapping.Element("function");
                        XElement scene = mapping.Element("scene");

                        if (dmxChannelName != null)
                            dmxChannel = byte.Parse(dmxChannelName.Value);

                        if (patch != null && function != null)
                            dmxChannel = channelFromFunction(patch.Value, function.Value);


                        // haze timer
                        if (controller.address == hazeTimeAddress)
                        {
                            int faderLevel = (int)(controller.value * 100);
                            hazeActive = (faderLevel > hazeStep);
                        }

                        if (controller.address == hazeLevelAddress && !hazeActive)
                            continue;
                        // end haze timer


                        if (dmxChannel != null)
                        {
                            LiveLevels[dmxChannel.Value] = System.Math.Max((byte)(controller.value * 255), LiveLevels[dmxChannel.Value]);
                        }

                        if (scene != null)
                        {
                            double multiplier = controller.value;
                            RunScene(scene.Value, multiplier);
                        }

                    }
                    catch (Exception ex)
                    {
                        OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    }
                }

            }
        }

        private void timerHaze_Tick(object sender, EventArgs e)
        {
            hazeStep++;
            if (hazeStep > 99)
                hazeStep = 0;
            //OutputText("Haze step:" + hazeStep);
        }

        private void SaveState(string stateName)
        {
            SaveState(stateName, null);
        }

        private void SaveState(string stateName, bool? exclusive)
        {
            xState state = xStates.FirstOrDefault(a => a.name == stateName);

            if (state == null)
            {
                state = new xState(stateName);
                xStates.Add(state);
            }

            state.scenes.Clear();
            var scenes = xScenes.Where(a => a.active);
            foreach (var scene in scenes)
                state.scenes.Add(scene.name);
            
            state.chases.Clear();
            var chases = xChases.Where(a => a.active);
            foreach (var chase in chases)
                state.chases.Add(chase.name);

            state.tempo = 60000 / tempo;

            if(exclusive.HasValue)
                state.exclusive = exclusive.Value;

        }

        #endregion DMX


        #region OSC

        private void StartOSC()
        {
            if (cbOSC.Checked)
            {
                try
                {
                    IPAddress ip = IPAddress.Parse("127.0.0.1");
                    try
                    {
                        var ips = GetIPv4Address().Where(a => a.AddressFamily == AddressFamily.InterNetwork);
                        ip = ips.Where(a => a.ToString().StartsWith("192.168.") || a.ToString().StartsWith("172.17.") || a.ToString().StartsWith("10.0.")).First();
                    }
                    catch (Exception ex)
                    {
                        OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    }

                    OutputText("OSC Receive IP: " + ip.ToString());
                    OutputText("OSC Port: " + oscPort.ToString());

                    // Create the receiver
                    oscReceiver = new OscReceiver(oscPort);

                    // Create a thread to do the listening
                    oscThread = new Thread(new ThreadStart(OscReceiveLoop));

                    // Connect the receiver
                    oscReceiver.Connect();

                    // Start the listen thread
                    oscThread.Start();

                    // wait for a key press to exit
                    OutputText("OSC Running...");

                    //udpClient = new UdpClient(oscPort);

                    timerX32.Interval = x32TimerInterval;
                    if (cbFeedback.Checked) timerX32.Enabled = true;

                    if (oscReceiver.State == OscSocketState.Connected)
                    {
                        OutputText("** OSC Receiving **");
                        OutputText("");

                    }
                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
        }

        private void StopOSC()
        {
            if (oscReceiver != null && oscReceiver.State == OscSocketState.Connected)
            {
                try
                {
                    oscReceiver.Close();
                    if (oscThread != null && oscThread.IsAlive)
                    {
                        if (!oscThread.Join(500))
                        {
                            try { oscThread.Interrupt(); } catch { }
                        }
                    }
                    OutputText("** OSC closed **");
                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
            timerX32.Enabled = false;
            //if (udpClient != null)
            //    udpClient.Close();
        }


        private void OscReceiveLoop()
        {
            try
            {
                while (oscReceiver.State != OscSocketState.Closed)
                {
                    try
                    {
                        // if we are in a state to recieve
                        if (oscReceiver.State == OscSocketState.Connected)
                        {

                            // get the next message 
                            // this will block until one arrives or the socket is closed
                            var packet = oscReceiver.Receive();

                            if (packet.GetType() == typeof(OscBundle))
                                ProcessBundle((OscBundle)packet);

                            //OutputText(packet.Origin.Address.ToString() + " " + message.Address + " " + message[0].ToString());

                            if (packet.GetType() == typeof(OscMessage))
                                ProcessMessage((OscMessage)packet);

                        }
                    }
                    catch (Exception ex)
                    {
                        OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                // if the socket was connected when this happens
                // then tell the user
                if (oscReceiver.State == OscSocketState.Connected)
                {
                    OutputText("Exception in listen loop - OSC Halted");
                    OutputText(ex.Message);
                }
            }
        }

        private void ProcessBundle(OscBundle bundle)
        {
            foreach (var message in bundle)
                ProcessMessage((OscMessage)message);
        }

        private void ProcessMessage(OscMessage message)
        {
            try
            {
                if (cbLoopback.Checked)
                    SendOSC(OSCLoopbackIP, OSCLoopbackPort, message);

                IPAddress sourceIP = message.Origin.Address;
                if (!OscClients.Contains(sourceIP) && sourceIP.ToString() != ReaperIP.ToString())
                {
                    OscClients.Add(sourceIP);
                    if (!message.Address.ToLower().StartsWith("/labels"))
                        SendOscLabels(sourceIP);
                    InitX32Faders();
                    OscAllOff(sourceIP);
                    //return;
                }

                //if (message.Origin.Address.ToString() == "192.168.1.118")
                //    OutputText(message.Address);

                if (message.Count < 1)
                    return;

                string address = message.Address;

                var rewrite = xRewrites.Where(a => a.address == address);
                if (rewrite.Count() == 1)
                {
                    address = rewrite.First().rewrite;
                    OutputText("Address rewrite: " + address);
                }

                if (cbMeters.Checked && address == "/meters/13")
                {
                    ProcessX32Meters(message);
                    return;
                }

                if (cbMeters.Checked && address == "/meters/1")
                {
                    ProcessXAirMeters(message);
                    return;
                }


                if (address.StartsWith("/Prog/"))
                {
                    float value2 = 0;
                    if (message.Count > 1)
                        value2 = float.Parse(message[1].ToString());
                    ProgReceive(address, message.Origin.Address, float.Parse(message[0].ToString()), value2);
                    return;
                }
                                
                if (address.StartsWith("/bus") && address.EndsWith("/on"))
                {
                    OutputText(string.Format("\nOSC Mix On Message Received [{0}]: {1} value: {2}", message.Origin.Address, address, message[0].ToString()));

                    if (message[0] is System.Int32)
                    {
                        if((int)message[0] == 1)
                        {
                            var split = address.Split('/');
                            var x = int.Parse(split[2]);
                            var y = int.Parse(split[4]);

                            if (x < 9)
                                return;

                            x = x - 8;

                            address = "/states/multi1/" + x.ToString() + "/" + y.ToString();
                            OutputText("Address rewrite: " + address);
                        }
                    }
                }

                if (address.StartsWith("/ch/") || address.StartsWith("/auxin/") || address.StartsWith("/fxrtn/") || address.Contains("/rtn/")) // X32 channel or aux input 
                {
                    if (debug) OutputText(string.Format("\nOSC Message Received [{0}]: {1}", message.Origin.Address, address));
                    if (address.EndsWith("/fader") || address.EndsWith("/level"))
                    {
                        if (message[0] is System.Single)
                            SendFader(address, (float)message[0]);
                        return;
                    }
                    //return;
                }

                if (address.ToLower().Contains("/reaper/") && (message[0] is System.Single || message[0] is System.Int32) && float.Parse(message[0].ToString()) == 1)
                {
                    SendReaper(address);
                    return;
                }

                if (address.StartsWith("/patch/"))
                {
                    ProcessPatch(message);
                    return;
                }

                if (address.Contains("/xy") && message.Count == 2)
                {
                    float xvalue = (float)message[1];
                    float yvalue = (float)message[1];

                    OutputText(string.Format("\nOSC Message Received [{0}]: {1}", message.Origin.Address, address));
                    OutputText(string.Format("OSC Message XY values: {0},{1}", xvalue, yvalue));

                    ProcessOsc(address + "/x", xvalue, message.Origin.Address);
                    ProcessOsc(address + "/y", yvalue, message.Origin.Address);

                    return;
                }

                ProcessReaperOutput(message);

                ProcessReaperControl(message);

                float value = 0;

                lastOscMessage = message;

                if ((message[0] is System.Single))
                    value = (float)message[0];
                else
                    float.TryParse(message[0].ToString(), out value);

                
                if (debug) OutputText(string.Format("\nOSC Message Received [{0}]: {1}", message.Origin.Address, address));
                if (debug) OutputText(string.Format("OSC Message value: {0}", value));

                if (address.StartsWith("/kb/"))
                {
                    ProcessKb(address, message.Origin.Address, value);
                    return;
                }

                ProcessOsc(address, value, message.Origin.Address);
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

        }

        private void ProcessPatch(OscMessage message)
        {
            var data = message.Address.Replace('_',' ').Split('/');

            var value = (float)message[0];

            var channel = (byte?)channelFromFunction(data[2], data[3]);

            if (channel.HasValue)
                OverrideLevels[channel.Value] = (byte)(value * 255);
        }

        private void ProcessReaperOutput(OscMessage message)
        {
            try
            {
                IPAddress sourceIP = message.Origin.Address;

                if (message.Count < 1)
                    return;

                string address = message.Address;

                IPAddress originIp = message.Origin.Address;

                if (originIp.ToString() != ReaperIP)
                    return;

                //if (address.EndsWith("/vu"))
                //{
                //    OutputText(string.Format("\nOSC Message Received [{0}]: {1}", message.Origin.Address, message.Address));
                //    OutputText(string.Format("OSC Message value: {0}", message[0].ToString()));
                //}

                if (address.StartsWith("/track") && address.EndsWith("/name") && address.Length <= 14)
                {
                    string track = address.Split('/')[2];

                    if (track != "name")
                    {

                        if (debug) OutputText(address + " " + track + " - Name: " + message[0]);

                        OscMessage labelMessage = new OscMessage("/rp/label/" + track, message[0]);

                        SendOSC(ReaperControlIp, ReaperControlPort, labelMessage);
                    }
                }

                if (address.StartsWith("/track") && address.EndsWith("/volume") && address.Length <= 16)
                {
                    string track = address.Split('/')[2];

                    if (track != "volume")
                    {
                        if (debug) OutputText(originIp.ToString() + " " + track + " - " + message[0]);

                        OscMessage faderMessage = new OscMessage("/rp/fader/" + track, (float)message[0]);

                        SendOSC(ReaperControlIp, ReaperControlPort, faderMessage);
                    }
                }

                if (address.StartsWith("/track") && address.EndsWith("/pan") && address.Length <= 13)
                {
                    string track = address.Split('/')[2];

                    if (track != "pan")
                    {
                        if (debug) OutputText(originIp.ToString() + " " + track + " - " + message[0]);

                        OscMessage faderMessage = new OscMessage("/rp/pan/" + track, (float)message[0]);

                        SendOSC(ReaperControlIp, ReaperControlPort, faderMessage);
                    }
                }

                if (address == "/master/volume")
                {
                    if (debug) OutputText(address + " Master " + " - volume: " + message[0]);

                    OscMessage labelMessage = new OscMessage("/rp/fader/master", message[0]);

                    SendOSC(ReaperControlIp, ReaperControlPort, labelMessage);
                }

                if (address.StartsWith("/track") && address.EndsWith("/volume/str") && address.Length <= 20)
                {
                    string track = address.Split('/')[2];

                    if (track != "volume")
                    {
                        if (debug) OutputText(address + " " + track + " - " + message[0]);

                        var level = message[0];

                        OscMessage faderMessage = new OscMessage("/rp/str/" + track, message[0].ToString());

                        SendOSC(ReaperControlIp, ReaperControlPort, faderMessage);
                    }
                }

                if (address == "/master/volume/str")
                {
                    if (debug) OutputText(address + " Master " + " - fader: " + message[0]);

                    OscMessage labelMessage = new OscMessage("/rp/str/master", message[0]);

                    SendOSC(ReaperControlIp, ReaperControlPort, labelMessage);
                }

                if (address.StartsWith("/track") && address.EndsWith("/vu"))
                {
                    string track = address.Split('/')[2];

                    if (track != "vu")
                    {
                        if (debug) OutputText(address + " " + track + " - " + message[0]);

                        var level = message[0];

                        OscMessage faderMessage = new OscMessage("/rp/vu/" + track, message[0].ToString());

                        SendOSC(ReaperControlIp, ReaperControlPort, faderMessage);
                    }
                }

                if (address == "/master/vu")
                {
                    if (debug) OutputText(address + " Master " + " - fader: " + message[0]);

                    OscMessage labelMessage = new OscMessage("/rp/vu/master", message[0]);

                    SendOSC(ReaperControlIp, ReaperControlPort, labelMessage);
                }

                if (address.StartsWith("/track") && address.EndsWith("/mute"))
                {
                    string track = address.Split('/')[2];

                    if (track != "mute")
                    {
                        if (debug) OutputText(address + " " + track + " - " + message[0]);

                        OscMessage faderMessage = new OscMessage("/rp/mute/" + track, message[0].ToString());

                        SendOSC(ReaperControlIp, ReaperControlPort, faderMessage);
                    }
                }

                if (address == "/time/str")
                {
                    OscMessage timeMessage = new OscMessage("/rp/time", message[0].ToString());

                    SendOSC(ReaperControlIp, ReaperControlPort, timeMessage);
                }

            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void ProcessReaperControl(OscMessage message)
        {
            try
            {
                IPAddress sourceIP = message.Origin.Address;

                if (message.Count < 1)
                    return;

                if (debug) OutputText(string.Format("\nOSC Message Received [{0}]: {1}", message.Origin.Address, message.Address));
                if (debug) OutputText(string.Format("OSC Message value: {0}", message[0].ToString()));

                string address = message.Address;

                if (!address.StartsWith("/rp/"))
                    return;

                if (address.StartsWith("/rp/fader/"))
                {
                    string track = address.Substring(10);

                    string strMessage = "/track/" + track + "/volume";

                    if (track == "master")
                        strMessage = "/master/volume";

                    TransmitOSC(strMessage, (float)message[0], ReaperPort, ReaperIP, true);
                }

                if (address.StartsWith("/rp/pan/"))
                {
                    string track = address.Substring(8);

                    string strMessage = "/track/" + track + "/pan";

                    if (track == "master")
                        return;

                    TransmitOSC(strMessage, (float)message[0], ReaperPort, ReaperIP, true);
                }

                if (address == "/rp/refresh")
                {
                    OutputText("Reaper /refresh");

                    TransmitOSC("/refresh", (float)1, ReaperPort, ReaperIP, true);
                }

            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void ProcessKb(string address, IPAddress clientIp, float value)
        {
            if (address.Contains("/char/") && value == 1)
            {
                if (textBuffer.Length >= 24)
                    return;

                string text = address.Substring(address.IndexOf("/char/") + 6);
                if (shift || caps)
                    textBuffer += text.ToUpper();
                else
                    textBuffer += text.ToLower();
                shift = false;
            }

            if (address.Contains("/cmd/"))
            {
                string text = address.Substring(address.IndexOf("/cmd/") + 5);
                OutputText(text);
                switch (text)
                {
                    case ("bksp"):
                        if (textBuffer.Length > 0 && value == 1)
                            textBuffer = textBuffer.Substring(0, textBuffer.Length - 1);
                        break;

                    case ("clear"):
                        if (value == 1)
                            textBuffer = "";
                        break;

                    case ("store"):
                        assignState = false;
                        unassignState = false;
                        storeState = !storeState;
                        break;

                    case ("assign"):
                        storeState = false;
                        unassignState = false;
                        assignState = !assignState;
                        break;

                    case ("unassign"):
                        storeState = false;
                        assignState = false;
                        unassignState = !unassignState;
                        break;

                    case ("shift"):
                        shift = !shift;
                        break;

                    case ("caps"):
                        caps = !caps;
                        break;

                    case ("down"):
                        if (selectedState > 0)
                            selectedState--;
                        break;

                    case ("down10"):
                        if (selectedState > 9)
                            selectedState = selectedState - 10;
                        else
                            selectedState = 0;
                        break;

                    case ("up"):
                        if (selectedState < xStates.Count - 1)
                            selectedState++;
                        break;

                    case ("up10"):
                        if (selectedState < xStates.Count - 11)
                            selectedState = selectedState + 10;
                        else
                            selectedState = xStates.Count - 1;
                        break;


                }
            }

            OutputText(textBuffer);
            OscMessage message = new OscMessage("/kb/display", textBuffer + "_");
            SendOSC(clientIp, 9000, message);
            SendOSC(clientIp, 9000, new OscMessage("/kb/state", xStates.OrderBy(a => a.name).ToList()[selectedState].name));
            SendOSC(clientIp, 9000, new OscMessage("/kb/cmd/store", (float)(storeState == true ? 1 : 0)));
            SendOSC(clientIp, 9000, new OscMessage("/kb/cmd/assign", (float)(assignState == true ? 1 : 0)));
            SendOSC(clientIp, 9000, new OscMessage("/kb/cmd/unassign", (float)(unassignState == true ? 1 : 0)));
            SendOSC(clientIp, 9000, new OscMessage("/kb/cmd/shift", (float)(shift == true ? 1 : 0)));
            SendOSC(clientIp, 9000, new OscMessage("/kb/cmd/caps", (float)(caps == true ? 1 : 0)));

        }

        private void SendReaper(string sendAddress)
        {
            OutputText("Reaper passthrough: " + sendAddress);
            TransmitOSC(sendAddress, (float)1.0, ReaperPort, ReaperIP, true);
        }

        private void SendFader(string x32address, float value) // send feedback message to osc device e.g. touchosc
        {
            var commands = xOscMappings.Where(a => a.Element("maptype").Value == "command");
            var allFaders = commands.Where(a => a.Element("command").Value == "x32-fader");
            var faders = allFaders.Where(a => a.Element("x32address").Value == x32address);

            foreach (var fader in faders)
            {
                string address = fader.Element("address").Value;
                string destIp = fader.Element("client-ip").Value;
                string port = fader.Element("client-port").Value;

                TransmitOSC(address, value, port, destIp,false);
                if (debug) OutputText(string.Format("SendFader ip:{0} address:{1} value:{2}", destIp, address, value));
            }
        }

        private void ProcessX32Meters(OscMessage message)
        {
            byte[] value = (byte[])message[0];

            List<OscPacket> messages = new List<OscPacket>();

            for (int i = 1; i <= 48; i++)
            {
                float myFloat = System.BitConverter.ToSingle(value, 4 * i);

                myFloat = ((float)System.Math.Log10((double)myFloat) + (float)2.5) / (float)2.5;

                if (debug) OutputText(String.Format("Float {0}: {1}", i, myFloat.ToString()));

                OscMessage m = new OscMessage("/x32/meter/" + i, myFloat);

                messages.Add(m);
            }

            OscBundle bundle = new OscBundle(DateTime.Now, messages.ToArray());

            SendOSC(ReaperControlIp, ReaperControlPort, bundle);
        }

        private void ProcessXAirMeters(OscMessage message)
        {
            byte[] value = (byte[])message[0];

            List<OscPacket> messages = new List<OscPacket>();

            for (int i = 1; i <= 26; i++)
            {
                int index = 2 * i + 2;

                short num = BitConverter.ToInt16(value, index);

                //float myFloat = ((float)System.Math.Log10((double)num) + (float)2.5) / (float)2.5;

                float myFloat = ((float)num + 16000) / 16000;

                if (debug) OutputText(String.Format("Float {0}: {1}", i, num.ToString()));
                //OutputText(String.Format("Int {0}: {1}", i, num.ToString()));

                OscMessage m = new OscMessage("/x32/meter/" + i, myFloat);

                messages.Add(m);
            }

            OscBundle bundle = new OscBundle(DateTime.Now, messages.ToArray());

            SendOSC(ReaperControlIp, ReaperControlPort, bundle);
        }

        public void ProcessOsc(string address, float value, IPAddress sourceIp)
        {

            ProcessOscControllers(address, value);

            var commandMappings = from M in xOscMappings
                                  where M.Element("maptype").Value == "command"
                                  && M.Element("address").Value == address
                                  select M;

            ProcessCommands(commandMappings, value, address, sourceIp);

            if (address.Contains("/states/") && value == 1)
                ProcessOscStates(address, sourceIp);

            //if (address.Contains("/auxin/01") && value == 1)
            //    ProcessOscStates(address, sourceIp);

            var allOff = AllOffTimes.Where(a => a.IPAddress.ToString() == sourceIp.ToString());
            if (allOff.Count() > 0 && value == 0)
            {
                if (allOff.First().DateTime > DateTime.Now.AddSeconds(-1))
                {
                    OutputText("AllOff Excluded " + sourceIp.ToString());
                    return;
                }
            }

            var sceneMappings = from S in xScenes
                                where S.oscMappings.Select(a => a.address).Contains(address)
                                select new xAction { name = S.name, action = S.oscMappings.First(a => a.address == address).action };

            ProcessScenes(sceneMappings, value);

            var chaseMappings = from C in xChases
                                where C.oscMappings.Select(a => a.address).Contains(address)
                                select new xAction { name = C.name, action = C.oscMappings.First(a => a.address == address).action };

            ProcessChases(chaseMappings, value);

        }

        private void UpdateState(string address, IPAddress sourceIp)
        {
            var assigned = xStates.Where(a => a.oscAddresses.Contains(address));

            if (assigned.Count() == 0)
            {
                OutputText("No state to update!");
                return;
            }

            string stateName = assigned.First().name;

            xStates.RemoveAll(a => a.name == textBuffer);

            SaveState(stateName);

            SaveStatesXML(currentShow);

            OutputText("State updated: " + stateName);
            SendOSC(sourceIp, 9000, new OscMessage("/kb/cmd/store", 0));
            storeState = false;
        }

        private void ProcessOscStates(string address, IPAddress sourceIp)
        {
            if(storeState)
            {
                if (textBuffer == "")
                {
                    UpdateState(address, sourceIp);
                    return;
                }

                var assigned = xStates.Where(a => a.oscAddresses.Contains(address));
                if (assigned.Count() > 0)
                {
                    foreach (var a in assigned)
                        OutputText("Already assigned to: " + a.name);
                    return;
                }

                if(xStates.Where(a => a.name == textBuffer && a.oscAddresses.Count() > 0).Count() > 0)
                {
                    OutputText(textBuffer + " already exists and is assigned");
                    return;
                }

                xStates.RemoveAll(a => a.name == textBuffer);
                SaveState(textBuffer);
                xStates.Single(a => a.name == textBuffer).oscAddresses.Add(address);
                SaveStatesXML(currentShow);
                SendOSCLabel(sourceIp, address, textBuffer);
                OutputText("State saved: " + textBuffer);
                storeState = false;
                textBuffer = "";
                SendOSC(sourceIp, 9000, new OscMessage("/kb/cmd/store",0));
                return;
            }


            if (assignState)
            {
                var assigned = xStates.Where(a => a.oscAddresses.Contains(address));
                if (assigned.Count() > 0)
                {
                    foreach (var a in assigned)
                        OutputText("Already assigned to: " + a.name);
                    return;
                }
                string stateName = xStates.OrderBy(a => a.name).ToList()[selectedState].name;
                if (xStates.Single(a => a.name == stateName).oscAddresses.Count() > 0)
                {
                    OutputText(stateName + " is already assigned");
                    return;
                }
                xStates.Single(a => a.name == stateName).oscAddresses.Add(address);
                SaveStatesXML(currentShow);
                SendOSCLabel(sourceIp, address, stateName);
                OutputText("State assigned: " + stateName);
                assignState = false;
                SendOSC(sourceIp, 9000, new OscMessage("/kb/cmd/assign", 0));
                return;
            }

            if (unassignState)
            {
                var s = xStates.Where(a => a.oscAddresses.Contains(address));
                foreach (var t in s)
                    t.oscAddresses.Remove(address);
                SaveStatesXML(currentShow);
                SendOSCLabel(sourceIp, address, "");
                unassignState = false;
                SendOSC(sourceIp, 9000, new OscMessage("/kb/cmd/unassign", 0));
                return;
            }

            var states = xStates.Where(a => a.oscAddresses.Contains(address));
            foreach (var state in states)
            {
                ProcessState(state, sourceIp);
            }
        }

        private void ProcessOscControllers(string address, float value)
        {
            var controllers = from C in oscControllerStates
                              where C.address == address
                              select C;

            foreach (var controller in controllers)
            {
                controller.value = value;
                OutputText(string.Format("OSC {0} {1}", address, value));
            }
        }

        private void SendOSC(IPAddress ip, int remotePort, OscMessage message)
        {
            try
            {
                // Create a new sender instance
                using (OscSender s = new OscSender(ip, remotePort))
                {


                    // Connect the sender socket  
                    s.Connect();

                    // Send a new message
                    s.Send(message);

                }
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void SendOSC(IPAddress ip, int remotePort, OscBundle bundle)
        {
            try
            {
                // Create a new sender instance
                using (OscSender s = new OscSender(ip, remotePort))
                {


                    // Connect the sender socket  
                    s.Connect();

                    // Send a new message
                    s.Send(bundle);

                }
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void SendOSCReaper(IPAddress ip, int remotePort, OscMessage message)
        {
            try
            {
                // Create a new sender instance
                using (OscSender s = new OscSender(ip, oscPort, remotePort))
                {


                    // Connect the sender socket  
                    s.Connect();

                    // Send a new message
                    s.Send(message);

                }
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void SendOSCX32(IPAddress ip, int remotePort, OscMessage message)
        {
            try
            {
                // Create a new sender instance
                using (OscSender s = new OscSender(ip, oscPort, remotePort))
                {


                    // Connect the sender socket  
                    s.Connect();

                    // Send a new message
                    s.Send(message);

                }
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        public void TransmitOSC(string sendAddress, object sendValue, string sendPort, string ip, bool reaper)
        {
            try
            {
                int port = int.Parse(sendPort);
                List<IPAddress> ipList = new List<IPAddress>() { IPAddress.Parse(ip) };

                if (ip == "127.0.0.1" && oscResendLoopback)
                {
                    var ips = GetIPv4Address().Where(a => a.AddressFamily == AddressFamily.InterNetwork);
                    foreach (var i in ips)
                        ipList.Add(i);
                }

                IPEndPoint sourceEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                OscMessage message = new OscMessage(sourceEndPoint, sendAddress, new object[] { sendValue });

                foreach (IPAddress sendIp in ipList)
                {
                    if(reaper)
                        SendOSCReaper(sendIp, port, message);
                    else
                        SendOSC(sendIp, port, message);
           
                    if (debug) OutputText(string.Format("OSC sent: {0}  value: {1}  ip: {2}  port: {3}", sendAddress, sendValue, ip, sendPort));
                }
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

        }

        public void TransmitOSCXY(string sendAddress, string sendPort, string ip, float X, float Y)
        {
            try
            {
                int port = int.Parse(sendPort);
                List<IPAddress> ipList = new List<IPAddress>() { IPAddress.Parse(ip) };

                if (ip == "127.0.0.1" && oscResendLoopback)
                {
                    var ips = GetIPv4Address().Where(a => a.AddressFamily == AddressFamily.InterNetwork);
                    foreach (var i in ips)
                        ipList.Add(i);
                }

                IPEndPoint sourceEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                OscMessage message = new OscMessage(sourceEndPoint, sendAddress, new object[] { X, Y });

                foreach (IPAddress sendIp in ipList)
                {
                    //IPEndPoint Destination = new IPEndPoint(sendIp, port);
                    SendOSC(sendIp, port, message);
                    //message.Send(Destination);

                    if (debug) OutputText(string.Format("OSC XY sent: {0}  ip: {1}  port: {2}  XY: {3},{4}", sendAddress, ip, sendPort, X, Y));
                }
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

        }

        private void TransmitX32(string sendAddress, object sendValue)
        {
            try
            {
                IPEndPoint sourceEndPoint = new IPEndPoint(IPAddress.Loopback, X32Port);
                OscMessage message = new OscMessage(sourceEndPoint, sendAddress, new object[] { sendValue });

                SendOSC(IPAddress.Parse(X32IP), X32Port, message);

                OutputText(string.Format("OSC sent: {0}  type: {1}  value: {2}  ip: {3}  port: {4}", sendAddress, sendValue.GetType().ToString(), sendValue, X32IP, X32Port));
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

        }

        private void TransmitX32Fader(string sendAddress, string clientIp, float value)
        {
            try
            {
                int port = X32Port;
                //IPEndPoint Destination = new IPEndPoint(IPAddress.Parse(X32IP), port);
                IPEndPoint sourceEndPoint = new IPEndPoint(IPAddress.Loopback, port);
                OscMessage message = new OscMessage(sourceEndPoint, sendAddress, new object[] { value });
                //message.Append(float.Parse(value));
                SendOSC(IPAddress.Parse(X32IP), port, message);
                //message.Send(Destination);

                if (debug) OutputText(string.Format("OSC sent: {0}  client: {1}  value: {2}  ip: {3}  port: {4}", sendAddress, clientIp, value, X32IP, X32Port));
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

        }

        private void TransmitX32Tap(string sendAddress)
        {
            DateTime now = DateTime.Now;
            float ms = (float)((now - x32TapLast).TotalMilliseconds / 3000.0);
            x32TapLast = now;
            if (ms <= 1)
            {
                TransmitX32(sendAddress, ms);
                OutputText("X32 Tap Sent: " + ms.ToString());
            }
        }

        private void InitX32Faders()
        {
            OutputText("Send X32 fader request");
            var faders = xOscMappings.Where(a => a.Element("maptype").Value == "command").Where(a => a.Element("command").Value == "x32-fader");

            try
            {
                // Create a new sender instance
                using (OscSender s = new OscSender(IPAddress.Parse(X32IP),oscPort, X32Port))
                {


                    // Connect the sender socket  
                    s.Connect();

                    // Send a new message
                    foreach (var fader in faders)
                    {
                        string x32Address = fader.Element("x32address").Value;
                        OutputText(x32Address);
                        OscMessage message = new OscMessage(x32Address);
                        s.Send(message);
                        //X32Request(x32Address);
                        System.Threading.Thread.Sleep(10);
                    }
                   
                }
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }


        }

        private void timerX32_Tick(object sender, EventArgs e)
        {
            SendOSCX32(IPAddress.Parse(X32IP), X32Port, new OscMessage(X32Port == 10023 ? "/xremote" : "/xremotenfb"));
            //SendOSCX32(IPAddress.Parse(X32IP), X32Port, new OscMessage("/xremotenfb"));

            if (cbMeters.Checked)
                SendOSCX32(IPAddress.Parse(X32IP), X32Port, new OscMessage("/meters", X32Port == 10023 ? "/meters/13" : "/meters/1", (int)0, (int)0, X32TimeFactor));
        }

        private void X32Request(string sendAddress)
        {
            try
            {
                //IPEndPoint Destination = new IPEndPoint(IPAddress.Parse(X32IP), X32Port);
                //IPEndPoint sourceEndPoint = new IPEndPoint(IPAddress.Loopback, oscPort);
                OscMessage message = new OscMessage(sendAddress);

                SendOSC(IPAddress.Parse(X32IP), X32Port, message);

                if (debug) OutputText(string.Format("X32 request sent: {0}", sendAddress));

            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

        }

        private void OscAllOff(IPAddress sourceIp)
        {
            if (sourceIp == null)
                return;

            foreach (var scene in xScenes)
            {
                foreach (var mapping in scene.oscMappings)
                {
                    try
                    {
                        TransmitOSC(mapping.address, (float)0, "9000", sourceIp.ToString(),false);
                        //OutputText("Sent " + mapping.address + " 0");
                    }
                    catch (Exception ex)
                    {
                        OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    }
                }
            }

            foreach (var chase in xChases)
            {
                foreach (var mapping in chase.oscMappings)
                {
                    try
                    {
                        TransmitOSC(mapping.address, (float)0, "9000", sourceIp.ToString(),false);
                    }
                    catch (Exception ex)
                    {
                        OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                    }
                }
            }

            AllOffTimes.RemoveAll(a => a.IPAddress.ToString() == sourceIp.ToString());
            AllOffTimes.Add(new AllOffTime(sourceIp));
        }

        private void SendOscLabels(IPAddress sourceIp)
        {
            if (sourceIp.ToString() == X32IP)
                return;

            OutputText("Sending labels to: " + sourceIp.ToString());

            List<xOscMapping> labels = new List<xOscMapping>();

            foreach (var scene in xScenes)
            {
                foreach (var mapping in scene.oscMappings.Where(a => !a.address.ToLower().StartsWith("/midi")))
                    labels.Add(new xOscMapping(mapping.address, scene.name));
            }

            foreach (var chase in xChases)
            {
                foreach (var mapping in chase.oscMappings.Where(a => !a.address.ToLower().StartsWith("/midi")))
                    labels.Add(new xOscMapping(mapping.address, chase.name));
            }

            foreach (var state in xStates)
            {
                string address = state.oscAddresses.FirstOrDefault();
                string name = state.name;
                labels.Add(new xOscMapping(address, name));
            }

            foreach (var label in labels.OrderBy(a => a.address))
            {
                SendOSCLabel(sourceIp, label.address, label.action);
                System.Threading.Thread.Sleep(10);
            }

            SendOSC(sourceIp, 9000, new OscMessage("/kb/state", xStates.OrderBy(a => a.name).ToList()[selectedState].name));

            OutputText("Done!");
        }

        private void SendOSCLabel(IPAddress sourceIp, string address, string name)
        {
            try
            {
                for (int i = 0; i < 5; i++)
                    TransmitOSC(address + "/label" + i.ToString(), "", "9000", sourceIp.ToString(),false);

                string[] split = name.Split(' ');
                for (int i = 0; i < split.Length; i++)
                {
                    OutputText(address + "/label" + i.ToString() + " " + split[i]);
                    TransmitOSC(address + "/label" + i.ToString(), split[i], "9000", sourceIp.ToString(),false);
                }
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void SendOscOverrideLevels(IPAddress sourceIp)
        {
            foreach (var patch in xPatches)
            {
                
                foreach (var function in patch.fixture.channels)
                {
                    var channel = channelFromFunction(patch.name, function.function);
                    var value = OverrideLevels[channel.Value];

                    string address = "/patch/" + patch.name + "/" + function.function;

                    address = address.Replace(' ', '_');

                    SendOSC(sourceIp, 9000, new OscMessage(address, value));

                    OutputText("Sent: " + address + " " + value.ToString());
                }
            }
        }


        #endregion OSC


        #region VLC

        private void TransmitVLCIP(string command, string value, int volume, string ip)
        {
            switch (command)
            {
                case ("/vlcip/playonce"):
                    OutputText("VLC Play Once: " + value);
                    VlcPlay(value, false, volume, ip);
                    break;

                case ("/vlcip/playrepeat"):
                    OutputText("VLC Play Repeat: " + value);
                    VlcPlay(value, true, volume, ip);
                    break;

                case ("/vlcip/stop"):
                    OutputText("VLC Stop");
                    SendVlcCommand("pl_stop", ip);
                    break;
            }
        }

        private void TransmitVLC(string command, string value, int volume)
        {
            switch (command)
            {
                case ("/vlc/playonce"):
                    OutputText("VLC Play Once: " + value);
                    foreach (string ip in vlcIPs)
                        VlcPlay(value, false, volume, ip);
                    break;

                case ("/vlc/playrepeat"):
                    OutputText("VLC Play Repeat: " + value);
                    foreach (string ip in vlcIPs)
                        VlcPlay(value, true, volume, ip);
                    break;

                case ("/vlc/playstream"):
                    OutputText("VLC Play Stream: " + value);
                    foreach (string ip in vlcIPs)
                        VlcPlayStream(value, false, ip);
                    break;

                case ("/vlc/playselected"):
                    OutputText("VLC Play Selected Once: " + value);
                    foreach (string ip in vlcIPs.Take(1))
                        VlcPlaySelected(false, volume, ip);
                    break;

                case ("/vlc/playselectedrepeat"):
                    OutputText("VLC Play Repeat: " + value);
                    foreach (string ip in vlcIPs.Take(1))
                        VlcPlaySelected(true, volume, ip);
                    break;

                case ("/vlc/stop"):
                    OutputText("VLC Stop");
                    foreach (string ip in vlcIPs)
                        SendVlcCommand("pl_stop", ip);
                    break;
            }
        }

        private void VlcPlay(string file, bool repeat, int volume, string vlcIP)
        {
            if (repeat != VlcIsRepeat(vlcIP))
                SendVlcCommand("pl_repeat", vlcIP);

            SendVlcCommand(string.Format("volume&val={0}", volume), vlcIP);

            //SendVlcCommand("pl_empty");

            SendVlcCommand(string.Format("in_play&input=file:///{0}", file), vlcIP);
        }

        //private void VlcMute()
        //{
        //    SendVlcCommand("?command=volume&val=0");
        //}

        //private void VlcUnmute()
        //{
        //    SendVlcCommand("?command=volume&val=256");
        //}

        private void VlcPlayStream(string url, bool repeat, string vlcIP)
        {
            if (repeat != VlcIsRepeat(vlcIP))
                SendVlcCommand("pl_repeat", vlcIP);

            SendVlcCommand(string.Format("in_play&input={0}", url), vlcIP);
        }

        private void SendVlcCommand(string command, string vlcIP)
        {
            try
            {
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(300);
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes(":vlcremote")));
                    var resp = client.GetAsync(string.Format("http://{0}:8080/requests/status.json?command={1}", vlcIP, command)).GetAwaiter().GetResult();
                    resp.EnsureSuccessStatusCode();
                }
                OutputText("VLC command sent: " + vlcIP + ": " + command);
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

        }

        private bool VlcIsRepeat(string vlcIP)
        {
            bool repeat = false;

            try
            {

                using (var client = new System.Net.Http.HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(300);
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes(":vlcremote")));
                    var resp = client.GetAsync(string.Format("http://{0}:8080/requests/status.json", vlcIP)).GetAwaiter().GetResult();
                    resp.EnsureSuccessStatusCode();
                    var result = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (result.Contains("\"repeat\":true") || result.Contains("\"repeat\": true"))
                        repeat = true;
                }

            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

            return (repeat);

        }

        private void VlcPlaySelected(bool repeat, int volume, string vlcIP)
        {
            try
            {
                string file = vlcFiles.Single(a => a.name == vlcSelectedFile).path;

                VlcPlay(file, repeat, volume, vlcIP);
            }
            catch
            {
                OutputText("No match on selected filename!\n");
            }

            //SendVlcCommand(string.Format("in_play&input=file:///{0}", file), vlcIP);
        }

        private void EnumerateVlcFiles(IPAddress sourceIp)
        {
            try
            {
                vlcFiles.Clear();

                foreach (var vlcIP in vlcIPs.Take(1))
                {

                    string dirpath = @"\\" + vlcIP + @"\\" + vlcBrowsePath;

                    OutputText("Path = " + dirpath);

                    foreach (string ext in vlcExtensions)
                    {
                        var files = Directory.EnumerateFiles(dirpath, ext, SearchOption.AllDirectories);

                        var fileInfosEnum = from F in files
                                            where (new FileInfo(F).Attributes.HasFlag(FileAttributes.Hidden) == false)
                                            select new VlcFileInfo { name = F.Split('\\')[F.Split('\\').Length - 1], path = F };

                        vlcFiles.AddRange(fileInfosEnum);

                    }

                }

                vlcFiles = vlcFiles.OrderBy(a => a.name).ToList();

                OutputText("VLC Files Found: \n");

                foreach (var fileInfo in vlcFiles)
                    OutputText(fileInfo.name);
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

            if (vlcFiles.Count() > 0)
                vlcSelectedFile = vlcFiles[0].name;

            OutputText("Selected file: " + vlcSelectedFile);

            //TransmitOSC("/vlcselected", vlcSelectedFile, "9000", sourceIp.ToString(), "string");
            VlcSendSelected(sourceIp);
        }

        private void VlcIncrementSelection(int step, IPAddress sourceIp)
        {
            try
            {
                var selected = vlcFiles.Single(a => a.name == vlcSelectedFile);

                int i = vlcFiles.IndexOf(selected);

                i += step;

                if (i < 0) i = 0;
                if (i >= vlcFiles.Count()) i = vlcFiles.Count() - 1;

                vlcSelectedFile = vlcFiles[i].name;

                OutputText("VLC selection: " + vlcSelectedFile);
                //TransmitOSC("/vlcselected", vlcSelectedFile, "9000", sourceIp.ToString(), "string");
                VlcSendSelected(sourceIp);
            }
            catch
            {
                OutputText("No file selected!");
            }

        }

        private void VlcSendSelected(IPAddress sendIp)
        {
            if (sendIp == null)
                sendIp = IPAddress.Loopback;
            string previous = "";
            string next = "";

            try
            {
                TransmitOSC("/vlcselected", vlcSelectedFile, "9000", sendIp.ToString(),false);

                var selected = vlcFiles.Single(a => a.name == vlcSelectedFile);

                int i = vlcFiles.IndexOf(selected);

                if (i > 0) previous = vlcFiles[i - 1].name;

                if (i < vlcFiles.Count() - 1) next = vlcFiles[i + 1].name;

                TransmitOSC("/vlcprevious", previous, "9000", sendIp.ToString(),false);
                TransmitOSC("/vlcnext", next, "9000", sendIp.ToString(),false);
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }




        }

        #endregion VLC


        #region Telldus

        private void ProcessTelldusCommand(string device, string command)
        {
            try
            {
                MethodDelegate dlgtTelldus = new MethodDelegate(TelldusCommand);
                IAsyncResult ar = dlgtTelldus.BeginInvoke(device, command, null, null);

            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        delegate void MethodDelegate(string device, string command);
        private void TelldusCommand(string device, string command)
        {
            try
            {
                int deviceId = TelldusNETWrapper.tdGetDeviceId(int.Parse(device));

                switch (command)
                {
                    case "on":
                        TelldusNETWrapper.tdTurnOn((int)deviceId);
                        TelldusNETWrapper.tdClose();
                        break;

                    case "off":
                        TelldusNETWrapper.tdTurnOff((int)deviceId);
                        TelldusNETWrapper.tdClose();
                        break;
                }

                OutputText(String.Format("Telldus command send: {0} {1}", deviceId, command));
            }
            catch (Exception ex)
            {
                OutputText("Telldus worker error: " + ex.Message);
            }
        }

        #endregion Telldus


        #region Common MIDI/OSC

        private void ProcessScenes(IEnumerable<xAction> mappings, float value)
        {
            bool solo = false;

            foreach (var mapping in mappings)
            {
                try
                {
                    var sceneName = mapping.name;
                    var action = mapping.action;

                    if (xScenes.Where(a => a.name == sceneName).Count() == 0)
                    {
                        OutputText("No scene named " + sceneName);
                        continue;
                    }

                    var scene = xScenes.First(a => a.name == sceneName);

                    if (action == "toggle" && value == 1)
                    {
                        scene.active = !scene.active;
                        OutputText("Toggle scene " + scene.name);
                    }

                    if (action == "solo" && value == 1)
                    {
                        OutputText("Toggle scene " + scene.name);
                        if (scene.active)
                        {
                            scene.active = false;
                        }
                        else
                        {
                            if (!solo)
                            {
                                foreach (var scn in xScenes) scn.active = false;
                                foreach (var c in xChases) c.active = false;
                            }
                            scene.active = true;
                            solo = true;

                        }

                    }

                    if (action == "flash")
                    {
                        OutputText("Flash scene " + scene.name);
                        scene.active = (value == 1);
                    }
                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
        }

        private void ProcessChases(IEnumerable<xAction> mappings, float value)
        {
            foreach (var mapping in mappings)
            {
                try
                {
                    var chaseName = mapping.name;
                    var action = mapping.action;

                    if (xChases.Where(a => a.name == chaseName).Count() == 0)
                    {
                        OutputText("No chase named " + chaseName);
                        continue;
                    }

                    var chase = xChases.First(a => a.name == chaseName);

                    chase.currentStep = 0;
                    chase.lastChange = null;
                    chase.lastStep = null;


                    if (action == "toggle" && value == 1)
                    {
                        chase.active = !chase.active;
                        OutputText("Toggle chase " + chase.name);
                    }

                    if (action == "solo" && value == 1)
                    {
                        OutputText("Toggle chase " + chase.name);
                        if (chase.active)
                        {
                            chase.active = false;
                        }
                        else
                        {
                            foreach (var c in xChases) c.active = false;
                            foreach (var s in xScenes) s.active = false;
                            chase.active = true;
                        }
                    }

                    if (action == "flash")
                    {
                        chase.active = (value == 1);
                        OutputText("Flash chase " + chase.name);
                    }
                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
        }

        private void ProcessCommands(IEnumerable<XElement> commands, float value, string address, IPAddress sourceIp)
        {
            if (value == 0 && !(address ?? "").ToLower().Contains("fader") && !(address ?? "").ToLower().Contains("encoder"))
                return;

            foreach (var command in commands)
            {
                try
                {
                    ProcessCommand(command, value, sourceIp);
                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
        }

        private void ProcessState(xState state, IPAddress sourceIp)
        {
            try
            {
                if (sourceIp == null)
                    if (lastOscMessage != null)
                        sourceIp = lastOscMessage.Origin.Address;
            }
            catch { }

            if (state.exclusive)
            {
                AllOff();
                if (sourceIp != null) OscAllOff(sourceIp);
            }

            OutputText("State: " + state.name);

            foreach (var scene in state.scenes)
            {
                OutputText("Scene: " + scene);
                var xScene = xScenes.First(a => a.name == scene);
                xScene.active = true;
                if (sourceIp != null)
                    foreach (var mapping in xScene.oscMappings)
                    {

                        TransmitOSC(mapping.address, (float)1, "9000", sourceIp.ToString(),false);
                        OutputText("Sent " + mapping.address + " 1");
                    }

            }

            foreach (var chase in state.chases)
            {
                OutputText("Chase: " + chase);
                var xChase = xChases.First(a => a.name == chase);
                xChase.active = true;
                if (sourceIp != null)
                    foreach (var mapping in xChase.oscMappings)
                        TransmitOSC(mapping.address, (float)1, "9000", sourceIp.ToString(),false);

            }
        }

        private void AllOff()
        {
            foreach (var scene in xScenes)
                scene.active = false;
            foreach (var chase in xChases)
                chase.active = false;
            foreach (var controller in controllerStates)
                controller.value = 0;

            for (int i = 0; i < OverrideLevels.Count(); i++)
                OverrideLevels[i] = 0;

        }

        private void ProcessCommand(XElement command, float value, IPAddress sourceIp)
        {
            string dmxCommand = command.Element("command").Value;
            if (debug) OutputText("Command: " + dmxCommand);
            DateTime now = DateTime.Now;
            double interval = 0;

            switch (dmxCommand)
            {
                case ("alloff"):
                    AllOff();
                    OscAllOff(sourceIp);
                    break;
                case ("tempo"):
                    now = DateTime.Now;
                    interval = (now - lastTap).TotalMilliseconds;
                    lastTap = now;
                    OutputText("Interval: " + interval);
                    if (interval < 10000)
                        tempo = (int)interval;
                    break;
                case ("tempodiv"):
                    now = DateTime.Now;
                    interval = (now - lastTap).TotalMilliseconds;
                    lastTap = now;
                    int div = int.Parse(command.Element("div").Value);
                    OutputText("Interval: " + interval);
                    if (interval < 20000)
                        tempo = (int)interval / div;
                    OutputText("Tempo: " + tempo.ToString());
                    break;
                case ("temposlow"):
                    tempo = 1000;
                    break;
                case ("tempofast"):
                    tempo = 200;
                    break;
                case ("tempomed"):
                    tempo = 500;
                    break;
                case ("reload"):
                    ReadAllFiles();
                    break;
                case ("sendosc"):
                    string ip = command.Element("ip").Value;
                    string port = command.Element("port").Value;
                    string sendAddress = command.Element("sendaddress").Value;
                    //string sendValue = command.Element("value").Value;
                    string type = command.Element("type").Value;
                    object sendValue = null;
                    switch (type)
                    {
                        case "int":
                            sendValue = int.Parse(command.Element("value").Value);
                            break;
                        case "float":
                            sendValue = int.Parse(command.Element("value").Value);
                            break;
                        default:
                            sendValue = command.Element("value").Value;
                            break;
                    }

                    TransmitOSC(sendAddress, sendValue, port, ip,false);
                    break;
                case ("x32"):
                    string x32SendAddress = command.Element("sendaddress").Value;
                    object x32SendValue = null;
                    switch (command.Element("type").Value)
                    {
                        case "int":
                            x32SendValue = int.Parse(command.Element("value").Value);
                            break;
                        case "float":
                            x32SendValue = int.Parse(command.Element("value").Value);
                            break;
                        default:
                            x32SendValue = command.Element("value").Value;
                            break;
                    }

                    if (command.Element("value").Value.ToLower() == "fader")
                        x32SendValue = value;

                    TransmitX32(x32SendAddress, x32SendValue);
                    break;
                case ("x32tap"):
                    string x32TapSendAddress = command.Element("sendaddress").Value;
                    TransmitX32Tap(x32TapSendAddress);
                    break;
                case ("x32-fader"):
                    string x32FaderSendAddress = command.Element("x32address").Value;
                    string clientIp = command.Element("client-ip").Value;
                    string x32FaderValue = value.ToString();
                    if (clientIp == sourceIp.ToString())
                        TransmitX32Fader(x32FaderSendAddress, clientIp, float.Parse(x32FaderValue));
                    break;
                case ("telldus"):
                    string device = command.Element("device").Value;
                    string telldusCommand = command.Element("value").Value;
                    ProcessTelldusCommand(device, telldusCommand);
                    break;
                case ("vlc"):
                    string vlcAddress = command.Element("sendaddress").Value;
                    string vlcValue = command.Element("value") == null ? null : command.Element("value").Value;
                    int vlcVolume = 0;
                    if (command.Element("volume") != null) int.TryParse(command.Element("volume").Value, out vlcVolume);
                    TransmitVLC(vlcAddress, vlcValue, vlcVolume);
                    break;
                case ("vlcip"):
                    string vlcAddress1 = command.Element("sendaddress").Value;
                    string vlcValue1 = command.Element("value") == null ? null : command.Element("value").Value;
                    int vlcVolume1 = 0;
                    string vlcIp = command.Element("vlcip").Value;
                    if (command.Element("volume") != null) int.TryParse(command.Element("volume").Value, out vlcVolume1);
                    TransmitVLCIP(vlcAddress1, vlcValue1, vlcVolume1,vlcIp);
                    break;
                case ("vlcinc"):
                    VlcIncrementSelection((int)value, sourceIp);
                    break;
                case ("enumeratevlcfiles"):
                    EnumerateVlcFiles(sourceIp);
                    break;
                case ("labels"):
                    if (value == 1)
                    {
                        SendOscLabels(sourceIp);
                        InitX32Faders();
                    }
                    break;
                case ("sync"):
                    if (value == 1)
                        InitX32Faders();
                    break;
                case ("midiprogchange"):
                    byte midiChannel = byte.Parse(command.Element("midichannel").Value);
                    byte midiProgram = byte.Parse(command.Element("value").Value);
                    SendMidiProgramChange(midiChannel, midiProgram);
                    break;
                case ("savecurrent"):
                    string statename = command.Element("statename").Value;
                    SaveState(statename, true);
                    break;
                case ("overridelevels"):
                    SendOscOverrideLevels(sourceIp);
                    break;
                default:
                    OutputText("Command not recognised");
                    break;
            }
        }

        #endregion Common MIDI/OSC


        #region MIDI

        private void NoteOnHandler(object sender, EventArgs e)
        {
            MidiNoteEventArgs a = (MidiNoteEventArgs)e;
            if(debug) OutputText(string.Format("Note on - ch {0}  note {1}  velocity {2}", a.channel, a.note, a.velocity));

            byte midiChannel = a.channel;
            byte pitch = a.note;

            var stateMappings = from M in midiMappings
                                where M.Elements("maptype").First().Value == "state"
                                && M.Element("midichannel").Value == midiChannel.ToString()
                                && M.Element("note").Value == pitch.ToString()
                                select M.Element("statename").Value;

            foreach (var state in xStates.Where(s => stateMappings.Contains(s.name)))
                ProcessState(state, null);


            var sceneMappings = from M in midiMappings
                                where M.Elements("maptype").First().Value == "scene"
                                && M.Element("midichannel").Value == midiChannel.ToString()
                                && M.Element("note").Value == pitch.ToString()
                                select new xAction { name = M.Element("scenename").Value, action = M.Element("action").Value };

            ProcessScenes(sceneMappings, 1);

            var chaseMappings = from M in midiMappings
                                where M.Elements("maptype").First().Value == "chase"
                                && M.Element("midichannel").Value == midiChannel.ToString()
                                && M.Element("note").Value == pitch.ToString()
                                select new xAction { name = M.Element("chasename").Value, action = M.Element("action").Value };

            ProcessChases(chaseMappings, 1);

            var commandMappings = from M in midiMappings
                                  where M.Element("maptype").Value == "command"
                                  && M.Element("midichannel").Value == midiChannel.ToString()
                                  && M.Element("note").Value == pitch.ToString()
                                  select M;

            ProcessCommands(commandMappings, 1, null, null);
        }

        private void NoteOffHandler(object sender, EventArgs e)
        {
            MidiNoteEventArgs a = (MidiNoteEventArgs)e;
            if(debug) OutputText(string.Format("Note off - ch {0}  note {1}  velocity {2}", a.channel, a.note, a.velocity));

            byte midiChannel = a.channel;
            byte pitch = a.note;


            var sceneMappings = from M in midiMappings
                                where M.Elements("maptype").First().Value == "scene"
                                && M.Element("midichannel").Value == midiChannel.ToString()
                                && M.Element("note").Value == pitch.ToString()
                                select new xAction { name = M.Element("scenename").Value, action = M.Element("action").Value };

            ProcessScenes(sceneMappings, 0);

            var chaseMappings = from M in midiMappings
                                where M.Elements("maptype").First().Value == "chase"
                                && M.Element("midichannel").Value == midiChannel.ToString()
                                && M.Element("note").Value == pitch.ToString()
                                select new xAction { name = M.Element("chasename").Value, action = M.Element("action").Value };

            ProcessChases(chaseMappings, 0);
        }

        private void ControllerHandler(object sender, EventArgs e)
        {
            MidiControllerEventArgs a = (MidiControllerEventArgs)e;
            if (debug) OutputText(string.Format("Midi controller - ch {0}  controller {1}  value {2}", a.channel, a.controller, a.value));

            byte midiChannel = a.channel;
            byte controllerNumber = a.controller;
            byte value = a.value;


            var controllers = from C in controllerStates
                              where C.midiChannel == midiChannel
                              && C.controller == controllerNumber
                              select C;

            foreach (var controller in controllers)
                controller.value = value;
        }

        private void ProgChangeHandler(object sender, EventArgs e)
        {
            MidiProgChangeEventArgs a = (MidiProgChangeEventArgs)e;
            if (debug) OutputText(string.Format("Midi prog change - ch {0}  program {1}", a.channel, a.program));

            byte midiChannel = a.channel;
            byte program = a.program;

            var stateMappings = from M in midiMappings
                                where M.Elements("maptype").First().Value == "progstate"
                                && M.Element("midichannel").Value == midiChannel.ToString()
                                && M.Element("program").Value == program.ToString()
                                select M.Element("statename").Value;

            foreach (var state in xStates.Where(s => stateMappings.Contains(s.name)))
                ProcessState(state, null);

            var commandMappings = from M in midiMappings
                                  where M.Element("maptype").Value == "progcommand"
                                  && M.Element("midichannel").Value == midiChannel.ToString()
                                  && M.Element("program").Value == program.ToString()
                                  select M;

            ProcessCommands(commandMappings, 1, null, null);
        }

        private void StartMidi()
        {
            try
            {
                MidiInPortCapsCollection midiInCaps = new MidiInPortCapsCollection();
                int portId = midiInCaps.ToList().FindIndex(a => a.Name == cbMidi.SelectedItem.ToString());

                receiver = new MidiReceiver();
                receiver.NoteOnHandler += new EventHandler(NoteOnHandler);
                receiver.NoteOffHandler += new EventHandler(NoteOffHandler);
                receiver.ControllerHandler += new EventHandler(ControllerHandler);
                receiver.ProgChangeHandler += new EventHandler(ProgChangeHandler);
                receiver.Start(portId);

                if (receiver.isRunning)
                {
                    OutputText("");
                    OutputText("** MIDI receiving. **");
                    OutputText("");
                }

            }
            catch (Exception ex)
            {
                OutputText("MIDI Error: " + ex.Message);
                OutputText(ex.ToString());
            }

            try
            {
                MidiOutPortCapsCollection midiOutCaps = new MidiOutPortCapsCollection();
                int outPortId = midiOutCaps.ToList().FindIndex(a => a.Name == cbMidiOut.SelectedItem.ToString());

                midiOutPort = new MidiOutPort();
                midiOutPort.Open(outPortId);

                if (midiOutPort.IsOpen)
                {
                    OutputText("");
                    OutputText("** MIDI Out enabled **");
                    OutputText("");
                }
            }
            catch (Exception ex)
            {
                OutputText("MIDI Out Error: " + ex.Message);
                OutputText(ex.ToString());
            }

            btnStop.Enabled = true;
            btnStart.Enabled = false;
            cbMidi.Enabled = false;
            cbMidiOut.Enabled = false;

        }

        private void StopMidi()
        {
            if (receiver != null)
            {
                if (receiver.isRunning)
                {
                    receiver.Stop();
                    receiver.Dispose();
                }

                if (!receiver.isRunning)
                    OutputText("** MIDI In closed. **");
            }

            if (midiOutPort != null)
            {
                if (midiOutPort.IsOpen)
                {
                    midiOutPort.Close();
                    midiOutPort.Dispose();
                    OutputText("** MIDI Out closed. **");
                }

            }

            btnStop.Enabled = false;
            btnStart.Enabled = true;
            cbMidi.Enabled = true;
            cbMidiOut.Enabled = true;
        }

        private void btnMidi_Click(object sender, EventArgs e)
        {
            if (midiForm != null)
                if (midiForm.Visible)
                    return;

            midiForm = new MidiMappingsForm(this);
            midiForm.Show();
        }

        private void SendMidiProgramChange(byte channel, byte program)
        {
            try
            {
                MidiData midiData = new MidiData();
                midiData.Status = (byte)(0xC0 + channel - 1);
                midiData.Parameter1 = program;
                midiOutPort.ShortData(midiData);
                OutputText(String.Format("Midi sent: {0} {1}", midiData.Status, midiData.Parameter1));
            }
            catch { }
        }

        private void SendMidi(MidiData midiData)
        {
            midiOutPort.ShortData(midiData);
        }

        #endregion MIDI


        #region Form Housekeeping

        private void SetupForm()
        {
            cbMidi.Items.Clear();

            try
            {
                var midiInCaps = new MidiInPortCapsCollection();
                foreach (var inCaps in midiInCaps)
                    cbMidi.Items.Add(inCaps.Name);

                cbMidi.Items.Add("Disable Midi");
            }
            catch
            {
                OutputText("No MIDI Input devices!");
            }

            cbMidiOut.Items.Clear();

            try
            {
                var midiOutCaps = new MidiOutPortCapsCollection();
                foreach (var outCaps in midiOutCaps)
                    cbMidiOut.Items.Add(outCaps.Name);

                cbMidiOut.Items.Add("Disable Midi");
            }
            catch
            {
                OutputText("No MIDI Output devices!");
            }

            dmxOutputDevice = new VComWrapper();
            dmxOutputDevice.SerialNumberReceived += new EventHandler<SerialNumberArgs>(dmx_SerialNumberReceived);
            dmxOutputDevice.WidgetParametersReceived += new EventHandler<WidgetParameterArgs>(dmx_WidgetParametersReceived);

            if (System.IO.Ports.SerialPort.GetPortNames().Count() > 0)
            {
                cbDmx.Items.AddRange(System.IO.Ports.SerialPort.GetPortNames());
                cbDmx.SelectedIndex = 0;
            }

            cbDmx.Items.Add("");
            cbDmx.Items.Add("Disable DMX");

            btnStop.Enabled = false;
            btnStart.Enabled = true;

            LoadGlobalSettings();

            var shows = Directory.GetDirectories(".\\", "*.xml");

            ddlShows.Items.Clear();
            foreach (var show in shows)
                ddlShows.Items.Add(show.Substring(2).Replace(".xml",""));
            ddlShows.SelectedItem = currentShow;


            LoadSettings(currentShow);

            OutputText("");
            OutputText("Local IP Addresses");
            var ips = GetIPv4Address();
            foreach (var ip in ips)
                if (ip.AddressFamily == AddressFamily.InterNetwork) OutputText(" " + ip.ToString());
            OutputText("");

            OutputText("X32 IP: " + X32IP.ToString());
            OutputText("X32 Port: " + X32Port.ToString());
            OutputText("");

            foreach (string ip in vlcIPs)
                OutputText("VLC IP: " + ip.ToString());
            OutputText("");

            OutputText("ARTNet Broadcast IP: " + artNetBroadcastIP);
            OutputText("");

            OutputText("OSC Resend Loopback Address: " + oscResendLoopback.ToString());


        }

        public void OutputText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.listBox1.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(OutputText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                if (!cbOutText.Checked)
                    return;

                this.listBox1.Items.Add(text);
                this.listBox1.SelectedIndex = this.listBox1.Items.Count - 1;
            }
        }

        private void ShowLevels()
        {
            if (form2 != null)
                if (form2.Visible)
                    return;

            form2 = new DMXLevelsForm();

            for (int j = 0; j < 16; j++)
            {
                for (int i = 0; i < 32; i++)
                {
                    Label label = new Label()
                    {
                        Name = "lblDmx" + i.ToString(),
                        Location = new Point(i * 30, j * 24),
                        Text = "0",
                        Width = 30,
                        Height = 12

                    };

                    ToolTip toolTip = new ToolTip();
                    //toolTip.ToolTipIcon = ToolTipIcon.Info;
                    toolTip.IsBalloon = true;
                    toolTip.ShowAlways = true;
                    toolTip.SetToolTip(label, (j * 32 + i + 1).ToString());

                    dmxLabels.Add(label);
                    form2.Controls.Add(label);
                }
            }

            form2.Show();
        }

        #endregion Form Housekeeping


        #region File IO

        public void Reload()
        {
            ReadAllFiles();
        }

        private void LoadGlobalSettings()
        {
            try
            {
                XElement root = XElement.Load("global.xml");

                if (root.Element("CurrentShow") != null) currentShow = root.Element("CurrentShow").Value;
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void SaveGlobalSettings()
        {
            try
            {
                using (XmlTextWriter writer = new XmlTextWriter("global.xml", Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Settings");

                    writer.WriteElementString("CurrentShow", (ddlShows.SelectedItem ?? "").ToString());

                    writer.WriteEndElement();
                }

                OutputText("Global settings saved");
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void SaveSettings(string show)
        {
            try
            {
                bool showLevels = false;
                if (form2 != null)
                    if (form2.Visible)
                        showLevels = true;

                using (XmlTextWriter writer = new XmlTextWriter(show +  ".xml\\settings.xml", Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Settings");

                    writer.WriteElementString("MIDIDevice", (cbMidi.SelectedItem ?? "").ToString());
                    writer.WriteElementString("MIDIOutDevice", (cbMidiOut.SelectedItem ?? "").ToString());
                    writer.WriteElementString("DMXDevice", (cbDmx.SelectedItem ?? "").ToString());
                    writer.WriteElementString("Running", btnStop.Enabled.ToString());
                    writer.WriteElementString("ShowLevels", showLevels.ToString());
                    writer.WriteElementString("MaxChannels", maxChannels.ToString());
                    writer.WriteElementString("DMXInterval", timerInterval.ToString());
                    writer.WriteElementString("FixtureFile", fixtureFile);
                    writer.WriteElementString("PatchFile", patchFile);
                    writer.WriteElementString("SceneFile", sceneFile);
                    writer.WriteElementString("ChaseFile", chaseFile);
                    writer.WriteElementString("MidiMappingsFile", midiMappingsFile);
                    writer.WriteElementString("OSCMappingsFile", oscMappingsFile);
                    writer.WriteElementString("ArtNetAddress", artNetBroadcastIP);
                    writer.WriteElementString("EnableArtNet", enableArtNet.ToString());
                  
                    writer.WriteElementString("EnableOSC", cbOSC.Checked.ToString());
                    writer.WriteElementString("OSCPort", oscPort.ToString());

                    foreach (string ip in vlcIPs)
                        writer.WriteElementString("VlcIP", ip);

                    writer.WriteElementString("VLCBrowsePath", vlcBrowsePath);

                    string extensions = "";
                    foreach (string e in vlcExtensions)
                        extensions += e + ";";
                    writer.WriteElementString("VLCExtensions", extensions.Substring(0, extensions.Length - 1));

                    writer.WriteElementString("X32IP", X32IP);
                    writer.WriteElementString("X32Port", X32Port.ToString());
                    writer.WriteElementString("X32TimerInterval", x32TimerInterval.ToString());
                    writer.WriteElementString("X32TimeFactor", X32TimeFactor.ToString());
                    writer.WriteElementString("X32Meters", cbMeters.Checked.ToString());


                    writer.WriteElementString("Tempo", tempo.ToString());
                    writer.WriteElementString("uDMX", cbUDMX.Checked.ToString());
                    writer.WriteElementString("Feedback", cbFeedback.Checked.ToString());
                                    
                    writer.WriteElementString("Debug", debug.ToString());

                    writer.WriteElementString("HazeLevelAddress", hazeLevelAddress);
                    writer.WriteElementString("HazeTimeAddress", hazeTimeAddress);

                    writer.WriteElementString("OscResendLoopback", oscResendLoopback.ToString());

                    writer.WriteElementString("ReaperIP", ReaperIP);
                    writer.WriteElementString("ReaperPort", ReaperPort);
                    writer.WriteElementString("ReaperControlIP", ReaperControlIp.ToString());
                    writer.WriteElementString("ReaperControlPort", ReaperControlPort.ToString());

                    writer.WriteElementString("OSCLoopbackIP", OSCLoopbackIP.ToString());
                    writer.WriteElementString("OSCLoopbackPort", OSCLoopbackPort.ToString());
                    writer.WriteElementString("OSCLoopbackEnabled", cbLoopback.Checked.ToString());

                    if (cbBaseScene.SelectedItem != null) writer.WriteElementString("BaseScene", cbBaseScene.SelectedItem.ToString());
                    writer.WriteElementString("BaseSceneActive", cbBaseSceneActive.Checked.ToString());

                    writer.WriteElementString("OutTextEnabled", cbOutText.Checked.ToString());
                    
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }

            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }



        private void LoadSettings(string show)
        {
            bool run = false;
            bool showLevels = false;
            OscClients.Clear();

            try
            {
                XElement root = XElement.Load(show + ".xml\\settings.xml");

                if (root.Element("ShowLevels") != null) showLevels = root.Element("ShowLevels").Value == "True";
                if (root.Element("Running") != null) run = root.Element("Running").Value == "True";
                try
                {
                    if (root.Element("MIDIDevice") != null) cbMidi.SelectedItem = root.Element("MIDIDevice").Value;
                }
                catch { }
                try
                {
                    if (root.Element("MIDIOutDevice") != null) cbMidiOut.SelectedItem = root.Element("MIDIOutDevice").Value;
                }
                
                catch { }
                try
                {
                    if (root.Element("DMXDevice") != null) cbDmx.SelectedItem = root.Element("DMXDevice").Value;
                }
                catch { }
 
                if (root.Element("Tempo") != null) int.TryParse(root.Element("Tempo").Value, out tempo);
                if (root.Element("MaxChannels") != null) int.TryParse(root.Element("MaxChannels").Value, out maxChannels);
                if (root.Element("DMXInterval") != null) int.TryParse(root.Element("DMXInterval").Value, out timerInterval);
                if (root.Element("FixtureFile") != null) fixtureFile = root.Element("FixtureFile").Value;
                if (root.Element("PatchFile") != null) patchFile = root.Element("PatchFile").Value;
                if (root.Element("MidiMappingsFile") != null) midiMappingsFile = root.Element("MidiMappingsFile").Value;
                if (root.Element("OSCMappingsFile") != null) oscMappingsFile = root.Element("OSCMappingsFile").Value;
                if (root.Element("SceneFile") != null) sceneFile = root.Element("SceneFile").Value;
                if (root.Element("ChaseFile") != null) chaseFile = root.Element("ChaseFile").Value;

                if (root.Element("ArtNetAddress") != null) artNetBroadcastIP = root.Element("ArtNetAddress").Value;
                if (root.Element("EnableArtNet") != null) enableArtNet = (root.Element("EnableArtNet").Value.ToLower() == "true");
                

                if (root.Element("EnableOSC") != null) cbOSC.Checked = (root.Element("EnableOSC").Value.ToLower() == "true");
                if (root.Element("OSCPort") != null) int.TryParse(root.Element("OSCPort").Value, out oscPort);

                //if (root.Element("VlcIP") != null) vlcIP = root.Element("VlcIP").Value;

                vlcIPs.Clear();
                foreach (var ip in root.Elements("VlcIP"))
                    vlcIPs.Add(ip.Value);

                if (root.Element("VLCBrowsePath") != null) vlcBrowsePath = root.Element("VLCBrowsePath").Value;

                if (root.Element("VLCExtensions") != null) vlcExtensions = root.Element("VLCExtensions").Value.Split(';').ToList();

                if (root.Element("X32IP") != null) X32IP = root.Element("X32IP").Value;
                if (root.Element("X32Port") != null) int.TryParse(root.Element("X32Port").Value, out X32Port);
                if (root.Element("X32TimerInterval") != null) int.TryParse(root.Element("X32TimerInterval").Value, out x32TimerInterval);
                if (root.Element("X32TimeFactor") != null) int.TryParse(root.Element("X32TimeFactor").Value, out X32TimeFactor);
                if (root.Element("X32Meters") != null) cbMeters.Checked = (root.Element("X32Meters").Value.ToLower() == "true");

                if (root.Element("uDMX") != null) cbUDMX.Checked = root.Element("uDMX").Value == "True";
                if (root.Element("Feedback") != null) cbFeedback.Checked = root.Element("Feedback").Value == "True";
                       
                if (root.Element("Debug") != null) debug = root.Element("Debug").Value.ToLower() == "true";

                if (root.Element("HazeLevelAddress") != null) hazeLevelAddress = root.Element("HazeLevelAddress").Value;
                if (root.Element("HazeTimeAddress") != null) hazeTimeAddress = root.Element("HazeTimeAddress").Value;

                if (root.Element("OscResendLoopback") != null) oscResendLoopback = (root.Element("OscResendLoopback").Value.ToLower() == "true");

                if (root.Element("ReaperIP") != null) ReaperIP = root.Element("ReaperIP").Value;
                if (root.Element("ReaperPort") != null) ReaperPort = root.Element("ReaperPort").Value;
                if (root.Element("ReaperControlIP") != null) ReaperControlIp = IPAddress.Parse(root.Element("ReaperControlIP").Value);
                if (root.Element("ReaperControlPort") != null) int.TryParse(root.Element("ReaperControlPort").Value, out ReaperControlPort);

                if (root.Element("OSCLoopbackIP") != null) IPAddress.TryParse(root.Element("OSCLoopbackIP").Value,out OSCLoopbackIP);
                if (root.Element("OSCLoopbackPort") != null) int.TryParse(root.Element("OSCLoopbackPort").Value, out OSCLoopbackPort);
                if (root.Element("OSCLoopbackEnabled") != null) cbLoopback.Checked = (root.Element("OSCLoopbackEnabled").Value.ToLower() == "true");

                if (root.Element("BaseScene") != null) BaseScene = root.Element("BaseScene").Value;
                if (root.Element("BaseSceneActive") != null) BaseSceneActive = (root.Element("BaseSceneActive").Value.ToLower() == "true");

                if (root.Element("OutTextEnabled") != null) cbOutText.Checked = (root.Element("OutTextEnabled").Value.ToLower() == "true");


            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

            if (run)
            {
                StartMidi();
                StartDMX();
                StartOSC();

            }

            if (showLevels) ShowLevels();

            OutputText("DMX max channels: " + maxChannels);
            OutputText("DMX timer interval: " + timerInterval);

            OutputText("");

            OutputText("Haze Level Address: " + hazeLevelAddress);
            OutputText("Haze Time Address: " + hazeTimeAddress);

        }

        private void ReadAllFiles()
        {
            try
            {
                ReadFixturesXML(currentShow);
                ReadPatchesXML(currentShow);
                ReadMidiMappingsXML(currentShow);
                ReadOscMappingsXML(currentShow);
                ReadScenesXML(currentShow);
                ReadChasesXML(currentShow);
                ReadStatesXML(currentShow);
                ReadRewrites(currentShow);
                CheckXML();

                xOscMappings.RemoveAll(a => a.Element("maptype").Value == "scene");
                xOscMappings.RemoveAll(a => a.Element("maptype").Value == "chase");
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void ReadRewrites(string show)
        {
            XElement root = XElement.Load(show + ".xml\\rewrite.xml");

            xRewrites.Clear();

            var items = from I in root.Elements("item")
                        select I;

            foreach (var item in items)
            {
                xRewrite rewrite = new xRewrite(item.Element("address").Value, item.Element("rewrite").Value);
                xRewrites.Add(rewrite);
            }
        }

        private void ReadStatesXML(string show)
        {
            XElement root = XElement.Load(show + ".xml\\" + statesFile);

            xStates.Clear();

            var tempStates = from F in root.Elements("state")
                       select F;

            foreach (var state in tempStates)
            {
                xState newState = new xState(state.Element("name").Value);

                var scenes = state.Elements("scene");
                foreach (var s in scenes)
                    newState.scenes.Add(s.Value);
          
                var chases = state.Elements("chase");
                foreach (var c in chases)
                    newState.chases.Add(c.Value);
         
                var oscmappings = state.Elements("oscmapping");
                foreach (var o in oscmappings)
                    newState.oscAddresses.Add(o.Value);

                if (state.Element("exclusive") != null)
                    if (state.Element("exclusive").Value.ToLower() == "true")
                        newState.exclusive = true;

                if (state.Element("tempo") != null)
                    newState.tempo = int.Parse(state.Element("tempo").Value);

         
                xStates.Add(newState);

                OutputText(" State: " + newState.name + " added");
            }
        }

        public void SaveStatesXML(string show)
        {
            if (File.Exists(".\\" + show + ".xml\\" + statesFile + ".bak"))
                File.Delete(".\\" + show + ".xml\\" + statesFile + ".bak");
            Microsoft.VisualBasic.FileIO.FileSystem.RenameFile(".\\" + show + ".xml\\" + statesFile, statesFile + ".bak");

            using (XmlTextWriter writer = new XmlTextWriter(".\\" + show + ".xml\\" + statesFile, Encoding.UTF8))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartDocument();
                writer.WriteStartElement("mididmx");

                foreach (var state in xStates)
                {
                    writer.WriteStartElement("state");
                    writer.WriteElementString("name", state.name);

                    foreach (var scene in state.scenes)
                        writer.WriteElementString("scene", scene);

                    foreach (var chase in state.chases)
                        writer.WriteElementString("chase", chase);

                    writer.WriteElementString("exclusive", state.exclusive.ToString().ToLower());

                    foreach (var osc in state.oscAddresses)
                        writer.WriteElementString("oscmapping", osc);

                    if (state.tempo != null)
                        writer.WriteElementString("tempo", state.tempo.ToString());

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

        }


        private void ReadFixturesXML(string show)
        {
            XElement root = XElement.Load(show + ".xml\\" + fixtureFile);

            var fixtures = from F in root.Elements("fixture")
                       select F;


            xFixtures.Clear();

            foreach (var fixture in fixtures)
            {
                xFixture newFixture = new xFixture(fixture.Element("name").Value);
                var channels = fixture.Elements("channel");
                foreach (var channel in channels)
                {
                    xChannel newChannel = new xChannel();
                    newChannel.function = channel.Element("function").Value;
                    newChannel.offset = int.Parse(channel.Element("offset").Value);
                    var ranges = channel.Elements("range");
                    foreach (var range in ranges)
                    {
                        xRange newRange = new xRange();
                        newRange.min = byte.Parse(range.Element("min").Value);
                        newRange.max = byte.Parse(range.Element("max").Value);
                        newRange.value = range.Element("value").Value;
                        newChannel.ranges.Add(newRange);
                    }
                    newFixture.channels.Add(newChannel);
                }
                xFixtures.Add(newFixture);
                OutputText("Fixture: " + newFixture.name);
            }

            OutputText("xFixtures Loaded\n");
        }

        private void ReadPatchesXML(string show)
        {
            XElement root = XElement.Load(show + ".xml\\" + patchFile);

            var patches = from P in root.Elements("patch")
                      select P;

            xPatches.Clear();

            foreach(var patch in patches)
            {
                xPatch newPatch = new xPatch(patch.Element("name").Value);
                var fixture = xFixtures.First(a => a.name == patch.Element("fixture").Value);
                newPatch.fixture = fixture;
                //newPatch.fixture = patch.Element("fixture").Value;
                newPatch.address = int.Parse(patch.Element("address").Value);

                xPatches.Add(newPatch);
                OutputText("Patch: " + newPatch.name);
            }

            OutputText("xPatches Loaded\n");
        }

        private void ReadMidiMappingsXML(string show)
        {
            XElement root = XElement.Load(show + ".xml\\" + midiMappingsFile);

            midiMappings = (from S in root.Elements("midimapping")
                           select S).ToList();

            controllerStates.Clear();

            OutputText("");
            OutputText("MIDI Controllers:");

            foreach (XElement controller in midiMappings.Where(a => a.Elements("maptype").First().Value == "controller"))
            {
                try
                {
                    byte midiChannel = byte.Parse(controller.Element("midichannel").Value);
                    byte controllerNumber = byte.Parse(controller.Element("controller").Value);
                    XElement dmxChannelName = controller.Element("dmxchannel");
                    XElement patch = controller.Element("patch");
                    XElement function = controller.Element("function");

                    if (dmxChannelName != null)
                        OutputText(string.Format("Channel: {0}  Controller: {1} DMX Channel: {2}", midiChannel, controllerNumber, dmxChannelName.Value));

                    if (patch != null && function != null)
                        OutputText(string.Format("Channel: {0}  Controller: {1}  Patch: {2}  Function: {3}", midiChannel, controllerNumber, patch.Value, function.Value));

                    controllerStates.Add(new ControllerState(midiChannel, controllerNumber, 0));

                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }

            }
        }

        private void ReadOscMappingsXML(string show)
        {
            xOscMappings.Clear();
            OutputText("");

            var files = Directory.GetFiles(show + ".xml\\", oscMappingsFile);

            foreach (var file in files)
            {
                OutputText("File: " + file);

                if (file.ToLower().Contains("conflicted"))
                    continue;
        
                XElement root = XElement.Load(file);

                foreach (var mapping in root.Elements("oscmapping"))
                    xOscMappings.Add(mapping);

                int x = xOscMappings.Where(a => a.Element("address").Value == "/X32Tap").Count();

                OutputText(x.ToString());
            }


            oscControllerStates.Clear();

            OutputText("");
            OutputText("OSC Controllers:");

            foreach (XElement controller in xOscMappings.Where(a => a.Elements("maptype").First().Value == "controller"))
            {
                try
                {
                    string address = controller.Element("address").Value;
                    XElement dmxChannelName = controller.Element("dmxchannel");
                    XElement patch = controller.Element("patch");
                    XElement function = controller.Element("function");

                    if (dmxChannelName != null)
                        OutputText(string.Format("Address: {0}  DMX Channel: {1}", address, dmxChannelName.Value));

                    if (patch != null && function != null)
                        OutputText(string.Format("Address: {0}  Patch: {1}  Function: {2}", address, patch.Value, function.Value));

                    oscControllerStates.Add(new OscControllerState(address, 0));

                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }

            }
        }

        private void ReadScenesXML(string show)
        {
            xScenes.Clear();

            var files = Directory.GetFiles(".\\" + show + ".xml\\", sceneFile);

            foreach (var file in files)
            {
                OutputText("");
                OutputText("File: " + file);

                if (file.ToLower().Contains("conflicted"))
                {
                    Console.WriteLine("Conflicted file ignored!");
                    continue;
                }

                XElement root = XElement.Load(file);

                OutputText(" Scenes:");

                foreach (var scene in root.Elements("scene"))
                {
                    xScene newScene = new xScene(scene.Element("name").Value);
                    var functions = scene.Elements("function");
                    foreach (var function in functions)
                    {
                        xFunction newFunction = new xFunction();
                        newFunction.patch = function.Element("patch").Value;
                        newFunction.function = function.Element("function").Value;
                        newFunction.value = byte.Parse(function.Element("value").Value);
                        newScene.functions.Add(newFunction);
                    }

                    var xMappings = scene.Elements("oscmapping");
                    foreach (var xMapping in xMappings)
                        newScene.oscMappings.Add(new xOscMapping()
                        {
                            address = xMapping.Element("address").Value,
                            action = xMapping.Element("action").Value
                        });

                    var sceneMappings = xOscMappings.Where(a => a.Element("maptype").Value == "scene");
                    var mappings = sceneMappings.Where(a => a.Element("scenename").Value == newScene.name);
                    foreach (var mapping in mappings)
                    {
                        if (newScene.oscMappings.Count(a => a.address == mapping.Element("address").Value) > 0)
                            continue;

                        newScene.oscMappings.Add(new xOscMapping()
                        {
                            address = mapping.Element("address").Value,
                            action = mapping.Element("action").Value
                        });
                    }

                    xScenes.Add(newScene);
                    OutputText("  Scene: " + newScene.name);
                }

                OutputText("");

            }
        }

        private void ReadChasesXML(string show)
        {
            xChases.Clear();

            var files = Directory.GetFiles(".\\" + show + ".xml\\", chaseFile);

            foreach (var file in files)
            {
                OutputText("");
                OutputText(" File: " + file);

                if (file.ToLower().Contains("conflicted"))
                {
                    Console.WriteLine("Conflicted file ignored!");
                    continue;
                }

                XElement root = XElement.Load(file);

                OutputText(" Chases:");

                foreach (var chase in root.Elements("chase"))
                {
                    xChase newChase = new xChase(chase.Element("name").Value);
                    newChase.fadein = int.Parse(chase.Element("fadein").Value);
                    newChase.hold = int.Parse(chase.Element("hold").Value);
                    var t = chase.Element("tap");
                    if (t != null && t.Value == "true")
                        newChase.tap = true;
                    var steps = chase.Elements("step");
                    foreach (var step in steps)
                    {
                        xChaseStep newStep = new xChaseStep();
                        var functions = step.Elements("function");
                        foreach (var function in functions)
                        {
                            xFunction newFunction = new xFunction();
                            newFunction.patch = function.Element("patch").Value;
                            newFunction.function = function.Element("function").Value;
                            newFunction.value = byte.Parse(function.Element("value").Value);
                            newStep.functions.Add(newFunction);
                        }
                        var scenes = step.Elements("scene");
                        foreach (var scene in scenes)
                        {
                            if (scene.Element("name") != null)
                                newStep.scenes.Add(scene.Element("name").Value);
                            else
                                newStep.scenes.Add(scene.Value);
                        }
                        
                        newChase.steps.Add(newStep);
                    }

                    var xMappings = chase.Elements("oscmapping");
                    foreach (var xMapping in xMappings)
                        newChase.oscMappings.Add(new xOscMapping()
                        {
                            address = xMapping.Element("address").Value,
                            action = xMapping.Element("action").Value
                        });


                    var chaseMappings = xOscMappings.Where(a => a.Element("maptype").Value == "chase");
                    var mappings = chaseMappings.Where(a => a.Element("chasename").Value == newChase.name);
                    foreach (var mapping in mappings)
                    {
                        if (newChase.oscMappings.Count(a => a.address == mapping.Element("address").Value) > 0)
                            continue;

                        newChase.oscMappings.Add(new xOscMapping()
                        {
                            address = mapping.Element("address").Value,
                            action = mapping.Element("action").Value
                        });
                    }

                    xChases.Add(newChase);
                    OutputText("  Chase: " + newChase.name);
                }
            }

            OutputText("");
        }
        
        private void CheckXML()
        {
            CheckMappings();
            CheckPatches();
        }

        private void CheckMappings()
        {
            OutputText("");
            OutputText("Checking Mapping Data:");

            foreach (var mapping in midiMappings)
            {
                try
                {
                    XElement mapType = mapping.Element("maptype");
                    XElement midiChannelElement = mapping.Element("midichannel");
                    XElement controllerNumberElement = mapping.Element("controller");
                    XElement dmxChannelNameElement = mapping.Element("dmxchannel");
                    XElement patchElement = mapping.Element("patch");
                    XElement sceneElement = mapping.Element("scenename");
                    XElement chaseElement = mapping.Element("chasename");
                    XElement functionElement = mapping.Element("function");
                    XElement commandElement = mapping.Element("command");

                    if (mapType == null) throw new Exception("Missing mapping type");
                    if (midiChannelElement == null) throw new Exception("Missing MIDI channel");
                    
                    if (dmxChannelNameElement == null && patchElement == null && functionElement == null && sceneElement == null 
                        && chaseElement == null && commandElement == null && controllerNumberElement == null)
                        throw new Exception("Missing mapping data");

                    byte midiChannel;

                    if (!byte.TryParse(midiChannelElement.Value, out midiChannel))
                        throw new Exception("Invalid MIDI channel");

                }
                catch (Exception ex)
                {
                    OutputText("****" + ex.Message + "****");

                    foreach (XElement element in mapping.Elements())
                    {
                        OutputText(string.Format("    Element: {0}  Value: {1}", element.Name, element.Value));
                    }
                }
            }

            OutputText("Done!");
            OutputText("");
        }

        private void CheckPatches()
        {
            OutputText("Checking Patches:");

            List<string> csv = new List<string>();
            csv.Add("Patch,Fixture,Start,End");

            foreach(var patch in xPatches.OrderBy(a => a.address ))
            {
                try
                {
                    var fixture = patch.fixture;

                    var channels = from C in fixture.channels
                                   select C.offset;

                    int channelCount = channels.Max();

                    int endChannel = patch.address + channelCount;


                    OutputText(string.Format("  Patch: {0}  Fixture: {1}  Start: {2}  End: {3}", patch.name, fixture.name, patch.address, endChannel));
                    csv.Add(string.Format("{0},{1},{2},{3},", patch.name, fixture.name, patch.address, endChannel));
                }
                catch (Exception ex)
                {
                    OutputText("****" + ex.Message + "****");

                    OutputText(string.Format("    Element: Name  Value: {0}", patch.name));
                    OutputText(string.Format("    Element: Fixture  Value: {0}", patch.fixture));
                    OutputText(string.Format("    Element: Address  Value: {0}", patch.address));
                }
            }

            try
            {
                File.WriteAllLines("patches.csv", csv);
                OutputText("Written to patches.csv");
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
            OutputText("Done!");
            OutputText("");

        }

        #endregion File IO


        #region Utility

        private List<string> GetLocalIPv4(NetworkInterfaceType _type)
        {
            List<string> output = new List<string>();
            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == _type && item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (UnicastIPAddressInformation ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            output.Add(ip.Address.ToString());
                        }
                    }
                }
            }
            return output;
        }
        
        private List<System.Net.IPAddress> GetIPv4Address()
        {
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST

            // Get the IP
            return Dns.GetHostAddresses(hostName).ToList();
        }



        #endregion Utility


    }



}
