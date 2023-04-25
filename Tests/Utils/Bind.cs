using System;
using System.Collections.Generic;
using System.CommandLine.Binding;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComputerAlgebra;

namespace LiveSPICE.CLI.Utils
{
    internal static class Bind
    {
        public static IValueDescriptor<T> FromServiceProvider<T>() => new ServiceBinder<T>();


        private class ServiceBinder<T> : BinderBase<T>
        {
            protected override T GetBoundValue(BindingContext bindingContext) => (T) bindingContext.GetService(typeof(T));
        }
    }


}
