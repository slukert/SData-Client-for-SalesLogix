using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Sage.SalesLogix.SData.Client.Test.Diagnostics
{
    public class CustomStopWatch : IDisposable
    {
        private Stopwatch _Stopwatch;
        private string _Name;

        public CustomStopWatch(string name)
        {
            this._Name = name;
            _Stopwatch = Stopwatch.StartNew();
        }

        #region IDisposable Members

        public void Dispose()
        {
            _Stopwatch.Stop();

            Console.WriteLine("'{0}' took {1} MS", _Name, _Stopwatch.ElapsedMilliseconds);
        }

        #endregion
    }
}
