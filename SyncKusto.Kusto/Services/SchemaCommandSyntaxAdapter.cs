// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Language.Syntax;
using SyncKusto.Kusto.Exceptions;

namespace SyncKusto.Kusto.Services;

/// <summary>
/// Isolates the SDK's variable-shaped command nodes from schema validation.
/// Every nonempty child must have a known role; parser layout changes fail closed.
/// </summary>
internal static class SchemaCommandSyntaxAdapter
{
    internal sealed record Property(string Name, Expression Value);
    internal sealed record FunctionDefinition(
        NameDeclaration Name, FunctionDeclaration Definition, IReadOnlyList<Property> Properties);
    internal sealed record TableDefinition(
        NameDeclaration Name, CustomNode Columns, IReadOnlyList<Property> Properties);

    public static FunctionDefinition ReadFunction(SyntaxElement syntax)
    {
        var command = new Shape(syntax);
        command.Token("create-or-alter");
        command.Token("function");
        var name = command.Named<NameDeclaration>("FunctionName");
        var definition = command.Required<FunctionDeclaration>();
        var properties = ReadProperties(command.Optional<CustomNode>());
        command.Complete();
        return new FunctionDefinition(name, definition, properties);
    }

    public static TableDefinition ReadTable(SyntaxElement syntax, bool merge)
    {
        var command = new Shape(syntax);
        command.Token(merge ? "create-merge" : "create");
        command.Token("table");
        var name = command.Named<NameDeclaration>("TableName");
        // The SDK leaves the explicit column list and optional with-clause unnamed.
        var columns = command.Required<CustomNode>(
            node => node.GetFirstToken() is { Kind: SyntaxKind.OpenParenToken });
        ReadColumns(columns);
        var properties = ReadProperties(command.Optional<CustomNode>());
        command.Complete();
        return new TableDefinition(name, columns, properties);
    }

    private static void ReadColumns(CustomNode syntax)
    {
        var columns = new Shape(syntax);
        columns.Token(SyntaxKind.OpenParenToken);
        var list = columns.Required<SyntaxList<SeparatedElement<SyntaxElement>>>();
        columns.Token(SyntaxKind.CloseParenToken);
        columns.Complete();

        foreach (var item in list)
        {
            CheckSeparator(item.Separator);
            var column = new Shape(item.Element);
            column.Named<NameDeclaration>("ColumnName");
            column.Token(SyntaxKind.ColonToken);
            column.Named<PrimitiveTypeExpression>("ColumnType");
            column.Complete();
        }
    }

    private static IReadOnlyList<Property> ReadProperties(CustomNode? syntax)
    {
        if (syntax == null)
            return Array.Empty<Property>();

        var clause = new Shape(syntax);
        clause.Token(SyntaxKind.WithKeyword);
        clause.Token(SyntaxKind.OpenParenToken);
        var list = clause.Required<SyntaxList<SeparatedElement<SyntaxElement>>>();
        clause.Token(SyntaxKind.CloseParenToken);
        clause.Complete();

        var properties = new List<Property>();
        foreach (var item in list)
        {
            CheckSeparator(item.Separator);
            var property = new Shape(item.Element);
            string name = property.Named<SyntaxElement>("PropertyName") switch
            {
                NameDeclaration declaration => declaration.SimpleName,
                SyntaxToken token => token.Text,
                _ => throw new SchemaParserCompatibilityException()
            };
            property.Token(SyntaxKind.EqualToken);
            var value = property.Named<Expression>("PropertyValue");
            property.Complete();
            properties.Add(new Property(name, value));
        }

        return properties;
    }

    private static void CheckSeparator(SyntaxToken? separator)
    {
        if (separator != null && separator.Kind != SyntaxKind.CommaToken)
            throw new SchemaParserCompatibilityException();
    }

    /// <summary>
    /// Consumes direct children by semantic field, type, or syntax token, never by position.
    /// Unknown, duplicate, or missing roles are incompatible rather than silently ignored.
    /// </summary>
    private sealed class Shape
    {
        private readonly List<(string Name, SyntaxElement Element)> _remaining = new();

        public Shape(SyntaxElement syntax)
        {
            if (syntax is not CustomNode node || node.ContainsSyntaxDiagnostics || node.IsMissing)
                throw new SchemaParserCompatibilityException();

            for (int i = 0; i < node.ChildCount; i++)
            {
                string name = node.GetName(i);
                if (node.GetChild(i) is { } child)
                {
                    _remaining.Add((name, child));
                }
                else if (!string.IsNullOrEmpty(name) || !node.IsOptional(i))
                {
                    throw new SchemaParserCompatibilityException();
                }
            }
        }

        public T Named<T>(string name) where T : SyntaxElement =>
            Take(child => child.Name == name, required: true) is T element
                ? element
                : throw new SchemaParserCompatibilityException();

        public T Required<T>(Func<T, bool>? predicate = null) where T : SyntaxElement =>
            Optional(predicate) ?? throw new SchemaParserCompatibilityException();

        public T? Optional<T>(Func<T, bool>? predicate = null) where T : SyntaxElement =>
            (T?)Take(child => string.IsNullOrEmpty(child.Name) &&
                child.Element is T element && (predicate == null || predicate(element)), required: false);

        public void Token(string text) =>
            Take(child => string.IsNullOrEmpty(child.Name) &&
                child.Element is SyntaxToken token && token.Text == text, required: true);

        public void Token(SyntaxKind kind) =>
            Take(child => string.IsNullOrEmpty(child.Name) &&
                child.Element is SyntaxToken token && token.Kind == kind, required: true);

        public void Complete()
        {
            if (_remaining.Count != 0)
                throw new SchemaParserCompatibilityException();
        }

        private SyntaxElement? Take(
            Func<(string Name, SyntaxElement Element), bool> predicate, bool required)
        {
            var matches = _remaining.Where(predicate).ToArray();
            if (matches.Length > 1 || (required && matches.Length == 0))
                throw new SchemaParserCompatibilityException();
            if (matches.Length == 0)
                return null;

            _remaining.Remove(matches[0]);
            return matches[0].Element;
        }
    }
}
