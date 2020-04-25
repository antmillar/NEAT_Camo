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

        public static Tuple<List<Box>, List<Line>> MakeFrame(Model model)
        {

            var points = new List<Box>();
            var beams = new List<Line>();

            foreach(var node in model.Nodes)
            {
                var size = 0.05;
                var bbox = new BoundingBox(-size, -size, -size, size, size, size);
                var box = new Box(bbox);

                box.Transform(Transform.Translation(node.Location.X, node.Location.Y, node.Location.Z));
                var temp = new Mesh();

                points.Add(box);
            }

            foreach(var bar in model.Elements)
            {
                var start = bar.Nodes[0].Location;
                var end = bar.Nodes[1].Location;

                var line = new Line(new Point3d(start.X, start.Y, start.Z), new Point3d(end.X, end.Y, end.Z));
                beams.Add(line);
            }

             return new Tuple<List<Box>, List<Line>>(points, beams);

        }

        public static Model CreateModel(Matrix<double> coords, int xSize, int ySize, int zSize)
        {
            // Initiating Model, Nodes and Members
            var model = new Model();

            var nodes = new Dictionary<int, Node>();

            for (int i = 0; i < coords.RowCount; i++)
            {
                nodes[i] = new Node(coords[i, 0], coords[i, 1], coords[i, 2]);
            }

            //for node in nodes, connect to neighbours

            //find neighbours
            for (int i = 0; i < xSize; i++)
            {
                for (int j = 0; j < ySize; j++)
                {
                    for (int k = 0; k < zSize; k++)
                    {
                        //coords are in range [-0.5, 0.5]
                        var loc = i * zSize * ySize + j * zSize + k;
                        var zNeigh = loc + 1;
                        var yNeigh = loc + zSize;
                        var xNeigh = loc + zSize * ySize;

                        var sec = new BriefFiniteElementNet.Sections.UniformParametric1DSection(a: 0.01, iy: 0.01, iz: 0.01, j: 0.01);
                        var mat = BriefFiniteElementNet.Materials.UniformIsotropicMaterial.CreateFromYoungPoisson(210e9, 0.3);

                        if (k < zSize - 1)
                        {
                            var a = new BarElement(nodes[loc], nodes[zNeigh]) { Label = loc.ToString() + ":" + zNeigh.ToString() };
                            a.Material = mat;
                            a.Section = sec;
                            model.Elements.Add(a);
                        }

                        if (j < ySize - 1)
                        {
                            var b = new BarElement(nodes[loc], nodes[yNeigh]) { Label = loc.ToString() + ":" + yNeigh.ToString() };
                            b.Material = mat;
                            b.Section = sec;
                            model.Elements.Add(b);
                        }

                        if (i < xSize - 1)
                        {
                            var c = new BarElement(nodes[loc], nodes[xNeigh]) { Label = loc.ToString() + ":" + xNeigh.ToString() };
                            c.Material = mat;
                            c.Section = sec;
                            model.Elements.Add(c);
                        }

                        coords[i * zSize * ySize + j * zSize + k, 0] = 1.0 * i / (xSize - 1);
                        coords[i * zSize * ySize + j * zSize + k, 1] = 1.0 * j / (ySize - 1);
                        coords[i * zSize * ySize + j * zSize + k, 2] = 1.0 * k / (zSize - 1);
                    }
                }
            }

            
            model.Nodes.AddRange(nodes.Values);

            //Applying restrains

            foreach (KeyValuePair<int, Node> keyValuePair in nodes)
            {
                //keyValuePair.Value.Constraints = new Constraint(DofConstraint.Fixed, DofConstraint.Fixed, DofConstraint.Released, DofConstraint.Released, DofConstraint.Released, DofConstraint.Released);
                //keyValuePair.Value.Constraints = Constraint.RotationFixed;
            }

            //fix bottom layer
            nodes[0].Constraints = Constraint.Fixed;
            nodes[3].Constraints = Constraint.Fixed;
            nodes[6].Constraints = Constraint.Fixed;
            nodes[9].Constraints = Constraint.Fixed;

            //node to load

            //Applying load
            var force = new Force(0, -0, -5000, 0, 0, 0);
            nodes[11].Loads.Add(new NodalLoad(force));//adds a load with LoadCase of DefaultLoadCase to node loads

            model.Solve();

            return model;
        }

        public static List<double> GetDisplacements(Model model)
        {
            var displacements = new List<double>();
            foreach (Node node in model.Nodes)
            {
                displacements.Add(node.GetNodalDisplacement().DZ);
            }

            return displacements;
        }

        public static List<double> GetStresses(Model model)
        {
            var stresses = new List<double>();

            foreach (var element in model.Elements)
            {
                var temp = element as BarElement;
                stresses.Add(vonMisesStress(temp.GetInternalForceAt(0)));
            }

            return stresses;
        }

        public static double vonMisesStress(Force stress)
        {
            return Math.Sqrt((Math.Pow((stress.Fx - stress.Fy), 2) + Math.Pow((stress.Fx - stress.Fy), 2) + Math.Pow((stress.Fx - stress.Fy), 2)) / 2);
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
