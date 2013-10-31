using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SyMath;
using System.ComponentModel;

namespace Circuit
{
    [TypeConverter(typeof(ModelConverter<DiodeModel>))]
    public abstract class DiodeModel : Model
    {
        public virtual bool IsLed { get { return false; } }

        public DiodeModel(string Name) : base(Name) { }

        public abstract Expression Evaluate(Expression V);

        public static List<DiodeModel> Models { get { return Model.GetModels<DiodeModel>(); } }

        static DiodeModel()
        {
            Models.Add(new ShockleyDiodeModel("1N270", 1e-6, 1.0));
            Models.Add(new ShockleyDiodeModel("1N4001", 1e-12, 1.0));
        }
    }

    /// <summary>
    /// Shockley diode model: http://en.wikipedia.org/wiki/Diode_modelling#Shockley_diode_model
    /// </summary>
    public class ShockleyDiodeModel : DiodeModel
    {
        // Shockley diode model parameters.
        protected double _is; // A
        protected double _n;
        protected bool led = false;

        public double IS { get { return _is; } set { _is = value; } }
        public double n { get { return _n; } set { _n = value; } }
        public bool Led { get { return led; } set { led = value; } }
        
        public ShockleyDiodeModel(string Name, double IS, double n) : base(Name)
        {
            _is = IS;
            _n = n;
        }

        public override bool IsLed { get { return Led; } }
        
        public override Expression Evaluate(Expression V)
        {
            return IS * (Call.Exp(V / (n * Component.VT)) - 1);
        }

        public override string ToString() { return base.ToString() + " (Shockley)"; }
    };

    [CategoryAttribute("Standard")]
    [DisplayName("Diode")]
    [DefaultProperty("Model")]
    [Description("Diode.")]
    public class Diode : TwoTerminal
    {
        protected Model model = DiodeModel.Models.First();
        [Serialize]
        public DiodeModel Model { get { return (DiodeModel)model; } set { model = value; NotifyChanged("Model"); } }

        public Diode() { Name = "D1"; }

        public override void Analyze(ModifiedNodalAnalysis Mna)
        {
            // Vac = Va - Vc
            Expression Vac = Mna.AddNewUnknownEqualTo("V" + Name, V);
            // Evaluate the model.
            Mna.AddPassiveComponent(Anode, Cathode, Mna.AddNewUnknownEqualTo("i" + Name, Model.Evaluate(Vac)));
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

        public override void LayoutSymbol(SymbolLayout Sym) { LayoutSymbol(Sym, Anode, Cathode, Model.IsLed, () => Name, () => Model.Name); }
    }
}
