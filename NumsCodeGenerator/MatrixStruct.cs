﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace NumsCodeGenerator {
    class MatrixStruct : FileGenerator {

        int rows, cols;
        string vectorRow;
        string vectorCol;
        string structname;
        string type;

        string[] rowNames;        

        public MatrixStruct(string name, string type, int rows, int cols) : base("matrices/") {
            this.rows = rows; this.cols = cols;
            this.type = type;

            this.vectorRow = Program.getVectorType(type) + cols;
            this.vectorCol = Program.getVectorType(type) + rows;

            structname = name + rows;
            if (rows != cols) structname += "x" + cols;
            fileName += structname;


            rowNames = new string[rows];
            for (int i = 0; i < rowNames.Length; i++) rowNames[i] = "row" + (i + 1);


        }

        protected override void generate() {

            writeline("using System;");
            writeline("using System.Runtime.InteropServices;");
            linebreak();

            startBlock("namespace Nums");
            linebreak();

            summary("A " + rows + " by " + cols + " matrix");
            writeline("[StructLayout(LayoutKind.Sequential)]");
            startBlock("public struct " + structname);


            genRowsAndCols();
            genConstructors();

            endBlock(); // end struct block
            endBlock(); // end namespace block
        }

        private void genRowsAndCols() {

            // rows
            region("rows and columns");
            for (int i = 1; i <= rows; i++) 
                writeline("public " + vectorRow + " row" + i + ";");
            linebreak();

            // cols
            for (int i = 1; i <= cols; i++) {
                var comp = Program.vectorComps[i - 1];
                var args = rowNames.Select(x => x + "." + comp);
                startBlock("public " + vectorCol + " col" + i);
                
                writeline("get => new " + vectorCol + "(" + args.Aggregate((x, y) => x + ", " + y) + ");");
                
                startBlock("set");
                for (int j = 0; j < args.Count(); j++)
                    writeline(args.ElementAt(j) + " = value." + Program.vectorComps[j] + ";");
                endBlock();

                endBlock();
            }
            endregion();
            linebreak();


            // indexprops
            region("indexed properties");
            for (int i = 0; i < rows; i++) {
                for (int j = 0; j < cols; j++) {
                    summary("Gets the value at the " + Program.Index2String(i) + " row in the " + Program.Index2String(j) + " column");
                    startBlock("public " + type + " m" + (i + 1) + (j + 1));
                    var id = "row" + (i + 1) + "." + Program.vectorComps[j];
                    writeline("get => " + id + ";");
                    writeline("set => " + id + " = value;");
                    endBlock();
                }
            }
            endregion();
            linebreak();
        }

        private void genConstructors() {
            // rows constructor
            startBlock("public " + structname + "(" + rowNames.Select(x => vectorRow + " " + x).Aggregate((x, y) => x + ", " + y) + ")");
            for (int i = 0; i < rowNames.Length; i++) {
                writeline("this." + rowNames[i] + " = " + rowNames[i] + ";");
            }
            endBlock();

            // sepperate values constructor
            var props = new string[rows * cols];
            for (int i = 0; i < rows; i++) {
                for (int j = 0; j < cols; j++) {
                    props[i * cols + j] = "m" + (i + 1) + (j + 1);
                }
            }
            var cargs = props.Select(x => type + " " + x).Aggregate((x, y) => x + ", " + y);
            startBlock("public " + structname + "(" + cargs + ")");

            var comps = Program.vectorComps[..cols];
            for (int i = 0; i < rows; i++) {
                for (int j = 0; j < cols; j++) {
                    writeline(rowNames[i] + "." + Program.vectorComps[j] + " = " + props[i * cols + j] + ";");
                }
            }

            endBlock();

        }
    }
}
