using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Circuit
{
    class EventHandlerList : List<EventHandler>
    {
        public void On(object sender, EventArgs e)
        {
            foreach (EventHandler i in this)
                i(sender, e);
        }
    }
}
