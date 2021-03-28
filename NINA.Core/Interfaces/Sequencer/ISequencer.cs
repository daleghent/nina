using NINA.Model;
using NINA.Sequencer.Container;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer {
    public interface ISequencer {
        ISequenceRootContainer MainContainer { get; set; }

        Task Start(IProgress<ApplicationStatus> progress, CancellationToken token);
    }
}