using IEA.EA.Abstraction;
using IEA.ProblemInstance;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace IEA.EA
{
    internal class EvolutionaryAlgorithmParameters : IEvolutionaryAlgorithmParameters
    {
        public int PopulationSize { get; }
        public int EliteCount { get; }
        public bool AllowElitism { get; }

        public IMutation Mutation { get; }

        public ICrossover Crossover { get; }

        public ISelection Selection { get; }

        public IFitnessFunction FitnessFunction { get; }

        public IGenotype Genotype { get; }

        public IMigration Migration { get; }

        public bool AllowInterationCriterion { get; }
        public int MaxIterationCount { get; }

        public bool AllowFitnessSTDCriterion { get; }

        public double FitnessSTDThreshold { get; }






        public static EvolutionaryAlgorithmParameters FromJSON(
            string json, 
            IMutation mutation,
            ICrossover crossover,
            ISelection selection,
            IFitnessFunction fitnessFunction,
            IGenotype genotype,
            IMigration migration)
        {
            JsonElement parameters = JsonDocument
                .Parse(json)
                .RootElement
                .GetProperty("EvolutionaryAlgorithmParameters");

            return new(
                parameters.GetProperty(nameof(PopulationSize)).GetInt32(),
                parameters.GetProperty(nameof(EliteCount)).GetInt32(),
                parameters.GetProperty(nameof(AllowElitism)).GetBoolean(),
                mutation,
                crossover,
                selection,
                fitnessFunction,
                genotype,
                migration
                );
        }

        public EvolutionaryAlgorithmParameters(
            int populationSize,
            int eliteCount,
            bool allowElitism,
            IMutation mutation,
            ICrossover crossover,
            ISelection selection,
            IFitnessFunction fitnessFunction,
            IGenotype genotype,
            IMigration migration
            )
        {
            PopulationSize = populationSize;
            EliteCount = eliteCount;
            AllowElitism = allowElitism;
            Mutation = mutation;
            Crossover = crossover;
            Selection = selection;
            Genotype = genotype;
            FitnessFunction = fitnessFunction;
            Migration = migration;

            Check();
        }

        private void Check()
        {
            Debug.Assert(PopulationSize > 0);
            Debug.Assert(EliteCount >= 0);
        }


    }
    internal class EvolutionaryAlgorithm : IEvolutionaryAlgorithm
    {

        private readonly EvolutionaryAlgorithmParameters Parameters;

        private readonly BlockingCollection<Solution> SolutionBuffer;

        private readonly BlockingCollection<Solution> MigrantsBuffer;

        private Solution BestSolution;

        public Action<Solution>? OnMigrantsAdded { get; set; }
        public EvolutionaryAlgorithm(EvolutionaryAlgorithmParameters parameters)
        {
            Parameters = parameters;
            SolutionBuffer = new BlockingCollection<Solution>();
            MigrantsBuffer = new BlockingCollection<Solution>();
        }

        public void AddSolution(Solution solution) => SolutionBuffer.Add(solution);
        public void AddSolutions(List<Solution> solutions)
        {
            foreach (Solution solution in solutions)
            {
                AddSolution(solution);
            }
        }

        public List<Solution> TakeMigrants()
        {
            return MigrantsBuffer.GetConsumingEnumerable().ToList();
        }

        private void SaveToDatabase(Solution solution)
        {
            // TODO: Implement
        }



        private void ManageBestSolution(List<Solution> fullPopulation)
        {
            double bestFitnessPopulation = fullPopulation.Min(e => e.Fitness);
            if (bestFitnessPopulation < (BestSolution?.Fitness ?? int.MaxValue))
            {
                BestSolution = fullPopulation.Find(e => e.Fitness == bestFitnessPopulation);
                SaveToDatabase(BestSolution);
            }
        }

        private void UpdatePopulation(List<Solution> population)
        {
            population.AddRange(SolutionBuffer.GetConsumingEnumerable().ToList());
        }

        private bool StopCriterion(int iteration, double fitnessSTD)
        {
            if (Parameters.AllowInterationCriterion && iteration >= Parameters.MaxIterationCount) return true;
            if (Parameters.AllowFitnessSTDCriterion && fitnessSTD < Parameters.FitnessSTDThreshold) return true;
            return false;
        }   

        private double CalculateFitnessSTD(List<Solution> population)
        {
            double mean = population.Average(e => e.Fitness);
            double sum = population.Sum(e => Math.Pow(e.Fitness - mean, 2));
            return Math.Sqrt(sum / population.Count);
        }

        private void ManageMigrants(List<Solution> migrants)
        {
            foreach (Solution migrant in migrants)
            {
                MigrantsBuffer.Add(migrant);
            }
            
            if (migrants.Any()) OnMigrantsAdded?.Invoke(migrants[0]);
        }
        public void Run()
        {
            List<Solution> population = Parameters.Genotype.GeneratePopulation(Parameters.PopulationSize);
            List<Solution> elite = new();
            int iteration = 0;
            while(!StopCriterion(iteration, CalculateFitnessSTD(population)))
            {
                UpdatePopulation(population);

                Parameters.Mutation.Mutate(population);
                List<Solution> offspring = Parameters.Crossover.Cross(population);
                population.AddRange(offspring);
                Parameters.FitnessFunction.Evaluate(population);
                population.AddRange(elite);

                ManageBestSolution(population);

                elite = Parameters.Selection.SelectElite(population, Parameters.EliteCount);
                population = Parameters.Selection.SelectPopulation(population, Parameters.PopulationSize);

                List<Solution> migrants = Parameters.Migration.Migrate(population);
                ManageMigrants(migrants);

                iteration++;
            }
        }
    }
}
