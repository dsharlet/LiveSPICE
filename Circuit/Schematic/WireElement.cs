using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Circuit
{
    /// <summary>
    /// Element for wires.
    /// </summary>
    public class WireElement : Element
    {
        protected Terminal anode = new Terminal(null);
        protected Terminal cathode = new Terminal(null);

        public Node Node { get { return anode.ConnectedTo; } set { anode.ConnectTo(value); cathode.ConnectTo(value); } }

        protected Coord a, b;

        public Coord A
        {
            get { return a; }
            set
            {
                if (a == value) return;
                a = value;
                OnLayoutChanged();
            }
        }
        public Coord B
        {
            get { return b; }
            set
            {
                if (b == value) return;
                b = value;
                OnLayoutChanged();
            }
        }                

        public WireElement() { }
        public WireElement(Coord A, Coord B) { a = A; b = B; }

        public override IEnumerable<Terminal> Terminals { get { yield return anode; yield return cathode; } }
        public override Coord MapTerminal(Terminal T)
        {
            if (T == anode) return a;
            else if (T == cathode) return b;
            else throw new ArgumentOutOfRangeException("T");
        }

        public override bool Intersects(Coord x1, Coord x2)
        {
            throw new NotImplementedException();
        }

        public override void Move(Coord dx)
        {
            a += dx;
            b += dx;
            OnLayoutChanged();
        }

        public override Coord LowerBound { get { return new Coord(Math.Min(A.x, B.x), Math.Min(A.y, B.y)); } }
        public override Coord UpperBound { get { return new Coord(Math.Max(A.x, B.x), Math.Max(A.y, B.y)); } }

        public override void RotateAround(int dt, Point at)
        {
            a = (Coord)Point.Round(RotateAround(a, dt, at));
            b = (Coord)Point.Round(RotateAround(b, dt, at));
            OnLayoutChanged();
        }

        public override void FlipOver(double y)
        {
            a.y += (int)Math.Round(2 * (y - a.y));
            b.y += (int)Math.Round(2 * (y - b.y));
            OnLayoutChanged();
        }
        
        public override XElement Serialize()
        {
            XElement X = base.Serialize();
            X.SetAttributeValue("A", a);
            X.SetAttributeValue("B", b);
            return X;
        }

        protected override void OnDeserialize(XElement X)
        {
            a = Coord.Parse(X.Attribute("A").Value);
            b = Coord.Parse(X.Attribute("B").Value);
        }
    }
}
