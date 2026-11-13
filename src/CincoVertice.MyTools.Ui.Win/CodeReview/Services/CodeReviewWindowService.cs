using CincoVertice.MyTools.Ui.CodeReview.Presenters;
using CincoVertice.MyTools.Ui.CodeReview.Views;
using CincoVertice.MyTools.Ui.Help.Services;
using CincoVertice.MyTools.Ui.Win.Help.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.Ui.Win.CodeReview.Services;

/// <summary>
///     Opens Code Review windows. Each window gets its own DI scope, disposed when the window closes.
/// </summary>
public sealed class CodeReviewWindowService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly HelpWindowService _help;

    public CodeReviewWindowService(IServiceScopeFactory scopeFactory, HelpWindowService help)
    {
        _scopeFactory = scopeFactory;
        _help = help;
    }

    /// <summary>
    ///     Opens a new Code Review window.
    /// </summary>
    /// <returns>The window's presenter, to start actions on it, e.g. a folder check.</returns>
    public CodeReviewPresenter Open()
    {
        var scope = _scopeFactory.CreateScope();
        var sp = scope.ServiceProvider;

        var form = sp.GetRequiredService<CodeReviewForm>();
        _help.OpenOnF1(form, () => HelpTopics.CodeReview);

        // Build presenter using the same form instance as ICodeReviewView
        var presenter = ActivatorUtilities.CreateInstance<CodeReviewPresenter>(sp, (ICodeReviewView)form);

        form.FormClosed += (_, _) => scope.Dispose();
        form.Show();

        return presenter;
    }
}
