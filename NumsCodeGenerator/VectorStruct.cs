﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace NumsCodeGenerator {
    public class VectorStruct : FileGenerator {

        private string name;
        private string[] compsNames;
        private string type;
        private string vecName;

        public VectorStruct(string name, string type, params string[] compNames) : base (name + compNames.Length) {
            this.name = name;
            this.compsNames = compNames;
            this.type = type;
            this.vecName = name + compNames.Length;
        }

        protected override void generate() {
            writeline("using System;");
            writeline("using System.Runtime.InteropServices;");
            linebreak();
            
            startBlock("namespace Nums");
            linebreak();

            summary("A " + compsNames.Length + " component vector of " + type);
            writeline("[StructLayout(LayoutKind.Sequential)]");
            startBlock("public struct " + vecName);

            genConstants();

            linebreak();
            // fields & basic properties:

            for (int i = 0; i < compsNames.Length; i++) {
                summary($"The {compsNames[i]} component is the {Program.Index2String(i)} index of the vector");
                writeline($"public {type} {compsNames[i]};");
            }
            //writeline($"public {type} {compsNames.Aggregate((x, y) => x + ", " + y)};");

            var sum = compsNames.Aggregate((x, y) => x + " + " + y);
            summary("The sum of the vectors components. " + sum);
            writeline($"public {type} sum => {sum};");

            summary("The number of bytes the vector type uses.");
            writeline($"public int bytesize => sizeof({type}) * {compsNames.Length};");

            summary("The magnitude of the vector");
            writeline($"public {type} length => ({type})Math.Sqrt(dot(this));");
            summary("The squared magnitude of the vector. sqlength is faster than length since a square-root operation is not needed.");
            writeline($"public {type} sqlength => dot(this);");

            summary("The normalized version of this vector.");
            writeline($"public {vecName} normalized => this / length;");


            genIndexAccessor();
            genSwizzlingProperties();
            genContructors();
            genArithmeticOperators();
            genMath();
            genCastOperands();

            region("other");
            writeline($"public override string ToString() => $\"({compsNames.Select(x => "{" + x + "}").Aggregate((x, y) => x + ", " + y)})\";");
            endregion();


            endBlock();
            endBlock();
        }


        private void genConstants() {
            region("constants");

            var constantcomps = new string[compsNames.Length];
            
            for (int i = 0; i < constantcomps.Length; i++)
                constantcomps[i] = "0";

            summary("The zero vector: A vector where all components are equal to zero.");
            writeline($"public static readonly {vecName} zero = ({constantcomps.Aggregate((x, y) => x + ", " + y)});");
            
            for (int i = 0; i < compsNames.Length; i++) {
                constantcomps[i] = "1";
                summary("A unit vector pointing in the positive " + compsNames[i] + " direction.");
                writeline($"public static readonly {vecName} unit{compsNames[i]} = ({constantcomps.Aggregate((x, y) => x + ", " + y)});");
                constantcomps[i] = "0";
            }
            
            for (int i = 0; i < constantcomps.Length; i++)
                constantcomps[i] = "1";
            summary("A vector where all components are equal to one.");
            writeline($"public static readonly {vecName} one = ({constantcomps.Aggregate((x, y) => x + ", " + y)});");

            endregion();
        }

        private void genIndexAccessor() {
            linebreak();
            startBlock($"public {type} this[int i]");
            var indexerror = $"throw new IndexOutOfRangeException(\"{vecName}[\" + i + \"] is not a valid index\")";
            //get:
            startBlock("get => i switch");
            for (int i = 0; i < compsNames.Length; i++) {
                writeline($"{i} => {compsNames[i]},");
            }
            writeline($"_ => {indexerror}");
            endBlock(";");
            //set:
            startBlock("set"); startBlock("switch (i)");
            for (int i = 0; i < compsNames.Length; i++) {
                writeline($"case {i}: {compsNames[i]} = value; return;");
            }
            writeline($"default: {indexerror};");
            endBlock(); endBlock();

            endBlock();
        }

        private void genSwizzlingProperties() {
            region("swizzling properties");

            void _genswizzles(int size) {
                var swizzleindexes = new int[size];
                var swizzlevecname = name + size;

                for (int si = 0; si < Math.Pow(compsNames.Length, size); si++) {


                    string decl = $"public {swizzlevecname} {swizzleindexes.Select(x => compsNames[x]).Aggregate((x, y) => x + y)}",
                           get = $"=> new {swizzlevecname}({swizzleindexes.Select(x => compsNames[x]).Aggregate((x, y) => x + ", " + y)});";

                    if (swizzleindexes.Distinct().Count() == swizzleindexes.Length) {
                        startBlock(decl);
                        writeline($"get {get}");
                        startBlock("set");
                        for (int j = 0; j < swizzleindexes.Length; j++) {
                            writeline(compsNames[swizzleindexes[j]] + " = value." + compsNames[j] + ";");
                        }
                        endBlock();
                        endBlock();
                    } else {
                        writeline(decl + " " + get);
                    }

                    // increment swizzleIndexes
                    for (int i = 0; i < swizzleindexes.Length; i++) {
                        if (swizzleindexes[i] < compsNames.Length) {
                            if (swizzleindexes[i] == compsNames.Length - 1) {
                                for (int j = i; j >= 0; j--)
                                    swizzleindexes[j] = 0;
                                continue;
                            }
                            swizzleindexes[i]++;
                            break;
                        }
                    }
                }
            }

            for (int i = 2; i <= compsNames.Length; i++)
                _genswizzles(i);

            endregion();
        }

        private void genContructors() {
            region("constructors");

            string _paramslist(string[] args) {
                var res = new string[args.Length];
                for (int i = 0; i < res.Length; i++) res[i] = type + " " + args[i];
                return res.Aggregate((x, y) => x + ", " + y);
            }
            void _paramsassigmentcode(string[] args) {
                foreach (var item in args)
                    writeline($"this.{item} = {item};");
            }

            startBlock($"public {vecName}({_paramslist(compsNames)})");
            _paramsassigmentcode(compsNames);
            endBlock();

            endregion();
        }

        private void genArithmeticOperators() {
            region("arithmetic");
            writeline($"public {type} dot({vecName} v) => (this * v).sum;");
            linebreak();

            void _vecoperator(string opr) {
                var res = new string[compsNames.Length];
                for (int i = 0; i < res.Length; i++)
                    res[i] = $"a.{compsNames[i]} {opr} b.{compsNames[i]}";

                writeline($"public static {vecName} operator {opr}({vecName} a, {vecName} b) => new {vecName}({res.Aggregate((x, y) => x + ", " + y)});");
            }

            void _vecscalaroperator(string opr) {
                var res = new string[compsNames.Length];
                for (int i = 0; i < res.Length; i++)
                    res[i] = $"a.{compsNames[i]} {opr} s";

                writeline($"public static {vecName} operator {opr}({vecName} a, {type} s) => new {vecName}({res.Aggregate((v, w) => v + ", " + w)});");
            }

            _vecoperator("*");
            _vecoperator("/");
            _vecoperator("+");
            _vecoperator("-");
            linebreak();
            _vecscalaroperator("*");
            _vecscalaroperator("/");
            //_vecscalaroperator("+");
            //_vecscalaroperator("-");
            linebreak();
            writeline($"public static {vecName} operator -({vecName} v) => new {vecName}({compsNames.Select((x) => "-v." + x).Aggregate((z, x) => z + ", " + x)});");

            endregion();
        }

        private void genMath() {
            region("math");
            writeline($"public {type} distTo({vecName} o) => (o - this).length;");
            writeline($"public {type} angleTo({vecName} o) => ({type})Math.Acos(this.dot(o) / (this.length * o.length));");
            writeline($"public {vecName} lerp({vecName} o, {type} t) => this + ((o - this) * t);");
            writeline($"public {vecName} reflect({vecName} normal) => this - (normal * 2 * (this.dot(normal) / normal.dot(normal)));");

            endregion();
        }

        private void genCastOperands() {
            region("conversion");

            var tupletype = compsNames.Select(x => type).Aggregate((x, c) => x + ", " + c);
            var tupleparams = new string[compsNames.Length];
            for (int i = 0; i < compsNames.Length; i++)
                tupleparams[i] = "tuple.Item" + (i + 1);
            writeline($"public static implicit operator {vecName}(({tupletype}) tuple) => new {vecName}({tupleparams.Aggregate((x, v) => x + ", " + v)});");

            endregion();
        }
    }
}