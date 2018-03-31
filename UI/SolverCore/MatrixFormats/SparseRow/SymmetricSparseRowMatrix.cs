﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SolverCore
{
    public class SymmetricSparseRowMatrix : IMatrix, ILinearOperator
    {
        private double[] a; //значения
        private int[] ja;   //положение ненулевых элементов в строке(индекс j)
        private int[] ia;   //количество ненулевых элементов в строк

        //ia1- первый элемент в строке
        //ia2 - последний элемент в строке или первый элемент следующий строки

        public SymmetricSparseRowMatrix(double[] a, int[] ja, int[] ia)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (ja == null)
            {
                throw new ArgumentNullException(nameof(ja));
            }

            if (ia == null)
            {
                throw new ArgumentNullException(nameof(ia));
            }

            if (a.Length != ja.Length)
            {
                throw new ArgumentNullException("a and ja must be equal size");
            }

            if (a.Length != ia[ia.Length - 1])
            {
                throw new ArgumentNullException("wrong count of elements");
            }
            this.ia = (int[])ia.Clone();
            this.ja = (int[])ja.Clone();
            this.a = (double[])a.Clone();

            if (this.ia[0] == 1)
            {
                for (int i = 0; i < this.ia.Length; i++)
                {
                    this.ia[i]--;
                }

                for (int j = 0; j < this.ja.Length; j++)
                {
                    this.ja[j]--;
                }
            }

            for (int i = 1; i < Size; i++)
            {
                Sorter.QuickSort(this.ja, this.ia[i], this.ia[i + 1] - 1, this.a);
            }
        }

        public SymmetricSparseRowMatrix(int[] ja, int[] ia)
        {

            if (ja == null)
            {
                throw new ArgumentNullException(nameof(ja));
            }

            if (ia == null)
            {
                throw new ArgumentNullException(nameof(ia));
            }
            if (ja.Length != ia[ia.Length - 1])
            {
                throw new ArgumentNullException("wrong count of elements");
            }
            this.ia = (int[])ia.Clone();
            this.ja = (int[])ja.Clone();
            this.a = new double[ja.Length];

            if (this.ia[0] == 1)
            {
                for (int i = 0; i < this.ia.Length; i++)
                {
                    this.ia[i]--;
                }

                for (int j = 0; j < this.ja.Length; j++)
                {
                    this.ja[j]--;
                }
            }

            for (int i = 1; i < Size; i++)
            {
                Array.Sort(this.ja, this.ia[i], this.ia[i + 1] - this.ia[i]);
            }
        }


        public SymmetricSparseRowMatrix(SymmetricCoordinationalMatrix matrix)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }
            var elems = matrix.OrderBy(key => key.row).ThenBy(key => key.col);
            ia = new int[matrix.Size + 1];
            List<int> list_ja = new List<int>();
            List<double> list_a = new List<double>();
            foreach (var item in elems)
            {
                if (item.row >= item.col)
                {
                    ia[item.row + 1]++;
                    list_ja.Add(item.col);
                    list_a.Add(item.value);
                }
            }
            for (int i = 1; i < Size; i++)
            {
                ia[i + 1] += ia[i];
            }
            ja = new int[ia[Size]];
            a = new double[ja.Length];
            ja = list_ja.ToArray();
            a = list_a.ToArray();

        }


        //получение элемента по индексу
        public double this[int i, int j]
        {
            get
            {
                try
                {
                    if (j > i)
                    {
                        var k = i;
                        i = j;
                        j = k;
                    }
                    var ia1 = ia[i];
                    var ia2 = ia[i + 1];
                    var m = Array.IndexOf(ja, j, ia1, ia2 - ia1);

                    return m == -1 ? 0.0 : a[m];

                }
                catch (IndexOutOfRangeException)
                {
                    throw new IndexOutOfRangeException();
                }
            }
            // set => throw new NotImplementedException();
        }

        //размер
        public int Size => ia.Length - 1;
        //диагональ
        public IVector Diagonal
        {
            get
            {
                var diagonal = new Vector(Size);

                for (int i = 0; i < Size; i++)
                {
                    int ia1 = ia[i];
                    int ia2 = ia[i + 1];

                    for (; ia1 < ia2; ia1++)
                    {
                        if (ja[ia1] == i)
                        {
                            diagonal[i] = a[ia1];
                        }
                    }
                }

                return diagonal;
            }
        }
        public ILinearOperator Transpose => this;

        //заполнение
        public void Fill(FillFunc elems)
        {
            if (elems == null)
            {
                throw new ArgumentNullException(nameof(elems));
            }

            for (int i = 0; i < Size; i++)
            {
                for (int ia1 = ia[i]; ia1 < ia[i + 1]; ia1++)
                {
                    a[ia1] = elems(i, ja[ia1]);
                }
            }
        }

        //коллекциятолько (элементы нижнего треугольника)
        public IEnumerator<(double value, int row, int col)> GetEnumerator()
        {
            for (int i = 0; i < Size; i++)
            {
                for (int ia1 = ia[i]; ia1 < ia[i + 1]; ia1++)
                {
                    yield return (a[ia1], i, ja[ia1]);

                    if (i != ja[ia1])
                        yield return (a[ia1], ja[ia1], i);
                }
            }
        }

        //умножение на нижний треугольник
        public IVector LMult(IVector vector, bool UseDiagonal, DiagonalElement diagonalElement = DiagonalElement.One)
        {
            if (vector == null)
            {
                throw new ArgumentNullException(nameof(vector));
            }
            if (vector.Size != Size)
            {
                throw new RankException();
            }
            var result = new Vector(Size);
            for (int i = 0; i < Size; i++)
            {
                double sum = 0;
                var ia1 = ia[i];
                var ia2 = ia[i + 1];
                for (; i > ja[ia1] && ia1 < ia2; ia1++)
                {
                    var j = ja[ia1];
                    sum += a[ia1] * vector[j];
                }
                    sum += UseDiagonal ? a[ia1] * vector[i] : (double)diagonalElement * vector[i];
                
                result[i] = sum;
            }
            return result;
        }
        //прямой ход
        public IVector LSolve(IVector vector, bool UseDiagonal)
        {
            if (vector == null)
            {
                throw new ArgumentNullException(nameof(vector));
            }

            if (vector.Size != Size)
            {
                throw new RankException();
            }
            double sum = 0;
            var di = Diagonal;
            var result = vector.Clone();
            for (int i = 0; i < Size; i++)
            {
                var ia1 = ia[i];
                var ia2 = ia[i + 1];
                int j;
                sum = 0;
                for (; ja[ia1] < i && ia1 < ia2; ia1++)
                {
                    j = ja[ia1];
                    sum += result[j] * a[ia1];
                }
                    result[i] = UseDiagonal ? (result[i] - sum) / di[i] : result[i] - sum;
        
            }
            return result;
        }
        //умножение
        public IVector Multiply(IVector vector)
        {
            if (vector == null)
            {
                throw new ArgumentNullException(nameof(vector));
            }

            if (vector.Size != Size)
            {
                throw new RankException();
            }
            var result = new Vector(Size);
            for (int i = 0; i < Size; i++)
            {
                var ia1 = ia[i];
                var ia2 = ia[i + 1];
                int j;
                for (; ia1 < ia2 - 1; ia1++)
                {
                    j = ja[ia1];
                    result[j] += a[ia1] * vector[i];
                    result[i] += a[ia1] * vector[j];
                }
                j = ja[ia1];
                result[i] += a[ia1] * vector[j];
            }
            return result;
        }
        //умножение на верхний треугольник
        public IVector UMult(IVector vector, bool UseDiagonal, DiagonalElement diagonalElement = DiagonalElement.One)
        {
            if (vector == null)
            {
                throw new ArgumentNullException(nameof(vector));
            }

            if (vector.Size != Size)
            {
                throw new RankException();
            }
            var result = new Vector(Size);
            for (int i = Size - 1; i >= 0; i--)
            {
                var ia1 = ia[i];
                var ia2 = ia[i + 1] - 1;
                result[i] += UseDiagonal ? a[ia2] * vector[i] : (double)diagonalElement * vector[i];
                for (ia2--; ia1 <= ia2; ia2--)
                {
                    var j = ja[ia2];
                    result[j] += a[ia2] * vector[i];
                }
            }
            return result;
        }

        public IVector USolve(IVector vector, bool UseDiagonal)
        {
            if (vector == null)
            {
                throw new ArgumentNullException(nameof(vector));
            }

            if (vector.Size != Size)
            {
                throw new RankException();
            }
            var result = vector.Clone();
            var di = Diagonal;
            for (int i = Size - 1; i >= 0; i--)
            {
                var ia1 = ia[i];
                var ia2 = ia[i + 1];
                int j;
                di[i] = UseDiagonal ? di[i] : 1.0;
                for (; ja[ia1] < i && ia1 < ia2; ia1++)
                {
                    j = ja[ia1];
                    result[j] -= result[i] * a[ia1] / di[i];//??????
                }
                 result[i] = result[i] / di[i];
                
            }
            return result;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public string Serialize(IVector b, IVector x0)
        {
            var obj = new { ia, b, x0, gg = a, ja };
            return JsonConvert.SerializeObject(obj);

        }
    }
}
