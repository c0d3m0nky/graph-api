using System;
using System.Threading.Tasks;

namespace AADB2C.GraphApi.PutOnNuget.Extensions
{
    public static class Experimental
    {
        #region Task

        public static Func<Task> Then(this Func<Task> func, Func<Task> then) => async () => await func().Then(then);

        public static async Task<TResult> Then<T, TResult>(this Task<T> task, Func<T, Task<TResult>> then)
        {
            var r = await task;

            return await then(r);
        }

        public static async Task Then<T>(this Task<T> task, Func<T, Task> then)
        {
            var r = await task;

            await then(r);
        }

        public static async Task<TResult> Then<TResult>(this Task task, Func<Task<TResult>> then)
        {
            await task;

            return await (then?.Invoke() ?? Task.FromResult(default(TResult)));
        }

        public static async Task Then(this Task task, Func<Task> then)
        {
            await task;
            await (then?.Invoke() ?? Task.CompletedTask);
        }

        public static async Task<TReselt> CatchFinally<TReselt>(this Task task, Func<Exception, Task<TReselt>> body)
        {
            Exception ex = null;
            TReselt finallyResult;

            try
            {
                await task;
            }
            catch (Exception e)
            {
                ex = e;
            }
            finally
            {
                finallyResult = await body(ex);
            }

            return finallyResult;
        }

        public static async Task<TResult> CatchFinally<T, TResult>(this Task<T> task, Func<Exception, T, Task<TResult>> body)
        {
            Exception ex = null;
            T taskResult = default;
            TResult finallyResult;

            try
            {
                taskResult = await task;
            }
            catch (Exception e)
            {
                ex = e;
            }
            finally
            {
                finallyResult = await body(ex, taskResult);
            }

            return finallyResult;
        }

        #endregion
    }
}