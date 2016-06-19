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

        

        private async Task<bool> startSequence(CancellationToken token = new CancellationToken()) {
            _cancelSequenceToken = new CancellationTokenSource();
            foreach (SequenceModel seq in SeqVM.Sequence) {
                int exposures = seq.ExposureCount;
                double duration = seq.ExposureTime;
                for(int i = 0; i< exposures; i++) {

                    /*Capture*/
                    ExpStatus = ExposureStatus.CAPTURING;
                    Cam.startExposure(duration, true);
                    ExposureSeconds = 1;

                    /* Wait for Capture */
                    if (duration >= 1) {
                        await Task.Run(async () => {
                            do {
                                await Task.Delay(1000);
                                if (_captureImageToken.IsCancellationRequested) {
                                    return;
                                }
                                ExposureSeconds += 1;
                            } while (ExposureSeconds < duration);
                        });
                    }
                    if (_cancelSequenceToken.IsCancellationRequested) {
                        ExpStatus = ExposureStatus.IDLE;
                        Cam.stopExposure();
                        return false;
                    }

                    /*Download Image */
                    ExpStatus = ExposureStatus.DOWNLOADING;
                    Int32[,] arr = await Task.Run<Int32[,]>(() => {
                        return Cam.downloadExposure(_cancelSequenceToken);
                    });

                    if (_cancelSequenceToken.IsCancellationRequested) {
                        ExpStatus = ExposureStatus.IDLE;                       
                        return false;
                    }

                    /*Convert Array to Int16*/
                    ExpStatus = ExposureStatus.PREPARING;
                    Utility.Utility.ImageArray iarr = await Task.Run<Utility.Utility.ImageArray>(() => {
                        return Utility.Utility.convert2DArray(arr);
                    });

                    if (_cancelSequenceToken.IsCancellationRequested) {
                        ExpStatus = ExposureStatus.IDLE;
                        return false;
                    }

                    /*Prepare Image for UI*/
                    BitmapSource tmp = Utility.Utility.createSourceFromArray(iarr.FlatArray, iarr.X, iarr.Y);
                    tmp = Cam.NormalizeTiffTo8BitImage(tmp);

                    /*Save to disk*/
                    ExpStatus = ExposureStatus.SAVING;
                    await Task.Run(() => {
                        Utility.Utility.saveTiff(iarr, string.Format("test{0}.tif", i));
                    });

                    if (_cancelSequenceToken.IsCancellationRequested) {
                        ExpStatus = ExposureStatus.IDLE;
                        return false;
                    }

                    Image = tmp;
                }
            }
            return true;
            
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

        private async Task<bool> captureImage() {
            _captureImageToken = new CancellationTokenSource();
            ExpStatus = ExposureStatus.CAPTURING;
            Cam.startExposure(SnapExposureDuration, true);
            ExposureSeconds = 1;
            if (SnapExposureDuration >= 1) {
                await Task.Run(async () => {
                    do {
                        await Task.Delay(1000);
                        if (_captureImageToken.IsCancellationRequested) {
                            return;
                        }
                       ExposureSeconds += 1;
                    } while (ExposureSeconds < SnapExposureDuration);
                });
            }            
            if (_captureImageToken.IsCancellationRequested) {
                ExpStatus = ExposureStatus.IDLE;
                Cam.stopExposure();
                return false;
            }

            ExpStatus = ExposureStatus.DOWNLOADING;
            Int32[,] arr = await Task.Run<Int32[,]>(() => {
                return Cam.downloadExposure(_captureImageToken);
            });

            if (_captureImageToken.IsCancellationRequested) {
                ExpStatus = ExposureStatus.IDLE;
                return false;
            }

            ExpStatus = ExposureStatus.PREPARING;
            Utility.Utility.ImageArray iarr = await Task.Run<Utility.Utility.ImageArray>(() => {
                return Utility.Utility.convert2DArray(arr);
            });

            if (_captureImageToken.IsCancellationRequested) {
                ExpStatus = ExposureStatus.IDLE;
                return false;
            }

            BitmapSource tmp = Utility.Utility.createSourceFromArray(iarr.FlatArray, iarr.X, iarr.Y);
            tmp = Cam.NormalizeTiffTo8BitImage(tmp);

            ExpStatus = ExposureStatus.SAVING;
            await Task.Run(() => {
                Utility.Utility.saveTiff(iarr, "test.tif");
            });

            if (_captureImageToken.IsCancellationRequested) {
                ExpStatus = ExposureStatus.IDLE;
                return false;
            }

            Image = tmp;
            ExpStatus = ExposureStatus.IDLE;
            return true;            
        }

        public static class ExposureStatus {
            public const string CAPTURING = "Capturing...";
            public const string DOWNLOADING = "Downloading...";
            public const string PREPARING = "Preparing...";
            public const string SAVING = "Saving...";
            public const string IDLE = "Idle";
        }
    }
}
