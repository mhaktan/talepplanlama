using Abp.AspNetCore;
using Abp.AspNetCore.Configuration;
using Abp.Modules;
using Abp.Reflection.Extensions;
using talepplanlama.EntityFrameworkCore;

namespace talepplanlama.Web.Host
{
    [DependsOn(typeof(talepplanlamaApplicationModule), typeof(talepplanlamaEntityFrameworkCoreModule), typeof(AbpAspNetCoreModule))]
    public class talepplanlamaWebHostModule : AbpModule
    {
        public override void PreInitialize()
        {
            // Expose all AppServices as dynamic API controllers
            Configuration.Modules.AbpAspNetCore()
                .CreateControllersForAppServices(
                    typeof(talepplanlamaApplicationModule).GetAssembly(),
                    moduleName: "app",
                    useConventionalHttpVerbs: true
                );
        }

        public override void Initialize()
        {
            IocManager.RegisterAssemblyByConvention(typeof(talepplanlamaWebHostModule).GetAssembly());
        }
    }
}
