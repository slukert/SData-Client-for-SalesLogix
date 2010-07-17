using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sage.Entity.Interfaces;
using Sage.SData.Client.Extensions;
using System.CodeDom;
using System.Reflection;
using System.CodeDom.Compiler;
using System.IO;

namespace Sage.SalesLogix.SData.Client
{
    public static class ClientFactory
    {
        public static bool OutputGeneratedClass;

        internal static Type GenerateProxyClass(Type interfaceType)
        {
            CodeCompileUnit compileUnit = new CodeCompileUnit();
            CodeNamespace codeNamespace = new CodeNamespace("Sage.SDataClient.Generated");

            {
                compileUnit.Namespaces.Add(codeNamespace);
            }

            //Generate Class
            CodeTypeDeclaration typeDeclatation = new CodeTypeDeclaration(String.Format("SData{0}Impl", interfaceType.Name));

            {
                typeDeclatation.BaseTypes.Add(new CodeTypeReference(typeof(ClientEntityBase)));
                typeDeclatation.BaseTypes.Add(new CodeTypeReference(interfaceType));
                codeNamespace.Types.Add(typeDeclatation);
            }

            //Generate Constructor
            CodeConstructor constructor = new CodeConstructor();

            {
                typeDeclatation.Members.Add(constructor);
                constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(SDataPayload), "payload"));
                constructor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ClientContext), "context"));
                constructor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("payload"));
                constructor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("context"));
                constructor.Attributes = MemberAttributes.Public;
            }

            //Generate Properties           
            foreach (PropertyInfo property in interfaceType.GetProperties())
            {

                GeneratePropertyCode(typeDeclatation, property);

            }

            //Generate Methods
            foreach (MethodInfo method in interfaceType.GetMethods())
                GenerateMethodCode(typeDeclatation, method);


            if (OutputGeneratedClass)
                OutputGeneratedCode(compileUnit);

            return CompileCode(compileUnit);
        }

        private static Type CompileCode(CodeCompileUnit compileUnit)
        {
            Microsoft.CSharp.CSharpCodeProvider codeProvider = new Microsoft.CSharp.CSharpCodeProvider();
            CompilerParameters compilerParameters = new CompilerParameters();

            compilerParameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            string currentBinLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            compilerParameters.ReferencedAssemblies.Add(Path.Combine(currentBinLocation, "Sage.Entity.Interfaces.dll"));
            compilerParameters.ReferencedAssemblies.Add(Path.Combine(currentBinLocation, "Sage.Platform.dll"));
            compilerParameters.ReferencedAssemblies.Add(Path.Combine(currentBinLocation, "Sage.SData.Client.dll"));

            CompilerResults result = codeProvider.CompileAssemblyFromDom(compilerParameters, compileUnit);

            if (result.Errors.Count > 0)
                throw new InvalidOperationException(result.Errors[0].ErrorText);

            //The only type within the Assembly is the type requested here.
            return result.CompiledAssembly.GetTypes()[0];
        }

        private static void OutputGeneratedCode(CodeCompileUnit compileUnit)
        {
            Microsoft.CSharp.CSharpCodeProvider codeProvider = new Microsoft.CSharp.CSharpCodeProvider();

            using (MemoryStream stream = new MemoryStream())
            using (TextWriter textWriter = new StreamWriter(stream))
            {
                codeProvider.GenerateCodeFromCompileUnit(compileUnit, textWriter, new System.CodeDom.Compiler.CodeGeneratorOptions());

                textWriter.Flush();

                stream.Seek(0, SeekOrigin.Begin);

                using (StreamReader reader = new StreamReader(stream))
                {
                    string ouptut = reader.ReadToEnd();

                    Console.WriteLine(ouptut);
                }
            }
        }

        private static void GenerateMethodCode(CodeTypeDeclaration typeDeclatation, MethodInfo method)
        {
            if (!(method.Name.StartsWith("get_") || method.Name.StartsWith("set_")))
            {
                CodeMemberMethod codeMethod = new CodeMemberMethod();

                codeMethod.Name = method.Name;
                codeMethod.ReturnType = new CodeTypeReference(method.ReturnType);
                codeMethod.Attributes = MemberAttributes.Public | MemberAttributes.Final;

                foreach (ParameterInfo parameterInfo in method.GetParameters())
                    codeMethod.Parameters.Add(new CodeParameterDeclarationExpression(parameterInfo.ParameterType, parameterInfo.Name));

                codeMethod.Statements.Add(
                    new CodeThrowExceptionStatement(
                        new CodeObjectCreateExpression(
                            new CodeTypeReference(typeof(InvalidOperationException)),
                            new CodePrimitiveExpression("Method invokation will be supported in SLX 7.5.3"))));

                typeDeclatation.Members.Add(codeMethod);
            }
        }

        private static void GeneratePropertyCode(CodeTypeDeclaration typeDeclatation, PropertyInfo property)
        {
            CodeMemberProperty codeProperty = new CodeMemberProperty();

            codeProperty.Name = property.Name;
            codeProperty.Type = new CodeTypeReference(property.PropertyType);
            codeProperty.Attributes = MemberAttributes.Public;

            if (property.PropertyType.Name == "ICollection`1")
            {

                GenerateCollectionPropertyCode(property, codeProperty);

            }
            else
                if (property.PropertyType.FullName.StartsWith("Sage.Entity.Interfaces."))
                {
                    GenerateEntityPropertyGetterCode(property, codeProperty);

                    if (property.CanWrite)
                        GeneratePropertySetterCode(property, codeProperty);

                }
                else
                {
                    GeneratePropertyGetterCode(property, codeProperty);

                    if (property.CanWrite)
                        GeneratePropertySetterCode(property, codeProperty);
                }

            typeDeclatation.Members.Add(codeProperty);
        }

        private static void GeneratePropertyGetterCode(PropertyInfo property, CodeMemberProperty codeProperty)
        {
            codeProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(
                            new CodeBaseReferenceExpression(),
                            "GetPrimitiveValue",
                            new CodeTypeReference(property.PropertyType)),
                        new CodePrimitiveExpression(property.Name))));
        }

        private static void GeneratePropertySetterCode(PropertyInfo property, CodeMemberProperty codeProperty)
        {
            codeProperty.SetStatements.Add(
                new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(
                            new CodeBaseReferenceExpression(),
                            "SetPrimitiveValue",
                            new CodeTypeReference(property.PropertyType)),
                        new CodePrimitiveExpression(property.Name),
                        new CodeVariableReferenceExpression("value")));
        }

        private static void GenerateEntityPropertyGetterCode(PropertyInfo property, CodeMemberProperty codeProperty)
        {
            codeProperty.GetStatements.Add(
                new CodeConditionStatement(
                    new CodeBinaryOperatorExpression(
                        new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("_Payload"),
                                    "Values"),
                                "ContainsKey"),
                            new CodePrimitiveExpression(property.Name)),

                        CodeBinaryOperatorType.IdentityEquality,
                        new CodePrimitiveExpression(false)),
                    new CodeMethodReturnStatement(
                        new CodeDefaultValueExpression(
                            new CodeTypeReference(property.PropertyType)))));

            codeProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(
                            new CodeVariableReferenceExpression("_Context"),
                            "GetProxyClient",
                            new CodeTypeReference(property.PropertyType)),
                        new CodeCastExpression(
                            new CodeTypeReference(
                                 typeof(SDataPayload)),
                            new CodeIndexerExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("_Payload"),
                                    "Values"),
                                new CodePrimitiveExpression(property.Name))))));
        }

        private static void GenerateCollectionPropertyCode(PropertyInfo property, CodeMemberProperty codeProperty)
        {
            Type propertyType = property.PropertyType.GetGenericArguments()[0];

            codeProperty.GetStatements.Add(
                new CodeConditionStatement(
                    new CodeBinaryOperatorExpression(
                        new CodeMethodInvokeExpression(
                            new CodeMethodReferenceExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("_Payload"),
                                    "Values"),
                                "ContainsKey"),
                            new CodePrimitiveExpression(property.Name)),

                        CodeBinaryOperatorType.IdentityEquality,
                        new CodePrimitiveExpression(false)),
                    new CodeMethodReturnStatement(
                        new CodeDefaultValueExpression(
                            new CodeTypeReference(property.PropertyType)))));

            codeProperty.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeMethodInvokeExpression(
                        new CodeMethodReferenceExpression(
                            new CodeVariableReferenceExpression("_Context"),
                            "GetProxyClients",
                            new CodeTypeReference(propertyType)),
                        new CodeCastExpression(
                            new CodeTypeReference(
                                 typeof(SDataPayloadCollection)),
                            new CodeIndexerExpression(
                                new CodePropertyReferenceExpression(
                                    new CodeVariableReferenceExpression("_Payload"),
                                    "Values"),
                                new CodePrimitiveExpression(property.Name))))));
        }
    }
}
