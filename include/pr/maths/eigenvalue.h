//*********************************************
// Eigenvalue Decomposition
//  Copyright (c) Rylogic Ltd 2025
//*********************************************
// Jacobi eigenvalue algorithm for real symmetric matrices.
#pragma once
#include <vector>
#include <span>
#include <algorithm>
#include <numeric>
#include <cmath>
#include <cassert>

namespace pr::maths
{
	// Result of eigenvalue decomposition
	struct EigenResult
	{
		std::vector<float> values;  // N eigenvalues, sorted descending
		std::vector<float> vectors; // N×N eigenvectors, column-major (column i corresponds to values[i])
	};

	// Compute eigenvalues and eigenvectors of a real symmetric matrix using the Jacobi rotation method.
	// 'matrix' is N×N in row-major layout. Only the upper triangle is read (symmetry is assumed).
	// Returns eigenvalues in descending order with corresponding eigenvectors as columns.
	inline EigenResult EigenSymmetric(std::span<float const> matrix, int n, int max_sweeps = 100)
	{
		assert(std::ssize(matrix) >= n * n);
		if (n == 0)
			return {};

		// Work on a copy of the matrix (row-major). The diagonal converges to eigenvalues.
		auto A = std::vector<float>(matrix.begin(), matrix.begin() + n * n);

		// Eigenvector matrix starts as identity (column-major)
		auto V = std::vector<float>(n * n, 0.0f);
		for (int i = 0; i != n; ++i)
			V[i * n + i] = 1.0f;

		// Accessors for row-major A and column-major V
		auto a = [&](int r, int c) -> float& { return A[r * n + c]; };
		auto v = [&](int r, int c) -> float& { return V[c * n + r]; };

		for (int sweep = 0; sweep != max_sweeps; ++sweep)
		{
			// Compute the sum of off-diagonal magnitudes to check convergence
			auto off_diag = 0.0f;
			for (int p = 0; p != n; ++p)
				for (int q = p + 1; q != n; ++q)
					off_diag += a(p, q) * a(p, q);

			if (off_diag < 1e-20f)
				break;

			// Sweep over all upper-triangle elements
			for (int p = 0; p != n; ++p)
			{
				for (int q = p + 1; q != n; ++q)
				{
					auto apq = a(p, q);
					if (std::abs(apq) < 1e-12f)
						continue;

					// Compute the Jacobi rotation angle
					auto diff = a(q, q) - a(p, p);
					float t; // tan(theta)
					if (std::abs(diff) < 1e-12f)
					{
						t = 1.0f; // theta = pi/4
					}
					else
					{
						auto ang = diff / (2.0f * apq);
						t = (ang >= 0.0f ? 1.0f : -1.0f) / (std::abs(ang) + std::sqrt(1.0f + ang * ang));
					}

					auto c = 1.0f / std::sqrt(1.0f + t * t); // cos(theta)
					auto s = t * c;                          // sin(theta)

					// Update matrix A: rotate rows/cols p and q
					a(p, q) = 0.0f;
					a(q, p) = 0.0f;
					a(p, p) -= t * apq;
					a(q, q) += t * apq;

					// Update off-diagonal elements in rows/cols p and q
					for (int r = 0; r != n; ++r)
					{
						if (r == p || r == q)
							continue;

						auto arp = a(r, p);
						auto arq = a(r, q);
						a(r, p) = c * arp - s * arq;
						a(p, r) = a(r, p);
						a(r, q) = s * arp + c * arq;
						a(q, r) = a(r, q);
					}

					// Accumulate eigenvector rotations
					for (int r = 0; r != n; ++r)
					{
						auto vrp = v(r, p);
						auto vrq = v(r, q);
						v(r, p) = c * vrp - s * vrq;
						v(r, q) = s * vrp + c * vrq;
					}
				}
			}
		}

		// Extract eigenvalues from the diagonal
		auto result = EigenResult{};
		result.values.resize(n);
		for (int i = 0; i != n; ++i)
			result.values[i] = a(i, i);

		// Sort by descending eigenvalue, reorder eigenvectors to match
		auto order = std::vector<int>(n);
		std::iota(order.begin(), order.end(), 0);
		std::sort(order.begin(), order.end(), [&](int a, int b) { return result.values[a] > result.values[b]; });

		auto sorted_values = std::vector<float>(n);
		auto sorted_vectors = std::vector<float>(n * n);
		for (int i = 0; i != n; ++i)
		{
			sorted_values[i] = result.values[order[i]];
			for (int r = 0; r != n; ++r)
				sorted_vectors[i * n + r] = v(r, order[i]);
		}

		result.values = std::move(sorted_values);
		result.vectors = std::move(sorted_vectors);
		return result;
	}
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::maths
{
	PRUnitTestClass(EigenSymmetricTests)
	{
		PRUnitTestMethod(Identity)
		{
			// Identity matrix: eigenvalues all 1, eigenvectors are axis-aligned
			auto I = std::vector<float>{ 1,0,0, 0,1,0, 0,0,1 };
			auto result = EigenSymmetric(I, 3);

			PR_EXPECT(result.values.size() == 3);
			for (int i = 0; i != 3; ++i)
				PR_EXPECT(std::abs(result.values[i] - 1.0f) < 1e-5f);
		}

		PRUnitTestMethod(Diagonal)
		{
			// Diagonal matrix: eigenvalues are the diagonal entries
			auto D = std::vector<float>{ 5,0,0, 0,2,0, 0,0,8 };
			auto result = EigenSymmetric(D, 3);

			PR_EXPECT(std::abs(result.values[0] - 8.0f) < 1e-5f);
			PR_EXPECT(std::abs(result.values[1] - 5.0f) < 1e-5f);
			PR_EXPECT(std::abs(result.values[2] - 2.0f) < 1e-5f);
		}

		PRUnitTestMethod(KnownSymmetric3x3)
		{
			// Symmetric matrix with known eigenvalues.
			// M = [2 1 0; 1 3 1; 0 1 2] has eigenvalues 4, 2, 1.
			auto M = std::vector<float>{ 2,1,0, 1,3,1, 0,1,2 };
			auto result = EigenSymmetric(M, 3);

			PR_EXPECT(std::abs(result.values[0] - 4.0f) < 1e-4f);
			PR_EXPECT(std::abs(result.values[1] - 2.0f) < 1e-4f);
			PR_EXPECT(std::abs(result.values[2] - 1.0f) < 1e-4f);

			// Verify eigenvectors: Mv should equal λv for each eigenvector
			for (int k = 0; k != 3; ++k)
			{
				auto lambda = result.values[k];
				for (int r = 0; r != 3; ++r)
				{
					auto mv = 0.0f;
					for (int c = 0; c != 3; ++c)
						mv += M[r * 3 + c] * result.vectors[k * 3 + c];

					auto lv = lambda * result.vectors[k * 3 + r];
					PR_EXPECT(std::abs(mv - lv) < 1e-4f);
				}
			}
		}

		PRUnitTestMethod(SingleElement)
		{
			auto M = std::vector<float>{ 7.0f };
			auto result = EigenSymmetric(M, 1);
			PR_EXPECT(result.values.size() == 1);
			PR_EXPECT(std::abs(result.values[0] - 7.0f) < 1e-5f);
		}

		PRUnitTestMethod(EmptyMatrix)
		{
			auto result = EigenSymmetric({}, 0);
			PR_EXPECT(result.values.empty());
			PR_EXPECT(result.vectors.empty());
		}
	};
}
#endif
