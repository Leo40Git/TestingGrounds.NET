using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace TestingGrounds.Extensions
{
    public static class SerializationExtensions
    {
        public static void AddTypedValue<T>(this SerializationInfo info, string name, T? value)
            => info.AddValue(name, value, typeof(T));

        public static T? GetValue<T>(this SerializationInfo info, string name)
            => (T?)info.GetValue(name, typeof(T));
    }
}
