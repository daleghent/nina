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
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Utility;

namespace NINA.ViewModel.Imaging {

    public interface IAnchorableSnapshotVM : IDockableVM {
        CameraInfo CameraInfo { get; set; }
        ICommand CancelSnapCommand { get; }
        bool IsLooping { get; set; }
        bool LiveViewEnabled { get; set; }
        bool Loop { get; set; }
        BinningMode SnapBin { get; set; }
        IAsyncCommand SnapCommand { get; }
        double SnapExposureDuration { get; set; }
        FilterInfo SnapFilter { get; set; }
        int SnapGain { get; set; }
        bool SnapSave { get; set; }
        bool SnapSubSample { get; set; }
        IAsyncCommand StartLiveViewCommand { get; }
        ApplicationStatus Status { get; set; }
        ICommand StopLiveViewCommand { get; }

        void Dispose();

        Task<bool> SnapImage(IProgress<ApplicationStatus> progress);

        void UpdateDeviceInfo(CameraInfo cameraStatus);
    }
}