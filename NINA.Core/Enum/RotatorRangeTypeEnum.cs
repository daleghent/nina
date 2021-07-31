using NINA.Core.Model;
using NINA.Core.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum RotatorRangeTypeEnum {

        [TooltipDescription("LblRotatorRangeFull", "LblRotatorRangeFullTooltip")]
        FULL,

        [TooltipDescription("LblRotatorRangeHalf", "LblRotatorRangeHalfTooltip")]
        HALF,

        [TooltipDescription("LblRotatorRangeQuarter", "LblRotatorRangeQuarterTooltip")]
        QUARTER,
    }
}
