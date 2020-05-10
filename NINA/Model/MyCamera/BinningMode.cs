#region "copyright"

/*
    Copyright © 2016 - 2020 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Utility;
using System;
using System.Xml.Serialization;

namespace NINA.Model.MyCamera {

    [Serializable()]
    [XmlRoot(ElementName = nameof(BinningMode))]
    public class BinningMode : BaseINPC {
        private const char SEPARATOR = 'x';

        private BinningMode() {
        }

        public BinningMode(short x, short y) {
            X = x;
            Y = y;
        }

        private short _x;
        private short _y;

        public string Name => string.Join(SEPARATOR.ToString(), X, Y);

        [XmlElement(nameof(X))]
        public short X {
            get => _x;

            set {
                _x = value;
                RaisePropertyChanged();
            }
        }

        [XmlElement(nameof(Y))]
        public short Y {
            get => _y;

            set {
                _y = value;
                RaisePropertyChanged();
            }
        }

        public override string ToString() {
            return Name;
        }

        public override bool Equals(object obj) {
            if (obj == null || this.GetType() != obj.GetType()) {
                return false;
            }

            var other = (BinningMode)obj;
            return _x == other._x && _y == other._y;
        }

        public override int GetHashCode() {
            //see https://en.wikipedia.org/wiki/Hash_function, used when BinningMode is used as a dictionary key for instance
            const int primeNumber = 397;
            unchecked {
                return (_x.GetHashCode() * primeNumber) ^ _y.GetHashCode();
            }
        }

        public static bool TryParse(string s, out BinningMode mode) {
            mode = null;
            if (string.IsNullOrEmpty(s)) return false;
            try {
                if (!short.TryParse(s.Split(SEPARATOR)[0], out var x)) return false;
                if (!short.TryParse(s.Split(SEPARATOR)[1], out var y)) return false;
                mode = new BinningMode(x, y);
                return true;
            } catch (Exception ex) {
                Logger.Error($"Could not parse binning mode from {s}. {ex}");
            }
            return false;
        }
    }
}