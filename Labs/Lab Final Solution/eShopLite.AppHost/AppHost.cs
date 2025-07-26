var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var productsdb = builder.AddPostgres("pg")
                        .WithDataVolume("productsdb-volume") // 같은 이름의 볼륨을 사용하여 데이터 지속
                        .WithLifetime(ContainerLifetime.Persistent) // 앱을 내려도 컨테이너가 유지됨
                        .WithPgAdmin()
                        .AddDatabase("productsdb");

var products = builder.AddProject<Projects.Products>("products")
                      .WithReference(productsdb)
                      .WaitFor(productsdb);

builder.AddProject<Projects.Store>("store")
       .WithExternalHttpEndpoints()
       .WithReference(products)
       .WithReference(redis)
       .WaitFor(products)
       .WaitFor(redis);
    
builder.Build().Run();
