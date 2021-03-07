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
using NINA.Profile;
using System.Linq;
using System.Windows.Input;
using System.Collections.Generic;

namespace NINA.ViewModel.Equipment {

    internal abstract class DeviceChooserVM : BaseVM, IDeviceChooserVM {

        public DeviceChooserVM(IProfileService profileService) : base(profileService) {
            this.profileService = profileService;
            this.Devices = new List<Model.IDevice>();
            SetupDialogCommand = new RelayCommand(OpenSetupDialog);
        }

        protected object lockObj = new object();

        private IList<Model.IDevice> devices;

        public IList<Model.IDevice> Devices {
            get => devices;
            protected set {
                devices = value;
                RaisePropertyChanged();
            }
        }

        public abstract void GetEquipment();

        private Model.IDevice _selectedDevice;

        public Model.IDevice SelectedDevice {
            get {
                return _selectedDevice;
            }
            set {
                _selectedDevice = value;
                RaisePropertyChanged();
            }
        }

        public ICommand SetupDialogCommand { get; private set; }

        private void OpenSetupDialog(object o) {
            if (SelectedDevice?.HasSetupDialog == true) {
                SelectedDevice.SetupDialog();
            }
        }

        public void DetermineSelectedDevice(string id) {
            if (Devices.Count > 0) {
                var items = (from device in Devices where device.Id == id select device);
                if (items.Count() > 0) {
                    SelectedDevice = items.First();
                } else {
                    SelectedDevice = Devices.First();
                }
            }
        }
    }
}