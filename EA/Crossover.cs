using IEA.EA.Abstraction;
using IEA.ProblemInstance;
using System.Diagnostics;
using System.Text.Json;

namespace IEA.EA
{

    internal class CrossoverParameters : ICrossoverParameters
    {
        public double CrossoverPropability { get; }
        public Problem Problem { get; }
        public int CutCount { get; }

        public static CrossoverParameters FromJSON(string json, Problem problem)
        {
            JsonElement parameters = JsonDocument
                .Parse(json)
                .RootElement
                .GetProperty("CrossoverParameters");

            return new(
                parameters.GetProperty(nameof(CrossoverPropability)).GetDouble(),
                problem,
                parameters.GetProperty(nameof(CutCount)).GetInt32()
                );
        }

        public CrossoverParameters(double crossoverPropability, Problem problem, int cutCount)
        {
            CrossoverPropability = crossoverPropability;
            Problem = problem;
            CutCount = cutCount;
            Check();
        }

        private void Check()
        {
            Debug.Assert(0 < CrossoverPropability && CrossoverPropability < 1);
            Debug.Assert(CutCount > 0);
        }
    }

    internal class Crossover : ICrossover
    {
        private readonly CrossoverParameters Parameters;

        public Crossover(CrossoverParameters parameters)
        {
            Parameters = parameters;
        }
        public List<Solution> Cross(List<Solution> solutions)
        {
            Random random = new();
            List<Solution> parents = solutions.OrderBy(x => random.Next()).ToList();
            List<Solution> offspring = new();

            for (int i = 0; i < parents.Count; i += 2)
            {
                if (random.NextDouble() < Parameters.CrossoverPropability)
                {
                    int[,] son = new int[Parameters.Problem.StudentsCount, Parameters.Problem.ClassesCount];
                    int[,] daughter = new int[Parameters.Problem.StudentsCount, Parameters.Problem.ClassesCount];

                    List<int> cutIndices = new(Parameters.CutCount);
                    for (int j = 0; j < Parameters.CutCount; j++)
                    {
                        cutIndices.Add(random.Next(1, Parameters.Problem.StudentsCount - 1));
                    }

                    int swap = 0;
                    for (int s = 0; s < Parameters.Problem.StudentsCount; s++)
                    {
                        if (cutIndices.Contains(s)) swap = 1 - swap;

                        for (int c = 0; c < Parameters.Problem.ClassesCount; c++)
                        {
                            son[s, c] = parents[i + swap].Assignment[s, c];
                            daughter[s, c] = parents[i + 1 - swap].Assignment[s, c];
                        }
                    }
                    offspring.Add(new Solution(Parameters.Problem, son));
                    offspring.Add(new Solution(Parameters.Problem, daughter));

                }
            }
            return offspring;
        }
    }
}
