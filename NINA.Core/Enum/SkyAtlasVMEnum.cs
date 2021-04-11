using NINA.Core.Utility;
using System.ComponentModel;

namespace NINA.Core.Enum {

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum SkyAtlasOrderByFieldsEnum {

        [Description("LblSize")]
        SIZEMAX,

        [Description("LblApparentMagnitude")]
        MAGNITUDE,

        [Description("LblConstellation")]
        CONSTELLATION,

        [Description("LblRA")]
        RA,

        [Description("LblDec")]
        DEC,

        [Description("LblSurfaceBrightness")]
        SURFACEBRIGHTNESS,

        [Description("LblObjectType")]
        DSOTYPE
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum SkyAtlasOrderByDirectionEnum {

        [Description("LblDescending")]
        DESC,

        [Description("LblAscending")]
        ASC
    }
}