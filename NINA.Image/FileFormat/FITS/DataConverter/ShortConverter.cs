﻿#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;

namespace NINA.Image.FileFormat.FITS.DataConverter {

    public class ShortConverter : IDataConverter {

        public ushort[] Convert(Array[] rawData, int width, int height) {
            ushort[] pixels = new ushort[width * height];
            var i = 0;
            foreach (var row in rawData) {
                foreach (object val in row) {
                    pixels[i++] = (ushort)((((short)val - short.MinValue) / ((double)short.MaxValue - short.MinValue)) * ushort.MaxValue);
                }
            }
            return pixels;
        }
    }
}