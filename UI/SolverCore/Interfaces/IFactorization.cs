﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolverCore
{
    public interface IFactorization
    {
        IVector LSolve(IVector x);
        IVector USolve(IVector x);
        IVector LMult(IVector x);
        IVector UMult(IVector x);
    }
}
