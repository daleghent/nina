#region "copyright"

/*
    Copyright © 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Exceptions {

    public class CannotDoFunctionsException : Exception {

        public CannotDoFunctionsException() : base("Cannot do functions!") {
        }

        public CannotDoFunctionsException(string message)
            : base(message) {
        }

        public CannotDoFunctionsException(string message, Exception inner)
            : base(message, inner) {
        }
    }
}