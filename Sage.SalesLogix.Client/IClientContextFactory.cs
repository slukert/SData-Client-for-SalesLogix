using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sage.SalesLogix.Client
{
    public interface IClientContextFactory
    {
        IClientContext CreateContext(IContextConfiguration configuration);
    }
}
