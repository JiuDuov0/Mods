using Autofac;
using EF;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Redis.Interface;
using Redis.Realization;
using System.Reflection;

namespace ModsAPI.tools
{
    /// <summary>
    /// Autofac 模块注册类，用于注册依赖注入的服务和组件。
    /// </summary>
    public class AutofacModuleRegister : Autofac.Module
    {
        /// <summary>
        /// 重写 Load 方法，在其中注册各种服务和组件到 Autofac 容器中。
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="InvalidOperationException"></exception>
        protected override void Load(ContainerBuilder builder)
        {
            var redisAssembly = Assembly.Load("Redis");
            var efAssembly = Assembly.Load("EF");
            var serviceAssembly = Assembly.Load("Service");

            builder.RegisterType<RedisManageService>().As<IRedisManageService>().As<IHostedService>().SingleInstance();

            builder.RegisterAssemblyTypes(redisAssembly).Where(type => type.Name.EndsWith("Service") && type != typeof(RedisManageService)).AsImplementedInterfaces().SingleInstance();

            // 注册 AllContext。
            // AllContext 的构造函数需要连接字符串，因此使用配置创建。
            builder.Register(context =>
            {
                var configuration = context.Resolve<IConfiguration>();

                var connectionString = configuration["WriteConnectionString"];

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException("WriteConnectionString 未配置。");
                }

                return new AllContext(connectionString);
            })
            .As<AllContext>().InstancePerLifetimeScope();

            builder.RegisterAssemblyTypes(efAssembly).Where(type => type.Name.EndsWith("Service")).AsImplementedInterfaces();

            builder.RegisterAssemblyTypes(serviceAssembly).Where(type => type.Name.EndsWith("Service")).AsImplementedInterfaces();

            builder.RegisterType<JwtHelper>().InstancePerLifetimeScope();
        }
    }
}