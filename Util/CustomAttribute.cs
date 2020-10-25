using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Util
{
    public static class CustomAttributeExtension
    {
        public static IEnumerable<T> CustomAttributes<T>(this Type This, bool Inherit) where T : Attribute
        {
            return This.GetCustomAttributes(typeof(T), Inherit).Cast<T>();
        }

        public static IEnumerable<T> CustomAttributes<T>(this MemberInfo This, bool Inherit) where T : Attribute
        {
            return This.GetCustomAttributes(typeof(T), Inherit).Cast<T>();
        }

        public static IEnumerable<T> CustomAttributes<T>(this ParameterInfo This, bool Inherit) where T : Attribute
        {
            return This.GetCustomAttributes(typeof(T), Inherit).Cast<T>();
        }

        public static IEnumerable<T> CustomAttributes<T>(this Type This) where T : Attribute { return This.CustomAttributes<T>(true); }
        public static IEnumerable<T> CustomAttributes<T>(this MemberInfo This) where T : Attribute { return This.CustomAttributes<T>(true); }
        public static IEnumerable<T> CustomAttributes<T>(this ParameterInfo This) where T : Attribute { return This.CustomAttributes<T>(true); }

        public static T CustomAttribute<T>(this Type This, bool Inherit) where T : Attribute { return This.CustomAttributes<T>(Inherit).FirstOrDefault(); }
        public static T CustomAttribute<T>(this MemberInfo This, bool Inherit) where T : Attribute { return This.CustomAttributes<T>(Inherit).FirstOrDefault(); }
        public static T CustomAttribute<T>(this ParameterInfo This, bool Inherit) where T : Attribute { return This.CustomAttributes<T>(Inherit).FirstOrDefault(); }

        public static T CustomAttribute<T>(this Type This) where T : Attribute { return This.CustomAttribute<T>(true); }
        public static T CustomAttribute<T>(this MemberInfo This) where T : Attribute { return This.CustomAttribute<T>(true); }
        public static T CustomAttribute<T>(this ParameterInfo This) where T : Attribute { return This.CustomAttribute<T>(true); }
    }
}
