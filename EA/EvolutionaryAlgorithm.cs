using IEA.EA.Abstraction;
using IEA.ProblemInstance;
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace IEA.EA
{

internal class SolutionRepository
{
    private readonly IMongoCollection<BsonDocument> Solutions;
    private readonly string IslandId;
    private readonly string CoupledIslandId;

    public SolutionRepository(string connectionString, string databaseName, string collectionName)
    {
        var client = new MongoClient(connectionString);
        var database = client.GetDatabase(databaseName);

        Solutions = database.GetCollection<BsonDocument>(collectionName);
        IslandId = Environment.GetEnvironmentVariable("ISLAND_ID") ?? throw new ArgumentNullException("ISLAND_ID");
        CoupledIslandId = Environment.GetEnvironmentVariable("COUPLED_ISLAND_ID") ?? throw new ArgumentNullException("COUPLED_ISLAND_ID");
    }

    public void SaveSolution(Solution solution)
    {
        var document = solution.ToBsonDocument();
        document["islandId"] = IslandId;
        document["timestamp"] = DateTimeOffset.UtcNow.ToString("yyyy:MM:dd HH:mm:ss");
        document["alreadyMigrated"] = false;
        Solutions.InsertOne(document);
    }

    public Solution? ReadSolution(Problem problem)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("islandId", CoupledIslandId);
        var documents = Solutions.Find(filter).ToList();

        var sortedDocuments = documents.OrderByDescending(document => ParseDateFromDocument(document)).ToList();

        if (sortedDocuments.Count > 0)
        {
            var latestDocument = sortedDocuments[0];

            if (latestDocument.TryGetValue("alreadyMigrated", out var alreadyMigratedValue) && alreadyMigratedValue.AsBoolean)
            {
                return null;
            }

            var updatedDocument = new BsonDocument(latestDocument);
            updatedDocument["alreadyMigrated"] = true;
            var updateFilter = Builders<BsonDocument>.Filter.Eq("_id", latestDocument["_id"]);
            Solutions.ReplaceOne(updateFilter, updatedDocument);

            latestDocument.Remove("_id");
            int[,] assignment = ParseAssignmentFromDocument(latestDocument);

            return new Solution(problem, assignment);
        }

        return null;
    }


    private DateTime ParseDateFromDocument(BsonDocument document)
    {
        var timestamp = document["timestamp"].AsString;
        return DateTime.ParseExact(timestamp, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
    }

    private int[,] ParseAssignmentFromDocument(BsonDocument document)
    {
        var json = document.ToJson();
        var jsonDocument = JsonDocument.Parse(json);
        var root = jsonDocument.RootElement;
        var assignmentJsonArray = root.GetProperty("Assignment");
        int rows = assignmentJsonArray.GetArrayLength();
        int cols = assignmentJsonArray[0].GetArrayLength();

        int[,] assignment = new int[rows, cols];

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                assignment[i, j] = assignmentJsonArray[i][j].GetInt32();
            }
        }

        return assignment;
    }
}


    internal class EvolutionaryAlgorithmParameters : IEvolutionaryAlgorithmParameters
    {
        public int PopulationSize { get; }
        public int EliteCount { get; }
        public bool AllowElitism { get; }

        public Problem problem { get; }

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
            Problem problem, 
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
                problem,
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
            Problem problem,
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
            this.problem = problem;

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

        private SolutionRepository Repository;

        public Action<Solution>? OnMigrantsAdded { get; set; }
        public EvolutionaryAlgorithm(EvolutionaryAlgorithmParameters parameters)
        {
            Parameters = parameters;
            SolutionBuffer = new BlockingCollection<Solution>();
            MigrantsBuffer = new BlockingCollection<Solution>();
            Repository = new SolutionRepository(Environment.GetEnvironmentVariable("MONGODB_URI"), "prod", "solutions");
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
            Repository.SaveSolution(solution);
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
        private void AddMigrantsToPopulation(List<Solution> population)
        {
            var rng = new Random();
            
            var incomingMigrant = Repository.ReadSolution(this.Parameters.problem);
            if (incomingMigrant != null)
            {
                var randomIndex = rng.Next(population.Count);
                population[randomIndex] = incomingMigrant;
            }
        }

        public void Run()
        {
            List<Solution> population = Parameters.Genotype.GeneratePopulation(Parameters.PopulationSize);
            List<Solution> elite = new();
            int iteration = 0;
            while(!StopCriterion(iteration, CalculateFitnessSTD(population)))
            {
                AddMigrantsToPopulation(population);

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
