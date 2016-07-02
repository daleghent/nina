using AstrophotographyBuddy.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstrophotographyBuddy.ViewModel {
    class PHD2VM :BaseVM {
        public PHD2VM() {
            Name = "PHD2";
            ImageURI = @"/AstrophotographyBuddy;component/Resources/PHD2.png";            
        }

        public PHD2Client PHD2Client {
            get {
                return Utility.Utility.PHDClient;
            }
        }




    }
}
