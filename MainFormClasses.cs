using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DMXServer
{
    public class AllOffTime
    {
        public IPAddress IPAddress { get; set; }
        public DateTime DateTime { get; set; }

        public AllOffTime(IPAddress iPAddress)
        {
            IPAddress = iPAddress;
            DateTime = DateTime.Now;
        }
    }

    public class xOscClient
    {
        public IPAddress IPAddress { get; set; }
        public string OSCAddress { get; set; }

        public xOscClient(IPAddress _IPAddress, string _OSCAddress)
        {
            IPAddress = _IPAddress;
            OSCAddress = _OSCAddress;
        }
    }

    public class xAction
    {
        public string name { get; set; }
        public string action { get; set; }
    }



    public class xOscMapping
    {
        public string address { get; set; }
        public string action { get; set; }

        public xOscMapping() { }

        public xOscMapping(string _address, string _action)
        {
            address = _address;
            action = _action;
        }
    }

    public class xStateOscAddress
    {
        public int page { get; set; }
        public string oscAddress { get; set; }


    }

    


    public class ControllerState
    {
        public byte midiChannel;
        public byte controller;
        public byte value;

        public ControllerState(byte _midiChannel, byte _controller, byte _value)
        {
            midiChannel = _midiChannel;
            controller = _controller;
            value = _value;
        }
    }




    public class VlcFileInfo
    {
        public string name;
        public string path;
    }

}
