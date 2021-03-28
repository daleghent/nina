using System.Collections.Generic;
using System.ComponentModel;
using NINA.Utility.Astrometry;
using NINACustomControlLibrary;
using Nito.Mvvm;

namespace NINA.ViewModel {

    public interface IDeepSkyObjectSearchVM : INotifyPropertyChanged {
        Coordinates Coordinates { get; set; }
        int Limit { get; set; }
        IAutoCompleteItem SelectedTargetSearchResult { get; set; }
        bool ShowPopup { get; set; }
        string TargetName { get; set; }
        NotifyTask<List<IAutoCompleteItem>> TargetSearchResult { get; set; }

        void SetTargetNameWithoutSearch(string targetName);
    }
}