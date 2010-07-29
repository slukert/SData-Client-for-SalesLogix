using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;
using System.Reflection;
using Sage.SData.Client.Core;

namespace Sage.SalesLogix.SData.Client.Linq
{

    /// <summary>
    /// This class implements the IQuerable interface and allows Users to perform SData against the database
    /// </summary>
    internal class SDataQuery<EntityType> : IQueryable<EntityType>, IOrderedQueryable<EntityType>
    {

        private ClientContext _context;
        private Expression _expression;
        private SDataQueryProvider _provider;        

        internal SDataQuery(ClientContext context)
        {
            _context = context;
            _expression = Expression.Constant(this);
        }

        internal SDataQuery(ClientContext context, Expression expression)
        {
            _context = context;
            _expression = expression;
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
            SDataTranslator translator = new SDataTranslator();

            IEnumerable<SDataRequest> requests = translator.TranslateExpression(_expression, _context.Service);

            List<EntityType> result = new List<EntityType>();

            foreach (SDataRequest request in requests)
            {
                //If a projection is used, the Load has to be called via reflection, because the generic type might not match 'EntityType'
                if (request.ProjectionExpressions.Count > 0)
                {
                    ICollection collection = (ICollection)(typeof(ClientContext).GetMethod("Load").MakeGenericMethod(request.RequestBaseType).Invoke(_context, new object[] { request.Request }));

                    foreach (object item in collection)
                        result.Add(request.PerformSelect<EntityType>(item));
                }
                else
                    result.AddRange(_context.Load<EntityType>(request.Request));
            }

            return result.GetEnumerator();
            
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
                if (_provider == null)
                    _provider = new SDataQueryProvider(_context);

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
                if (_provider == null)
                    _provider = new SDataQueryProvider(_context);

                return _provider;
            }
        }

        #endregion
    }

    /// <summary>
    /// Extention methods to IQuery for use with SData
    /// </summary>
    public static class SDataQueryExtensions
    {
        public static IQueryable<TSource> Include<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, object>> predicate)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            if (predicate == null)
                throw new ArgumentNullException("predicate");

            return source.Provider.CreateQuery<TSource>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(TSource) }), new Expression[] { source.Expression, Expression.Quote(predicate) }));

        }

        public static IQueryable<TSource> SetMaxRows<TSource>(this IQueryable<TSource> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            return source.Provider.CreateQuery<TSource>(Expression.Call(null, ((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(new Type[] { typeof(TSource) }), new Expression[] { source.Expression, Expression.Constant(count) }));
        }

        /// <summary>
        /// This method can by used in SData statements to search for certain values
        /// </summary>
        /// <param name="source"></param>
        /// <param name="field"></param>
        /// <returns></returns>
        public static bool In(this IEnumerable source, object field)
        {
            throw new NotImplementedException();
        }

    }
}
