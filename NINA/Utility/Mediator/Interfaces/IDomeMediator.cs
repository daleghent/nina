using NINA.Model.MyDome;
using NINA.ViewModel.Equipment.Dome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator.Interfaces {
    public interface IDomeMediator : IDeviceMediator<IDomeVM, IDomeConsumer, DomeInfo> {
    }
}
