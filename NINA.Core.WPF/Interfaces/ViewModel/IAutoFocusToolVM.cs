#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Equipment.Equipment.MyCamera;
using NINA.Equipment.Equipment.MyFilterWheel;
using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.ViewModel.Imaging;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IAutoFocusToolVM : IDockableVM {
        IAutoFocusVM AutoFocusVM { get; }
        ICommand CancelAutoFocusCommand { get; }
        AsyncObservableCollection<AutoFocusToolVM.Chart> ChartList { get; set; }
        bool ChartListSelectable { get; set; }
        AutoFocusToolVM.Chart SelectedChart { get; set; }
        IAsyncCommand StartAutoFocusCommand { get; }
        ApplicationStatus Status { get; set; }

        void Dispose();

        void UpdateDeviceInfo(CameraInfo deviceInfo);

        void UpdateDeviceInfo(FocuserInfo deviceInfo);
    }
}