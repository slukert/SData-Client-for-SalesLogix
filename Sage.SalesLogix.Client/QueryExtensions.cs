using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

namespace Sage.SalesLogix.Client
{
    /// <summary>
    /// Extension methods to IQuery for use with SLX
    /// </summary>
    public static class QueryExtensions
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
