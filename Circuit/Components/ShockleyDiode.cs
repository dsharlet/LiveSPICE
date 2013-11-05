using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    /// <summary>
    /// Shockley diode model: http://en.wikipedia.org/wiki/Diode_modelling#Shockley_diode_model
    /// </summary>
    [Category("Diodes")]
    [DisplayName("Diode (Shockley)")]
    [Description("Diode implemented using the Shockley diode model.")]
    public class ShockleyDiode : TwoTerminal
    {
        protected Quantity _is = new Quantity(1e-12, Units.A);
        [Description("Saturation current.")]
        [Serialize]
        public Quantity IS { get { return _is; } set { if (_is.Set(value)) NotifyChanged("IS"); } }

        protected double _n = 1.0;
        [Description("Idealization factor.")]
        [Serialize]
        public double n { get { return _n; } set { _n = value; NotifyChanged("n"); } }

        protected bool led = false;
        [Description("Indicates that this part is an LED. This property only affects the schematic symbol, it does not affect the simulation.")]
        [Serialize]
        public bool IsLed { get { return led; } set { led = value; NotifyChanged("IsLed"); } }

        protected string partName = "";
        [Description("Part name/number. This property only affects the schematic symbol, it does not affect the simulation.")]
        [Serialize]
        public string PartName { get { return partName; } set { partName = value; NotifyChanged("PartName"); } }

        public ShockleyDiode() { Name = "D1"; }

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            // V = Va - Vc
            Expression Vac = Mna.AddNewUnknownEqualTo("V" + Name, V);
            
            Expression i = (Expression)IS * (Call.Exp(Vac / (n * VT)) - 1);

            // Evaluate the model.
            Mna.AddPassiveComponent(Anode, Cathode, Mna.AddNewUnknownEqualTo("i" + Name, i));
        }

        public static void LayoutSymbol(SymbolLayout Sym, Terminal A, Terminal C, bool IsLed, Func<string> Name, Func<string> Part)
        {
            Sym.AddTerminal(A, new Coord(0, 20));
            Sym.AddWire(A, new Coord(0, 10));

            Sym.AddTerminal(C, new Coord(0, -20));
            Sym.AddWire(C, new Coord(0, -10));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(10, 10),
                new Coord(0, -10));
            Sym.AddLine(EdgeType.Black, new Coord(-10, -10), new Coord(10, -10));

            if (IsLed)
            {
                Sym.DrawArrow(EdgeType.Black, new Coord(-12, 5), new Coord(-20, -3), 0.2);
                Sym.DrawArrow(EdgeType.Black, new Coord(-8, -2), new Coord(-16, -10), 0.2);
            }

            if (Part != null)
                Sym.DrawText(Part, new Coord(10, 4), Alignment.Near, Alignment.Near);
            Sym.DrawText(Name, new Coord(10, -4), Alignment.Near, Alignment.Far);
        }

        public override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, Anode, Cathode, IsLed, () => Name, () => PartName); }

        public static IEnumerable<Component> Parts = new Component[]
        {
            new ModelSpecialization(new ShockleyDiode() { PartName = "",        IS = 1e-12m,    n = 1.0 }) { DisplayName = "Diode", Category = "Standard", Description = "Generic diode." },

            new ModelSpecialization(new ShockleyDiode() { PartName = "",        IS = 1e-6m,     n = 1.0 }) { DisplayName = "Germanium Diode", Category = "Diodes", Description = "Generic germanium diode." },
            new ModelSpecialization(new ShockleyDiode() { PartName = "",        IS = 1e-12m,    n = 1.0 }) { DisplayName = "Silicon Diode", Category = "Diodes", Description = "Generic silicon diode." },
        };

        private static KeyValuePair<string, object> KV(string Key, object Value) { return new KeyValuePair<string, object>(Key, Value); }
    }
}
