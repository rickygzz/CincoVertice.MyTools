using CincoVertice.MyTools.Ui.CodeReview.Presenters;
using CincoVertice.MyTools.Ui.CodeReview.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CincoVertice.MyTools.Ui.Win.CodeReview.Services;

/// <summary>
///     Opens Code Review windows. Each window gets its own DI scope, disposed when the window closes.
/// </summary>
public sealed class CodeReviewWindowService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public CodeReviewWindowService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
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

        // Build presenter using the same form instance as ICodeReviewView
        var presenter = ActivatorUtilities.CreateInstance<CodeReviewPresenter>(sp, (ICodeReviewView)form);

        form.FormClosed += (_, _) => scope.Dispose();
        form.Show();

        return presenter;
    }
}
