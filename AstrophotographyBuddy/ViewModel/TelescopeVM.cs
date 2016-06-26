using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstrophotographyBuddy.ViewModel {
    class TelescopeVM : BaseVM {
        public TelescopeVM() {
            Name = "Telescope";
            ImageURI = @"/AstrophotographyBuddy;component/Resources/Telescope.png";
        }
    }
}
