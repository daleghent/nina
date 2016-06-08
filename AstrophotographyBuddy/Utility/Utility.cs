using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AstrophotographyBuddy.Utility {
    class Utility {
        public static T[] flattenArray<T>(Array arr) {
            int width = arr.GetLength(0);
            int height = arr.GetLength(1);
            T[] flatArray = new T[width * height];
            T val;
            int idx = 0;
            for (int i = 0; i < height; i++) {
                for (int j = 0; j < width; j++) {
                    val = (T)Convert.ChangeType(arr.GetValue(j, i), typeof(T));

                    flatArray[idx] = val;
                    idx++;
                }
            }
            return flatArray;
        }
    }
}
