#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Equipment.Equipment.MyFocuser;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Equipment.Interfaces.ViewModel;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.WPF.Base.Mediator {

    public class FocuserMediator : DeviceMediator<IFocuserVM, IFocuserConsumer, FocuserInfo>, IFocuserMediator {

        public void ToggleTempComp(bool tempComp) {
            handler.ToggleTempComp(tempComp);
        }

        public Task<int> MoveFocuser(int position, CancellationToken ct) {
            return handler.MoveFocuser(position, ct);
        }

        public Task<int> MoveFocuserRelative(int position, CancellationToken ct) {
            return handler.MoveFocuserRelative(position, ct);
        }

        public void BroadcastSuccessfulAutoFocusRun(AutoFocusInfo info) {
            foreach (IFocuserConsumer c in consumers) {
                c.UpdateEndAutoFocusRun(info);
            }
        }

        public void BroadcastUserFocused(FocuserInfo info) {
            foreach (IFocuserConsumer c in consumers) {
                c.UpdateUserFocused(info);
            }
        }
    }
}