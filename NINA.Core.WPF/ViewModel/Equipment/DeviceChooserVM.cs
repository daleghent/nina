#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Profile.Interfaces;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;
using NINA.Equipment.Interfaces;
using NINA.Equipment.Interfaces.ViewModel;

namespace NINA.WPF.Base.ViewModel.Equipment {

    public abstract class DeviceChooserVM : BaseVM, IDeviceChooserVM {

        public DeviceChooserVM(IProfileService profileService) : base(profileService) {
            this.profileService = profileService;
            this.Devices = new List<IDevice>();
            SetupDialogCommand = new RelayCommand(OpenSetupDialog);
        }

        protected object lockObj = new object();

        private IList<IDevice> devices;

        public IList<IDevice> Devices {
            get => devices;
            protected set {
                devices = value;
                RaisePropertyChanged();
            }
        }

        public abstract void GetEquipment();

        private IDevice _selectedDevice;

        public IDevice SelectedDevice {
            get {
                lock (lockObj) {
                    return _selectedDevice;
                }
            }
            set {
                lock (lockObj) {
                    _selectedDevice = value;
                    RaisePropertyChanged();
                }
            }
        }

        public ICommand SetupDialogCommand { get; private set; }

        private void OpenSetupDialog(object o) {
            if (SelectedDevice?.HasSetupDialog == true) {
                SelectedDevice.SetupDialog();
            }
        }

        protected void DetermineSelectedDevice(IList<IDevice> d, string id) {
            if (d.Count > 0) {
                var items = (from device in d where device.Id == id select device);
                if (items.Count() > 0) {
                    SelectedDevice = items.First();
                } else {
                    SelectedDevice = d.First();
                }
            }
            Devices = d;
        }
    }
}