using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public abstract class DiodeModel
    {
        public abstract Expression Evaluate(Expression V);

        public virtual bool IsLed() { return false; }
    }

    /// <summary>
    /// Shockley diode model: http://en.wikipedia.org/wiki/Diode_modelling#Shockley_diode_model
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public class ShockleyDiodeModel : DiodeModel
    {
        // Shockley diode model parameters.
        protected double _is = 6.734e-15; // A
        protected double vt = 25.85e-3; // V
        protected double _n = 1.0;
        protected bool led = false;

        public double IS { get { return _is; } set { _is = value; } }
        public double VT { get { return vt; } set { vt = value; } }
        public double n { get { return _n; } set { _n = value; } }
        public bool Led { get { return led; } set { led = value; } }

        public ShockleyDiodeModel(double IS, double VT, double n)
        {
            _is = IS;
            vt = VT;
            _n = n;
        }

        public ShockleyDiodeModel() { }

        public override bool IsLed() { return Led; }
        
        public override Expression Evaluate(Expression V)
        {
            return IS * (Call.Exp(V / (n * VT)) - 1);
        }
    };

    [CategoryAttribute("Standard")]
    [DisplayName("Diode")]
    public class Diode : TwoTerminal
    {
        protected DiodeModel model = new ShockleyDiodeModel();
        public DiodeModel Model { get { return model; } set { model = value; NotifyChanged("Model"); } }

        public Diode() { Name = "D1"; }

        public override void Analyze(ICollection<Equal> Mna, ICollection<Expression> Unknowns)
        {
            // Make a new unknown for Va - Vc to reduce the number of non-linear variables.

            // Vac = Va - Vc
            Expression Vac = DependentVariable("V" + Name, t);
            Mna.Add(Equal.New(Vac, V));
            Unknowns.Add(Vac);

            Expression i = model.Evaluate(Vac);
            Anode.i = i;
            Cathode.i = -i;
        }

        protected override void DrawSymbol(SymbolLayout Sym)
        {
            Sym.AddWire(Anode, new Coord(0, 10));
            Sym.AddWire(Cathode, new Coord(0, -10));

            Sym.AddLoop(EdgeType.Black,
                new Coord(-10, 10),
                new Coord(10, 10),
                new Coord(0, -10));
            Sym.AddLine(EdgeType.Black, new Coord(-10, -10), new Coord(10, -10));

            if (model.IsLed())
            {
                Sym.DrawArrow(EdgeType.Black, new Coord(-12, 5), new Coord(-20, -3), 0.2);
                Sym.DrawArrow(EdgeType.Black, new Coord(-8, -2), new Coord(-16, -10), 0.2);
            }

            Sym.DrawText(Name, new Coord(10, 0), Alignment.Near, Alignment.Center);
        }
    }
}
