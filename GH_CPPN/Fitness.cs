﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rhino.Geometry;
using NumSharp;
using System.Drawing;

using CPPN.NEAT;
using CPPN.Net;

namespace CPPN.Fitness
{
 
    public static class Fitness
    {
        /// <summary> Static class where user create a fitness function, must take input genomes and assign the fitness attribute of each genome </summary>
        /// 
        //user defined fitness function
        public static List<double> Function(Population pop, NDArray coords)
        {
            List<Network> nets = new List<Network>();

            for (int i = 0; i < pop.Genomes.Count; i++)
            {
                nets.Add(new Network(pop.Genomes[i]));
            }


            var outputs = new List<NDArray>();

            foreach (var net in nets)
            {
                var output = net.ForwardPass(coords);
                outputs.Add(output);
            }

            var targetOutput = np.zeros(outputs[0].Shape);
            var fitnesses = new List<double>();

            for (int i = 0; i < outputs.Count; i++)
            {
                var fitness = np.mean(outputs[i] - targetOutput);
                fitnesses.Add(fitness);
                pop.Genomes[i].Fitness = fitness;
                
                //need to connect up the fitness and output and genome better here without using indices
            }

            return fitnesses;


            //have a config setting for min max fitnesses

        }
    }
}