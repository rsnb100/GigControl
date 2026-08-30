using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CannedBytes.Midi;

namespace DMXServer
{
    class MidiReceiver : IMidiDataReceiver, IDisposable
    {
        private MidiInPort _inPort = new MidiInPort();
        public EventHandler NoteOnHandler;
        public EventHandler NoteOffHandler;
        public EventHandler ControllerHandler;
        public EventHandler ProgChangeHandler;
        public EventHandler PitchBendHandler;
        public bool isRunning;

        public void Start(int inPortId)
        {
            try
            {
                _inPort.Successor = this;
                _inPort.Open(inPortId);
                _inPort.Start();
                isRunning = true;
            }
            catch { }
        }

        public void Stop()
        {
            _inPort.Stop();
            _inPort.Close();
            isRunning = false;
        }


        public void ShortData(int data, long timestamp)
        {
            try
            {
                MidiData eventData = new MidiData(data);
                byte status = eventData.Status;
                                
                // note on
                if (status >= 0x90 && status < 0xA0)
                    NoteOnHandler(this, new MidiNoteEventArgs(++eventData.Channel, eventData.Parameter1, eventData.Parameter2));

                // note off
                if (status >= 0x80 && status < 0x90)
                    NoteOffHandler(this, new MidiNoteEventArgs(++eventData.Channel, eventData.Parameter1, eventData.Parameter2));

                //controller
                if (status >= 0xB0 && status < 0xC0)
                    ControllerHandler(this, new MidiControllerEventArgs(++eventData.Channel, eventData.Parameter1, eventData.Parameter2));

                //prog change
                if (status >= 0xC0 && status < 0xD0)
                    ProgChangeHandler(this, new MidiProgChangeEventArgs(++eventData.Channel, eventData.Parameter1));

                //pitch bend
                if (status >= 0xE0 && status < 0xEF)
                    PitchBendHandler(this, new MidiPitchBendEventArgs(++eventData.Channel, eventData.Parameter1, eventData.Parameter2));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        public void LongData(MidiBufferStream buffer, long timestamp)
        {
            // not used for short midi messages
        }

        #region IDisposable Members

        public void Dispose()
        {
            try
            {
                _inPort.Dispose();
            }
            catch { }
        }

        #endregion IDisposable Members
    }

    public class MidiNoteEventArgs : EventArgs
    {
        public byte channel;
        public byte note;
        public byte velocity;

        public MidiNoteEventArgs(byte ch, byte nt, byte vel)
        {
            channel = ch;
            note = nt;
            velocity = vel;
        }
    }

    public class MidiControllerEventArgs : EventArgs
    {
        public byte channel;
        public byte controller;
        public byte value;

        public MidiControllerEventArgs(byte ch, byte cnt, byte val)
        {
            channel = ch;
            controller = cnt;
            value = val;
        }
    }

    public class MidiProgChangeEventArgs : EventArgs
    {
        public byte channel;
        public byte program;
        
        public MidiProgChangeEventArgs(byte ch, byte prog)
        {
            channel = ch;
            program = ++prog;
        }
    }

    public class MidiPitchBendEventArgs : EventArgs
    {
        public byte channel;
        public byte value1;
        public byte value2;

        public MidiPitchBendEventArgs(byte ch, byte val1, byte val2)
        {
            channel = ch;
            value1 = val1;
            value2 = val2;
        }
    }

}
