using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.TcpRaw {

    internal class BasicQuery {
        public string Address { get; set; }

        public int Port { get; set; }

        public string Command { get; set; }

        public BasicQuery(string address, int port, string command) {
            Address = address;
            Port = port;
            Command = command;
        }

        public async Task<string> SendQuery() {
            using (var client = new TcpClient()) {
                try {
                    await client.ConnectAsync(Address, Port);
                } catch (Exception ex) {
                    Logger.Error($"TcpRaw: Error connecting to {Address}:{Port}: {ex}");
                    throw;
                }

                Logger.Trace($"TcpRaw: Sending command: {ToLiteral(Command)}");
                var data = Encoding.ASCII.GetBytes($"{Command}");
                var stream = client.GetStream();
                stream.Write(data, 0, data.Length);

                var buffer = new byte[2048];
                var length = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.ASCII.GetString(buffer, 0, length);

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