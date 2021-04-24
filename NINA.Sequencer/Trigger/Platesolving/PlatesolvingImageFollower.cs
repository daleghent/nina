using NINA.Astrometry;
using NINA.Core.Locale;
using NINA.Core.Model;
using NINA.Core.Utility;
using NINA.Core.Utility.Notification;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Image.ImageData;
using NINA.Image.Interfaces;
using NINA.PlateSolving;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.Interfaces.Mediator;
using NINA.WPF.Base.Interfaces.ViewModel;
using NINA.WPF.Base.Model;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Sequencer.Trigger.Platesolving {
    public class PlatesolvingImageFollower : BaseINPC, IDisposable {
        private readonly IImageSaveMediator imageSaveMediator;
        private readonly IImageHistoryVM history;
        private readonly ITelescopeMediator telescopeMediator;
        private readonly IProfileService profileService;
        private readonly IApplicationStatusMediator applicationStatusMediator;
        private readonly IProgress<ApplicationStatus> progress;
        private bool closed = false;
        private Task solverBackgroundTask;
        private CancellationTokenSource solverBackgroundTaskCancellationSource = new CancellationTokenSource();

        public PlatesolvingImageFollower(IProfileService profileService, IImageHistoryVM history, ITelescopeMediator telescopeMediator, IImageSaveMediator imageSaveMediator,
            IApplicationStatusMediator applicationStatusMediator) {
            this.profileService = profileService;
            this.imageSaveMediator = imageSaveMediator;
            this.history = history;
            this.telescopeMediator = telescopeMediator;
            this.imageSaveMediator.ImageSaved += ImageSaveMediator_ImageSaved;
            this.applicationStatusMediator = applicationStatusMediator;
            this.progress = new Progress<ApplicationStatus>(ProgressStatusUpdate);
            var lastLightImage = history.ImageHistory.Where(x => x.Type == "LIGHT").LastOrDefault();
            LastPlatesolvedId = lastLightImage != null ? lastLightImage.Id : -1;
        }

        private void ProgressStatusUpdate(ApplicationStatus status) {
            if (string.IsNullOrWhiteSpace(status.Source)) {
                status.Source = Loc.Instance["LblSequence"];
            }
            applicationStatusMediator.StatusUpdate(status);
        }

        private void ImageSaveMediator_ImageSaved(object sender, ImageSavedEventArgs e) {
            if (ProgressExposures < AfterExposures) {
                return;
            }

            var recentLightHistory = history.ImageHistory.Where(x => x.Type == "LIGHT" && x.Id > LastPlatesolvedId).ToList();
            var matchingHistoryItem = recentLightHistory.Where(x => new Uri(x.LocalPath) == e.PathToImage).FirstOrDefault();
            if (matchingHistoryItem == null) {
                return;
            }

            if (solverBackgroundTask != null && !solverBackgroundTask.IsCompleted) {
                Logger.Info($"Won't platesolve {e.PathToImage} because another operation is already in progress");
                return;
            }

            LastPlatesolvedId = matchingHistoryItem.Id;
            solverBackgroundTask = Task.Run(async () => await SolveLastImage(matchingHistoryItem, solverBackgroundTaskCancellationSource.Token));
        }

        public void Dispose() {
            if (!closed) {
                this.imageSaveMediator.ImageSaved += ImageSaveMediator_ImageSaved;
                solverBackgroundTaskCancellationSource.Cancel();
                closed = true;
            }
        }

        private int lastPlatesolvedId = -1;
        public int LastPlatesolvedId {
            get => lastPlatesolvedId;
            set {
                lastPlatesolvedId = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ProgressExposures));
            }
        }

        private int afterExposures = 1;
        public int AfterExposures {
            get => afterExposures;
            set {
                afterExposures = value;
                RaisePropertyChanged();
            }
        }

        public int ProgressExposures {
            get {
                return history.ImageHistory.Count(x => x.Type == "LIGHT" && x.Id > LastPlatesolvedId);
            }
        }

        private Coordinates lastCoordinates;
        public Coordinates LastCoordinates {
            get => lastCoordinates;
            set {
                lastCoordinates = value;
                RaisePropertyChanged();
            }
        }

        private async Task<IImageData> LoadHistoryImage(ImageHistoryPoint historyImage) {
            try {
                if (File.Exists(historyImage.LocalPath)) {
                    return await BaseImageData.FromFile(historyImage.LocalPath, (int)profileService.ActiveProfile.CameraSettings.BitDepth, historyImage.IsBayered, profileService.ActiveProfile.CameraSettings.RawConverter);
                } else {
                    Notification.ShowError($"File {historyImage.Filename} does not exist");
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.ShowError(ex.Message);
            }
            return null;
        }

        private async Task SolveLastImage(ImageHistoryPoint historyImage, CancellationToken token) {
            var loadedImage = await LoadHistoryImage(historyImage);
            if (loadedImage == null) {
                return;
            }

            var plateSolver = PlateSolverFactory.GetPlateSolver(profileService.ActiveProfile.PlateSolveSettings);
            var blindSolver = PlateSolverFactory.GetBlindSolver(profileService.ActiveProfile.PlateSolveSettings);

            var solver = new ImageSolver(plateSolver, blindSolver);
            Coordinates coordinates = telescopeMediator.GetInfo().Connected ? telescopeMediator.GetCurrentPosition() : null;
            var parameter = new PlateSolveParameter() {
                Coordinates = coordinates,
                Binning = profileService.ActiveProfile.PlateSolveSettings.Binning,
                DownSampleFactor = profileService.ActiveProfile.PlateSolveSettings.DownSampleFactor,
                FocalLength = profileService.ActiveProfile.TelescopeSettings.FocalLength,
                MaxObjects = profileService.ActiveProfile.PlateSolveSettings.MaxObjects,
                PixelSize = profileService.ActiveProfile.CameraSettings.PixelSize,
                Regions = profileService.ActiveProfile.PlateSolveSettings.Regions,
                SearchRadius = profileService.ActiveProfile.PlateSolveSettings.SearchRadius
            };
            var solveResult = await solver.Solve(loadedImage, parameter, this.progress, token);
            if (!solveResult.Success) {
                Notification.ShowWarning(Loc.Instance["LblPlatesolveFailed"]);
                return;
            }

            LastCoordinates = solveResult.Coordinates;
        }
    }
}
