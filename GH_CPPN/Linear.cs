using System;
using System.Collections.Generic;
using NumSharp;

namespace GH_CPPN
{

    /// <summary>
    /// Simple implementation of Multilayer perceptron
    /// Not used in the final project
    /// </summary>
    public class temp
    {
        //MODELS
        public interface Model
        {
            NDArray ForwardPass(NDArray input);
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
            NDArray Apply(NDArray input);
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
}
