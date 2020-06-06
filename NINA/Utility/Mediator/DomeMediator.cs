using NINA.Model.MyDome;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Equipment.Dome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {
    internal class DomeMediator : DeviceMediator<IDomeVM, IDomeConsumer, DomeInfo>, IDomeMediator {
    }
}
