using Microsoft.Extensions.DependencyInjection;

using StackExchange.Redis;

using System.Net.Sockets;


var builder = DistributedApplication.CreateBuilder(args);

#region Cash

var cache = builder.AddRedis("cache");

#endregion

#region Postgres

var dbPassword = builder.AddParameter("dbPassword", "password", true);

var postgres = builder.AddPostgres("postgres", password: dbPassword, port: 5432)
    //.WithEndpoint("myendpoint", e =>
    //{
    //    e.Port = 5432;
    //    e.TargetPort = 5432;
    //    e.Protocol = ProtocolType.Tcp;
    //    e.UriScheme = "tcp";
    //})
    .WithImage("ankane/pgvector")
    .WithImageTag("latest")
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

var postgresdb = postgres.AddDatabase("packagedb");

builder.AddProject<Projects.PackageUniverse_MigrationService>("migrations")
    .WithReference(postgres)
    .WaitFor(postgres);

#endregion

var apiService = builder.AddProject<Projects.PackageUniverse_ApiService>("apiservice")
 .WithReference(postgresdb).WaitFor(postgresdb);

builder.AddProject<Projects.PackageUniverse_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(cache)
    .WaitFor(cache)
    .WithReference(apiService)
    .WaitFor(apiService);

builder.Build().Run();
