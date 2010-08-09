using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.Collections;

namespace Sage.SalesLogix.Client.Hibernate.Linq
{
    public class HibernateQueryProvider : IQueryProvider
    {
        private HibernateClientContext _context;
        private Type _baseType;

        internal HibernateQueryProvider(HibernateClientContext context, Type baseType)
        {
            this._context = context;
            this._baseType = baseType;
        }

        #region IQueryProvider Members

        public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        {
            return new HibernateQuery<TElement>(_context, expression, this);
        }

        public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        {
            throw new NotImplementedException();
        }

        internal IEnumerable<EntityType> ExecuteInternal<EntityType>(System.Linq.Expressions.Expression expression)
        {
            using (NHibernate.ISession session = new Sage.Platform.Orm.SessionScopeWrapper(false))
            {

                HibernateTranslator<EntityType> translator = new HibernateTranslator<EntityType>();

                HibernateRequest request = translator.TranslateExpression(expression, session);

                IEnumerable enumerable = (IEnumerable)request.GetType().GetMethod("List", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).MakeGenericMethod(_baseType).Invoke(request, new object[] { }); ;

                foreach (object item in enumerable)
                {
                    //If a projection is used, the Load has to be called via reflection, because the generic type might not match 'EntityType'
                    if (request.ProjectionExpressions.Count > 0)
                        yield return request.PerformSelect<EntityType>(item);
                    else
                        yield return (EntityType)item;
                }
            }

        }

        public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        {
            MethodCallExpression methodCallExpression = (MethodCallExpression)expression;

            if (methodCallExpression.Method.Name == "First")
                return ExecuteInternal<TResult>(methodCallExpression.Arguments[0]).First();

            if (methodCallExpression.Method.Name == "Last")
                return ExecuteInternal<TResult>(methodCallExpression.Arguments[0]).Last();


            throw new InvalidOperationException(String.Format("Operation '{0}' is not implemented", methodCallExpression.Method.Name));
        }

        public object Execute(System.Linq.Expressions.Expression expression)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
