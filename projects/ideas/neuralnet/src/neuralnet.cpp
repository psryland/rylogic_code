//*********************************************
// Neural Net
//  Copyright (c) Rylogic Ltd 2015
//*********************************************

#include "pr\neuralnet\forward.h"
#include "pr\neuralnet\neuralnet.h"

namespace pr
{
	namespace neuralnet
	{
		Network::Network(std::initializer_list<size_t> const& neurons_per_layer)
			:m_layer_count(neurons_per_layer.size() - 1)
			,m_impl_npl(neurons_per_layer)
			,m_neurons_per_layer()
			,m_biases()
			,m_weights()
		{
			if (neurons_per_layer.size() < 2)
				throw std::exception("At least two layers are required, first is the input, last is the output");

			// Tread index position [-1] as the input layer and [m_layer_count-1] as the output layer
			m_neurons_per_layer = &m_impl_npl[1];
			for (size_t i = 0; i != m_layer_count; ++i)
			{
				// A bias for each neuron
				m_biases.emplace_back(m_neurons_per_layer[i]);

				// Each neuron in layer 0 is connected to each neuron in layer 1
				m_weights.emplace_back(m_neurons_per_layer[i-1] * m_neurons_per_layer[i]);
			}

			std::random_device rd;
			std::mt19937 gen(rd());
			std::normal_distribution<double> gaus_rand(0.0, 1.0);

			// Initialise the weights and biases using guassian distributed random numbers
			// with a mean of 0 and sd of 1 (for now)
			for (auto& layer : m_weights)
				for (auto& w : layer)
					w = static_cast<float>(gaus_rand(gen));

			for (auto& layer : m_biases)
				for (auto& b : layer)
					b = static_cast<float>(gaus_rand(gen));
		}

		// Given an input vector, find the output vector
		Network::Weights Network::Think(Weights const& input)
		{
			if (input.size() != m_neurons_per_layer[-1])
				throw std::exception(pr::FmtS("Input vector has the wrong dimension, expected %d", m_neurons_per_layer[-1]));

			Weights tmp0, tmp1;
			std::vector<Weights*> buf = {const_cast<Weights*>(&input), &tmp0, &tmp1};
			for (size_t i = 3; i <= m_layer_count; ++i)
				buf.push_back(buf[i - 2]);

			// Feed forward through each layer
			for (int i = 0; i != m_layer_count; ++i)
			{
				auto count0    = m_neurons_per_layer[i-1];
				auto count1    = m_neurons_per_layer[i  ];
				auto const& Ak = *buf[i + 0];  // Activations from the previous layer
				auto&       Aj = *buf[i + 1];  // Output activations from this layer

				assert(Ak.size() == count0);
				assert(m_biases[i].size() == count1);
				assert(m_weights[i].size() == count0*count1);
				Aj.resize(count1);

				auto* w = &m_weights[i][0]; // Weight values for this layer (count0 per neuron)
				auto* b = &m_biases[i][0];  // Bias values for this layer (1 per neuron)
				auto* a = &Aj[0];           // The output activations for this layer (1 per neuron)

				// Aj = sigmoid(Zj), Zj = SUM(Wk.Ak) + Bj
				for (auto j = 0; j != count1; ++j, ++b)
				{
					auto z = *b;
					for (auto k = 0; k != count0; ++k, ++w)
						z += Ak[k] * *w;

					*a++ = sigmoid(z);
				}
			}

			return std::move(*buf[m_layer_count]);
		}

		// Train the network using stochastic gradient descent
		void Network::Train()
		{
		}
	}
}

