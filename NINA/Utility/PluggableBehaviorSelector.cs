using NINA.Core.Interfaces;
using NINA.Core.Utility;
using NINA.Plugin.Interfaces;
using NINA.Profile.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace NINA.Utility {
    /// <summary>
    /// A PluggableBehaviorSelector instance exists for each pluggable interface. It encapsulates a list of default and plugged instances as well as saving/loading
    /// the selected type from the profile, 
    /// </summary>
    /// <typeparam name="T">The pluggable interface type</typeparam>
    /// <typeparam name="DefaultT">The default implementation type provided by NINA</typeparam>
    public class PluggableBehaviorSelector<T, DefaultT> : BaseINPC, IPluggableBehaviorSelector<T>
        where T : class, IPluggableBehavior
        where DefaultT : T {
        private readonly IProfileService profileService;
        private readonly DefaultT ninaDefault;

        public PluggableBehaviorSelector(IProfileService profileService, DefaultT ninaDefault) {
            this.profileService = profileService;
            this.ninaDefault = ninaDefault;
            Behaviors = new AsyncObservableCollection<T>();
            Behaviors.Add(ninaDefault);
        }

        public Type GetInterfaceType() {
            return typeof(T);
        }

        private void Behaviors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            RaisePropertyChanged(nameof(Behaviors));
            RaisePropertyChanged(nameof(SelectedBehavior));
        }

        private AsyncObservableCollection<T> behaviors;

        public AsyncObservableCollection<T> Behaviors {
            get => behaviors;
            set {
                if (behaviors != value) {
                    if (behaviors != null) {
                        behaviors.CollectionChanged -= Behaviors_CollectionChanged;
                    }
                    behaviors = value;
                    if (behaviors != null) {
                        behaviors.CollectionChanged += Behaviors_CollectionChanged;
                    }
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(SelectedBehavior));
                }
            }
        }

        public T SelectedBehavior {
            get => GetBehavior();
            set {
                if (value == null) {
                    throw new ArgumentException("SelectedBehavior cannot be set to null", "SelectedBehavior");
                }
                if (!Behaviors.Any(b => b.ContentId == value.ContentId)) {
                    throw new ArgumentException($"{value.ContentId} is not a plugged {typeof(T).FullName} behavior", "SelectedBehavior");
                }

                var existingSelection = profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviors.FirstOrDefault(p => p.Key == typeof(T).FullName);
                if (!String.IsNullOrEmpty(existingSelection.Key)) {
                    profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviors.Remove(existingSelection);
                }
                profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviors.Add(new KeyValuePair<string, string>(typeof(T).FullName, value.ContentId));
                RaisePropertyChanged();
            }
        }

        public T GetBehavior(string pluggableBehaviorContentId) {
            if (String.IsNullOrEmpty(pluggableBehaviorContentId)) {
                return ninaDefault;
            }

            var selected = behaviors.FirstOrDefault(b => b.ContentId == pluggableBehaviorContentId);
            if (selected != null) {
                return selected;
            }
            return ninaDefault;
        }

        public T GetBehavior() {
            profileService.ActiveProfile.ApplicationSettings.SelectedPluggableBehaviorsLookup.TryGetValue(typeof(T).FullName, out string contentId);
            return GetBehavior(contentId);
        }

        public void AddBehavior(object behavior) {
            var typedBehavior = behavior as T;
            if (behavior == null) {
                throw new ArgumentException($"Can't add behavior {behavior.GetType().FullName} since it doesn't implement {typeof(T).FullName}");
            }

            Behaviors.Add(typedBehavior);
        }
    }
}