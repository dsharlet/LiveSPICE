using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace LiveSPICE
{
    class MoveElements : Edit
    {
        List<Element> elements;
        Vector dx;

        public MoveElements(IEnumerable<Element> Elements, Vector dx)
        {
            Debug.Assert(Elements.Any());
            elements = Elements.ToList();
            this.dx = dx;
        }

        public override void Do() { foreach (Element i in elements) i.Move(dx); }
        public override void Undo() { foreach (Element i in elements) i.Move(-dx); }

        public override string ToString() { return "Move"; }
    }

    class RotateElements : Edit
    {
        List<Element> elements;
        int delta;
        Point around;

        public RotateElements(IEnumerable<Element> Elements, int Delta, Point Around)
        {
            Debug.Assert(Elements.Any());
            elements = Elements.ToList();
            delta = Delta; 
            around = Around;
        }

        public override void Do() { foreach (Element i in elements) i.RotateAround(delta, around); }
        public override void Undo() { foreach (Element i in elements) i.RotateAround(-delta, around); }

        public override string ToString() { return "Rotate"; }
    }

    class FlipElements : Edit
    {
        List<Element> elements;
        double at;

        public FlipElements(IEnumerable<Element> Elements, double At)
        {
            Debug.Assert(Elements.Any());
            elements = Elements.ToList();
            at = At;
        }

        public override void Do() { foreach (Element i in elements) i.FlipOver(at); }
        public override void Undo() { foreach (Element i in elements) i.FlipOver(at); }

        public override string ToString() { return "Flip"; }
    }

    class AddElements : Edit
    {
        Schematic target;
        List<Element> elements;

        public AddElements(Schematic Target, params Element[] Elements) : this(Target, Elements.AsEnumerable()) { }
        public AddElements(Schematic Target, IEnumerable<Element> Elements)
        {
            Debug.Assert(Elements.Any());
            target = Target;
            elements = Elements.ToList();
        }

        public override void Do() { foreach (Element i in elements) target.components.Children.Add(i); }
        public override void Undo() { foreach (Element i in elements) target.components.Children.Remove(i); }

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
        Schematic target;
        List<Element> elements;

        public RemoveElements(Schematic Target, IEnumerable<Element> Elements)
        {
            Debug.Assert(Elements.Any());
            target = Target;
            elements = Elements.ToList();
        }

        public override void Do() { foreach (Element i in elements) target.components.Children.Remove(i); }
        public override void Undo() { foreach (Element i in elements) target.components.Children.Add(i); }
        
        public override string ToString()
        {
            if (elements.Count > 1)
                return "Remove " + elements.Count + " Elements";
            else
                return "Remove " + elements.First().ToString();
        }
    }

}
