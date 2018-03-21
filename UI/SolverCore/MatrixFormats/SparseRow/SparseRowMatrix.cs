﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace SolverCore
{
    public class SparseRowMatrix : IMatrix, ILinearOperator, ITransposeLinearOperator
    {
        private double[] a;//значения
        private int[] ja;//положение ненулевых элементов в строке(индекс j)
        private int[] ia;//количество ненулевых элементов в строк

        //ia1- первый элемент в строке
        //ia2 - последний элемент в строке или первый элемент следующий строки

        //конструктор
        public SparseRowMatrix(double[] a, int[] ja, int[] ia)
        {
            if (a == null)
            {
                throw new ArgumentNullException(nameof(a));
            }

            if (ja == null)
            {
                throw new ArgumentNullException(nameof(ja));
            }

            if (a.Length != ja.Length)
            {
                throw new ArgumentException("a and ja must be equal size");
            }

            if (a.Length != ia[ia.Length - 1])
            {
                throw new ArgumentException("wrong count of elements");
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
            for (int i = 0; i < Size; i++)
            {
                Sorter.QuickSort(this.ja, this.ia[i], this.ia[i + 1] - 1, this.a);

            }
        }

        public SparseRowMatrix(int[] ja,int[] ia )
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
                throw new ArgumentException("wrong count of elements");
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
            for (int i = 0; i < Size; i++)
            {
                Array.Sort(this.ja, this.ia[i], this.ia[i + 1] - this.ia[i]);
            }
        }


        public SparseRowMatrix(CoordinationalMatrix matrix)
        {
            if (matrix == null)
            {
                throw new ArgumentNullException(nameof(matrix));
            }
            
            var elems = matrix.OrderBy(key => key.row).ThenBy(key => key.col);

            ia = new int[matrix.Size + 1];
            ja = new int[matrix.Count()];
            a = new double[ja.Length];
            int i = 0, j = 0;
            ia[i] = 0;
            ia[i + 1] = 0;

            foreach (var item in elems)
            {
                ja[j] = item.col;
                a[j] = item.value;
                j++;
                ia[item.row+1]++;
             
            }
            for (int k = 0; k < Size; k++)
            {
                ia[k + 1] += ia[k];
            }

        }

        //получение элемента по индексу(добавить try catch
        public double this[int i, int j]
        {
            get
            {
                try
                {
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
            //  set => throw new NotImplementedException();
        }
        //размер
        public int Size => ia.Length - 1;//так как последний элемент в массиве кол-во ненулевых элементов
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

        //транспонирование
        public ILinearOperator Transpose => new TransposeMatrix<SparseRowMatrix>() { Matrix = this };

        //заполнение
        public void Fill(FillFunc elems)
        {
            if (elems == null)
            {
                throw new ArgumentNullException(nameof(elems));
            }
            for (int i=0;i<=Size;i++)
            {
                int ia1 = ia[i];
                int ia2 = ia[i+1];
                for (; ia1 < ia2; ia1++)
                {
                    a[ia1] = elems(i,ja[ia1]);
                }
            }
        }

        //значение и координаты
        public IEnumerator<(double value, int row, int col)> GetEnumerator()
        {
        for (int i = 0; i < Size; i++)
        {
            var ia1 = ia[i];
            var ia2 = ia[i + 1];
            for (; ia1 < ia2; ia1++)
            {
                yield return (a[ia1], i, ja[ia1]);
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
                var ia1 = ia[i];//1ый элемент строки
                var ia2 = ia[i + 1];//1ый элемент следующий строки строки
                int j;
                for (; ja[ia1] < i && ia1 < ia2; ia1++)
                {
                    j = ja[ia1];
                    sum += a[ia1] * vector[j];
                }
                j = ja[ia1];
                if (j == i && ia1 < ia2)
                {
                    sum += UseDiagonal ?  a[ia1] * vector[j]: (double)diagonalElement * vector[j];
                }
                result[i] = sum;
            }
            return result;
        }
        //умножение на транспонированный нижний треугольник
        public IVector LMultTranspose(IVector vector, bool UseDiagonal, DiagonalElement diagonalElement = DiagonalElement.One)
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
                var ia1 = ia[i];//1ый элемент строки 
                var ia2 = ia[i + 1];//последний элемент строки
                int j;
                for (; ja[ia1] < i && ia1 < ia2; ia1++)
                {
                    j = ja[ia1];
                    result[j] += a[ia1] * vector[i];
                }
                j = ja[ia1];
                if (j == i && ia1 < ia2)
                {
                    result[j] += UseDiagonal? a[ia1] * vector[j]: (double)diagonalElement * vector[j];
                }
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
            var result = vector.Clone();
            double sum = 0;
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
                if (i == ja[ia1] && ia1 < ia2)
                {
                    result[i] = UseDiagonal ? (result[i] - sum) / a[ia1] : result[i] - sum;
                }
                else
                {
                    throw new ArgumentNullException(nameof(a));
                }
            }
            return result;
        }
        //прямой ход с транспонированным нижним треугольником
        public IVector LSolveTranspose(IVector vector, bool UseDiagonal)
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
                    result[j] -= result[i] * a[ia1] / di[i];
                }
                if (i == ja[ia1] && ia1 < ia2)
                {
                    result[i] = result[i] / di[i];
                }
                else
                {
                    throw new ArgumentNullException( nameof(a));
                }
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
                double sum = 0;
                var ia1 = ia[i];
                var ia2 = ia[i + 1];
                for (; ia1 < ia2; ia1++)
                {
                    int j = ja[ia1];
                    sum += a[ia1] * vector[j];
                }
                result[i] = sum;
            }
            return result;
        }
        //умножение на транспонированную матрицу
        public IVector MultiplyTranspose(IVector vector)
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
                for (; ia1 <= ia2; ia2--)
                {
                    int j = ja[ia2];
                    result[j] += a[ia2] * vector[i];
                }
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
            for (int i = 0; i < Size; i++)
            {
                double sum = 0;
                var ia1 = ia[i];
                var ia2 = ia[i + 1] - 1;
                var j = ja[ia2];
                for (; j > i && ia1 < ia2; ia2--)
                {
                    j = ja[ia2];
                    sum += a[ia2] * vector[j];
                }
                j = ja[ia2];
                if (j == i && ia1 <= ia2)
                {
                    sum += UseDiagonal ? a[ia2] * vector[j] : (double)diagonalElement*vector[j];
                }
                result[i] = sum;
            }
            return result;
        }
        //умножение на транспонированный верхний треугольник
        public IVector UMultTranspose(IVector vector, bool UseDiagonal, DiagonalElement diagonalElement = DiagonalElement.One)
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
                var ia2 = ia[i + 1] - 1;
                var j = ja[ia2];
                for (; i < j && ia1 < ia2; ia2--)
                {
                    j = ja[ia2];
                    result[j] += a[ia2] * vector[i];
                }
                j = ja[ia2];
                if (j == i && ia1 <= ia2)
                    result[i] += UseDiagonal ? a[ia2] * vector[i] : (double)diagonalElement * vector[i];
            }
            return result;
        }
        //обратный ход
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
            for (int i = Size - 1; i >= 0; i--)
            {
                var ia1 = ia[i];
                var ia2 = ia[i + 1] - 1;
                int j;
                double sum = 0;
                for (j = ja[ia2]; j > i && ia1 < ia2; ia2--)
                {
                    j = ja[ia2];
                    sum += result[j] * a[ia2];
                }
                j = ja[ia2];
                if (i == j && ia1 <= ia2)
                {
                    result[i] = UseDiagonal ? (result[i] - sum)/ a[ia2] : result[i] - sum;
                }
                else
                {
                    throw new ArgumentNullException(nameof(a));
                }
            }
            return result;
        }
        //обратный ход с транспонированием
        public IVector USolveTranspose(IVector vector, bool UseDiagonal)
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
            for (int i = 0; i < Size; i++)
            {
                var ia1 = ia[i];
                var ia2 = ia[i + 1] - 1;
                int j;
                di[i] = UseDiagonal ? di[i] : 1.0;
                for (; ja[ia2] > i && ia1 < ia2; ia2--)
                {
                    j = ja[ia2];
                    result[j] -= result[i] * a[ia2] / di[i];
                }
                if (i == ja[ia2] && ia1 <= ia2)
                {
                    result[i] = result[i] / di[i];
                }
                else
                {
                    throw new ArgumentNullException();
                }
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
