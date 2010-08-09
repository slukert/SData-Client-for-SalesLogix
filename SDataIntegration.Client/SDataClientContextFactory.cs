using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sage.SalesLogix.Client.SData
{
    public class SDataClientContextFactory : IClientContextFactory
    {
        #region IClientContextFactory Members

        public IClientContext CreateContext(IContextConfiguration configuration)
        {
            return new SDataClientContext((SDataContextConfiguration)configuration);
        }

        #endregion
    }
}
