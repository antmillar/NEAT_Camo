using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NumSharp;


using WIP;

namespace test
{
  
    //MODELS
    public interface Model
    {
        NDArray ForwardPass(NDArray input);
    }

    public class Network : Model
    {
        int _inputCount;
        NEAT.Genome _genome;
        public List<List<NEAT.ConnectionGene>> _layers;

        public Network(NEAT.Genome genome)
        {
            _inputCount = 0;
            _genome = genome;


            foreach(NEAT.NodeGene nodeGene in _genome.Nodes)
            {
                if(nodeGene._type == "input")
                {
                    _inputCount += 1;
                }
            }
        }

        //calculates the layers for the network based on nodes and connections
        public void GenerateLayers()
        {
            var currentNodes = new List<int>();
            _layers = new List<List<NEAT.ConnectionGene>>();

            //find input nodes
            foreach (NEAT.NodeGene nodeGene in _genome.Nodes)
            {
                if (nodeGene._type == "input")
                {
                    currentNodes.Add(nodeGene._id);
                }
            }

            var output = MakeLayer(currentNodes);

        }
        public List<int> MakeLayer(List<int> currentNodes)
        {

            var currentLayer = new List<NEAT.ConnectionGene>();
            var potentialNodes = new List<int>();
            var nextNodes = new List<int>();

            //find all connections starting from current nodes and their output nodes
            foreach (NEAT.ConnectionGene connectionGene in _genome.Connections)
            {
                if (currentNodes.Contains(connectionGene._inputNode))
                {
                    currentLayer.Add(connectionGene);
                    potentialNodes.Add(connectionGene._outputNode);
                }
            }

            //finds nodes where all inputs are from current nodes
            foreach (var id in potentialNodes)
            {
                var inputs = _genome.GetNodeByID(id)._inputs;

                //check if all inputs into node are from the current nodes
                if (currentNodes.Intersect(inputs).Count() == inputs.Count())
                {
                    nextNodes.Add(id);
                }
            }

            //keep only nodes with inputs from current nodes
            currentLayer.RemoveAll(x => !nextNodes.Contains(x._outputNode));

            _layers.Add(currentLayer);

            //recursively traverse the network
            if (nextNodes.Count == 0)
            {
                return nextNodes;
            }
            else
            {
                return MakeLayer(nextNodes);
            }
        }

        public NDArray ForwardPass(NDArray input)
        {
            if (_inputCount != input.shape[1]) throw new IncorrectShapeException($"Network has {_inputCount} inputs, input data has shape {input.shape}");

            //var input0 = null;
            //var input1 = null;

         


            throw new NotImplementedException();
        }
    }
    public class CustomModel : Model
    {
        public NDArray ForwardPass(NDArray input)
        {
            //input has size numDims x inputFeats
            //output has size numDims x outputFeats
            //weights has size inputFeats x outputFeats

            //TODO create weights, create biases,

            //number of input feats
            int num_input = input.shape[1];
            int num_hidden = 16;
            int num_output = 1;

            var sigmoid = new Sigmoid();
            var tanh = new Tanh();

            var fc1 = new Linear(num_input, num_hidden);
            var x = fc1.Apply(input);
            x = tanh.Apply(x);

            var fc2 = new Linear(num_hidden, num_hidden);
            x = fc2.Apply(x);
            x = tanh.Apply(x);

            var fc3 = new Linear(num_hidden, num_output);
            x = fc3.Apply(x);
            x = sigmoid.Apply(x);

            return x;
        }
    }

    public class MLP : Model
    {
        /// <summary> Creates a model with a num_layers each with num_neurons. </summary>

        List<ILayer> layers;

        public MLP(int _input_feats, int _output_feats, int _num_hiddenlayers = 0, int _num_neurons = 16)
        {
            layers = new List<ILayer>();
            var layerIn = new Linear(_input_feats, _num_neurons);
            layers.Add(layerIn);

            //hidden layers
            for (int i = 0; i < _num_hiddenlayers; i++)
            {
                layers.Add(new Linear(_num_neurons, _num_neurons));
            }

            var layerOut = new Linear(_num_neurons, _output_feats);
            layers.Add(layerOut);
        }

        public NDArray ForwardPass(NDArray input)
        {
            var x = input;
            var tanh = new Tanh();
            var sigmoid = new Sigmoid();

            for (int i = 0; i < layers.Count - 1; i++)
            {
                x = layers[i].Apply(x);
                x = tanh.Apply(x);
            }

            //final layer needs to be sigmoid so output in range (0, 1)
            x = layers[layers.Count - 1].Apply(x);
            x = sigmoid.Apply(x);

            return x;
        }
    }


    //LAYERS
    public interface ILayer
    {
        NDArray Apply(NDArray input);

    }

    public class Linear : ILayer
    {
        NDArray weights;
        NDArray biases;

        public Linear(int _input_feats, int _output_feats, bool _bias = true)
        {
            //initialise weights and biases
            weights = np.random.uniform(-1.0, 1.0, (_input_feats, _output_feats));
            biases = np.random.uniform(-1.0, 1.0, (1, _output_feats));// : np.zeros((1 , _output_feats)); //need to initialise these at some point
  
        }
        public NDArray Apply(NDArray input)
        {
 
            return np.matmul(input, weights) + biases;
        }
    }

    //ACTIVATIONS
    //interface for activation functions
    public interface IActivation
    {
        NDArray Apply (NDArray input);
    }

    public class Sigmoid : IActivation
    {
        public NDArray Apply(NDArray input)
        {
            return 1.0 / (1.0 + np.exp(-1.0 * input));
        }
    }

    public class Tanh : IActivation
    {
        public NDArray Apply(NDArray input)
        {
            return (np.exp(input) - np.exp(-1.0 * input)) / (np.exp(input) + np.exp(-1.0 * input));
        }
    }

    public class Relu : IActivation
    {
        public NDArray Apply(NDArray input)
        {
            return np.maximum(input, 0);
        }


    }

}
