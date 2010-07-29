using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using Sage.SData.Client.Core;
using System.Collections;

namespace Sage.SalesLogix.SData.Client.Linq
{
    /// <summary>
    /// This class translates Linq expressions into SData requests
    /// </summary>
    internal class SDataTranslator
    {
        private SDataRequest _request;

        private List<string> _inStatement;

        private int _currentInIteration;

        private const int MaxListSize = 5000;

        /// <summary>
        /// This method performs the translation from the Linq-Expression into the SData-Request. The request passed in here is a bl
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="request"></param>
        internal List<SDataRequest> TranslateExpression(Expression expression, ISDataService sdataService)
        {

            //Find in Statement to be able to split it
            List<SDataRequest> result = new List<SDataRequest>();

            while (_currentInIteration == 0 || (_inStatement != null && _inStatement.Count > _currentInIteration))
            {
                _request = new SDataRequest(sdataService);

                TranslateExpressionInternal(expression);

                result.Add(_request);
                _currentInIteration++;
            }
            return result;

        }

        private Expression TranslateExpressionInternal(Expression expression)
        {
            if (expression == null)
                return expression;

            if (expression.NodeType == ExpressionType.Call)
                TranslateMethodCallExpression((MethodCallExpression)expression);

            return expression;
        }

        private void TranslateMethodCallExpression(MethodCallExpression expression)
        {

            foreach (var argument in expression.Arguments)
                TranslateExpressionInternal(argument);

            #region SetMaxRows Method

            if (expression.Method.Name == "SetMaxRows")
            {
                _request.Request.Count = (int)(((ConstantExpression)expression.Arguments[1]).Value);

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

                if (String.IsNullOrEmpty(_request.Request.Include))
                    _request.Request.Include = String.Empty;
                else
                    _request.Request.Include += ",";

                _request.Request.Include += includeExpression;

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
                    sortExpression += " desc";

                SetRequestQueryValue("orderby", sortExpression, ",");

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


                string conditionExpression = TranslateConditionExpression(lambdaExpression.Body);

                SetRequestQueryValue("where", conditionExpression, " and ");

                return;
            }

            #endregion

            #region Select Method

            if (expression.Method.Name == "Select")
            {
                _request.SetProjection(expression);
                return;
            }

            #endregion

            throw new InvalidOperationException(String.Format("Method '{0}' is not supported", expression.Method.Name));
        }

        private void SetRequestQueryValue(string valueName, string value, string catValue)
        {
            if (!_request.Request.QueryValues.ContainsKey(valueName))
                _request.Request.QueryValues[valueName] = String.Empty;

            if (!String.IsNullOrEmpty(_request.Request.QueryValues[valueName]))
                _request.Request.QueryValues[valueName] = _request.Request.QueryValues[valueName] + catValue;

            _request.Request.QueryValues[valueName] = _request.Request.QueryValues[valueName] + value;
        }

        private string TranslateConditionExpression(Expression expression)
        {
            return TranslateConditionExpression(expression, false);
        }

        private string TranslateConditionExpression(Expression expression, bool isInString)
        {
            if (expression == null)
                return String.Empty;

            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                    return TranslateBinaryExpression(expression, "({0} + {1})");
                case ExpressionType.AddChecked:
                    break;
                case ExpressionType.And:
                    break;
                case ExpressionType.AndAlso:
                    return TranslateBinaryExpression(expression, "({0} and {1})");
                case ExpressionType.ArrayIndex:
                    break;
                case ExpressionType.ArrayLength:
                    break;
                case ExpressionType.Call:
                    //A number of Methods can be translated into sdata. The Rest has to be invoked
                    MethodCallExpression methodCallExpression = (MethodCallExpression)expression;

                    if (methodCallExpression.Method.Name == "StartsWith" &&
                        methodCallExpression.Method.DeclaringType == typeof(String))
                        return String.Format("({0} like '{1}%')", TranslateConditionExpression(methodCallExpression.Object), TranslateConditionExpression(methodCallExpression.Arguments[0], true));

                    if (methodCallExpression.Method.Name == "EndsWith" &&
                        methodCallExpression.Method.DeclaringType == typeof(String))
                        return String.Format("({0} like '%{1}')", TranslateConditionExpression(methodCallExpression.Object), TranslateConditionExpression(methodCallExpression.Arguments[0], true));

                    if (methodCallExpression.Method.Name == "Contains" &&
                       methodCallExpression.Method.DeclaringType == typeof(String))
                        return String.Format("({0} like '%{1}%')", TranslateConditionExpression(methodCallExpression.Object), TranslateConditionExpression(methodCallExpression.Arguments[0], true));

                    if (methodCallExpression.Method.Name == "Replace" &&
                        methodCallExpression.Method.DeclaringType == typeof(String) &&
                        methodCallExpression.Arguments.Count == 2)
                        return String.Format("replace({0}, {1}, {2})", TranslateConditionExpression(methodCallExpression.Object), TranslateConditionExpression(methodCallExpression.Arguments[0]), TranslateConditionExpression(methodCallExpression.Arguments[1]), true);

                    if (methodCallExpression.Method.Name == "Substring" &&
                        methodCallExpression.Method.DeclaringType == typeof(String) &&
                        methodCallExpression.Arguments.Count == 2)
                        return String.Format("replace({0}, {1}, {2})", TranslateConditionExpression(methodCallExpression.Object), TranslateConditionExpression(methodCallExpression.Arguments[0]), TranslateConditionExpression(methodCallExpression.Arguments[1]), true);

                    if (methodCallExpression.Method.Name == "In" &&
                        methodCallExpression.Method.DeclaringType == typeof(SDataQueryExtensions))
                    {                                                                       
                        return String.Format("({0} in ({1}))", TranslateConditionExpression(methodCallExpression.Arguments[1]), GetCurrentInCondition((IEnumerable)(Expression.Lambda(methodCallExpression.Arguments[0]).Compile().DynamicInvoke())));
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
                    return TranslateBinaryExpression(expression, "({0} / {1})");
                case ExpressionType.Equal:
                    return TranslateBinaryExpression(expression, "({0} eq {1})");
                case ExpressionType.ExclusiveOr:
                    break;
                case ExpressionType.GreaterThan:
                    return TranslateBinaryExpression(expression, "({0} gt {1})");
                case ExpressionType.GreaterThanOrEqual:
                    return TranslateBinaryExpression(expression, "({0} ge {1})");
                case ExpressionType.Invoke:
                    break;
                case ExpressionType.Lambda:
                    break;
                case ExpressionType.LeftShift:
                    break;
                case ExpressionType.LessThan:
                    return TranslateBinaryExpression(expression, "({0} lt {1})");
                case ExpressionType.LessThanOrEqual:
                    return TranslateBinaryExpression(expression, "({0} le {1})");
                case ExpressionType.ListInit:
                    break;
                case ExpressionType.MemberAccess:
                    MemberExpression memberExpression = (MemberExpression)expression;

                    if (memberExpression.Member.Name == "Length" &&
                        memberExpression.Member.DeclaringType == typeof(String))
                        return String.Format("length({0})", TranslateConditionExpression(memberExpression.Expression));

                    if (memberExpression.Expression.NodeType == ExpressionType.Parameter)
                        return memberExpression.Member.Name;

                    break;
                case ExpressionType.MemberInit:
                    break;
                case ExpressionType.Modulo:
                    break;
                case ExpressionType.Multiply:
                    return TranslateBinaryExpression(expression, "({0} * {1})");
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

                            conditions.Append(TranslateConditionExpression(item));
                        }

                        return conditions.ToString();

                    }
                case ExpressionType.Not:
                    return String.Format("not ({0})", TranslateConditionExpression(((UnaryExpression)expression).Operand));
                case ExpressionType.NotEqual:
                    return TranslateBinaryExpression(expression, "({0} ne {1})");
                case ExpressionType.Or:
                    break;
                case ExpressionType.OrElse:
                    return TranslateBinaryExpression(expression, "({0} or {1})");
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

                return ParseValue(resultValue, isInString);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(String.Format("Expression '{0}' could not be evaluated.", expression.ToString()), ex);
            }
        }

        /// <summary>
        /// Returns the current values for an in-statement. In case of large lists, multiple requests have to be made in order not to create too long uris.
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        private string GetCurrentInCondition(IEnumerable items)
        {

            List<object> inStatementItems = new List<object>();

            foreach (object item in items)
                inStatementItems.Add(item);;

            if (inStatementItems.Count > 50)
            {
                if (_inStatement == null)
                {
                    _inStatement = new List<string>();

                    StringBuilder conditions = new StringBuilder();

                    foreach (var item in inStatementItems)
                    {
                        string parsedValue = ParseValue(item, false);

                        if ((conditions.Length + parsedValue.Length + 1) > MaxListSize)
                        {
                            _inStatement.Add(conditions.ToString());
                            conditions = new StringBuilder();
                        }

                        if (conditions.Length > 0)
                            conditions.Append(",");

                        conditions.Append(parsedValue);
                    }

                    if (conditions.Length > 0)
                        _inStatement.Add(conditions.ToString());
                }

                return _inStatement[_currentInIteration];
            }
            else
            {
                //For small lists, the output can be returned directly
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

        private string TranslateBinaryExpression(Expression expression, string format)
        {

            BinaryExpression binaryExpressiong = (BinaryExpression)expression;

            return String.Format(format, TranslateConditionExpression(binaryExpressiong.Left), TranslateConditionExpression(binaryExpressiong.Right));

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
    }
}
