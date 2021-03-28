using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using NINA.Model;
using NINA.Sequencer.Container;
using NINA.Utility.WindowService;

namespace NINA.ViewModel.Interfaces {

    public interface ISimpleSequenceVM {
        ICommand AddTargetCommand { get; }
        TimeSpan EstimatedDownloadTime { get; set; }
        ObservableCollection<string> ImageTypes { get; set; }
        TimeSpan OverallDuration { get; }
        DateTime OverallEndTime { get; }
        DateTime OverallStartTime { get; }
        ICommand SaveAsSequenceCommand { get; }
        ICommand SaveSequenceCommand { get; }
        ICommand SaveTargetSetCommand { get; }
        ICommand BuildSequenceCommand { get; }
        ISimpleDSOContainer SelectedTarget { get; set; }
        IWindowServiceFactory WindowServiceFactory { get; set; }

        bool LoadTarget();

        bool LoadTargetSet();

        bool ImportTargets();

        void AddDownloadTime(TimeSpan t);

        void AddTarget(DeepSkyObject deepSkyObject);
    }
}