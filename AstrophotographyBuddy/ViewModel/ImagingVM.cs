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
            Stopwatch sw = Stopwatch.StartNew();
            Int32[,] arr = await Task.Run<Int32[,]>(() => {                
                return Cam.snap(1, true);                
            });
            sw.Stop();
            Console.WriteLine("Camerasnap: "  + sw.Elapsed);
            
            sw = Stopwatch.StartNew();
            Utility.Utility.ImageArray iarr = await Task.Run<Utility.Utility.ImageArray>(() => {
                return Utility.Utility.convert2DArray(arr);
            });           
            sw.Stop();
            Console.WriteLine("Arrayconversion: " + sw.Elapsed);

            sw = Stopwatch.StartNew();
            
            BitmapSource tmp = Utility.Utility.createSourceFromArray(iarr.FlatArray, iarr.X, iarr.Y);
            tmp = Cam.NormalizeTiffTo8BitImage(tmp);

            sw.Stop();
            Console.WriteLine("ImageSource: " + sw.Elapsed);

            sw = Stopwatch.StartNew();
            await Task.Run(() => {
                //int[] dim = Utility.Utility.getDim(arr);
                Utility.Utility.saveTiff(iarr);
            });
            sw.Stop();
            Console.WriteLine("SaveFits: " + sw.Elapsed);

            return tmp;
        }
        
    }
}
