using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ComputerAlgebra
{
    /// <summary>
    /// Represents an MxN matrix.
    /// </summary>
    public class Matrix : IEnumerable<Expression>
    {
        protected Expression[,] m;

        /// <summary>
        /// Create an arbitrary MxN matrix.
        /// </summary>
        /// <param name="M"></param>
        /// <param name="N"></param>
        public Matrix(int M, int N)
        {
            m = new Expression[M, N];
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    m[i, j] = 0;
        }

        /// <summary>
        /// Create an NxN identity matrix.
        /// </summary>
        /// <param name="M"></param>
        /// <param name="N"></param>
        public Matrix(int N)
        {
            m = new Expression[N, N];
            for (int i = 0; i < N; ++i)
                for (int j = 0; j < N; ++j)
                    m[i, j] = i == j ? 1 : 0;
        }

        public Matrix(Matrix Clone)
        {
            m = new Expression[Clone.M, Clone.N];
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    m[i, j] = Clone[i, j];
        }
        
        public int M { get { return m.GetLength(0); } }
        public int N { get { return m.GetLength(1); } }

        public Matrix Evaluate(IEnumerable<Arrow> x)
        {
            Matrix E = new Matrix(M, N);
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    E[i, j] = this[i, j].Evaluate(x);
            return E;
        }

        /// <summary>
        /// Access a matrix element.
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <returns></returns>
        public Expression this[int i, int j]
        {
            get { return m[i, j]; }
            set { m[i, j] = value; }
        }

        /// <summary>
        /// Extract a row from the matrix.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Matrix Row(int i)
        {
            Matrix R = new Matrix(1, N);
            for (int j = 0; j < N; ++j)
                R[0, j] = m[i, j];
            return R;
        }

        /// <summary>
        /// Extract a column from the matrix.
        /// </summary>
        /// <param name="j"></param>
        /// <returns></returns>
        public Matrix Column(int j)
        {
            Matrix C = new Matrix(M, 1);
            for (int i = 0; i < M; ++i)
                C[i, 0] = m[i, j];
            return C;
        }

        /// <summary>
        /// Enumerate the rows of the matrix.
        /// </summary>
        public IEnumerable<Matrix> Rows { get { for (int i = 0; i < M; ++i) yield return Row(i); } }
        /// <summary>
        /// Enumerate the columns of the matrix.
        /// </summary>
        public IEnumerable<Matrix> Columns { get { for (int j = 0; j < N; ++j) yield return Column(j); } }

        /// <summary>
        /// Access an element of a vector.
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public Expression this[int i]
        {
            get
            {
                if (M == 1)
                    return m[0, i];
                else if (N == 1)
                    return m[i, 0];
                else
                    throw new InvalidOperationException("Matrix is not a vector");
            }
            set
            {
                if (M == 1)
                    m[0, i] = value;
                else if (N == 1)
                    m[i, 0] = value;
                else
                    throw new InvalidOperationException("Matrix is not a vector");
            }
        }

        // IEnumerator<Expression> interface.
        private class Enumerator : IEnumerator<Expression>, IEnumerator
        {
            private Matrix a;
            private int i = 0;
            private int N;

            object IEnumerator.Current { get { return a[i]; } }
            Expression IEnumerator<Expression>.Current { get { return a[i]; } }

            public Enumerator(Matrix A) { a = A; N = Math.Max(A.M, A.N); }

            public void Dispose() { }
            public bool MoveNext() { return ++i >= N; }
            public void Reset() { i = 0; }
        }
        IEnumerator<Expression> IEnumerable<Expression>.GetEnumerator() { return new Enumerator(this); }
        IEnumerator IEnumerable.GetEnumerator() { return new Enumerator(this); }

        public override string ToString()
        {
            StringBuilder SB = new StringBuilder();

            SB.Append("[");
            for (int i = 0; i < M; ++i)
            {
                SB.Append("[");
                for (int j = 0; j < N; ++j)
                {
                    if (j > 0) SB.Append(", ");
                    SB.Append(m[i, j].ToString());
                }
                SB.Append("]");
            }
            SB.Append("]");

            return SB.ToString();
        }

        private static void SwapRows(Matrix A, int i1, int i2)
        {
            if (i1 == i2)
                return;

            int N = A.N;
            for (int j = 0; j < N; ++j)
            {
                Expression t = A[i1, j];
                A[i1, j] = A[i2, j];
                A[i2, j] = t;
            }
        }
        private static void ScaleRow(Matrix A, int i, Expression s)
        {
            int N = A.N;
            for (int j = 0; j < N; ++j)
                A[i, j] *= s;
        }
        private static void ScaleAddRow(Matrix A, int i1, Expression s, int i2)
        {
            int N = A.N;
            for (int j = 0; j < N; ++j)
                A[i2, j] += A[i1, j] * s;
        }

        public static Matrix operator ^(Matrix A, int B)
        {
            if (A.M != A.N)
                throw new ArgumentException("Non-square matrix");

            int N = A.N;
            if (B < 0)
            {
                Matrix A_ = new Matrix(A);
                Matrix Inv = new Matrix(N);

                // Gaussian elimination, [ A I ] ~ [ I, A^-1 ]
                for (int i = 0; i < N; ++i)
                {
                    // Find pivot row.
                    int p;
                    for (p = i; p < N; ++p)
                        if (!A_[p, i].EqualsZero())
                            break;
                    if (p >= N)
                        throw new ArgumentException("Singular matrix");

                    // Swap pivot row with row i.
                    SwapRows(A_, i, p);
                    SwapRows(Inv, i, p);

                    // Put a 1 in the pivot position.
                    Expression s = 1 / A_[i, i];
                    ScaleRow(A_, i, s);
                    ScaleRow(Inv, i, s);

                    // Zero the pivot column elsewhere.
                    for (p = 0; p < N; ++p)
                    {
                        if (i != p)
                        {
                            Expression a = -A_[p, i];
                            ScaleAddRow(A_, i, a, p);
                            ScaleAddRow(Inv, i, a, p);
                        }
                    }
                }
                return Inv ^ -B;
            }

            if (B != 1)
                throw new ArgumentException("Unsupported matrix exponent");

            return A;
        }

        public static Matrix operator *(Matrix A, Matrix B)
        {
            if (A.N != B.M)
                throw new ArgumentException("Invalid matrix multiply");
            int M = A.M;
            int N = A.N;
            int L = B.N;

            Matrix AB = new Matrix(M, L);
            for (int i = 0; i < M; ++i)
            {
                for (int j = 0; j < L; ++j)
                {
                    Expression ABij = 0;
                    for (int k = 0; k < N; ++k)
                        ABij += A[i, k] * B[k, j];
                    AB[i, j] = ABij;
                }
            }
            return AB;
        }
        public static Matrix operator *(Matrix A, Expression B)
        {
            int M = A.M, N = A.N;
            Matrix AB = new Matrix(M, N);
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    AB[i, j] = A[i, j] * B;
            return AB;
        }
        public static Matrix operator *(Expression A, Matrix B)
        {
            int M = B.M, N = B.N;
            Matrix AB = new Matrix(M, N);
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    AB[i, j] = A * B[i, j];
            return AB;
        }

        public static Matrix operator +(Matrix A, Matrix B)
        {
            if (A.M != B.M || A.N != B.N)
                throw new ArgumentException("Invalid matrix addition");

            int M = A.M, N = A.N;
            Matrix AB = new Matrix(M, N);
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    AB[i, j] = A[i, j] + B[i, j];
            return AB;
        }
        public static Matrix operator +(Matrix A, Expression B)
        {
            int M = A.M, N = A.N;
            Matrix AB = new Matrix(M, N);
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    AB[i, j] = A[i, j] + B;
            return AB;
        }
        public static Matrix operator +(Expression A, Matrix B)
        {
            int M = B.M, N = B.N;
            Matrix AB = new Matrix(M, N);
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    AB[i, j] = A + B[i, j];
            return AB;
        }

        public static Matrix operator -(Matrix A, Matrix B)
        {
            if (A.M != B.M || A.N != B.N)
                throw new ArgumentException("Invalid matrix addition");

            int M = A.M, N = A.N;
            Matrix AB = new Matrix(M, N);
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    AB[i, j] = A[i, j] - B[i, j];
            return AB;
        }
        public static Matrix operator -(Matrix A, Expression B)
        {
            int M = A.M, N = A.N;
            Matrix AB = new Matrix(M, N);
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    AB[i, j] = A[i, j] - B;
            return AB;
        }
        public static Matrix operator -(Expression A, Matrix B)
        {
            int M = B.M, N = B.N;
            Matrix AB = new Matrix(M, N);
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    AB[i, j] = A - B[i, j];
            return AB;
        }

        public static Matrix operator -(Matrix A)
        {
            int M = A.M, N = A.N;
            Matrix nA = new Matrix(M, N);
            for (int i = 0; i < M; ++i)
                for (int j = 0; j < N; ++j)
                    nA[i, j] = -A[i, j];
            return nA;
        }
    }
}
