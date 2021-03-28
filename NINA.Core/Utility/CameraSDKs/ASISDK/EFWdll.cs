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
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ZWOptical.EFWSDK {

    public class EFWdll {
        private const string DLLNAME = "EFW_filter.dll";

        static EFWdll() {
            DllLoader.LoadDll(Path.Combine("ASI", DLLNAME));
        }

        public enum EFW_ERROR_CODE {
            EFW_SUCCESS = 0,
            EFW_ERROR_INVALID_INDEX,
            EFW_ERROR_INVALID_ID,
            EFW_ERROR_INVALID_VALUE,
            EFW_ERROR_REMOVED,
            EFW_ERROR_MOVING,
            EFW_ERROR_ERROR_STATE,
            EFW_ERROR_GENERAL_ERROR,//other error
            EFW_ERROR_END = -1
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct EFW_INFO {
            public int ID;

            [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.U1, SizeConst = 64)]
            public byte[] name;

            public int slotNum;

            public string Name {
                get { return Encoding.ASCII.GetString(name).TrimEnd((Char)0); }
            }
        };

        [DllImport(DLLNAME, EntryPoint = "EFWGetNum", CallingConvention = CallingConvention.Cdecl)]
        private static extern int EFWGetNum();

        [DllImport(DLLNAME, EntryPoint = "EFWGetID", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWGetID(int index, out int ID);

        [DllImport(DLLNAME, EntryPoint = "EFWGetProperty", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWGetProperty(int ID, out EFW_INFO pInfo);

        [DllImport(DLLNAME, EntryPoint = "EFWOpen", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWOpen(int index);

        [DllImport(DLLNAME, EntryPoint = "EFWClose", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWClose(int ID);

        [DllImport(DLLNAME, EntryPoint = "EFWGetPosition", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWGetPosition(int ID, out int pPosition);

        [DllImport(DLLNAME, EntryPoint = "EFWSetPosition", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWSetPosition(int ID, int Position);

        [DllImport(DLLNAME, EntryPoint = "EFWSetDirection", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWSetDirection(int ID, [MarshalAs(UnmanagedType.I1)]bool bUnidirectional);

        [DllImport(DLLNAME, EntryPoint = "EFWGetDirection", CallingConvention = CallingConvention.Cdecl)]
        private static extern EFW_ERROR_CODE EFWGetDirection(int ID, [MarshalAs(UnmanagedType.I1)]out bool bUnidirectional);

        public static int GetNum() {
            return EFWGetNum();
        }

        public static EFW_ERROR_CODE GetID(int index, out int ID) {
            return EFWGetID(index, out ID);
        }

        public static EFW_ERROR_CODE GetProperty(int ID, out EFW_INFO pInfo) {
            return EFWGetProperty(ID, out pInfo);
        }

        public static EFW_ERROR_CODE Open(int index) {
            return EFWOpen(index);
        }

        public static EFW_ERROR_CODE Close(int ID) {
            return EFWClose(ID);
        }

        public static EFW_ERROR_CODE GetPosition(int ID, out int pPosition) {
            return EFWGetPosition(ID, out pPosition);
        }

        public static EFW_ERROR_CODE SetPosition(int ID, int Position) {
            return EFWSetPosition(ID, Position);
        }

        public static EFW_ERROR_CODE GetDirection(int ID, out bool bUnidirectional) {
            return EFWGetDirection(ID, out bUnidirectional);
        }

        public static EFW_ERROR_CODE SetDirection(int ID, bool bUnidirectional) {
            return EFWSetDirection(ID, bUnidirectional);
        }
    }
}