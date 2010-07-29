using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.SData.Client.Core;
using System.Linq.Expressions;

namespace Sage.SalesLogix.SData.Client.Linq
{
    internal class SDataRequest
    {
        internal SDataResourceCollectionRequest Request { get; private set; }
        
        internal List<LambdaExpression> ProjectionExpressions { get; private set; }

        private List<Delegate> ProjectionDelegates { get; set; }

        internal Type RequestBaseType
        {
            get
            {
                if (ProjectionExpressions.Count == 0)
                    return null;

                return ProjectionExpressions[0].Parameters[0].Type;
            }
        }

        internal T PerformSelect<T>(object item)
        {
            foreach (Delegate method in ProjectionDelegates)            
                item = method.DynamicInvoke(item);            

            return (T)item;
        }

        public SDataRequest(ISDataService service)
        {
            Request = new SDataResourceCollectionRequest(service);
            ProjectionExpressions = new List<LambdaExpression>();
            ProjectionDelegates = new List<Delegate>();
        }

        
        internal void SetProjection(MethodCallExpression expression)
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
    }
}
