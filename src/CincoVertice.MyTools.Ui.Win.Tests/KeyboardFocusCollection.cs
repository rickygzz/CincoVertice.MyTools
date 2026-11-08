using Xunit;

namespace CincoVertice.MyTools.Ui.Win.Tests;

/// <summary>
///     Tests that send real keys, which go to the window in front. They run when no other test does, since another
///     test showing a form would take the front, and the keys with it.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class KeyboardFocusCollection
{
    public const string Name = "Keyboard focus";
}
