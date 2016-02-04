
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equus.Horse;

namespace Equus.Gidran
{

    public class Matrix
    {

        // Private Variables //
        internal const int MATRIX_ROUND = 5;
        protected int _Rows = -1; // Rows
        protected int _Columns = -1; // Columns
        protected double[,] _Data;

        // Constructors //
        /// <summary>
        /// Builds a new matrix
        /// </summary>
        /// <param name="Rows"></param>
        /// <param name="Columns"></param>
        public Matrix(int Rows, int Columns)
        {

            // Check to see if the rows and columns are valid //
            if (Rows < 1 || Columns < 1)
            {
                throw new Exception("Row " + Rows.ToString() + " or column  " + Columns.ToString() + "  submitted is invalid");
            }

            // Build Matrix //
            this._Rows = Rows;
            this._Columns = Columns;

            this._Data = new double[this._Rows, this._Columns];

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="A"></param>
        public Matrix(Matrix A)
            : this(A.RowCount, A.ColumnCount)
        {

            for (int i = 0; i < A.RowCount; i++)
                for (int j = 0; j < A.ColumnCount; j++)
                    this[i, j] = A[i, j];
            
        }

        // Public Properties //
        /// <summary>
        /// Gets count of matrix rows
        /// </summary>
        public int RowCount
        {
            get
            {
                return this._Rows;
            }
        }

        /// <summary>
        /// Gets count of matrix columns
        /// </summary>
        public int ColumnCount
        {
            get
            {
                return this._Columns;
            }
        }

        /// <summary>
        /// Gets or sets the element of a matrix
        /// </summary>
        /// <param name="Row"></param>
        /// <param name="Column"></param>
        /// <returns></returns>
        public virtual double this[int Row, int Column]
        {

            get { return this._Data[Row ,Column]; }
            set { this._Data[Row, Column] = value; }

        }

        /// <summary>
        /// Gets an array vector 
        /// </summary>
        public double[,] ToArray
        {
            get { return _Data; }
        }

        /// <summary>
        /// 
        /// </summary>
        public bool IsSquare
        {
            get
            {
                return this.RowCount == this.ColumnCount;
            }
        }

        /// <summary>
        /// Gets the matrix determinate
        /// </summary>
        public double Determinate
        {
            get { return new LUDecomposition(this).det(); }
        }

        public bool AnyNaN
        {
            get
            {
                for (int i = 0; i < this.RowCount; i++)
                {
                    for (int j = 0; j < this.ColumnCount; j++)
                    {
                        if (double.IsNaN(this[i, j]))
                            return true;
                    }
                }
                return false;
            }
        }

        public bool AnyInfinity
        {
            get
            {
                for (int i = 0; i < this.RowCount; i++)
                {
                    for (int j = 0; j < this.ColumnCount; j++)
                    {
                        if (double.IsInfinity(this[i, j]))
                            return true;
                    }
                }
                return false;
            }
        }

        public bool IsInvalid
        {
            get
            {
                for (int i = 0; i < this.RowCount; i++)
                {
                    for (int j = 0; j < this.ColumnCount; j++)
                    {
                        if (double.IsInfinity(this[i, j]) | double.IsNaN(this[i,j]))
                            return true;
                    }
                }
                return false;
            }
        }

        // Methods //
        /// <summary>
        /// Checks to see if the given rows/columns are valid
        /// </summary>
        /// <param name="Row"></param>
        /// <param name="Column"></param>
        /// <returns></returns>
        private bool CheckBounds(int Row, int Column)
        {
            if (Row >= 0 && Row < this.RowCount && Column >= 0 && Column < this.ColumnCount)
                return true;
            else
                return false;
        }

        public virtual Vector RowVector(int ColumnIndex)
        {
            Vector v = new Vector(this.RowCount);
            for (int i = 0; i < this.RowCount; i++)
                v[i] = this[i, ColumnIndex];
            return v;
        }

        public virtual Vector ColumnVector(int RowIndex)
        {
            Vector v = new Vector(this.ColumnCount);
            for (int i = 0; i < this.ColumnCount; i++)
                v[i] = this[RowIndex, i];
            return v;
        }

        /// <summary>
        /// Gets a string value of the matrix
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < this.RowCount; i++)
            {

                for (int j = 0; j < this.ColumnCount; j++)
                {
                    sb.Append(Math.Round(this[i, j], MATRIX_ROUND).ToString());
                    if (j != this.ColumnCount - 1) 
                        sb.Append(",");
                }
                if (i != this.RowCount - 1) 
                    sb.Append('\n');

            }
            return sb.ToString();
        }

        #region Opperators

        /// <summary>
        /// 
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        private static bool CheckDimensions(Matrix A, Matrix B)
        {
            return (A.RowCount == B.RowCount && A.ColumnCount == B.ColumnCount);
        }

        /// <summary>
        /// Use for checking matrix multiplication
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        private static bool CheckDimensions2(Matrix A, Matrix B)
        {
            return (A.ColumnCount == B.RowCount);
        }

        /// <summary>
        /// Adds two matricies together
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Matrix operator +(Matrix A, Matrix B)
        {

            // Check bounds are the same //
            if (Matrix.CheckDimensions(A, B) == false)
            {
                throw new Exception(string.Format("Dimension mismatch A {0}x{1} B {2}x{3}", A.RowCount, A.ColumnCount, B.RowCount, B.ColumnCount));
            }

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = A[i, j] + B[i, j];
                }
            }

            // Return //
            return C;

        }

        /// <summary>
        /// Adds a scalar to each element of the matrix
        /// </summary>
        /// <param name="A"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Matrix operator +(Matrix A, double b)
        {

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = A[i, j] + b;
                }
            }

            // Return //
            return C;

        }

        /// <summary>
        /// Adds a scalar to each element of the matrix
        /// </summary>
        /// <param name="b"></param>
        /// <param name="A"></param>
        /// <returns></returns>
        public static Matrix operator +(double b, Matrix A)
        {
            return A + b;
        }

        /// <summary>
        /// Negates a matrix
        /// </summary>
        /// <param name="A">Matrix to negate</param>
        /// <returns>0 - A</returns>
        public static Matrix operator -(Matrix A)
        {
            return 0 - A;
        }

        /// <summary>
        /// Subtracts two matricies
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Matrix operator -(Matrix A, Matrix B)
        {

            // Check bounds are the same //
            if (Matrix.CheckDimensions(A, B) == false)
            {
                throw new Exception(string.Format("Dimension mismatch A {0}x{1} B {2}x{3}", A.RowCount, A.ColumnCount, B.RowCount, B.ColumnCount));
            }

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = A[i, j] - B[i, j];
                }
            }

            // Return //
            return C;

        }

        /// <summary>
        /// Subtracts a scalar from a matrix
        /// </summary>
        /// <param name="A"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Matrix operator -(Matrix A, double b)
        {
            return A + (-b);
        }

        /// <summary>
        /// Subtracts a scalar from a matrix
        /// </summary>
        /// <param name="b"></param>
        /// <param name="A"></param>
        /// <returns></returns>
        public static Matrix operator -(double b, Matrix A)
        {

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = b - A[i, j];
                }
            }

            // Return //
            return C;

        }

        /// <summary>
        /// Multiplies each element in a matrix by each other element in another matrix
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Matrix operator *(Matrix A, Matrix B)
        {

            // Check bounds are the same //
            if (Matrix.CheckDimensions(A, B) == false)
            {
                throw new Exception(string.Format("Dimension mismatch A {0}x{1} B {2}x{3}", A.RowCount, A.ColumnCount, B.RowCount, B.ColumnCount));
            }

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = A[i, j] * B[i, j];
                }
            }

            // Return //
            return C;

        }

        /// <summary>
        /// Multiplies each element in a matrix by a scalar
        /// </summary>
        /// <param name="A"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Matrix operator *(Matrix A, double b)
        {

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = A[i, j] * b;
                }
            }

            // Return //
            return C;

        }

        /// <summary>
        /// Multiplies each element in a matrix by a scalar
        /// </summary>
        /// <param name="b"></param>
        /// <param name="A"></param>
        /// <returns></returns>
        public static Matrix operator *(double b, Matrix A)
        {
            return A * b;
        }

        /// <summary>
        /// Dividies each element in a matrix by each other element in another matrix
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Matrix operator /(Matrix A, Matrix B)
        {

            // Check bounds are the same //
            if (Matrix.CheckDimensions(A, B) == false)
            {
                throw new Exception(string.Format("Dimension mismatch A {0}x{1} B {2}x{3}", A.RowCount, A.ColumnCount, B.RowCount, B.ColumnCount));
            }

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = A[i, j] / B[i, j];
                }
            }

            // Return //
            return C;

        }

        /// <summary>
        /// Divides each element in a matrix by a scalar
        /// </summary>
        /// <param name="A"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Matrix operator /(Matrix A, double b)
        {

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = A[i, j] / b;
                }
            }

            // Return //
            return C;

        }

        /// <summary>
        /// Divides each element in a matrix by a scalar
        /// </summary>
        /// <param name="b"></param>
        /// <param name="A"></param>
        /// <returns></returns>
        public static Matrix operator /(double b, Matrix A)
        {
            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = b / A[i, j];
                }
            }

            // Return //
            return C;
        }

        public static Matrix Divide2(Matrix A, Matrix B)
        {

            // Check bounds are the same //
            if (Matrix.CheckDimensions(A, B) == false)
            {
                throw new Exception(string.Format("Dimension mismatch A {0}x{1} B {2}x{3}", A.RowCount, A.ColumnCount, B.RowCount, B.ColumnCount));
            }

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] =  (B[i, j] == 0 ? 0 : A[i, j] / B[i, j]);
                }
            }

            // Return //
            return C;

        }

        public static Matrix Divide2(Matrix A, double b)
        {

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = (b == 0 ? 0 : A[i, j] / b);
                }
            }

            // Return //
            return C;

        }

        public static Matrix Divide2(double b, Matrix A)
        {

            // Build a matrix //
            Matrix C = new Matrix(A.RowCount, A.ColumnCount);

            // Main loop //
            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    C[i, j] = (A[i, j] == 0 ? 0 : b / A[i, j]);
                }
            }

            // Return //
            return C;

        }

        /// <summary>
        /// Performs the true matrix multiplication between two matricies
        /// </summary>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <returns></returns>
        public static Matrix operator ^(Matrix A, Matrix B)
        {

            if (Matrix.CheckDimensions2(A, B) == false)
            {
                throw new Exception(string.Format("Dimension mismatch A {0}x{1} B {2}x{3}", A.RowCount, A.ColumnCount, B.RowCount, B.ColumnCount));
            }

            Matrix C = new Matrix(A.RowCount, B.ColumnCount);

            // Main Loop //
            for (int i = 0; i < A.RowCount; i++)
            {

                // Sub Loop One //
                for (int j = 0; j < B.ColumnCount; j++)
                {

                    // Sub Loop Two //
                    for (int k = 0; k < A.ColumnCount; k++)
                    {

                        C[i, j] = C[i, j] + A[i, k] * B[k, j];

                    }

                }

            }

            // Return C //
            return C;

        }

        /// <summary>
        /// Inverts a matrix
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        public static Matrix operator !(Matrix A)
        {
            return Matrix.Invert(A);
        }

        /// <summary>
        /// Transposes a matrix
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        public static Matrix operator ~(Matrix A)
        {

            // Create Another Matrix //
            Matrix B = new Matrix(A.ColumnCount, A.RowCount);

            // Loop through A and copy element to B //
            for (int i = 0; i < A.RowCount; i++)
                for (int j = 0; j < A.ColumnCount; j++)
                    B[j, i] = A[i, j];

            // Return //
            return B;

        }

        /// <summary>
        /// Returns the identity matrix given a dimension
        /// </summary>
        /// <param name="Dimension"></param>
        /// <returns></returns>
        public static Matrix Identity(int Dimension)
        {

            // Check that a positive number was passed //
            if (Dimension < 1)
            {
                throw new Exception("Dimension must be greater than or equal to 1");
            }

            // Create a matrix //
            Matrix A = new Matrix(Dimension, Dimension);

            for (int i = 0; i < Dimension; i++)
            {
                for (int j = 0; j < Dimension; j++)
                {
                    if (i != j)
                    {
                        A[i, j] = 0;
                    }
                    else
                    {
                        A[i, j] = 1;
                    }
                }
            }

            return A;

        }

        public static Matrix Invert(Matrix A)
        {

            // New decomposition //
            LUDecomposition lu = new LUDecomposition(A);

            // New identity //
            Matrix I = Matrix.Identity(A.RowCount);

            return ~lu.solve(I);

        }

        public static double Sum(Matrix A)
        {
            double d = 0;
            for (int i = 0; i < A.RowCount; i++)
                for (int j = 0; j < A.ColumnCount; j++)
                    d += A[i, j];
            return d;
        }

        public static double SumSquare(Matrix A)
        {
            double d = 0;
            for (int i = 0; i < A.RowCount; i++)
                for (int j = 0; j < A.ColumnCount; j++)
                    d += A[i, j] * A[i, j];
            return d;
        }

        public static Matrix Trace(Matrix A)
        {

            if (!A.IsSquare)
                throw new Exception(string.Format("Cannot trace a non-square matrix : {0} x {1}", A.RowCount, A.ColumnCount));

            Matrix B = new Matrix(A.RowCount, A.RowCount);

            for (int i = 0; i < A.RowCount; i++)
                B[i, i] = A[i, i];

            return B;

        }

        public static Vector TraceVector(Matrix A)
        {

            if (!A.IsSquare)
                throw new Exception(string.Format("Cannot trace a non-square matrix : {0} x {1}", A.RowCount, A.ColumnCount));

            Vector B = new Vector(A.RowCount);

            for (int i = 0; i < A.RowCount; i++)
                B[i] = A[i, i];

            return B;

        }

        #endregion

        public static Matrix ToMatrix(RecordSet Data, Key K)
        {

            Matrix m = new Matrix(Data.Count, K.Count);

            for (int i = 0; i < Data.Count; i++)
            {

                for (int j = 0; j < K.Count; j++)
                {
                    int k = K[j];
                    m[i, j] = Data[i][k].valueDOUBLE;
                }

            }
            return m;

        }

        public static Matrix ToMatrix(RecordSet Data)
        {
            Key k = Key.Build(Data.Columns.Count);
            return ToMatrix(Data, k);
        }

        public static Matrix ToMatrixWithIntercept(RecordSet Data, Key K)
        {

            Matrix m = new Matrix(Data.Count, K.Count + 1);

            for (int i = 0; i < Data.Count; i++)
            {

                m[i, 0] = 1;
                for (int j = 0; j < K.Count; j++)
                {
                    int k = K[j];
                    m[i, j + 1] = Data[i][k].valueDOUBLE;
                }

            }
            return m;

        }

        public static Matrix ToMatrixWithIntercept(RecordSet Data)
        {
            Key k = Key.Build(Data.Columns.Count);
            return ToMatrixWithIntercept(Data, k);
        }

        public static Matrix Randomize(Matrix A, int Seed, double Scale, double Offset)
        {

            Random r = new Random(Seed);
            Matrix B = new Matrix(A.RowCount, A.ColumnCount);

            for (int i = 0; i < A.RowCount; i++)
            {
                for (int j = 0; j < A.ColumnCount; j++)
                {
                    B[i, j] = r.NextDouble() * Scale - Offset;
                }
            }
            return B;

        }

        // Private class //
        private class LUDecomposition 
        {

            private Matrix LU;
            private int m, n, pivsign; 
            private int[] piv;

            public LUDecomposition (Matrix A) 
            {

                // Use a "left-looking", dot-product, Crout/Doolittle algorithm.

                LU = new Matrix(A);
                m = A.RowCount;
                n = A.ColumnCount;
                piv = new int[m];
                for (int i = 0; i < m; i++) 
                {
                    piv[i] = i;
                }
                pivsign = 1;
                double[] LUcolj = new double[m];

                // Outer loop.

                for (int j = 0; j < n; j++) {

                    // Make a copy of the j-th column to localize references.

                    for (int i = 0; i < m; i++) 
                    {
                        LUcolj[i] = LU[i,j];
                    }

                    // Apply previous transformations.

                    for (int i = 0; i < m; i++) 
                    {

                        // Most of the time is spent in the following dot product.

                        int kmax = Math.Min(i,j);
                        double s = 0.0;
                        for (int k = 0; k < kmax; k++) 
                        {
                            s += LU[i,k] * LUcolj[k];
                        }

                        LU[i, j] = LUcolj[i] -= s;

                    }
   
                    // Find pivot and exchange if necessary.

                    int p = j;
                    for (int i = j+1; i < m; i++) 
                    {
                        if (Math.Abs(LUcolj[i]) > Math.Abs(LUcolj[p])) 
                        {
                            p = i;
                        }
                    }
                    if (p != j) 
                    {
                        for (int k = 0; k < n; k++) 
                        {
                            double t = LU[p,k]; 
                            LU[p,k] = LU[j,k]; 
                            LU[j,k] = t;
                        }
                        int l = piv[p]; 
                        piv[p] = piv[j]; 
                        piv[j] = l;
                        pivsign = -pivsign;
                    }

                    // Compute multipliers.
         
                    if (j < m & LU[j,j] != 0.0) 
                    {
                        for (int i = j+1; i < m; i++) 
                        {
                            LU[i,j] /= LU[j,j];
                        }
                    }
                }
            }

            public bool isNonsingular () 
            {
                for (int j = 0; j < n; j++) 
                {
                    if (LU[j,j] == 0)
                    return false;
                }
                return true;
            }

            public Matrix getL () 
            {
                
                Matrix L = new Matrix(m, n);
                for (int i = 0; i < m; i++) 
                {
                    for (int j = 0; j < n; j++) 
                    {
                        if (i > j) 
                        {
                            L[i,j] = LU[i,j];
                        } else if (i == j) 
                        {
                            L[i,j] = 1.0;
                        } else 
                        {
                            L[i,j] = 0.0;
                        }
                    }
                }
                return L;

            }

            public Matrix getU () 
            {
                Matrix X = new Matrix(n,n);
                for (int i = 0; i < n; i++) 
                {
                    for (int j = 0; j < n; j++) 
                    {
                        if (i <= j) 
                        {
                            X[i,j] = LU[i,j];
                        } else 
                        {
                            X[i,j] = 0.0;
                        }
                    }
                }
                return X;
            }

            public int[] getPivot () 
            {
                int[] p = new int[m];
                for (int i = 0; i < m; i++) 
                {
                    p[i] = piv[i];
                }
                return p;
            }

            public double[] getDoublePivot () 
            {
                double[] vals = new double[m];
                for (int i = 0; i < m; i++) 
                {
                    vals[i] = (double) piv[i];
                }
                return vals;
            }

            public double det() 
            {
                if (m != n) 
                {
                    throw new Exception("Matrix must be square.");
                }
                double d = (double) pivsign;
                for (int j = 0; j < n; j++) 
                {
                    d *= LU[j,j];
                }
                return d;
            }

            public Matrix solve(Matrix B) 
            {
                
                if (B.RowCount != m) 
                {
                    throw new Exception("Matrix row dimensions must agree.");
                }
                if (!this.isNonsingular()) 
                {
                    throw new Exception("Matrix is singular.");
                }

                // Copy right hand side with pivoting
                int nx = B.ColumnCount;
                Matrix X = this.getMatrix(B, piv, 0, nx - 1);
                
                // Solve L*Y = B(piv,:)
                for (int k = 0; k < n; k++) 
                {

                    for (int i = k+1; i < n; i++) 
                    {

                        for (int j = 0; j < nx; j++) 
                        {
                            X[i,j] -= X[k,j] * LU[i,k];
                        }

                    }

                }
                // Solve U*X = Y;
                for (int k = n-1; k >= 0; k--) 
                {

                    for (int j = 0; j < nx; j++) 
                    {
                        X[k,j] /= LU[k,k];
                    }

                    for (int i = 0; i < k; i++) 
                    {

                        for (int j = 0; j < nx; j++) 
                        {
                            X[i,j] -= X[k,j] * LU[i,k];
                        }

                    }

                }

                return X;

            }

            public Matrix getMatrix(Matrix A, int[] r, int j0, int j1)
            {
                
                Matrix X = new Matrix(r.Length, j1 - j0 + 1);
                
                for (int i = 0; i < r.Length; i++)
                {
                    for (int j = j0; j <= j1; j++)
                    {
                        X[i, j - j0] = A[r[i], j];
                    }
                }

                return X;

            }

        }
        // end private class //

    }

}