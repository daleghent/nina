#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using System;

namespace NINA.Model.MyGuider.PHD2 {

    public abstract class Phd2Method {

        [JsonProperty(PropertyName = "id")]
        public abstract string Id { get; }

        [JsonProperty(PropertyName = "method")]
        public abstract string Method { get; }
    }

    public abstract class Phd2Method<T> : Phd2Method {

        [JsonProperty(PropertyName = "params")]
        public T Parameters { get; set; }
    }

    public class Phd2Guide : Phd2Method<Phd2GuideParameter> {
        public override string Id { get => PHD2EventId.GUIDE; }
        public override string Method { get => "guide"; }
    }

    public class Phd2GuideParameter {

        [JsonProperty(PropertyName = "settle")]
        public Phd2Settle Settle { get; set; }

        [JsonProperty(PropertyName = "recalibrate")]
        public bool Recalibrate { get; set; }

        [JsonProperty(PropertyName = "roi")]
        public int[] Roi { get; set; }
    }

    public class Phd2Dither : Phd2Method<Phd2DitherParameter> {
        public override string Id { get => PHD2EventId.DITHER; }
        public override string Method { get => "dither"; }
    }

    public class Phd2DitherParameter {

        [JsonProperty(PropertyName = "amount")]
        public double Amount { get; set; }

        [JsonProperty(PropertyName = "raOnly")]
        public bool RaOnly { get; set; }

        [JsonProperty(PropertyName = "settle")]
        public Phd2Settle Settle { get; set; }
    }

    public class Phd2Settle {

        [JsonProperty(PropertyName = "pixels")]
        public double Pixels { get; set; }

        [JsonProperty(PropertyName = "time")]
        public int Time { get; set; }

        [JsonProperty(PropertyName = "timeout")]
        public int Timeout { get; set; }
    }

    public class Phd2FindStar : Phd2Method<Phd2FindStarParameter> {
        public override string Id { get => PHD2EventId.AUTO_SELECT_STAR; }
        public override string Method { get => "find_star"; }
    }

    public class Phd2FindStarParameter {

        [JsonProperty(PropertyName = "roi")]
        public int[] Roi { get; set; }
    }

    public class Phd2Loop : Phd2Method {
        public override string Id { get => PHD2EventId.LOOP; }
        public override string Method { get => "loop"; }
    }

    public class Phd2StopCapture : Phd2Method {
        public override string Id { get => PHD2EventId.STOP_CAPTURE; }
        public override string Method { get => "stop_capture"; }
    }

    public class Phd2GetStarImage : Phd2Method {
        public override string Id { get => PHD2EventId.GET_STAR_IMAGE; }
        public override string Method { get => "get_star_image"; }
    }

    public class Phd2GetPixelScale : Phd2Method {
        public override string Id { get => PHD2EventId.GET_PIXEL_SCALE; }
        public override string Method { get => "get_pixel_scale"; }
    }

    public class Phd2GetExposure : Phd2Method {
        public override string Id { get => PHD2EventId.GET_EXPOSURE; }
        public override string Method { get => "get_exposure"; }
    }

    public class Phd2GetAppState : Phd2Method {
        public override string Id { get => PHD2EventId.GET_APP_STATE; }
        public override string Method { get => "get_app_state"; }
    }

    public class Phd2Pause : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.PAUSE; }
        public override string Method { get => "set_paused"; }
    }

    public class Phd2SetConnected : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.SET_CONNECTED; }
        public override string Method { get => "set_connected"; }
    }

    public class Phd2ClearCalibration : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.CLEAR_CALIBRATION; }
        public override string Method { get => "clear_calibration"; }
    }

    internal class PHD2EventId {
        public const string LOOP = "1";
        public const string AUTO_SELECT_STAR = "2";
        public const string GUIDE = "3";
        public const string CLEAR_CALIBRATION = "4";
        public const string DITHER = "5";
        public const string STOP_CAPTURE = "6";
        public const string PAUSE = "10";
        public const string SET_CONNECTED = "20";

        public const string GET_PIXEL_SCALE = "96";
        public const string GET_STAR_IMAGE = "97";
        public const string GET_EXPOSURE = "98";
        public const string GET_APP_STATE = "99";
    }
}