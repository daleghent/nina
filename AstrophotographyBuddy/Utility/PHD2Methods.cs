using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstrophotographyBuddy.Utility {
    class PHD2Methods {
        /// <summary>
        /// 0: PIXELS TO MOVE: int
        /// 1: RA_ONLY: true/false
        /// Not Variable:
        /// 2: SETTLE PIXELS: int - maximum guide distance for guiding to be considered stable or "in-range"
        /// 3: SETTLE TIME: int - minimum time to be in-range before considering guiding to be stable
        /// 4: SETTLE TIMEOUT: int - time limit before settling is considered to have failed
        /// </summary>
        public static string DITHER = "{{\"method\": \"dither\", \"params\": [{0}, {1}, {{\"pixels\": 1.5, \"time\": 8, \"timeout\": 40}}], \"id\": 5}}\r\n";
        public const string DITHERID = "5";


        public static string LOOP = "{\"method\": \"loop\", \"id\": 1}\r\n";

        public static string AUTO_SELECT_STAR = "{\"method\": \"find_star\", \"params\": [], \"id\": 2}\r\n";

        /// <summary>
        /// 0: RECALIBRATE: true/false
        /// Not Variable:
        /// 1: SETTLE PIXELS: int - maximum guide distance for guiding to be considered stable or "in-range"
        /// 2: SETTLE TIME: int - minimum time to be in-range before considering guiding to be stable
        /// 3: SETTLE TIMEOUT: int - time limit before settling is considered to have failed
        /// 
        /// </summary>
        public static string GUIDE = "{\"method\": \"guide\", \"params\": [{\"pixels\": 1.5, \"time\": 8, \"timeout\": 40}, {0}], \"id\": 3}\r\n";

        public static string CLEAR_CALIBRATION = "{\"method\": \"clear_calibration\", \"params\": [\"both\"], \"id\": 4}\r\n";






        public static string GET_STAR_IMAGE = "{\"method\": \"get_star_image\",\"id\": 97}\r\n";
        public const string GET_STAR_IMAGE_ID = "97";


        public static string GET_EXPOSURE = "{\"method\": \"get_exposure\",\"id\": 98}\r\n";
        public static string GET_APP_STATE = "{\"method\": \"get_app_state\",\"id\": 99}\r\n";
        public const string GET_APP_STATE_ID = "99";
        





        //public static string AUTO_SELECT_STAR = "{\"method\": \"\", \"params\": [\"\"], \"id\": }\r\n"

    }
}
