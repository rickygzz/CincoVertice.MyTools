using System.Runtime.ExceptionServices;

namespace CincoVertice.MyTools.Ui.Win.Tests.JsonEditor;

/// <summary>
///     Runs a test on a thread of the STA kind, as the app's UI thread is: the clipboard needs one, and so does a
///     shown window that takes dropped files, whose Show waits forever on any other thread.
/// </summary>
internal static class StaThread
{
    public static void Run(Action test)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                test();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (failure is not null)
        {
            ExceptionDispatchInfo.Throw(failure);
        }
    }
}
