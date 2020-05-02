
using MathNet.Numerics.LinearAlgebra;
using NumSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using TopolEvo.Architecture;
using TopolEvo.Utilities;

namespace TopolEvo.NEAT
{
    /// <summary>
    /// Configuration Settings for the NEAT algorithm
    /// </summary>
    public static class Config
    {
        internal const double mutateConnectionRate = 0.2;
        internal const string fitnessTarget = "min";
        internal static double survivalCutoff = 0.5;
        internal static double asexualRate = 0.25;

        internal static double addNodeRate = 0.00;
        internal static double addConnectionRate = 0.00; //in higher pop can increase this
        internal static double permuteOrResetRate = 0.9;


        //global random singleton
        internal static readonly System.Random globalRandom = new System.Random();
        internal static int genomeID = 0;
        internal static int newNodeCounter = 100;
    }

    /// <summary>
    /// Population class which holds Genomes and Fitness information
    /// </summary>
    public class Population
    {
        private int _inputDims;
            
        //constructor
        public Population(int size, int inputDims)
        {
            _inputDims = inputDims;

            for (int i = 0; i < size; i++)
            {
                Genomes.Add(new Genome(inputDims));
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

            //replace old list with new one
            var children = new List<Genome>();
            var parents = new List<Genome>();

            //elitist keep the parents

            //for (int i = 0; i <4; i++)
            //{
            //    children.Add(new Genome(Genomes[i]));
            //}

            for (int i = 0; i < Genomes.Count; i++)
            {
                int parentCount = (int)(Genomes.Count * Config.survivalCutoff);

                //ensure at least two parents
                parentCount = Math.Max(parentCount, 2);

                //add sorted genome to the parent pool up to cutoff percentage
                for (int j = 0; i < parentCount; i++)
                {
                    parents.Add(Genomes[i]);
                }

                ////adds genomes to parents using a fitness proportional approach

                //var runningtotal = 0.0;
                //var lotteryBall = Config.globalRandom.NextDouble();

                //foreach (var genome in Genomes)
                //{

                //    //using 1 / fit, because minimising
                //    runningtotal += (1 / genome.Fitness);
                //    var proportion = runningtotal / totalFitness;

                //    //loop until we find the proportional selection
                //    if (lotteryBall < proportion)
                //    {
                //        parents.Add(genome);
                //        break;
                //    }
                //}
            }

            int asexualCount = (int)(Genomes.Count * Config.asexualRate);

            //pick random parents and cross them to get child
            for (int i = 0; i < (Genomes.Count - asexualCount); i++)
            {
                var parent1 = parents[Config.globalRandom.Next(0, parents.Count)];
                var parent2 = parents[Config.globalRandom.Next(0, parents.Count)];

                var parentFittest = parent1.Fitness >= parent2.Fitness ? parent1 : parent2;

                //create child topology from fitter parent
                var child = parentFittest.Clone();

                //randomly crossover any shared connections
                child.CrossOver(parent1, parent2);

                children.Add(child);
            }

            //asexual reproduction
            for (int i = 0; i < asexualCount; i++)
            {
                var parent1 = parents[Config.globalRandom.Next(0, parents.Count)];

                children.Add(parent1.Clone());
            }


            ////loop over parent 1s
            //foreach (var parent1 in parents)
            //{
            //    var runningtotal = 0.0;
            //    var lotteryBall = Config.globalRandom.NextDouble();

            //    foreach (var parent2 in Genomes)
            //    {
            //        //using 1 / fit, because minimising
            //        runningtotal += (1 / parent2.Fitness);
            //        var proportion = runningtotal / totalFitness;

            //        //loop until we find the proportional selection
            //        if (lotteryBall < proportion)
            //        {

            //            var child = new Genome();
            //            child.CrossOver(parent1, parent2);

            //            children.Add(child);

            //            break;
            //        }
            //    }
            //}

            //mutate all the children genomes
            foreach (var child in children)
            {
                //mutate connection weights
                child.Mutate();

                //mutate add new connection
                var r = Config.globalRandom.NextDouble();

                if (r < Config.addConnectionRate)
                {
                    child.MutateAddConnection();
                }


                if (r < Config.addNodeRate)
                {
                    child.MutateAddNode();
                }
            }

            Genomes = children;
        }

        //evaluates the current set of genomes
        public Dictionary<int, Matrix<double>> Evaluate(Matrix<double> coords)
        {

            var Outputs = new Dictionary<int, Matrix<double>>();

            //convert each genome into a network and evaluate it
            //fine in parallel as not interrelated


            //Parallel.For(0, Genomes.Count, (i) =>
            //    {

            //        var net = new Network(Genomes[i]);
            //        var output = net.ForwardPass(coords);
            //        Outputs[Genomes[i].ID] = output;
            //    }
            // );

            for (int i = 0; i < Genomes.Count; i++)
            {
                var net = new Network(Genomes[i]);
                var output = net.ForwardPass(coords);
                Outputs[Genomes[i].ID] = output;
            }

            return Outputs;
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
        protected internal Func<double, double> _activationType;
        protected internal int _id;
        protected internal string _type;
        protected internal List<int> _inputs;

        public NodeGene(int id, string type, string activationType = "tanh")
        {
            _id = id;
            _type = type;
            _inputs = new List<int>();

            if (activationType == "tanh")
            {
                _activationType = Activation.Tanh();
            }
            else if (activationType == "sigmoid")
            {
                _activationType = Activation.Sigmoid();
            }  
        }

        //copy constructor
        public NodeGene(NodeGene parent)
        {
            _id =  parent._id;
            _type = parent._type;
            _inputs = new List<int>();

            foreach(int i in parent._inputs)
            {
                _inputs.Add(i);
            }

            _activationType = parent._activationType;

        }

        public override string ToString()
        {
            return _id.ToString();
        }

    }
    public class ConnectionGene : Gene
    {
        protected internal double Weight { get; set; }
        protected internal int InputNode { get; set; }
        protected internal int OutputNode { get; set; }

        public ConnectionGene(int inputNode, int outputNode)
        {
            InputNode = inputNode;
            OutputNode = outputNode;

            Weight = Utils.Gaussian(0.0, 5.0);
            //Weight = Config.globalRandom.NextDouble() * 2 - 1.0;
        }

        //copy constructor
        public ConnectionGene(ConnectionGene parent)
        {
            InputNode = parent.InputNode;
            OutputNode = parent.OutputNode;

            Weight = parent.Weight;
        }

        //pick a weight randomly from parents
        public void CrossOver(ConnectionGene parent1, ConnectionGene parent2)
        {

            var test = Config.globalRandom.NextDouble();
            Weight = test > 0.5 ? parent1.Weight : parent2.Weight;
        }

        public override bool Equals(object obj)
        {
            var other = obj as ConnectionGene;

            if (other == null)
                return false;

            if (InputNode == other.InputNode & OutputNode == other.OutputNode)
                return true;
            else
                return false;
        }

        public override string ToString()
        {
            return $"({InputNode}, {OutputNode}) w = {Math.Round(Weight, 2)} ";
        }
    }


    public class Genome
    {
        public List<NodeGene> Nodes { get; set; }
        public List<ConnectionGene> Connections { get; set; }
        protected internal double Fitness { get; set; }
        public int ID { get; set; }
        public string FitnessAsString { get; set; }
        public BriefFiniteElementNet.Model FEMModel { get; set; }

        public Genome(int inputNodes = 3 , int hiddenNodes = 3, int outputNodes = 1)
        {
            //initialise a standard architecture
            Nodes = new List<NodeGene>();
            Connections = new List<ConnectionGene>();
            Fitness = 0.0;
            ID = Config.genomeID++;

            var nodeCount = 0;

            //add input nodes and ongoing connections
            for (int i = 0; i < inputNodes; i++)
            {
                Nodes.Add(new NodeGene(nodeCount, "input"));

                //add connection to each node in the next layer
                for (int j = inputNodes; j < inputNodes + hiddenNodes ; j++)
                {
                    Connections.Add(new ConnectionGene(nodeCount, j));
                }
                nodeCount++;
            }
            
            //add hidden nodes and ongoing connections and biases
            for (int i = 0; i < hiddenNodes; i++)
            {
                Nodes.Add(new NodeGene(nodeCount, "hidden", "tanh"));
                Connections.Add(new ConnectionGene(9999, nodeCount));
                //add connection to each node in the next layer
                for (int j = inputNodes + hiddenNodes; j < inputNodes + hiddenNodes + outputNodes; j++)
                {
                    Connections.Add(new ConnectionGene(nodeCount, j));
                }

                nodeCount++;
            }

            //add output nodes and biases
            for (int i = 0; i < outputNodes; i++)
            {
                Nodes.Add(new NodeGene(nodeCount, "output", "sigmoid"));
                Connections.Add(new ConnectionGene(9999, nodeCount));
                nodeCount++;
            }

            Nodes.Add(new NodeGene(9999, "bias"));

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

        //deep clone of the genome
        public Genome Clone()
        {
            //uses the copy constructor to make deep copy
            return new Genome(this);
        }

        public NodeGene GetNodeByID(int id) => Nodes.Single(x => x._id == id);
        

        //populate the list of input node ids into each node
        private void CalculateInputs()
        {
            foreach(NodeGene nodeGene in Nodes)
            {
                foreach(ConnectionGene connection in Connections)
                {
                    if(connection.OutputNode == nodeGene._id)
                    {
                        if(!nodeGene._inputs.Contains(connection.InputNode))
                        {
                            nodeGene._inputs.Add(connection.InputNode);
                        }
                    }
                }
            }
        }

        //mutates just the connection genes currently
        public void Mutate()
        {
            foreach(ConnectionGene connection in Connections)
            {
                //need to clamp weights?

                if (Config.globalRandom.NextDouble() < Config.mutateConnectionRate)
                {
                    //90% chance permute, 10% chance create new value
                    if (Config.globalRandom.NextDouble() < Config.permuteOrResetRate)
                        {
                            connection.Weight += Utils.Gaussian(0.0, 1.25);
                        }
                    else
                        {
                            connection.Weight = Utils.Gaussian(0.0, 5.0);
                        }
                }
            }
        }

        /// <summary>
        /// Adds a new connection provided the random selection fulfills criteria below
        /// </summary>
        public void MutateAddConnection()
        {
            
            var rand1 = Config.globalRandom.Next(0, Nodes.Count);
            var startNode = Nodes[rand1];
            var rand2 = Config.globalRandom.Next(0, Nodes.Count);
            var endNode = Nodes[rand2];

            //order by ascending
            if(startNode._id > endNode._id)
            {
                var temp = endNode;
                endNode = startNode;
                startNode = temp;
            }

            //cancel if same node or if connected to a bias
            if (startNode == endNode | startNode._id == 9999 | endNode._id == 9999)
            {
            }
            //not allowed to connect output into the inputs
            else if (GetNodeByID(endNode._id)._type == "input")
            {

            }
            //not allowed to connect output as an input
            else if (GetNodeByID(startNode._id)._type == "output")
            {

            }
            else
            {
                //prevent repeated connections
                var newConnection = new ConnectionGene(startNode._id, endNode._id);

                bool unique = !Connections.Contains(newConnection);

                //no repeated nodes
                if (unique)
                {
                    Connections.Add(newConnection);
                    CalculateInputs();
                }

            }
        }

        public void MutateAddNode()
        {
            var choice = Config.globalRandom.Next(0, Connections.Count);

            var existingConnection = Connections[choice];
            if(existingConnection.InputNode == 9999) { return; }

            var newNodeIndex = Config.newNodeCounter++;
            var newNode = new NodeGene(newNodeIndex, "hidden");

            var newConnectionIn = new ConnectionGene(existingConnection.InputNode, newNode._id);
            var newConnectionOut = new ConnectionGene(newNode._id, existingConnection.OutputNode);
            var newConnectionBias = new ConnectionGene(9999, newNode._id);

            Nodes.Add(newNode);
            Connections.Add(newConnectionIn);
            Connections.Add(newConnectionOut);
            Connections.Add(newConnectionBias);

            CalculateInputs();
        }


        /// <summary>
        /// Crossover weights randomly between parents to create a new child genome. 
        /// If the topologies don't match take the weight from the fitter parent.
        /// </summary>
        public void CrossOver(Genome parent1, Genome parent2)
        {

            var parentWeaker = parent1.Fitness < parent2.Fitness ? parent1 : parent2;

            for (int i = 0; i < Connections.Count; i++)
            {
                //if connection present in both choose random otherwise just keep fitter parents
                if (parentWeaker.Connections.Contains(Connections[i]))
                {
                    int index = parentWeaker.Connections.FindIndex(a => a.Equals(Connections[i]));

                    Connections[i].CrossOver(Connections[i], parentWeaker.Connections[index]);
                }
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
