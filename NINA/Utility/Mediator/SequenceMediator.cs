using NINA.Model;
using NINA.Utility.Mediator.Interfaces;
using NINA.ViewModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NINA.Utility.Mediator {

    internal class SequenceMediator : ISequenceMediator {
        private ISequenceVM handler;

        public void RegisterHandler(ISequenceVM handler) {
            if (this.handler != null) {
                throw new Exception("Handler already registered!");
            }
            this.handler = handler;
        }

        public async Task<bool> SetSequenceCoordinates(ICollection<DeepSkyObject> dso, bool replace) {
            return await handler.SetSequenceCoordiantes(dso, replace);
        }

        public async Task<bool> SetSequenceCoordinates(DeepSkyObject dso) {
            return await handler.SetSequenceCoordiantes(dso);
        }

        public bool OkToExit() {
            return handler.OKtoExit();
        }
    }
}