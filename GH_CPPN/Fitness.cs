using GH_CPPN;
using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using NumSharp;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TopolEvo.NEAT;

namespace TopolEvo.Fitness
{

    public static class Fitness
    {

        public static double Map(double num, double x1, double y1, double x2, double y2)
        {
            var result = ((num - x1) / (y1 - x1)) * (y2 - x2) + x2;  

            return result;
        }


        /// <summary> 
        /// Static class where user create a fitness function, must take input genomes and assign the fitness attribute of each genome
        /// </summary>
        /// 

        public static List<double> Function(Population pop, Dictionary<int, Matrix<double>> outputs, Matrix<double> coords, Matrix<double> occupancy, int subdivisions)
        {
            //create a target grid
            
            //var targetOutput = CreateTarget(outputs[pop.Genomes[0].ID].RowCount, outputs[pop.Genomes[0].ID].ColumnCount, coords);
            var targetOutput = occupancy;

            var fitnesses = new List<double>();
    
            //loop over each member of the population
            foreach (KeyValuePair<int, Matrix<double> > entry in outputs)
            {
                var FEMWeight = 1.0;
                try
                {
                    var FEMModel = FEM.CreateModel(coords, entry.Value, subdivisions, subdivisions, subdivisions);
                    var displacements = FEM.GetDisplacements(FEMModel);
                    var scaled = displacements.Max(i => Math.Abs(i)) * 10e7;
                    var map = 10 - Map(scaled, 0.01, 1.00, 0, 10);
                    FEMWeight = map;
                }
                catch
                {
                    FEMWeight = -10.0;
                }

                var vals = entry.Value.ToRowMajorArray();
                var height = 0;
                //Height
                for (int i = 0; i < subdivisions; i++)
                {
                    var cellsInHoriz = vals.Skip(i * subdivisions * subdivisions).Take(subdivisions * subdivisions).Sum();

                    if(cellsInHoriz > 0)
                    {
                        height++;
                    }
                }

                var depth = 0;


                for (int i = 0; i < subdivisions; i++)
                {
                    var cellsInVert = 0.0;

                    for (int j = 0; j < subdivisions; j++)
                    {
                        cellsInVert += vals.Skip(j * subdivisions * subdivisions + i * subdivisions).Take(subdivisions).Sum();
                    }

                    if (cellsInVert > 0)
                    {
                        depth++;
                    }
                }


                var cellCount = vals.Sum();

                var bottomLayerCount = vals.Take(subdivisions * subdivisions).Sum();

                var bottomWeight = 0.0;
                
                if(bottomLayerCount > 10)
                { bottomWeight = 10.0; }
                else
                {
                    bottomWeight = bottomLayerCount;
                }
                var topLayerCount = vals.Skip(subdivisions * subdivisions * (subdivisions - 1)).Take(subdivisions * subdivisions).Sum();

                var topWeight = 0.0;

                if (topLayerCount > 10)
                { topWeight = 10.0; }
                else
                {
                    topWeight = topLayerCount;
                }



                var fitness = height + (10.0 - (10.0 * cellCount / (subdivisions * subdivisions * subdivisions))) + FEMWeight; // height + depth + (10 - cellCount / 100) + FEMWeight; //+ bottomWeight / 10 + topWeight / 10;
                
                //L1 Norm
                //var fitness = (entry.Value - targetOutput).PointwiseAbs().ToRowMajorArray().Sum();

                //L2 Norm
                //var fitness = Math.Sqrt((entry.Value - targetOutput).PointwisePower(2).ToRowMajorArray().Sum());
                
                fitnesses.Add(fitness);
                pop.GetGenomeByID(entry.Key).Fitness = fitness;
            }

            fitnesses.Sort();
            fitnesses.Reverse();

            return fitnesses;
            //have a config setting for min max fitnesses
        }

        public static Matrix<double> CreateTarget(int rows, int cols, Matrix<double> coords)
        {
  
            var targets = Matrix<double>.Build.Dense(rows, cols, 0.0);

            for (int i = 0; i < targets.RowCount; i++)
            {
                ////equation of circle
                if (Math.Pow(coords[i, 0], 2) + Math.Pow(coords[i, 1], 2) + Math.Pow(coords[i, 2], 2)  < 0.2)
                {
                    //values[i] = 1.0;
                    targets[i, 0] = 1.0;
                }

                //vert bar
                //if (coords[i, 0] < -0.25 || coords[i, 0] > 0.25)
                //{
                //    values[i] = 1.0;
                //}


                //vert partition
                //if (coords[i, 0] < 0.0)
                //{
                //    values[i] = 1.0;
                //}

                //all white

                //values[i] = 1.0;

            }

            return targets;
        }

        //find points in voxel grid contained inside the target mesh
        public static Matrix<double> CreateOccupancy(int rows, int cols, Matrix<double> coords, Mesh inputMesh)
        {

            inputMesh.FillHoles();

            var occupancy = Matrix<double>.Build.Dense(rows, cols, 0.0);

            for (int i = 0; i < occupancy.RowCount; i++)
            {
                ////equation of circle
                var pt = new Point3d(coords[i, 0], coords[i, 1], coords[i, 2]);

                if (inputMesh.IsPointInside(pt, 0.5, false))
                {
                    occupancy[i, 0] = 1.0;
                }

            }

            return occupancy;
        }

        public static Dictionary<int, Matrix<double>> OutputOccupancy(Dictionary<int, Matrix<double>> outputs)
        {

            foreach (int key in outputs.Keys.ToList())
            {
                outputs[key] = outputs[key].Map(i => i > 0.5 ? 1.0 : 0.0);
            }

            return outputs;
        }
    }
}
