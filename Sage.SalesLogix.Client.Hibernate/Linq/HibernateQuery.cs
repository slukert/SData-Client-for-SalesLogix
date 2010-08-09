using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.Platform;
using Sage.Entity.Interfaces;
using System.Linq.Expressions;
using System.Collections;
using Sage.Platform.NHibernateRepository;
using System.Diagnostics;
using Sage.Platform.Repository;

namespace Sage.SalesLogix.Client.Hibernate.Linq
{
    internal class HibernateQuery<EntityType> : IQueryable<EntityType>, IOrderedQueryable<EntityType>
    {

        private HibernateClientContext _context;
        private HibernateQueryProvider _provider;
        private Expression _expression;


        public HibernateQuery(HibernateClientContext context, HibernateQueryProvider provider)
        {
            this._context = context;
            _provider = provider;
            _expression = Expression.Constant(this);
        }

        public HibernateQuery(HibernateClientContext context, Expression expression, HibernateQueryProvider provider)
        {
            this._context = context;
            this._expression = expression;
            this._provider = provider;
        }

        #region IEnumerable<EntityType> Members

        public IEnumerator<EntityType> GetEnumerator()
        {
            return (IEnumerator<EntityType>)(((IEnumerable)this).GetEnumerator());
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _provider.ExecuteInternal<EntityType>(_expression).GetEnumerator();      
        }

        #endregion

        #region IQueryable Members

        public Type ElementType
        {
            get { throw new NotImplementedException(); }
        }

        public System.Linq.Expressions.Expression Expression
        {
            get { return _expression; }
        }

        public IQueryProvider Provider
        {
            get
            {              
                return _provider;
            }
        }

        #endregion

        #region IEnumerable<EntityType> Members

        IEnumerator<EntityType> IEnumerable<EntityType>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        #region IQueryable Members

        Type System.Linq.IQueryable.ElementType
        {
            get { throw new NotImplementedException(); }
        }

        System.Linq.Expressions.Expression System.Linq.IQueryable.Expression
        {
            get { return _expression; }
        }

        IQueryProvider System.Linq.IQueryable.Provider
        {
            get { return Provider; }
        }

        #endregion
    }
}
