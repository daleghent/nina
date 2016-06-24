using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _tokenSource;
        private Task listener;

        public bool Connected {
            get {
                if(_client==null) {
                    return false;
                }
                return _client.Connected;
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

        public bool disconnect() {
            
            if (Connected) {
                _tokenSource.Cancel();
                _stream.Close();
                _client.Close();                    
                RaisePropertyChanged("Connected");
            }
            return !Connected;
        }
        
        private async void startListener(CancellationToken token) {
            StreamReader sr = new StreamReader(_stream);
            string buffer;
            while(Connected) {
                try {
                    token.ThrowIfCancellationRequested();
                    
                    if (_stream.DataAvailable) {
                        token.ThrowIfCancellationRequested();

                        buffer = sr.ReadLine();

                        
                        if (buffer.Length > 0) {
                            JObject o = JObject.Parse(buffer);
                            JToken t = o.GetValue("Event");
                            string phdevent = "";
                            if (t != null) {
                                phdevent = t.ToString();
                            }
                            
                            switch(phdevent) {
                                case "Version": {
                                        PhdEventVersion version = o.ToObject<PhdEventVersion>();
                                        break;
                                    }
                                case "AppState": {
                                        PhdEventAppState appstate = o.ToObject<PhdEventAppState>();
                                        break;
                                    }
                                default: {
                                        break;
                                    }
                            }
                            
                            //JObject o = JObject.Parse(buffer);
                            //Process PHD2 Answers

                        }
                    }
                    else {
                        await Task.Delay(1000);
                    }
                    
                }catch (Exception ex) {

                }
                
            }
        }
        
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
        int Frame;
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
