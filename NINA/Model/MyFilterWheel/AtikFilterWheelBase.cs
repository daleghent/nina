using NINA.Profile;
using NINA.Utility;
using NINA.Utility.AtikSDK;
using System;
using System.Collections;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Model.MyFilterWheel {
    internal abstract class AtikFilterWheelBase : BaseINPC, IFilterWheel {
        protected readonly IProfileService profileService;

        public AtikFilterWheelBase(IProfileService profileService) {
            this.profileService = profileService;
        }

        public short InterfaceVersion => 1;

        public int[] FocusOffsets => Filters.Select((x) => x.FocusOffset).ToArray();

        public string[] Names => Filters.Select((x) => x.Name).ToArray();

        public abstract short Position { get; set; }

        public ArrayList SupportedActions => new ArrayList();

        public AsyncObservableCollection<FilterInfo> Filters {
            get {
                var filtersList = profileService.ActiveProfile.FilterWheelSettings.FilterWheelFilters;
                int i = filtersList.Count();
                int positions = GetEfwPositions();

                if (positions < i) {
                    /* Too many filters defined. Truncate the list */
                    for (; i > positions; i--) {
                        filtersList.RemoveAt(i - 1);
                    }
                } else if (positions > i) {
                    /* Too few filters defined. Add missing filter names using Slot <#> format */
                    for (; i <= positions; i++) {
                        var filter = new FilterInfo(string.Format($"Slot {i}"), 0, (short)i);
                        filtersList.Add(filter);
                    }
                }

                return filtersList;
            }
        }

        protected abstract int GetEfwPositions();

        public bool HasSetupDialog => false;

        public abstract string Id { get; }

        public abstract string Name { get; }

        public string Category => "Atik";

        public abstract bool Connected { get; }

        public abstract string Description { get; }

        public string DriverInfo => AtikCameraDll.DriverName;

        public string DriverVersion => AtikCameraDll.DriverVersion;

        public abstract Task<bool> Connect(CancellationToken token);

        public abstract void Disconnect();

        public void SetupDialog() {
            throw new NotImplementedException();
        }
    }
}
