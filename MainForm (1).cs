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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using TelldusWrapper;
using CannedBytes.Midi;
using Rug.Osc;
using System.Threading;
using System.Threading.Tasks;

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

        #endregion Globals


        #region Entry points

        public MainForm()
        {
            InitializeComponent();

        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetupForm();
            ReadAllFiles();

            
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            try
            {

                SaveSettings(currentShow);
                WriteAllFiles();

                StopMidi();
                StopOSC();
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
            Reload();


            SaveGlobalSettings();
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
                    return;
                }

                if(address.StartsWith("/setlist/"))
                {
                    if (message[0].ToString() != "1")
                        return;
                    string songNumber = address.Substring(9);
                    var songs = setlistMappings.Where(a => a.Element("position").Value == songNumber);
                    if (songs.Count() != 1)
                        return;
                    SendReaper(songs.First().Element("oscaddress").Value);

                    if (songs.First().Element("lyrics") != null)
                    {
                        string lyrics = songs.First().Element("lyrics").Value;

                        var lines = lyrics.Count(a => a == '\n');

                        int count = 0;
                        int halfway = 0;
                        for (int i = 0; i < lyrics.Length; i++)
                        {
                            if (lyrics[i] == '\n')
                                count++;
                            if (count >= lines / 2)
                            {
                                halfway = i;
                                break;
                            }
                        }

                        int size = 1050 / lines;

                        TransmitOSC("/lyrics1size", size, "9000", sourceIP.ToString(), false);
                        TransmitOSC("/lyrics2size", size, "9000", sourceIP.ToString(), false);

                        OutputText(string.Format("Font size: {0}", size));

                        string lyrics1 = lyrics.Substring(0, halfway);
                        string lyrics2 = lyrics.Substring(halfway + 1);
                        TransmitOSC("/lyrics1", lyrics1, "9000", sourceIP.ToString(), false);
                        TransmitOSC("/lyrics2", lyrics2, "9000", sourceIP.ToString(), false);
                    }
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

        private void SendOscLabels(IPAddress sourceIp)
        {
            if (sourceIp.ToString() == X32IP)
                return;

            OutputText("Sending labels to: " + sourceIp.ToString());

            List<xOscMapping> labels = new List<xOscMapping>();


            foreach (var song in setlistMappings.Where(a => a.Element("position").Value != ""))
            {
                labels.Add(new xOscMapping("/setlist/" + song.Element("position").Value, song.Element("name").Value));
            }

            for (int i = 1; i <= 50; i++)
            {
                var address = "/setlist/" + i.ToString();
                SendOSCText(sourceIp, address, "");
                System.Threading.Thread.Sleep(10);
            }

            foreach (var label in labels.OrderBy(a => a.address))
            {
                SendOSCText(sourceIp, label.address, label.action);
                System.Threading.Thread.Sleep(10);
            }

            
            OutputText("Done!");
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
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(300);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes(":vlcremote")));
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
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMilliseconds(300);
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.Default.GetBytes(":vlcremote")));
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

        #endregion Utility


    }


}
