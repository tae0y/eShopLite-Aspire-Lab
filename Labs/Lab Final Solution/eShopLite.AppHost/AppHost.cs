using eShopLite.AppHost.OpenTelemetryCollector;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var productsdb = builder.AddPostgres("pg")
                        .WithPgAdmin()
                        .AddDatabase("productsdb");

var products = builder.AddProject<Projects.Products>("products")
                      .WithReference(productsdb)
                      .WaitFor(productsdb);

var prometheus = builder.AddContainer("prometheus", "prom/prometheus", "v3.2.1")
       .WithBindMount("../prometheus", "/etc/prometheus", isReadOnly: true)
       .WithArgs("--web.enable-otlp-receiver", "--config.file=/etc/prometheus/prometheus.yml")
       .WithHttpEndpoint(targetPort: 9090, name: "http");

var grafana = builder.AddContainer("grafana", "grafana/grafana")
                     .WithBindMount("../grafana/config", "/etc/grafana", isReadOnly: true)
                     .WithBindMount("../grafana/dashboards", "/var/lib/grafana/dashboards", isReadOnly: true)
                     .WithEnvironment("PROMETHEUS_ENDPOINT", prometheus.GetEndpoint("http"))
                     .WithHttpEndpoint(targetPort: 3000, name: "http");

builder.AddOpenTelemetryCollector("otelcollector", "../otelcollector/config.yaml")
       .WithEnvironment("PROMETHEUS_ENDPOINT", $"{prometheus.GetEndpoint("http")}/api/v1/otlp");

builder.AddProject<Projects.Store>("store")
       .WithExternalHttpEndpoints()
       .WithEnvironment("GRAFANA_URL", grafana.GetEndpoint("http"))
       .WithReference(products)
       .WithReference(redis)
       .WaitFor(products)
       .WaitFor(redis);
    
builder.Build().Run();
