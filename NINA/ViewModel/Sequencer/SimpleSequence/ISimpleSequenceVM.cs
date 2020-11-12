using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Model;
using NINA.Model.MyCamera;
using NINA.Model.MyFilterWheel;
using NINA.Model.MyFlatDevice;
using NINA.Model.MyFocuser;
using NINA.Model.MyGuider;
using NINA.Model.MyRotator;
using NINA.Model.MyTelescope;
using NINA.Model.MyWeatherData;
using NINA.Sequencer.Container;
using NINA.Utility;
using NINA.Utility.WindowService;
using NINA.ViewModel.ImageHistory;

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
        SimpleDSOContainer SelectedTarget { get; set; }
        IWindowServiceFactory WindowServiceFactory { get; set; }

        bool LoadTarget();

        bool LoadTargetSet();

        bool ImportTargets();

        void AddDownloadTime(TimeSpan t);

        void AddTarget(DeepSkyObject deepSkyObject);
    }
}