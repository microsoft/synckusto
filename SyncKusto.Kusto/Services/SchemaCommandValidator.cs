// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using Kusto.Language;
using Kusto.Language.Syntax;
using SyncKusto.Kusto.Exceptions;

namespace SyncKusto.Kusto.Services;

/// <summary>
/// Accepts one local schema declaration, without consulting or connecting to Kusto.
/// </summary>
public static class SchemaCommandValidator
{
    public static string Function(string text) => Validate(text, function: true);
    public static string Table(string text) => Validate(text, function: false);

    private static string Validate(string text, bool function)
    {
        ArgumentNullException.ThrowIfNull(text);

        // Parse, not ParseAndAnalyze: dependencies need not exist in the scratch database.
        var code = KustoCode.Parse(text);
        var diagnostic = code.GetDiagnostics().FirstOrDefault(d => d.Severity == DiagnosticSeverity.Error);
        if (diagnostic != null)
        {
            int? line = null, column = null;
            if (code.TryGetLineAndOffset(diagnostic.Start, out int parsedLine, out int parsedColumn))
            {
                line = parsedLine;
                column = parsedColumn;
            }

            throw new SchemaCommandException("has a syntax error", SyntaxHint(diagnostic), line, column);
        }

        var supported = function
            ? ".create-or-alter function"
            : ".create table or .create-merge table";

        if (code.Syntax is not CommandBlock block ||
            block.Directives.Count != 0 ||
            block.Statements.Count != 1 ||
            block.SkippedTokens != null ||
            block.Statements[0].Element is not ExpressionStatement statement ||
            statement.Expression is not CustomCommand command ||
            command.Custom is not CustomNode parts ||
            (function
                ? command.CommandKind != "CreateOrAlterFunction"
                : command.CommandKind is not ("CreateTable" or "CreateMergeTable")) ||
            parts.ChildCount != 5 ||
            command.GetDescendants<SkippedTokens>().Count != 0)
        {
            throw new SchemaCommandException($"must contain a single {supported} definition",
                "Keep one supported definition, remove other commands, and compare again.");
        }

        int nameIndex = function ? 3 : 2;
        if (parts.GetChild(nameIndex) is not NameDeclaration name ||
            name.Name is not (TokenName or BracketedName))
        {
            throw new SchemaCommandException("must declare an unqualified object name",
                "Remove the database or cluster qualifier and compare again.");
        }

        var properties = ReadProperties(parts.GetChild(function ? 2 : 4), text, function);
        if (function)
        {
            if (parts.GetChild(4) is not FunctionDeclaration declaration || declaration.ViewKeyword != null)
            {
                throw new SchemaCommandException($"must contain a single {supported} definition",
                    "Use a parameter list followed by a braced function body and compare again.");
            }

            // Preserve the parsed parameter list and body verbatim, including defaults, comments,
            // quoted identifiers, multiline strings, and references to other databases.
            properties.Add("skipvalidation = true");
            return $".create-or-alter function with ({string.Join(", ", properties)}) " +
                CslSyntaxGenerator.NormalizeName(name.SimpleName) + " " +
                text.Substring(declaration.TextStart, declaration.Width);
        }

        if (parts.GetChild(3) is not CustomNode columns ||
            columns.GetChild(0) is not SyntaxToken { Kind: SyntaxKind.OpenParenToken } ||
            columns.GetChild(1) is not SyntaxList<SeparatedElement<SyntaxElement>>)
        {
            throw new SchemaCommandException($"must contain a single {supported} definition",
                "Use an explicit column list and compare again.");
        }

        string verb = command.CommandKind == "CreateMergeTable" ? ".create-merge" : ".create";
        string metadata = properties.Count == 0 ? "" : $" with ({string.Join(", ", properties)})";
        return $"{verb} table {CslSyntaxGenerator.NormalizeName(name.SimpleName)} " +
            text.Substring(columns.TextStart, columns.Width) + metadata;
    }

    private static List<string> ReadProperties(SyntaxElement? clause, string text, bool function)
    {
        var result = new List<string>();
        if (clause == null)
            return result;

        if (clause is not CustomNode properties ||
            properties.ChildCount != 4 ||
            properties.GetChild(0) is not SyntaxToken { Kind: SyntaxKind.WithKeyword } ||
            properties.GetChild(2) is not SyntaxList<SeparatedElement<SyntaxElement>> list)
        {
            throw PropertyError(function);
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in list)
        {
            if (item.Element is not CustomNode { ChildCount: 3 } property)
                throw PropertyError(function);
            string? name = property.GetChild(0) switch
            {
                NameDeclaration declaration => declaration.SimpleName,
                SyntaxToken token => token.Text,
                _ => null
            };

            if (name == null || !seen.Add(name) ||
                property.GetChild(2) is not Expression value)
                throw PropertyError(function);

            if (name is "folder" or "docstring")
            {
                if (value is not (LiteralExpression { Kind: SyntaxKind.StringLiteralExpression }
                    or CompoundStringLiteralExpression))
                    throw PropertyError(function);
                result.Add($"{name} = {text.Substring(value.TextStart, value.Width)}");
            }
            else if (function && name is ("skipvalidation" or "view"))
            {
                if (value.Kind is not (SyntaxKind.BooleanLiteralExpression
                    or SyntaxKind.StringLiteralExpression or SyntaxKind.CompoundStringLiteralExpression))
                    throw PropertyError(function);
                bool? boolean = value.LiteralValue switch
                {
                    bool b => b,
                    string s when bool.TryParse(s, out bool b) => b,
                    _ => null
                };
                if (boolean == null)
                    throw PropertyError(function);
                if (name == "view")
                    result.Add($"view = {(boolean.Value ? "true" : "false")}");
                // Normalization always supplies skipvalidation, independent of the input value.
            }
            else
            {
                throw PropertyError(function);
            }
        }

        return result;
    }

    private static string SyntaxHint(Diagnostic diagnostic)
    {
        foreach (var (kind, token) in new[]
        {
            (SyntaxKind.CloseBraceToken, "}"), (SyntaxKind.CloseParenToken, ")"),
            (SyntaxKind.CloseBracketToken, "]")
        })
        {
            if (diagnostic.Message == DiagnosticFacts.GetTokenExpected(new[] { kind }).Message)
                return $"Add the missing '{token}' near this location and compare again.";
        }

        return diagnostic.Message == DiagnosticFacts.GetMissingExpression().Message
            ? "Complete the expression near this location and compare again."
            : "Correct the definition near this location and compare again.";
    }

    private static SchemaCommandException PropertyError(bool function) =>
        new("has unsupported or repeated definition properties",
            function
                ? "Use folder/docstring strings and optional skipvalidation/view booleans, once each."
                : "Use only folder and docstring string properties, once each.");
}
