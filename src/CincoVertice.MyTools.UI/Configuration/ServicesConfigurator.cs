using CincoVertice.MyTools.UI.CodeReview;
using CincoVertice.MyTools.UI.Main;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.UI.Configuration;

public static class ServicesConfigurator
{
    public static void Configure(IServiceCollection services)
    {
        services.AddTransient<MainForm>();
        services.AddTransient<CodeReviewForm>();
    }
}
