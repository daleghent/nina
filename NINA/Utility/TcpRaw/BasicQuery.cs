using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.TcpRaw {

    internal class BasicQuery {
        public string Address { get; set; } = "localhost";

        public int Port { get; set; } = 0;

        public string Command { get; set; } = string.Empty;

        public BasicQuery(string address, int port, string command) {
            Address = address;
            Port = port;
            Command = command;
        }

        public async Task<string> SendQuery() {
            using var client = new TcpClient();
            try {
                await client.ConnectAsync(Address, Port);
            } catch (Exception ex) {
                Logger.Error($"TcpRaw: Error connecting to {Address}:{Port}: {ex}");
                throw ex;
            }

            Logger.Trace($"TcpRaw: Sending command: {ToLiteral(Command)}");
            byte[] data = Encoding.ASCII.GetBytes($"{Command}");
            var stream = client.GetStream();
            stream.Write(data, 0, data.Length);

            byte[] buffer = new byte[2048];
            var length = stream.Read(buffer, 0, buffer.Length);
            string response = Encoding.ASCII.GetString(buffer, 0, length);

            stream.Close();
            client.Close();

            Logger.Trace($"TcpRaw: Received message: {ToLiteral(response)}");

            return response;
        }

        private string ToLiteral(string str) {
            str = str.Replace("\r", @"\r").Replace("\n", @"\n").Replace("\t", @"\t");

            return str;
        }
    }
}