using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Reflection.Emit;
using SyMath;
using Roslyn.Compilers.CSharp;

namespace Circuit
{
    /// <summary>
    /// Simulate a circuit.
    /// </summary>
    public class Simulation
    {
        class Node
        {
            public Expression step;
            public float V = 0.0f;

            public Node(Expression Step) { step = Step; }
        }
        private List<KeyValuePair<Expression, Node>> nodes;

        protected Expression _t = Constant.Zero;
        public Expression t { get { return _t; } }

        protected Expression _T;
        public Expression T { get { return _T; } }

        private static Expression t0 = Variable.New("t0");

        protected int iterations = 1;
        /// <summary>
        /// Get or set the maximum number of iterations to use when numerically solving equations.
        /// </summary>
        public int Iterations { get { return iterations; } set { iterations = value; } }
        protected int oversample = 1;
        /// <summary>
        /// Get or set the oversampling factor for the simulation.
        /// </summary>
        public int Oversample { get { return oversample; } set { oversample = value; } }

        // Shorthand for df/dx.
        private static Expression D(Expression f, Expression x) { return f.Differentiate(x); }
        // Solve a system of equations, possibly including linear differential equations.
        private static List<Arrow> Solve(List<Equal> f, List<Expression> y, Expression t, Expression t0, Expression h, IntegrationMethod method, int iterations)
        {
            List<Expression> dydt = y.Select(i => D(i, t)).ToList();

            List<Arrow> step = new List<Arrow>();
            // Try solving for y algebraically.
            List<Arrow> linear = f.Solve(y);
            // Only accept independent solutions.
            linear.RemoveAll(i => i.Right.IsFunctionOf(dydt.Append(i.Left)));
            step.AddRange(linear);
            // Substitute the solutions found.
            f = f.Evaluate(linear).OfType<Equal>().ToList();
            y.RemoveAll(i => step.Find(j => j.Left.Equals(i)) != null);
            
            // Solve for any non-differential functions remaining and substitute them into the system.
            List<Arrow> nondifferential = f.Where(i => !i.IsFunctionOf(dydt)).Solve(y);
            List<Equal> df = f.Evaluate(nondifferential).OfType<Equal>().ToList();
            // Solve the differential equations.
            List<Arrow> differentials = df.NDSolve(y, t, t0, h, method, iterations);
            step.AddRange(differentials);
            // Replace differentials with approximations.
            f = f.Evaluate(y.Select(i => Arrow.New(D(i, t), (i - i.Evaluate(t, t0)) / h))).Cast<Equal>().ToList();
            f = f.Evaluate(differentials).OfType<Equal>().ToList();
            y.RemoveAll(i => step.Find(j => j.Left.Equals(i)) != null);

            // Try solving for numerical solutions.
            List<Arrow> nonlinear = f.NSolve(y.Select(i => Arrow.New(i, i.Evaluate(t, t0))), iterations);
            nonlinear.RemoveAll(i => i.Right.IsFunctionOf(dydt));
            step.AddRange(nonlinear);

            return step;
        }

        /// <summary>
        /// Create a simulation for the given circuit.
        /// </summary>
        /// <param name="C">Circuit to simulate.</param>
        /// <param name="T">Sampling period.</param>
        /// <returns></returns>
        public Simulation(Circuit C, Quantity F)
        {
            _T = 1.0 / ((Expression)F * oversample);

            // Compute the KCL equations for this circuit.
            List<Equal> kcl = C.Analyze();
            
            // Find the expression for the next timestep via the trapezoid method (ala bilinear transform).
            List<Arrow> step = Solve(
                kcl.ToList(),
                C.Nodes.Select(i => i.V).Cast<Expression>().ToList(), 
                Component.t, t0, 
                _T, 
                IntegrationMethod.Trapezoid, 
                iterations);

            // Create the nodes.
            nodes = step.Select(i => new KeyValuePair<Expression, Node>(
                (Expression)Call.New(((Call)i.Left).Target, Component.t), 
                new Node(i.Right))).ToList();
        }

        // Inefficient wrapper around process, useful for testing.
        public Dictionary<Expression, Expression> Process(IDictionary<Expression, Expression> Input)
        {
            Expression _t0 = _t;
            _t += _T;

            List<Arrow> state = new List<Arrow>();
            // Set previous state.
            foreach (KeyValuePair<Expression, Expression> i in Input)
                state.Add(Arrow.New(i.Key.Evaluate(Component.t, t0), i.Value));
            foreach (KeyValuePair<Expression, Node> i in nodes)
                state.Add(Arrow.New(i.Key.Evaluate(Component.t, t0), i.Value.V));
            foreach (KeyValuePair<Expression, Expression> i in Input)
                state.Add(Arrow.New(i.Key, i.Value));

            List<Arrow> globals = new List<Arrow>();
            // Set global state variables.
            globals.Add(Arrow.New(Component.t, _t));
            globals.Add(Arrow.New(t0, _t0));
            globals.Add(Arrow.New(Component.T, T));

            // Evaluate timestep.
            foreach (KeyValuePair<Expression, Node> i in nodes)
            {
                Expression V = i.Value.step.Evaluate(state.Concat(globals));
                i.Value.V = (float)V;
                state.Add(Arrow.New(i.Key, V));
            }

            return nodes.ToDictionary(i => i.Key, i => (Expression)i.Value.V);
        }
        public Dictionary<Expression, Expression> Process(params Arrow[] Input) { return Process(Input.ToDictionary(i => i.Left, i => i.Right)); }

        static TypeSyntax VoidType = Syntax.PredefinedType(Syntax.Token(SyntaxKind.VoidKeyword));
        static TypeSyntax FloatType = Syntax.PredefinedType(Syntax.Token(SyntaxKind.FloatKeyword));

        //protected MethodDeclarationSyntax ProcessOneSample(ClassDeclarationSyntax Class, IEnumerable<Expression> Input)
        //{
        //    // Create the declaration for the method.
        //    MethodDeclarationSyntax f = Syntax.MethodDeclaration(FloatType, "ProcessOneSample");

        //    // Map expressions to identifiers in the syntax tree.
        //    Dictionary<Expression, IdentifierNameSyntax> values = new Dictionary<Expression, IdentifierNameSyntax>();

        //    // Create parameters for method.
        //    SeparatedSyntaxList<ParameterSyntax> parameters = new SeparatedSyntaxList<ParameterSyntax>();

        //    values[t0] = Syntax.IdentifierName("t0");
        //    ParameterSyntax pt0 = Syntax.Parameter(Syntax.Identifier("t0")).WithType(FloatType);
        //    parameters.Add(pt0);
            
        //    values[Component.T] = Syntax.IdentifierName("T");
        //    ParameterSyntax pT = Syntax.Parameter(Syntax.Identifier("T")).WithType(FloatType);
        //    parameters.Add(pT);

        //    foreach (Expression i in Input)
        //    {
        //        values[i] = Syntax.IdentifierName(i.ToString());
        //        ParameterSyntax arg = Syntax.Parameter(Syntax.Identifier(i.ToString())).WithType(FloatType);
        //        parameters.Add(arg);
        //    }

        //    // Statements for the body of the method.
        //    List<StatementSyntax> body = new List<StatementSyntax>();
            
        //    // Declare t.
        //    VariableDeclarationSyntax vt = Syntax.VariableDeclaration(FloatType).WithVariables(
        //        Syntax.SeparatedList<VariableDeclaratorSyntax>(Syntax.VariableDeclarator("t")));
        //    values[Component.t] = Syntax.IdentifierName("t");

        //    // Add T to t.
        //    body.Add(Syntax.LocalDeclarationStatement(vt));
        //    body.Add(Syntax.ExpressionStatement(Syntax.BinaryExpression(SyntaxKind.AssignExpression, values[t], 
        //        Syntax.BinaryExpression(SyntaxKind.AddExpression, values[t0], values[T]))));



        //    // Return the new value of t.
        //    body.Add(Syntax.ReturnStatement(values[t]));

        //    Class.AddMembers(f
        //        .WithParameterList(Syntax.ParameterList(parameters))
        //        .WithBody(Syntax.Block(body)));
        //}

        // Process some samples. Requested nodes are stored in Output.
        public void Process(int SampleCount, IDictionary<Expression, float[]> Input, IDictionary<Expression, float[]> Output)
        {
            for (int n = 0; n < SampleCount; ++n)
            {
                Dictionary<Expression, Expression> input = Input.ToDictionary(i => i.Key, i => (Expression)i.Value[n]);
                Dictionary<Expression, Expression> output = Process(input);
                foreach (KeyValuePair<Expression, float[]> i in Output)
                    i.Value[n] = (float)output[i.Key];
            }
        }

        public void Process(Expression InputNode, float[] InputSamples, IDictionary<Expression, float[]> Output)
        {
            Process(InputSamples.Length, new Dictionary<Expression, float[]>() { { InputNode, InputSamples } }, Output);
        }
    }
}
