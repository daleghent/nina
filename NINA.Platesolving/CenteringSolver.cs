#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Core.Utility;
using NINA.Astrometry;
using NINA.Core.Utility.Notification;
using System;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Core.Locale;
using NINA.Equipment.Model;
using NINA.PlateSolving.Interfaces;

namespace NINA.PlateSolving {

    public class CenteringSolver : ICenteringSolver {
        private ITelescopeMediator telescopeMediator;

        public CenteringSolver(IPlateSolver plateSolver,
                IPlateSolver blindSolver,
                IImagingMediator imagingMediator,
                ITelescopeMediator telescopeMediator,
                IFilterWheelMediator filterWheelMediator) {
            this.telescopeMediator = telescopeMediator;
            this.CaptureSolver = new CaptureSolver(plateSolver, blindSolver, imagingMediator, filterWheelMediator);
        }

        public ICaptureSolver CaptureSolver { get; set; }

        public async Task<PlateSolveResult> Center(CaptureSequence seq, CenterSolveParameter parameter, IProgress<PlateSolveProgress> solveProgress, IProgress<ApplicationStatus> progress, CancellationToken ct) {
            if (parameter?.Coordinates == null) { throw new ArgumentException(nameof(CenterSolveParameter.Coordinates)); }
            if (parameter?.Threshold <= 0) { throw new ArgumentException(nameof(CenterSolveParameter.Threshold)); }

            var centered = false;
            PlateSolveResult result;
            Separation offset = new Separation();
            do {
                result = await CaptureSolver.Solve(seq, parameter, solveProgress, progress, ct);

                if (result.Success == false) {
                    //Solving failed. Give up.
                    break;
                }

                result.Separation = result.DetermineSeparation(parameter.Coordinates);

                var position = (telescopeMediator.GetCurrentPosition()).Transform(result.Coordinates.Epoch);
                var positionWithOffset = position - offset;
                Logger.Info($"Centering Solver - Scope Position: {position}; Offset: {offset}; Centering Coordinates: {parameter.Coordinates}; Solved: {result.Coordinates}; Separation {result.Separation}");

                solveProgress?.Report(new PlateSolveProgress() { PlateSolveResult = result });

                if (Math.Abs(result.Separation.Distance.ArcMinutes) > parameter.Threshold) {
                    progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveNotInsideToleranceSyncing"] });
                    if (parameter.NoSync || !await telescopeMediator.Sync(result.Coordinates)) {
                        var oldOffset = offset;
                        offset = result.DetermineSeparation(position);

                        Logger.Info($"Sync {(parameter.NoSync ? "disabled" : "failed")} - calculating offset instead to compensate.  Original: {positionWithOffset}; Original Offset {oldOffset}; Solved: {result.Coordinates}; New Offset: {offset}");
                        progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveSyncViaTargetOffset"] });
                    } else {
                        var positionAfterSync = telescopeMediator.GetCurrentPosition().Transform(result.Coordinates.Epoch);

                        var syncDistance = result.DetermineSeparation(positionAfterSync);
                        if (Math.Abs(syncDistance.Distance.ArcMinutes) > parameter.Threshold) {
                            offset = syncDistance;
                            Logger.Warning($"Sync failed silently - calculating offset instead to compensate.  Position after sync: {positionAfterSync}; Solved: {result.Coordinates}; New Offset: {offset}");
                        } else {
                            // Sync worked - reset offset
                            Logger.Debug($"Synced sucessfully. Position after sync: {positionAfterSync}");
                            offset = new Separation();
                        }
                    }

                    var scopePosition = telescopeMediator.GetCurrentPosition().Transform(result.Coordinates.Epoch);
                    Logger.Info($"Slewing to target after sync. Current Position: {scopePosition}; Target coordinates: {parameter.Coordinates}; Offset {offset}");
                    progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveNotInsideToleranceReslew"] });

                    await telescopeMediator.SlewToCoordinatesAsync(parameter.Coordinates + offset, ct);

                    progress?.Report(new ApplicationStatus() { Status = Loc.Instance["LblPlateSolveNotInsideToleranceRepeating"] });
                } else {
                    centered = true;
                }
            } while (!centered);
            return result;
        }
    }
}