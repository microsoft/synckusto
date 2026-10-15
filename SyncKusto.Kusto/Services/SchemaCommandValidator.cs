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
            (function
                ? command.CommandKind != "CreateOrAlterFunction"
                : command.CommandKind is not ("CreateTable" or "CreateMergeTable")) ||
            command.GetDescendants<SkippedTokens>().Count != 0)
        {
            throw new SchemaCommandException($"must contain a single {supported} definition",
                "Keep one supported definition, remove other commands, and compare again.");
        }

        if (function)
        {
            var parsed = SchemaCommandSyntaxAdapter.ReadFunction(command.Custom);
            string name = ObjectName(parsed.Name);
            if (parsed.Definition.ViewKeyword != null)
            {
                throw new SchemaCommandException($"must contain a single {supported} definition",
                    "Use a parameter list followed by a braced function body and compare again.");
            }

            // Preserve the parsed parameter list and body verbatim, including defaults, comments,
            // quoted identifiers, multiline strings, and references to other databases.
            var properties = ReadProperties(parsed.Properties, text, function: true);
            properties.Add("skipvalidation = true");
            return $".create-or-alter function with ({string.Join(", ", properties)}) " +
                name + " " + text.Substring(parsed.Definition.TextStart, parsed.Definition.Width);
        }

        bool merge = command.CommandKind == "CreateMergeTable";
        var table = SchemaCommandSyntaxAdapter.ReadTable(command.Custom, merge);
        string tableName = ObjectName(table.Name);
        var tableProperties = ReadProperties(table.Properties, text, function: false);
        string verb = merge ? ".create-merge" : ".create";
        string metadata = tableProperties.Count == 0 ? "" : $" with ({string.Join(", ", tableProperties)})";
        return $"{verb} table {tableName} " +
            text.Substring(table.Columns.TextStart, table.Columns.Width) + metadata;
    }

    private static string ObjectName(NameDeclaration name)
    {
        if (name.Name is not (TokenName or BracketedName))
        {
            throw new SchemaCommandException("must declare an unqualified object name",
                "Remove the database or cluster qualifier and compare again.");
        }

        return CslSyntaxGenerator.NormalizeName(name.SimpleName);
    }

    private static List<string> ReadProperties(
        IReadOnlyList<SchemaCommandSyntaxAdapter.Property> properties, string text, bool function)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in properties)
        {
            string name = property.Name;
            var value = property.Value;
            if (!seen.Add(name))
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
