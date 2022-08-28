using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ASTPANptical.ASTPANSDK {
    public class ASTPANCameraDll {
        private const string DLLNAME = "ASTPANCamera.dll";

        static ASTPANCameraDll() {
            DllLoader.LoadDll(Path.Combine("ASTPAN", DLLNAME));
        }

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetNumOfCameras", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANGetNumOfCameras(out int Number);

       
        [DllImport(DLLNAME, EntryPoint = "ASTPANGetCameraInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANGetCameraInfo(
          out ASTPANCameraDll.ASTPAN_CAMERA_INFO pCameraInfo,
          int iCameraIndex);

        
        [DllImport(DLLNAME, EntryPoint = "ASTPANGetCameraInfoByID", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANGetCameraInfoByID(
          int ID,
          out ASTPANCameraDll.ASTPAN_CAMERA_INFO pCameraInfo);

        

        [DllImport(DLLNAME, EntryPoint = "ASTPANOpenCamera", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANOpenCamera(int ID);

        

        [DllImport(DLLNAME, EntryPoint = "ASTPANInitCamera", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANInitCamera(int ID);

       

        [DllImport(DLLNAME, EntryPoint = "ASTPANCloseCamera", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANCloseCamera(int ID);

        

        [DllImport(DLLNAME, EntryPoint = "ASTPANSetConfigValue", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANSetConfigValue(
          int ID,
          ref ASTPANCameraDll.ASTPAN_CONFIG pConfig,
          int index);

        

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetConfigValue", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANGetConfigValue(
          int ID,
          ref ASTPANCameraDll.ASTPAN_CONFIG pConfig,
          int index);

        

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetAutoConfigInfo", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANGetAutoConfigInfo(
          int ID,
          ref ASTPANCameraDll.ASTPAN_AUTO_CONFIG_INFO pAutoConfigInfo);

       

        [DllImport(DLLNAME, EntryPoint = "ASTPANSetAutoConfigValue", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANSetAutoConfigValue(
          int ID,
          int ASTPAN_AUTO_CFG_index,
          int Value,
          int bAuto);

        

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetAutoConfigValue", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANGetAutoConfigValue(
          int ID,
          int ASTPAN_AUTO_CFG_index,
          out int Value,
          out int pbAuto);

        

        [DllImport(DLLNAME, EntryPoint = "ASTPANStartVideoCapture", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANStartVideoCapture(int ID);

        

        [DllImport(DLLNAME, EntryPoint = "ASTPANStopVideoCapture", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANStopVideoCapture(int ID);

        

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetVideoData", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANGetVideoData(
          int ID,
          IntPtr pBuffer,
          int lBuffSize,
          int iWaitms);

       

        [DllImport(DLLNAME, EntryPoint = "ASTPANStartExposure", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANStartExposure(int ID);

       

        [DllImport(DLLNAME, EntryPoint = "ASTPANStopExposure", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANStopExposure(int ID);

       

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetExpStatus", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANGetExpStatus(int ID, out int pExpStatus);

        

        [DllImport(DLLNAME, EntryPoint = "ASTPANGetDataAfterExp", CallingConvention = CallingConvention.Cdecl)]
        private static extern int ASTPANGetDataAfterExp(int ID, IntPtr pBuffer, int lBuffSize);

        public static int GetNumOfCameras(out int Number) => ASTPANCameraDll.ASTPANGetNumOfCameras(out Number);

        public static int GetCameraInfo(
          out ASTPANCameraDll.ASTPAN_CAMERA_INFO pCameraInfo,
          int iCameraIndex) {
            return ASTPANCameraDll.ASTPANGetCameraInfo(out pCameraInfo, iCameraIndex);
        }

        public static int GetCameraInfoByID(
          int ID,
          out ASTPANCameraDll.ASTPAN_CAMERA_INFO pCameraInfo) {
            return ASTPANCameraDll.ASTPANGetCameraInfoByID(ID, out pCameraInfo);
        }

        public static int OpenCamera(int ID) =>  ASTPANCameraDll.ASTPANOpenCamera(ID);

        public static int InitCamera(int ID) => ASTPANCameraDll.ASTPANInitCamera(ID);

        public static int CloseCamera(int ID) => ASTPANCameraDll.ASTPANCloseCamera(ID);

        public static int SetConfigValue(
          int ID,
          ref ASTPANCameraDll.ASTPAN_CONFIG pConfig,
          int index) {
            return ASTPANCameraDll.ASTPANSetConfigValue(ID, ref pConfig, index);
        }

        public static int GetConfigValue(
          int ID,
          ref ASTPANCameraDll.ASTPAN_CONFIG pConfig,
          int index) {
            return ASTPANCameraDll.ASTPANGetConfigValue(ID, ref pConfig, index);
        }

        public static int GetAutoConfigInfo(
          int ID,
          ref ASTPANCameraDll.ASTPAN_AUTO_CONFIG_INFO pAutoConfigInfo) {
            return ASTPANCameraDll.ASTPANGetAutoConfigInfo(ID, ref pAutoConfigInfo);
        }

        public static int SetAutoConfigValue(int ID, int ASTPAN_AUTO_CFG_index, int Value) => ASTPANCameraDll.ASTPANSetAutoConfigValue(ID, ASTPAN_AUTO_CFG_index, Value, 0);

        public static int GetAutoConfigValue(int ID, int ASTPAN_AUTO_CFG_index, out int pValue) {
            int pbAuto;
            return ASTPANCameraDll.ASTPANGetAutoConfigValue(ID, ASTPAN_AUTO_CFG_index, out pValue, out pbAuto);
        }

        public static int StartVideoCapture(int ID) => ASTPANCameraDll.ASTPANStartVideoCapture(ID);

        public static int StopVideoCapture(int ID) => ASTPANCameraDll.ASTPANStopVideoCapture(ID);

        public static int GetVideoData(int ID, IntPtr pBuffer, int lBuffSize, int iWaitms) =>ASTPANCameraDll.ASTPANGetVideoData(ID, pBuffer, lBuffSize, iWaitms);

        public static int StartExposure(int ID) =>  ASTPANCameraDll.ASTPANStartExposure(ID);

        public static int StopExposure(int ID) => ASTPANCameraDll.ASTPANStopExposure(ID);

        public static int GetExpStatus(int ID, out int pExpStatus) =>  ASTPANCameraDll.ASTPANGetExpStatus(ID, out pExpStatus);

        public static int GetDataAfterExp(int ID, IntPtr pBuffer, int lBuffSize) => ASTPANCameraDll.ASTPANGetDataAfterExp(ID, pBuffer, lBuffSize);

        public enum ASTPAN_RET_TYPE {
            ASTPAN_RET_SUCCESS,
            ASTPAN_RET_ERROR_INDEX,
            ASTPAN_RET_ERROR_ID,
            ASTPAN_RET_ERROR_PARAMETER,
            ASTPAN_RET_ERROR_SEQUENCE,
            ASTPAN_RET_ERROR_PATH,
            ASTPAN_RET_ERROR_FILEFORMAT,
            ASTPAN_RET_ERROR_SIZE,
            ASTPAN_RET_ERROR_IMGTYPE,
            ASTPAN_RET_ERROR_MODE,
            ASTPAN_RET_CAMERA_CLOSED,
            ASTPAN_RET_CAMERA_REMOVED,
            ASTPAN_RET_OUTOF_BOUNDARY,
            ASTPAN_RET_TIMEOUT,
            ASTPAN_RET_BUFFER_TOO_SMALL,
            ASTPAN_RET_VIDEO_MODE_ACTIVE,
            ASTPAN_RET_EXPOSURE_IN_PROGRESS,
            ASTPAN_RET_GENERAL_ERROR,
            ASTPAN_RET_END,
        }

        public enum ASTPAN_AUTO_TYPE {
            ASTPAN_AUTO_CFG_Gain,
            ASTPAN_AUTO_CFG_Exposure,
            ASTPAN_AUTO_CFG_Gamma,
            ASTPAN_AUTO_CFG_Wb_r,
            ASTPAN_AUTO_CFG_Wb_b,
            ASTPAN_AUTO_CFG_Offset,
            ASTPAN_AUTO_CFG_BandwidthOverload,
            ASTPAN_AUTO_CFG_Flip,
            ASTPAN_AUTO_CFG_OverClock,
            ASTPAN_AUTO_CFG_AutoMaxGain,
            ASTPAN_AUTO_CFG_AutoMaxExp,
            ASTPAN_AUTO_CFG_AutoTargetBrightn,
            ASTPAN_AUTO_CFG_HardwareBin,
            ASTPAN_AUTO_CFG_HighSpeedMode,
            ASTPAN_AUTO_CFG_CoolerPowerPerc,
            ASTPAN_AUTO_CFG_TargetTemp,
            ASTPAN_AUTO_CFG_CoolerOn,
            ASTPAN_AUTO_CFG_MonoBin,
            ASTPAN_AUTO_CFG_FanOn,
            ASTPAN_AUTO_CFG_PatternAdjust,
            ASTPAN_AUTO_CFG_Temperature,
            ASTPAN_AUTO_CFG_AntiDewHedter,
        }

        public enum ASTPAN_CFG_TYPE {
            ASTPAN_CFG_Wr_ROI,
            ASTPAN_CFG_Wr_StartPos,
            ASTPAN_CFG_Wr_PulseGuide,
            ASTPAN_CFG_wr_CameraMode,
            ASTPAN_CFG_Wr_TriggerOut,
            ASTPAN_CFG_Wr_ID,
            ASTPAN_CFG_r_SN,
            ASTPAN_CFG_r_DroppedFrames,
            ASTPAN_CFG_r_SupportedCameraMode,
            ASTPAN_CFG_r_OffsetGain,
        }

        public enum ASTPAN_BAYER_TYPE {
            ASTPAN_BAYER_RG,
            ASTPAN_BAYER_BG,
            ASTPAN_BAYER_GR,
            ASTPAN_BAYER_GB,
        }

        public enum ASTPAN_IMG_TYPE {
            ASTPAN_IMG_END = -1, // 0xFFFFFFFF
            ASTPAN_IMG_RAW8 = 0,
            ASTPAN_IMG_RGB24 = 1,
            ASTPAN_IMG_RAW16 = 2,
            ASTPAN_IMG_Y8 = 3,
        }

        public enum ASTPAN_GUIDE_TYPE {
            ASTPAN_GUIDE_NORTH,
            ASTPAN_GUIDE_SOUTH,
            ASTPAN_GUIDE_EAST,
            ASTPAN_GUIDE_WEST,
        }

        public enum ASTPAN_FLIP_TYPE {
            ASTPAN_FLIP_NONE,
            ASTPAN_FLIP_HORIZ,
            ASTPAN_FLIP_VERT,
            ASTPAN_FLIP_BOTH,
        }

        public enum ASTPAN_EXP_TYPE {
            ASTPAN_EXP_IDLE,
            ASTPAN_EXP_WORKING,
            ASTPAN_EXP_SUCCESS,
            ASTPAN_EXP_FAILED,
        }

        public struct ASTPAN_CAMERA_INFO {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U1)]
            public byte[] name;
            public int CameraID;
            public int MaxHeight;
            public int MaxWidth;
            public int BayerPattern;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public int[] SupportedBins;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public int[] SupportedVideoFormat;
            public double PixelSize;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public int[] SupportedCameraMode;
            public int MechanicalShutter;
            public int ST4GuidePort;
            public int IsColorCam;
            public int IsCoolerCam;
            public int IsUSB3Host;
            public int IsUSB3Camera;
            public int IsEdgeTrigger;
            public int IsLevelTrigger;
            public float ElecPerADU;
            public int BitDepth;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.U1)]
            public byte[] Unused;

            public string Name => Encoding.ASCII.GetString(this.name).TrimEnd(new char[1]);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ASTPAN_CONFIG {
            public int Wr_ROIWidth;
            public int Wr_ROIHeight;
            public int Wr_ROIBin;
            public int Wr_ROImg_type;
            public int Wr_StartPosX;
            public int Wr_StartPosY;
            public int Wr_PulseGuideOn;
            public int Wr_PulseGuideDirection;
            public int Wr_CameraMode;
            public int Wr_TriggerOutPin;
            public int Wr_TriggerOutHigh;
            public int Wr_TriggerOutDelay;
            public int Wr_TriggerOutDuration;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U1)]
            public byte[] Wr_ID;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.U1)]
            public byte[] r_SN;
            public int r_DroppedFrames;
            public int r_OffsetHighestDR;
            public int r_OffsetUnityGain;
            public int r_OffsetLowestRN;
            public int r_GainLowestRN;

            public string wr_id => Encoding.ASCII.GetString(this.Wr_ID).TrimEnd(new char[1]);

            public string r_sn => Encoding.ASCII.GetString(this.r_SN).TrimEnd(new char[1]);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ASTPAN_MUL_CONFIG {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64, ArraySubType = UnmanagedType.U1)]
            public byte[] r_Name;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128, ArraySubType = UnmanagedType.U1)]
            public byte[] r_Description;
            public int r_MaxValue;
            public int r_MinValue;
            public int r_DefaultValue;
            public int r_IsAutoSupported;
            public int r_IsWritable;
            public int r_IsSupported;

            public string r_name => Encoding.ASCII.GetString(this.r_Name).TrimEnd(new char[1]);

            public string r_description => Encoding.ASCII.GetString(this.r_Description).TrimEnd(new char[1]);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ASTPAN_AUTO_CONFIG_INFO {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22, ArraySubType = UnmanagedType.Struct)]
            public ASTPANCameraDll.ASTPAN_MUL_CONFIG[] AutoConfigInfo;
        }
    }
}

