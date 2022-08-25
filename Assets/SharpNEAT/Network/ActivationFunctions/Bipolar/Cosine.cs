using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;
using SharpNeat.Utility;

namespace SharpNeat.Network
{
    /// <summary>
    /// Cosine activation function with doubled period.
    /// </summary>
    public class Cosine : IActivationFunction
    {
        /// <summary>
        /// Default instance provided as a public static field.
        /// </summary>
        public static readonly IActivationFunction __DefaultInstance = new Cosine();

        /// <summary>
        /// Gets the unique ID of the function. Stored in network XML to identify which function a network or neuron 
        /// is using.
        /// </summary>
        public string FunctionId
        {
            get { return this.GetType().Name; }
        }

        /// <summary>
        /// Gets a human readable string representation of the function. E.g 'y=1/x'.
        /// </summary>
        public string FunctionString
        {
            get { return "y = cos(2*x)"; }
        }

        /// <summary>
        /// Gets a human readable verbose description of the activation function.
        /// </summary>
        public string FunctionDescription
        {
            get { return "Cosine function with doubled period.\r\nEffective xrange->[-Inf,Inf] yrange[-1,1]"; }
        }

        /// <summary>
        /// Gets a flag that indicates if the activation function accepts auxiliary arguments.
        /// </summary>
        public bool AcceptsAuxArgs
        {
            get { return false; }
        }

        /// <summary>
        /// Calculates the output value for the specified input value and optional activation function auxiliary arguments.
        /// </summary>
        public double Calculate(double x, double[] auxArgs)
        {
            var y = Math.Cos(2.0 * x);
            return y;
        }

        /// <summary>
        /// Calculates the output value for the specified input value and optional activation function auxiliary arguments.
        /// This single precision overload of Calculate() will be used in neural network code 
        /// that has been specifically written to use floats instead of doubles.
        /// </summary>
        public float Calculate(float x, float[] auxArgs)
        {   // ENHANCEMENT: Search for a math lib that operates on single precision floats.
            var y = (float)Math.Cos(2f * x);
            return y;
        }

        /// <summary>
        /// For activation functions that accept auxiliary arguments; generates random initial values for aux arguments for newly
        /// added nodes (from an 'add neuron' mutation).
        /// </summary>
        public double[] GetRandomAuxArgs(FastRandom rng, double connectionWeightRange)
        {
            throw new SharpNeatException("GetRandomAuxArgs() called on activation function that does not use auxiliary arguments.");
        }

        /// <summary>
        /// Genetic mutation for auxiliary argument data.
        /// </summary>
        public void MutateAuxArgs(double[] auxArgs, FastRandom rng, ZigguratGaussianSampler gaussianSampler, double connectionWeightRange)
        {
            throw new SharpNeatException("MutateAuxArgs() called on activation function that does not use auxiliary arguments.");
        }
    }
}
