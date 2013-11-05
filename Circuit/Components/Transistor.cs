using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    public abstract class Transistor : Component
    {
        private Terminal c, e, b;
        public override IEnumerable<Terminal> Terminals 
        { 
            get 
            {
                yield return c;
                yield return e;
                yield return b;
            } 
        }
        [Browsable(false)]
        public Terminal Collector { get { return c; } }
        [Browsable(false)]
        public Terminal Emitter { get { return e; } }
        [Browsable(false)]
        public Terminal Base { get { return b; } }
        
        protected string partName = "";
        [Description("Name of this part. This property only affects the schematic symbol, it does not affect the simulation.")]
        [Serialize]
        public string PartName { get { return partName; } set { partName = value; NotifyChanged("PartName"); } }

        public Transistor()
        {
            c = new Terminal(this, "C");
            e = new Terminal(this, "E");
            b = new Terminal(this, "B");
            Name = "Q1";
        }
    }
}
