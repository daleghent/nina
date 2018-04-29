﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Utility.Enum
{
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum WeatherDataEnum {
        [Description("LblOpenWeatherMapOrg")]
        OPENWEATHERMAP
    }
}
