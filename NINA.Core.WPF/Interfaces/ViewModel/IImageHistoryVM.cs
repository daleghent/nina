#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model.ImageData;
using NINA.Utility;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.AutoFocus;
using System.Collections.Generic;
using System.Windows.Input;

namespace NINA.ViewModel.ImageHistory {

    public interface IImageHistoryVM : IDockableVM {
        AsyncObservableCollection<ImageHistoryPoint> AutoFocusPoints { get; set; }
        List<ImageHistoryPoint> ImageHistory { get; }
        AsyncObservableCollection<ImageHistoryPoint> ObservableImageHistory { get; set; }
        ICommand PlotClearCommand { get; }

        void Add(int id, IImageStatistics statistics, string imageType);

        void AppendImageProperties(ImageSavedEventArgs imageSavedEventArgs);

        void AppendAutoFocusPoint(AutoFocusReport report);

        void PlotClear();
    }
}