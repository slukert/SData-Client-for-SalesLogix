using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.SalesLogix.Client.Hibernate.Linq;
using Sage.Platform.NHibernateRepository;
using Sage.Entity.Interfaces;
using NHibernate;
using Sage.Platform.Orm;
using Sage.Platform;

namespace Sage.SalesLogix.Client.Hibernate
{
    public class HibernateClientContext : IClientContext
    {

        private HibernateContextConfiguration _configuration;

        internal HibernateClientContext(HibernateContextConfiguration configuration)
        {
            if (configuration == null)
                throw new ArgumentNullException("configuration");

            this._configuration = configuration;

            //Check, if applicationcontext is availible
            try
            {
                //using (ISession session = new SessionScopeWrapper(false))
                {
                    NHibernateRepository repository = new NHibernateRepository(typeof(IAccount));
                }
            }
            catch (NullReferenceException)
            {
                throw new InvalidOperationException("Hibernate components could not be accessed. This code can only run inside the SalesLogix web application, or after the Sage.SalesLogix.Client.HibernateContext has been loaded");
            }
        }

        #region IClientContext Members

        public IQueryable<T> CreateQuery<T>()
        {

            return new HibernateQuery<T>(this, new HibernateQueryProvider(this, typeof(T)));
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            //Nothing to do yet
        }


        public T GetById<T>(object id)
        {
            return EntityFactory.GetById<T>(id);
        }

        #endregion

        #region IDisposable Members

        void IDisposable.Dispose()
        {
            //Nothing to do yet
        }

        #endregion
    }
}
