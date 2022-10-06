using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace VSharp.TestRenderer;

using static CodeRenderer;

public static class Renderer
{
    public struct MethodFormat
    {
        public bool HasArgs = false;
        public string? CallingTest = null;

        public MethodFormat()
        {
        }
    }
    private static readonly Dictionary<string, MethodFormat> MethodsFormat = new ();

    private static IEnumerable<string>? _extraAssemblyLoadDirs;

    // TODO: create class 'Expression' with operators?

    private static void RenderTest(
        MethodRenderer test,
        MethodBase method,
        IEnumerable<object> args,
        object? thisArg,
        bool isError,
        Type? ex,
        object expected)
    {
        var mainBlock = test.Body;
        MethodFormat f = new MethodFormat();

        // Declaring arguments and 'this' of testing method
        IdentifierNameSyntax? thisArgId = null;
        if (thisArg != null)
        {
            Debug.Assert(Reflection.hasThis(method));
            var renderedThis = mainBlock.RenderObject(thisArg);
            if (renderedThis is IdentifierNameSyntax id)
            {
                thisArgId = id;
            }
            else
            {
                var thisType = RenderType(method.DeclaringType ?? typeof(object));
                thisArgId = mainBlock.AddDecl("thisArg", thisType, mainBlock.RenderObject(thisArg));
            }
        }
        var renderedArgs = args.Select(mainBlock.RenderObject).ToArray();

        var parameters = method.GetParameters();
        Debug.Assert(parameters.Length == renderedArgs.Length);
        for (int i = 0; i < parameters.Length; i++)
        {
            var parameterInfo = parameters[i];
            var type = parameterInfo.ParameterType;
            var value = renderedArgs[i];
            if (type.IsByRef && value is not IdentifierNameSyntax)
            {
                Debug.Assert(type.GetElementType() != null);
                var typeExpr = RenderType(type.GetElementType());
                var id = mainBlock.AddDecl("obj", typeExpr, value);
                renderedArgs[i] = id;
            }
            else
            {
                f.HasArgs = true;
            }
        }

        f.HasArgs = f.HasArgs || thisArgId != null;

        // Calling testing method
        var callMethod = RenderCall(thisArgId, method, renderedArgs);
        f.CallingTest = callMethod.NormalizeWhitespace().ToString();

        var hasResult = Reflection.hasNonVoidResult(method) || method.IsConstructor;
        var shouldUseDecl = method.IsConstructor || IsGetPropertyMethod(method, out _);
        var shouldCompareResult = hasResult && ex == null && !isError;

        if (shouldCompareResult)
        {
            var retType = Reflection.getMethodReturnType(method);
            if (retType.IsPrimitive || retType == typeof(string))
            {
                var expectedExpr = method.IsConstructor ? thisArgId : mainBlock.RenderObject(expected);

                var resultId = mainBlock.AddDecl("result", null, callMethod);
                var condition = RenderCall(RenderMemberAccess("Assert", "AreEqual"), resultId, expectedExpr);
                mainBlock.AddExpression(condition);
            }
            else
            {
                // Adding namespace of objects comparer to usings
                AddObjectsComparer();
                var expectedExpr = method.IsConstructor ? thisArgId : mainBlock.RenderObject(expected);

                var resultId = mainBlock.AddDecl("result", null, callMethod);
                var condition = RenderCall(CompareObjects, resultId, expectedExpr);
                mainBlock.AddAssert(condition);
            }
        }
        else if (ex == null || isError)
        {
            if (shouldUseDecl)
                mainBlock.AddDecl("unused", null, callMethod);
            else
                mainBlock.AddExpression(callMethod);
        }
        else
        {
            // Handling exceptions
            LambdaExpressionSyntax delegateExpr;
            if (shouldUseDecl)
            {
                var block = mainBlock.NewBlock();
                block.AddDecl("unused", null, callMethod);
                delegateExpr = ParenthesizedLambdaExpression(block.Render());
            }
            else
                delegateExpr = ParenthesizedLambdaExpression(callMethod);

            var assertThrows =
                RenderCall(
                    "Assert", "Throws",
                    new []{ RenderType(ex) },
                    delegateExpr
                );
            mainBlock.AddExpression(assertThrows);
        }

        MethodsFormat[test.MethodId.ToString()] = f;
    }

    private static Assembly TryLoadAssemblyFrom(object sender, ResolveEventArgs args)
    {
        var existingInstance = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.FullName == args.Name);
        if (existingInstance != null)
        {
            return existingInstance;
        }
        foreach (string path in _extraAssemblyLoadDirs)
        {
            string assemblyPath = Path.Combine(path, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath))
                return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }

        return null;
    }

    private class IndentsRewriter : CSharpSyntaxRewriter
    {
        private const int TabSize = 4;
        private int _currentOffset = 0;
        private MethodFormat _format = new MethodFormat();
        private string? _firstExpr = null;

        private static SyntaxTrivia WhitespaceTrivia(int offset)
        {
            return Whitespace(new string(' ', offset));
        }

        public override SyntaxNode? Visit(SyntaxNode? node)
        {
            if (node == null)
                return null;

            if (node.HasLeadingTrivia)
            {
                var triviaList = node.GetLeadingTrivia();
                var whitespaces =
                    triviaList
                        .Where(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia))
                        .ToArray();

                _currentOffset = whitespaces[whitespaces.Length - 1].ToFullString().Length;
            }

            switch (node)
            {
                // Deleting whitespace between 'null' and '!'
                case PostfixUnaryExpressionSyntax unary
                    when unary.IsKind(SyntaxKind.SuppressNullableWarningExpression):
                {
                    var operand = unary.Operand.WithTrailingTrivia();
                    var unaryOperator = unary.OperatorToken.WithLeadingTrivia();
                    node = unary.WithOperatorToken(unaryOperator).WithOperand(operand);
                    return base.Visit(node);
                }
                // Formatting initializer with line breaks
                case ObjectCreationExpressionSyntax objCreation:
                {
                    var init = objCreation.Initializer;
                    if (init != null)
                    {
                        var expressions = init.Expressions;
                        if (expressions.Count > 0)
                        {
                            var formattedExpressions = new List<ExpressionSyntax>();
                            foreach (var assign in expressions)
                            {
                                var formatted =
                                    assign.WithLeadingTrivia(LineFeed, WhitespaceTrivia(_currentOffset + TabSize));
                                formattedExpressions.Add(formatted);
                            }

                            init = init.WithExpressions(SeparatedList(formattedExpressions));
                            var formattedCloseBrace =
                                init.CloseBraceToken
                                    .WithLeadingTrivia(LineFeed, WhitespaceTrivia(_currentOffset));
                            init = init.WithCloseBraceToken(formattedCloseBrace);
                        }

                        node = objCreation.WithInitializer(init);
                    }

                    return base.Visit(node);
                }
                case StatementSyntax statement when statement.ToString() == _firstExpr:
                {
                    var comment = Comment("// arrange");
                    node = statement.WithLeadingTrivia().WithLeadingTrivia(WhitespaceTrivia(_currentOffset), comment, LineFeed,
                        WhitespaceTrivia(_currentOffset));
                    return base.Visit(node);
                }
                case LocalDeclarationStatementSyntax varDecl:
                {
                    var vars = varDecl.Declaration.Variables;
                    if (vars.Count > 0)
                    {
                        var init = vars[0].Initializer;
                        // Adding '// test' comment before calling test
                        if (_format.CallingTest != null && init != null && _format.CallingTest == init.Value.ToString())
                        {
                            if (_format.HasArgs)
                            {
                                var comment = Comment("// act");
                                node = node.WithLeadingTrivia(LineFeed, WhitespaceTrivia(_currentOffset), comment, LineFeed,
                                    WhitespaceTrivia(_currentOffset));
                            }
                            else
                            {
                                var comment = Comment("// act");
                                node = node.WithLeadingTrivia(WhitespaceTrivia(_currentOffset), comment, LineFeed,
                                    WhitespaceTrivia(_currentOffset));
                            }
                        }

                        return base.Visit(node);
                    }
                    break;
                }
                // Adding '// test' comment before calling test
                case ExpressionStatementSyntax expr
                    when _format.CallingTest != null && _format.CallingTest == expr.Expression.ToString():
                {
                    if (_format.HasArgs)
                    {
                        var comment = Comment("// act");
                        node = node.WithLeadingTrivia(LineFeed, WhitespaceTrivia(_currentOffset), comment, LineFeed,
                            WhitespaceTrivia(_currentOffset));
                    }
                    else
                    {
                        var comment = Comment("// act");
                        node = node.WithLeadingTrivia(WhitespaceTrivia(_currentOffset), comment, LineFeed,
                            WhitespaceTrivia(_currentOffset));
                    }

                    return base.Visit(node);
                }
                // Remembering current method
                case MethodDeclarationSyntax expr:
                {
                    MethodsFormat.TryGetValue(expr.Identifier.ToString(), out _format);
                    if (_format.HasArgs)
                        _firstExpr = expr.Body?.Statements[0].ToString();

                    return base.Visit(node);
                }
                // Adding blank line and '// assert' comment before 'Assert.Throws' and 'Assert.IsTrue'
                case ExpressionStatementSyntax expr when expr.ToString().Contains("Assert"):
                {
                    var comment = Comment("// assert");
                    node = node.WithLeadingTrivia(LineFeed, WhitespaceTrivia(_currentOffset), comment, LineFeed,
                        WhitespaceTrivia(_currentOffset));
                    return base.Visit(node);
                }
            }

            return base.Visit(node);
        }
    }

    public static SyntaxNode Format(SyntaxNode node)
    {
        var normalized = node.NormalizeWhitespace();
        var myRewriter = new IndentsRewriter();
        var formatted = myRewriter.Visit(normalized);
        Debug.Assert(formatted != null);
        return formatted;
    }

    public static void RenderTests(IEnumerable<FileInfo> tests)
    {
        AppDomain.CurrentDomain.AssemblyResolve += TryLoadAssemblyFrom;

        PrepareCache();
        // Adding NUnit namespace to usings
        AddNUnit();

        // Creating main class for generating tests
        var generatedClass =
            new ClassRenderer(
                "GeneratedClass",
                RenderAttributeList("TestFixture"),
                null
            );

        foreach (var fi in tests)
        {
            testInfo ti;
            using (var stream = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read))
            {
                ti = UnitTest.DeserializeTestInfo(stream);
            }

            _extraAssemblyLoadDirs = ti.extraAssemblyLoadDirs;
            UnitTest test = UnitTest.DeserializeFromTestInfo(ti, true);

            var method = test.Method;
            var parameters = test.Args ?? method.GetParameters()
                .Select(t => Reflection.defaultOf(t.ParameterType)).ToArray();
            var thisArg = test.ThisArg;
            if (thisArg == null && !method.IsStatic)
                thisArg = Reflection.createObject(method.DeclaringType);
            string suitTypeName;
            if (fi.Name.Contains("error"))
                suitTypeName = "Error";
            else
            {
                Debug.Assert(fi.Name.Contains("test"));
                suitTypeName = "Test";
            }

            string testName;
            if (method.IsConstructor)
                testName = $"{RenderType(method.DeclaringType)}Constructor";
            else if (IsGetPropertyMethod(method, out testName))
                testName = $"Get{testName}";
            else if (IsSetPropertyMethod(method, out testName))
                testName = $"Set{testName}";
            else
                testName = method.Name;

            bool isUnsafe =
                Reflection.getMethodReturnType(method).IsPointer || method.GetParameters()
                    .Select(arg => arg.ParameterType)
                    .Any(type => type.IsPointer);
            var modifiers = isUnsafe ? new[] { Public, Unsafe } : new[] { Public };

            var testRenderer = generatedClass.AddMethod(
                testName + suitTypeName,
                RenderAttributeList("Test"),
                modifiers,
                VoidType,
                System.Array.Empty<(TypeSyntax, string)>()
            );
            RenderTest(testRenderer, method, parameters, thisArg, test.IsError, test.Exception, test.Expected);
        }

        SyntaxNode compilation =
            RenderProgram(
                "GeneratedNamespace",
                generatedClass.Render()
            );

        compilation = Format(compilation);
        // compilation.WriteTo(Console.Out);
        var dir = $"{Directory.GetCurrentDirectory()}{Path.DirectorySeparatorChar}generated.cs";
        using var streamWriter = new StreamWriter(File.Create(dir));
        compilation.WriteTo(streamWriter);
    }
}
