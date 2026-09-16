using Abp.AutoMapper;
using Abp.Modules;
using Abp.Reflection.Extensions;

namespace talepplanlama
{
    [DependsOn(typeof(talepplanlamaCoreModule), typeof(AbpAutoMapperModule))]
    public class talepplanlamaApplicationModule : AbpModule
    {
        public override void PreInitialize()
        {
            Configuration.Modules.AbpAutoMapper().Configurators.Add(cfg =>
            {
                cfg.AddMaps(typeof(talepplanlamaApplicationModule).GetAssembly());
            });
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(talepplanlamaApplicationModule).GetAssembly());
        }
    }
}
