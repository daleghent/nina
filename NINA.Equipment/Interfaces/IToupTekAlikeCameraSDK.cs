#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Runtime.InteropServices;

namespace NINA.Equipment.Interfaces {

    public delegate void ToupTekAlikeCallback(ToupTekAlikeEvent tEvent);

    public interface IToupTekAlikeCameraSDK {
        string Category { get; }
        uint MaxSpeed { get; }
        bool MonoMode { get; }

        void get_Temperature(out short temp);

        void get_Option(ToupTekAlikeOption option, out int target);

        bool put_Option(ToupTekAlikeOption option, int v);

        void get_ExpTimeRange(out uint min, out uint max, out uint def);

        void get_Speed(out ushort speed);

        bool put_Speed(ushort value);

        bool get_ExpoAGain(out ushort gain);

        bool put_AutoExpoEnable(bool v);

        void get_Size(out int width, out int height);

        void get_ExpoAGainRange(out ushort min, out ushort max, out ushort def);

        bool put_ExpoAGain(ushort value);

        bool StartPullModeWithCallback(ToupTekAlikeCallback toupTekAlikeCallback);

        bool get_RawFormat(out uint fourCC, out uint bitDepth);

        bool PullImageV2(ushort[] data, int bitDepth, out ToupTekAlikeFrameInfo info);

        void Close();

        bool put_ExpoTime(uint µsTime);

        bool Trigger(ushort v);

        IToupTekAlikeCameraSDK Open(string id);

        string Version();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ToupTekAlikeFrameInfo {
        public uint width;
        public uint height;
        public uint flag;         /* FRAMEINFO_FLAG_xxxx */
        public uint seq;          /* sequence number */
        public ulong timestamp;    /* microsecond */
    };

    public struct ToupTekAlikeResolution {
        public uint width;
        public uint height;
    };

    public struct ToupTekAlikeModel {
        public string name;         /* model name */
        public ulong flag;          /* sdk_FLAG_xxx, 64 bits */
        public uint maxspeed;       /* number of speed level, same as get_MaxSpeed(), the speed range = [0, maxspeed], closed interval */
        public uint preview;        /* number of preview resolution, same as get_ResolutionNumber() */
        public uint still;          /* number of still resolution, same as get_StillResolutionNumber() */
        public uint maxfanspeed;    /* maximum fan speed */
        public uint ioctrol;        /* number of input/output control */
        public float xpixsz;        /* physical pixel size */
        public float ypixsz;        /* physical pixel size */
        public ToupTekAlikeResolution[] res;
    };

    public struct ToupTekAlikeDeviceInfo {
        public string displayname; /* display name */
        public string id;          /* unique and opaque id of a connected camera */
        public ToupTekAlikeModel model;
    };

    [Flags]
    public enum ToupTekAlikeFlag : ulong {
        FLAG_CMOS = 0x00000001,   /* cmos sensor */
        FLAG_CCD_PROGRESSIVE = 0x00000002,   /* progressive ccd sensor */
        FLAG_CCD_INTERLACED = 0x00000004,   /* interlaced ccd sensor */
        FLAG_ROI_HARDWARE = 0x00000008,   /* support hardware ROI */
        FLAG_MONO = 0x00000010,   /* monochromatic */
        FLAG_BINSKIP_SUPPORTED = 0x00000020,   /* support bin/skip mode */
        FLAG_USB30 = 0x00000040,   /* usb3.0 */
        FLAG_TEC = 0x00000080,   /* Thermoelectric Cooler */
        FLAG_USB30_OVER_USB20 = 0x00000100,   /* usb3.0 camera connected to usb2.0 port */
        FLAG_ST4 = 0x00000200,   /* ST4 */
        FLAG_GETTEMPERATURE = 0x00000400,   /* support to get the temperature of the sensor */
        FLAG_RAW10 = 0x00001000,   /* pixel format, RAW 10bits */
        FLAG_RAW12 = 0x00002000,   /* pixel format, RAW 12bits */
        FLAG_RAW14 = 0x00004000,   /* pixel format, RAW 14bits */
        FLAG_RAW16 = 0x00008000,   /* pixel format, RAW 16bits */
        FLAG_FAN = 0x00010000,   /* cooling fan */
        FLAG_TEC_ONOFF = 0x00020000,   /* Thermoelectric Cooler can be turn on or off, support to set the target temperature of TEC */
        FLAG_ISP = 0x00040000,   /* ISP (Image Signal Processing) chip */
        FLAG_TRIGGER_SOFTWARE = 0x00080000,   /* support software trigger */
        FLAG_TRIGGER_EXTERNAL = 0x00100000,   /* support external trigger */
        FLAG_TRIGGER_SINGLE = 0x00200000,   /* only support trigger single: one trigger, one image */
        FLAG_BLACKLEVEL = 0x00400000,   /* support set and get the black level */
        FLAG_AUTO_FOCUS = 0x00800000,   /* support auto focus */
        FLAG_BUFFER = 0x01000000,   /* frame buffer */
        FLAG_DDR = 0x02000000,   /* use very large capacity DDR (Double Data Rate SDRAM) for frame buffer */
        FLAG_CG = 0x04000000,   /* support Conversion Gain mode: HCG, LCG */
        FLAG_YUV411 = 0x08000000,   /* pixel format, yuv411 */
        FLAG_VUYY = 0x10000000,   /* pixel format, yuv422, VUYY */
        FLAG_YUV444 = 0x20000000,   /* pixel format, yuv444 */
        FLAG_RGB888 = 0x40000000,   /* pixel format, RGB888 */

        [Obsolete("Use FLAG_RAW10")]
        FLAG_BITDEPTH10 = FLAG_RAW10,   /* obsolete, same as FLAG_RAW10 */

        [Obsolete("Use FLAG_RAW12")]
        FLAG_BITDEPTH12 = FLAG_RAW12,   /* obsolete, same as FLAG_RAW12 */

        [Obsolete("Use FLAG_RAW14")]
        FLAG_BITDEPTH14 = FLAG_RAW14,   /* obsolete, same as FLAG_RAW14 */

        [Obsolete("Use FLAG_RAW16")]
        FLAG_BITDEPTH16 = FLAG_RAW16,   /* obsolete, same as FLAG_RAW16 */

        FLAG_RAW8 = 0x80000000,   /* pixel format, RAW 8 bits */
        FLAG_GMCY8 = 0x0000000100000000,  /* pixel format, GMCY, 8 bits */
        FLAG_GMCY12 = 0x0000000200000000,  /* pixel format, GMCY, 12 bits */
        FLAG_UYVY = 0x0000000400000000,  /* pixel format, yuv422, UYVY */
        FLAG_CGHDR = 0x0000000800000000,  /* Conversion Gain: HCG, LCG, HDR */
        FLAG_GLOBALSHUTTER = 0x0000001000000000,  /* global shutter */
        FLAG_FOCUSMOTOR = 0x0000002000000000,  /* support focus motor */
        FLAG_PRECISE_FRAMERATE = 0x0000004000000000,  /* support precise framerate & bandwidth, see OPTION_PRECISE_FRAMERATE & OPTION_BANDWIDTH */
        FLAG_HEAT = 0x0000008000000000,  /* heat to prevent fogging up */
        FLAG_LOW_NOISE = 0x0000010000000000,  /* low noise mode */
        FLAG_LEVELRANGE_HARDWARE = 0x0000020000000000,  /* hardware level range, put(get)_LevelRangeV2 */
        FLAG_EVENT_HARDWARE = 0x0000040000000000   /* hardware event, such as exposure start & stop */
    };

    public enum ToupTekAlikeEvent : uint {
        EVENT_EXPOSURE = 0x0001, /* exposure time or gain changed */
        EVENT_TEMPTINT = 0x0002, /* white balance changed, Temp/Tint mode */
        EVENT_CHROME = 0x0003, /* reversed, do not use it */
        EVENT_IMAGE = 0x0004, /* live image arrived, use sdk_PullImage to get this image */
        EVENT_STILLIMAGE = 0x0005, /* snap (still) frame arrived, use sdk_PullStillImage to get this frame */
        EVENT_WBGAIN = 0x0006, /* white balance changed, RGB Gain mode */
        EVENT_TRIGGERFAIL = 0x0007, /* trigger failed */
        EVENT_BLACK = 0x0008, /* black balance changed */
        EVENT_FFC = 0x0009, /* flat field correction status changed */
        EVENT_DFC = 0x000a, /* dark field correction status changed */
        EVENT_ROI = 0x000b, /* roi changed */
        EVENT_LEVELRANGE = 0x000c, /* level range changed */
        EVENT_ERROR = 0x0080, /* generic error */
        EVENT_DISCONNECTED = 0x0081, /* camera disconnected */
        EVENT_NOFRAMETIMEOUT = 0x0082, /* no frame timeout error */
        EVENT_AFFEEDBACK = 0x0083, /* auto focus feedback information */
        EVENT_AFPOSITION = 0x0084, /* auto focus sensor board positon */
        EVENT_NOPACKETTIMEOUT = 0x0085, /* no packet timeout */
        EVENT_EXPO_START = 0x4000, /* exposure start */
        EVENT_EXPO_STOP = 0x4001, /* exposure stop */
        EVENT_TRIGGER_ALLOW = 0x4002, /* next trigger allow */
        EVENT_FACTORY = 0x8001  /* restore factory settings */
    };

    public enum ToupTekAlikeOption : uint {
        OPTION_NOFRAME_TIMEOUT = 0x01,       /* no frame timeout: 1 = enable; 0 = disable. default: disable */
        OPTION_THREAD_PRIORITY = 0x02,       /* set the priority of the internal thread which grab data from the usb device. iValue: 0 = THREAD_PRIORITY_NORMAL; 1 = THREAD_PRIORITY_ABOVE_NORMAL; 2 = THREAD_PRIORITY_HIGHEST; default: 0; see: msdn SetThreadPriority */
        OPTION_RAW = 0x04,       /* raw data mode, read the sensor "raw" data. This can be set only BEFORE sdk_StartXXX(). 0 = rgb, 1 = raw, default value: 0 */
        OPTION_HISTOGRAM = 0x05,       /* 0 = only one, 1 = continue mode */
        OPTION_BITDEPTH = 0x06,       /* 0 = 8 bits mode, 1 = 16 bits mode */
        OPTION_FAN = 0x07,       /* 0 = turn off the cooling fan, [1, max] = fan speed */
        OPTION_TEC = 0x08,       /* 0 = turn off the thermoelectric cooler, 1 = turn on the thermoelectric cooler */
        OPTION_LINEAR = 0x09,       /* 0 = turn off the builtin linear tone mapping, 1 = turn on the builtin linear tone mapping, default value: 1 */
        OPTION_CURVE = 0x0a,       /* 0 = turn off the builtin curve tone mapping, 1 = turn on the builtin polynomial curve tone mapping, 2 = logarithmic curve tone mapping, default value: 2 */
        OPTION_TRIGGER = 0x0b,       /* 0 = video mode, 1 = software or simulated trigger mode, 2 = external trigger mode, 3 = external + software trigger, default value = 0 */
        OPTION_RGB = 0x0c,       /* 0 => RGB24; 1 => enable RGB48 format when bitdepth > 8; 2 => RGB32; 3 => 8 Bits Gray (only for mono camera); 4 => 16 Bits Gray (only for mono camera when bitdepth > 8) */
        OPTION_COLORMATIX = 0x0d,       /* enable or disable the builtin color matrix, default value: 1 */
        OPTION_WBGAIN = 0x0e,       /* enable or disable the builtin white balance gain, default value: 1 */
        OPTION_TECTARGET = 0x0f,       /* get or set the target temperature of the thermoelectric cooler, in 0.1 degree Celsius. For example, 125 means 12.5 degree Celsius, -35 means -3.5 degree Celsius */
        OPTION_AUTOEXP_POLICY = 0x10,       /* auto exposure policy:
                                                           0: Exposure Only
                                                           1: Exposure Preferred
                                                           2: Gain Only
                                                           3: Gain Preferred
                                                        default value: 1
                                                   */
        OPTION_FRAMERATE = 0x11,       /* limit the frame rate, range=[0, 63], the default value 0 means no limit */
        OPTION_DEMOSAIC = 0x12,       /* demosaic method for both video and still image: BILINEAR = 0, VNG(Variable Number of Gradients interpolation) = 1, PPG(Patterned Pixel Grouping interpolation) = 2, AHD(Adaptive Homogeneity-Directed interpolation) = 3, see https://en.wikipedia.org/wiki/Demosaicing, default value: 0 */
        OPTION_DEMOSAIC_VIDEO = 0x13,       /* demosaic method for video */
        OPTION_DEMOSAIC_STILL = 0x14,       /* demosaic method for still image */
        OPTION_BLACKLEVEL = 0x15,       /* black level */
        OPTION_MULTITHREAD = 0x16,       /* multithread image processing */
        OPTION_BINNING = 0x17,       /* binning, 0x01 (no binning), 0x02 (add, 2*2), 0x03 (add, 3*3), 0x04 (add, 4*4), 0x05 (add, 5*5), 0x06 (add, 6*6), 0x07 (add, 7*7), 0x08 (add, 8*8), 0x82 (average, 2*2), 0x83 (average, 3*3), 0x84 (average, 4*4), 0x85 (average, 5*5), 0x86 (average, 6*6), 0x87 (average, 7*7), 0x88 (average, 8*8). The final image size is rounded down to an even number, such as 640/3 to get 212 */
        OPTION_ROTATE = 0x18,       /* rotate clockwise: 0, 90, 180, 270 */
        OPTION_CG = 0x19,       /* Conversion Gain mode: 0 = LCG, 1 = HCG, 2 = HDR */
        OPTION_PIXEL_FORMAT = 0x1a,       /* pixel format */
        OPTION_FFC = 0x1b,       /* flat field correction
                                                       set:
                                                           0: disable
                                                           1: enable
                                                           -1: reset
                                                           (0xff000000 | n): set the average number to n, [1~255]
                                                       get:
                                                           (val & 0xff): 0 -> disable, 1 -> enable, 2 -> inited
                                                           ((val & 0xff00) >> 8): sequence
                                                           ((val & 0xff0000) >> 8): average number
                                                   */
        OPTION_DDR_DEPTH = 0x1c,       /* the number of the frames that DDR can cache
                                                       1: DDR cache only one frame
                                                       0: Auto:
                                                           ->one for video mode when auto exposure is enabled
                                                           ->full capacity for others
                                                       1: DDR can cache frames to full capacity
                                                   */
        OPTION_DFC = 0x1d,       /* dark field correction
                                                       set:
                                                           0: disable
                                                           1: enable
                                                           -1: reset
                                                           (0xff000000 | n): set the average number to n, [1~255]
                                                       get:
                                                           (val & 0xff): 0 -> disable, 1 -> enable, 2 -> inited
                                                           ((val & 0xff00) >> 8): sequence
                                                           ((val & 0xff0000) >> 8): average number
                                                   */
        OPTION_SHARPENING = 0x1e,       /* Sharpening: (threshold << 24) | (radius << 16) | strength)
                                                       strength: [0, 500], default: 0 (disable)
                                                       radius: [1, 10]
                                                       threshold: [0, 255]
                                                   */
        OPTION_FACTORY = 0x1f,       /* restore the factory settings */
        OPTION_TEC_VOLTAGE = 0x20,       /* get the current TEC voltage in 0.1V, 59 mean 5.9V; readonly */
        OPTION_TEC_VOLTAGE_MAX = 0x21,       /* get the TEC maximum voltage in 0.1V; readonly */
        OPTION_DEVICE_RESET = 0x22,       /* reset usb device, simulate a replug */
        OPTION_UPSIDE_DOWN = 0x23,       /* upsize down:
                                                       1: yes
                                                       0: no
                                                       default: 1 (win), 0 (linux/macos)
                                                   */
        OPTION_AFPOSITION = 0x24,       /* auto focus sensor board positon */
        OPTION_AFMODE = 0x25,       /* auto focus mode (0:manul focus; 1:auto focus; 2:once focus; 3:conjugate calibration) */
        OPTION_AFZONE = 0x26,       /* auto focus zone */
        OPTION_AFFEEDBACK = 0x27,       /* auto focus information feedback; 0:unknown; 1:focused; 2:focusing; 3:defocus; 4:up; 5:down */
        OPTION_TESTPATTERN = 0x28,       /* test pattern:
                                                       0: TestPattern Off
                                                       3: monochrome diagonal stripes
                                                       5: monochrome vertical stripes
                                                       7: monochrome horizontal stripes
                                                       9: chromatic diagonal stripes
                                                   */
        OPTION_AUTOEXP_THRESHOLD = 0x29,       /* threshold of auto exposure, default value: 5, range = [2, 15] */
        OPTION_BYTEORDER = 0x2a,       /* Byte order, BGR or RGB: 0->RGB, 1->BGR, default value: 1(Win), 0(macOS, Linux, Android) */
        OPTION_NOPACKET_TIMEOUT = 0x2b,       /* no packet timeout: 0 = disable, positive value = timeout milliseconds. default: disable */
        OPTION_MAX_PRECISE_FRAMERATE = 0x2c,       /* precise frame rate maximum value in 0.1 fps, such as 115 means 11.5 fps. E_NOTIMPL means not supported */
        OPTION_PRECISE_FRAMERATE = 0x2d,       /* precise frame rate current value in 0.1 fps, range:[1~maximum] */
        OPTION_BANDWIDTH = 0x2e,       /* bandwidth, [1-100]% */
        OPTION_RELOAD = 0x2f,       /* reload the last frame in trigger mode */
        OPTION_CALLBACK_THREAD = 0x30,       /* dedicated thread for callback */
        OPTION_FRAME_DEQUE_LENGTH = 0x31,       /* frame buffer deque length, range: [2, 1024], default: 3 */
        OPTION_MIN_PRECISE_FRAMERATE = 0x32,       /* precise frame rate minimum value in 0.1 fps, such as 15 means 1.5 fps */
        OPTION_SEQUENCER_ONOFF = 0x33,       /* sequencer trigger: on/off */
        OPTION_SEQUENCER_NUMBER = 0x34,       /* sequencer trigger: number, range = [1, 255] */
        OPTION_SEQUENCER_EXPOTIME = 0x01000000, /* sequencer trigger: exposure time, iOption = OPTION_SEQUENCER_EXPOTIME | index, iValue = exposure time
                                                        For example, to set the exposure time of the third group to 50ms, call:
                                                           sdk_put_Option(sdk_OPTION_SEQUENCER_EXPOTIME | 3, 50000)
                                                   */
        OPTION_SEQUENCER_EXPOGAIN = 0x02000000, /* sequencer trigger: exposure gain, iOption = OPTION_SEQUENCER_EXPOGAIN | index, iValue = gain */
        OPTION_DENOISE = 0x35,       /* denoise, strength range: [0, 100], 0 means disable */
        OPTION_HEAT_MAX = 0x36,       /* maximum level: heat to prevent fogging up */
        OPTION_HEAT = 0x37,       /* heat to prevent fogging up */
        OPTION_LOW_NOISE = 0x38,       /* low noise mode: 1 => enable */
        OPTION_POWER = 0x39,       /* get power consumption, unit: milliwatt */
        OPTION_GLOBAL_RESET_MODE = 0x3a,       /* global reset mode */
        OPTION_OPEN_USB_ERRORCODE = 0x3b,       /* open usb error code */
        OPTION_LINUX_USB_ZEROCOPY = 0x3c,       /* global option for linux platform:
                                                       enable or disable usb zerocopy (helps to reduce memory copy and improve efficiency. Requires kernel version >= 4.6 and hardware platform support)
                                                       if the image is wrong, this indicates that the hardware platform does not support this feature, please disable it when the program starts:
                                                         sdk_put_Option((this is a global option, the camera handle parameter is not required, use nullptr), sdk_OPTION_LINUX_USB_ZEROCOPY, 0)
                                                       default value:
                                                         disable(0): android or arm32
                                                         enable(1):  others
                                                    */
        OPTION_FLUSH = 0x3d         /* 1 = hard flush, discard frames cached by camera DDR (if any)
                                                       2 = soft flush, discard frames cached by sdk.dll (if any)
                                                       3 = both flush
                                                       sdk_Flush means 'both flush'
                                                    */
    };
}