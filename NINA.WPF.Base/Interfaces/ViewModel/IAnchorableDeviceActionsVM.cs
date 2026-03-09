#region "copyright"

/*
    Copyright © 2016 - 2026 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using CommunityToolkit.Mvvm.Input;
using NINA.Core.Enum;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.Interfaces.ViewModel {

    public interface IAnchorableDeviceActionsVM : IDockableVM {

        // Device Actions
        bool ActionDeviceConnected { get; }
        DeviceTypeEnum ActionDeviceType { get; set; }
        AsyncObservableCollection<string> SupportedActions { get; set; }
        string ActionName { get; set; }
        string ActionParameters { get; set; }
        string ActionOutput { get; }
        IAsyncRelayCommand DoRunActionCommand { get; }

        // SendCommand
        bool SendCommandDeviceConnected { get; }
        string Command { get; set; }
        bool Raw { get; set; }
        DeviceTypeEnum SendCommandDeviceType { get; set; }
        SendCommandTypeEnum SendCommandType { get; set; }
        string SendCommandOutput { get; }
        IAsyncRelayCommand DoRunSendCommand { get; }

        bool GetDeviceStatus(DeviceTypeEnum deviceType);
        void Dispose();
    }
}
