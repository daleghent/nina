using NINA.Utility;
using System;

namespace NINA.Model.MyGuider.MetaGuide {
    public class MetaGuideMountMsg : MetaGuideBaseMsg {
        private MetaGuideMountMsg() { }

        public static MetaGuideMountMsg Create(string[] args) {
            if (args.Length < 6) {
                return null;
            }
            try {
                return new MetaGuideMountMsg() {
                    MountName = args[5]
                };
            } catch (Exception ex) {
                Logger.Error(ex);
                return null;
            }
        }
        public string MountName { get; private set; }
    }
}
