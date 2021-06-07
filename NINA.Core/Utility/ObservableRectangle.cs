#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

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

namespace NINA.Core.Utility {

    public class ObservableRectangle : BaseINPC {

        public ObservableRectangle() {
        }

        public ObservableRectangle(double x, double y, double width, double height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        private double _x;

        public double X {
            get {
                return _x;
            }
            set {
                _x = value;
                RaisePropertyChanged();
            }
        }

        private double _y;

        public double Y {
            get {
                return _y;
            }
            set {
                _y = value;
                RaisePropertyChanged();
            }
        }

        private double _width;

        public double Width {
            get {
                return _width;
            }
            set {
                _width = value;
                RaisePropertyChanged();
            }
        }

        private double _height;

        public double Height {
            get {
                return _height;
            }
            set {
                _height = value;
                RaisePropertyChanged();
            }
        }
    }
}