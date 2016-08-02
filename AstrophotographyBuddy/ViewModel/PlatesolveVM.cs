using AstrophotographyBuddy.Model;
using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AstrophotographyBuddy.ViewModel {
    class PlatesolveVM : BaseVM {

        public PlatesolveVM() {
            Name = "Plate Solving";
            ImageGeometry = (System.Windows.Media.GeometryGroup)System.Windows.Application.Current.Resources["PlatesolveSVG"];
            Platesolver = new AstrometryPlateSolver();
            BlindSolveCommand = new AsyncCommand<bool>(() => blindSolve());
            SyncCommand = new RelayCommand(syncTelescope);         
        }

        private void syncTelescope(object obj) {
            if(PlateSolveResult != null) {
                Telescope.sync(PlateSolveResult.RaString, PlateSolveResult.DecString);
            }
        }

        private async Task<bool> blindSolve() {
            PlateSolveResult = await Platesolver.blindSolve(ImagingVM.Image);
            return true;
        }

        IPlateSolver _platesolver;
        IPlateSolver Platesolver {
            get {
                return _platesolver;
            }
            set {
                _platesolver = value;
                RaisePropertyChanged();
            }

        }

        private PlateSolveResult _plateSolveResult;

        private ImagingVM _imagingVM;
        public ImagingVM ImagingVM {
            get {
                return _imagingVM;
            }
            set {
                _imagingVM = value;
                RaisePropertyChanged();
            }
        }

        private TelescopeModel _telescope;
        public TelescopeModel Telescope {
            get {
                return _telescope;
            }
            set {
                _telescope = value;
                RaisePropertyChanged();
            }
        }

        private IAsyncCommand _blindSolveCommand;
        public IAsyncCommand BlindSolveCommand {
            get {
                return _blindSolveCommand;
            }
            set {
                _blindSolveCommand = value;
                RaisePropertyChanged();
            }
        }

        private ICommand _syncCommand;
        public ICommand SyncCommand {
            get {
                return _syncCommand;
            }
            set {
                _syncCommand = value;
                RaisePropertyChanged();
            }
        }

        public PlateSolveResult PlateSolveResult {
            get {
                return _plateSolveResult;
            }

            set {
                _plateSolveResult = value;
                RaisePropertyChanged();
            }
        }
    }
}
