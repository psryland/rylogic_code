//*********************************************
// Neural Net
//  Copyright (c) Rylogic Ltd 2015
//*********************************************
#pragma once
#include "pr/neuralnet/forward.h"

namespace pr::neuralnet
{
	class Network
	{
		using Weight = float;
		using Weights = std::vector<Weight>;

		// The number of layers
		size_t m_layer_count;

		// The number of neurons per layer
		std::vector<size_t> m_impl_npl;
		size_t const* m_neurons_per_layer;

		// The biases per layer in the network
		// Note 'm_biases[0].size() == 0' because inputs don't have biases
		std::vector<Weights> m_biases;

		// The weights per layer in the network.
		// Note 'm_weights[0].size() == 0' because inputs don't have weights
		std::vector<Weights> m_weights;

		// Access the bias for 'neuron' in 'layer'
		Weight& bias(size_t layer, size_t neuron)
		{
			// The input layer has no weights
			assert(layer  >= 0 && layer  < m_weights.size()           && "layer out of range");
			assert(neuron >= 0 && neuron < m_neurons_per_layer[layer] && "neuron out of range");
			return m_biases[layer][neuron];
		}

		// Access the input weight for 'neuron' in 'layer'
		Weight& weight(size_t layer, size_t neuron)
		{
			// The input layer has no weights
			assert(layer  >= 0 && layer  < m_weights.size()           && "layer out of range");
			assert(neuron >= 0 && neuron < m_neurons_per_layer[layer] && "neuron out of range");
			return m_weights[layer][neuron * m_neurons_per_layer[layer-1]];
		}

		// Sigmoid node activation function
		Weight sigmoid(Weight z) const
		{
			return 1.0f / (1.0f + exp(-z));
		}

	public:
		Network(std::initializer_list<size_t> const& neurons_per_layer);

		// Given an input vector, find the output vector
		Weights Think(std::vector<Weight> const& input);

		// Train the network using stochastic gradient descent
		void Train();
	};
}