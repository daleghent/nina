﻿#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Core.Utility.TcpRaw {

    public class BasicQuery {
        public string Address { get; set; }

        public int Port { get; set; }

        public string Command { get; set; }

        public string WaitFor { get; set; }

        public BasicQuery(string address, int port, string command, string waitFor = null) {
            Address = address;
            Port = port;
            Command = command;
            WaitFor = waitFor;
        }
        public Task<string> SendQuery() {
            return SendQuery(CancellationToken.None);
        }

        public async Task<string> SendQuery(CancellationToken token) {
            using (var client = new TcpClient()) {
                try {
                    Logger.Trace($"TcpRaw: Connecting to {Address}:{Port}");
                    await client.ConnectAsync(Address, Port, token);
                } catch(OperationCanceledException) {
                    throw;
                } catch (Exception ex) {
                    Logger.Error($"TcpRaw: Error connecting to {Address}:{Port}: {ex}");
                    throw;
                }

                var stream = client.GetStream();
                try {

                    var buffer = new byte[2048];
                    int length;
                    string response = string.Empty;

                    if (!string.IsNullOrEmpty(WaitFor) && stream.CanRead) {
                        bool waitDone = false;

                        while (!waitDone) {
                            length = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                            response = Encoding.ASCII.GetString(buffer, 0, length);
                            Logger.Trace($"TcpRaw: Received message: {ToLiteral(response)}");

                            if (response.Equals(WaitFor)) { waitDone = true; }
                        }
                    }

                    // Send command
                    Logger.Trace($"TcpRaw: Sending command: {ToLiteral(Command)}");
                    var data = Encoding.ASCII.GetBytes($"{Command}");
                    await stream.WriteAsync(data, 0, data.Length, token);

                    // Read response
                    length = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    response = Encoding.ASCII.GetString(buffer, 0, length);


                    Logger.Trace($"TcpRaw: Received message: {ToLiteral(response)}");

                    return response;
                } finally {
                    stream.Close();
                    client.Close();
                }
            }
        }

        private static string ToLiteral(string str) {
            str = str.Replace("\r", @"\r").Replace("\n", @"\n").Replace("\t", @"\t");

            return str;
        }
    }
}