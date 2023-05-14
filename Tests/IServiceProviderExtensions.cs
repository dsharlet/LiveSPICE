using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LiveSPICE.CLI
{
    internal static class IServiceProviderExtensions
    {
        public static T GetService<T>(this IServiceProvider serviceProvider) => (T) serviceProvider.GetService(typeof(T)); 
    }
}
