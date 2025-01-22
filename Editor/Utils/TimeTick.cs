using System;
using System.Diagnostics;

using UDebug = UnityEngine.Debug;

namespace AssetBundleBrowser
{
    public class TimeTick : IDisposable
    {
        #region [Fields]
        private string _Tips;
        private Stopwatch _WatchDog;
        private Action<string> _Logger;
        #endregion

        #region [Construct]
        public TimeTick(string varTip, Action<string> varLogger)
        {
            _Tips = varTip;
            _Logger = varLogger;
            _WatchDog = new Stopwatch();
            _WatchDog.Start();
        }
        #endregion

        #region [IDisposable]
        public void Dispose()
        {
            _WatchDog.Stop();
            _Logger?.Invoke(string.Format("[{0}] - {1}ms", _Tips, _WatchDog.ElapsedMilliseconds));
        }
        #endregion
    }
    public sealed class UDebugTimeTick : TimeTick
    {
        #region [Construct]
        public UDebugTimeTick(string varTips) : base(varTips, UDebug.Log) { }
        #endregion
    }
}