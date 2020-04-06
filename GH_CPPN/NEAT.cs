using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WIP
{
    public class NEAT
    {
        public static System.Random globalRandom = new System.Random();
        public static double mutateRate = 0.2;

        public class Population
        {
            List<Genome> Genomes { get; set; }

            public Population(int size)
            {
                Genomes = new List<Genome>();

                for (int i = 0; i < size; i++)
                {
                    Genomes.Add(new Genome());
                }
            }


            public void NextGeneration()
            {
                foreach(var genome in Genomes)
                {
                    //could create a copy in here instead and return it
                    genome.Mutate();
                }

            }

            public override string ToString()
            {
                return String.Join("\n", Genomes.Select(x => x.ToString()));
            }

        }
         
        public class Gene { };

        public class NodeGene : Gene
        {
            string _activationType;
            int _id;
            string _type;

            public NodeGene(int id, string type)
            {
                _id = id;
                _type = type;
            }



            public override string ToString()
            {
                return _id.ToString();
            }

        }
        public class ConnectionGene : Gene
        {
            public double Weight { get; set; }
            int _inputNode;
            int _outputNode;

            public ConnectionGene(int inputNode, int outputNode)
            {
                _inputNode = inputNode;
                _outputNode = outputNode;

                Weight = globalRandom.NextDouble() * 2 - 1.0;
            }

            public override string ToString()
            {
                return $"({_inputNode}, {_outputNode}) w = {Math.Round(Weight, 2)} ";
            }
        }


        public class Genome
        {
            List<Gene> Nodes { get; set; }
            List<Gene> Connections { get; set; }

            float _fitness;

            public Genome()
            {
                //initialise a standard architecture
                Nodes = new List<Gene>();
                Connections = new List<Gene>();

                Nodes.Add(new NodeGene(0, "input"));
                Nodes.Add(new NodeGene(1, "input"));
                Nodes.Add(new NodeGene(2, "hidden"));
                Nodes.Add(new NodeGene(3, "hidden"));
                Nodes.Add(new NodeGene(4, "output"));

                Connections.Add(new ConnectionGene(0, 2));
                Connections.Add(new ConnectionGene(0, 3));
                Connections.Add(new ConnectionGene(1, 2));
                Connections.Add(new ConnectionGene(1, 3));
                Connections.Add(new ConnectionGene(2, 4));
                Connections.Add(new ConnectionGene(3, 4));

            }

            //mutates just the connection genes currently
            public void Mutate()
            {
                foreach(ConnectionGene connection in Connections)
                {
                    if (globalRandom.NextDouble() < mutateRate)
                    {
                        //need to clamp weights?
                        connection.Weight += (globalRandom.NextDouble() - 0.5);
                    }
                }
            }

            public void CrossOver(Genome other)
            {


            }

            public override string ToString()
            {
                string s = "Nodes : " + String.Join(" ", Nodes.Select(x => x.ToString()));
                s += " | Conns : " + String.Join(" ", Connections.Select(x => x.ToString()));

                return s;
            }
        }
    }
}
