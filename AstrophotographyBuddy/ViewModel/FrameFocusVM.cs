using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace AstrophotographyBuddy.ViewModel {
    class FrameFocusVM : BaseVM {
        public FrameFocusVM() {
            Name = "Frame & Focus";
            ImageURI = @"/AstrophotographyBuddy;component/Resources/Focus.png";

            SnapCommand = new RelayCommand(capture);
            ApplyImageParamsCommand = new RelayCommand(applyImageParams);
            Gamma = 1;
            Contrast = 1;
            Brightness = 1;

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

        private NotifyTaskCompletion<BitmapSource> _imgSource;
        public NotifyTaskCompletion<BitmapSource> ImgSource {
            get {
                return _imgSource;
            }
            set {
                _imgSource = value;
                RaisePropertyChanged();
            }
        }

        private NotifyTaskCompletion<BitmapSource> _imgSource16;
        public NotifyTaskCompletion<BitmapSource> ImgSource16 {
            get {
                return _imgSource16;
            }

            set {
                _imgSource16 = value;
                RaisePropertyChanged();
            }
        }

        /*http://stackoverflow.com/questions/17187113/how-can-i-use-async-in-an-mvvmcross-view-model*/
        private void capture(object o) {
            /*ImgSource = new NotifyTaskCompletion<BitmapSource>(Task<BitmapSource>.Run(() => {
                var arr = Cam.snap(1, true);
                return Utility.Utility.createSourceFromArray(arr, );
           })
           );*/
            


            //NotifyTaskCompletion<BitmapSource> t = new NotifyTaskCompletion<BitmapSource>(Task<BitmapSource>.Run(() => Cam.snap(1, true)));

            //ImgSource16 = await t.Task;

            //ImgSource = new NotifyTaskCompletion<BitmapSource>(Task<BitmapSource>.Run(() => Cam.NormalizeTiffTo8BitImage(a.Clone())));

            //ImgSource = Cam.NormalizeTiffTo8BitImage(ImgSource16.Clone());
            //ImgSource16 = Cam.snap(1, true);
            //ImgSource = Cam.NormalizeTiffTo8BitImage(ImgSource16.Result.Clone());

        }

        private void applyImageParams(object o) {
            RaisePropertyChanged("ImgSource");
            if(ImgSource != null) {
                //ImgSource = applyImageParams();
                //ImgSource = await Task.Run(() => applyImageParams(clone));
            }
            
        }


        private BitmapSource applyImageParams() { 
            Bitmap b = BitmapFromSource(ImgSource.Result);            
            adjustGamma(b, 4,4,4);
            //adjustGamma(b, Gamma, Gamma, Gamma);          
            BitmapSource result = ConvertBitmap(b);
            b.Dispose();
            return result;
        }

        /*private Task<BitmapSource> applyImageParams(BitmapSource source) {
            return Task.Run(() => {
                Bitmap b = BitmapFromSource(source.Clone());
                adjustGamma(b, Gamma, Gamma, Gamma);
                b.Dispose();
                BitmapSource result = ConvertBitmap(b);
                return result;
            });            
        }*/




        private int _brightness;
        public int Brightness {
            get {
                return _brightness;
            }

            set {
                _brightness = value;
                RaisePropertyChanged();
            }
        }

        private sbyte _contrast;
        public sbyte Contrast {
            get {
                return _contrast;
            }

            set {
                _contrast = value;
                RaisePropertyChanged();
            }
        }

        private double _gamma;
        public double Gamma {
            get {
                return _gamma;
            }

            set {
                _gamma = value;
                RaisePropertyChanged();
            }
        }



        private ICommand _snapCommand;
        public ICommand SnapCommand {
            get {
                return _snapCommand;
            }
            set {
                _snapCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _applyImageParamsCommand;
        public ICommand ApplyImageParamsCommand {
            get {
                return _applyImageParamsCommand;
            }
            set {
                _applyImageParamsCommand = value;
                RaisePropertyChanged();
            }
        }

       

        public static BitmapSource ConvertBitmap(Bitmap source) {
            return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                          source.GetHbitmap(),
                          IntPtr.Zero,
                          System.Windows.Int32Rect.Empty,
                          BitmapSizeOptions.FromEmptyOptions());
        }

        public static Bitmap BitmapFromSource(BitmapSource bitmapsource) {
            Bitmap bitmap;
            using (var outStream = new MemoryStream()) {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }

        public static bool adjustGamma(Bitmap b, double red, double green, double blue) {
            if (red < .2 || red > 5) return false;
            if (green < .2 || green > 5) return false;
            if (blue < .2 || blue > 5) return false;

            byte[] redGamma = new byte[256];
            byte[] greenGamma = new byte[256];
            byte[] blueGamma = new byte[256];

            for (int i = 0; i < 256; ++i) {
                redGamma[i] = (byte)Math.Min(255, (int)((255.0 * Math.Pow(i / 255.0, 1.0 / red)) + 0.5));
                greenGamma[i] = (byte)Math.Min(255, (int)((255.0 * Math.Pow(i / 255.0, 1.0 / green)) + 0.5));
                blueGamma[i] = (byte)Math.Min(255, (int)((255.0 * Math.Pow(i / 255.0, 1.0 / blue)) + 0.5));
            }

            // GDI+ still lies to us - the return format is BGR, NOT RGB.
            BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            int stride = bmData.Stride;
            System.IntPtr Scan0 = bmData.Scan0;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                int nOffset = stride - b.Width * 3;

                for (int y = 0; y < b.Height; ++y) {
                    for (int x = 0; x < b.Width; ++x) {
                        p[2] = redGamma[p[2]];
                        p[1] = greenGamma[p[1]];
                        p[0] = blueGamma[p[0]];

                        p += 3;
                    }
                    p += nOffset;
                }
            }

            b.UnlockBits(bmData);

            return true;
        }

        public static bool adjustBrightness(Bitmap b, int nBrightness)
   		{
   			if (nBrightness< -255 || nBrightness> 255)
   				return false;
   
   			// GDI+ still lies to us - the return format is BGR, NOT RGB.
   			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
   
   			int stride = bmData.Stride;
   			System.IntPtr Scan0 = bmData.Scan0;
   
   			int nVal = 0;
   
   			unsafe
   			{
   				byte* p = (byte*)(void*)Scan0;
   
   				int nOffset = stride - b.Width * 3;
   				int nWidth = b.Width * 3;
   
   				for(int y = 0; y<b.Height;++y)
   				{
  					for(int x = 0; x<nWidth; ++x )
  					{
  						nVal = (int) (p[0] + nBrightness);
  		
  						if (nVal< 0) nVal = 0;
  						if (nVal > 255) nVal = 255;
  
  						p[0] = (byte)nVal;
  
  						++p;
  					}
  					p += nOffset;
  				}
  			}
  
  			b.UnlockBits(bmData);
  
  			return true;
  		}
  
  		public static bool adjustContrast(Bitmap b, sbyte nContrast)
  		{
  			if (nContrast< -100) return false;
  			if (nContrast >  100) return false;
  
  			double pixel = 0, contrast = (100.0 + nContrast) / 100.0;
  
  			contrast *= contrast;
  
  			int red, green, blue;
  			
  			// GDI+ still lies to us - the return format is BGR, NOT RGB.
  			BitmapData bmData = b.LockBits(new Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
  
  			int stride = bmData.Stride;
  			System.IntPtr Scan0 = bmData.Scan0;
  
  			unsafe
  			{
  				byte* p = (byte*)(void*)Scan0;
  
  				int nOffset = stride - b.Width * 3;
  
  				for(int y = 0; y<b.Height;++y)
  				{
  					for(int x = 0; x<b.Width; ++x )
  					{
  						blue = p[0];
  						green = p[1];
  						red = p[2];
  				
  						pixel = red/255.0;
  						pixel -= 0.5;
  						pixel *= contrast;
  						pixel += 0.5;
  						pixel *= 255;
  						if (pixel< 0) pixel = 0;
  						if (pixel > 255) pixel = 255;
  						p[2] = (byte) pixel;
  
  						pixel = green/255.0;
  						pixel -= 0.5;
  						pixel *= contrast;
  						pixel += 0.5;
  						pixel *= 255;
  						if (pixel< 0) pixel = 0;
  						if (pixel > 255) pixel = 255;
  						p[1] = (byte) pixel;
  
  						pixel = blue/255.0;
  						pixel -= 0.5;
  						pixel *= contrast;
  						pixel += 0.5;
  						pixel *= 255;
  						if (pixel< 0) pixel = 0;
  						if (pixel > 255) pixel = 255;
  						p[0] = (byte) pixel;					
  
  						p += 3;
  					}
  					p += nOffset;
  				}
  			}
  
  			b.UnlockBits(bmData);
  
  			return true;
  		}

        public Bitmap equalizeHist(Bitmap b) {
            BitmapData data = b.LockBits(new System.Drawing.Rectangle(0, 0, b.Width, b.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* ptr = (byte*)data.Scan0;

                int remain = data.Stride - data.Width * 3;

                int[] histogram = new int[256];
                for (int i = 0; i < histogram.Length; i++)
                    histogram[i] = 0;

                for (int i = 0; i < data.Height; i++) {
                    for (int j = 0; j < data.Width; j++) {
                        int mean = ptr[0] + ptr[1] + ptr[2];
                        mean /= 3;

                        histogram[mean]++;
                        ptr += 3;
                    }

                    ptr += remain;
                }

                float[] LUT = equalize(histogram, data.Width * data.Height);
                ptr = (byte*)data.Scan0;

                for (int i = 0; i < data.Height; i++) {
                    for (int j = 0; j < data.Width; j++) {
                        int index = ptr[0];
                        byte nValue = (byte)LUT[index];
                        if (LUT[index] > 255)
                            nValue = 255;
                        ptr[0] = ptr[1] = ptr[2] = nValue;
                        ptr += 3;
                    }

                    ptr += remain;
                }

                ptr = (byte*)data.Scan0;

                histogram = new int[256];
                for (int i = 0; i < histogram.Length; i++)
                    histogram[i] = 0;

                for (int i = 0; i < data.Height; i++) {
                    for (int j = 0; j < data.Width; j++) {
                        int mean = ptr[0];

                        histogram[mean]++;
                        ptr += 3;
                    }

                    ptr += remain;
                }

                

            }

            b.UnlockBits(data);
            return b;
        }

        public float[] equalize(int[] histogram, long numPixel) {
            float[] hist = new float[256];

            hist[0] = histogram[0] * histogram.Length / numPixel;
            long prev = histogram[0];
            string str = "";
            str += (int)hist[0] + "\n";

            for (int i = 1; i < hist.Length; i++) {
                prev += histogram[i];
                hist[i] = prev * histogram.Length / numPixel;
                str += (int)hist[i] + "   _" + i + "\t";
            }
                       
            return hist;

        }

    }
}
