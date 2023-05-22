using IEA.EA.Abstraction;
using IEA.ProblemInstance;
using System.Diagnostics;
using System.Text.Json;

namespace IEA.EA
{

    internal class ShuffleMutationParameters : IMutationParameters
    {
        public double MutationPropability { get; }

        public Problem Problem { get; }

        public static ShuffleMutationParameters FromJSON(string json, Problem problem)
        {
            JsonElement parameters = JsonDocument
                            .Parse(json)
                            .RootElement
                            .GetProperty("ShuffleMutationParameters");
            return new
                (
                parameters.GetProperty(nameof(MutationPropability)).GetDouble(),
                problem
                );
        }
        public ShuffleMutationParameters(double MutationProbability, Problem problem)
        {
            this.MutationPropability = MutationProbability;
            this.Problem = problem;
            Check();
        }

        private void Check()
        {
            Debug.Assert(MutationPropability > 0 && MutationPropability < 1);
        }
    }

    internal class ShuffleMutation : IMutation
    {
        private readonly ShuffleMutationParameters Parameters;
        public ShuffleMutation(ShuffleMutationParameters parameters)
        {
            Parameters = parameters;
        }

        public void Mutate(List<Solution> solutions)
        {
            Random random = new();
            foreach (Solution solution in solutions)
            {
                if (random.NextDouble() < Parameters.MutationPropability)
                {

                    int i = random.Next(1, Parameters.Problem.StudentsCount - 1);
                    int j = random.Next(1, Parameters.Problem.StudentsCount - 1);

                    for (int c = 0; c < Parameters.Problem.ClassesCount; c++)
                    {
                        int temp = solution.Assignment[i, c];
                        solution.Assignment[i, c] = solution.Assignment[j, c];
                        solution.Assignment[j, c] = temp;
                    }

                    solution.CastSemiFeasible();
                }
            }
        }
    }


    internal class NoiseMutationParameters : IMutationParameters
    {
        public double MutationPropability { get; }

        public Problem Problem { get; }

        public double FractionNoised { get; }

        public static NoiseMutationParameters FromJSON(string json, Problem problem)
        {
            JsonElement parameters = JsonDocument
                            .Parse(json)
                            .RootElement
                            .GetProperty("NoiseMutationParameters");
            return new
                (
                parameters.GetProperty(nameof(MutationPropability)).GetDouble(),
                problem,
                parameters.GetProperty(nameof(FractionNoised)).GetDouble()
                );
        }   

        public NoiseMutationParameters(double MutationProbability, Problem problem, double franctionNoised)
        {
            this.MutationPropability = MutationProbability;
            this.Problem = problem;
            FractionNoised = franctionNoised;
            Check();

        }

        private void Check()
        {
            Debug.Assert(MutationPropability > 0 && MutationPropability < 1);
            Debug.Assert(FractionNoised > 0 && FractionNoised < 1);
        }
    }

    internal class NoiseMutation : IMutation
    {
        private readonly NoiseMutationParameters Parameters;
        public NoiseMutation(NoiseMutationParameters parameters)
        {
            Parameters = parameters;
        }

        public void Mutate(List<Solution> solutions)
        {
            int maxNoises = Parameters.Problem.ClassesCount * Parameters.Problem.StudentsCount * Parameters.Problem.ClassesCount;
            Random random = new Random();
            int s;
            int c;
            foreach (Solution solution in solutions)
            {
                if (random.NextDouble() < Parameters.MutationPropability)
                {
                    for (int i = 0; i < maxNoises; i++)
                    {
                        s = random.Next(1, Parameters.Problem.StudentsCount - 1);
                        c = random.Next(1, Parameters.Problem.ClassesCount - 1);
                        solution.Assignment[s, c] += random.NextDouble() > .5 ? 1 : -1;
                    }
                    solution.CastSemiFeasible();
                }

            }
        }
    }
}
