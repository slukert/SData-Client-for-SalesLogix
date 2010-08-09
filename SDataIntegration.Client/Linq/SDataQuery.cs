using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;
using System.Reflection;
using Sage.SData.Client.Core;

namespace Sage.SalesLogix.Client.SData.Linq
{

    /// <summary>
    /// This class implements the IQuerable interface and allows Users to perform SData against the database
    /// </summary>
    internal class SDataQuery<EntityType> : IQueryable<EntityType>, IOrderedQueryable<EntityType>
    {

        private SDataClientContext _context;
        private Expression _expression;
        private SDataQueryProvider _provider;

        internal SDataQuery(SDataClientContext context, SDataQueryProvider provider)
        {
            _context = context;
            _expression = Expression.Constant(this);
            _provider = provider;
        }

        internal SDataQuery(SDataClientContext context, SDataQueryProvider provider, Expression expression)
        {
            _context = context;
            _expression = expression;
            _provider = provider;
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
                return (IQueryProvider)_context;
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

        Type IQueryable.ElementType
        {
            get { throw new InvalidOperationException(); }
        }

        Expression IQueryable.Expression
        {
            get { return _expression; }
        }

        IQueryProvider IQueryable.Provider
        {
            get
            {                
                return _provider;
            }
        }

        #endregion
    }

}
