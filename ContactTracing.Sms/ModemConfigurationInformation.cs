using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace ContactTracing.Sms
{
    /// <summary>
    /// A class used to handle SMS modem configuration
    /// </summary>
    [Serializable()]
    public sealed class ModemConfigInfo
    {
        private ObservableCollection<string> _comPorts = new ObservableCollection<string>();
        private ObservableCollection<string> _baudRates = new ObservableCollection<string>();

        [XmlElement]
        public string ComPort { get; set; }

        [XmlElement]
        public string OwnPhoneNumber { get; set; }

        [XmlElement]
        public string BaudRate { get; set; }

        [XmlElement]
        public int DataBits { get; set; }

        [XmlElement]
        public int StopBits { get; set; }

        [XmlElement]
        public string ParityBits { get; set; }

        [XmlElement]
        public int ReadTimeout { get; set; }

        [XmlElement]
        public int WriteTimeout { get; set; }

        [XmlIgnore]
        public ObservableCollection<string> ComPorts
        {
            get
            {
                return this._comPorts;
            }
        }

        [XmlIgnore]
        public ObservableCollection<string> BaudRates
        {
            get
            {
                return this._baudRates;
            }
        }

        /// <summary>
        /// Gets/sets the list of AT commands to run on startup
        /// </summary>
        [XmlElement]
        public List<string> StartupCommands { get; set; }

        public ModemConfigInfo()
        {
            Construct();
            StartupCommands = new List<string>();
        }

        public ModemConfigInfo(string unparsedCommands)
        {
            Construct();
            StartupCommands = new List<string>();

            string[] commands = unparsedCommands.Split('\n');
            foreach (string command in commands)
            {
                StartupCommands.Add(command);
            }
        }

        private void Construct()
        {
            ComPort = Properties.Settings.Default.LastUsedComPort;
            OwnPhoneNumber = "5555555555";
            BaudRate = Properties.Settings.Default.LastUsedBaudRate.ToString();
            DataBits = 8;
            StopBits = 1;
            ParityBits = "None";
            ReadTimeout = 300;
            WriteTimeout = 300;

            foreach (string s in System.IO.Ports.SerialPort.GetPortNames())
            {
                ComPorts.Add(s);
            }

            BaudRates.Add("4800");
            BaudRates.Add("9600");
            BaudRates.Add("19200");
            BaudRates.Add("38400");
            BaudRates.Add("57600");
        }
    }
}
