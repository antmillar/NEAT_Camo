using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GH_CPPN
{
    class NEAT
    {
        public static System.Random globalRandom = new System.Random();

        public class Population
        {
            public List<Genome> _genomes;

            public Population(int size)
            {
                _genomes = new List<Genome>();

                for (int i = 0; i < size; i++)
                {
                    _genomes.Add(new Genome());
                }
            }

            public override string ToString()
            {
                return String.Join("\n", _genomes.Select(x => x.ToString()));
            }

        }
         
        public class Gene { };

        public class NodeGene : Gene
        {
            string _activationType;
            int _id;

            public NodeGene(int id)
            {
                _id = id;
            }

            public override string ToString()
            {
                return _id.ToString();
            }

        }
        public class ConnectionGene : Gene
        {
            double _weight;
            int _inputNode;
            int _outputNode;

            public ConnectionGene(int inputNode, int outputNode)
            {
                _inputNode = inputNode;
                _outputNode = outputNode;

                _weight = globalRandom.NextDouble() * 2 - 1.0;
            }

            public override string ToString()
            {
                return $"({_inputNode}, {_outputNode}) w = {Math.Round(_weight, 2)} ";
            }
        }


        public class Genome
        {
            List<Gene> _nodes;
            List<Gene> _connections;
            float _fitness;

            public Genome()
            {
                //initialise a standard architecture
                _nodes = new List<Gene>();
                _connections = new List<Gene>();

                _nodes.Add(new NodeGene(0));
                _nodes.Add(new NodeGene(1));
                _nodes.Add(new NodeGene(2));
                _nodes.Add(new NodeGene(3));

                _connections.Add(new ConnectionGene(0, 1));
                _connections.Add(new ConnectionGene(0, 2));
                _connections.Add(new ConnectionGene(1, 3));
                _connections.Add(new ConnectionGene(2, 3));

            }

            public override string ToString()
            {
                string s = "nodes : " + String.Join(" ", _nodes.Select(x => x.ToString()));
                s += " conns : " + String.Join(" ", _connections.Select(x => x.ToString()));

                return s;
            }
        }
    }
}
