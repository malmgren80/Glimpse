using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Glimpse.EF6.Sample.Startup))]
namespace Glimpse.EF6.Sample
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
