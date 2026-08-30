//using TelldusWrapper;
using CannedBytes.Midi;
using Rug.Osc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
//using System.Net.Http;
//using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;
using CsvHelper;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace DMXServer
{
    public partial class MainForm : Form
    {

        #region Globals

        delegate void SetTextCallback(string text);
        delegate void SetTrackBarValueCallback(Panel panel,int value);
        MidiReceiver receiver;
        public MidiOutPort midiOutPort;
        public List<XElement> midiMappings;
        public List<XElement> setlistMappings;
        public List<XElement> xOscMappings = new List<XElement>();
        public List<Label> dmxLabels = new List<Label>();
        public bool dmxRunning = false;
        //string fixtureFile = "fixtures.xml";
        //public string patchFile = "patches.xml";
        public string midiMappingsFile = "midimappings.xml";
        string oscMappingsFile = "oscmappings*.xml";
        public string setlistFile = "setlist.xml";
        int setlistPos = 0;
        IPAddress setListIp = null;
        int fxMuteCount = 0;
        static OscReceiver oscReceiver;
        static Thread oscThread;
        public int oscPort = 8000;
        List<string> vlcIPs = new List<string>() { "192.168.1.198" };
        string X32IP = "192.168.1.32";
        int X32Port = 10024;
        int X32TimeFactor = 5;
        public bool debug = true;
        int x32TimerInterval = 500;
        bool oscResendLoopback = true;
        List<string> vlcExtensions = new List<string>() { "*.mov", "*.mp4", "*.mpg" };
        List<VlcFileInfo> vlcFiles = new List<VlcFileInfo>();
        string vlcBrowsePath = @"e$";
        string vlcSelectedFile = "";
        SetlistForm setlistForm;
        string ReaperIP = "127.0.0.1";
        string ReaperPort = "8001";
        int OSCLoopbackPort = 7700;
        List<xOscClient> OscClients = new List<xOscClient>();
        public OscMessage lastOscMessage = new OscMessage("/nomessage");
        public IPAddress lastOscIp;
        List<AllOffTime> AllOffTimes = new List<AllOffTime>();
        bool BaseSceneActive = false;
        string BaseScene = null;
        DateTime x32TapLast = DateTime.MinValue;
        public string currentShow = "";
        bool loadComplete = false;
        int controllerAdd = 0;
        DateTime ignoreReaper = DateTime.MinValue;
        public string oscMarkerSendPort = "7000";


        #endregion Globals


        #region Entry points

        public MainForm()
        {
            InitializeComponent();

        }

        private void cbDarkMode_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                // apply to all open forms
                foreach (System.Windows.Forms.Form f in System.Windows.Forms.Application.OpenForms)
                {
                    ApplyThemeToForm(f, cbDarkMode.Checked);
                }
            }
            catch { }
        }

        public bool DarkModeEnabled { get { try { return cbDarkMode != null && cbDarkMode.Checked; } catch { return false; } } }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetupForm();
            ReadAllFiles();

            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            

            try
            {
                    SaveSettings(currentShow);
                    WriteAllFiles();
    

                StopMidi();
                StopOSC();
                base.OnClosing(e);
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            StartMidi();
            StartOSC();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            StopMidi();
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
            StopOSC();

            LoadSettings(currentShow);

            // Apply theme after settings loaded
            try
            {
                ApplyTheme(cbDarkMode.Checked);
            }
            catch { }
            Reload();


            SaveGlobalSettings();
        }

        private void ApplyTheme(bool dark)
        {
            ApplyThemeToForm(this, dark);
        }

        public void ApplyThemeToForm(System.Windows.Forms.Form form, bool dark)
        {
            if (form == null) return;

            System.Drawing.Color formBack = dark ? System.Drawing.Color.FromArgb(30, 30, 30) : SystemColors.Control;
            System.Drawing.Color panelBack = dark ? System.Drawing.Color.FromArgb(28, 28, 28) : SystemColors.Control;
            System.Drawing.Color windowBack = dark ? System.Drawing.Color.FromArgb(45, 45, 48) : SystemColors.Window;
            System.Drawing.Color controlBack = dark ? System.Drawing.Color.FromArgb(63, 63, 70) : SystemColors.Control;
            System.Drawing.Color fore = dark ? System.Drawing.Color.White : SystemColors.ControlText;

            form.BackColor = formBack;
            form.ForeColor = fore;

            foreach (System.Windows.Forms.Control c in form.Controls)
            {
                try
                {
                    if (c is System.Windows.Forms.Panel)
                    {
                        c.BackColor = panelBack;
                        c.ForeColor = fore;
                        foreach (System.Windows.Forms.Control cc in c.Controls)
                        {
                            if (cc is System.Windows.Forms.Button)
                            {
                                cc.BackColor = controlBack;
                                cc.ForeColor = fore;
                            }
                            else if (cc is System.Windows.Forms.ListBox)
                            {
                                cc.BackColor = windowBack;
                                cc.ForeColor = fore;
                            }
                            else if (cc is System.Windows.Forms.ComboBox)
                            {
                                cc.BackColor = windowBack;
                                cc.ForeColor = fore;
                            }
                            else if (cc is System.Windows.Forms.CheckBox)
                            {
                                cc.BackColor = panelBack;
                                cc.ForeColor = fore;
                            }
                            else if (cc is System.Windows.Forms.Label)
                            {
                                cc.BackColor = panelBack;
                                cc.ForeColor = fore;
                            }
                            else if (cc is System.Windows.Forms.TextBox)
                            {
                                cc.BackColor = windowBack;
                                cc.ForeColor = fore;
                            }
                        }
                    }
                    else if (c is System.Windows.Forms.ListBox)
                    {
                        c.BackColor = windowBack;
                        c.ForeColor = fore;
                    }
                    else if (c is System.Windows.Forms.Button)
                    {
                        c.BackColor = controlBack;
                        c.ForeColor = fore;
                    }
                    else if (c is System.Windows.Forms.ComboBox)
                    {
                        c.BackColor = windowBack;
                        c.ForeColor = fore;
                    }
                    else if (c is System.Windows.Forms.CheckBox)
                    {
                        c.BackColor = formBack;
                        c.ForeColor = fore;
                    }
                    else if (c is System.Windows.Forms.Label)
                    {
                        c.BackColor = formBack;
                        c.ForeColor = fore;
                    }
                    else if (c is System.Windows.Forms.TextBox)
                    {
                        c.BackColor = windowBack;
                        c.ForeColor = fore;
                    }
                }
                catch { }
            }
        }


        #endregion Entry points


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
                    lblOscIp.Text = ip.ToString() + ":" + oscPort.ToString();

                    // Create the receiver
                    oscReceiver = new OscReceiver(ip,oscPort);
                    //oscReceiver = new OscReceiver(ip, 8000);

                    // Create a thread to do the listening
                    oscThread = new Thread(new ThreadStart(OscReceiveLoop));

                    // Connect the receiver
                    oscReceiver.Connect();

                    // Start the listen thread
                    oscThread.Start();

                    if (oscReceiver.State == OscSocketState.Connected)
                    {
                        OutputText("** OSC Receiving **");
                        
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
                    // Close the receiver so the receive loop can exit gracefully
                    oscReceiver.Close();

                    // Wait briefly for the listen thread to exit, then interrupt if still alive
                    if (oscThread != null && oscThread.IsAlive)
                    {
                        if (!oscThread.Join(500))
                        {
                            //try { oscThread.Interrupt(); } catch { }
                        }
                    }

                    OutputText("** OSC closed **");
                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }
            }
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
                            //oscReceiver.Port = 123456;

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
                IPAddress sourceIP = message.Origin.Address;

                lastOscIp = sourceIP;

                string address = message.Address;

                OutputText(string.Format("\nOSC Message Received [{0}]: {1}", message.Origin.Address, address));
                OutputText(string.Format("OSC Message value: {0}", message[0].ToString()));

                if (address.StartsWith("/midi"))
                {
                    var midiType = address.Split('/')[2];

                    if (midiType == "progchange")
                    {
                        var midiChannel = address.Split('/')[3];
                        var midiProgram = address.Split('/')[4];

                        OutputText($"Send Midi channel: {midiChannel} controller {midiProgram}");
                        SendMidiProgramChange(byte.Parse(midiChannel), byte.Parse(midiProgram));

                    }
                    return;
                }

                if (address == "/lastmarker/name" && message[0].ToString() != "")
                {
                    string markerAddress = address + "/" + message[0];

                    OutputText(string.Format( "Send Marker address: {0} to localhost on port {1}", markerAddress, oscMarkerSendPort));
                                        
                    TransmitOSC(markerAddress, (float)1.0, oscMarkerSendPort, "127.0.0.1", true);

                }

                if (address.StartsWith("/fadersync") && sourceIP.ToString() != ReaperIP.ToString() && sourceIP.ToString() != oscReceiver.LocalAddress.ToString())
                {
                    if (OscClients.Select(a => a.IPAddress).Contains(sourceIP))
                        OscClients.RemoveAll(a => a.IPAddress.ToString() == sourceIP.ToString());
                    else
                        SendOscLabels(sourceIP);

                    string oscAddress = message.Address.Replace("/fadersync", "");
                    OscClients.Add(new xOscClient(sourceIP, oscAddress ));                   
                    OscAllOff(sourceIP);
                    return;
                }

                if (message.Count < 1)
                    return;

                if (address.StartsWith("/midimappings") && message[0].ToString() == "1")
                {
                    string fileName = address.Replace("/", "");

                    if (!File.Exists(currentShow + ".xml\\" + fileName))
                        return;

                    midiMappingsFile = fileName;

                    ReadMidiMappingsXML(currentShow);
                    
                }
                                

                if (address.ToLower().Contains("/reaper/") && (message[0] is System.Single || message[0] is System.Int32) && float.Parse(message[0].ToString()) == 1)
                {
                    SendReaper(address);

                    if (address.ToLower() == "/reaper/stop")
                    {
                        ignoreReaper = DateTime.Now;

                    }

                    return;
                }

                if (address == "/FXOn" && message[0].ToString() == "1" && setListIp != null)
                {
                    TransmitOSC("/setlist/btn_go", 1, "9000", setListIp.ToString(), false);
              
                }

                if (address == "/FXOn" && message[0].ToString() == "0" && setListIp != null)
                {
                    TransmitOSC("/setlist/btn_go", 0, "9000", setListIp.ToString(), false);

                    if (fxMuteCount++ > 1 && ignoreReaper < DateTime.Now.AddSeconds(fxMuteCount - 4))
                    {
             
                        var setlist = setlistMappings.Where(a => a.Element("position").Value != "").OrderBy(b => int.Parse(b.Element("position").Value)).ToList();
                        var currentSong = setlist[setlistPos];

                        setlistPos++;
                        UpdateSetList();
                        fxMuteCount = 0;

                        var song = setlist[setlistPos];
                        var osc = song.Element("oscaddress").Value;
                        if (currentSong.Element("continue").Value == "1" && ignoreReaper < DateTime.Now.AddSeconds(-4))
                            SendReaper(osc);
                    }
                }


                if (address.StartsWith("/setlist/"))
                {
                    if (message[0].ToString() != "1")
                        return;

                    setListIp = sourceIP;

                    if (address == "/setlist/btn_go")
                    {
                        var setlist = setlistMappings.Where(a => a.Element("position").Value != "").OrderBy(b => int.Parse(b.Element("position").Value)).ToList();
                        var song = setlist[setlistPos];
                        var osc = song.Element("oscaddress").Value;
                        SendReaper(osc);
                        //setlistPos++;
                    }

                    if (address == "/setlist/btn_next")
                        setlistPos++;

                    if (address == "/setlist/btn_prev" && setlistPos > 0)
                        setlistPos--;

                    SendOscLabels(sourceIP);


                }

                float value = 0;

                lastOscMessage = message;

                if ((message[0] is System.Single))
                    value = (float)message[0];
                else
                    float.TryParse(message[0].ToString(), out value);

                if (debug) OutputText(string.Format("\nOSC Message Received [{0}]: {1}", message.Origin.Address, address));
                if (debug) OutputText(string.Format("OSC Message value: {0}", value));


                ProcessOsc(address, value, message.Origin.Address);
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

        }

        private void SendReaper(string sendAddress)
        {
            OutputText("Reaper passthrough: " + sendAddress);
            TransmitOSC(sendAddress, (float)1.0, ReaperPort, ReaperIP, true);
        }
        
        public void ProcessOsc(string address, float value, IPAddress sourceIp)
        {

            
            var commandMappings = from M in xOscMappings
                                  where M.Element("maptype").Value == "command"
                                  && M.Element("address").Value == address
                                  select M;

            ProcessCommands(commandMappings, value, address, sourceIp);


            var allOff = AllOffTimes.Where(a => a.IPAddress.ToString() == sourceIp.ToString());
            if (allOff.Count() > 0 && value == 0)
            {
                if (allOff.First().DateTime > DateTime.Now.AddSeconds(-1))
                {
                    OutputText("AllOff Excluded " + sourceIp.ToString());
                    return;
                }
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


        private void OscAllOff(IPAddress sourceIp)
        {
            if (sourceIp == null)
                return;

            if (sourceIp.ToString() == "0.0.0.0")
                return;




            AllOffTimes.RemoveAll(a => a.IPAddress.ToString() == sourceIp.ToString());
            AllOffTimes.Add(new AllOffTime(sourceIp));
        }

        private void UpdateSetList()
        {
            OutputText(string.Format("Update setlist: {0}, position {1} mute count {2}", setListIp.ToString(), setlistPos, fxMuteCount));

            var setlist = setlistMappings.Where(a => a.Element("position").Value != "").OrderBy(b => int.Parse(b.Element("position").Value)).ToList();

            if (setlistPos > setlist.Count - 1)
                setlistPos--;

            if (setlistPos == 0)
                SendOSCText(setListIp, "/setlist/prev", "");
            else
                SendOSCText(setListIp, "/setlist/prev", setlist[setlistPos - 1].Element("name").Value);

            SendOSCText(setListIp, "/setlist/current", setlist[setlistPos].Element("name").Value);

            if (setlistPos >= setlist.Count - 1)
                SendOSCText(setListIp, "/setlist/next", "");
            else
                SendOSCText(setListIp, "/setlist/next", setlist[setlistPos + 1].Element("name").Value);

            OutputText("Done!");

        }

        private void SendOscLabels(IPAddress sourceIp)
        {
            if (sourceIp.ToString() == X32IP)
                return;

            fxMuteCount = 0;

            setListIp = sourceIp;

            UpdateSetList();
        }

        public void SendOSCText(IPAddress sourceIp, string address, string name)
        {
            try
            {
                //OutputText(address + "/text" + " " + name);
                TransmitOSC(address + "/text", name, "9000", sourceIp.ToString(), false);

            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        #endregion OSC




        #region Common MIDI/OSC


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

        private void ProcessCommand(XElement command, float value, IPAddress sourceIp)
        {
            string dmxCommand = command.Element("command").Value;
            if (debug) OutputText("Command: " + dmxCommand);
            DateTime now = DateTime.Now;
            
            switch (dmxCommand)
            {
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
                    if (value == 1)
                    {
                        string x32TapSendAddress = command.Element("sendaddress").Value;
                        TransmitX32Tap(x32TapSendAddress);
                    }
                    break;

                case ("labels"):
                    if (value == 1)
                        SendOscLabels(sourceIp);
                    break;
                case ("midiprogchange"):
                    byte midiChannel = byte.Parse(command.Element("midichannel").Value);
                    byte midiProgram = byte.Parse(command.Element("value").Value);
                    SendMidiProgramChange(midiChannel, midiProgram);
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
            byte velocity = a.velocity;

            if (pitch >= 100 && pitch <= 103)
            {
                controllerAdd = (pitch - 100) * 10;
                OutputText(String.Format("Controller Offset: {0}", controllerAdd));
            }

            var commandMappings = from M in midiMappings
                                  where M.Element("maptype").Value == "command"
                                  && M.Element("midichannel").Value == midiChannel.ToString()
                                  && M.Element("note").Value == pitch.ToString()
                                  select M;

            float value = velocity > 0 ? 1 : 0;

            ProcessCommands(commandMappings, value, null, null);
        }

        private void NoteOffHandler(object sender, EventArgs e)
        {
            MidiNoteEventArgs a = (MidiNoteEventArgs)e;
            if(debug) OutputText(string.Format("Note off - ch {0}  note {1}  velocity {2}", a.channel, a.note, a.velocity));

            byte midiChannel = a.channel;
            byte pitch = a.note;

        }

        private void ControllerHandler(object sender, EventArgs e)
        {
            MidiControllerEventArgs a = (MidiControllerEventArgs)e;
            if (debug) OutputText(string.Format("Midi controller - ch {0}  controller {1}  value {2}", a.channel, a.controller, a.value));

            byte midiChannel = a.channel;
            byte controllerNumber = a.controller;
            byte value = a.value;

            SendMidiController(midiChannel, (byte)(controllerNumber + controllerAdd), value);
        }

        private void SendMidiController(byte channel, byte controller, byte value)
        {
            try
            {
                MidiData midiData = new MidiData();
                midiData.Status = (byte)(0xC0 + channel - 1);
                midiData.Parameter1 = controller;
                midiData.Parameter2 = value;

                midiOutPort.ShortData(midiData);
                OutputText(String.Format("Midi sent: {0} {1} {2}", midiData.Status, midiData.Parameter1, midiData.Parameter2));
            }
            catch { }
        }

        private void PitchBendHandler(object sender, EventArgs e)
        {
            MidiPitchBendEventArgs a = (MidiPitchBendEventArgs)e;
            int value = (int)a.value2 * 128 + a.value1;

            if (debug) OutputText(string.Format("Midi pitch - ch {0}  value1 {1}  value2 {2} value {3}", a.channel, a.value1, a.value2, value));



        }

        private void ProgChangeHandler(object sender, EventArgs e)
        {
            MidiProgChangeEventArgs a = (MidiProgChangeEventArgs)e;
            if (debug) OutputText(string.Format("Midi prog change - ch {0}  program {1}", a.channel, a.program));

            byte midiChannel = a.channel;
            byte program = a.program;



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
                int portId = 0;
                if (cbMidi.SelectedItem != null)
                    portId = midiInCaps.ToList().FindIndex(a => a.Name == cbMidi.SelectedItem.ToString());

                receiver = new MidiReceiver();
                receiver.NoteOnHandler += new EventHandler(NoteOnHandler);
                receiver.NoteOffHandler += new EventHandler(NoteOffHandler);
                receiver.ControllerHandler += new EventHandler(ControllerHandler);
                receiver.ProgChangeHandler += new EventHandler(ProgChangeHandler);
                receiver.PitchBendHandler += new EventHandler(PitchBendHandler);
                
                
                receiver.Start(portId);

                if (receiver.isRunning)
                {
                    OutputText("** MIDI receiving. **");
                }

            }
            catch (Exception ex)
            {
                OutputText("MIDI Error: " + ex.Message);
                OutputText(ex.ToString());
            }

            try
            {
                if (cbMidiOut.SelectedItem != null)
                {

                    MidiOutPortCapsCollection midiOutCaps = new MidiOutPortCapsCollection();
                    int outPortId = midiOutCaps.ToList().FindIndex(a => a.Name == cbMidiOut.SelectedItem.ToString());

                    midiOutPort = new MidiOutPort();
                    midiOutPort.Open(outPortId);

                    if (midiOutPort.IsOpen)
                    {
                        OutputText("** MIDI Out enabled **");
                    }
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


        #region Setlist

        private void btnSetlist_Click(object sender, EventArgs e)
        {
            if (setlistForm != null)
                if (setlistForm.Visible)
                    return;

            setlistForm = new SetlistForm(this);
            setlistForm.Show();
        }

        #endregion Setlist


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
        


            btnStop.Enabled = false;
            btnStart.Enabled = true;

            LoadGlobalSettings();

            var shows = Directory.GetDirectories(".\\", "*.xml");

            ddlShows.Items.Clear();
            foreach (var show in shows)
                ddlShows.Items.Add(show.Substring(2).Replace(".xml",""));
            ddlShows.SelectedItem = currentShow;


            LoadSettings(currentShow);

            // Apply theme to all open forms based on global setting
            try
            {
                foreach (System.Windows.Forms.Form f in System.Windows.Forms.Application.OpenForms)
                {
                    ApplyThemeToForm(f, cbDarkMode.Checked);
                }
            }
            catch { }

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
                if (root.Element("DarkMode") != null)
                {
                    try { cbDarkMode.Checked = (root.Element("DarkMode").Value.ToLower() == "true"); } catch { }
                }
                // apply theme immediately
                try { foreach (System.Windows.Forms.Form f in System.Windows.Forms.Application.OpenForms) ApplyThemeToForm(f, cbDarkMode.Checked); } catch { }
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
                    writer.WriteElementString("DarkMode", cbDarkMode.Checked.ToString());

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


                using (XmlTextWriter writer = new XmlTextWriter(show +  ".xml\\settings.xml", Encoding.UTF8))
                {
                    writer.Formatting = Formatting.Indented;
                    writer.WriteStartDocument();
                    writer.WriteStartElement("Settings");

                    writer.WriteElementString("MIDIDevice", (cbMidi.SelectedItem ?? "").ToString());
                    writer.WriteElementString("MIDIOutDevice", (cbMidiOut.SelectedItem ?? "").ToString());
                    writer.WriteElementString("Running", btnStop.Enabled.ToString());
                    writer.WriteElementString("MidiMappingsFile", midiMappingsFile);
                    writer.WriteElementString("OSCMappingsFile", oscMappingsFile);
                    
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
                    //writer.WriteElementString("X32Meters", cbMeters.Checked.ToString());


                                    
                    writer.WriteElementString("Debug", debug.ToString());

                    
                    writer.WriteElementString("OscResendLoopback", oscResendLoopback.ToString());

                    writer.WriteElementString("ReaperIP", ReaperIP);
                    writer.WriteElementString("ReaperPort", ReaperPort);
                    //writer.WriteElementString("ReaperControlIP", ReaperControlIp.ToString());
                    //writer.WriteElementString("ReaperControlPort", ReaperControlPort.ToString());

                    writer.WriteElementString("OSCLoopbackPort", OSCLoopbackPort.ToString());
                    //writer.WriteElementString("OSCLoopbackEnabled", cbLoopback.Checked.ToString());


                    writer.WriteElementString("OutTextEnabled", cbOutText.Checked.ToString());
                    writer.WriteElementString("DarkMode", cbDarkMode.Checked.ToString());

                    //writer.WriteElementString("EnableArtNet", cbArtNet.Checked.ToString());
                    //writer.WriteElementString("ArtNetAddress", artNetBroadcastIP);
                    
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

 
                if (root.Element("MidiMappingsFile") != null) midiMappingsFile = root.Element("MidiMappingsFile").Value;
                if (root.Element("OSCMappingsFile") != null) oscMappingsFile = root.Element("OSCMappingsFile").Value;

                

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
                //if (root.Element("X32Meters") != null) cbMeters.Checked = (root.Element("X32Meters").Value.ToLower() == "true");

                       
                if (root.Element("Debug") != null) debug = root.Element("Debug").Value.ToLower() == "true";
                                
                if (root.Element("OscResendLoopback") != null) oscResendLoopback = (root.Element("OscResendLoopback").Value.ToLower() == "true");

                if (root.Element("ReaperIP") != null) ReaperIP = root.Element("ReaperIP").Value;
                if (root.Element("ReaperPort") != null) ReaperPort = root.Element("ReaperPort").Value;
                //if (root.Element("ReaperControlIP") != null) ReaperControlIp = IPAddress.Parse(root.Element("ReaperControlIP").Value);
                //if (root.Element("ReaperControlPort") != null) int.TryParse(root.Element("ReaperControlPort").Value, out ReaperControlPort);

                if (root.Element("OSCLoopbackPort") != null) int.TryParse(root.Element("OSCLoopbackPort").Value, out OSCLoopbackPort);
                //if (root.Element("OSCLoopbackEnabled") != null) cbLoopback.Checked = (root.Element("OSCLoopbackEnabled").Value.ToLower() == "true");

                if (root.Element("BaseScene") != null) BaseScene = root.Element("BaseScene").Value;
                if (root.Element("BaseSceneActive") != null) BaseSceneActive = (root.Element("BaseSceneActive").Value.ToLower() == "true");

                if (root.Element("OutTextEnabled") != null) cbOutText.Checked = (root.Element("OutTextEnabled").Value.ToLower() == "true");
                if (root.Element("DarkMode") != null)
                {
                    try { cbDarkMode.Checked = (root.Element("DarkMode").Value.ToLower() == "true"); }
                    catch { cbDarkMode.Checked = false; }
                }

                // Apply theme immediately after loading setting
                try { ApplyTheme(cbDarkMode.Checked); } catch { }


            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }

            if (run)
            {
                StartMidi();
                StartOSC();

            }

            
        }

        private void ReadAllFiles()
        {
            try
            {
                ReadMidiMappingsXML(currentShow);
                ReadSetlistXML(currentShow);
                ReadOscMappingsXML(currentShow);
                loadComplete = true;
                xOscMappings.RemoveAll(a => a.Element("maptype").Value == "scene");
                xOscMappings.RemoveAll(a => a.Element("maptype").Value == "chase");
            }
            catch (Exception ex)
            {
                OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
            }
        }

        private void WriteAllFiles()
        {
            if(!loadComplete)
            {
                OutputText("File load incomplete - abandoning save!");
                return;
            }


        }
               
        
        
        private void ReadMidiMappingsXML(string show)
        {
            XElement root = XElement.Load(show + ".xml\\" + midiMappingsFile);

            midiMappings = (from S in root.Elements("midimapping")
                           select S).ToList();

            
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

                    
                }
                catch (Exception ex)
                {
                    OutputText("Error in " + System.Reflection.MethodBase.GetCurrentMethod().Name + ": " + ex.Message);
                }

            }
			
			OutputText("MIDI mapping file loaded: " + midiMappingsFile);
        }

        private void ReadSetlistXML(string show)
        {
            XElement root = XElement.Load(show + ".xml\\" + setlistFile);

            setlistMappings = (from S in root.Elements("song")
                            select S).ToList();

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

        private void ExportSetlist()
        {
            var setlistPath = Path.Combine(AppContext.BaseDirectory,currentShow + ".xml", "setlist.xml");
            var songsPath = Path.Combine(AppContext.BaseDirectory, "Songs.csv");
            var outputPath = Path.Combine(AppContext.BaseDirectory, "Setlist.docx");
            var title = $"Tiggers - Setlist {DateTime.Today:dd/MM/yyyy}";

            // Print file locations to the console
            OutputText("Exporting setlist...");
            OutputText($"Setlist file: {setlistPath}");
            OutputText($"Songs CSV: {songsPath}");
            OutputText($"Output file: {outputPath}");

            var notes = LoadNotes(songsPath);

            var songs =
                XDocument.Load(setlistPath)
                .Descendants("song")
                .Where(s =>
                {
                    var pos = (string?)s.Element("position");
                    return !string.IsNullOrWhiteSpace(pos);
                })
                .Select(s => new
                {
                    Name = ((string?)s.Element("name"))?.Trim() ?? "",
                    Position = int.Parse(((string?)s.Element("position"))!)
                })
                .OrderBy(s => s.Position)
                .ToList();

            CreateWordDocument(outputPath, title, songs, notes);

            OutputText($"Created: {outputPath}");
        }

        private Dictionary<string, string> LoadNotes(string csvFile)
        {
            using var reader = new StreamReader(csvFile);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            var records = csv.GetRecords<SongRecord>();

            return records
                .Where(r => !string.IsNullOrWhiteSpace(r.Title))
                .ToDictionary(
                    r => Normalize(r.Title),
                    r => r.Note?.Trim() ?? "",
                    StringComparer.OrdinalIgnoreCase);
        }

        private string? FindNewestFile(string rootPath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                return null;

            string? newest = null;
            var newestTime = DateTime.MinValue;

            try
            {
                foreach (var file in Directory.EnumerateFiles(rootPath, fileName, SearchOption.AllDirectories))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        if (info.LastWriteTime > newestTime)
                        {
                            newestTime = info.LastWriteTime;
                            newest = file;
                        }
                    }
                    catch
                    {
                        // ignore inaccessible files
                    }
                }
            }
            catch
            {
                // ignore directory access exceptions
            }

            return newest;
        }

        private void CreateWordDocument(
            string fileName,
            string title,
            IEnumerable<dynamic> songs,
            Dictionary<string, string> notes)
        {
            using var document =
                WordprocessingDocument.Create(
                    fileName,
                    WordprocessingDocumentType.Document);

            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new Document();

            var body = new DocumentFormat.OpenXml.Wordprocessing.Body();

            var sectionProps = new SectionProperties(
                new PageMargin
                {
                    Top = 720,
                    Bottom = 720,
                    Left = 720,
                    Right = 720
                });

            // Add title as first line using Calibri 24pt bold
            var titleRun = new Run(
                new RunProperties(
                    new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                    new Bold(),
                    new FontSize { Val = "48" } // 24pt
                ),
                new DocumentFormat.OpenXml.Wordprocessing.Text(title)
            );

            var titleParagraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(titleRun);
            body.Append(titleParagraph);

            foreach (var song in songs)
            {
                notes.TryGetValue(Normalize(song.Name), out string note);

                var text =
                    string.IsNullOrWhiteSpace(note)
                        ? song.Name
                        : $"{song.Name} {note}";

                var run = new Run(
                    new RunProperties(
                        new RunFonts { Ascii = "Calibri", HighAnsi = "Calibri" },
                        new Bold(),
                        new FontSize { Val = "40" } // 20pt
                    ),
                    new DocumentFormat.OpenXml.Wordprocessing.Text(text)
                );

                var paragraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(run);

                body.Append(paragraph);
                if (string.Equals(
                        song.Name,
                        "Bubbles",
                        StringComparison.OrdinalIgnoreCase))
                {
                    // Add a horizontal line on the line below by adding an empty paragraph
                    // with a top border
                    var hrParagraph = new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                        new ParagraphProperties(
                            new ParagraphBorders(
                                new TopBorder
                                {
                                    Val = BorderValues.Single,
                                    Size = 6,
                                    Color = "000000"
                                })));

                    body.Append(hrParagraph);
                }


                if (string.Equals(
                        song.Name,
                        "Hysteria Cydonia",
                        StringComparison.OrdinalIgnoreCase))
                {
                    body.Append(
                        new Paragraph(
                            new Run(
                                new Break
                                {
                                    Type = BreakValues.Page
                                })));
                }
            }

            body.Append(sectionProps);

            mainPart.Document.Append(body);
            mainPart.Document.Save();
        }

        static string Normalize(string value)
        {
            return value
                .Trim()
                .Replace("’", "'")
                .ToLowerInvariant();
        }

        public class SongRecord
        {
            public string Title { get; set; } = "";
            public string Note { get; set; } = "";
        }


        #endregion Utility

        private void btnSetlistExport_Click(object sender, EventArgs e)
        {
            try
            {
                ExportSetlist();
            } catch (Exception ex)
            {
                OutputText($"Error occurred: {ex.Message}");
            }
        }
    }


}
