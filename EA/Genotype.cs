using IEA.EA.Abstraction;
using IEA.ProblemInstance;
using System.Text.Json;

namespace IEA.EA
{

    internal class GenotypeParameters : IGenotypeParameters
    {
        public bool GenerateFeasible { get; }

        public Problem Problem { get; }

        public static GenotypeParameters FromJSON(string json, Problem problem)
        {
            JsonElement parameters = JsonDocument
                                        .Parse(json)
                                        .RootElement
                                        .GetProperty("GenotypeParameters");
            return new(
                parameters.GetProperty(nameof(GenerateFeasible)).GetBoolean(),
                problem
                );
        }

        public GenotypeParameters(bool GenerateFeasible, Problem problem)
        {
            this.GenerateFeasible = GenerateFeasible;
            this.Problem = problem;
        }
    }

    internal class Genotype : IGenotype
    {

        private readonly GenotypeParameters Parameters;
        public Genotype(GenotypeParameters Parameters)
        {
            this.Parameters = Parameters;
        }
        public List<Solution> GeneratePopulation(int PopulationSize)
        {
            List<Solution> population = new();
            for (int i = 0; i < PopulationSize; i++)
            {
                population.Add(GenerateSolution());
            }
            return population;
        }
        private Solution GenerateSolution()
        {
            Random random = new();
            int[,] assignment = new int[Parameters.Problem.StudentsCount, Parameters.Problem.ClassesCount];
            for (int s = 0; s < Parameters.Problem.StudentsCount; s++)
            {
                for (int c = 0; c < Parameters.Problem.ClassesCount; c++)
                {
                    assignment[s, c] = Parameters.Problem.MustAttend[s, c] ? Parameters.Problem.ClassGroups[c][random.Next(Parameters.Problem.ClassGroups[c].Count)] : -1;
                }
            }
            Solution solution = new Solution(Parameters.Problem, assignment);
            if (Parameters.GenerateFeasible) solution.CastSemiFeasible();
            return solution;
        }
    }
}

