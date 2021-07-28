#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Sequencer.Container;
using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NINA.Sequencer.Trigger;
using NINA.Sequencer.Trigger.MeridianFlip;

namespace NINA.Sequencer.Utility {

    public class ItemUtility {

        public static (Coordinates, double) RetrieveContextCoordinates(ISequenceContainer parent) {
            if (parent != null) {
                var container = parent as IDeepSkyObjectContainer;
                if (container != null) {
                    return (container.Target.InputCoordinates.Coordinates.Transform(container.Target.InputCoordinates.Coordinates.Epoch), container.Target.Rotation);
                } else {
                    return RetrieveContextCoordinates(parent.Parent);
                }
            } else {
                return (null, 0);
            }
        }

        public static bool IsInRootContainer(ISequenceContainer parent) {
            return GetRootContainer(parent) != null;
        }

        public static ISequenceRootContainer GetRootContainer(ISequenceContainer parent) {
            if (parent != null) {
                if (parent is ISequenceRootContainer rootContainer) {
                    return rootContainer;
                }
                return GetRootContainer(parent.Parent);
            } else {
                return null;
            }
        }

        /// <summary>
        /// Checks if the current context or one of its parents contains a meridian flip trigger and returns the estimated time of the flip
        /// </summary>
        /// <param name="context">current context instruction set</param>
        /// <returns></returns>
        public static DateTime GetMeridianFlipTime(ISequenceContainer context) {
            if (context == null) { return DateTime.MinValue; }

            if (context is ITriggerable triggerable) {
                var snapshot = triggerable.GetTriggersSnapshot();
                if (snapshot?.Count > 0) {
                    var item = snapshot.FirstOrDefault(x => typeof(MeridianFlipTrigger).IsAssignableFrom(x.GetType()));
                    if (item != null) {
                        return ((MeridianFlipTrigger)item).LatestFlipTime;
                    }
                }
            }
            if (context.Parent != null) {
                return GetMeridianFlipTime(context.Parent);
            } else {
                return DateTime.MinValue;
            }
        }

        /// <summary>
        /// Checks if the current context or one of its parents contains a meridian flip trigger and checks if the remaining time is enough to complete prior to the flip
        /// </summary>
        /// <param name="context">current context instruction set</param>
        /// <param name="estimatedDuration">estimated duration of the item to run</param>
        /// <returns>
        /// true: is too close and can't finish before flip
        /// false: can finish before flip or no meridian flip trigger is present
        /// </returns>
        public static bool IsTooCloseToMeridianFlip(ISequenceContainer context, TimeSpan estimatedDuration) {
            var estimatedItemFinishTime = DateTime.Now + TimeSpan.FromSeconds(estimatedDuration.TotalSeconds * 1.5);

            var flipTime = GetMeridianFlipTime(context);

            if (flipTime > DateTime.MinValue && estimatedItemFinishTime > flipTime) {
                return true;
            }
            return false;
        }
    }
}