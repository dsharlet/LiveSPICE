using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyMath;

namespace Circuit
{
    /// <summary>
    /// Represents a parameter in a circuit analysis.
    /// </summary>
    public abstract class Parameter
    {
        private SyMath.Expression name;
        public SyMath.Expression Name { get { return name; } }

        private double def = 1.0;
        public double Default { get { return def; } }

        public Parameter(SyMath.Expression Name, double Default) { name = Name; def = Default; }

        public override string ToString() { return Name.ToString(); }
    }

    /// <summary>
    /// Continuous parameter value in [0, 1].
    /// </summary>
    public class RangeParameter : Parameter
    {
        private bool log;
        public bool Logarithmic { get { return log; } }


        public RangeParameter(string Name, bool Log) : this(Name, 1.0, Log) { }
        public RangeParameter(string Name, double Default, bool Log) : base(Name, Default) { log = Log; }

        public static Expression New(string Name, double Default, bool Log)
        {
            List<Expression> args = new List<Expression>();
            args.Add(Name);
            if (Default != 1.0)
                args.Add(Constant.New(Default));
            if (Log != false)
                args.Add(Constant.New(Log));
            return Call.New(ExprFunction.New("P", args.Select((x, i) => Variable.New(i.ToString()))), args);
        }
    }
}
