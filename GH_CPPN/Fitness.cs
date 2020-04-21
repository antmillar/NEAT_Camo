using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using NumSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TopolEvo.NEAT;

namespace TopolEvo.Fitness
{

    public static class Fitness
    {
        /// <summary> 
        /// Static class where user create a fitness function, must take input genomes and assign the fitness attribute of each genome
        /// </summary>
        public static List<double> Function(Population pop, Dictionary<int, Matrix<double>> outputs, Matrix<double> coords)
        {
            //create a target grid
            

            var targetOutput = CreateTarget(outputs[pop.Genomes[0].ID].RowCount, outputs[pop.Genomes[0].ID].ColumnCount, coords);

            var fitnesses = new List<double>();
    
            foreach (KeyValuePair<int, Matrix<double> > entry in outputs)
            {

                //L1 Norm
                //var fitness = (entry.Value - targetOutput).PointwiseAbs().ToRowMajorArray().Sum();

                //L2 Norm
                var fitness = Math.Sqrt((entry.Value - targetOutput).PointwisePower(2).ToRowMajorArray().Sum());
                
                fitnesses.Add(fitness);
                pop.GetGenomeByID(entry.Key).Fitness = fitness;
            }

            fitnesses.Sort();

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
    }
}
