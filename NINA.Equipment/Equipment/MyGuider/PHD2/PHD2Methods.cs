#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using Newtonsoft.Json;
using System;

namespace NINA.Equipment.Equipment.MyGuider.PHD2 {

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

    public class Phd2GetCameraFrameSize : Phd2Method {
        public override string Id { get => PHD2EventId.GET_CAMERA_FRAME_SIZE; }
        public override string Method { get => "get_camera_frame_size"; }
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
        public override string Id { get => PHD2EventId.SET_PAUSED; }
        public override string Method { get => "set_paused"; }
    }

    public class Phd2GetConnected : Phd2Method {
        public override string Id { get => PHD2EventId.GET_CONNECTED; }
        public override string Method { get => "get_connected"; }
    }

    public class Phd2SetConnected : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.SET_CONNECTED; }
        public override string Method { get => "set_connected"; }
    }

    public class Phd2ClearCalibration : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.CLEAR_CALIBRATION; }
        public override string Method { get => "clear_calibration"; }
    }

    public class Phd2GetProfile : Phd2Method {
        public override string Id { get => PHD2EventId.GET_PROFILE; }
        public override string Method { get => "get_profile"; }
    }

    public class Phd2GetProfiles : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.GET_PROFILES; }
        public override string Method { get => "get_profiles"; }
    }

    public class Phd2GetLockPosition : Phd2Method {
        public override string Id { get => PHD2EventId.GET_LOCK_POSITION; }
        public override string Method { get => "get_lock_position"; }
    }

    public class Phd2SetProfile : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.SET_PROFILE; }
        public override string Method { get => "set_profile"; }
    }

    public class Phd2GetAlgoParamNames : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.GET_ALGO_PARAM_NAMES; }
        public override string Method { get => "get_algo_param_names"; }
    }

    public class Phd2GetAlgoParam : Phd2Method {
        public override string Id { get => PHD2EventId.GET_ALGO_PARAM; }
        public override string Method { get => "get_algo_param"; }
    }

    public class Phd2GetCalibrated : Phd2Method {
        public override string Id { get => PHD2EventId.GET_CALIBRATED; }
        public override string Method { get => "get_calibrated"; }
    }

    public class Phd2GetCalibrationData : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.GET_CALIBRATION_DATA; }
        public override string Method { get => "get_calibration_data"; }
    }

    public class Phd2GetCoolerStatus : Phd2Method {
        public override string Id { get => PHD2EventId.GET_COOLER_STATUS; }
        public override string Method { get => "get_cooler_status"; }
    }

    public class Phd2GetCurrentEquipment : Phd2Method {
        public override string Id { get => PHD2EventId.GET_CURRENT_EQUIPMENT; }
        public override string Method { get => "get_current_equipment"; }
    }

    public class Phd2GetDecGuideMode : Phd2Method {
        public override string Id { get => PHD2EventId.GET_DEC_GUIDE_MODE; }
        public override string Method { get => "get_dec_guide_mode"; }
    }

    public class Phd2GetExposureDurations : Phd2Method {
        public override string Id { get => PHD2EventId.GET_EXPOSURE_DURATIONS; }
        public override string Method { get => "get_exposure_durations"; }
    }

    public class Phd2GetGuideOutputEnabled : Phd2Method {
        public override string Id { get => PHD2EventId.GET_GUIDE_OUTPUT_ENABLED; }
        public override string Method { get => "get_guide_output_enabled"; }
    }

    public class Phd2GetLockShiftEnabled : Phd2Method {
        public override string Id { get => PHD2EventId.GET_LOCK_SHIFT_ENABLED; }
        public override string Method { get => "get_lock_shift_enabled"; }
    }

    public class Phd2GetLockShiftParams : Phd2Method {
        public override string Id { get => PHD2EventId.GET_LOCK_SHIFT_PARAMS; }
        public override string Method { get => "get_lock_shift_params"; }
    }

    public class Phd2GetPaused : Phd2Method {
        public override string Id { get => PHD2EventId.GET_PAUSED; }
        public override string Method { get => "get_paused"; }
    }

    public class Phd2GetSearchRegion : Phd2Method {
        public override string Id { get => PHD2EventId.GET_SEARCH_REGION; }
        public override string Method { get => "get_search_region"; }
    }

    public class Phd2GetCCDTemperature : Phd2Method {
        public override string Id { get => PHD2EventId.GET_CCD_TEMPERATURE; }
        public override string Method { get => "get_ccd_temperature"; }
    }

    public class Phd2GetUseSubFrames : Phd2Method {
        public override string Id { get => PHD2EventId.GET_USE_SUBFRAMES; }
        public override string Method { get => "get_use_subframes"; }
    }

    public class Phd2SetAlgoParam : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.SET_ALGO_PARAM; }
        public override string Method { get => "set_algo_param"; }
    }

    public class Phd2SetDecGuideMode : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.SET_DEC_GUIDE_MODE; }
        public override string Method { get => "set_dec_guide_mode"; }
    }

    public class Phd2SetExposure : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.SET_EXPOSURE; }
        public override string Method { get => "set_exposure"; }
    }

    public class Phd2SetGuideOutputEnabled : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.SET_GUIDE_OUTPUT_ENABLED; }
        public override string Method { get => "set_guide_output_enabled"; }
    }

    public class Phd2SetLockPosition : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.SET_LOCK_POSITION; }
        public override string Method { get => "set_lock_position"; }
    }

    public class Phd2SetLockShiftEnabled : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.SET_LOCK_SHIFT_ENABLED; }
        public override string Method { get => "set_lock_shift_enabled"; }
    }

    public class Phd2SetLockShiftParams : Phd2Method<Phd2SetLockShiftParamsParameter> {
        public override string Id { get => PHD2EventId.SET_LOCK_SHIFT_PARAMS; }
        public override string Method { get => "set_lock_shift_params"; }
    }

    public class Phd2SetLockShiftParamsParameter {

        [JsonProperty(PropertyName = "rate")]
        public float Rate { get; set; }

        [JsonProperty(PropertyName = "units")]
        public string Units { get; set; }

        [JsonProperty(PropertyName = "axes")]
        public string Axes { get; set; }
    }

    public class Phd2CaptureSingleFrame : Phd2Method<Phd2CaptureSingleFrameParameter> {
        public override string Id { get => PHD2EventId.CAPTURE_SINGLE_FRAME; }
        public override string Method { get => "capture_single_frame"; }
    }

    public class Phd2CaptureSingleFrameParameter {

        [JsonProperty(PropertyName = "exposure")]
        public int Exposure { get; set; }

        [JsonProperty(PropertyName = "subframe")]
        public int[] Subframe { get; set; }
    }

    public class Phd2FlipCalibration : Phd2Method {
        public override string Id { get => PHD2EventId.FLIP_CALIBRATION; }
        public override string Method { get => "flip_calibration"; }
    }

    public class Phd2GuidePulse : Phd2Method<Array> {
        public override string Id { get => PHD2EventId.GUIDE_PULSE; }
        public override string Method { get => "guide_pulse"; }
    }

    public class Phd2SaveImage : Phd2Method {
        public override string Id { get => PHD2EventId.SAVE_IMAGE; }
        public override string Method { get => "save_image"; }
    }

    public class Phd2Shutdown : Phd2Method {
        public override string Id { get => PHD2EventId.SHUTDOWN; }
        public override string Method { get => "shutdown"; }
    }

    internal class PHD2EventId {
        public const string LOOP = "1";
        public const string AUTO_SELECT_STAR = "2";
        public const string GUIDE = "3";
        public const string CLEAR_CALIBRATION = "4";
        public const string DITHER = "5";
        public const string STOP_CAPTURE = "6";
        public const string SET_PAUSED = "10";
        public const string SET_CONNECTED = "20";
        public const string SET_PROFILE = "30";

        public const string SET_ALGO_PARAM = "40";
        public const string SET_DEC_GUIDE_MODE = "41";
        public const string SET_EXPOSURE = "42";
        public const string SET_GUIDE_OUTPUT_ENABLED = "43";
        public const string SET_LOCK_POSITION = "44";
        public const string SET_LOCK_SHIFT_ENABLED = "45";
        public const string SET_LOCK_SHIFT_PARAMS = "46";

        public const string CAPTURE_SINGLE_FRAME = "50";
        public const string FLIP_CALIBRATION = "51";
        public const string GUIDE_PULSE = "52";
        public const string SAVE_IMAGE = "53";
        public const string SHUTDOWN = "54";

        public const string GET_LOCK_POSITION = "94";
        public const string GET_CAMERA_FRAME_SIZE = "95";
        public const string GET_PIXEL_SCALE = "96";
        public const string GET_STAR_IMAGE = "97";
        public const string GET_EXPOSURE = "98";
        public const string GET_APP_STATE = "99";
        public const string GET_PROFILE = "100";
        public const string GET_PROFILES = "101";
        public const string GET_CONNECTED = "102";

        public const string GET_ALGO_PARAM_NAMES = "102";
        public const string GET_ALGO_PARAM = "104";
        public const string GET_CALIBRATED = "105";
        public const string GET_CALIBRATION_DATA = "106";
        public const string GET_COOLER_STATUS = "107";
        public const string GET_CURRENT_EQUIPMENT = "108";
        public const string GET_DEC_GUIDE_MODE = "109";
        public const string GET_EXPOSURE_DURATIONS = "110";
        public const string GET_GUIDE_OUTPUT_ENABLED = "111";
        public const string GET_LOCK_SHIFT_ENABLED = "112";
        public const string GET_LOCK_SHIFT_PARAMS = "113";
        public const string GET_PAUSED = "114";
        public const string GET_SEARCH_REGION = "115";
        public const string GET_CCD_TEMPERATURE = "116";
        public const string GET_USE_SUBFRAMES = "117";
    }

    public class PhdMethodResponse {
        public string jsonrpc;
        public PhdError error;
        public int id;
    }

    public class GenericPhdMethodResponse : PhdMethodResponse {
        public object result;
    }

    public class BooleanPhdMethodResponse : PhdMethodResponse {
        public bool result;
    }

    public class GetCameraFrameSizeResponse : PhdMethodResponse {
        public int[] result;
    }

    public class PhdImageResult {
        public int frame;
        public int width;
        public int height;
        public double[] star_pos;
        public string pixels;
    }

    public class Phd2ProfileResponse {
        public int id;
        public string name { get; set; }
    }

    public class GetProfileResponse : PhdMethodResponse {
        public Phd2ProfileResponse result;
    }

    public class GetProfilesResponse : PhdMethodResponse {
        public Phd2ProfileResponse[] result;
    }

    public class GetLockPositionResponse : PhdMethodResponse {
        public int[] result;
    }

    public class PhdError {
        public int code;
        public string message;
    }
}