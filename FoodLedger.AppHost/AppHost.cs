var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.FoodLedger>("foodledger");

builder.Build().Run();
