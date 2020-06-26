#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Model;
using NINA.ViewModel.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {

    public interface ISequenceMediator : IMediator<ISequenceVM> {

        Task<bool> SetSequenceCoordinates(DeepSkyObject dso);

        Task<bool> SetSequenceCoordinates(ICollection<DeepSkyObject> dso, bool replace);

        bool OkToExit();
    }
}