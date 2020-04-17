using NumSharp;
using System.Collections.Generic;
using TopolEvo.NEAT;

namespace TopolEvo.Fitness
{

    public static class Fitness
    {
        /// <summary> 
        /// Static class where user create a fitness function, must take input genomes and assign the fitness attribute of each genome
        /// </summary>
        public static List<double> Function(Population pop, List<NDArray> outputs, NDArray coords)
        {
            //create a target grid
            var targetOutput = CreateTarget(outputs[0].Shape, coords);

            var fitnesses = new List<double>();

            for (int i = 0; i < outputs.Count; i++)
            {
                var fitness = np.sum(np.abs(outputs[i] - targetOutput));
                fitnesses.Add(fitness);
                pop.Genomes[i].Fitness = fitness;
                //need to connect up the fitness and output and genome better here without using indices
            }

            return fitnesses;
            //have a config setting for min max fitnesses
        }

        public static NDArray CreateTarget(Shape shape, NDArray coords)
        {
            NDArray values = np.zeros(shape);

            for (int i = 0; i < values.Shape[0]; i++)
            {   
                ////equation of circle
                //if(Math.Pow(coords[i, 0].GetDouble(),2) + Math.Pow(coords[i, 1].GetDouble(), 2) < 0.3)
                //{
                //    values[i] = 1.0;
                //}

                //horizontal partition
                //if (coords[i, 0].GetDouble() < 0.0)
                //{
                //    values[i] = 1.0;
                //}

                //allblack

                values[i] = 1.0;

            }



            return values;
        }
    }
}
