using IEA.EA;
using IEA.EA.Abstraction;
using IEA.ORTools;
using IEA.ProblemInstance;
using System.Diagnostics;
using System.Runtime.ConstrainedExecution;
using System.Text.Json;

namespace IEA.Island

{ 
    internal class IslandParameters
    {
        public double FractionToPreserve { get; }

        public Problem Problem { get; }


        public static IslandParameters FromJSON(string json, Problem problem)
        {
            JsonElement parameters = JsonDocument
                            .Parse(json)
                            .RootElement
                            .GetProperty("IslandParameters");
            return new
                (
                parameters.GetProperty(nameof(FractionToPreserve)).GetDouble(),
                problem
                );
        }

        public IslandParameters(double fractionToPreserve, Problem problem)
        {
            FractionToPreserve = fractionToPreserve;
            Problem = problem;
            Check();
        }

        private void Check()
        {
            Debug.Assert(FractionToPreserve >= 0 && FractionToPreserve <= 1);   
        }
    }
    internal class Island
    {
        private readonly IslandParameters Parameters;
        private readonly EvolutionaryAlgorithm EA;
        private readonly ORToolsSolverWrapper Solver;

        public static Island FromJSON(string json)
        {
            Problem problem = Problem.FromJSON(json);
            NoiseMutationParameters mutationParameters = NoiseMutationParameters.FromJSON(json, problem);
            IMutation mutation = new NoiseMutation(mutationParameters);
            TournamentSelectionParameters tournamentSelectionParameters = TournamentSelectionParameters.FromJSON(json);
            ISelection selection = new TournamentSelection(tournamentSelectionParameters);
            CrossoverParameters crossoverParameters = CrossoverParameters.FromJSON(json, problem);
            ICrossover crossover = new Crossover(crossoverParameters);
            MigrationParameters migrationParameters = MigrationParameters.FromJSON(json);
            IMigration migration = new Migration(migrationParameters);
            GenotypeParameters genotypeParameters = GenotypeParameters.FromJSON(json, problem);
            IGenotype genotype = new Genotype(genotypeParameters);
            FitnessFunctionParameters fitnessFunctionParameters = FitnessFunctionParameters.FromJSON(json, problem);
            IFitnessFunction fitnessFunction = new FitnessFunction(fitnessFunctionParameters);
            IslandParameters parameters = IslandParameters.FromJSON(json, problem);
            EvolutionaryAlgorithmParameters evolutionaryAlgorithmParameters = EvolutionaryAlgorithmParameters.FromJSON(
                json, 
                mutation, 
                crossover,
                selection, 
                fitnessFunction,
                genotype, 
                migration);

            return new Island(parameters, evolutionaryAlgorithmParameters);
        }
        public Island(IslandParameters parameters, EvolutionaryAlgorithmParameters evolutionaryAlgorithmParameters)
        {
            this.Parameters = parameters;
            this.EA = new EvolutionaryAlgorithm(evolutionaryAlgorithmParameters);
            this.Solver = new ORToolsSolverWrapper(GenerateMask(), parameters.Problem);

        }

        private bool[,] GenerateMask()
        {
            Random random = new();
            bool[,] mask = new bool[Parameters.Problem.StudentsCount, Parameters.Problem.GroupCount];
            for (int s = 0; s < Parameters.Problem.StudentsCount; s++)
            {
                for (int g = 0; g < Parameters.Problem.GroupCount; g++)
                {
                    mask[s, g] = random.NextDouble() < Parameters.FractionToPreserve;
                }
            }
            return mask;
        }

        public void Setup()
        {
            this.EA.OnMigrantsAdded = (solution) => this.Solver.RequestSolveMinize(solution);
            this.Solver.OnSolutionReturned = (solution) => EA.AddSolution(solution);
        }

        public void Run()
        {
            Thread ea = new(() => EA.Run());
            Thread solver = new(() => Solver.Run());

            ea.Start();
            solver.Start();
        }
    }
}
