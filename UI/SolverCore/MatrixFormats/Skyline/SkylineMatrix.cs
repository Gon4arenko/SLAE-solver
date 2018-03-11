﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace SolverCore
{
    /// <summary>
    /// Профильный
    /// </summary>
    public class SkylineMatrix : IMatrix, ILinearOperator, ITransposeLinearOperator
    {
        private double[] di; // диагональ
        private double[] al; // массив элементов профиля нижнего треугольника
        private double[] au; // массив элементов профиля верхнего треугольника
        private int[] ia; // целочисленный массив с указателями начала строк профиля 

        /// конструктор
        public SkylineMatrix(double[] di, int[] ia, double[] al, double[] au)
        {
            if (di == null)
            {
                throw new ArgumentNullException(nameof(di));
            }

            if (ia == null)
            {
                throw new ArgumentNullException(nameof(ia));
            }

            if (al == null)
            {
                throw new ArgumentNullException(nameof(al));
            }

            if (au == null)
            {
                throw new ArgumentNullException(nameof(au));
            }

            var size = di.Length;
            this.di = (double[])di.Clone();

            var size1 = ia.Length;
            if (size1 != size + 1)
            {
                throw new ArgumentNullException("Array ia is not properly filled");
            }
            this.ia = (int[])ia.Clone();
            if (this.ia[0] == 1) //если массив начинается с 1, то уменьшаем значения всех элементов на 1
            {
                for (int i = 0; i < size1; i++) this.ia[i]--;
            }  
            
            var size2 = al.Length;
            var size3 = au.Length;
            if(this.ia[size1 - 1] != size2)
            {
                throw new ArgumentNullException("Arrays ia or al is not properly filled");
            }
            if (this.ia[size1 - 1] != size2 || this.ia[size1 - 1] != size3)
            {
                throw new ArgumentNullException("Arrays ia or al or au is not properly filled");
            }
            this.al = (double[])al.Clone();
         
            this.au = (double[])au.Clone();
        }

        public double this[int i, int j]
        {
            get
            {
                try
                {
                    if (i == j) return di[i]; // если диагональ

                    if (i > j)
                    {
                        if (ia[i + 1] - ia[i] == 0) return 0; // если данный элемент не содержится в профиле, значит, он нулевой

                        int k = i - (ia[i + 1] - ia[i]); // индекс столбца первого элемента в профиле
                        return al[ia[i] + j - k];
                    }
                    else
                    {
                        if (ia[i + 1] - ia[i] == 0) return 0; // если данный элемент не содержится в профиле, значит, он нулевой

                        int k = i - (ia[i + 1] - ia[i]); // индекс строки первого элемента в профиле
                        return au[ia[i] + j - k];
                    }
                }
                catch(IndexOutOfRangeException)
                {
                    throw new IndexOutOfRangeException(); //если выходим за рамки размерности матрицы
                }
            }
        }

        /// <summary>
        /// конструктор
        /// </summary>
        /// <param name="dimension"> размерность матрицы</param>
        /// <param name="elementCount">количество элементов в профиле одного треугольника без диагонали</param>
        public SkylineMatrix(int dimension, int elementCount)
        {
            di = new double[dimension];
            ia = new int[dimension + 1];
            al = new double[elementCount];
            au = new double[elementCount];
        }

        public int Size => di.Length;

        public IVector Diagonal => new Vector(di);

        public ILinearOperator Transpose => new TransposeMatrix<SkylineMatrix> { Matrix = this };

        public IEnumerator<(double value, int row, int col)> GetEnumerator()
        {
            for (int i = 1; i < Size; i++)
            {
                int ia1 = ia[i];
                int ia2 = ia[i + 1];
                int k = i - (ia2 - ia1);
                for ( ; ia1 < ia2; ia1++, k++)
                {
                    yield return (al[ia1], i, k);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // заполнение матрицы
        public void Fill(FillFunc elems) 
        { 
            if (elems == null) 
            { 
                throw new ArgumentNullException(nameof(elems)); 
            } 
            int i = 0, ju = 0, jl=0, k = 0; 
            ia[0] = 0; 
            ia[1] = 0; 
            foreach (var elem in this) 
            {
                if (elem.col == elem.row)
                {
                    di[i] = elems(elem.row, elem.col);
                }
                else
                if (elem.col > elem.row)
                {
                    au[ju] = elems(elem.row, elem.col);
                    ju++;
                }
                else
                {
                    al[jl] = elems(elem.row, elem.col);
                    jl++;
                }

                if (elem.row == i && i > 0)
                {
                    k++; // подсчет количества элементов в профиле
                }    
                else
                {
                    ia[i+1] = k;
                    i++;
                    k = 0;
                }
            } 
        }

        //умножение на нижний треугольник
        public IVector LMult(IVector vector, bool isUseDiagonal, DiagonalElement diagonalElement = DiagonalElement.One)
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
                int k = ia[i];
                for (int j = i - (ia[i + 1] - k); j < i; j++, k++)
                    result[i] += al[k] * vector[j];
            }
            
            for (int i = 0; i < Size; i++)
                result[i] += isUseDiagonal ? di[i] * vector[i] : (double)diagonalElement * vector[i];

            return result;
        }

        //умножение на нижний треугольник трансп
        public IVector LMultTranspose(IVector vector, bool isUseDiagonal, DiagonalElement diagonalElement = DiagonalElement.One)
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
                int k = ia[i];
                for (int j = i - (ia[i + 1] - k); j < i; j++, k++)
                    result[j] += al[k] * vector[i];
            }

           for (int i = 0; i < Size; i++)
                result[i] += isUseDiagonal ? di[i] * vector[i] : (double)diagonalElement * vector[i];

            return result;
        }

        //умножение матрицы на вектор
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
                int k = ia[i];
                for (int j = i - (ia[i + 1] - k); j < i; j++, k++)
                {
                    vector[i] += al[k] * vector[j];
                    vector[j] += au[k] * vector[i];
                }
                vector[i] += di[i] * vector[i];
            }
            return result;
        }

        //умножение матрицы на вектор трансп
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

            for (int i = 0; i < Size; i++)
            {
                int k = ia[i];
                for (int j = i - (ia[i + 1] - k); j < i; j++, k++)
                {
                    vector[i] += au[k] * vector[j];
                    vector[j] += al[k] * vector[i];
                }
                vector[i] += di[i] * vector[i];
            }
            return result;
        }


        //умножение на верхний треугольник
        public IVector UMult(IVector vector, bool isUseDiagonal, DiagonalElement diagonalElement = DiagonalElement.One)
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

            for (int j = 0; j < Size; j++)
            {
                int k = ia[j];
                for (int i = j - (ia[j + 1] - k); i < j; i++, k++)
                    result[i] += au[k] * vector[j];
            }

            for (int i = 0; i < Size; i++)
                result[i] += isUseDiagonal ? di[i] * vector[i] : (double)diagonalElement * vector[i];

            return result;
        }
        
        //умножение на верхний треугольник трансп
        public IVector UMultTranspose(IVector vector, bool isUseDiagonal, DiagonalElement diagonalElement = DiagonalElement.One)
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

            for (int j = 0; j < Size; j++)
            {
                int k = ia[j];
                for (int i = j - (ia[j + 1] - k); i < j; i++, k++)
                    result[j] += au[k] * vector[i];
            }

            for (int i = 0; i < Size; i++)
                result[i] += isUseDiagonal ? di[i] * vector[i] : (double)diagonalElement * vector[i];

            return result;
        }

        //прямой ход
        public IVector LSolve(IVector vector, bool isUseDiagonal)
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

            for (int i = 0; i < Size; i++)
            {
                double sum = 0;
                int k = i - (ia[i + 1] - ia[i]);
                for (int j = ia[i]; k < i; j++, k++)
                    sum += al[j] * result[k];
                result[i] = isUseDiagonal ? (vector[i] - sum) / di[i] : vector[i] - sum;
            }
            
            return result;
        }

        //прямой ход трансп
        public IVector LSolveTranspose(IVector vector, bool isUseDiagonal)
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
                if (isUseDiagonal) result[i] /= di[i];
                int k = i - (ia[i + 1] - ia[i]);
                for (int j = ia[i + 1] - 1, m = i - 1; m >= k; j--, m--)
                    result[m] -= al[j] * result[i];
            }
            
            return result;
        }

        //обратный ход
        public IVector USolve(IVector vector, bool isUseDiagonal)
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
                if (isUseDiagonal) result[i] /= di[i];
                int k = i - (ia[i + 1] - ia[i]);
                for (int j = ia[i + 1] - 1, m = i - 1; m >= k; j--, m--)
                    result[m] -= au[j] * result[i];
            }
            
            return result;
        }

        //обратный ход трансп
        public IVector USolveTranspose(IVector vector, bool isUseDiagonal)
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
            
            for (int i = 0; i < Size; i++)
            {
                double sum = 0;
                int k = i - (ia[i + 1] - ia[i]);
                for (int j = ia[i]; k < i; j++, k++)
                    sum += au[j] * result[k];
                result[i] = isUseDiagonal ? (vector[i] - sum) / di[i] : vector[i] - sum;
            }
            
            return result;
        }

        public CoordinationalMatrix ConvertToCoordinationalMatrix()
        {
            // перевод в координатную
            throw new NotImplementedException();
        }
    }        
}
