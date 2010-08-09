using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sage.SalesLogix.Client.Hibernate
{
    public class HibernateClientContextFactory : IClientContextFactory
    {
        #region IClientContextFactory Members

        public IClientContext CreateContext(IContextConfiguration configuration)
        {
            return new HibernateClientContext((HibernateContextConfiguration)configuration);
        }

        #endregion
    }
}
