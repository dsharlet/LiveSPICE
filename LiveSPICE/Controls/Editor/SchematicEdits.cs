using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LiveSPICE
{
    class MoveElements : Edit
    {
        List<Circuit.Element> elements;
        Circuit.Coord dx;

        public MoveElements(IEnumerable<Circuit.Element> Elements, Circuit.Coord dx)
        {
            Debug.Assert(Elements.Any());
            elements = Elements.ToList();
            this.dx = dx;
        }

        public override void Do() { foreach (Circuit.Element i in elements) i.Move(dx); }
        public override void Undo() { foreach (Circuit.Element i in elements) i.Move(-dx); }

        public override string ToString() { return "Move"; }
    }

    class RotateElements : Edit
    {
        List<Circuit.Element> elements;
        int delta;
        Circuit.Point around;

        public RotateElements(IEnumerable<Circuit.Element> Elements, int Delta, Circuit.Point Around)
        {
            Debug.Assert(Elements.Any());
            elements = Elements.ToList();
            delta = Delta;
            around = Around;
        }

        public override void Do() { foreach (Circuit.Element i in elements) i.RotateAround(delta, around); }
        public override void Undo() { foreach (Circuit.Element i in elements) i.RotateAround(-delta, around); }

        public override string ToString() { return "Rotate"; }
    }

    class FlipElements : Edit
    {
        List<Circuit.Element> elements;
        double at;

        public FlipElements(IEnumerable<Circuit.Element> Elements, double At)
        {
            Debug.Assert(Elements.Any());
            elements = Elements.ToList();
            at = At;
        }

        public override void Do() { foreach (Circuit.Element i in elements) i.FlipOver(at); }
        public override void Undo() { foreach (Circuit.Element i in elements) i.FlipOver(at); }

        public override string ToString() { return "Flip"; }
    }

    class AddElements : Edit
    {
        Circuit.Schematic target;
        List<Circuit.Element> elements;

        public AddElements(Circuit.Schematic Target, params Circuit.Element[] Elements) : this(Target, Elements.AsEnumerable()) { }
        public AddElements(Circuit.Schematic Target, IEnumerable<Circuit.Element> Elements)
        {
            Debug.Assert(Elements.Any());
            target = Target;
            elements = Elements.ToList();
        }

        public override void Do() { target.Add(elements); }
        public override void Undo() { target.Remove(elements); }

        public override string ToString()
        {
            if (elements.Count > 1)
                return "Add " + elements.Count + " Elements";
            else
                return "Add " + elements.First().ToString();
        }
    }

    class RemoveElements : Edit
    {
        Circuit.Schematic target;
        List<Circuit.Element> elements;

        public RemoveElements(Circuit.Schematic Target, IEnumerable<Circuit.Element> Elements)
        {
            Debug.Assert(Elements.Any());
            target = Target;
            elements = Elements.ToList();
        }

        public override void Do() { target.Remove(elements); }
        public override void Undo() { target.Add(elements); }

        public override string ToString()
        {
            if (elements.Count > 1)
                return "Remove " + elements.Count + " Elements";
            else
                return "Remove " + elements.First().ToString();
        }
    }

}
