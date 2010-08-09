using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.SalesLogix.Client.SData;
using System.Collections;
using System.Reflection;
using System.Linq.Expressions;

namespace Sage.SalesLogix.Client.SData.Linq
{
    internal class SDataQueryProvider : IQueryProvider
    {
        private Type _baseType;
        private SDataClientContext _context;

        internal SDataQueryProvider(SDataClientContext context, Type baseType)
        {
            this._context = context;
            this._baseType = baseType;
        }

        internal IEnumerable<EntityType> ExecuteInternal<EntityType>(System.Linq.Expressions.Expression expression)
        {
            SDataTranslator translator = new SDataTranslator();

            IEnumerable<SDataRequest> requests = translator.TranslateExpression(expression, _context.Service);

            foreach (SDataRequest request in requests)
            {
                //If a projection is used, the Load has to be called via reflection, because the generic type might not match 'EntityType'
                if (request.ProjectionExpressions.Count > 0)
                {

                    IEnumerable collection = (IEnumerable)(typeof(SDataClientContext).GetMethod("Load", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(BaseType).Invoke(_context, new object[] { request.Request }));

                    foreach (object item in collection)
                        yield return request.PerformSelect<EntityType>(item);
                }
                else
                    foreach (var item in _context.Load<EntityType>(request.Request))
                        yield return item;

            }

        }

        internal Type BaseType
        {
            get
            {
                return _baseType;
            }
        }

        #region IQueryProvider Members

        public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        {
            return new SDataQuery<TElement>(_context, this, expression);
        }

        public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        {
            throw new NotImplementedException();
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
            IEnumerable enumerable = (IEnumerable)GetType().GetMethod("ExecuteInternal", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(expression.Type).Invoke(this, new object[] { expression });

            return enumerable.GetEnumerator().Current;
        }


        #endregion
    }
}
