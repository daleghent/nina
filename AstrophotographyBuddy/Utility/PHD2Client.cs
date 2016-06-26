using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AstrophotographyBuddy.Utility {
    class PHD2Client : BaseINPC {
        public PHD2Client() {

        }

        private PhdEventVersion _version;
        public PhdEventVersion Version {
            get {
                return _version;
            }
            set {
                _version = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventAppState _appState;
        public PhdEventAppState AppState {
            get {
                return _appState;
            }
            set {
                _appState = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventSettling _settling;
        public PhdEventSettling Settling {
            get {
                return _settling;
            }
            set {
                _settling = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventSettleDone _settleDone;
        public PhdEventSettleDone SettleDone {
            get {
                return _settleDone;
            }
            set {
                _settleDone = value;
                RaisePropertyChanged();
            }
        }

        private PhdEventGuidingDithered _guidingDithered;
        public PhdEventGuidingDithered GuidingDithered {
            get {
                return _guidingDithered;
            }
            set {
                _guidingDithered = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<PhdEventGuideStep> _guideSteps;
        public ObservableCollection<PhdEventGuideStep> GuideSteps {
            get {
                if (_guideSteps == null) {
                    _guideSteps = new ObservableCollection<PhdEventGuideStep>();
                }
                return _guideSteps;
            }
            set {
                _guideSteps = value;
                RaisePropertyChanged();
            }
        }

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _tokenSource;

        public bool Connected {
            get {
                if (_client == null) {
                    return false;
                }
                return _client.Connected;
            }
        }

        private bool _isDithering;
        public bool IsDithering {
            get {
                return _isDithering;
            }
            set {
                _isDithering = value;
                RaisePropertyChanged();
            }
        }

        public async Task<bool> connect() {
            
            try {
                
                _client = new TcpClient();
                await _client.ConnectAsync(Settings.PHD2ServerUrl, Settings.PHD2ServerPort);
                _stream = _client.GetStream();
                RaisePropertyChanged("Connected");
                _tokenSource = new CancellationTokenSource();
                startListener(_tokenSource.Token);
                    


            }
            catch (SocketException e) {
                System.Windows.MessageBox.Show(e.Message);
            }
            return Connected;
        }

        public async Task<bool> dither() {
            if(Connected) {
                IsDithering = true;
                await sendMessage(String.Format(PHD2Methods.DITHER, Settings.DitherPixels.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), Settings.DitherRAOnly.ToString().ToLower()));
            }

            return IsDithering;
        }

        private async Task<bool> sendMessage(string msg) {
            if(Connected) {
                // Translate the passed message into ASCII and store it as a byte array.
                Byte[] data = new Byte[1024];
                data = System.Text.Encoding.ASCII.GetBytes(msg);

                // Get a client stream for reading and writing.
                // Stream stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                await _stream.WriteAsync(data, 0, data.Length);
                
            }
            return true;            
        }

        public bool disconnect() {
            
            if (Connected) {
                _tokenSource.Cancel();
                _stream.Close();
                _client.Close();
                IsDithering = false;            
                RaisePropertyChanged("Connected");
            }
            return !Connected;
        }
        
        private async void startListener(CancellationToken token) {
            
            while(Connected) {
                try {
                    if (_stream.DataAvailable) {
                        token.ThrowIfCancellationRequested();
                        byte[] resp = new byte[2048];
                        var memStream = new MemoryStream();
                        var bytes = 0;
                        bytes = await _stream.ReadAsync(resp, 0, resp.Length);
                        await memStream.WriteAsync(resp, 0, bytes);
                        List<string> rows = new List<string>();
                        memStream.Position = 0;
                        using (var reader = new StreamReader(memStream, Encoding.ASCII)) {
                            string line;
                            while ((line = reader.ReadLine()) != null) {
                                rows.Add(line);
                            }
                        }
                        
                        foreach(string row in rows) {
                            JObject o = JObject.Parse(row);
                            JToken t = o.GetValue("Event");
                            string phdevent = "";
                            if (t != null) {
                                phdevent = t.ToString();
                            } else {
                                t = o.GetValue("id");
                                if(t!= null) {
                                    phdevent = t.ToString();
                                }
                            }

                            switch (phdevent) {
                                case PHD2Methods.DITHERID: {
                                        PhdMethodResponse phdresp = o.ToObject<PhdMethodResponse>();
                                        if (phdresp.error != null) {
                                                IsDithering = false;
                                            }
                                        
                                        
                                        break;
                                    }
                                case "Version": {
                                        Version = o.ToObject<PhdEventVersion>();                                        
                                        break;
                                    }
                                case "AppState": {
                                        AppState = o.ToObject<PhdEventAppState>();
                                        break;
                                    }
                                case "GuideStep": {
                                        if (GuideSteps.Count > 100) {
                                            GuideSteps.RemoveAt(0);
                                        }
                                        GuideSteps.Add(o.ToObject<PhdEventGuideStep>());
                                        break;
                                    }
                                case "GuidingDithered": {
                                        SettleDone = null;
                                        GuidingDithered = o.ToObject<PhdEventGuidingDithered>();
                                        break;
                                    }
                                case "Settling": {
                                        SettleDone = null;
                                        Settling = o.ToObject<PhdEventSettling>();
                                        break;
                                    }
                                case "SettleDone": {
                                        GuidingDithered = null;
                                        Settling = null;
                                        IsDithering = false;
                                        SettleDone = o.ToObject<PhdEventSettleDone>();
                                        if(SettleDone.Error != null) {
                                            System.Windows.MessageBox.Show(SettleDone.Error, "PHD2 Dither Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                                        }
                                        break;
                                    }
                                default: {
                                        break;
                                    }
                            }
                        }
                        
                        
                    }
                    else {
                        await Task.Delay(1000);
                    }
                    
                }catch (Exception ex) {
                    Logger.error(ex.Message);
                }
                
            }
        }
        
    }

    public class PhdMethodResponse {
        public string jsonrpc;
        public string result;
        public PhdError error;
        public int id;
    }

    public class PhdError {
        public int code;
        public string message;
    }

    public class PhdEvent {
        public string Event;
        public string TimeStamp;
        public string Host;
        public int Inst;
    }

    public class PhdEventVersion : PhdEvent {
        public string PHDVersion;
        public string PHDSubver;
        public int MsgVersion;
    }

    public class PhdEventLockPositionSet : PhdEvent {
        public int X;
        public int Y;
    }

    public class PhdEventCalibrationComplete : PhdEvent {
        public string Mount;
    }

    public class PhdEventStarSelected : PhdEvent {
        public int X;
        public int Y;
    }

    public class PhdEventStartGuiding : PhdEvent {

    }

    public class PhdEventPaused : PhdEvent {

    }

    public class PhdEventStartCalibration : PhdEvent {
        public string Mount;
    }

    public class PhdEventAppState : PhdEvent {
        public string State;
    }

    public class PhdEventCalibrationFailed : PhdEvent {
        public string Reason;
    }

    public class PhdEventCalibrationDataFlipped : PhdEvent {
        public string Mount;
    }

    public class PhdEventLoopingExposures : PhdEvent {
        public int Frame;
    }

    public class PhdEventLoopingExposuresStopped : PhdEvent {

    }

    public class PhdEventSettling : PhdEvent {
        public int Distance;
        public int Time;
        public int SettleTime;

    }

    public class PhdEventSettleDone : PhdEvent {
        public int Status;
        public string Error;
    }

    public class PhdEventStarLost : PhdEvent {
        public int Frame;
        public int Time;
        public int StarMass;
        public int SNR;
        public int AvgDist;
        public int ErrorCode;
        public int Status;
    }

    public class PhdEventGuidingStopped : PhdEvent {

    }

    public class PhdEventResumed : PhdEvent {

    }

    public class PhdEventGuideStep : PhdEvent {
        public int Frame;
        public int Time;
        public string Mount;
        public int dx;
        public int dy;
        public int RADistanceRaw;
        public int DecDistanceRaw;
        public int RADistanceGuide;
        public int DecDistanceGuide;
        public int RADuration;
        public string RADirection;
        public int DECDuration;
        public string DecDirection;
        public int StarMass;
        public int SNR;
        public int AvgDist;
        public bool RALimited;
        public bool DecLimited;
        public int ErrorCode;
    }

    public class PhdEventGuidingDithered : PhdEvent {
        public int dx;
        public int dy;
    }

    public class PhdEventLockPositionLost : PhdEvent {

    }

    public class PhdEventAlert : PhdEvent {
        public string Msg;
        public string Type;
    }



}
