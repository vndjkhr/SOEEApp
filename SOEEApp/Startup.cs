using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(SOEEApp.Startup))]
namespace SOEEApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
