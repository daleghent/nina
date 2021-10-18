using Accord.Imaging.Filters;
using System;
using System.Drawing;

namespace NINA.Image.ImageAnalysis {
    public static class DetectionUtility {
        public static Rectangle GetCropRectangle(Bitmap image, double cropRatio) {
            int xcoord = (int)Math.Floor((image.Width - image.Width * cropRatio) / 2d);
            int ycoord = (int)Math.Floor((image.Height - image.Height * cropRatio) / 2d);
            int width = (int)Math.Floor(image.Width * cropRatio);
            int height = (int)Math.Floor(image.Height * cropRatio);
            return new Rectangle(xcoord, ycoord, width, height);
        }

        public static double LaplacianOfGaussianFunction(double x, double y, double sigma) {
            double result = -1 * 1 / (Math.PI * Math.Pow(sigma, 4)) * (1 - ((x * x + y * y) / (2 * sigma * sigma))) * Math.Exp(-1 * (x * x + y * y) / (2 * sigma * sigma)) * 600;
            return result;
        }

        public static int[,] LaplacianOfGaussianKernel(int size, double sigma) {
            int[,] LoGKernel = new int[size, size];
            int halfsize = size / 2;
            int sumKernel = 0;
            for (int x = -halfsize; x < halfsize + 1; x++) {
                for (int y = -halfsize; y < halfsize + 1; y++) {
                    int value = (int)Math.Round(LaplacianOfGaussianFunction(x, y, sigma));
                    LoGKernel[x + halfsize, y + halfsize] = value;
                    sumKernel += value;
                }
            }
            LoGKernel[halfsize, halfsize] = LoGKernel[halfsize, halfsize] - sumKernel;
            return LoGKernel;
        }

        public static Bitmap ResizeForDetection(Bitmap image, int maxWidth, double resizeFactor) {
            if (image.Width > maxWidth) {
                var bmp = new ResizeBicubic((int)Math.Floor(image.Width * resizeFactor), (int)Math.Floor(image.Height * resizeFactor)).Apply(image);
                image.Dispose();
                return bmp;
            }
            return image;
        }

        public static bool InROI(Size imageSize, Rectangle blob, double outerCropRatio = 1.0, double innerCropRatio = 1.0) {
            if (outerCropRatio == 1 && (blob.X + blob.Width / 2 > (1 - innerCropRatio) * imageSize.Width / 2
                && blob.X + blob.Width / 2 < imageSize.Width * (1 - (1 - innerCropRatio) / 2)
                && blob.Y + blob.Height / 2 > (1 - innerCropRatio) * imageSize.Height / 2
                && blob.Y + blob.Height / 2 < imageSize.Height * (1 - (1 - innerCropRatio) / 2))) {
                return true;
            }
            if (outerCropRatio < 1 && (blob.X + blob.Width / 2 < (1 - innerCropRatio) * imageSize.Width / 2
                || blob.X + blob.Width / 2 > imageSize.Width * (1 - (1 - innerCropRatio) / 2)
                || blob.Y + blob.Height / 2 < (1 - innerCropRatio) * imageSize.Height / 2
                || blob.Y + blob.Height / 2 > imageSize.Height * (1 - (1 - innerCropRatio) / 2)) &&
                (blob.X + blob.Width / 2 > (1 - outerCropRatio) * imageSize.Width / 2
                && blob.X + blob.Width / 2 < imageSize.Width * (1 - (1 - outerCropRatio) / 2)
                && blob.Y + blob.Height / 2 > (1 - outerCropRatio) * imageSize.Height / 2
                && blob.Y + blob.Height / 2 < imageSize.Height * (1 - (1 - outerCropRatio) / 2))) {
                return true;
            }
            return false;
        }
    }
}
