using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NINA.Core.Utility {
    /// <summary>
    /// Blittable types can be natively represented on the machine hardware, so no marshaling is required to P/Invoke native code.
    /// This class provides a convenience mechanism to cache and return the blittability of a type
    /// 
    /// Usage:
    ///   Blittable<int>.IsBlittable (true)
    ///   Blittable<Point>.IsBlittable (false)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Blittable<T> {
        public static readonly bool IsBlittable = IsBlittableImpl(typeof(T));

        private static bool IsBlittableImpl(Type type) {
            if (type.IsArray) {
                var elem = type.GetElementType();
                return elem.IsValueType && IsBlittableImpl(elem);
            }
            try {
                object instance = FormatterServices.GetUninitializedObject(type);
                GCHandle.Alloc(instance, GCHandleType.Pinned).Free();
                return true;
            } catch (Exception e) {
                return false;
            }
        }
    }
}
