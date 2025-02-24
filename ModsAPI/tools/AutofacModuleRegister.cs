using Autofac;
using Autofac.Core;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection;

namespace ModsAPI.tools
{
    public class AutofacModuleRegister : Autofac.Module
    {
        /// <inheritdoc/>
        /// 重写Autofac管道Load方法，在这里注册注入
        protected override void Load(ContainerBuilder builder)
        {
            //程序集注入业务服务
            var Redis = Assembly.Load("Redis");
            var EF = Assembly.Load("EF");
            var Service = Assembly.Load("Service");
            //根据名称约定（服务层的接口和实现均以Service结尾），实现服务接口和服务实现的依赖
            builder.RegisterAssemblyTypes(Redis, Redis).Where(t => t.Name.EndsWith("Service")).AsImplementedInterfaces();
            builder.RegisterAssemblyTypes(EF, EF).Where(t => t.Name.EndsWith("Service")).AsImplementedInterfaces();
            builder.RegisterAssemblyTypes(Service, Service).Where(t => t.Name.EndsWith("Service")).AsImplementedInterfaces();
            builder.RegisterType<JwtHelper>().InstancePerLifetimeScope();
        }
    }
}
