#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;
using NINA.Utility.Notification;
using NINA.Profile;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using NINA.Model.ImageData;
using NINA.Core.Enum;

namespace NINA.ViewModel {

    public class Thumbnail : BaseINPC {

        public Thumbnail() {
        }

        public async Task<IImageData> LoadOriginalImage(IProfileService profileService) {
            try {
                if (File.Exists(ImagePath.LocalPath)) {
                    return await ImageData.FromFile(ImagePath.LocalPath, (int)profileService.ActiveProfile.CameraSettings.BitDepth, IsBayered, profileService.ActiveProfile.CameraSettings.RawConverter);
                } else {
                    Notification.ShowError($"File ${ImagePath.LocalPath} does not exist");
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }

            return null;
        }

        public BitmapSource ThumbnailImage { get; set; }

        public double Mean { get; set; }

        public double HFR { get; set; }

        public bool IsBayered { get; set; }

        public Uri ImagePath { get; set; }

        public FileTypeEnum FileType { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;

        public string Filter { get; set; }

        public double Duration { get; set; }
    }
}