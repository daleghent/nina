using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
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
            SnapCommand = new AsyncCommand<BitmapSource>(() =>
             captureImage());        
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

        /*private BitmapSource _imgSource;
        public BitmapSource ImgSource {
            get {
                return _imgSource;
            }
            set {
                _imgSource = value;
                RaisePropertyChanged();
            }
        }*/

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

        /*
        private void capture(object o) {
            ImgSource =  Cam.snap(30, true);

            using (FileStream fs = new FileStream("test.tif", FileMode.Create)) {
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Compression = TiffCompressOption.None;
                encoder.Frames.Add(BitmapFrame.Create(ImgSource));
                encoder.Save(fs);
            }
        }*/

        

        private async Task<BitmapSource> captureImage(CancellationToken token = new CancellationToken()) {            
            var arr = await Task.Run<Int16[]>(() => {                
                return Cam.snap(1, true);                
            });
            BitmapSource tmp = Cam.createSourceFromArray(arr);
            tmp = Cam.NormalizeTiffTo8BitImage(tmp);
            return tmp;
        }

        //test
        private BitmapSource load() {
            FileStream ImageStream = new FileStream(@"E:\Astrofotografie\2015-03-07 Oriongürtel\Oriongürtel1.tif", FileMode.Open, FileAccess.Read, FileShare.Read);
            TiffBitmapDecoder ImageDecoder = new TiffBitmapDecoder(ImageStream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);
            return ImageDecoder.Frames.FirstOrDefault();
        }


        /*public void snap(object o) {
            ASCOM.Utilities.Util U = new ASCOM.Utilities.Util();
            Cam.AscomCamera.StartExposure(5.151, true);
            while (!Cam.ImageReady) {
                //Console.Write(".");
                U.WaitForMilliseconds(100);
            }

            Array camArray = (Array)Cam.AscomCamera.ImageArray;

            Int16[] flatArray = flattenArray<Int16>(camArray);

            System.Windows.Media.PixelFormat pf = System.Windows.Media.PixelFormats.Gray16;

            List<System.Windows.Media.Color> colors = new List<System.Windows.Media.Color>();
            colors.Add(System.Windows.Media.Colors.Gray);
            BitmapPalette pallet = new BitmapPalette(colors);
            //int stride = C.CameraYSize * ((Convert.ToString(C.MaxADU, 2)).Length + 7) / 8;
            int stride = (Cam.CameraXSize * pf.BitsPerPixel + 7) / 8;
            double dpi = 96;


            ImgSource = BitmapSource.Create(Cam.CameraXSize, Cam.CameraYSize, dpi, dpi, pf, null, flatArray, stride);

            
            using (FileStream fs = new FileStream("test.tif", FileMode.Create)) {
                TiffBitmapEncoder encoder = new TiffBitmapEncoder();
                encoder.Compression = TiffCompressOption.None;
                encoder.Frames.Add(BitmapFrame.Create(bmpSource));
                encoder.Save(fs);
            }

            
        }*/



    }
}
