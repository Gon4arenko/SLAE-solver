﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SolverCore;

namespace UI
{
    public partial class PatternForm : Form
    {
        int width, heigth;
        string format;
        int cellWidth = MatrixVisualRepresentation.CellWidth;
        int cellHeight = MatrixVisualRepresentation.CellHeight;

        ConstructorForm SLAESource;
        MainForm mainForm;

        IMatrix matrix;
        IVector x0, b;

        public PatternForm()
        {
            InitializeComponent();
        }

        private void A_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            //
        }

        private void A_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            MatrixVisualRepresentation.InverseElementPatternStatus(ref A, e.RowIndex, e.ColumnIndex);
            MatrixVisualRepresentation.PaintPattern(ref A, Color.SteelBlue);
        }

        private void PatternForm_Load(object sender, EventArgs e)
        {
            SLAESource = (ConstructorForm)(Owner.Owner);
            mainForm = (MainForm)(Owner.Owner.Owner);


            infoTextBox.Width = Width - 40;
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void A_SelectionChanged(object sender, EventArgs e)
        {
            A.ClearSelection();
        }

        private void forwardToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            bool symmetric = SLAESource.IsSymmetric;
            if (symmetric)
                matrix = FormatFactory.Convert((SymmetricCoordinationalMatrix)MatrixVisualRepresentation.GridViewToCoordinational(A, symmetric), format);
            else
                            if (symmetric)
                matrix = FormatFactory.Convert((CoordinationalMatrix)MatrixVisualRepresentation.GridViewToCoordinational(A, symmetric), format);

            mainForm.SetSLAE(matrix, b, x0);
            Close();
        }

        private void PatternForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
            }
            mainForm.Show();
        }

        private void PatternForm_Shown(object sender, EventArgs e)
        {
        }

        private void backwardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Owner.Show();
            Hide();
        }

        public void Update(string type)
        {
            SLAESource.GetSLAE(out matrix, type, out b, out x0);
            int n = matrix.Size;

            Location = Owner.Location;
            format = type;

            DataGridView mat = new DataGridView();

            mat = MatrixVisualRepresentation.CoordinationalToGridView(MatrixExtensions.ConvertToCoordinationalMatrix(matrix));
            MatrixVisualRepresentation.CopyDataGridView(mat, ref A);

            width = cellWidth * (n - 2);
            heigth = cellHeight * (n - 2);

            Size size = new Size(Width > 115 + width ? Width : 115 + width, 190 + heigth);
            MaximumSize = size;
            MinimumSize = size;
            Size = size;
            A.ReadOnly = true;
            MatrixVisualRepresentation.GenerateInitialPattern(ref A);
            MatrixVisualRepresentation.PaintPattern(ref A, Color.SteelBlue);
        }
    }
}
