using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.TcpRaw {

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

        public async Task<string> SendQuery() {
            using (var client = new TcpClient()) {
                try {
                    Logger.Trace($"TcpRaw: Connecting to {Address}:{Port}");
                    await client.ConnectAsync(Address, Port);
                } catch (Exception ex) {
                    Logger.Error($"TcpRaw: Error connecting to {Address}:{Port}: {ex}");
                    throw;
                }

                var stream = client.GetStream();
                var buffer = new byte[2048];
                int length;
                string response = string.Empty;

                if (!string.IsNullOrEmpty(WaitFor) && stream.CanRead) {
                    bool waitDone = false;

                    while (!waitDone) {
                        length = stream.Read(buffer, 0, buffer.Length);
                        response = Encoding.ASCII.GetString(buffer, 0, length);
                        Logger.Trace($"TcpRaw: Received message: {ToLiteral(response)}");

                        if (response.Equals(WaitFor)) { waitDone = true; }
                    }
                }

                // Send command
                Logger.Trace($"TcpRaw: Sending command: {ToLiteral(Command)}");
                var data = Encoding.ASCII.GetBytes($"{Command}");
                stream.Write(data, 0, data.Length);

                // Read response
                length = stream.Read(buffer, 0, buffer.Length);
                response = Encoding.ASCII.GetString(buffer, 0, length);

                stream.Close();
                client.Close();

                Logger.Trace($"TcpRaw: Received message: {ToLiteral(response)}");

                return response;
            }
        }

        private static string ToLiteral(string str) {
            str = str.Replace("\r", @"\r").Replace("\n", @"\n").Replace("\t", @"\t");

            return str;
        }
    }
}