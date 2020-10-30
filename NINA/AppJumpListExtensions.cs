using NINA.Profile;
using System.Linq;
using System.Windows.Shell;
using static System.Windows.Shell.JumpList;

namespace NINA {
    internal static class AppJumpListExtensions {
        internal static void RefreshJumpList(
            this App app,
            IProfileService profileService) =>
                SetJumpList(app,
                (GetJumpList(app) ?? new JumpList())
                .WithJumpTaskForEachAvailableProfile(profileService));

        private static JumpList WithJumpTaskForEachAvailableProfile(
            this JumpList jumpList,
            IProfileService profileService) {
            profileService
                .Profiles
                .GroupJoin(
                    jumpList.JumpItems.OfType<JumpTask>(),
                    p => $"/profileid {p.Id}",
                    jt => jt.Arguments,
                    (p, jt) => new { Profile = p, JumpTask = jt.SingleOrDefault() }
                )
                .OrderBy(p => p.Profile.Name)
                .ToList()
                .ForEach(x => {
                    if (x.JumpTask == null) {
                        jumpList.JumpItems.Add(
                            new JumpTask {
                                Title = x.Profile.Name,
                                Description = "Launch N.I.N.A. using profile " + x.Profile.Name,
                                ApplicationPath = typeof(App).Assembly.Location,
                                Arguments = $"/profileid {x.Profile.Id}",
                                WorkingDirectory = System.IO.Directory.GetCurrentDirectory(),
                                CustomCategory = "Profiles",
                            });
                    } else {
                        x.JumpTask.Title = x.Profile.Name;
                        x.JumpTask.Description = "Launch N.I.N.A. using profile " + x.Profile.Name;
                    }
                });
            jumpList.Apply();
            return jumpList;
        }
    }
}