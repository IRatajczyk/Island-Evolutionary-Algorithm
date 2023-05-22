// See https://aka.ms/new-console-template for more information
using IEA.EA.Abstraction;
using IEA.ProblemInstance;
using IEA.EA;
using IEA.ORTools;

Console.WriteLine("Hello, World!");
string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "data.json");
Problem problem = Problem.FromJSON(File.ReadAllText("C:\\Users\\igor\\Desktop\\Studia\\AiR\\Semestr8\\SR\\Projekt\\IEA\\Data\\data.json"));





