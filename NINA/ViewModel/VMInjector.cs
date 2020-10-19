using Accord.Statistics.Kernels;
using NINA.Utility;
using NINA.ViewModel.FlatWizard;
using NINA.ViewModel.FramingAssistant;
using NINA.ViewModel.ImageHistory;
using NINA.ViewModel.Interfaces;
using NINA.ViewModel.Sequencer;
using Ninject;
using System;
using System.Linq.Expressions;
using System.Windows;

namespace NINA.ViewModel {

    internal class VMInjector {

        public VMInjector() {
            try {
                Application.Current.DispatcherUnhandledException += Current_DispatcherUnhandledException;

                IReadOnlyKernel _kernel = new KernelConfiguration(new IoCBindings()).BuildReadonlyKernel();
                AppVM = _kernel.Get<IApplicationVM>();
                ImageSaveController = _kernel.Get<IImageSaveController>();
                ImagingVM = _kernel.Get<IImagingVM>();
                EquipmentVM = _kernel.Get<IEquipmentVM>();
                SkyAtlasVM = _kernel.Get<ISkyAtlasVM>();
                SeqVM = _kernel.Get<ISequenceVM>();
                FramingAssistantVM = _kernel.Get<IFramingAssistantVM>();
                FlatWizardVM = _kernel.Get<IFlatWizardVM>();
                DockManagerVM = _kernel.Get<IDockManagerVM>();

                OptionsVM = _kernel.Get<IOptionsVM>();
                ApplicationDeviceConnectionVM = _kernel.Get<IApplicationDeviceConnectionVM>();
                VersionCheckVM = _kernel.Get<IVersionCheckVM>();
                ApplicationStatusVM = _kernel.Get<IApplicationStatusVM>();

                Sequence2VM = _kernel.Get<ISequence2VM>();
                ImageHistoryVM = _kernel.Get<IImageHistoryVM>();
            } catch (Exception ex) {
                Logger.Error(ex);
                throw ex;
            }
        }

        private void Current_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e) {
            Logger.Error(e.Exception);

            if (Application.Current != null) {
                var result = MyMessageBox.MyMessageBox.Show(Locale.Loc.Instance["LblApplicationInBreakMode"], Locale.Loc.Instance["LblUnhandledException"], MessageBoxButton.YesNo, MessageBoxResult.No);
                if (result == MessageBoxResult.Yes) {
                    e.Handled = true;
                } else {
                    try {
                        ApplicationDeviceConnectionVM.DisconnectEquipment();
                    } catch (Exception ex) {
                        Logger.Error(ex);
                    }
                    e.Handled = true;
                    Application.Current.Shutdown();
                }
            }
        }

        public IImagingVM ImagingVM { get; private set; }

        public IApplicationVM AppVM { get; private set; }

        public IEquipmentVM EquipmentVM { get; private set; }

        public ISkyAtlasVM SkyAtlasVM { get; private set; }

        public IFramingAssistantVM FramingAssistantVM { get; private set; }

        public IFlatWizardVM FlatWizardVM { get; private set; }

        public IDockManagerVM DockManagerVM { get; private set; }

        public ISequenceVM SeqVM { get; private set; }

        public IOptionsVM OptionsVM { get; private set; }

        public IVersionCheckVM VersionCheckVM { get; private set; }

        public IApplicationStatusVM ApplicationStatusVM { get; private set; }

        public IApplicationDeviceConnectionVM ApplicationDeviceConnectionVM { get; private set; }

        public ISequence2VM Sequence2VM { get; private set; }

        public IImageSaveController ImageSaveController { get; private set; }

        public IImageHistoryVM ImageHistoryVM { get; private set; }
    }
}