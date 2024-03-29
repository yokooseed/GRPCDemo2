﻿using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using CXBIM.Core.Consul;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


//Consul
builder.Services.AddHealthChecks();
builder.Configuration.AddJsonFile("consulsetting.json", optional: false, reloadOnChange: true);
builder.Services.Configure<ConsulServiceOptions>(new
    ConfigurationBuilder()
    .AddJsonFile("consulsetting.json").Build()
    );

//Ocelot
builder.Configuration.AddJsonFile("ocelotsetting.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration).AddConsul().AddConfigStoredInConsul(); //注入中间件



var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    //app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}


//Ocelot

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


//Consul
app.UseHealthChecks("/Health");
app.UseConsul(builder.Configuration);

//Ocelot
app.UseOcelot().Wait();

app.Run();


