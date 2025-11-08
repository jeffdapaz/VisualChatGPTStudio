using System;
using System.Threading.Tasks;

namespace JeffPires.VisualChatGPTStudio.Utils;

public static class AsyncEventHandler
{
    public static async void SafeFireAndForget(
        Func<Task> asyncAction,
        Action<Exception> onError = null)
    {
        try
        {
            await asyncAction();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}
