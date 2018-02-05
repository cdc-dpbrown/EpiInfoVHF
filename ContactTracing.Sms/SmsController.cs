using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using ContactTracing.Core;
using ContactTracing.Core.Data;
using ContactTracing.Sms;

namespace ContactTracing.Sms
{
    /// <summary>
    /// A class used to handle sending and receiving Sms messages
    /// </summary>
    public sealed class SmsController : IDisposable
    {
        #region Members
        private object _execCommandLock = new object();
        #endregion // Members

        #region Properties
        private AutoResetEvent ReceiveNow { get; set; }
        private SerialPort Port { get; set; }
        private Queue<ShortMessage> SmsMessages { get; set; }
        public System.Timers.Timer UpdateTimer { get; set; }
        public ModemConfigInfo ModemConfiguration { get; private set; }
        #endregion // Properties

        #region Events
        public event SmsReceivedHandler SmsReceived;
        #endregion // Events

        #region Constructors
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="modemInfo">Configuration information for the Sms modem</param>
        /// <param name="pollRate">The rate at which to check the modem for incoming SMS messages in milliseconds</param>
        public SmsController(ModemConfigInfo modemInfo, int pollRate)
        {
            ModemConfiguration = modemInfo;

            OpenPort();

            ExecuteStartupCommands();

            SmsMessages = new Queue<ShortMessage>();
            UpdateTimer = new System.Timers.Timer(pollRate); // 6 seconds
            UpdateTimer.Elapsed += UpdateTimer_Elapsed;
            UpdateTimer.Start();
        }
        #endregion // Constructors

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                UpdateTimer.Stop();
                UpdateTimer.Dispose();
                // free managed resources
                ClosePort(Port);
            }
            // free native resources if there are any.
        }

        /// <summary>
        /// Handles each tick of the poll timer
        /// </summary>
        /// <param name="sender">The timer that fired this event</param>
        /// <param name="e">The event arguments</param>
        private void UpdateTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            int messageCount = CountSMSMessages();
            if (messageCount > 0)
            {
                //UpdateTimer.Stop();

                ShortMessageCollection messages = ReadSMS(Port, "AT+CMGL=\"ALL\"");
                foreach (ShortMessage message in messages)
                {
                    SmsMessages.Enqueue(message);
                }

                if (DeleteSMS(Port, "AT+CMGD=1,4"))
                {
                    // success on deletion!
                }
                else
                {
                    // failed to delete, which is bad, uh-oh...
                    throw new InvalidOperationException("Can't proceed - old SMS messages cannot be deleted.");
                }

                do
                {
                    if (SmsMessages.Count > 0)
                    {
                        ShortMessage message = SmsMessages.Dequeue();
                        ExecuteMessageContent(message);
                    }
                    
                } while (SmsMessages.Count != 0);

                //UpdateTimer.Start();
            }
        }

        /// <summary>
        /// Deletes an SMS message from the modem
        /// </summary>
        /// <param name="port">The modem's port</param>
        /// <param name="command">The AT command to execute</param>
        /// <returns>bool; whether the deletion was successful</returns>
        private bool DeleteSMS(SerialPort port, string command)
        {
            bool isDeleted = false;
            try
            {

                #region Execute Command
                string recievedData = ExecCommand("AT", 300, "No phone connected");
                recievedData = ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                recievedData = ExecCommand(command, 300, "Failed to delete message");
                #endregion

                if (recievedData.EndsWith("\r\nOK\r\n"))
                {
                    isDeleted = true;
                }
                if (recievedData.Contains("ERROR"))
                {
                    isDeleted = false;
                }
                return isDeleted;
            }
            catch
            {
                throw;
            }
        }  

        /// <summary>
        /// Executes the content of a parsed SMS message
        /// </summary>
        /// <param name="message">The SMS message to be executed</param>
        private void ExecuteMessageContent(ShortMessage message)
        {
            if (this.SmsReceived != null)
            {
                Core.Events.SmsReceivedArgs args = new Core.Events.SmsReceivedArgs(message);
                SmsReceived(this, args);
            }
        }

        /// <summary>
        /// Opens a SerialPort port for SMS communications
        /// </summary>
        /// <param name="modemInfo">The modem configuration information</param>
        /// <returns>SerialPort; represents the port that was opened</returns>
        private void OpenPort()
        {
            ReceiveNow = new AutoResetEvent(false);

            if (Port != null && Port.IsOpen)
            {
                Port.Close();
                Port.Dispose();
            }

            Port = new SerialPort();

            try
            {
                Port.PortName = ModemConfiguration.ComPort;
                Port.BaudRate = int.Parse(ModemConfiguration.BaudRate);
                Port.DataBits = ModemConfiguration.DataBits;
                Port.StopBits = StopBits.One;                  //1
                Port.Parity = Parity.None;                     //None
                Port.ReadTimeout = ModemConfiguration.ReadTimeout;
                Port.WriteTimeout = ModemConfiguration.WriteTimeout;
                Port.Encoding = Encoding.GetEncoding("iso-8859-1");
                Port.NewLine = '\r'.ToString();
                Port.DataReceived += new SerialDataReceivedEventHandler(port_DataReceived);
                Port.Open();
                Port.DtrEnable = true;
                Port.RtsEnable = true;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// Closes the specified serial port
        /// </summary>
        /// <param name="port">The port to close</param>
        private void ClosePort(SerialPort port)
        {
            try
            {
                port.Close();
                port.DataReceived -= new SerialDataReceivedEventHandler(port_DataReceived);
                port = null;
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// An event handler for receiving data from a serial port
        /// </summary>
        /// <param name="sender">The .NET object that sent this event</param>
        /// <param name="e">Event arguments for this event</param>
        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                if (e.EventType == SerialData.Chars)
                {
                    ReceiveNow.Set();
                }
            }
            catch
            {
                throw;
            }
        }

        internal string ExecCommand(string command, int responseTimeout, string errorMessage)
        {
            lock (_execCommandLock)
            {
                try
                {
                    Port.DiscardOutBuffer();
                    Port.DiscardInBuffer();
                    ReceiveNow.Reset();
                    //Port.Write(command + "\r");
                    Port.WriteLine(command);

                    string input = ReadResponse(Port, responseTimeout);
                    if ((input.Length == 0) || ((!input.EndsWith("\r\n> ")) && (!input.EndsWith("\r\nOK\r\n"))))
                        throw new ApplicationException("No success message was received.");
                    return input;
                }
                catch
                {
                    throw;
                }
            }
        }

        private string ReadResponse(SerialPort port, int timeout)
        {
            string buffer = string.Empty;
            try
            {
                do
                {
                    if (ReceiveNow.WaitOne(timeout, false))
                    {
                        string t = port.ReadExisting();
                        buffer += t;
                    }
                    else
                    {
                        if (buffer.Length > 0)
                            throw new ApplicationException("Response received is incomplete.");
                        else
                            throw new ApplicationException("No data received from phone.");
                    }
                }
                while (!buffer.EndsWith("\r\nOK\r\n") && !buffer.EndsWith("\r\n> ") && !buffer.EndsWith("\r\nERROR\r\n"));
            }
            catch
            {
                throw;
            }
            return buffer;
        }

        /// <summary>
        /// Counts the number of SMS messages waiting on the modem
        /// </summary>
        /// <param name="port">The modem's serial port</param>
        /// <returns>int; the number of messages on the modem</returns>
        public int CountSMSMessages()
        {
            int totalMessages = 0;

            try
            {
                #region Execute Command

                string recievedData = ExecCommand("AT", 300, "No phone connected at ");
                recievedData = ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                String command = "AT+CPMS?";
                recievedData = ExecCommand(command, 1000, "Failed to count SMS message");
                int uReceivedDataLength = recievedData.Length;

                #endregion

                #region If command is executed successfully
                if ((recievedData.Length >= 45) && (recievedData.StartsWith("AT+CPMS?") || recievedData.StartsWith("\r\n+CPMS:")))
                {
                    #region Parsing SMS
                    string[] strSplit = recievedData.Split(',');
                    string strMessageStorageArea1 = strSplit[0];     //SM
                    string strMessageExist1 = strSplit[1];           //Msgs exist in SM
                    #endregion

                    #region Count Total Number of SMS In SIM
                    totalMessages = Convert.ToInt32(strMessageExist1);
                    #endregion
                }
                #endregion

                #region If command is not executed successfully
                else if (recievedData.Contains("ERROR"))
                {
                    #region Error in Counting total number of SMS
                    string recievedError = recievedData;
                    recievedError = recievedError.Trim();
                    recievedData = "Following error occurred while counting the message" + recievedError;
                    #endregion
                }
                #endregion

                return totalMessages;
            }
            catch
            {
                throw;
            }
        }

        private void ExecuteStartupCommands()
        {
            foreach (string command in ModemConfiguration.StartupCommands)
            {
                try
                {
                    string data = ExecCommand(command, 300, "Failed to execute startup command " + command);
                }
                catch
                {
                }
            }
        }

        public bool SendSMS(string phoneNumber, string messageText)
        {
            bool isSend = false;

            try
            {

                string recievedData = ExecCommand("AT", 500, "No phone connected");

                ExecuteStartupCommands();

                recievedData = ExecCommand("AT+CMGF=1", 500, "Failed to set message format.");
                String command = "AT+CMGS=\"" + phoneNumber + "\"";
                recievedData = ExecCommand(command, 500, "Failed to accept phoneNo");
                command = messageText + char.ConvertFromUtf32(26);// +"\r";
                recievedData = ExecCommand(command, 3000, "Failed to send message"); //3 seconds
                if (recievedData.EndsWith("\r\nOK\r\n"))
                {
                    isSend = true;
                }
                else if (recievedData.Contains("ERROR"))
                {
                    isSend = false;
                }
                return isSend;
            }
            catch
            {
                throw;
            }
        }     

        /// <summary>
        /// Reads all SMS messages on the modem
        /// </summary>
        /// <param name="port">The serial port on which the modem resides</param>
        /// <param name="command">The AT read command to execute</param>
        /// <returns>A collection of SMS messages</returns>
        private ShortMessageCollection ReadSMS(SerialPort port, string command)
        {
            // Set up the phone and read the messages
            ShortMessageCollection messages = null;
            try
            {
                #region Execute Command
                // Check connection
                ExecCommand("AT", 300, "No phone connected");
                // Use message format "Text mode"
                ExecCommand("AT+CMGF=1", 300, "Failed to set message format.");
                // Use character set "PCCP437"
                ExecCommand("AT+CSCS=\"PCCP437\"", 300, "Failed to set character set.");
                // Select SIM storage
                ExecCommand("AT+CPMS=\"SM\"", 300, "Failed to select message storage.");
                // Read the messages
                string input = ExecCommand(command, 5000, "Failed to read the messages.");
                #endregion

                #region Parse messages
                messages = ParseMessages(input);
                #endregion

            }
            catch
            {
                throw;
            }

            if (messages != null)
                return messages;
            else
                return null;

        }

        private ShortMessageCollection ParseMessages(string input)
        {
            ShortMessageCollection messages = new ShortMessageCollection();
            try
            {
                Regex r = new Regex(@"\+CMGL: (\d+),""(.+)"",""(.+)"",(.*),""(.+)""\r\n(.+)\r\n");
                Match m = r.Match(input);
                while (m.Success)
                {
                    ShortMessage msg = new ShortMessage();
                    msg.Index = m.Groups[1].Value;
                    msg.Status = m.Groups[2].Value;
                    msg.Sender = m.Groups[3].Value;
                    msg.Alphabet = m.Groups[4].Value;
                    msg.Sent = m.Groups[5].Value;
                    msg.Message = m.Groups[6].Value;
                    messages.Add(msg);

                    m = m.NextMatch();
                }

            }
            catch
            {
                throw;
            }
            return messages;
        }
    }
}
