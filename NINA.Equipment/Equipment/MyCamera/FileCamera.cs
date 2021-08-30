#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Profile.Interfaces;
using NINA.Core.Utility;
using NINA.Core.Utility.WindowService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Model.Equipment;
using NINA.Core.Locale;
using NINA.Image.Interfaces;
using NINA.Equipment.Model;
using NINA.Image.ImageData;
using NINA.Equipment.Interfaces;

namespace NINA.Equipment.Equipment.MyCamera {

    public class FileCamera : BaseINPC, ICamera {

        public FileCamera(IProfileService profileService, ITelescopeMediator telescopeMediator) {
            OpenFolderDiagCommand = new RelayCommand(OpenFolderDiag);
            this.profileService = profileService;
            this.telescopeMediator = telescopeMediator;
            CameraState = "Idle";
            SelectedFileExtension = FileExtensions.FirstOrDefault(x => x.Name == profileService.ActiveProfile.CameraSettings.FileCameraExtension) ?? FileExtensions.First();
        }

        private void OpenFolderDiag(object obj) {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog()) {
                dialog.SelectedPath = FolderPath;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) {
                    FolderPath = dialog.SelectedPath;
                }
            }
        }

        public ICommand OpenFolderDiagCommand { get; }
        public FileCameraFolderWatcher folderWatcher;

        public string FolderPath {
            get => profileService.ActiveProfile.CameraSettings.FileCameraFolder;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraFolder = value;
                RaisePropertyChanged();
            }
        }

        public string Category { get; } = "N.I.N.A.";

        public bool HasShutter {
            get {
                return false;
            }
        }

        public bool Connected { get; private set; }

        public double CCDTemperature {
            get {
                return double.NaN;
            }
        }

        public double SetCCDTemperature {
            get {
                return double.NaN;
            }

            set {
            }
        }

        public short BinX {
            get {
                return -1;
            }

            set {
            }
        }

        public short BinY {
            get {
                return -1;
            }

            set {
            }
        }

        public string Description {
            get {
                return Loc.Instance["LblFileCameraDescription"];
            }
        }

        public string DriverInfo {
            get {
                return string.Empty;
            }
        }

        public string DriverVersion {
            get {
                return CoreUtil.Version;
            }
        }

        public string SensorName {
            get {
                return "";
            }
        }

        public SensorType SensorType {
            get {
                return SensorType.Monochrome;
            }
        }

        public short BayerOffsetX => 0;

        public short BayerOffsetY => 0;

        public int CameraXSize {
            get {
                return -1;
            }
        }

        public int CameraYSize {
            get {
                return -1;
            }
        }

        public double ExposureMin {
            get {
                return 0;
            }
        }

        public double ExposureMax {
            get {
                return double.MaxValue;
            }
        }

        public double ElectronsPerADU => double.NaN;

        public short MaxBinX {
            get {
                return 1;
            }
        }

        public short MaxBinY {
            get {
                return 1;
            }
        }

        public double PixelSizeX {
            get {
                return profileService.ActiveProfile.CameraSettings.PixelSize;
            }
        }

        public double PixelSizeY {
            get {
                return profileService.ActiveProfile.CameraSettings.PixelSize;
            }
        }

        public bool CanSetCCDTemperature {
            get {
                return false;
            }
        }

        public bool CoolerOn {
            get {
                return false;
            }

            set {
            }
        }

        public double CoolerPower {
            get {
                return double.NaN;
            }
        }

        private string cameraState;

        public string CameraState {
            get {
                return cameraState;
            }
            set {
                cameraState = value;
                RaisePropertyChanged();
            }
        }

        public int Offset {
            get {
                return -1;
            }

            set {
            }
        }

        public int USBLimit {
            get {
                return -1;
            }

            set {
            }
        }

        public int USBLimitMax => -1;
        public int USBLimitMin => -1;
        public int USBLimitStep => -1;

        public bool CanSetOffset {
            get {
                return false;
            }
        }

        public int OffsetMin {
            get {
                return 0;
            }
        }

        public int OffsetMax {
            get {
                return 0;
            }
        }

        public bool CanSetUSBLimit {
            get {
                return false;
            }
        }

        public bool CanGetGain {
            get {
                return false;
            }
        }

        public bool CanSetGain {
            get {
                return false;
            }
        }

        public int GainMax {
            get {
                return -1;
            }
        }

        public int GainMin {
            get {
                return -1;
            }
        }

        public int Gain {
            get {
                return -1;
            }

            set {
            }
        }

        public IList<int> Gains {
            get {
                return new List<int>();
            }
        }

        private AsyncObservableCollection<BinningMode> binningModes;

        public AsyncObservableCollection<BinningMode> BinningModes {
            get {
                if (binningModes == null) {
                    binningModes = new AsyncObservableCollection<BinningMode> {
                        new BinningMode(1,1)
                    };
                }
                return binningModes;
            }
        }

        public bool HasSetupDialog {
            get {
                return true;
            }
        }

        public string Id {
            get {
                return "209D6981-1E09-438C-A1B6-7452F5C34A59";
            }
        }

        public string Name {
            get {
                return "N.I.N.A. File Camera";
            }
        }

        public double Temperature {
            get {
                return double.NaN;
            }
        }

        public double TemperatureSetPoint {
            get {
                return double.NaN;
            }

            set {
                throw new NotImplementedException();
            }
        }

        public bool CanSetTemperature {
            get {
                return false;
            }
        }

        public bool CanSubSample {
            get {
                return false;
            }
        }

        public bool EnableSubSample {
            get {
                return false;
            }

            set {
            }
        }

        public int SubSampleX { get; set; }

        public int SubSampleY { get; set; }

        public int SubSampleWidth { get; set; }

        public int SubSampleHeight { get; set; }

        public bool CanShowLiveView {
            get {
                return false;
            }
        }

        public bool LiveViewEnabled {
            get {
                return false;
            }
            set {
            }
        }

        public bool HasDewHeater {
            get {
                return false;
            }
        }

        public bool DewHeaterOn {
            get {
                return false;
            }

            set {
            }
        }

        public bool HasBattery {
            get {
                return false;
            }
        }

        public int BatteryLevel {
            get {
                return -1;
            }
        }

        public int BitDepth {
            get {
                return (int)profileService.ActiveProfile.CameraSettings.BitDepth;
            }
        }

        public IList<string> ReadoutModes => new List<string> { "Default" };

        public short ReadoutMode {
            get => 0;
            set { }
        }

        public short ReadoutModeForSnapImages {
            get {
                return 0;
            }

            set {
            }
        }

        public short ReadoutModeForNormalImages {
            get {
                return 0;
            }

            set {
            }
        }

        public void AbortExposure() {
            folderWatcher.Suspend();
        }

        public Task<bool> Connect(CancellationToken token) {
            folderWatcher = new FileCameraFolderWatcher(FolderPath, SelectedFileExtension);
            if (AlwaysListen) {
                folderWatcher.Start();
            }
            Connected = true;
            return Task.FromResult(true);
        }

        public FileExtension selectedFileExtension;

        public FileExtension SelectedFileExtension {
            get => selectedFileExtension;
            set {
                selectedFileExtension = value;
                profileService.ActiveProfile.CameraSettings.FileCameraExtension = selectedFileExtension.Name;
                RaisePropertyChanged();
            }
        }

        public ICollection<FileExtension> FileExtensions { get; } = new List<FileExtension>() {
            new FileExtension ("ALL", @"\.tiff|\.tif|\.png|\.gif|\.jpg|\.jpeg|\.png|\.cr2|\.cr3|\.nef|\.raw|\.raf|\.xisf|\.fit|\.fits|\.pef|\.dng|\.arw|\.orf"),
            new FileExtension ("Canon", @"\.cr2|\.cr3"),
            new FileExtension ("NEF", @"\.nef"),
            new FileExtension ("RAW", @"\.raw"),
            new FileExtension ("RAF", @"\.raf"),
            new FileExtension ("PEF", @"\.pef"),
            new FileExtension ("DNG", @"\.dng"),
            new FileExtension ("ARW", @"\.arw"),
            new FileExtension ("ORF", @"\.orf"),
            new FileExtension ("TIFF", @"\.tiff|\.tif"),
            new FileExtension ("PNG", @"\.png"),
            new FileExtension ("JPG", @"\.jpg|\.jpeg"),
            new FileExtension ("GIF", @"\.gif"),
            new FileExtension ("XISF", @"\.xisf"),
            new FileExtension ("FITS", @"\.fit|\.fits"),
        };

        public void Disconnect() {
            if (AlwaysListen) {
                folderWatcher.Suspend();
            }
            folderWatcher?.Dispose();
            serialPortInteraction?.Dispose();
            serialRelayInteraction?.Dispose();
            serialPortInteraction = null;
            serialRelayInteraction = null;
            Connected = false;
        }

        public async Task WaitUntilExposureIsReady(CancellationToken token) {
            using (token.Register(() => AbortExposure())) {
                var remaining = exposureTime - (DateTime.Now - exposureStart);
                await Task.Delay(remaining, token);
            }
        }

        public async Task<IExposureData> DownloadExposure(CancellationToken token) {
            try {
                string path;
                while ((path = folderWatcher.GetNextItem()) == null) {
                    CameraState = "Waiting for file";
                    await CoreUtil.Wait(TimeSpan.FromSeconds(1), token);
                }

                folderWatcher.Suspend();

                CameraState = "Loading from file";
                var tries = 0;
                while (true) {
                    tries++;
                    try {
                        var image = await BaseImageData.FromFile(path, BitDepth, IsBayered, profileService.ActiveProfile.CameraSettings.RawConverter, token);
                        return new CachedExposureData(image);
                    } catch (Exception ex) {
                        if (tries > 3) {
                            throw;
                        }
                        await CoreUtil.Wait(TimeSpan.FromSeconds(1), token);
                    }
                }
            } finally {
                CameraState = "Idle";
            }
        }

        private IProfileService profileService;
        private ITelescopeMediator telescopeMediator;

        public void SetBinning(short x, short y) {
        }

        private IWindowService windowService;

        public IWindowService WindowService {
            get {
                if (windowService == null) {
                    windowService = new WindowService();
                }
                return windowService;
            }
            set {
                windowService = value;
            }
        }

        public void SetupDialog() {
            var task = WindowService.ShowDialog(this, "File Camera Setup", System.Windows.ResizeMode.NoResize, System.Windows.WindowStyle.SingleBorderWindow);
            task.Task.ContinueWith(t => {
                if (Connected) {
                    folderWatcher?.Dispose();
                    folderWatcher = new FileCameraFolderWatcher(FolderPath, SelectedFileExtension);
                    if (AlwaysListen) {
                        folderWatcher.Start();
                    }
                }
            });
        }

        public bool IsBayered {
            get => profileService.ActiveProfile.CameraSettings.FileCameraIsBayered;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraIsBayered = value;
                RaisePropertyChanged();
            }
        }

        public bool AlwaysListen {
            get => profileService.ActiveProfile.CameraSettings.FileCameraAlwaysListen;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraAlwaysListen = value;
                RaisePropertyChanged();
            }
        }

        public bool UseBulbMode {
            get => profileService.ActiveProfile.CameraSettings.FileCameraUseBulbMode;
            set {
                profileService.ActiveProfile.CameraSettings.FileCameraUseBulbMode = value;
                RaisePropertyChanged();
            }
        }

        private DateTime exposureStart;
        private TimeSpan exposureTime;

        public void StartExposure(CaptureSequence captureSequence) {
            if (!AlwaysListen) {
                folderWatcher.Start();
            }
            exposureStart = DateTime.Now;
            exposureTime = TimeSpan.FromSeconds(captureSequence.ExposureTime);
            if (UseBulbMode) {
                var exposureTime = captureSequence.ExposureTime;
                if (profileService.ActiveProfile.CameraSettings.BulbMode == CameraBulbModeEnum.TELESCOPESNAPPORT) {
                    Logger.Debug("Use Telescope Snap Port");

                    BulbCapture(exposureTime, RequestSnapPortCaptureStart, RequestSnapPortCaptureStop);
                } else if (profileService.ActiveProfile.CameraSettings.BulbMode == CameraBulbModeEnum.SERIALPORT) {
                    Logger.Debug("Use Serial Port for camera");

                    BulbCapture(exposureTime, StartSerialPortCapture, StopSerialPortCapture);
                } else if (profileService.ActiveProfile.CameraSettings.BulbMode == CameraBulbModeEnum.SERIALRELAY) {
                    Logger.Debug("Use serial relay for camera");

                    BulbCapture(exposureTime, StartSerialRelayCapture, StopSerialRelayCapture);
                } else {
                    throw new NotSupportedException("The file camera does not support the selected BulbMode");
                }
            }
        }

        public void StopExposure() {
            if (!AlwaysListen) {
                folderWatcher.Suspend();
            }
        }

        private void BulbCapture(double exposureTime, Action capture, Action stopCapture) {
            Logger.Debug("Starting bulb capture");
            capture();

            /*Stop Exposure after exposure time */
            Task.Run(async () => {
                await CoreUtil.Wait(TimeSpan.FromSeconds(exposureTime));

                stopCapture();

                Logger.Debug("Restore previous shutter speed");
            });
        }

        private void StartSerialRelayCapture() {
            Logger.Debug("Serial relay start of exposure");
            OpenSerialRelay();
            serialRelayInteraction.Send(new byte[] { 0xFF, 0x01, 0x01 });
        }

        private void StopSerialRelayCapture() {
            Logger.Debug("Serial relay stop of exposure");
            OpenSerialRelay();
            serialRelayInteraction.Send(new byte[] { 0xFF, 0x01, 0x00 });
        }

        private void StartSerialPortCapture() {
            Logger.Debug("Serial port start of exposure");
            OpenSerialPort();
            serialPortInteraction.EnableRts(true);
        }

        private void StopSerialPortCapture() {
            Logger.Debug("Serial port stop of exposure");
            OpenSerialPort();
            serialPortInteraction.EnableRts(false);
        }

        private void OpenSerialPort() {
            if (serialPortInteraction?.PortName != profileService.ActiveProfile.CameraSettings.SerialPort) {
                serialPortInteraction = new SerialPortInteraction(profileService.ActiveProfile.CameraSettings.SerialPort);
            }
            if (!serialPortInteraction.Open()) {
                throw new Exception("Unable to open SerialPort " + profileService.ActiveProfile.CameraSettings.SerialPort);
            }
        }

        private void OpenSerialRelay() {
            if (serialRelayInteraction?.PortName != profileService.ActiveProfile.CameraSettings.SerialPort) {
                serialRelayInteraction = new SerialRelayInteraction(profileService.ActiveProfile.CameraSettings.SerialPort);
            }
            if (!serialRelayInteraction.Open()) {
                throw new Exception("Unable to open SerialPort " + profileService.ActiveProfile.CameraSettings.SerialPort);
            }
        }

        private SerialPortInteraction serialPortInteraction;
        private SerialRelayInteraction serialRelayInteraction;

        private void RequestSnapPortCaptureStart() {
            Logger.Debug("Request start of exposure");
            var success = telescopeMediator.SendToSnapPort(true);
            if (!success) {
                throw new Exception("Request to telescope snap port failed");
            }
        }

        private void RequestSnapPortCaptureStop() {
            Logger.Debug("Request stop of exposure");
            var success = telescopeMediator.SendToSnapPort(false);
            if (!success) {
                throw new Exception("Request to telescope snap port failed");
            }
        }

        public void StartLiveView() {
            throw new System.NotImplementedException();
        }

        public Task<IExposureData> DownloadLiveView(CancellationToken token) {
            throw new System.NotImplementedException();
        }

        public void StopLiveView() {
            throw new System.NotImplementedException();
        }
    }

    public class FileCameraFolderWatcher : IDisposable {
        private ConcurrentQueue<string> fileQueue = new ConcurrentQueue<string>();
        private FileSystemWatcher fileWatcher;
        private string watchedFolder;
        private FileExtension fileExtension;
        private object lockObj = new object();

        public string GetNextItem() {
            if (fileQueue.Count == 0) {
                return null;
            }
            fileQueue.TryDequeue(out var path);
            return path;
        }

        public FileCameraFolderWatcher(string folder, FileExtension fileExtension) {
            if (string.IsNullOrWhiteSpace(folder)) {
                throw new Exception("No Folder for camera to watch was specified!");
            }
            this.fileExtension = fileExtension;
            watchedFolder = folder;
            fileQueue = new ConcurrentQueue<string>();
            fileWatcher = new FileSystemWatcher() {
                Path = watchedFolder,
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.*",
                EnableRaisingEvents = false,
                IncludeSubdirectories = false
            };

            fileWatcher.Created += FileWatcher_Created;
            fileWatcher.Renamed += FileWatcher_Renamed;
        }

        public void Start() {
            fileQueue = new ConcurrentQueue<string>();
            fileWatcher.EnableRaisingEvents = true;
        }

        public void Suspend() {
            fileWatcher.EnableRaisingEvents = false;
        }

        private void AddQueueItem(string path) {
            lock (lockObj) {
                var fileExt = Path.GetExtension(path).ToLower();
                if (Regex.IsMatch(fileExt, fileExtension.Pattern)) {
                    Logger.Trace($"Added file to Queue at {path}");
                    fileQueue.Enqueue(path);
                } else {
                    Logger.Trace($"Invalid file for Queue at {path}");
                }
            }
        }

        private void FileWatcher_Created(object sender, FileSystemEventArgs e) {
            Logger.Trace($"New file detected at {e.FullPath}");
            AddQueueItem(e.FullPath);
        }

        private void FileWatcher_Renamed(object sender, RenamedEventArgs e) {
            Logger.Trace($"File renaming detected. New file path {e.FullPath} - old file path {e.OldFullPath}");

            lock (lockObj) {
                var list = fileQueue.ToList();
                if (list.Contains(e.OldFullPath)) {
                    list.Remove(e.OldFullPath);
                }
                fileQueue = new ConcurrentQueue<string>(list);
                AddQueueItem(e.FullPath);
            }
        }

        public void Dispose() {
            if (fileWatcher != null) fileWatcher.Dispose();
        }
    }

    public class FileExtension {

        public FileExtension(string name, string pattern) {
            Name = name;
            Pattern = pattern;
        }

        public string Name { get; }
        public string Pattern { get; }

        public override string ToString() {
            return $"{Name} - {Pattern}";
        }
    }
}