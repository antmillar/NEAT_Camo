
using MathNet.Numerics.LinearAlgebra;
using NumSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using TopolEvo.Architecture;
using TopolEvo.Speciation;
using TopolEvo.Utilities;

namespace TopolEvo.NEAT
{
    /// <summary>
    /// Configuration Settings for the NEAT algorithm
    /// </summary>
    public static class Config
    {
        internal const string fitnessTarget = "min";
        internal static double survivalCutoff = 0.25;
        internal static double rateAsexual = 0.25;

        internal const double rateMutateConnection = 0.4;
        internal static double rateAndNode = 0.2;
        internal static double rateAddConnection = 0.3; //in higher pop can increase this
        internal static double ratePermuteOrReset = 0.9;

        internal static readonly System.Random globalRandom = new System.Random(); 
        internal static int genomeID = 0;
        internal static int speciesID = 0;
        internal static int newNodeCounter = 100;
    }
    
    /// <summary>
    /// Population class which holds Genomes and Fitness information
    /// </summary>
    public class Population
    {
        private int _inputDims;
        public Dictionary<string, int> AddedConnections;
            
        //constructor
        public Population(int size, int inputDims)
        {
            _inputDims = inputDims;
            AddedConnections = new Dictionary<string, int>();

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
 
        public void NextGen(List<Species> speciesList)
        {
            //var speciesList = _speciesList.Where(x => (x.Stagnant == false)).ToList();

            var popSize = Genomes.Count;
            Genomes.Clear();

            var totalFitness = speciesList.Select(x => (1.0 /x.MinFitness)).Sum();
            //var expSum = speciesList.Select(x => Math.Exp(1.0/ x.Fitness)).Sum();
            var remainder = popSize;
            var count = speciesList.Count;

            foreach(var species in speciesList)
            {
                //var newProp = Math.Exp((1.0/species.Fitness)) / expSum * popSize; ;
                var proportion = (1.0 / species.MinFitness) / totalFitness * popSize;
                //var proportion = newProp;

                var decimalPart = proportion - Math.Truncate(proportion);

                //ensures the total adds up at the end
                int numChildren = decimalPart < 0.5 ? (int) Math.Floor(proportion) : (int) Math.Ceiling(proportion);


                if(count == 1)
                {
                    numChildren = remainder;
                }
                numChildren = numChildren < 0 ? 0 : numChildren;
                remainder -= numChildren;
                NextGeneration(species, numChildren);

                count--;
            }
        }

        public void NextGeneration(Species species, int numChildren)
        {
            if (numChildren == 0) return; //hacky....

            //replace old list with new one
            var children = new List<Genome>();
            var parents = new List<Genome>();

            int parentCount = (int)(species.Genomes.Count * Config.survivalCutoff);
            //ensure at least one parent
            parentCount = Math.Max(parentCount, 1);

            //only add a proportion of the population to parent pool based on survival cutoff
            for (int i = 0; i < parentCount; i++)
            {
                parents.Add(species.Genomes[i]);
            }

            int asexualCount = (int)(numChildren * Config.rateAsexual);

            if (parentCount == 1)
            {
                asexualCount = numChildren;
            }


            //pick random parents and cross them to get child
            for (int i = 0; i < (numChildren - asexualCount); i++)
            {
                //don't cross the same parents
                var parent1 = parents[Config.globalRandom.Next(0, parents.Count)];
                var parent2 = parents[Config.globalRandom.Next(0, parents.Count)];


                var parentFittest = parent1.Fitness >= parent2.Fitness ? parent1 : parent2;
                //var parentWeaker = parent1.Fitness < parent2.Fitness ? parent1 : parent2;

                //create child topology from fitter parent
                var child = parentFittest.Clone();

                //randomly crossover any shared connections
                child.CrossOver(parent1, parent2);

                children.Add(child);
            }


            //asexual reproduction
            for (int i = 0; i < asexualCount; i++)
            {
                //add asexual in order of parents
                var parent1 = parents[Config.globalRandom.Next(0, parents.Count)];

                children.Add(parent1.Clone());
            }


            //mutate all the children genomes
            foreach (var child in children)
            {
                //mutate individual connection weights
                child.MutateConnectionWeights();

                //mutate add new connection
                var r1 = Config.globalRandom.NextDouble();

                if (r1 < Config.rateAddConnection)
                {
                    child.MutateAddConnection();
                }

                var r2 = Config.globalRandom.NextDouble();

                if (r2 < Config.rateAndNode)
                {
                    child.MutateAddNode(this);
                }

                //if (r2 < 0.2)
                //{
                //    child.MutateNodeType();
                //}

                //if (r2 < 0.02)
                //{
                //    child.MutateRemoveNode();
                //}
            }

            //elitism 
            if (species.Genomes.Count > 5)
            {
                children[Config.globalRandom.Next(0, children.Count)] = parents[0].Clone();
            }

            species.Genomes = children;
            Genomes.AddRange(children);
        }

        //evaluates the current set of genomes
        public Dictionary<int, Matrix<double>> Evaluate(Matrix<double> coords)
        {

            var Outputs = new Dictionary<int, Matrix<double>>();

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
        protected internal int _id;
        protected internal string _type;
        protected internal List<int> _inputs;

        protected internal Activation Activation { get; set; }

        protected internal Enum NodeType; 

        public NodeGene(int id, string type, string activationType = "tanh")
        {
            _id = id;
            _type = type;
            _inputs = new List<int>();
            SetActivation(activationType);

        }

        public void SetActivation(string activationType)
        {

            if (activationType == "tanh")
            {
                Activation = Activations.Tanh();
            }
            else if (activationType == "tanhAbs")
            {
                Activation = Activations.TanhAbs();
            }
            else if (activationType == "sigmoid")
            {
                Activation = Activations.Sigmoid();
            }
            else if (activationType == "sin")
            {
                Activation = Activations.Sin();
            }
            else if (activationType == "fract")
            {
                Activation = Activations.Fract();
            }
            else if (activationType == "rescale")
            {
                Activation = Activations.Rescale();
            }
            else if (activationType == "downscale")
            {
                Activation = Activations.Downscale();
            }
            else if (activationType == "gaussian")
            {
                Activation = Activations.Gaussian();
            }
            else if (activationType == "square")
            {
                Activation = Activations.Square();
            }
            else if (activationType == "abs")
            {
                Activation = Activations.Abs();
            }
            else if (activationType == "cos")
            {
                Activation = Activations.Cos();
            }
            else if (activationType == "linear")
            {
                Activation = Activations.Linear();
            }
            else if (activationType == "random")
            {
                Activation = Activations.Random();
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

            Activation = parent.Activation;

        }
        public override bool Equals(object obj)
        {
            var other = obj as NodeGene;

            if (other == null)
                return false;

            if (_id == other._id)
                return true;
            else
                return false;
        }

        public override string ToString()
        {
            return _id.ToString() + " (" + Activation.Name + ") | " ;
        }

    }
    
    public class ConnectionGene : Gene
    {
        protected internal double Weight { get; set; }
        protected internal int InputNode { get; set; }
        protected internal int OutputNode { get; set; }
        protected internal bool Enabled { get; set; } = true;

        public ConnectionGene(int inputNode, int outputNode, double defaultWeight = double.NaN)
        {
            InputNode = inputNode;
            OutputNode = outputNode;

            if (double.IsNaN(defaultWeight))
            {
                Weight = Utils.Gaussian(0.0, 1.0);
            }
            else
            {
                Weight = defaultWeight;
            }
        }

        //copy constructor
        public ConnectionGene(ConnectionGene parent)
        {
            InputNode = parent.InputNode;
            OutputNode = parent.OutputNode;
            Enabled = parent.Enabled;

            Weight = parent.Weight;
        }

        public ConnectionGene Clone()
        {
            return new ConnectionGene(this);
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

        //ID for connection gene, will match in different instances if same input and output
        public string GetID()
        {
            return $"({InputNode}:{OutputNode})";
        }

        public override string ToString()
        {
            return $"({InputNode}, {OutputNode}) w = {Math.Round(Weight, 2)} ";
        }
    }


    public class Genome
    {
        public List<NodeGene> Nodes { get; set; } = new List<NodeGene>();
        public List<ConnectionGene> Connections { get; set; } = new List<ConnectionGene>();
        protected internal double Fitness { get; set; } = 0.0;
        protected internal int Complexity { get { return this.Connections.Count + this.Nodes.Count; } set { } }
        public int ID { get; set; }
        public string FitnessAsString { get; set; }
        public BriefFiniteElementNet.Model FEMModel { get; set; }
        public int SpeciesID { get; set; }

        public Genome(int inputNodes = 3 , int hiddenNodes = 2, int outputNodes = 1)
        {
            ID = Config.genomeID++;

            var nodeCounter = 0;

            //create default topology

            //add input nodes and ongoing connections
            for (int i = 0; i < inputNodes; i++)
            {
                Nodes.Add(new NodeGene(nodeCounter, "input"));

                //add connection to each node in the next layer
                for (int j = inputNodes; j < inputNodes + hiddenNodes ; j++)
                {
                    Connections.Add(new ConnectionGene(nodeCounter, j));
                }

                if (hiddenNodes == 0)
                {
                    for (int j = inputNodes; j < inputNodes + outputNodes; j++)
                    {
                        Connections.Add(new ConnectionGene(nodeCounter, j));
                     }
                }

                nodeCounter++;
            }
            
                //add hidden nodes and ongoing connections and biases
                for (int i = 0; i < hiddenNodes; i++)
                {
                    Nodes.Add(new NodeGene(nodeCounter, "hidden", "tanh"));
                    //add bias connection to each new node
                    Connections.Add(new ConnectionGene(9999, nodeCounter));

                    //add connection to each node in the next layer
                    for (int j = inputNodes + hiddenNodes; j < inputNodes + hiddenNodes + outputNodes; j++)
                    {
                        Connections.Add(new ConnectionGene(nodeCounter, j));
                    }
                    nodeCounter++;
                }

            //add output nodes and biases
            for (int i = 0; i < outputNodes; i++)
            {
                Nodes.Add(new NodeGene(nodeCounter, "output", "sigmoid"));
                Connections.Add(new ConnectionGene(9999, nodeCounter));
                nodeCounter++;
            }

            Nodes.Add(new NodeGene(9999, "bias"));

            CalculateInputs();
        }

        //copy constructor
        public Genome(Genome parent)
        {
            Nodes = new List<NodeGene>();
            Connections = new List<ConnectionGene>();
            ID = Config.genomeID++;
            SpeciesID = parent.SpeciesID;

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
        
        //measures the 'distance' between genomes for use in speciation
        public double Distance(Genome other)
        {
            var connDist = 0.0;
            var weightDist = 0.0;
            var actDist = 0.0;

            var longerGenome = this.Connections.Count > other.Connections.Count ? this : other;
            var shorterGenome = this.Connections.Count <= other.Connections.Count ? this : other;

            foreach (var conn in longerGenome.Connections)
            {
                //if disjoint/excess add 1 to count
                if (!shorterGenome.Connections.Contains(conn))
                {
                    connDist += 1.0;
                }

                //if present add weight difference
                if (shorterGenome.Connections.Contains(conn))
                {
                    var otherConn = shorterGenome.Connections.Find(a => a.Equals(conn));
                    weightDist = Math.Abs(otherConn.Weight - conn.Weight);
                }

                //could add check to see if they are both enabled
            }

            //measures the number of different activation functions on matching ID nodes
            foreach (var node in longerGenome.Nodes)
            {
                if (shorterGenome.Nodes.Contains(node))
                {
                    var otherNode = shorterGenome.Nodes.Find(a => a._id == node._id);
                    if (otherNode.Activation.Name != node.Activation.Name)
                    {
                        actDist += 1.0;
                    }
                }

                //could add check to see if they are both enabled
            }

            //want to add a weight distance


            //normalize
            //dist /= longerGenome.Connections.Count;
            var output = connDist  + actDist;

            return output;
        }

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
        public void MutateConnectionWeights()
        {
            foreach(ConnectionGene connection in Connections)
            {
                //need to clamp weights?

                if (Config.globalRandom.NextDouble() < Config.rateMutateConnection)
                {
                    //90% chance permute, 10% chance create new value
                    if (Config.globalRandom.NextDouble() < Config.ratePermuteOrReset)
                    {
                        connection.Weight += Utils.Gaussian(0.0, 2.0);
                        connection.Weight = Utils.Clamp(connection.Weight, 15.0);
                    }
                    else
                    {
                        connection.Weight = Utils.Gaussian(0.0, 5.0);
                        connection.Weight = Utils.Clamp(connection.Weight, 15.0);

                    }
                }
            }
        }

        //mutates just the connection genes currently
        public void MutateNodeType()
        {
            var rand = Config.globalRandom.Next(0, Nodes.Count);
            var node = Nodes[rand];

            var choices = new List<string>() { "tanh"};
            var pick = choices[Config.globalRandom.Next(0, choices.Count)];

            if(!(node._type == "output"))
            {
                node.SetActivation(pick);
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
                //prevent repeated and recursive connections
                var newConnection = new ConnectionGene(startNode._id, endNode._id);
                var newConnectionReverse = new ConnectionGene(endNode._id, startNode._id);

                bool unique = !Connections.Contains(newConnection) & !Connections.Contains(newConnectionReverse);

                //no repeated nodes
                if (unique & CheckForLoops(newConnection))
                {
                    Connections.Add(newConnection);
                    CalculateInputs();
                }

            }
        }

        public void MutateRemoveNode()
        {
            var rand = Config.globalRandom.Next(0, Nodes.Count);
            var nodeToRemove = Nodes[rand];

            //cancel if same node or if connected to a bias
            if (nodeToRemove._id == 9999)
            {
            }
            //not allowed to connect output into the inputs
            else if (GetNodeByID(nodeToRemove._id)._type == "input")
            {

            }
            //not allowed to connect output as an input
            else if (GetNodeByID(nodeToRemove._id)._type == "output")
            {

            }
            else
            {
                var connsToRemove = new List<ConnectionGene>();

                foreach (var conn in Connections)
                {
                    if (conn.InputNode == nodeToRemove._id | conn.OutputNode == nodeToRemove._id)
                    {
                        connsToRemove.Add(conn);
                    }
                }

                foreach (var c in connsToRemove)
                {
                    Connections.Remove(c);
                }

                Nodes.Remove(nodeToRemove);

                CalculateInputs();

            }

        }

        //checks if adding a connection would create a loop/cycle in the network
        //algo ported from https://github.com/CodeReclaimers/neat-python/blob/c2b79c88667a1798bfe33c00dd8e251ef8be41fa/neat/graphs.py
        public bool CheckForLoops(ConnectionGene newConnection)
        {
            var inNode = newConnection.InputNode;

            var visitedNodes = new HashSet<int>() { inNode };

            while (true)
            {
                var newNodesTraversed = 0;

                foreach (var conn in Connections)
                {
                    if (visitedNodes.Contains(conn.InputNode) & !visitedNodes.Contains(conn.OutputNode))
                    {
                        //if we find our way back to the start point we have a loop
                        if (conn.OutputNode == inNode)
                        {
                            return true;
                        }

                        visitedNodes.Add(conn.OutputNode);
                        newNodesTraversed += 1;
                    }

                    //if no new nodes have been traversed we've not added any loops
                    if (newNodesTraversed == 0) return false;
                }
            }
            
        }

        public void MutateAddNode(Population pop)
        {
            var choice = Config.globalRandom.Next(0, Connections.Count);

            var existingConnection = Connections[choice];
            if(existingConnection.InputNode == 9999) { return; }

            //check if existingConnection has been split before
            int newNodeID;

            if(pop.AddedConnections.Keys.Contains(existingConnection.GetID()))
            {
                newNodeID = pop.AddedConnections[existingConnection.GetID()];
            }
            else
            {
                newNodeID = Config.newNodeCounter++;
                pop.AddedConnections[existingConnection.GetID()] = newNodeID;
            }

            var choices = new List<string>() { "sin", "sigmoid", "gaussian" };
            var pick = choices[Config.globalRandom.Next(0, choices.Count)];

            var newNode = new NodeGene(newNodeID, "hidden", pick);

            //exit if this innovation has been created before
            if (Nodes.Contains(newNode)) { return;  }

            var existingBiasConnection = Connections.Single(x => x.InputNode == 9999 & x.OutputNode == existingConnection.OutputNode);

            //new connection in has weight 1
            var newConnectionIn = new ConnectionGene(existingConnection.InputNode, newNode._id, 1.0);

            //new connection out has weight equal to old connection
            var newConnectionOut = new ConnectionGene(newNode._id, existingConnection.OutputNode, existingConnection.Weight);
            var newConnectionBias = new ConnectionGene(9999, newNode._id, 1.0);

            Nodes.Add(newNode);
            Connections.Add(newConnectionIn);
            Connections.Add(newConnectionOut);
            Connections.Add(newConnectionBias);

            Connections.Remove(existingConnection);

            //existingConnection.Enabled = false;
            //existingConnection.Weight = 0.0;

            CalculateInputs();
        }

        public Genome Merge(Genome other)
        {
            var merged = new Genome(this);
            merged.ID = Config.genomeID++;
            merged.SpeciesID = SpeciesID;

            foreach(var node in other.Nodes)
            {
                if (!merged.Nodes.Contains(node))
                {
                    merged.Nodes.Add(node);
                }
            }

            foreach (var conn in other.Connections)
            {
                if (!merged.Connections.Contains(conn) & merged.CheckForLoops(conn))
                {
                    merged.Connections.Add(conn);


                }
            }
            merged.CalculateInputs();

            return merged;
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
        }

        public override string ToString()
        {
            string s = "Species : " + String.Join(" ", SpeciesID);
            s += " | Nodes : " + String.Join(" ", Nodes.Select(x => x.ToString()));
            s += " | Conns : " + String.Join(" ", Connections.Select(x => x.ToString()));

            return s;
        }
    }
}
