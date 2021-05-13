#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Profile.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Image.ImageData;
using NINA.Core.Enum;
using NINA.Image.Interfaces;

namespace NINA.Image {

    public class Thumbnail : BaseINPC {

        public Thumbnail() {
            Grade = "";
        }

        public async Task<IImageData> LoadOriginalImage(IProfileService profileService) {
            try {
                var filePath = GetFilePath();
                if (File.Exists(filePath)) {
                    return await BaseImageData.FromFile(filePath, (int)profileService.ActiveProfile.CameraSettings.BitDepth, IsBayered, profileService.ActiveProfile.CameraSettings.RawConverter);
                } else {
                    Notification.ShowError($"File ${filePath} does not exist");
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }

            return null;
        }

        public void ChangeGrade(string grade) {
            var oldFileName = GetFilePath();
            if (File.Exists(oldFileName)) {
                try {
                    Grade = grade;
                    File.Move(oldFileName, GetFilePath());
                    RaisePropertyChanged(nameof(FilePath));
                    RaisePropertyChanged(nameof(Grade));
                } catch (Exception ex) {
                    Logger.Error(ex);
                    Notification.ShowError(ex.Message);
                }
            }
        }

        private string GetFilePath() {
            var fileName = Path.GetFileNameWithoutExtension(ImagePath.LocalPath);
            var extension = Path.GetExtension(ImagePath.LocalPath);

            if (string.IsNullOrEmpty(Grade)) {
                return ImagePath.LocalPath;
            } else {
                return ImagePath.LocalPath.Replace($"{fileName}{extension}", ($"{Grade}_{fileName}{extension}"));
            }
        }

        public BitmapSource ThumbnailImage { get; set; }

        public double Mean { get; set; }

        public double HFR { get; set; }

        public bool IsBayered { get; set; }

        public string FilePath {
            get => GetFilePath();
        }

        public Uri ImagePath { get; set; }

        public FileTypeEnum FileType { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public string Filter { get; set; }

        public double Duration { get; set; }

        public string Grade { get; private set; }
    }
}