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
        public static List<double> Function(Population pop, Dictionary<int, NDArray> outputs, NDArray coords)
        {
            //create a target grid
            var targetOutput = CreateTarget(outputs[pop.Genomes[0].ID].Shape, coords);

            var fitnesses = new List<double>();
    
            foreach (KeyValuePair<int, NDArray> entry in outputs)
            {
                
                //L1 norm
                //var fitness = np.sum(np.abs(entry.Value - targetOutput));
                
                //L2 norm
                
                var fitness = np.sqrt( np.mean(np.power(entry.Value - targetOutput, 2)));


                fitnesses.Add(fitness);
                pop.GetGenomeByID(entry.Key).Fitness = fitness;
            }

            fitnesses.Sort();

            return fitnesses;
            //have a config setting for min max fitnesses
        }

        public static NDArray CreateTarget(Shape shape, NDArray coords)
        {
            NDArray values = np.zeros(shape);

            for (int i = 0; i < values.Shape[0]; i++)
            {
                ////equation of circle
                if (Math.Pow(coords[i, 0].GetDouble(), 2) + Math.Pow(coords[i, 1].GetDouble(), 2) + Math.Pow(coords[i, 2].GetDouble(), 2)  < 0.2)
                {
                    values[i] = 1.0;
                }

                //vert bar
                //if (coords[i, 0].GetDouble() < -0.25 || coords[i, 0].GetDouble() > 0.25)
                //{
                //    values[i] = 1.0;
                //}


                //vert partition
                //if (coords[i, 0].GetDouble() < 0.0)
                //{
                //    values[i] = 1.0;
                //}

                //all white

                //values[i] = 1.0;

            }



            return values;
        }
    }
}
