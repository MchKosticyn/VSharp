using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace VSharp.TestRenderer;

using static CodeRenderer;

internal interface IBlock
{
    IdentifierNameSyntax NewIdentifier(string idName);
    IBlock NewBlock();
    IdentifierNameSyntax AddDecl(string varName, TypeSyntax? type, ExpressionSyntax init, bool reuse = false);
    void AddExpression(ExpressionSyntax expression);
    void AddAssertEqual(ExpressionSyntax x, ExpressionSyntax y);
    void AddAssert(ExpressionSyntax condition);
    void AddTryCatch(BlockSyntax tryBlock, TypeSyntax catchType, IdentifierNameSyntax exVar, BlockSyntax catchBlock);
    void AddTryCatch(BlockSyntax tryBlock, BlockSyntax catchBlock);
    void AddIf(ExpressionSyntax condition, StatementSyntax thenBranch, StatementSyntax? elseBranch = null);
    void AddFor(TypeSyntax? type, IdentifierNameSyntax iterator, ExpressionSyntax condition, ExpressionSyntax increment, BlockSyntax forBody);
    void AddFor(TypeSyntax? type, IdentifierNameSyntax iterator, ExpressionSyntax length, BlockSyntax forBody);
    void AddWhile(ExpressionSyntax condition, BlockSyntax whileBody);
    void AddForEach(TypeSyntax? type, IdentifierNameSyntax iterator, IdentifierNameSyntax where, BlockSyntax foreachBody);
    void AddForEach(TypeSyntax? type, IdentifierNameSyntax[] iterators, IdentifierNameSyntax where, BlockSyntax foreachBody);
    void AddReturn(ExpressionSyntax? whatToReturn);
    ExpressionSyntax RenderObject(object? obj, string? preferredName);
    BlockSyntax Render();
}

internal class MethodRenderer
{
    private readonly BaseMethodDeclarationSyntax _declaration;

    public IdentifierNameSyntax[] ParametersIds { get; }
    public SimpleNameSyntax MethodId { get; }
    public IBlock Body { get; }

    public MethodRenderer(
        IdentifiersCache cache,
        SimpleNameSyntax methodId,
        AttributeListSyntax? attributes,
        SyntaxToken[] modifiers,
        bool isConstructor,
        TypeSyntax resultType,
        IdentifierNameSyntax[]? generics,
        params (TypeSyntax, string)[] args)
    {
        // Creating identifiers cache
        var methodCache = new IdentifiersCache(cache);
        // Creating method declaration
        MethodId = methodId;
        if (isConstructor)
        {
            _declaration = ConstructorDeclaration(methodId.Identifier);
        }
        else
        {
            var methodDecl = MethodDeclaration(resultType, methodId.Identifier);
            if (generics != null)
            {
                var typeVars =
                    TypeParameterList(
                        SeparatedList(
                            generics.Select(generic =>
                                TypeParameter(generic.Identifier)
                            )
                        )
                    );
                methodDecl = methodDecl.WithTypeParameterList(typeVars);
            }
            _declaration = methodDecl;
        }

        if (attributes != null)
            _declaration = _declaration.AddAttributeLists(attributes);
        var parameters = new ParameterSyntax[args.Length];
        ParametersIds = new IdentifierNameSyntax[args.Length];
        for (var i = 0; i < args.Length; i++)
        {
            var (type, varName) = args[i];
            var arg = methodCache.GenerateIdentifier(varName);
            ParametersIds[i] = arg;
            parameters[i] = Parameter(arg.Identifier).WithType(type);
        }
        var parameterList =
            ParameterList(
                SeparatedList<ParameterSyntax>()
                    .AddRange(parameters)
            );
        _declaration =
            _declaration
                .AddModifiers(modifiers)
                .WithParameterList(parameterList);
        Body = new BlockBuilder(methodCache);
    }

    private class BlockBuilder : IBlock
    {
        // Variables cache
        private readonly IdentifiersCache _cache;
        // Rendering objects cache
        private readonly Dictionary<physicalAddress, ExpressionSyntax> _renderedObjects;
        private readonly HashSet<physicalAddress> _startToRender;

        private readonly List<StatementSyntax> _statements = new();

        public BlockBuilder(IdentifiersCache cache)
        {
            _cache = cache;
            _renderedObjects = new Dictionary<physicalAddress, ExpressionSyntax>();
            _startToRender = new HashSet<physicalAddress>();
        }

        public IdentifierNameSyntax NewIdentifier(string idName)
        {
            return _cache.GenerateIdentifier(idName);
        }

        public IBlock NewBlock()
        {
            return new BlockBuilder(new IdentifiersCache(_cache));
        }

        public IdentifierNameSyntax AddDecl(
            string varName,
            TypeSyntax? type,
            ExpressionSyntax init,
            bool reuse = false)
        {
            // TODO: to check for equality of syntax nodes use 'AreEquivalent'
            string initializerString = init.ToString();
            if (reuse && _cache.TryGetIdByInit(initializerString, out var result))
            {
                Debug.Assert(result != null);
                return result;
            }

            var var = _cache.GenerateIdentifier(varName);
            var varDecl = RenderVarDecl(type, var.Identifier, init);
            _statements.Add(LocalDeclarationStatement(varDecl));
            _cache.SetIdInit(var, initializerString);
            return var;
        }

        public void AddExpression(ExpressionSyntax expression)
        {
            _statements.Add(ExpressionStatement(expression));
        }

        public void AddAssignment(AssignmentExpressionSyntax assignment)
        {
            _statements.Add(ExpressionStatement(assignment));
        }

        public void AddAssertEqual(ExpressionSyntax x, ExpressionSyntax y)
        {
            _statements.Add(ExpressionStatement(RenderAssertEqual(x, y)));
        }

        public void AddAssert(ExpressionSyntax condition)
        {
            _statements.Add(ExpressionStatement(RenderAssert(condition)));
        }

        private void AddTryCatch(BlockSyntax tryBlock, CatchDeclarationSyntax? declaration, BlockSyntax catchBlock)
        {
            var catchClause = CatchClause(declaration, null, catchBlock);
            var clauses = SingletonList(catchClause);
            var tryCatchBlock = TryStatement(tryBlock, clauses, null);
            _statements.Add(tryCatchBlock);
        }

        public void AddTryCatch(BlockSyntax tryBlock, BlockSyntax catchBlock)
        {
            AddTryCatch(tryBlock, null, catchBlock);
        }

        public void AddTryCatch(BlockSyntax tryBlock, TypeSyntax catchType, IdentifierNameSyntax exVar, BlockSyntax catchBlock)
        {
            var declaration = CatchDeclaration(catchType, exVar.Identifier);
            AddTryCatch(tryBlock, declaration, catchBlock);
        }

        public void AddIf(ExpressionSyntax condition, StatementSyntax thenBranch, StatementSyntax? elseBranch = null)
        {
            ElseClauseSyntax elseClause = null!;
            if (elseBranch != null) elseClause = ElseClause(elseBranch);
            var ifExpr = IfStatement(condition, thenBranch, elseClause);
            _statements.Add(ifExpr);
        }

        public void AddFor(
            TypeSyntax? type,
            IdentifierNameSyntax iterator,
            ExpressionSyntax condition,
            ExpressionSyntax increment,
            BlockSyntax forBody)
        {
            type ??= VarKeyword;
            var forStatement =
                ForStatement(
                    RenderVarDecl(type, iterator.Identifier, Zero),
                    SeparatedList<ExpressionSyntax>(),
                    condition,
                    SingletonSeparatedList(increment),
                    forBody
                );
            _statements.Add(forStatement);
        }

        public void AddFor(
            TypeSyntax? type,
            IdentifierNameSyntax iterator,
            ExpressionSyntax length,
            BlockSyntax forBody)
        {
            var increment = PrefixUnaryExpression(SyntaxKind.PreIncrementExpression, iterator);
            var condition = BinaryExpression(SyntaxKind.LessThanExpression, iterator, length);

            AddFor(type, iterator, condition, increment, forBody);
        }

        public void AddWhile(ExpressionSyntax condition, BlockSyntax whileBody)
        {
            var whileStatement = WhileStatement(condition, whileBody);
            _statements.Add(whileStatement);
        }

        public void AddForEach(TypeSyntax? type, IdentifierNameSyntax iterator, IdentifierNameSyntax where, BlockSyntax foreachBody)
        {
            type ??= VarKeyword;
            var forEach = ForEachStatement(type, iterator.Identifier, where, foreachBody);
            _statements.Add(forEach);
        }

        public void AddForEach(TypeSyntax? type, IdentifierNameSyntax[] iterators, IdentifierNameSyntax where, BlockSyntax foreachBody)
        {
            type ??= VarKeyword;
            var designation =
                ParenthesizedVariableDesignation(
                    SeparatedList(
                        iterators.Select(iterator =>
                            (VariableDesignationSyntax) SingleVariableDesignation(iterator.Identifier)
                        )
                    )
                );
            var varDecl = DeclarationExpression(type, designation);
            var forEach = ForEachVariableStatement(varDecl, where, foreachBody);
            _statements.Add(forEach);
        }

        public void AddReturn(ExpressionSyntax? whatToReturn)
        {
            _statements.Add(ReturnStatement(whatToReturn));
        }

        private ExpressionSyntax RenderArray(ArrayTypeSyntax type, System.Array obj, string? preferredName)
        {
            // TODO: use compact array representation, if array is big enough?
            var rank = obj.Rank;
            Debug.Assert(type != null);
            var initializer = new List<ExpressionSyntax>();
            if (rank > 1)
            {
                throw new NotImplementedException("implement rendering for non-vector arrays");
            }
            else
            {
                for (int i = obj.GetLowerBound(0); i <= obj.GetUpperBound(0); i++)
                {
                    var elementPreferredName = (preferredName ?? "array") + "_Elem" + i;
                    // TODO: if lower bound != 0, use Array.CreateInstance
                    initializer.Add(RenderObject(obj.GetValue(i), elementPreferredName));
                }
            }

            return RenderArrayCreation(type, initializer);
        }

        private ExpressionSyntax RenderArray(System.Array obj, string? preferredName)
        {
            var type = (ArrayTypeSyntax) RenderType(obj.GetType());
            return RenderArray(type, obj, preferredName);
        }

        private ExpressionSyntax RenderCompactVector(
            ArrayTypeSyntax type,
            System.Array array,
            int[][] indices,
            object[] values,
            string? preferredName,
            object? defaultValue = null)
        {
            var arrayPreferredName = preferredName ?? "array";
            var createArray = RenderArrayCreation(type, array.Length);
            var arrayId = AddDecl(arrayPreferredName, type, createArray);
            if (defaultValue != null)
            {
                var defaultId = RenderObject(defaultValue, preferredName);
                var call =
                    RenderCall(AllocatorType(), "Fill", arrayId, defaultId);
                AddExpression(call);
            }
            for (int i = 0; i < indices.Length; i++)
            {
                var elementPreferredName = arrayPreferredName + "_Elem" + i;
                var value = RenderObject(values[i], elementPreferredName);
                var assignment = RenderArrayAssignment(arrayId, value, indices[i]);
                AddAssignment(assignment);
            }

            return arrayId;
        }

        private ExpressionSyntax RenderCompactArray(CompactArrayRepr obj, string? preferredName)
        {
            var array = obj.array;
            var indices = obj.indices;
            var values = obj.values;
            var t = array.GetType();
            var type = (ArrayTypeSyntax) RenderType(t);
            Debug.Assert(type != null);
            var defaultOf = Reflection.defaultOf(t.GetElementType());
            var defaultValue = obj.defaultValue;
            if (defaultValue == null || defaultValue.Equals(defaultOf))
            {
                if (t.IsSZArray)
                    return RenderCompactVector(type, array, indices, values, preferredName);
                throw new NotImplementedException();
            }
            if (t.IsSZArray)
                return RenderCompactVector(type, array, indices, values, preferredName, defaultValue);
            throw new NotImplementedException();
        }

        private (ExpressionSyntax, ExpressionSyntax)[] RenderFieldValues(Type type, object obj)
        {
            var fields = Reflection.fieldsOf(false, type);
            var fieldsWithValues = new (ExpressionSyntax, ExpressionSyntax)[fields.Length];
            var i = 0;
            foreach (var (_, fieldInfo) in fields)
            {
                var name = fieldInfo.Name;
                var index = name.IndexOf(">k__BackingField", StringComparison.Ordinal);
                if (index > 0)
                    name = name[1 .. index];
                var fieldName = RenderObject(name, null);
                var fieldValue = RenderObject(fieldInfo.GetValue(obj), name);
                fieldsWithValues[i] = (fieldName, fieldValue);
                i++;
            }

            return fieldsWithValues;
        }

        private ExpressionSyntax RenderFields(object obj, string? preferredName)
        {
            var physAddress = new physicalAddress(obj);
            if (_renderedObjects.TryGetValue(physAddress, out var renderedResult))
                return renderedResult;

            // Adding namespace of allocator to usings
            AddTestExtensions();

            var type = obj.GetType();
            var isPublicType = type.IsPublic || type.IsNested && type.IsNestedPublic;
            var typeExpr = RenderType(isPublicType ? type : typeof(object));

            // Rendering field values of object
            (ExpressionSyntax, ExpressionSyntax)[] fieldsWithValues;
            if (_startToRender.Contains(physAddress))
            {
                fieldsWithValues = System.Array.Empty<(ExpressionSyntax, ExpressionSyntax)>();
            }
            else
            {
                _startToRender.Add(physAddress);
                fieldsWithValues = RenderFieldValues(type, obj);
            }

            // Rendering allocator arguments
            ExpressionSyntax[] args;
            if (_renderedObjects.TryGetValue(physAddress, out var rendered))
            {
                args = new[] {rendered};
            }
            else if (isPublicType)
            {
                args = System.Array.Empty<ExpressionSyntax>();
            }
            else
            {
                Debug.Assert(type.FullName != null);
                var name =
                    LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(type.FullName));
                args = new ExpressionSyntax[] {name};
            }

            var allocator =
                RenderObjectCreation(AllocatorType(typeExpr), args, fieldsWithValues);
            var resultObject = RenderMemberAccess(allocator, AllocatorObject);
            ExpressionSyntax objId;
            // If object was not rendered already, declaring new variable for it
            if (rendered == null)
            {
                objId = AddDecl(preferredName ?? "obj", typeExpr, resultObject);
                _renderedObjects[physAddress] = objId;
            }
            else
            {
                AddAssignment(RenderAssignment(rendered, resultObject));
                objId = rendered;
            }
            return objId;
        }

        private ExpressionSyntax RenderMock(object obj, string? preferredName)
        {
            var typeOfMock = obj.GetType();
            var storageField =
                typeOfMock.GetFields(BindingFlags.Static | BindingFlags.NonPublic)
                    .First(f => f.Name.Contains("Storage"));
            var storage = storageField.GetValue(null) as global::System.Array;
            Debug.Assert(storage != null);
            var mockInfo = GetMockInfo(obj.GetType().Name);
            var mockType = mockInfo.MockName;
            var empty = System.Array.Empty<ExpressionSyntax>();
            var allocator = RenderObjectCreation(AllocatorType(mockType), empty, empty);
            var resultObject = RenderMemberAccess(allocator, AllocatorObject);
            var mockId = AddDecl(preferredName ?? "mock", mockType, resultObject);
            foreach (var (valuesType, setupMethod) in mockInfo.MethodsInfo)
            {
                var values = RenderArray(valuesType, storage, "values");
                var renderedValues =
                    storage.Length <= 5 ? values : AddDecl("values", null, values);
                AddExpression(RenderCall(mockId, setupMethod, renderedValues));
            }

            return mockId;
        }

        public ExpressionSyntax RenderObject(object? obj, string? preferredName) => obj switch
        {
            null => RenderNull(),
            true => True,
            false => False,
            byte n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            sbyte n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            char n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            short n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            ushort n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            int n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            uint n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            long n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            ulong n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            float.NaN => RenderNaN(RenderType(typeof(float))),
            double.NaN => RenderNaN(RenderType(typeof(double))),
            float.Epsilon => RenderEpsilon(RenderType(typeof(float))),
            double.Epsilon => RenderEpsilon(RenderType(typeof(double))),
            float.PositiveInfinity => RenderPosInfinity(RenderType(typeof(float))),
            double.PositiveInfinity => RenderPosInfinity(RenderType(typeof(double))),
            float.NegativeInfinity => RenderNegInfinity(RenderType(typeof(float))),
            double.NegativeInfinity => RenderNegInfinity(RenderType(typeof(double))),
            double n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            decimal n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            nuint n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            nint n => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(n)),
            string s => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(s)),
            System.Array a => RenderArray(a, preferredName),
            CompactArrayRepr a => RenderCompactArray(a, preferredName),
            Enum e => RenderEnum(e),
            Pointer => throw new NotImplementedException("RenderObject: implement rendering of pointers"),
            ValueType => RenderFields(obj, preferredName),
            _ when HasMockInfo(obj.GetType().Name) => RenderMock(obj, preferredName),
            _ when obj.GetType().IsClass => RenderFields(obj, preferredName),
            _ => throw new NotImplementedException($"RenderObject: unexpected object {obj}")
        };

        public BlockSyntax Render()
        {
            return Block(_statements);
        }
    }

    public IdentifierNameSyntax GetOneArg()
    {
        Debug.Assert(ParametersIds.Length == 1);
        return ParametersIds[0];
    }

    public (IdentifierNameSyntax, IdentifierNameSyntax) GetTwoArgs()
    {
        Debug.Assert(ParametersIds.Length == 2);
        return (ParametersIds[0], ParametersIds[1]);
    }

    public BaseMethodDeclarationSyntax Render()
    {
        return _declaration.WithBody(Body.Render());
    }
}
