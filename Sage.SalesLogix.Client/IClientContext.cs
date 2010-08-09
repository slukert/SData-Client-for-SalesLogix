using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sage.SalesLogix.Client
{
    public interface IClientContext : IDisposable
    {
        IQueryable<T> CreateQuery<T>();

        T GetById<T>(object id);
    }
}
