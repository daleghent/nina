using NINA.Model.MyGuider.MetaGuide;
using NINA.Profile;
using NINA.Utility;
using NINA.Utility.Notification;
using Nito.AsyncEx;
using System;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyGuider {
    class MetaGuideGuider : BaseINPC, IGuider {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(int hWnd, uint Msg, int wParam, int lParam);

        private static readonly int HWND_BROADCAST = 0xffff;
        private static readonly uint remoteLockMsg = RegisterWindowMessage("MG_RemoteLock");
        private static readonly uint remoteUnlockMsg = RegisterWindowMessage("MG_RemoteUnLock");
        private static readonly uint remoteGuideMsg = RegisterWindowMessage("MG_RemoteGuide");
        private static readonly uint remoteUnguideMsg = RegisterWindowMessage("MG_RemoteUnGuide");
        private static readonly uint remoteDitherMsg = RegisterWindowMessage("MG_RemoteDither");
        private static readonly uint remoteDitherRadiusMsg = RegisterWindowMessage("MG_RemoteDitherRadius");

        private static readonly int METAGUIDE_CONNECT_TIMEOUT_MS = 5000;
        private static readonly int METAGUIDE_QUEUE_FLUSH_TIMEOUT_MS = 2000;

        private static readonly Version MINIMUM_MG_VERSION = Version.Parse("5.4.9");

        private readonly object lockobj = new object();
        private readonly AsyncAutoResetEvent metaGuideMessageReceivedEvent = new AsyncAutoResetEvent(false);

        private readonly IProfileService profileService;
        private CancellationTokenSource clientCTS = null;
        private MetaGuideListener listener = null;
        private Task listenerTask = null;
        private bool connecting = false;
        private MetaGuideGuideMsg latestUnpublishedGuide = null;
        private volatile MetaGuideStatusMsg latestStatus = null;

        public MetaGuideGuider(IProfileService profileService) {
            this.profileService = profileService;
        }

        public string Name => "MetaGuide";
        public string Id => "MetaGuide";
        public event EventHandler<IGuideStep> GuideEvent;

        private bool connected;
        public bool Connected {
            get {
                return connected;
            }
            private set {
                if (connected != value) {
                    connected = value;
                    UpdateState();
                    RaisePropertyChanged();
                }
            }
        }

        private double pixelScale;
        public double PixelScale {
            get {
                return pixelScale;
            }
            set {
                if (pixelScale != value) {
                    pixelScale = value;
                    RaisePropertyChanged();
                }
            }
        }

        private string state = "Not Ready";
        public string State {
            get {
                return state;
            }
            set {
                if (state != value) {
                    state = value;
                    RaisePropertyChanged();
                }
            }
        }

        private volatile bool isLocked = false;
        public bool IsLocked {
            get {
                return this.isLocked;
            }
            private set {
                if (this.isLocked != value) {
                    this.isLocked = value;
                    UpdateState();
                    RaisePropertyChanged();
                }
            }
        }

        private volatile bool isGuiding = false;
        public bool IsGuiding {
            get {
                return this.isGuiding;
            }
            private set {
                if (this.isGuiding != value) {
                    this.isGuiding = value;
                    UpdateState();
                    RaisePropertyChanged();
                }
            }
        }

        private double intensity;
        public double Intensity {
            get {
                return intensity;
            }
            set {
                if (intensity != value) {
                    intensity = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double fwhm;
        public double FWHM {
            get {
                return fwhm;
            }
            set {
                if (fwhm != value) {
                    fwhm = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double seeing;
        public double Seeing {
            get {
                return seeing;
            }
            set {
                if (seeing != value) {
                    seeing = value;
                    RaisePropertyChanged();
                }
            }
        }

        private CalibrationState calibrationState;
        public CalibrationState CalibrationState {
            get {
                return calibrationState;
            }
            set {
                if (calibrationState != value) {
                    calibrationState = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double focalLength;
        public double FocalLength {
            get {
                return focalLength;
            }
            set {
                if (focalLength != value) {
                    focalLength = value;
                    RaisePropertyChanged();
                }
            }
        }

        private double pixelSize;
        public double PixelSize {
            get {
                return pixelSize;
            }
            set {
                if (pixelSize != value) {
                    pixelSize = value;
                    RaisePropertyChanged();
                }
            }
        }

        public Task<bool> AutoSelectGuideStar() {
            // Fire and forget
            return Task.FromResult(PostAndCheckMessage("LockOn", remoteLockMsg, 0, 0));
        }

        public async Task<bool> Connect() {
            if (Connected) {
                return true;
            }

            lock (this.lockobj) {
                if (this.connecting) {
                    return false;
                }
                this.connecting = true;
            }

            IPAddress ipAddress = IPAddress.Parse(this.profileService.ActiveProfile.GuiderSettings.MetaGuideIP);
            int port = this.profileService.ActiveProfile.GuiderSettings.MetaGuidePort;
            this.clientCTS = new CancellationTokenSource();
            this.listener = new MetaGuideListener();
            this.listener.OnStatus += Listener_OnStatus;
            this.listener.OnGuide += Listener_OnGuide;
            this.listener.OnDisconnected += Listener_OnDisconnected;

            this.listenerTask = this.listener.RunListener(ipAddress, port, this.clientCTS.Token);
            this.listenerTask.GetAwaiter().OnCompleted(() => this.Disconnect());
            bool connectionSuccess = false;

            try {
                var connectTimeoutTokenSource = new CancellationTokenSource(TimeSpan.FromMilliseconds(METAGUIDE_CONNECT_TIMEOUT_MS));
                connectionSuccess = await WaitOnEventChangeCondition(() => this.latestStatus != null, connectTimeoutTokenSource.Token);
                lock (this.lockobj) {
                    if (!connectionSuccess) {
                        Logger.Error("Failed to connect to MetaGuide. Check to make sure it is running, that broadcast is enabled in Setup -> Extra, and that the broadcast address and port match up with NINA settings.");
                        Notification.ShowError(Locale.Loc.Instance["LblMetaGuideConnectionFailed"]);
                    } else if (this.latestStatus != null && this.latestStatus.MetaGuideVersion < MINIMUM_MG_VERSION) {
                        Logger.Error($"MetaGuide is version {this.latestStatus.MetaGuideVersion} but must be at least {MINIMUM_MG_VERSION}");
                        Notification.ShowError(String.Format(Locale.Loc.Instance["LblMetaGuideVersionCheckFailed"], MINIMUM_MG_VERSION));
                        connectionSuccess = false;
                    }
                    this.Connected = connectionSuccess;
                    this.connecting = false;
                }
                return connectionSuccess;
            } catch (Exception ex) {
                Logger.Error("Failed to connect to MetaGuide. Check to make sure it is running, that broadcast is enabled in Setup -> Extra, and that the broadcast address and port match up with NINA settings.", ex);
                Notification.ShowError(Locale.Loc.Instance["LblMetaGuideConnectionFailed"]);
                return false;
            } finally {
                if (!connectionSuccess) {
                    this.Disconnect();
                }
            }
        }

        private void Listener_OnDisconnected() {
            this.Disconnect();
        }

        public bool Disconnect() {
            var listenerTask = Interlocked.Exchange(ref this.listenerTask, null);
            lock (this.lockobj) {
                this.Connected = false;
                this.connecting = false;
                this.clientCTS?.Cancel();
                this.clientCTS = null;
                this.latestStatus = null;
                this.IsGuiding = false;
                this.IsLocked = false;
                if (this.listener != null) {
                    this.listener.OnGuide -= Listener_OnGuide;
                    this.listener.OnStatus -= Listener_OnStatus;
                    this.listener.OnDisconnected -= Listener_OnDisconnected;
                }
                this.listener = null;
                this.listenerTask = null;
            }

            try {
                listenerTask?.Wait(TimeSpan.FromMilliseconds(METAGUIDE_QUEUE_FLUSH_TIMEOUT_MS));
            } catch (Exception) {
            }
            return true;
        }

        public async Task<bool> Dither(CancellationToken ct) {
            if (!Connected) {
                return false;
            }
            if (!IsGuiding) {
                return false;
            }

            var ditherRadius = this.profileService.ActiveProfile.GuiderSettings.DitherPixels / this.PixelScale;
            if (!PostAndCheckMessage("DitherRadius", remoteDitherRadiusMsg, (int)(Math.Round(ditherRadius * 10.0)), 0)) {
                return false;
            }
            if (!PostAndCheckMessage("Dither", remoteDitherMsg, 0, 0)) {
                return false;
            }

            // Give MG some time to process the dither message
            await Task.Delay(TimeSpan.FromSeconds(this.profileService.ActiveProfile.GuiderSettings.MetaGuideDitherSettleSeconds), ct);
            return !ct.IsCancellationRequested && this.IsGuiding;
        }

        public async Task<bool> StartGuiding(CancellationToken ct) {
            if (!Connected) {
                return false;
            }

            if (!IsLocked) {
                if (!await LockStar(ct)) {
                    return false;
                }
            }

            return await Guide(ct);
        }

        public async Task<bool> StopGuiding(CancellationToken ct) {
            if (!Connected) {
                return false;
            }

            if (!PostAndCheckMessage("GuideOff", remoteUnguideMsg, 0, 0)) {
                return false;
            }
            if (!PostAndCheckMessage("LockOff", remoteUnlockMsg, 0, 0)) {
                return false;
            }
            return await WaitOnEventChangeCondition(() => !this.IsGuiding && !this.IsLocked, ct);
        }

        private static readonly TimeSpan LOW_INTENSITY_THRESHOLD = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan LOW_INTENSITY_GUIDING_TIMEOUT = TimeSpan.FromSeconds(5);
        private static readonly int MAX_LOW_INTENSITY_GUIDING_RETRIES = 3;
        private DateTime? lowIntensityStart;
        private bool guidingHaltedDueToLowIntensity = false;
        private Task lowIntensityChangeGuidingTask = Task.CompletedTask;
        private volatile int lowIntensityGuidingAttemptCount = 0;
        private void Listener_OnStatus(MetaGuideStatusMsg message) {
            MetaGuideGuideMsg guideMsg;
            lock (this.lockobj) {
                if (!this.Connected && !this.connecting) {
                    return;
                }

                guideMsg = Interlocked.Exchange(ref this.latestUnpublishedGuide, null);
                this.IsLocked = message.Locked;
                this.IsGuiding = message.Guiding;
                this.PixelScale = message.ArcSecPerPixel;
                this.Intensity = message.Intensity;
                this.FWHM = message.FWHM;
                this.Seeing = message.Seeing;
                this.CalibrationState = message.CalibrationState;
                this.FocalLength = message.FocalLength;
                this.PixelSize = message.PixelSize;
                this.latestStatus = message;
            }

            // Guide star intensity is on a scale of 0-255, and can drop if clouds appear. This can cause spurious guide pulses,
            // so add a check to temporarily pause and restore guiding when intensity drops below a configurable threshold
            var minIntensity = profileService.ActiveProfile.GuiderSettings.MetaGuideMinIntensity;
            if (message.Intensity < minIntensity) {
                this.OnLowIntensity((int)message.Intensity, minIntensity);
            } else {
                this.OnAdequateIntensity((int)message.Intensity, minIntensity);
            }

            if (message.Guiding) {
                // GuideEvent expects RADuration in pixel units, and MetaGuide updates are in arcsecs
                var step = new MetaGuideGuideStep() {
                    RADistanceRaw = message.DeltaEastArcsec / this.PixelScale,
                    DECDistanceRaw = message.DeltaNorthArcsec / this.PixelScale,
                    RADuration = (double)(guideMsg?.WestPulse).GetValueOrDefault(0) / 1000.0,
                    DECDuration = (double)(guideMsg?.NorthPulse).GetValueOrDefault(0) / 1000.0,
                };
                GuideEvent?.Invoke(this, step);
            }
            this.metaGuideMessageReceivedEvent.Set();
        }

        private void OnLowIntensity(int currentIntensity, int minIntensity) {
            if (this.IsGuiding) {
                if (lowIntensityStart == null) {
                    lowIntensityStart = DateTime.Now;
                }

                var elapsedTime = DateTime.Now - lowIntensityStart;
                if (elapsedTime >= LOW_INTENSITY_THRESHOLD) {
                    // Stop guiding due to low star intensity
                    if (lowIntensityChangeGuidingTask.Status > TaskStatus.Running) {
                        Logger.Warning($"Star intensity {currentIntensity} lower than {minIntensity} for longer than {LOW_INTENSITY_THRESHOLD}. Stopping guiding until intensity comes back.");
                        Notification.ShowWarning(Locale.Loc.Instance["LblMetaGuideLowIntensityStopGuiding"]);
                        lowIntensityChangeGuidingTask = Task.Run(async () => {
                            if (await StopGuiding(new CancellationTokenSource(LOW_INTENSITY_GUIDING_TIMEOUT).Token)) {
                                guidingHaltedDueToLowIntensity = true;
                            } else {
                                Logger.Error("Failed to stop guiding due to low star intensity");
                            }
                        });
                    }
                }
            }
        }

        private void OnAdequateIntensity(int currentIntensity, int minIntensity) {
            lowIntensityStart = null;
            if (guidingHaltedDueToLowIntensity) {
                // Start guiding again due to restored star intensity
                if (lowIntensityChangeGuidingTask.Status > TaskStatus.Running) {
                    if (lowIntensityGuidingAttemptCount == 0) {
                        Logger.Info($"Star intensity {currentIntensity} now above {minIntensity}. Resuming guiding.");
                        Notification.ShowInformation(Locale.Loc.Instance["LblMetaGuideLowIntensityRestoreGuiding"]);
                    }

                    ++lowIntensityGuidingAttemptCount;
                    lowIntensityChangeGuidingTask = Task.Run(async () => {
                        if (await StartGuiding(new CancellationTokenSource(LOW_INTENSITY_GUIDING_TIMEOUT).Token)) {
                            guidingHaltedDueToLowIntensity = false;
                            lowIntensityGuidingAttemptCount = 0;
                        } else if (lowIntensityGuidingAttemptCount >= MAX_LOW_INTENSITY_GUIDING_RETRIES) {
                            Logger.Error("Failed to resume guiding after star intensity recovered");
                            Notification.ShowError(Locale.Loc.Instance["LblMetaGuideLowIntensityRestoreGuidingFailed"]);
                            guidingHaltedDueToLowIntensity = false;
                            lowIntensityGuidingAttemptCount = 0;
                        }
                    });
                }
            }
        }

        private void Listener_OnGuide(MetaGuideGuideMsg message) {
            lock (this.lockobj) {
                if (!this.Connected) {
                    return;
                }

                this.latestUnpublishedGuide = message;
                this.IsGuiding = true;
            }
            this.metaGuideMessageReceivedEvent.Set();
        }

        private async Task<bool> WaitOnEventChangeCondition(Func<bool> condition, CancellationToken ct) {
            var connectionAndCallerTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.clientCTS.Token, ct);
            while (!connectionAndCallerTokenSource.IsCancellationRequested) {
                if (condition.Invoke()) {
                    return true;
                }
                await this.metaGuideMessageReceivedEvent.WaitAsync(connectionAndCallerTokenSource.Token);
            }
            return false;
        }

        private static bool PostAndCheckMessage(string messageType, uint msg, int wParam, int lParam) {
            if (!PostMessage(HWND_BROADCAST, msg, wParam, lParam)) {
                Logger.Error($"Failed to post {messageType} message");
                Notification.ShowError($"Failed to post {messageType} message");
                return false;
            }
            return true;
        }

        private void UpdateState() {
            lock (this.lockobj) {
                if (!Connected) {
                    this.State = "Not Ready";
                } else if (this.IsGuiding && this.IsLocked) {
                    this.State = "Locked and Guiding";
                } else if (this.IsGuiding) {
                    this.State = "Guiding";
                } else if (this.IsLocked) {
                    this.State = "Locked";
                } else {
                    this.State = "Connected";
                }
            }
        }

        private async Task<bool> LockStar(CancellationToken ct) {
            if (!PostAndCheckMessage("LockOn", remoteLockMsg, 0, 0)) {
                return false;
            }
            return await WaitOnEventChangeCondition(() => this.IsLocked, ct);
        }

        private async Task<bool> Guide(CancellationToken ct) {
            if (!PostAndCheckMessage("GuideOn", remoteGuideMsg, 0, 0)) {
                return false;
            }
            return await WaitOnEventChangeCondition(() => this.IsGuiding, ct);
        }
    }

    public class MetaGuideGuideStep : IGuideStep {
        public double Frame { get; set; }
        public double Time { get; set; }
        public double RADistanceRaw { get; set; }
        public double DECDistanceRaw { get; set; }
        public double RADuration { get; set; }
        public double DECDuration { get; set; }
        public string Event { get; set; }
        public string TimeStamp { get; set; }
        public string Host { get; set; }
        public int Inst { get; set; }

        public IGuideStep Clone() {
            return (MetaGuideGuideStep)this.MemberwiseClone();
        }
    }
}
