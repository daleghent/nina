using NINA.Model;
using NINA.Sequencer.Conditions;
using NINA.Sequencer.Container;
using NINA.Sequencer.DragDrop;
using NINA.Sequencer.SequenceItem;
using NINA.Sequencer.Trigger;
using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.ViewModel.Sequencer.SimpleSequence {

    public interface ISimpleExposure : ISequenceContainer, IDropContainer, IConditionable, ITriggerable, ISequenceItem {
        bool Dither { get; set; }
        bool Enabled { get; set; }

        Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token);

        ISequenceTrigger GetDitherAfterExposures();

        ISequenceCondition GetLoopCondition();

        ISequenceItem GetSwitchFilter();

        ISequenceItem GetTakeExposure();

        void OnDeserializing(StreamingContext context);

        IImmutableContainer TransformToSmartExposure();
    }
}