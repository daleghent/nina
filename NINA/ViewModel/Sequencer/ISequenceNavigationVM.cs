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
using NINA.Sequencer.Container;
using NINA.ViewModel.Interfaces;
using System.Collections.Generic;
using System.Windows.Input;

namespace NINA.ViewModel.Sequencer {

    public interface ISequenceNavigationVM : IDockableVM {
        object ActiveSequencerVM { get; set; }
        ISimpleSequenceVM SimpleSequenceVM { get; }
        ISequence2VM Sequence2VM { get; }
        ICommand AddTargetCommand { get; }
        ICommand ImportTargetsCommand { get; }
        ICommand LoadSequenceCommand { get; }
        ICommand LoadTargetSetCommand { get; }
        ICommand SwitchToAdvancedSequenceCommand { get; }

        void SwitchToAdvancedView();

        void SwitchToOverview();

        void AddSimpleTarget(DeepSkyObject deepSkyObject);

        void AddAdvancedTarget(IDeepSkyObjectContainer container);

        void SetAdvancedSequence(ISequenceRootContainer container);

        IList<IDeepSkyObjectContainer> GetDeepSkyObjectContainerTemplates();
        void AddTargetToTargetList(IDeepSkyObjectContainer container);
    }
}