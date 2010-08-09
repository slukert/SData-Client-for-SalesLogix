using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.Platform;
using Sage.Platform.Repository;
using System.Linq.Expressions;

namespace Sage.SalesLogix.Client.Hibernate.Linq
{
    public class HibernateRequest
    {
        public HibernateRequest()
        {
            Includes = new List<string>();
            OrderBy = new List<string>();
            ProjectionExpressions = new List<LambdaExpression>();
            ProjectionDelegates = new List<Delegate>();
        }

        public NHibernate.ISession Session { get; set; }
        public List<string> Includes { get; private set; }
        public List<string> OrderBy { get; private set; }
        public string Where { get; set; }
        internal List<LambdaExpression> ProjectionExpressions { get; private set; }
        private List<Delegate> ProjectionDelegates { get; set; }

        public int MaxResults { get; set; }

        internal IList<EntityType> List<EntityType>()
        {
            NHibernate.IQuery query = Session.CreateQuery(CreateQuery<EntityType>());

            if (MaxResults > 0)
                query = query.SetMaxResults(MaxResults);
            return query.List<EntityType>();
            //return (System.Collections.IList)query.GetType().GetMethods().First(x => x.Name == "List" && x.IsGenericMethod).MakeGenericMethod(BaseEntityType).Invoke(query, new object[] { });
        }

        private string CreateQuery<EntityType>()
        {
            StringBuilder queryBuilder = new StringBuilder();

            queryBuilder.Append("from ");

            if (!typeof(EntityType).Name.StartsWith("I"))
                throw new InvalidOperationException(String.Format("'{0}' is not a valid type for query expression. Only use interface types"));

            queryBuilder.Append(typeof(EntityType).Name.Substring(1)).Append(" ");

            if (!String.IsNullOrEmpty(Where))
                queryBuilder.AppendFormat("where {0} ", Where);

            if (OrderBy.Count > 0)
            {
                StringBuilder orderBuilder = new StringBuilder();

                foreach (string orderByExpression in OrderBy)
                {
                    if (orderBuilder.Length > 0)
                        orderBuilder.Append(",");

                    orderBuilder.Append(orderByExpression);
                }

                queryBuilder.AppendFormat("order by {0} ", orderBuilder.ToString());
            }

            return queryBuilder.ToString();
        }

        internal void SetProjection(System.Linq.Expressions.MethodCallExpression expression)
        {
            LambdaExpression lambdaExpression = (LambdaExpression)StripQuotes(expression.Arguments[1]);
            ProjectionExpressions.Add(lambdaExpression);
            ProjectionDelegates.Add(lambdaExpression.Compile());
        }
       
        private Expression StripQuotes(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Quote)
            {
                expression = ((UnaryExpression)expression).Operand;
            }
            return expression;
        }

        internal T PerformSelect<T>(object item)
        {
            foreach (Delegate method in ProjectionDelegates)
                item = method.DynamicInvoke(item);

            return (T)item;
        }
    }
}
