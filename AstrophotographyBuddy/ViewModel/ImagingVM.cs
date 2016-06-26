using AstrophotographyBuddy.Model;
using AstrophotographyBuddy.Utility;
using nom.tam.fits;
using nom.tam.util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AstrophotographyBuddy.ViewModel {
    class ImagingVM : BaseVM{

        public ImagingVM() {
            Name = "Imaging";
            SnapExposureDuration = 1;
            SnapCommand = new AsyncCommand<bool>(() => captureImage());
            CancelSnapCommand = new RelayCommand(cancelCaptureImage);
            StartSequenceCommand = new AsyncCommand<bool>(() => startSequence());
            CancelSequenceCommand = new RelayCommand(cancelSequence);
    }

        public PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
            }
        }

        private SequenceVM _seqVM;
        public SequenceVM SeqVM {
            get {
                if(_seqVM == null) {
                    _seqVM = new SequenceVM();
                }
                return _seqVM;
            }
            set {
                _seqVM = value;
                RaisePropertyChanged();
            }
        }

        private CameraModel _cam;
        public CameraModel Cam {
            get {
                return _cam;
            }
            set {
                _cam = value;
                RaisePropertyChanged();
            }
        }

        private FilterWheelModel _fW;
        public FilterWheelModel FW {
            get {
                return _fW;
            }
            set {
                _fW = value;
                RaisePropertyChanged();
            }
        }



        private double _snapExposureDuration;
        public double SnapExposureDuration {
            get {
                return _snapExposureDuration;
            }

            set {
                _snapExposureDuration = value;
                RaisePropertyChanged();
            }
        }

        private int _exposureSeconds; 
        public int ExposureSeconds {
            get {
                return _exposureSeconds;
            }
            set {
                _exposureSeconds = value;
                RaisePropertyChanged();
            }
        }

        private String _expStatus;
        public String ExpStatus {
            get {
                return _expStatus;
            }

            set {
                _expStatus = value;
                RaisePropertyChanged();
            }
        }
        
        private IAsyncCommand _snapCommand;
        public IAsyncCommand SnapCommand {
            get {
                return _snapCommand;
            }
            set {
                _snapCommand = value;
                RaisePropertyChanged();
            }
        }

        private IAsyncCommand _startSequenceCommand;
        public IAsyncCommand StartSequenceCommand {
            get {
                return _startSequenceCommand;
            }
            set {
                _startSequenceCommand = value;
                RaisePropertyChanged();
            }
        }

        

        private async Task<bool> startSequence(ICollection<SequenceModel> sequence, CancellationTokenSource tokenSource) {
            short i = 1;
            foreach (SequenceModel seq in sequence) {
                seq.Active = true;
                double duration = seq.ExposureTime;
                while (seq.ExposureCount > 0) {

                    if (seq.FilterType != null && FW.Connected) {
                        FW.Position = seq.FilterType.Position;
                        ExpStatus = ExposureStatus.FILTERCHANGE;

                        await Task.Run(() => {
                            while (FW.Position == -1) {
                                //Wait for filter change;
                                if (tokenSource.IsCancellationRequested) {
                                    return;
                                }
                            }
                        });
                        if (tokenSource.IsCancellationRequested) {
                            return false;
                        }
                    }

                    if(seq.Binning == null) {
                        Cam.setBinning(1,1);
                    } else {
                        Cam.setBinning(seq.Binning.X, seq.Binning.Y);
                    }
                    


                    /*Capture*/
                    ExpStatus = ExposureStatus.CAPTURING;
                    bool isLight = false;
                    if (Cam.HasShutter) {
                        isLight = true;
                    }
                    Cam.startExposure(duration, isLight);
                    ExposureSeconds = 1;

                    /* Wait for Capture */
                    if (duration >= 1) {
                        await Task.Run(async () => {
                            do {
                                await Task.Delay(1000);
                                if (tokenSource.IsCancellationRequested) {
                                    return;
                                }
                                ExposureSeconds += 1;
                            } while (ExposureSeconds < duration);
                        });
                    }
                    if (tokenSource.IsCancellationRequested) {
                        ExpStatus = ExposureStatus.IDLE;
                        Cam.stopExposure();
                        return false;
                    }

                    /*Download Image */
                    ExpStatus = ExposureStatus.DOWNLOADING;
                    Int32[,] arr = await Task.Run<Int32[,]>(() => {
                        return Cam.downloadExposure(tokenSource);
                    });

                    if (tokenSource.IsCancellationRequested) {
                        ExpStatus = ExposureStatus.IDLE;
                        return false;
                    }

                    /*Convert Array to Int16*/
                    ExpStatus = ExposureStatus.PREPARING;
                    Utility.Utility.ImageArray iarr = await Task.Run<Utility.Utility.ImageArray>(() => {
                        return Utility.Utility.convert2DArray(arr);
                    });

                    if (tokenSource.IsCancellationRequested) {
                        ExpStatus = ExposureStatus.IDLE;
                        return false;
                    }

                    /*Prepare Image for UI*/
                    BitmapSource tmp = Utility.Utility.createSourceFromArray(iarr.FlatArray, iarr.X, iarr.Y);
                    tmp = Cam.NormalizeTiffTo8BitImage(tmp);

                    /*Save to disk*/
                    ExpStatus = ExposureStatus.SAVING;
                    await Task.Run(() => {

                        List<OptionsVM.ImagePattern> p = new List<OptionsVM.ImagePattern>();
                        if(FW.Filters != null) {
                            p.Add(new OptionsVM.ImagePattern("$$FILTER$$", "Filtername", FW.Filters.ElementAt(FW.Position).Name));
                        } else {
                            p.Add(new OptionsVM.ImagePattern("$$FILTER$$", "Filtername", ""));
                        }
                        p.Add(new OptionsVM.ImagePattern("$$EXPOSURETIME$$", "Exposure Time in seconds", string.Format("{0:0.00}", seq.ExposureTime)));     
                        p.Add(new OptionsVM.ImagePattern("$$DATE$$", "Date with format YYYY-MM-DD", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")));
                        p.Add(new OptionsVM.ImagePattern("$$FRAMENR$$", "# of the Frame with format ####", string.Format("{0:0000}", i)));
                        p.Add(new OptionsVM.ImagePattern("$$IMAGETYPE$$", "Light, Flat, Dark, Bias", seq.ImageType));
                        if(seq.Binning == null) {
                            p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", "1x1"));
                        } else {
                            p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", seq.Binning.Name));
                        }
                        
                        p.Add(new OptionsVM.ImagePattern("$$SENSORTEMP$$", "Temperature of the Camera", string.Format("{0:00}", Cam.CCDTemperature)));

                        string filename = Utility.Utility.getImageFileString(p);
                        string completefilename = Settings.ImageFilePath + filename;
                        Utility.Utility.saveTiff(iarr, string.Format(completefilename, seq.ExposureCount));
                    });

                    if (tokenSource.IsCancellationRequested) {
                        ExpStatus = ExposureStatus.IDLE;
                        return false;
                    }

                    if(seq.Dither && ((seq.ExposureCount % seq.DitherAmount) == 0)) {
                        ExpStatus = ExposureStatus.DITHERING;
                        await PHD2Client.dither();
                        
                        ExpStatus = ExposureStatus.SETTLING;
                        await Task.Run<bool>(async () => {
                            while (PHD2Client.IsDithering) {
                                await Task.Delay(100);
                                if (tokenSource.IsCancellationRequested) {
                                    ExpStatus = ExposureStatus.IDLE;
                                    return false;
                                }
                            }
                            return true;
                        });
                    }
                    if (tokenSource.IsCancellationRequested) {
                        ExpStatus = ExposureStatus.IDLE;
                        return false;
                    }

                    Image = tmp;
                    seq.ExposureCount -= 1;
                    i++;
                }
                seq.Active = false;
            }

            ExpStatus = ExposureStatus.IDLE;
            return await Task.Run<bool>(() => { return true; });
        }

        private async Task<bool> startSequence(CancellationToken token = new CancellationToken()) {
            _cancelSequenceToken = new CancellationTokenSource();
            return await startSequence(SeqVM.Sequence, _cancelSequenceToken);           
        }

        BitmapSource _image;
        public BitmapSource Image {
            get {
                return _image;
            }
            set {
                _image = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand CancelSnapCommand {
            get {
                return _cancelSnapCommand;
            }

            set {
                _cancelSnapCommand = value;
                RaisePropertyChanged();
            }
        }

        public RelayCommand CancelSequenceCommand {
            get {
                return _cancelSequenceCommand;
            }

            set {
                _cancelSequenceCommand = value;
                RaisePropertyChanged();
            }
        }

        private void cancelCaptureImage(object o) {
            if (_captureImageToken != null) {
                _captureImageToken.Cancel();
            }
        }

        private void cancelSequence(object o) {
            if (_cancelSequenceToken != null) {
                _cancelSequenceToken.Cancel();
            }
        }

        CancellationTokenSource _cancelSequenceToken;
        private RelayCommand _cancelSequenceCommand;

        CancellationTokenSource _captureImageToken;
        private RelayCommand _cancelSnapCommand;

        private FilterWheelModel.FilterInfo _snapFilter;
        public FilterWheelModel.FilterInfo SnapFilter {
            get {
                return _snapFilter;
            }
            set {
                _snapFilter = value;
                RaisePropertyChanged();
            }
        }

        private CameraModel.BinningMode _snapBin;
        public CameraModel.BinningMode SnapBin {
            get {
                if(_snapBin == null) {
                    _snapBin = new CameraModel.BinningMode(1, 1);
                }
                return _snapBin;
            }
            set {
                _snapBin = value;
                RaisePropertyChanged();
            }
        }

        private async Task<bool> captureImage() {
            _captureImageToken = new CancellationTokenSource();
            List<SequenceModel> seq = new List<SequenceModel>();
            seq.Add(new SequenceModel(SnapExposureDuration, "Snap", SnapFilter, SnapBin, 1));
            return await startSequence(seq, _captureImageToken);     
        }

        public static class ExposureStatus {
            public const string CAPTURING = "Capturing...";
            public const string DOWNLOADING = "Downloading...";
            public const string FILTERCHANGE = "Switching Filter...";
            public const string PREPARING = "Preparing...";
            public const string SAVING = "Saving...";
            public const string IDLE = "Idle";
            public const string DITHERING = "Dithering...";
            public const string SETTLING = "Settling...";
        }
    }
}
