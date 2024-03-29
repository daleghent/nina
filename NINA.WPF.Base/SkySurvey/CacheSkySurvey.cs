#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using NINA.Core.Utility;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml.Linq;

namespace NINA.WPF.Base.SkySurvey {

    public class CacheSkySurvey {
        public readonly string framingAssistantCachePath;
        private string framingAssistantCachInfo;

        public CacheSkySurvey(string framingAssistantCachePath) {
            this.framingAssistantCachePath = framingAssistantCachePath;
            this.framingAssistantCachInfo = Path.Combine(framingAssistantCachePath, "CacheInfo.xml");
            Initialize();
        }

        private void Initialize() {
            if (!Directory.Exists(framingAssistantCachePath)) {
                Directory.CreateDirectory(framingAssistantCachePath);
            }

            if (!File.Exists(framingAssistantCachInfo)) {
                XElement info = new XElement("ImageCacheInfo");
                info.Save(framingAssistantCachInfo);
                Cache = info;
                return;
            } else {
                Cache = XElement.Load(framingAssistantCachInfo);

                /* Ensure Backwards compatibility with v1.6.0.2 */
                var elements = Cache.Elements("Image").Where(x => x.Attribute("Id") == null);
                foreach (var element in elements) {
                    element.Add(new XAttribute("Id", Guid.NewGuid()));
                }
                elements = Cache.Elements("Image").Where(x => x.Attribute("Source") == null);
                foreach (var element in elements) {
                    if (element.Attribute("Rotation").Value != "0") {
                        element.Add(new XAttribute("Source", nameof(FileSkySurvey)));
                    } else {
                        element.Add(new XAttribute("Source", nameof(NASASkySurvey)));
                    }
                }
            }
        }

        public void Clear() {
            System.IO.DirectoryInfo di = new DirectoryInfo(framingAssistantCachePath);

            foreach (FileInfo file in di.GetFiles()) {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories()) {
                dir.Delete(true);
            }
            Initialize();
        }


        public void DeleteFromCache(XElement element) {
            if(Cache != null && element != null) {

                var fileNameAttribute = element.Attribute("FileName");

                if(fileNameAttribute != null) {
                    var fullFileName = Path.Combine(framingAssistantCachePath, fileNameAttribute.Value);                 
                    var thumbnailBig = CacheImage.GetImagePathForThumbnail(fullFileName, CacheImage.BigThumbnailSize);
                    var thumbnailMedium = CacheImage.GetImagePathForThumbnail(fullFileName, CacheImage.MediumThumbnailSize);
                    var thumbnailSmall = CacheImage.GetImagePathForThumbnail(fullFileName, CacheImage.SmallThumbnailSize);

                    if(File.Exists(fullFileName)) {
                        try { File.Delete(fullFileName); } catch { }
                    }
                    if (File.Exists(thumbnailBig)) {
                        try { File.Delete(thumbnailBig); } catch { }
                    }
                    if (File.Exists(thumbnailMedium)) {
                        try { File.Delete(thumbnailMedium); } catch { }
                    }
                    if (File.Exists(thumbnailSmall)) {
                        try { File.Delete(thumbnailSmall); } catch { }
                    }

                }

                element.Remove();
                Cache.Save(framingAssistantCachInfo);
            }
        }

        public XElement SaveImageToCache(SkySurveyImage skySurveyImage) {
            try {
                var element =
                    Cache
                    .Elements("Image")
                    .Where(
                        x => x.Attribute("Id").Value == skySurveyImage.Id.ToString()
                    ).FirstOrDefault();
                if (element == null) {
                    element =
                    Cache
                    .Elements("Image")
                    .Where(
                        x =>
                            x.Attribute("RA").Value == skySurveyImage.Coordinates.RA.ToString("R", CultureInfo.InvariantCulture)
                            && x.Attribute("Dec").Value == skySurveyImage.Coordinates.Dec.ToString("R", CultureInfo.InvariantCulture)
                            && x.Attribute("FoVW").Value == skySurveyImage.FoVWidth.ToString("R", CultureInfo.InvariantCulture)
                            && x.Attribute("Source").Value == skySurveyImage.Source
                    ).FirstOrDefault();

                    if (element == null) {
                        if (!Directory.Exists(framingAssistantCachePath)) {
                            Directory.CreateDirectory(framingAssistantCachePath);
                        }

                        var sanitizedName = CoreUtil.ReplaceAllInvalidFilenameChars(skySurveyImage.Name);
                        var originalImgFilePath = Path.Combine(framingAssistantCachePath, sanitizedName + ".jpg");


                        originalImgFilePath = RestoreNameFromUniqueBracket(originalImgFilePath);
                        
                        var imgFilePath = CoreUtil.GetUniqueFilePath(originalImgFilePath);
                        var name = Path.GetFileNameWithoutExtension(originalImgFilePath);

                        using (var fileStream = new FileStream(imgFilePath, FileMode.Create)) {
                            var encoder = new JpegBitmapEncoder();
                            encoder.QualityLevel = 70;
                            encoder.Frames.Add(BitmapFrame.Create(skySurveyImage.Image));
                            encoder.Save(fileStream);
                        }

                        SaveThumbnail(skySurveyImage.Image, CacheImage.BigThumbnailSize, imgFilePath);
                        SaveThumbnail(skySurveyImage.Image, CacheImage.MediumThumbnailSize, imgFilePath);
                        SaveThumbnail(skySurveyImage.Image, CacheImage.SmallThumbnailSize, imgFilePath);

                        XElement xml = new XElement("Image",
                            new XAttribute("Id", skySurveyImage.Id),
                            new XAttribute("RA", skySurveyImage.Coordinates.RA.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("Dec", skySurveyImage.Coordinates.Dec.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("Rotation", skySurveyImage.Rotation),
                            new XAttribute("FoVW", skySurveyImage.FoVWidth.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("FoVH", skySurveyImage.FoVHeight.ToString("R", CultureInfo.InvariantCulture)),
                            new XAttribute("FileName", Path.GetFileName(imgFilePath)),
                            new XAttribute("Source", skySurveyImage.Source),
                            new XAttribute("Name", name)
                        );

                        Cache.Add(xml);
                        Cache.Save(framingAssistantCachInfo);
                        return xml;
                    }
                }
                return element;
            } catch (Exception ex) {
                Logger.Error(ex);
                throw;
            }
        }
        
        private void SaveThumbnail(BitmapSource image, int size, string path) {
            var rescaledImage = new TransformedBitmap(image, new ScaleTransform((double)size / image.PixelWidth, (double)size / image.PixelHeight));
            var adjustedPath = CacheImage.GetImagePathForThumbnail(path, size);

            using (var fileStream = new FileStream(adjustedPath, FileMode.Create)) {
                var encoder = new JpegBitmapEncoder();
                encoder.QualityLevel = 70;
                encoder.Frames.Add(BitmapFrame.Create(rescaledImage));
                encoder.Save(fileStream);
            }
        }
        
        private string RestoreNameFromUniqueBracket(string originalImgFilePath) {
            var filename = Path.GetFileName(originalImgFilePath);

            var regex = new Regex(@"^(.*?)(?:\((\d+)\))?\.(.+)$");
            var matches = regex.Match(filename);
            var path = originalImgFilePath;
            if (matches.Groups.Count == 4 && !string.IsNullOrEmpty(matches.Groups[2].Value)) {
                var originalFileName = matches.Groups[1].Value + "." + matches.Groups[3].Value;

                path = Path.Combine(framingAssistantCachePath, originalFileName);
            }
            return path;
        }

        public XElement Cache { get; private set; }

        public object CulutureInfo { get; private set; }

        /// <summary>
        /// Restores an image from cache based on given parameter
        /// </summary>
        /// <param name="source">The selected source</param>
        /// <param name="ra">Right Ascension</param>
        /// <param name="dec">Declination</param>
        /// <param name="rotation">Rotation of image</param>
        /// <param name="fov">Field of View in Arcminutes</param>
        /// <returns></returns>
        public Task<SkySurveyImage> GetImage(string source, double ra, double dec, double rotation, double fov) {
            return Task.Run(() => {
                var element =
                    Cache
                    .Elements("Image")
                    .Where(x => x.Attribute("Source").Value == source)
                    .Where(x => x.Attribute("RA").Value == ra.ToString("R", CultureInfo.InvariantCulture))
                    .Where(x => x.Attribute("Dec").Value == dec.ToString("R", CultureInfo.InvariantCulture))
                    .Where(x => x.Attribute("Rotation").Value == rotation.ToString(CultureInfo.InvariantCulture))
                    .Where(x => x.Attribute("FoVW").Value == fov.ToString("R", CultureInfo.InvariantCulture))
                    .FirstOrDefault();

                if (element != null) {
                    return Load(element);
                }

                return null;
            });
        }

        public Task<SkySurveyImage> GetImage(Guid id) {
            return Task.Run(() => {
                var element =
                    Cache
                    .Elements("Image")
                    .Where(
                        x => x.Attribute("Id").Value == id.ToString()
                    ).FirstOrDefault();
                if (element != null) {
                    return Load(element);
                } else {
                    return null;
                }
            });
        }

        private SkySurveyImage Load(XElement element) {
            var img = LoadJpg(element.Attribute("FileName").Value);
            Guid id = Guid.Parse(element.Attribute("Id").Value);
            var fovW = double.Parse(element.Attribute("FoVW").Value, CultureInfo.InvariantCulture);
            var fovH = double.Parse(element.Attribute("FoVH").Value, CultureInfo.InvariantCulture);
            var rotation = double.Parse(element.Attribute("Rotation").Value, CultureInfo.InvariantCulture);
            var ra = double.Parse(element.Attribute("RA").Value, CultureInfo.InvariantCulture);
            var dec = double.Parse(element.Attribute("Dec").Value, CultureInfo.InvariantCulture);
            var name = element.Attribute("Name").Value;
            var source = element.Attribute("Source")?.Value ?? string.Empty;

            img.Freeze();
            return new SkySurveyImage() {
                Id = id,
                Image = img,
                FoVHeight = fovH,
                FoVWidth = fovW,
                Coordinates = new Coordinates(ra, dec, Epoch.J2000, Coordinates.RAType.Hours),
                Name = name,
                Rotation = rotation
            };
        }

        private BitmapSource LoadJpg(string filename) {
            if (!Path.IsPathRooted(filename)) {
                filename = Path.Combine(framingAssistantCachePath, filename);
            }

            JpegBitmapDecoder JpgDec = new JpegBitmapDecoder(new Uri(filename), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);

            return ConvertTo96Dpi(JpgDec.Frames[0]);
        }

        private BitmapSource ConvertTo96Dpi(BitmapSource image) {
            if (image.DpiX != 96) {
                byte[] pixelData = new byte[image.PixelWidth * 4 * image.PixelHeight];
                image.CopyPixels(pixelData, image.PixelWidth * 4, 0);

                return BitmapSource.Create(image.PixelWidth, image.PixelHeight, 96, 96, image.Format, null, pixelData,
                    image.PixelWidth * 4);
            }

            return image;
        }
    }
}