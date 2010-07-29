using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sage.SalesLogix.SData.Client.Linq
{
    internal class SDataQueryProvider : IQueryProvider
    {

        private ClientContext _context;

        internal SDataQueryProvider(ClientContext context)
        {
            this._context = context;
        }

        #region IQueryProvider Members

        public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        {
            return new SDataQuery<TElement>(_context, expression);
        }

        public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        {
            throw new NotImplementedException();
        }

        public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        {
            throw new NotImplementedException();
        }

        public object Execute(System.Linq.Expressions.Expression expression)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
