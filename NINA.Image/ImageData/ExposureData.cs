#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Image.ImageData;
using NINA.Core.Utility;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Image.RawConverter;
using NINA.Core.Locale;
using NINA.Image.Interfaces;

namespace NINA.Image.ImageData {

    public abstract class BaseExposureData : IExposureData {
        public int BitDepth { get; private set; }
        public ImageMetaData MetaData { get; private set; }

        protected BaseExposureData(int bitDepth, ImageMetaData metadata) {
            this.BitDepth = bitDepth;
            this.MetaData = metadata;
        }

        public abstract Task<IImageData> ToImageData(IProgress<ApplicationStatus> progress = default, CancellationToken cancelToken = default);
    }

    public class CachedExposureData : BaseExposureData {
        private readonly IImageData imageData;

        public CachedExposureData(IImageData imageData)
            : base(imageData.Properties.BitDepth, imageData.MetaData) {
            this.imageData = imageData;
        }

        public override Task<IImageData> ToImageData(IProgress<ApplicationStatus> progress = default, CancellationToken cancelToken = default) {
            return Task.FromResult(this.imageData);
        }
    }

    public class Flipped2DExposureData : BaseExposureData {
        private readonly Array flipped2DArray;
        public bool IsBayered { get; private set; }

        public Flipped2DExposureData(
            Array flipped2DArray,
            int bitDepth,
            bool isBayered,
            ImageMetaData metaData)
            : base(bitDepth, metaData) {
            if (flipped2DArray.Rank > 2) { throw new NotSupportedException(); }
            this.flipped2DArray = flipped2DArray;
            this.IsBayered = isBayered;
        }

        public override async Task<IImageData> ToImageData(IProgress<ApplicationStatus> progress = default, CancellationToken cancelToken = default) {
            try {
                progress?.Report(new ApplicationStatus { Status = Loc.Instance["LblPrepareExposure"] });
                var flatArray = await Task.Run(() => FlipAndConvert2d(this.flipped2DArray), cancelToken);
                return new BaseImageData(
                    imageArray: new ImageArray(flatArray),
                    width: this.flipped2DArray.GetLength(0),
                    height: this.flipped2DArray.GetLength(1),
                    bitDepth: this.BitDepth,
                    isBayered: this.IsBayered,
                    metaData: this.MetaData);
            } finally {
                progress?.Report(new ApplicationStatus { Status = string.Empty });
            }
        }

        private static ushort[] FlipAndConvert2d(Array input) {
            using (MyStopWatch.Measure("FlipAndConvert2d")) {
                Int32[,] arr = (Int32[,])input;
                int width = arr.GetLength(0);
                int height = arr.GetLength(1);

                int length = width * height;
                ushort[] flatArray = new ushort[length];
                ushort value;

                unsafe {
                    fixed (Int32* ptr = arr) {
                        int idx = 0, row = 0;
                        for (int i = 0; i < length; i++) {
                            value = (ushort)ptr[i];

                            idx = ((i % height) * width) + row;
                            if ((i % (height)) == (height - 1)) row++;

                            ushort b = value;
                            flatArray[idx] = b;
                        }
                    }
                }
                return flatArray;
            }
        }
    }

    public class RAWExposureData : BaseExposureData {
        private readonly byte[] rawBytes;
        private readonly IRawConverter rawConverter;
        private readonly string rawType;

        public RAWExposureData(
            IRawConverter rawConverter,
            byte[] rawBytes,
            string rawType,
            int bitDepth,
            ImageMetaData metaData)
            : base(bitDepth, metaData) {
            this.rawConverter = rawConverter;
            this.rawBytes = rawBytes;
            this.rawType = rawType;
        }

        public override async Task<IImageData> ToImageData(IProgress<ApplicationStatus> progress = default, CancellationToken cancelToken = default) {
            try {
                progress?.Report(new ApplicationStatus { Status = Loc.Instance["LblPrepareExposure"] });
                using (var memoryStream = new System.IO.MemoryStream(this.rawBytes)) {
                    var data = await this.rawConverter.Convert(
                        s: memoryStream,
                        rawType: this.rawType,
                        bitDepth: this.BitDepth,
                        metaData: this.MetaData,
                        token: cancelToken);
                    return data;
                }
            } finally {
                progress?.Report(new ApplicationStatus { Status = string.Empty });
            }
        }
    }
}