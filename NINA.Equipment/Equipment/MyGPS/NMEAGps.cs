#region "copyright"

/*
    Copyright © 2016 - 2023 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Locale;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Equipment.Equipment.MyGPS {

    /// <summary>
    /// NMEA GPS Class detects comport based NMEA GPS Devices
    /// Flow : construct -> Autodiscover [detect, Connect, listens to messages]
    /// </summary>
    public class NMEAGps : BaseINPC, IDevice, IDisposable {
        private readonly IProfileService profileService;
        private readonly int gpsId;
        private bool connected;
        private string portName;
        private int baudRate = 0;
        private System.Timers.Timer fixTimer;
        private NmeaParser.SerialPortDevice currentDevice;
        private const int sentenceWait = 4;
        private TaskCompletionSource<bool> gotGPSFix;
        public double[] Coords = new double[3];

        public NMEAGps(int gpsId, IProfileService profileService) {
            this.profileService = profileService;
            this.gpsId = gpsId;
            connected = false;
        }

        public string Category => "NMEA";

        public string Id => $"#{gpsId} (#{portName})";

        public string Name => "NMEA GPS Device";

        public string Description => string.Empty;

        public string DriverInfo => "NMEA GPS";

        public string DriverVersion => "0.1";

        public bool HasSetupDialog => false;

        public void SetupDialog() {
            //TODO : allow the user to select a specific com port maybe?
        }

        public void Initialize() {
            if (connected) Disconnect();
            connected = false;
            baudRate = 0;
            portName = "";
        }

        private void OnFixTimedEvent(Object source, System.Timers.ElapsedEventArgs e) {
            // give up with a connected GPS that offers no GGA senstences
            Logger.Error($"Did not observe a GGA sentence within {sentenceWait} seconds");
            Notification.ShowWarning(Loc.Instance["LblGPSNoGGA"]);

            Disconnect();
            gotGPSFix.TrySetResult(false);
        }

        public bool Connected {
            get => connected;
            set {
                connected = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        ///Checks GPS messages, transfers location to the options view
        ///if a fix is obtained
        /// </summary>
        private void Device_MessageReceived(object sender, NmeaParser.NmeaMessageReceivedEventArgs args) {
            var message = args.Message;

            if (args.Message is NmeaParser.Messages.Gga) {
                if (((NmeaParser.Messages.Gga)message).Quality != NmeaParser.Messages.Gga.FixQuality.Invalid &&
                    ((NmeaParser.Messages.Gga)message).Quality != NmeaParser.Messages.Gga.FixQuality.Simulation &&
                    ((NmeaParser.Messages.Gga)message).Quality != NmeaParser.Messages.Gga.FixQuality.ManualInput) {
                    Coords[0] = ((NmeaParser.Messages.Gga)message).Longitude;
                    Coords[1] = ((NmeaParser.Messages.Gga)message).Latitude;
                    Coords[2] = ((NmeaParser.Messages.Gga)message).Altitude;
                } else {
                    Logger.Error($"GPSGGA sentence indicates no fix. Quality: {((NmeaParser.Messages.Gga)message).Quality}");
                    Notification.ShowError(Loc.Instance["LblGPSNoFix"]);

                    Disconnect();
                    gotGPSFix.TrySetResult(false);

                    return;
                }
            } else return;

            Logger.Info($"GPSGGA sentence received. Lat: {Coords[1]}, Long: {Coords[0]}, Altitude: {Coords[2]}, " +
                        $"Quality: {((NmeaParser.Messages.Gga)message).Quality}, HDOP: {((NmeaParser.Messages.Gga)message).Hdop}");

            Notification.ShowSuccess(Loc.Instance["LblGPSLocationSet"]);

            Disconnect();
            gotGPSFix.TrySetResult(true);
        }

        public async Task<bool> Connect(CancellationToken token) {
            if ((currentDevice != null) || (baudRate == 0)) return false; // disconnect first

            try {
                var device = new NmeaParser.SerialPortDevice(new System.IO.Ports.SerialPort(portName, baudRate));
                currentDevice = device;
                device.MessageReceived += Device_MessageReceived;
                fixTimer = new System.Timers.Timer(TimeSpan.FromSeconds(sentenceWait));
                fixTimer.Elapsed += OnFixTimedEvent;
                fixTimer.AutoReset = false;
                fixTimer.Enabled = true;
                gotGPSFix = new TaskCompletionSource<bool>();
                connected = true;
                await device.OpenAsync();

                Logger.Info($"GPS device found on {portName}");
                return await gotGPSFix.Task;
            } catch (Exception ex) {
                Logger.Error(ex.Message);
                Notification.ShowError(Loc.Instance["LblGPSConnectFail"] + " " + portName);
                return false;
            }
        }

        public void Disconnect() {
            if (currentDevice.IsOpen) currentDevice.CloseAsync();

            try {
                currentDevice.MessageReceived -= Device_MessageReceived; // unsubscribe to avoid multiple messages
                fixTimer.Enabled = false;
                fixTimer.Dispose();
                currentDevice.Dispose();
            } catch (Exception ex) {
                Logger.Error(ex.Message);
                throw;
            }

            Connected = false;
        }

        private static string[] GetComPorts() {
            var ports = System.IO.Ports.SerialPort.GetPortNames().OrderBy(s => s);
            string[] ret = new string[ports.Count()];
            int i = 0;
            foreach (var cportName in ports)
                ret[i++] = cportName;
            return ret;
        }

        /// <summary>
        /// this finds a suitable comport, connects and listens for CPS messages
        /// </summary>
        private static System.IO.Ports.SerialPort FindPort() {
            string[] allPorts = GetComPorts();
            int[,] portRates = new int[allPorts.Length, 7];
            // set port / baud test precedence
            for (int pnum = 0; pnum < allPorts.Length; pnum++) {
                List<int> baudRatesToTest = new List<int>(new[]
                   { 9600, 4800, 115200, 19200, 57600, 38400, 2400 });
                string cportName = allPorts[pnum];
                using (var port = new System.IO.Ports.SerialPort(cportName)) {
                    var defaultRate = port.BaudRate;
                    if (baudRatesToTest.Contains(defaultRate)) baudRatesToTest.Remove(defaultRate);
                    baudRatesToTest.Insert(0, defaultRate);
                    for (int bnum = 0; bnum < baudRatesToTest.Count; bnum++)
                        portRates[pnum, bnum] = baudRatesToTest[bnum];
                }
            }
            // use computed precedences to test the ports
            for (int bnum = 0; bnum < 7; bnum++)
                for (var pnum = 0; pnum < allPorts.Length; pnum++) {
                    string cportName = allPorts[pnum];
                    int baud = portRates[pnum, bnum];
                    using (var port = new System.IO.Ports.SerialPort(cportName)) {
                        port.BaudRate = baud;
                        port.ReadTimeout = 2000;
                        bool success = false;
                        try {
                            port.Open();
                            if (!port.IsOpen)
                                continue; //couldn't open port
                            try { // ths is blocking
                                port.ReadTo("$GP");
                            } catch (TimeoutException) {
                                continue;
                            }
                            success = true;
                        } catch (Exception ex) {
                            //Error reading
                            Logger.Error(ex);
                        }
                        if (success) {
                            return new System.IO.Ports.SerialPort(cportName, baud);
                        }
                    }
                }
            return null;
        }

        public void Dispose() {
            if (connected) Disconnect();
        }

        /// <summary>
        /// discovers the first GPS device connected to a serial port
        /// </summary>
        public bool AutoDiscover() {
            using (System.IO.Ports.SerialPort port = FindPort()) {
                if (port != null) { //we found a port with a GPS
                    portName = port.PortName;
                    baudRate = port.BaudRate;
                    return true;
                } else { // no GPS found
                    portName = "";
                    baudRate = 0;
                    Notification.ShowError(Loc.Instance["LblGPSNotFound"]);
                    return false;
                }
            }
        }

        public IList<string> SupportedActions => new List<string>();

        public string Action(string actionName, string actionParameters) {
            throw new NotImplementedException();
        }

        public string SendCommandString(string command, bool raw) {
            throw new NotImplementedException();
        }

        public bool SendCommandBool(string command, bool raw) {
            throw new NotImplementedException();
        }

        public void SendCommandBlind(string command, bool raw) {
            throw new NotImplementedException();
        }
    }
}