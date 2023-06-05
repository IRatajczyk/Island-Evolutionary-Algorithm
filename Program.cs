// See https://aka.ms/new-console-template for more information
using IEA.EA.Abstraction;
using IEA.ProblemInstance;
using IEA.EA;
using IEA.ORTools;
using IEA.Island;

var island = Island.FromJSON(File.ReadAllText("./Data/IslandParams.json"));
island.Run();