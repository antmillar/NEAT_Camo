using NumSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TopolEvo.Architecture;

namespace TopolEvo.NEAT
{
    /// <summary>
    /// Configuration Settings for the NEAT algorithm
    /// </summary>
    public static class Config
    {
        public const double mutateRate = 0.2;
        public const string fitnessTarget = "min";

        //global random singleton
        public static readonly System.Random globalRandom = new System.Random();
        public static int genomeID = 0;
    }

    /// <summary>
    /// Population class which holds Genomes and Fitness information
    /// </summary>
    public class Population
    {

        //constructor
        public Population(int size)
        {
            for (int i = 0; i < size; i++)
            {
                Genomes.Add(new Genome());
            }
        }

        //properties
        protected internal List<Genome> Genomes { get; set; } = new List<Genome>();
        protected internal List<NDArray> Outputs { get; set; } = new List<NDArray>();
        protected internal double totalFitness { get; set; }

        public Genome GetGenomeByID(int id) => Genomes.Single(x => x.ID == id);
 
        //methods
        //currently has to be run after the fitnesses are calculated
        public void NextGeneration()
        {


            //for (int i = Genomes.Count / 2; i < Genomes.Count; i++)
            //{
            //    Genomes[i] = new Genome(Genomes[i - Genomes.Count/2]);
            //}

            //foreach (var genome in Genomes)
            //{
            //    //could create a copy in here instead and return it
            //    genome.Mutate();
            //}

            //replace old list with new one
            var newGenomes = new List<Genome>();
            var parents = new List<Genome>();

            //elitist keep the parents

            //for (int i = 0; i <4; i++)
            //{
            //    newGenomes.Add(new Genome(Genomes[i]));
            //}

            //creating two children so only loop half for the parent1 s 
            for (int i = 0; i < Genomes.Count; i++)
            {
                var runningtotal = 0.0;
                var lotteryBall = Config.globalRandom.NextDouble();

                foreach (var genome in Genomes)
                {
                    //using 1 minus fit, because minimising
                    runningtotal += (1 / genome.Fitness);
                    var proportion = runningtotal / totalFitness;

                    //loop until we find the proportional selection
                    if (lotteryBall < proportion)
                    {
                        parents.Add(genome);
                        break;
                    }
                }
            }

            //loop over parent 1s
            foreach(var parent1 in parents)
            {
                var runningtotal = 0.0;
                var lotteryBall = Config.globalRandom.NextDouble();

                foreach (var parent2 in Genomes)
                {
                    //using 1 minus fit, because minimising
                    runningtotal += (1 /  parent2.Fitness);
                    var proportion = runningtotal / totalFitness;

                    //loop until we find the proportional selection
                    if (lotteryBall < proportion)
                    {
                        
                        var child = new Genome();
                        child.CrossOver(parent1, parent2);
                        
                        newGenomes.Add(child);

                        break;
                    }
                }
            }
            Genomes = newGenomes;
        }

        //evaluates the current set of genomes
        public Dictionary<int, NDArray> Evaluate(NDArray coords)
        {

            var Outputs = new Dictionary<int, NDArray>();

            for (int i = 0; i < Genomes.Count; i++)
            {
                var net = new Network(Genomes[i]);
                var output = net.ForwardPass(coords);
                Outputs[Genomes[i].ID] = output;
            }

            return Outputs;

        }

        public void SortByFitness()
        {
            if (Config.fitnessTarget == "min")
            {
                Genomes = Genomes.OrderBy(x => x.Fitness).ToList();
            }
            else if (Config.fitnessTarget == "max")
            {
                Genomes = Genomes.OrderByDescending(x => x.Fitness).ToList();
            }

            totalFitness = Genomes.Select(x => 1 / x.Fitness).Sum();
        }

        public override string ToString()
        {
            return String.Join("\n", Genomes.Select(x => x.ToString()));
        }

        public string PrintInputs()
        {
            string s = "Node Inputs : " + String.Join(" ", Genomes.Select(x => x.ToString()));

            return s;
        }

    }
         
    public abstract class Gene { };

    public class NodeGene : Gene
    {
        protected internal string _activationType;
        protected internal int _id;
        protected internal string _type;
        protected internal List<int> _inputs;
        protected internal double _bias;

        public NodeGene(int id, string type, string activationType = "tanh")
        {
            _id = id;
            _type = type;
            _inputs = new List<int>();
            _activationType = activationType;
            _bias = Config.globalRandom.NextDouble() * 2 - 1.0;
        }

        //copy constructor
        public NodeGene(NodeGene parent)
        {
            _id =  parent._id;
            _type = parent._type;
            _inputs = parent._inputs;
            _activationType = parent._activationType;
            _bias = parent._bias;

        }

        public override string ToString()
        {
            return _id.ToString();
        }

    }
    public class ConnectionGene : Gene
    {
        protected internal double Weight { get; set; }
        protected internal int _inputNode;
        protected internal int _outputNode;

        public ConnectionGene(int inputNode, int outputNode)
        {
            _inputNode = inputNode;
            _outputNode = outputNode;

            Weight = Config.globalRandom.NextDouble() * 2 - 1.0;
        }

        //copy constructor
        public ConnectionGene(ConnectionGene parent)
        {
            _inputNode = parent._inputNode;
            _outputNode = parent._outputNode;

            Weight = parent.Weight;
        }

        //pick a weight randomly from parents
        public void CrossOver(ConnectionGene parent1, ConnectionGene parent2)
        {

            var test = Config.globalRandom.NextDouble();
            Weight = test > 0.5 ? parent1.Weight : parent2.Weight;

        }

        public override string ToString()
        {
            return $"({_inputNode}, {_outputNode}) w = {Math.Round(Weight, 2)} ";
        }
    }


    public class Genome
    {
        public List<NodeGene> Nodes { get; set; }
        public List<ConnectionGene> Connections { get; set; }
        protected internal double Fitness { get; set; }

        public int ID { get; set; } 


        public Genome()
        {
            //initialise a standard architecture
            Nodes = new List<NodeGene>();
            Connections = new List<ConnectionGene>();
            Fitness = 0.0;
            ID = Config.genomeID++;

            Nodes.Add(new NodeGene(0, "input"));
            Nodes.Add(new NodeGene(1, "input"));
            Nodes.Add(new NodeGene(2, "hidden"));
            Nodes.Add(new NodeGene(3, "hidden"));
            Nodes.Add(new NodeGene(4, "hidden"));
            Nodes.Add(new NodeGene(5, "hidden"));
            Nodes.Add(new NodeGene(6, "output", "sigmoid"));

            Connections.Add(new ConnectionGene(0, 2));
            Connections.Add(new ConnectionGene(0, 3));
            Connections.Add(new ConnectionGene(0, 4));
            Connections.Add(new ConnectionGene(0, 5));

            Connections.Add(new ConnectionGene(1, 2));
            Connections.Add(new ConnectionGene(1, 3));
            Connections.Add(new ConnectionGene(1, 4));
            Connections.Add(new ConnectionGene(1, 5));

            Connections.Add(new ConnectionGene(2, 6));
            Connections.Add(new ConnectionGene(3, 6));
            Connections.Add(new ConnectionGene(4, 6));
            Connections.Add(new ConnectionGene(5, 6));

            CalculateInputs();
        }

        //copy constructor
        public Genome(Genome parent)
        {
            Nodes = new List<NodeGene>();
            Connections = new List<ConnectionGene>();
            Fitness = parent.Fitness;
            ID = Config.genomeID++;

            foreach (var node in parent.Nodes)
            {
                Nodes.Add(new NodeGene(node));
            }

            foreach(var conn in parent.Connections)
            {
                Connections.Add(new ConnectionGene(conn));
            }
        }

        public NodeGene GetNodeByID(int id) => Nodes.Single(x => x._id == id);
        

        //populate the list of input node ids into each node
        private void CalculateInputs()
        {
            foreach(NodeGene nodeGene in Nodes)
            {
                foreach(ConnectionGene connection in Connections)
                {
                    if(connection._outputNode == nodeGene._id)
                    {
                        nodeGene._inputs.Add(connection._inputNode);
                    }
                }
            }
        }

        //mutates just the connection genes currently
        public void Mutate()
        {
            foreach(ConnectionGene connection in Connections)
            {
                if (Config.globalRandom.NextDouble() < Config.mutateRate)
                {
                    //need to clamp weights?
                    connection.Weight += (Config.globalRandom.NextDouble() - 0.5) / 2.0;
                }
            }
        }

        /// <summary>
        /// Crossover weights randomly between matching connections.
        /// </summary>
        /// <param name="other"></param>
        public void CrossOver(Genome parent1, Genome parent2)
        {
            //apply random crossover for each gene
            for (int i = 0; i < Connections.Count; i++)
            {
                Connections[i].CrossOver(parent1.Connections[i], parent2.Connections[i]);
            }

            ////alternative single crossover point based approach

            //var count = parent1.Connections.Count;
            //var crossPt = Config.globalRandom.Next(count);

            ////one point crossover swap slices of lists
            //Connections = parent1.Connections.Take(crossPt).Concat(parent2.Connections.Skip(crossPt)).ToList();
        }

        public override string ToString()
        {
            string s = "Nodes : " + String.Join(" ", Nodes.Select(x => x.ToString()));
            s += " | Conns : " + String.Join(" ", Connections.Select(x => x.ToString()));

            return s;
        }

    }
    
}
