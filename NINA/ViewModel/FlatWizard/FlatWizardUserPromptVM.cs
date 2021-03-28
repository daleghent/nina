#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Enum;
using NINA.Model.MyCamera;
using NINA.Model.MyFlatDevice;
using NINA.Utility;

namespace NINA.ViewModel.FlatWizard {

    internal class FlatWizardUserPromptVM : BaseINPC, IFlatWizardUserPromptVM {
        public RelayCommand ResetAndContinueCommand { get; }
        public RelayCommand ContinueCommand { get; }
        public RelayCommand CancelCommand { get; }
        public FlatWizardUserPromptResult Result { get; set; }

        public FlatWizardUserPromptVM() {
            ResetAndContinueCommand = new RelayCommand(ResetAndContinueContinueFlatWizard);
            ContinueCommand = new RelayCommand(ContinueFlatWizard);
            CancelCommand = new RelayCommand(CancelFlatWizard);
        }

        private void ResetAndContinueContinueFlatWizard(object obj) {
            Result = FlatWizardUserPromptResult.ResetAndContinue;
        }

        private void CancelFlatWizard(object obj) {
            Result = FlatWizardUserPromptResult.Cancel;
        }

        private void ContinueFlatWizard(object obj) {
            Result = FlatWizardUserPromptResult.Continue;
        }

        private FlatWizardFilterSettingsWrapper settings;

        public FlatWizardFilterSettingsWrapper Settings {
            get => settings;
            set {
                settings = value;
                RaisePropertyChanged();
            }
        }

        private string message;

        public string Message {
            get => message;
            set {
                message = value;
                RaisePropertyChanged();
            }
        }

        private double currentMean;

        public double CurrentMean {
            get => currentMean;
            set {
                currentMean = value;
                RaisePropertyChanged();
            }
        }

        private double cameraBitDepth;

        public double CameraBitDepth {
            get => cameraBitDepth;
            set {
                cameraBitDepth = value;
                RaisePropertyChanged();
            }
        }

        private double expectedExposureTime;

        public double ExpectedExposureTime {
            get => expectedExposureTime;
            set {
                expectedExposureTime = value;
                RaisePropertyChanged();
            }
        }

        private double expectedBrightness;

        public double ExpectedBrightness {
            get => expectedBrightness;
            set {
                expectedBrightness = value;
                RaisePropertyChanged();
            }
        }

        private FlatWizardMode flatWizardMode;

        public FlatWizardMode FlatWizardMode {
            get => flatWizardMode;
            set {
                flatWizardMode = value;
                RaisePropertyChanged();
            }
        }

        private CameraInfo cameraInfo;

        public CameraInfo CameraInfo {
            get => cameraInfo;
            set {
                cameraInfo = value;
                RaisePropertyChanged();
            }
        }

        private FlatDeviceInfo flatDeviceInfo;

        public FlatDeviceInfo FlatDeviceInfo {
            get => flatDeviceInfo;
            set {
                flatDeviceInfo = value;
                RaisePropertyChanged();
            }
        }
    }
}