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
using static AstrophotographyBuddy.Model.SequenceModel;

namespace AstrophotographyBuddy.ViewModel {
    class ImagingVM : BaseVM{

        public ImagingVM() {
            Name = "Imaging";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["ImagingSVG"];

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

        private bool _isExposing; 
        public bool IsExposing {
            get {
                return _isExposing;
            } set {
                _isExposing = value;
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

        

        private async Task changeFilter(SequenceModel seq, CancellationTokenSource tokenSource) {
            if (seq.FilterType != null && FW.Connected) {
                FW.Position = seq.FilterType.Position;
                ExpStatus = ExposureStatus.FILTERCHANGE;

                await Task.Run(() => {
                    while (FW.Position == -1) {
                        //Wait for filter change;
                        tokenSource.Token.ThrowIfCancellationRequested();
                    }
                });
                tokenSource.Token.ThrowIfCancellationRequested();                
            }
        }

        private void setBinning(SequenceModel seq) {
            if (seq.Binning == null) {
                Cam.setBinning(1, 1);
            }
            else {
                Cam.setBinning(seq.Binning.X, seq.Binning.Y);
            }
        }

        private async Task capture(SequenceModel seq, CancellationTokenSource tokenSource) {            
            double duration = seq.ExposureTime;
            ExpStatus = string.Format(ExposureStatus.EXPOSING, 0, duration);
            bool isLight = false;
            if (Cam.HasShutter) {
                isLight = true;
            }
            Cam.startExposure(duration, isLight);
            ExposureSeconds = 1;
            ExpStatus = string.Format(ExposureStatus.EXPOSING, 1, duration);
            /* Wait for Capture */
            if (duration >= 1) {
                await Task.Run(async () => {
                    do {
                        await Task.Delay(1000);
                        tokenSource.Token.ThrowIfCancellationRequested();
                        ExposureSeconds += 1;
                        ExpStatus = string.Format(ExposureStatus.EXPOSING, ExposureSeconds, duration);
                    } while (ExposureSeconds < duration);
                });
            }
            tokenSource.Token.ThrowIfCancellationRequested();
        }

        private async Task<Int32[,]> download(CancellationTokenSource tokenSource) {
            ExpStatus = ExposureStatus.DOWNLOADING;
            return await Cam.downloadExposure(tokenSource);
        }

        private async Task<Utility.Utility.ImageArray> convert(Int32[,] arr, CancellationTokenSource tokenSource) {
            ExpStatus = ExposureStatus.PREPARING;
            Utility.Utility.ImageArray iarr = await Utility.Utility.convert2DArray(arr);
            tokenSource.Token.ThrowIfCancellationRequested();
            return iarr;
        }

        public BitmapSource prepare(ushort[] arr, int x, int y) {
            BitmapSource src = Utility.Utility.createSourceFromArray(arr, x, y, System.Windows.Media.PixelFormats.Gray16);
            return src;// Utility.Utility.NormalizeTiffTo8BitImage(src);
        }

        private async Task<bool> save(SequenceModel seq, Utility.Utility.ImageArray iarr, ushort framenr,  CancellationTokenSource tokenSource) {
            ExpStatus = ExposureStatus.SAVING;
            await Task.Run(() => {

                List<OptionsVM.ImagePattern> p = new List<OptionsVM.ImagePattern>();
                string filter = string.Empty;
                if (FW.Filters != null) {
                    filter = FW.Filters.ElementAt(FW.Position).Name;
                    p.Add(new OptionsVM.ImagePattern("$$FILTER$$", "Filtername", filter));
                }
                else {
                    p.Add(new OptionsVM.ImagePattern("$$FILTER$$", "Filtername", filter));
                }
                p.Add(new OptionsVM.ImagePattern("$$EXPOSURETIME$$", "Exposure Time in seconds", string.Format("{0:0.00}", seq.ExposureTime)));
                p.Add(new OptionsVM.ImagePattern("$$DATE$$", "Date with format YYYY-MM-DD", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")));
                p.Add(new OptionsVM.ImagePattern("$$FRAMENR$$", "# of the Frame with format ####", string.Format("{0:0000}", framenr)));
                p.Add(new OptionsVM.ImagePattern("$$IMAGETYPE$$", "Light, Flat, Dark, Bias", seq.ImageType));
                
                if (seq.Binning == null) {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", "1x1"));
                }
                else {
                    p.Add(new OptionsVM.ImagePattern("$$BINNING$$", "Binning of the camera", seq.Binning.Name));
                }

                p.Add(new OptionsVM.ImagePattern("$$SENSORTEMP$$", "Temperature of the Camera", string.Format("{0:00}", Cam.CCDTemperature)));

                string filename = Utility.Utility.getImageFileString(p);
                string completefilename = Settings.ImageFilePath + filename;
                if (Settings.FileType == FileTypeEnum.FITS) {                    
                    string imagetype = seq.ImageType;
                    if (imagetype == "SNAP") imagetype = "LIGHT";
                    Utility.Utility.saveFits(iarr, completefilename, seq.ImageType, seq.ExposureTime, filter, seq.Binning, Cam.CCDTemperature);
                } else if (Settings.FileType == FileTypeEnum.TIFF) {
                    Utility.Utility.saveTiff(iarr, completefilename);
                } else {
                    Utility.Utility.saveTiff(iarr, completefilename);
                }
                
                
            });

            tokenSource.Token.ThrowIfCancellationRequested();
            return true;
        }

        private async Task<bool> dither(SequenceModel seq, CancellationTokenSource tokenSource) {
            if (seq.Dither && ((seq.ExposureCount % seq.DitherAmount) == 0)) {
                ExpStatus = ExposureStatus.DITHERING;
                await PHD2Client.dither();

                ExpStatus = ExposureStatus.SETTLING;
                await Task.Run<bool>(async () => {
                    while (PHD2Client.IsDithering) {
                        await Task.Delay(100);
                        tokenSource.Token.ThrowIfCancellationRequested();
                    }
                    return true;
                });
            }
            tokenSource.Token.ThrowIfCancellationRequested();
            return true;
        }

        public  async Task<bool> startSequence(ICollection<SequenceModel> sequence, bool bSave, CancellationTokenSource tokenSource) {
            try {
                IsExposing = true;

                ushort framenr = 1;
                foreach (SequenceModel seq in sequence) {
                    seq.Active = true;
                    
                    while (seq.ExposureCount > 0) {

                        /*Change Filter*/
                        await changeFilter(seq, tokenSource);

                        /*Set Camera Binning*/
                        setBinning(seq);
                        
                        /*Capture*/
                        await capture(seq, tokenSource);

                        /*Download Image */                        
                        Int32[,] arr = await download(tokenSource);

                        /*Convert Array to ushort*/
                        SourceArray = await convert(arr, tokenSource);

                        /*Prepare Image for UI*/
                        ExpStatus = ImagingVM.ExposureStatus.PREPARING;
                        BitmapSource tmp = prepare(SourceArray.FlatArray, SourceArray.X, SourceArray.Y);
                        Image = tmp;

                        /*Save to disk*/
                        if (bSave) { 
                            await save(seq, SourceArray, framenr, tokenSource);
                        }

                        /*Dither*/
                        await dither(seq, tokenSource);

                        if (AutoStretch) {
                            await stretch();
                        }

                        seq.ExposureCount -= 1;
                        framenr++;
                    }
                    seq.Active = false;
                }
            }
            catch (System.OperationCanceledException ex) {
                Logger.trace(ex.Message);
            }
            finally {
                ExpStatus = ExposureStatus.IDLE;
                Cam.stopExposure();
                IsExposing = false;
            }            
            return await Task.Run<bool>(() => { return true; });
        }

        private async Task<bool> startSequence(CancellationToken token = new CancellationToken()) {
            _cancelSequenceToken = new CancellationTokenSource();
            return await startSequence(SeqVM.Sequence, true, _cancelSequenceToken);           
        }


        private bool _autoStretch;
        public bool AutoStretch {
            get {
                return _autoStretch;
            }
            set {
                _autoStretch = value;
                RaisePropertyChanged();
            }

        }

        public async Task<bool> stretch() {
            if (Image != null) {
                ushort[] arr = await Utility.Utility.stretchArray(_sourceArray);
                BitmapSource bs = prepare(arr, _sourceArray.X, _sourceArray.Y);
                Image = bs;
            }
            bool run = await Task.Run<bool>(() => { return true; });
            return run;
        }

        private Utility.Utility.ImageArray _sourceArray;
        public Utility.Utility.ImageArray SourceArray {
            get {
                return _sourceArray;
            }
            private set {
                _sourceArray = value;
                RaisePropertyChanged();
            }
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
            seq.Add(new SequenceModel(SnapExposureDuration, ImageTypes.SNAP, SnapFilter, SnapBin, 1));
            return await startSequence(seq, true, _captureImageToken);     
        }

        public static class ExposureStatus {
            public const string EXPOSING = "Exposing {0}/{1}s...";
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
