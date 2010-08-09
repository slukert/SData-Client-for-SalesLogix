using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.Platform;
using System.Linq.Expressions;

namespace Sage.SalesLogix.Client.Hibernate.Linq
{
    public class HibernateTranslator<EntityType>
    {
        internal HibernateRequest TranslateExpression(System.Linq.Expressions.Expression expression, NHibernate.ISession session)
        {
            HibernateRequest request = new HibernateRequest();

            request.Session = session;

            TranslateExpressionInternal(expression, request);

            return request;
        }

        private Expression TranslateExpressionInternal(System.Linq.Expressions.Expression expression, HibernateRequest request)
        {
            if (expression == null)
                return expression;

            if (expression.NodeType == ExpressionType.Call)
                TranslateMethodCallExpression((MethodCallExpression)expression, request);

            return expression;
        }

        private string ReadExpressionMember(Expression expression)
        {

            UnaryExpression sortExpression = expression as UnaryExpression;

            if (sortExpression == null)
                return null;

            LambdaExpression lambdaExpression = sortExpression.Operand as LambdaExpression;

            if (lambdaExpression == null)
                return null;

            MemberExpression memberExpression = lambdaExpression.Body as MemberExpression;

            if (memberExpression == null)
                return null;

            return memberExpression.Member.Name;

        }

        private void TranslateMethodCallExpression(MethodCallExpression expression, HibernateRequest request)
        {

            foreach (var argument in expression.Arguments)
                TranslateExpressionInternal(argument, request);

            #region SetMaxRows Method

            if (expression.Method.Name == "SetMaxRows")
            {
                request.MaxResults = (int)(((ConstantExpression)expression.Arguments[1]).Value);
                return;
            }

            #endregion

            #region Include Method

            if (expression.Method.Name == "Include")
            {
                //Adding include statement
                string includeExpression = ReadExpressionMember(expression.Arguments[1]);

                if (String.IsNullOrEmpty(includeExpression))
                    throw new InvalidOperationException(String.Format("'{0}' is not a valid include expression", expression.Arguments[1]));

                request.Includes.Add(includeExpression);

                return;
            }

            #endregion

            #region Orderby Method

            if (expression.Method.Name == "OrderBy" ||
                expression.Method.Name == "ThenBy" ||
                expression.Method.Name == "ThenByDescending" ||
                expression.Method.Name == "OrderByDescending")
            {
                //Adding sort expression                               
                string sortExpression = ReadExpressionMember(expression.Arguments[1]);

                if (String.IsNullOrEmpty(sortExpression))
                    throw new InvalidOperationException(String.Format("'{0}' is not a valid sorting expression", expression.Arguments[1]));

                if (expression.Method.Name.EndsWith("Descending"))
                    request.OrderBy.Add(String.Format("{0} DESC", sortExpression));
                else
                    request.OrderBy.Add(String.Format("{0}", sortExpression));

                return;
            }

            #endregion

            #region Where Method

            if (expression.Method.Name == "Where")
            {
                UnaryExpression unaryExpression = expression.Arguments[1] as UnaryExpression;

                if (unaryExpression == null)
                    throw new InvalidOperationException(String.Format("'{0}' is not a valid where expression", expression.Arguments[1]));

                LambdaExpression lambdaExpression = unaryExpression.Operand as LambdaExpression;

                if (lambdaExpression == null)
                    throw new InvalidOperationException(String.Format("'{0}' is not a valid where expression", expression.Arguments[1]));


                request.Where = TranslateConditionExpression(lambdaExpression.Body, request);

                return;
            }

            #endregion

            #region Select Method

            if (expression.Method.Name == "Select")
            {
                request.SetProjection(expression);
                return;
            }

            #endregion

            throw new InvalidOperationException(String.Format("Method '{0}' is not supported", expression.Method.Name));
        }

        /// <summary>
        /// This helpermethod converts object into SData valid arguments
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isInString">Indicates, if a string should be surrounded by quotes</param>
        /// <returns></returns>
        private Sage.Platform.Repository.IExpression ParseValue(object value, bool isInString, HibernateRequest request)
        {
            return null;

            //if (value == null)
            //    return "null";

            //if (value is string && !isInString)
            //{
            //    string valueTyped = (String)value;

            //    return String.Format("'{0}'", valueTyped.Replace("'", "''"));
            //}

            //if (value is DateTime)
            //{
            //    DateTime date = (DateTime)value;

            //    if (date == DateTime.MinValue)
            //        return "null";

            //    return String.Format("@{0:yyyy-MM-ddTHH:mm:ss}@", date);
            //}

            //return Convert.ToString(value);
        }

        private string TranslateConditionExpression(Expression expression, HibernateRequest request)
        {
            return TranslateConditionExpression(expression, request, false);
        }

        private string TranslateBinaryExpression(Expression expression, HibernateRequest request, string format)
        {

            BinaryExpression binaryExpressiong = (BinaryExpression)expression;

            return String.Format(format, TranslateConditionExpression(binaryExpressiong.Left, request), TranslateConditionExpression(binaryExpressiong.Right, request));

        }

        private string TranslateConditionExpression(Expression expression, HibernateRequest request, bool inString)
        {
            if (expression == null)
                return String.Empty;

            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                    return TranslateBinaryExpression(expression, request, "({0} + {1})");
                case ExpressionType.AddChecked:
                    break;
                case ExpressionType.And:
                    break;
                case ExpressionType.AndAlso:
                    return TranslateBinaryExpression(expression, request, "({0} and {1})");
                case ExpressionType.ArrayIndex:
                    break;
                case ExpressionType.ArrayLength:
                    break;
                case ExpressionType.Call:
                    //A number of Methods can be translated into sdata. The Rest has to be invoked
                    MethodCallExpression methodCallExpression = (MethodCallExpression)expression;

                    if (methodCallExpression.Method.Name == "StartsWith" &&
                        methodCallExpression.Method.DeclaringType == typeof(String))
                        return String.Format("{0} like '{1}%'", TranslateConditionExpression(methodCallExpression.Object, request), TranslateConditionExpression(methodCallExpression.Arguments[0], request, true));

                    if (methodCallExpression.Method.Name == "EndsWith" &&
                        methodCallExpression.Method.DeclaringType == typeof(String))
                        return String.Format("{0} like '%{1}'", TranslateConditionExpression(methodCallExpression.Object, request), TranslateConditionExpression(methodCallExpression.Arguments[0], request, true));

                    if (methodCallExpression.Method.Name == "Contains" &&
                       methodCallExpression.Method.DeclaringType == typeof(String))
                        return String.Format("{0} like '%{1}%'", TranslateConditionExpression(methodCallExpression.Object, request), TranslateConditionExpression(methodCallExpression.Arguments[0], request, true));

                    if (methodCallExpression.Method.Name == "Replace" &&
                        methodCallExpression.Method.DeclaringType == typeof(String) &&
                        methodCallExpression.Arguments.Count == 2)
                        return String.Format("replace({0}, {1}, {2})", TranslateConditionExpression(methodCallExpression.Object, request), TranslateConditionExpression(methodCallExpression.Arguments[0], request), TranslateConditionExpression(methodCallExpression.Arguments[1], request), true);

                    if (methodCallExpression.Method.Name == "Substring" &&
                        methodCallExpression.Method.DeclaringType == typeof(String) &&
                        methodCallExpression.Arguments.Count == 2)
                        return String.Format("replace({0}, {1}, {2})", TranslateConditionExpression(methodCallExpression.Object, request), TranslateConditionExpression(methodCallExpression.Arguments[0], request), TranslateConditionExpression(methodCallExpression.Arguments[1], request), true);

                    if (methodCallExpression.Method.Name == "In" &&
                        methodCallExpression.Method.DeclaringType == typeof(QueryExtensions))
                    {
                        return String.Format("({0} in ({1}))", TranslateConditionExpression(methodCallExpression.Arguments[1], request), GetCurrentInCondition((System.Collections.IEnumerable)(Expression.Lambda(methodCallExpression.Arguments[0]).Compile().DynamicInvoke())));
                    }

                    break;
                case ExpressionType.Coalesce:
                    break;
                case ExpressionType.Conditional:
                    break;
                case ExpressionType.Convert:
                    break;
                case ExpressionType.ConvertChecked:
                    break;
                case ExpressionType.Divide:
                    return TranslateBinaryExpression(expression, request, "({0} / {1})");
                case ExpressionType.Equal:
                    return TranslateBinaryExpression(expression, request, "({0} = {1})");
                case ExpressionType.ExclusiveOr:
                    break;
                case ExpressionType.GreaterThan:
                    return TranslateBinaryExpression(expression, request, "({0} > {1})");
                case ExpressionType.GreaterThanOrEqual:
                    return TranslateBinaryExpression(expression, request, "({0} >= {1})");
                case ExpressionType.Invoke:
                    break;
                case ExpressionType.Lambda:
                    break;
                case ExpressionType.LeftShift:
                    break;
                case ExpressionType.LessThan:
                    return TranslateBinaryExpression(expression, request, "({0} < {1})");
                case ExpressionType.LessThanOrEqual:
                    return TranslateBinaryExpression(expression, request, "({0} <= {1})");
                case ExpressionType.ListInit:
                    break;
                case ExpressionType.MemberAccess:
                    MemberExpression memberExpression = (MemberExpression)expression;

                    if (ContainsParameterExpression(memberExpression))
                    {

                        
                        if (memberExpression.Member.Name == "Length" &&
                            memberExpression.Member.DeclaringType == typeof(String))
                            return String.Format("length({0})", TranslateConditionExpression(memberExpression.Expression, request));

                        if (memberExpression.Expression.NodeType == ExpressionType.MemberAccess)
                            return String.Format("{0}.{1}", TranslateConditionExpression(memberExpression.Expression, request), memberExpression.Member.Name);                        

                        if (memberExpression.Expression.NodeType == ExpressionType.Parameter)
                            return memberExpression.Member.Name;
                    }
                    break;
                case ExpressionType.MemberInit:
                    break;
                case ExpressionType.Modulo:
                    break;
                case ExpressionType.Multiply:
                    return TranslateBinaryExpression(expression, request, "({0} * {1})");
                case ExpressionType.MultiplyChecked:
                    break;
                case ExpressionType.Negate:
                    break;
                case ExpressionType.NegateChecked:
                    break;
                case ExpressionType.New:
                    break;
                case ExpressionType.NewArrayBounds:
                    break;
                case ExpressionType.NewArrayInit:
                    {
                        NewArrayExpression arrayExpression = (NewArrayExpression)expression;

                        StringBuilder conditions = new StringBuilder();

                        foreach (Expression item in arrayExpression.Expressions)
                        {
                            if (conditions.Length > 0)
                                conditions.Append(",");

                            conditions.Append(TranslateConditionExpression(item, request));
                        }

                        return conditions.ToString();

                    }
                case ExpressionType.Not:
                    return String.Format("not ({0})", TranslateConditionExpression(((UnaryExpression)expression).Operand, request));
                case ExpressionType.NotEqual:
                    return TranslateBinaryExpression(expression, request, "({0} ne {1})");
                case ExpressionType.Or:
                    break;
                case ExpressionType.OrElse:
                    return TranslateBinaryExpression(expression, request, "({0} or {1})");
                case ExpressionType.Parameter:
                    break;
                case ExpressionType.Power:
                    break;
                case ExpressionType.Quote:
                    break;
                case ExpressionType.RightShift:
                    break;
                case ExpressionType.Subtract:
                    break;
                case ExpressionType.SubtractChecked:
                    break;
                case ExpressionType.TypeAs:
                    break;
                case ExpressionType.TypeIs:
                    break;
                case ExpressionType.UnaryPlus:
                    break;
                default:
                    break;
            }

            //Default handler: Execute method
            try
            {
                object resultValue = Expression.Lambda(expression).Compile().DynamicInvoke();

                return ParseValue(resultValue, inString);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(String.Format("Expression '{0}' could not be evaluated.", expression.ToString()), ex);
            }
        }

        private bool ContainsParameterExpression(Expression expression)
        {

            {
                MemberExpression memberExpression = expression as MemberExpression;

                if (memberExpression != null)
                    return ContainsParameterExpression(memberExpression.Expression);
            }

            {
                ParameterExpression parameterExpression = expression as ParameterExpression;

                if (parameterExpression != null)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Returns the current values for an in-statement. In case of large lists, multiple requests have to be made in order not to create too long uris.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private string GetCurrentInCondition(System.Collections.IEnumerable items)
        {

            List<object> inStatementItems = new List<object>();

            foreach (object item in items)
                inStatementItems.Add(item); ;


            StringBuilder conditions = new StringBuilder();

            foreach (var item in inStatementItems)
            {
                string parsedValue = ParseValue(item, false);

                if (conditions.Length > 0)
                    conditions.Append(",");

                conditions.Append(parsedValue);
            }

            return conditions.ToString();
        }

        /// <summary>
        /// This helpermethod converts object into SData valid arguments
        /// </summary>
        /// <param name="item"></param>
        /// <param name="isInString">Indicates, if a string should be surrounded by quotes</param>
        /// <returns></returns>
        private string ParseValue(object value, bool isInString)
        {
            if (value == null)
                return "null";

            if (value is string && !isInString)
            {
                string valueTyped = (String)value;

                return String.Format("'{0}'", valueTyped.Replace("'", "''"));
            }

            if (value is DateTime)
            {
                DateTime date = (DateTime)value;

                if (date == DateTime.MinValue)
                    return "null";

                return String.Format("@{0:yyyy-MM-ddTHH:mm:ss}@", date);
            }

            return Convert.ToString(value);
        }

    }
}
