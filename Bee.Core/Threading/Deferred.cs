using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading;
using Bee.Logging;
using System.Runtime.Serialization;

namespace Bee.Threading
{
    public enum State
    {
        PENDING,
        REJECTED,
        RESOLVED
    }

    public interface IPromise<D, F, P>
    {
        State State
        {
            get;
        }

        bool IsPending
        {
            get;
        }

        bool IsResolved
        {
            get;
        }

        bool IsRejected
        {
            get;
        }

        IPromise<D, F, P> Then(Action<D> doneCallback);

        IPromise<D, F, P> Then(Action<D> doneCallback, Action<F> failCallback);

        IPromise<D, F, P> Then(Action<D> doneCallback, Action<F> failCallback, Action<P> progressCallback);

        IPromise<D, F, P> Done(Action<D> doneCallback);

        IPromise<D, F, P> Fail(Action<F> failCallback);

        IPromise<D, F, P> Always(Action<State, D, F> callback);

        IPromise<D, F, P> Progress(Action<P> callback);
    }

    public interface IDeferred<D, F, P> : IPromise<D, F, P>
    {
        IDeferred<D, F, P> Resolve(D resolve);

        IDeferred<D, F, P> Reject(F reject);

        IDeferred<D, F, P> Notify(P progress);

        IPromise<D, F, P> Promise();
    }

    public class DeferredPromise<D, F, P> : IPromise<D, F, P>
    {
        private IPromise<D, F, P> promise;
        private IDeferred<D, F, P> deferred;

        public DeferredPromise(IDeferred<D, F, P> deferred)
        {
            this.deferred = deferred;
            this.promise = deferred.Promise();
        }

        public State State
        {
            get { return promise.State; }
        }

        public bool IsPending
        {
            get { return promise.IsPending; }
        }

        public bool IsResolved
        {
            get { return promise.IsResolved; }
        }

        public bool IsRejected
        {
            get { return promise.IsRejected; }
        }

        public IPromise<D, F, P> Then(Action<D> doneCallback)
        {
            return promise.Then(doneCallback);
        }

        public IPromise<D, F, P> Then(Action<D> doneCallback, Action<F> failCallback)
        {
            return promise.Then(doneCallback, failCallback);
        }

        public IPromise<D, F, P> Then(Action<D> doneCallback, Action<F> failCallback, Action<P> progressCallback)
        {
            return promise.Then(doneCallback, failCallback, progressCallback);
        }

        public IPromise<D, F, P> Done(Action<D> doneCallback)
        {
            return promise.Done(doneCallback);
        }

        public IPromise<D, F, P> Fail(Action<F> failCallback)
        {
            return promise.Fail(failCallback);
        }

        public IPromise<D, F, P> Always(Action<State, D, F> callback)
        {
            return promise.Always(callback);
        }

        public IPromise<D, F, P> Progress(Action<P> callback)
        {
            return promise.Progress(callback);
        }
    }

    public abstract class AbstractPromise<D, F, P> : IPromise<D, F, P>
    {
        protected State state = State.PENDING;

        protected List<Action<D>> doneCallbackList = new List<Action<D>>();
        protected List<Action<F>> failCallbackList = new List<Action<F>>();
        protected List<Action<P>> progressCallbackList = new List<Action<P>>();
        protected List<Action<State, D, F>> alwaysCallbackList = new List<Action<State, D, F>>();

        protected D resolvedResult;
        protected F rejectedResult;


        #region IPromise<D,F,P> 成员

        public State State
        {
            get { return this.state; }
        }

        public bool IsPending
        {
            get { return state == State.PENDING; }
        }

        public bool IsResolved
        {
            get { return state == State.RESOLVED; }
        }

        public bool IsRejected
        {
            get { return state == State.REJECTED; }
        }

        public IPromise<D, F, P> Then(Action<D> doneCallback)
        {
            return Then(doneCallback, null, null);
        }

        public IPromise<D, F, P> Then(Action<D> doneCallback, Action<F> failCallback)
        {
            return Then(doneCallback, failCallback, null);
        }

        public IPromise<D, F, P> Then(Action<D> doneCallback, Action<F> failCallback, Action<P> progressCallback)
        {
            if (doneCallback != null)
            {
                Done(doneCallback);
            }
            if (failCallback != null)
            {
                Fail(failCallback);
            }
            if (progressCallback != null)
            {
                Progress(progressCallback);
            }

            return this;
        }

        public IPromise<D, F, P> Done(Action<D> doneCallback)
        {
            lock (this)
            {
                if (doneCallback != null)
                {
                    doneCallbackList.Add(doneCallback);
                    if (IsResolved)
                    {
                        TriggerDone(doneCallback, resolvedResult);
                    }
                }
            }

            return this;
        }

        public IPromise<D, F, P> Fail(Action<F> failCallback)
        {
            lock (this)
            {
                if (failCallback != null)
                {
                    failCallbackList.Add(failCallback);
                    if (IsRejected)
                    {
                        TriggerFail(failCallback, rejectedResult);
                    }
                }
            }

            return this;
        }

        public IPromise<D, F, P> Always(Action<State, D, F> callback)
        {
            lock (this)
            {
                if (callback != null)
                {
                    alwaysCallbackList.Add(callback);
                    if (!IsPending)
                    {
                        TriggerAlways(callback, state, resolvedResult, rejectedResult);
                    }
                }
            }

            return this;
        }

        public IPromise<D, F, P> Progress(Action<P> callback)
        {
            if (callback != null)
            {
                progressCallbackList.Add(callback);
            }

            return this;
        }

        #endregion

        protected void TriggerDone(Action<D> callback, D resolved)
        {
            callback(resolved);
        }

        protected void TriggerFail(Action<F> callback, F rejected)
        {
            callback(rejected);
        }

        protected void TriggerProgress(Action<P> callback, P progress)
        {
            callback(progress);
        }

        protected void TriggerAlways(Action<State, D, F> callback, State state, D resolved, F rejected)
        {
            callback(state, resolved, rejected);
        }

        protected void TriggerDone(D resolved)
        {
            foreach (Action<D> callback in doneCallbackList)
            {
                try
                {
                    TriggerDone(callback, resolved);
                }
                catch (Exception e)
                {
                    Logger.Error("an uncaught exception occured in a DoneCallback", e);
                }
            }
        }

        protected void TriggerFail(F rejected)
        {
            foreach (Action<F> callback in failCallbackList)
            {
                try
                {
                    TriggerFail(callback, rejected);
                }
                catch (Exception e)
                {
                    Logger.Error("an uncaught exception occured in a FailCallback", e);
                }
            }
        }

        protected void TriggerProgress(P progress)
        {
            foreach (Action<P> callback in progressCallbackList)
            {
                try
                {
                    TriggerProgress(callback, progress);
                }
                catch (Exception e)
                {
                    Logger.Error("an uncaught exception occured in a ProgressCallback", e);
                }
            }
        }

        protected void TriggerAlways(State state, D resolved, F rejected)
        {
            foreach (Action<State, D, F> callback in alwaysCallbackList)
            {
                try
                {
                    TriggerAlways(callback, state, resolved, rejected);
                }
                catch (Exception e)
                {
                    Logger.Error("an uncaught exception occured in a AlwaysCallback", e);
                }
            }

            //lock (this)
            //{
            //    //this.NotifyAll();
            //}
        }
    }

    public class DeferredObject<D, F, P> : AbstractPromise<D, F, P>, IDeferred<D, F, P>
    {

        #region IDeferred<D,F,P> 成员

        public IDeferred<D, F, P> Resolve(D resolve)
        {
            lock (this)
            {
                if (!IsPending)
                {
                    throw new ArgumentException("Deferred object already finished, cannot resolve again");
                }

                this.state = State.RESOLVED;
                this.resolvedResult = resolve;

                try
                {
                    TriggerDone(resolve);
                }
                finally
                {
                    TriggerAlways(state, resolve, default(F));
                }

            }

            return this;
        }

        public IDeferred<D, F, P> Reject(F reject)
        {
            lock (this)
            {
                if (!IsPending)
                {
                    throw new ArgumentException("Deferred object already finished, cannot resolve again");
                }

                this.state = State.REJECTED;
                this.rejectedResult = reject;

                try
                {
                    TriggerFail(rejectedResult);
                }
                finally
                {
                    TriggerAlways(state, default(D), rejectedResult);
                }
            }

            return this;
        }

        public IDeferred<D, F, P> Notify(P progress)
        {
            lock (this)
            {
                if (!IsPending)
                {
                    throw new ArgumentException("Deferred object already finished, cannot resolve again");
                }

                TriggerProgress(progress);
            }

            return this;
        }

        public IPromise<D, F, P> Promise()
        {
            return this;
        }

        #endregion
    }

    public class DeferredException : Exception
    {
        public DeferredException()
        {
        }

        public DeferredException(string message)
            : base(message)
        {
        }

        public DeferredException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected DeferredException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    public class DeferredProgress
    {
        public int Done { get; set; }
        public int Total { get; set; }
        public int Fail { get; set; }
    }

    internal sealed class DeferredInvoker<T> : DeferredObject<T, DeferredException, DeferredProgress>
    {
        private Func<T> callback;

        private readonly ManualResetEvent asyncWaitHandle;

        public DeferredInvoker(Func<T> callback)
        {
            this.callback = callback;
            asyncWaitHandle = new ManualResetEvent(false);

            Invoke();
        }

        public void Invoke()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                T result;
                try
                {
                    result = callback();
                    base.Resolve(result);
                }
                catch (Exception ex)
                {
                    base.Reject(new DeferredException(ex.Message, ex));
                }
                
            });

            //new Thread(new ThreadStart(()=>{
            //    T result;
            //    try
            //    {
            //        result = callback();
            //        base.Resolve(result);
            //    }
            //    catch (Exception ex)
            //    {
            //        base.Reject(new DeferredException(ex.Message, ex));
            //    }
            //})).Start();
        }
    }

    public static class DeferredHelper
    {
        public static IPromise<D, DeferredException, DeferredProgress> When<D>(Func<D> func)
        {
            return new DeferredInvoker<D>(func).Promise();
        }
    }
}
