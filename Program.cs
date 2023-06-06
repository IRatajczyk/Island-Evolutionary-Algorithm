using IEA.Island;

var island = Island.FromJSON(File.ReadAllText("<PathToIslandParamsFile>"));
island.Run();