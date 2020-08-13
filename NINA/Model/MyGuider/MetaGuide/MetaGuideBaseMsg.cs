using System;
using System.Reflection;
using System.Text;

namespace NINA.Model.MyGuider.MetaGuide {
    public class MetaGuideBaseMsg {
        public override String ToString() {
            Type objType = this.GetType();
            PropertyInfo[] propertyInfoList = objType.GetProperties();
            StringBuilder result = new StringBuilder();
            foreach (PropertyInfo propertyInfo in propertyInfoList) {
                result.AppendFormat("{0}={1} ", propertyInfo.Name, propertyInfo.GetValue(this));
            }

            return result.ToString();
        }
    }
}
