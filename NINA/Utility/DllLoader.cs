﻿#region "copyright"

/*
    Copyright © 2016 - 2019 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace NINA.Utility {

    public static class DllLoader {

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        private extern static IntPtr LoadLibrary(string librayName);

        public static void LoadDll(string dllSubPath) {
            String path;

            //IntPtr.Size will be 4 in 32-bit processes, 8 in 64-bit processes
            if (IsX86())
                path = System.AppDomain.CurrentDomain.BaseDirectory + "/External/x86/" + dllSubPath;
            else
                path = System.AppDomain.CurrentDomain.BaseDirectory + "/External/x64/" + dllSubPath;

            if (LoadLibrary(path) == IntPtr.Zero) {
                var error = Marshal.GetLastWin32Error().ToString();
                var message = $"DllLoader failed to load library {dllSubPath} due to error code {error}";
                Logger.Error(message, null);
            }
        }

        public static FileVersionInfo DllVersion(string dllSubPath) {
            String path;

            //IntPtr.Size will be 4 in 32-bit processes, 8 in 64-bit processes
            if (IsX86())
                path = System.AppDomain.CurrentDomain.BaseDirectory + "/External/x86/" + dllSubPath;
            else
                path = System.AppDomain.CurrentDomain.BaseDirectory + "/External/x64/" + dllSubPath;

            return FileVersionInfo.GetVersionInfo(path.Replace('/', '\\'));
        }

        public static bool IsX86() {
            return !Environment.Is64BitProcess;
        }
    }
}