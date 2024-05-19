using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gadgethunt.lib
{
    internal class Filterers
    {

        public static bool HasMethodNameReturningType(string methodName, Type typeToReturn)
        {
            var parseMethod = typeToReturn.GetMethod(methodName);
            return parseMethod != null &&
                   parseMethod.ReturnType == typeToReturn;
        }

        public static bool HasMethodWithTypeConverterAttribute(Type type)
        {
            return type.GetCustomAttributes(typeof(TypeConverterAttribute), false).Length > 0;
        }
    }
}
