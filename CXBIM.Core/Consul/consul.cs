﻿/// <summary>
/// Consul注册
/// </summary>
using Consul;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Hosting.Server.Features;

namespace CXBIM.Core.Consul;

public class ConsulServiceOptions
{
    // 服务注册地址（Consul的地址）
    public string ConsulAddress { get; set; }

    // 服务ID
    public string ServiceId { get; set; }

    // 服务名称
    public string ServiceName { get; set; }

    // 健康检查地址
    public string HealthCheck { get; set; }

    // 站点地址
    public string ServiceAddress { get; set; }
}

public static class ConsulRegistrationExtensions
{
    public static IApplicationBuilder UseConsul(this IApplicationBuilder app, IConfiguration configuration)
    {
        // 获取主机生命周期管理接口
        var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
        // 获取服务配置项
        var serviceOptions = app.ApplicationServices.GetRequiredService<IOptions<ConsulServiceOptions>>().Value;
        // 服务ID必须保证唯一
        serviceOptions.ServiceId = Guid.NewGuid().ToString("N");
        //serviceOptions.ServiceAddress =
        var consulClient = new ConsulClient(configuration =>
        {
            //服务注册的地址，集群中任意一个地址
            configuration.Address = new Uri(serviceOptions.ConsulAddress);
        });
        // 获取当前服务地址和端口，配置方式，也可以用自动获取
        //var features = app.Properties["server.Features"] as FeatureCollection;
        Uri uri = null;
        //var address = features.Get<IServerAddressesFeature>().Addresses?.FirstOrDefault();
        //if (address == null)
        //{
        //    Console.Write($"Urls:{configuration["urls"]}");
        //    // 方便使用命令启用多个服务
        //    uri = new Uri(configuration["urls"] ?? "https://localhost:44309");
        //}
        //else
        //{
        //    uri = new Uri(address);
        //}

        uri = new Uri(configuration["urls"] ?? "https://localhost:44309");
        //uri = new Uri("http://localhost:8433");
        Console.WriteLine("URI:"+uri);
        // 节点服务注册对象
        var registration = new AgentServiceRegistration()
        {
            ID = serviceOptions.ServiceId,
            Name = serviceOptions.ServiceName,// 服务名
            Address = uri.Host,
            Port = uri.Port, // 服务端口
            Check = new AgentServiceCheck
            {
                // 注册超时
                Timeout = TimeSpan.FromSeconds(5),
                // 服务停止多久后注销服务
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),
                // 健康检查地址
                HTTP = $"{uri.Scheme}://{uri.Host}:{uri.Port}{serviceOptions.HealthCheck}",
                // 健康检查时间间隔
                Interval = TimeSpan.FromSeconds(10),
                TLSSkipVerify = true,// 禁用https检查
            }
        };
        Console.WriteLine(registration.Check.HTTP);
        // 注册服务
        consulClient.Agent.ServiceRegister(registration).Wait();
        // 应用程序终止时，注销服务
        lifetime.ApplicationStopping.Register(() =>
        {
            consulClient.Agent.ServiceDeregister(serviceOptions.ServiceId).Wait();
        });
        return app;
    }
}

public static class ConsulChannelExtensions
{
    /// <summary>
    /// 获取Consul服务地址
    /// </summary>
    /// <param name="consulAddr"></param>
    /// <param name="serverName"></param>
    /// <returns></returns>
    public static string? GetConsuleAddress(string consulAddr, string serverName)
    {
        using var consulClient = new ConsulClient(a => a.Address = new Uri(consulAddr));
        var services = consulClient.Catalog.Service(serverName).Result.Response;
        if (services != null && services.Any())
        {
            var service = services[0];
            return $"http://{service.ServiceAddress}:{service.ServicePort}";
        }
        return null;
    }
}