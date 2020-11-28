#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Utility;

namespace NINA.ViewModel.FlatWizard {

    internal class FlatWizardUserPromptVM : BaseINPC, IFlatWizardUserPromptVM {
        public RelayCommand ResetAndContinueCommand { get; }
        public RelayCommand ContinueCommand { get; }
        public RelayCommand CancelCommand { get; }
        public DialogResult Result { get; set; }

        public FlatWizardUserPromptVM() {
            ResetAndContinueCommand = new RelayCommand(ResetAndContinueContinueFlatWizard);
            ContinueCommand = new RelayCommand(ContinueFlatWizard);
            CancelCommand = new RelayCommand(CancelFlatWizard);
        }

        private void ResetAndContinueContinueFlatWizard(object obj) {
            Result = DialogResult.ResetAndContinue;
        }

        private void CancelFlatWizard(object obj) {
            Result = DialogResult.Cancel;
        }

        private void ContinueFlatWizard(object obj) {
            Result = DialogResult.Continue;
        }

        private FlatWizardFilterSettingsWrapper settings;
        private string message;
        private double currentMean;
        private double cameraBitDepth;
        private double expectedExposureTime;

        public FlatWizardFilterSettingsWrapper Settings {
            get => settings;
            set {
                settings = value;
                RaisePropertyChanged();
            }
        }

        public string Message {
            get => message;
            set {
                message = value;
                RaisePropertyChanged();
            }
        }

        public double CurrentMean {
            get => currentMean;
            set {
                currentMean = value;
                RaisePropertyChanged();
            }
        }

        public double CameraBitDepth {
            get => cameraBitDepth;
            set {
                cameraBitDepth = value;
                RaisePropertyChanged();
            }
        }

        public double ExpectedExposureTime {
            get => expectedExposureTime;
            set {
                expectedExposureTime = value;
                RaisePropertyChanged();
            }
        }
    }
}