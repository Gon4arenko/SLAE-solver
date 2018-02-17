﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI.MatrixFormats
{
    [Serializable]
    class CoordinateMatrix
    {
        public int[] rows { get; set; }
        public int[] cols { get; set; }
        public double gg { get; set; }

        public double[] gl { get; set; }
        public double[] gu { get; set; }

        public double[] x0 { get; set; }
        public double eps { get; set; }

        public bool symmetry { get; set; }
    }
}
