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

using NINA.ViewModel.Equipment.FlatDevice;
using System.Windows;
using System.Windows.Controls;

namespace NINA.View.Equipment.FlatDevice {

    internal class WizardGridCellTemplateSelector : DataTemplateSelector {
        public DataTemplate FilterNameCell { get; set; }
        public DataTemplate TimingCell { get; set; }
        public DataTemplate EmptyCell { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            switch (item) {
                case FilterTiming timing when timing.ShowFilterNameOnly:
                    return FilterNameCell;

                case FilterTiming timing when timing.IsEmpty:
                    return EmptyCell;

                case FilterTiming _:
                    return TimingCell;

                default:
                    return EmptyCell;
            }
        }
    }
}