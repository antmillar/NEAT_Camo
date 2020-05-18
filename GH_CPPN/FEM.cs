using System;
using System.Collections.Generic;
using System.Drawing;
using BriefFiniteElementNet;
using BriefFiniteElementNet.Elements;
using MathNet.Numerics.LinearAlgebra;
using Rhino.Geometry;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using TopolEvo.Display;

namespace GH_CPPN
{

    //Not used in the camouflage version, was used in 3d voxel grids
    public static class FEM
    {

        public static Model CreateModel(Matrix<double> coords, Matrix<double> occupancy, int xSize, int ySize, int zSize)
        {
            // Initiating Model, Nodes and Members
            var model = new Model();

            var nodes = new Dictionary<int, Node>();
            var nodeIndices = new List<int>();

            //if occupied add node to model
            for (int i = 0; i < coords.RowCount; i++)
            {
                if (occupancy[i, 0] == 1.0)
                {
                    nodes[i] = new Node(coords[i, 0], coords[i, 1], coords[i, 2]);
                    nodeIndices.Add(i);
                }
            }

            //for node in nodes, connect to neighbours

            int groundLevel = 0; // nodeIndices.Select(i => i / (xSize * ySize)).Min();
            int topLevel = zSize - 1; // nodeIndices.Select(i => i / (xSize * ySize)).Max();
            //int topNode = nodeIndices.Max();
            var b = nodeIndices.Where(value => (value / (xSize * ySize) == 5)).Count();

            int topNode;
            try
            {
                topNode = nodeIndices.Where(value => (value / (xSize * ySize) == topLevel)).Max(); //this line fails if can't find something on top row.
            }
            catch
            {
                return null;
            }    
                
                int fixedCount = 0;

            var sec = new BriefFiniteElementNet.Sections.UniformParametric1DSection(a: 0.1, iy: 0.1, iz: 0.1);
            var mat = BriefFiniteElementNet.Materials.UniformIsotropicMaterial.CreateFromYoungPoisson(210e9, 0.3);

            var elements = new ConcurrentBag<Element>();

            Parallel.For(0, nodeIndices.Count, i =>
            {
                var index = nodeIndices[i];

                int x = (index % (xSize * ySize)) % xSize;
                int y = (index % (xSize * ySize)) / xSize; //integer division
                int z = index / (xSize * ySize); //integer division

                var xNeigh = index + 1;
                var yNeigh = index + xSize;
                var zNeigh = index + xSize * ySize;

                //lock nodes on the ground floor
                if (z == groundLevel)
                {
                    nodes[index].Constraints = Constraint.Fixed;
                    fixedCount++;
                }

                if (x < xSize - 1 & nodeIndices.Contains(xNeigh))
                {
                    var xBar = new BarElement(nodes[index], nodes[xNeigh]) { Label = index.ToString() + ":" + xNeigh.ToString() };
                    xBar.Material = mat;
                    xBar.Section = sec;
                    elements.Add(xBar);
                }

                if (y < ySize - 1 & nodeIndices.Contains(yNeigh))
                {
                    var yBar = new BarElement(nodes[index], nodes[yNeigh]) { Label = index.ToString() + ":" + yNeigh.ToString() };
                    yBar.Material = mat;
                    yBar.Section = sec;
                    elements.Add(yBar);
                }

                if (z < zSize - 1 & nodeIndices.Contains(zNeigh))
                {
                    var zBar = new BarElement(nodes[index], nodes[zNeigh]) { Label = index.ToString() + ":" + zNeigh.ToString() };
                    zBar.Material = mat;
                    zBar.Section = sec;
                    elements.Add(zBar);
                }
            });
            

            model.Nodes.AddRange(nodes.Values);
            model.Elements.Add(elements.ToArray());

            //Applying load
            var force = new Force(0, -5000, -0, 0, 0, 0);

            nodes[topNode].Loads.Add(new NodalLoad(force));//adds a load with LoadCase of DefaultLoadCase to node loads

            model.Solve();

            return model;
        }

        public static Tuple<Mesh, List<Line>> MakeFrame(Model model, List<double> displacements)
        {
            var points = new List<Box>();
            var beams = new List<Line>();
            var meshes = new List<Mesh>();
            Mesh meshComb = new Mesh();

            if (model is null | displacements is null) return new Tuple<Mesh, List<Line>>(meshComb, beams);

            foreach (var node in model.Nodes)
            {
                var size = 0.02; //1.0 / xSize;
                var bbox = new BoundingBox(-size, -size, -size, size, size, size);
                var box = new Box(bbox);

                box.Transform(Transform.Translation(node.Location.X, node.Location.Y, node.Location.Z));
                var temp = new Mesh();

                points.Add(box);
            }

            foreach (var bar in model.Elements)
            {
                var start = bar.Nodes[0].Location;
                var end = bar.Nodes[1].Location;

                var line = new Line(new Point3d(start.X, start.Y, start.Z), new Point3d(end.X, end.Y, end.Z));
                beams.Add(line);
            }


            var maxdisp = displacements.Select(i => Math.Abs(i)).Max();
            var scale = 0.33 / maxdisp;

            for (int i = 0; i < points.Count; i++)
            {

                var mesh = Mesh.CreateFromBox(points[i], 1, 1, 1);

                Color color = ColorScale.ColorFromHSL((0.33 - scale * Math.Abs(displacements[i])), 1.0, 0.5);
                Color[] colors = Enumerable.Repeat(color, 24).ToArray();


                mesh.VertexColors.AppendColors(colors);
                meshes.Add(mesh.DuplicateMesh());

            }

            meshComb.Append(meshes);

            return new Tuple<Mesh, List<Line>>(meshComb, beams);

        }

        public static List<double> GetDisplacements(Model model)
        {
            if (model is null) return null;
            var displacements = new List<double>();

            foreach (Node node in model.Nodes)
            {
                displacements.Add(node.GetNodalDisplacement().DY);

            }

            return displacements;
        }

        public static List<double> GetStresses(Model model)
        {
            var xStresses = new List<double>();
            var yStresses = new List<double>();
            var zStresses = new List<double>();

            foreach (var element in model.Elements)
            {
                var temp = element as BarElement;
                xStresses.Add((temp.GetInternalForceAt(0)).Fx);
                yStresses.Add((temp.GetInternalForceAt(0)).Fy);
                zStresses.Add((temp.GetInternalForceAt(0)).Fz);
            }

            var a = xStresses.Sum();
            var b = yStresses.Sum();
            var c = zStresses.Sum();

            return zStresses;
        }

        public static double vonMisesStress(Force stress)
        {
            return Math.Sqrt((Math.Pow((stress.Fx - stress.Fy), 2) + Math.Pow((stress.Fx - stress.Fy), 2) + Math.Pow((stress.Fx - stress.Fy), 2)) / 2);
        }
            }


}
