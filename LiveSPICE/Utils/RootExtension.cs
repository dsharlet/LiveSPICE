using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Xaml;

namespace LiveSPICE
{
    // http://www.codeproject.com/Tips/612994/Binding-with-Properties-defined-in-Code-Behind
    public class RootExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            IRootObjectProvider provider = serviceProvider.GetService
            (typeof(IRootObjectProvider)) as IRootObjectProvider;
            if (provider != null)
            {
                return provider.RootObject;
            }

            return null;
        }
    }    
}
