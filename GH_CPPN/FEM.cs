using System;
using System.Collections.Generic;
using BriefFiniteElementNet;
using BriefFiniteElementNet.Elements;
using MathNet.Numerics.LinearAlgebra;
using Rhino.Geometry;

namespace GH_CPPN
{
    public static class FEM
    {

        public static Tuple<List<Box>, List<Line>> MakeFrame()
        {

            var coords = PopulateCoords(2, 2, 3, 3);
            var side = 2.0;
            var counter = 0;
            //var coords = Matrix<double>.Build.Dense(12, 3);
            var points = new List<Box>();
            var beams = new List<Line>();

            var xSide = 1.0 / 2;
            var ySide = 1.0 / 2;
            var zSide = 1.0 / 3;

            for (int i = 0; i < coords.RowCount; i++)
            {
                var x = coords[i, 0];
                var y = coords[i, 1];
                var z = coords[i, 2];

                var size = 0.125;
                var bbox = new BoundingBox(-size, -size, -size, size, size, size);
                var box = new Box(bbox);


                box.Transform(Transform.Translation(x, y, z));
                var temp = new Mesh();

                points.Add(box);

                var line1 = new Line(new Point3d(x, y, z), new Point3d(x, y, (z + zSide) % 1));
                var line2 = new Line(new Point3d(x, y, z), new Point3d((x + xSide) % 1, y, z));
                var line3 = new Line(new Point3d(x, y, z), new Point3d(x, (y + ySide) % 1, z));

                beams.Add(line1);
                beams.Add(line2);
                beams.Add(line3);

            }

             return new Tuple<List<Box>, List<Line>>(points, beams);

        }

        public static List<double> AnalyseFrame(List<Box> boxes)
        {

            // Initiating Model, Nodes and Members
            var model = new Model();

            var nodes = new Dictionary<int, Node>();

            for (int i = 0; i < boxes.Count; i++)
            {
                nodes[i] = new Node(boxes[i].Center.X, boxes[i].Center.Y, boxes[i].Center.Z);
            }

            //vertical connections
            var e1 = new TrussElement2Node(nodes[0], nodes[1]) { Label = "e1" };
            var e2 = new TrussElement2Node(nodes[3], nodes[4]) { Label = "e2" };
            var e3 = new TrussElement2Node(nodes[9], nodes[10]) { Label = "e3" };
            var e4 = new TrussElement2Node(nodes[6], nodes[7]) { Label = "e4" };

            //vertical connections
            var e5 = new TrussElement2Node(nodes[1], nodes[2]) { Label = "e5" };
            var e6 = new TrussElement2Node(nodes[4], nodes[5]) { Label = "e6" };
            var e7 = new TrussElement2Node(nodes[10], nodes[11]) { Label = "e7" };
            var e8 = new TrussElement2Node(nodes[7], nodes[8]) { Label = "e8" };
            //Note: labels for all members should be unique, else you will receive InvalidLabelException when adding it to model

            //floor connections
            var e9 = new TrussElement2Node(nodes[0], nodes[3]) { Label = "e9" };
            var e10 = new TrussElement2Node(nodes[3], nodes[9]) { Label = "e10" };
            var e11 = new TrussElement2Node(nodes[9], nodes[6]) { Label = "e11" };
            var e12 = new TrussElement2Node(nodes[6], nodes[0]) { Label = "e12" };

            //roof connections
            var e13 = new TrussElement2Node(nodes[1], nodes[4]) { Label = "e13" };
            var e14 = new TrussElement2Node(nodes[4], nodes[10]) { Label = "e14" };
            var e15 = new TrussElement2Node(nodes[10], nodes[7]) { Label = "e15" };
            var e16 = new TrussElement2Node(nodes[7], nodes[1]) { Label = "e16" };

            //roof connections
            var e17 = new TrussElement2Node(nodes[2], nodes[5]) { Label = "e17" };
            var e18 = new TrussElement2Node(nodes[5], nodes[11]) { Label = "e18" };
            var e19 = new TrussElement2Node(nodes[11], nodes[8]) { Label = "e19" };
            var e20 = new TrussElement2Node(nodes[8], nodes[2]) { Label = "e20" };


            e1.A = e2.A = e3.A = e4.A = e5.A = e6.A = e7.A = e8.A = e9.A = e10.A = e11.A = e12.A = e13.A = e14.A = e15.A = e16.A = e17.A = e18.A = e19.A = e20.A = 9e-4;
            e1.E = e2.E = e3.E = e4.E = e5.E = e6.E = e7.E = e4.E = e9.E = e10.E = e11.E = e12.E = e13.E = e14.E = e15.E = e16.E = e17.E = e18.E = e19.E = e20.E = 210e9;

            model.Nodes.AddRange(nodes.Values);
            model.Elements.Add(e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20);

            //Applying restrains

            foreach(KeyValuePair<int, Node> keyValuePair in nodes)
            {
                keyValuePair.Value.Constraints = Constraint.Fixed;
            }

            //node to load

            nodes[11].Constraints = Constraint.RotationFixed;
            nodes[10].Constraints = Constraint.RotationFixed;

            //Applying load
            var force = new Force(-1000, -1000, -5000, 0, 0, 0);
            nodes[11].Loads.Add(new NodalLoad(force));//adds a load with LoadCase of DefaultLoadCase to node loads

            //Adds a NodalLoad with Default LoadCase

            model.Solve();

            var reactions = new List<Force>();

            foreach (KeyValuePair<int, Node> keyValuePair in nodes)
            {
                reactions.Add(keyValuePair.Value.GetSupportReaction());
            }

            var absForces = new List<double>();

            foreach(var reaction in reactions)
            {
                var absForce = Math.Abs(reaction.Fx) + Math.Abs(reaction.Fy) + Math.Abs(reaction.Fz);
                absForces.Add(absForce);
            }

            return absForces;
        }


        public static string Example1()
        {
            Console.WriteLine("Example 1: Simple 3D truss with four members");


            // Initiating Model, Nodes and Members
            var model = new Model();

            var n1 = new Node(1, 1, 0);
            n1.Label = "n1";//Set a unique label for node
            var n2 = new Node(-1, 1, 0) { Label = "n2" };//using object initializer for assigning Label
            var n3 = new Node(1, -1, 0) { Label = "n3" };
            var n4 = new Node(-1, -1, 0) { Label = "n4" };
            var n5 = new Node(0, 0, 1) { Label = "n5" };

            var e1 = new TrussElement2Node(n1, n5) { Label = "e1" };
            var e2 = new TrussElement2Node(n2, n5) { Label = "e2" };
            var e3 = new TrussElement2Node(n3, n5) { Label = "e3" };
            var e4 = new TrussElement2Node(n4, n5) { Label = "e4" };
            //Note: labels for all members should be unique, else you will receive InvalidLabelException when adding it to model

            e1.A = e2.A = e3.A = e4.A = 9e-4;
            e1.E = e2.E = e3.E = e4.E = 210e9;

            model.Nodes.Add(n1, n2, n3, n4, n5);
            model.Elements.Add(e1, e2, e3, e4);

            //Applying restrains


            n1.Constraints = n2.Constraints = n3.Constraints = n4.Constraints = Constraint.Fixed;
            n5.Constraints = Constraint.RotationFixed;


            //Applying load
            var force = new Force(0, 0, -1000, 0, 0, 0);
            n5.Loads.Add(new NodalLoad(force));//adds a load with LoadCase of DefaultLoadCase to node loads

            //Adds a NodalLoad with Default LoadCase

            model.Solve();

            var r1 = n1.GetSupportReaction();
            var r2 = n2.GetSupportReaction();
            var r3 = n3.GetSupportReaction();
            var r4 = n4.GetSupportReaction();


            var rt = r1 + r2 + r3 + r4;//shows the Fz=1000 and Fx=Fy=Mx=My=Mz=0.0

            Console.WriteLine("Total reactions SUM :" + rt.ToString());

            return rt.ToString();
        }


        public static Matrix<double> PopulateCoords(int xSize, int ySize, int zSize, int dims)
        {
            //populate coords

            var coords = Matrix<double>.Build.Dense(xSize * ySize * zSize, 3);

            //var shift = subdivisions / 2;

            //if (dims == 2)
            //{
            //    for (int i = -shift; i < shift; i++)
            //    {
            //        for (int j = -shift; j < shift; j++)
            //        {
            //            //coords are in range [-0.5, 0.5]
            //            coords[(i + shift) * subdivisions + j + shift, 0] = 1.0 * i / subdivisions;
            //            coords[(i + shift) * subdivisions + j + shift, 1] = 1.0 * j / subdivisions;
            //        }
            //    }
            //}

            if (dims == 3)
            {
                for (int i = 0; i < xSize; i++)
                {
                    for (int j = 0; j < ySize; j++)
                    {
                        for (int k = 0; k < zSize; k++)
                        {
                            //coords are in range [-0.5, 0.5]

                            coords[i * zSize * ySize + j * zSize + k, 0] = 1.0 * i / (xSize - 1);
                            coords[i * zSize * ySize + j * zSize + k, 1] = 1.0 * j / (ySize - 1);
                            coords[i * zSize * ySize + j * zSize + k, 2] = 1.0 * k / (zSize - 1);
                        }
                    }
                }

                coords -= 0.5;
            }

            return coords;
        }

    }

}
