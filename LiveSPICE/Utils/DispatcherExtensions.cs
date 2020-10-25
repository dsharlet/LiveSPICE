using System;
using System.Windows.Threading;

namespace LiveSPICE
{
    static class DispatcherExtensions
    {
        public static object Invoke(this Dispatcher This, Action Call)
        {
            return This.Invoke(Call, null);
        }

        public static DispatcherOperation InvokeAsync(this Dispatcher This, Action Call, DispatcherPriority Priority)
        {
            return This.BeginInvoke(Call, Priority);
        }

        public static DispatcherOperation InvokeAsync(this Dispatcher This, Action Call)
        {
            return This.BeginInvoke(Call);
        }
    }
}
