// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Language;
using Kusto.Language.Syntax;
using NUnit.Framework;
using SyncKusto.Kusto.Exceptions;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Tests.Kusto;

[TestFixture]
[Category("SchemaImport")]
public class SchemaCommandSyntaxAdapterTests
{
    private const string FunctionText = ".create-or-alter function ['a function'](x:string='default') { database('other').T }";
    private const string TableText = ".create table ['a table'] (['a column']:long, b:string)";

    [TestCase(FunctionText, 0)]
    [TestCase(".create-or-alter function with(folder='a',docstring='b',skipvalidation='true',view=true) ['a function'](x:string='default') { database('other').T }", 4)]
    public void FunctionContract_ExtractsTargetTypedDeclarationAndProperties(string text, int propertyCount)
    {
        var syntax = Parse(text);
        var result = SchemaCommandSyntaxAdapter.ReadFunction(syntax);

        Assert.That(result.Name.SimpleName, Is.EqualTo("a function"));
        Assert.That(result.Definition.Parameters.Parameters.Count, Is.EqualTo(1));
        Assert.That(Text(text, result.Definition), Is.EqualTo("(x:string='default') { database('other').T }"));
        Assert.That(result.Properties.Count, Is.EqualTo(propertyCount));
        if (propertyCount != 0)
        {
            Assert.That(result.Properties.Select(property => property.Name),
                Is.EqualTo(new[] { "folder", "docstring", "skipvalidation", "view" }));
            Assert.That(result.Properties.Select(property => property.Value.LiteralValue),
                Is.EqualTo(new object[] { "a", "b", "true", true }));
        }
    }

    [TestCase(TableText, false, 0)]
    [TestCase(".create table ['a table'] (['a column']:long, b:string) with(folder='a',docstring='b')", false, 2)]
    [TestCase(".create-merge table ['a table'] (['a column']:long, b:string)", true, 0)]
    [TestCase(".create-merge table ['a table'] (['a column']:long, b:string) with(docstring='b',folder='a')", true, 2)]
    public void TableContract_ExtractsTargetExplicitColumnsAndProperties(string text, bool merge, int propertyCount)
    {
        var result = SchemaCommandSyntaxAdapter.ReadTable(Parse(text), merge);

        Assert.That(result.Name.SimpleName, Is.EqualTo("a table"));
        Assert.That(Text(text, result.Columns), Is.EqualTo("(['a column']:long, b:string)"));
        Assert.That(result.Properties.Count, Is.EqualTo(propertyCount));
        Assert.That(result.Columns.GetDescendants<NameDeclaration>().Select(name => name.SimpleName),
            Is.EqualTo(new[] { "a column", "b" }));
    }

    [Test]
    public void NamedFunctionAndPropertyFields_AreNotBoundToChildPositions()
    {
        var syntax = Parse(".create-or-alter function with(docstring='help') f() {T}");
        var clause = Children(syntax).Select(child => child.Element).OfType<CustomNode>().Single();
        var list = List(clause);
        var property = (CustomNode)list.Single().Element;
        // Synthetic SDK layouts, not alternative input syntax: production still requires a recognized command.
        var reorderedProperty = Node(Children(property).Reverse());
        var reorderedClause = Replace(clause, list, Items(reorderedProperty));
        var reorderedCommand = Node(Children(Replace(syntax, clause, reorderedClause)).Reverse());

        var result = SchemaCommandSyntaxAdapter.ReadFunction(reorderedCommand);

        Assert.That(result.Name.SimpleName, Is.EqualTo("f"));
        Assert.That(result.Properties.Single().Name, Is.EqualTo("docstring"));
        Assert.That(result.Properties.Single().Value.LiteralValue, Is.EqualTo("help"));
        Assert.That(result.Definition.Body.Expression, Is.TypeOf<NameReference>());
    }

    [Test]
    public void NamedTableAndColumnFields_AreNotBoundToChildPositions()
    {
        var syntax = Parse(TableText);
        var columns = Children(syntax).Select(child => child.Element).OfType<CustomNode>().Single();
        var list = List(columns);
        var reorderedColumns = list.Select(item => Node(Children((CustomNode)item.Element).Reverse())).ToArray();
        var reorderedCommand = Node(Children(Replace(syntax, columns,
            Replace(columns, list, Items(reorderedColumns)))).Reverse());

        var result = SchemaCommandSyntaxAdapter.ReadTable(reorderedCommand, false);

        Assert.That(result.Name.SimpleName, Is.EqualTo("a table"));
        Assert.That(result.Columns.GetDescendants<NameDeclaration>().Select(name => name.SimpleName),
            Is.EqualTo(new[] { "a column", "b" }));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void CommandContract_RejectsMissingRenamedDuplicateOrWrongTypedTargets(bool function)
    {
        var syntax = Parse(function ? FunctionText : TableText);
        string name = function ? "FunctionName" : "TableName";
        var children = Children(syntax).ToArray();
        var target = children.Single(child => child.Name == name);
        var variants = new[]
        {
            Node(children.Where(child => child.Name != name)),
            Node(children.Select(child => child.Name == name ? child with { Name = "UnknownTarget" } : child)),
            Node(children.Append(target)),
            Node(children.Select(child => child.Name == name
                ? child with { Element = SyntaxToken.Identifier("", "unexpected") } : child)),
            Node(children.Append(new Child("UnknownField", SyntaxToken.Identifier("", "unexpected")))),
            Node(children.Append(new Child("", SyntaxToken.Punctuation("", SyntaxKind.BarToken))))
        };

        foreach (var variant in variants)
            Assert.Throws<SchemaParserCompatibilityException>(() => Read(variant, function));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void CommandContract_RejectsMissingOrDuplicateDefinitionsAndUnexpectedTokens(bool function)
    {
        var syntax = Parse(function ? FunctionText : TableText);
        var children = Children(syntax).ToArray();
        var definition = children.Single(child => child.Element is FunctionDeclaration or CustomNode);
        foreach (var variant in new[]
        {
            Node(children.Where(child => child != definition)),
            Node(children.Append(definition)),
            Node(children.Where(child => child.Element is not SyntaxToken)),
            Node(children.Select(child => child.Element is SyntaxToken { Text: "function" or "table" }
                ? child with { Element = SyntaxToken.Identifier("", "unsupported") } : child))
        })
        {
            Assert.Throws<SchemaParserCompatibilityException>(() => Read(variant, function));
        }
    }

    [Test]
    public void WithClauseAndPropertyContract_RejectsUnknownAndAmbiguousLayouts()
    {
        var syntax = Parse(".create-or-alter function with(docstring='help') f() {T}");
        var clause = Children(syntax).Select(child => child.Element).OfType<CustomNode>().Single();
        var list = List(clause);
        var property = (CustomNode)list.Single().Element;
        var propertyChildren = Children(property).ToArray();
        foreach (var invalidProperty in new[]
        {
            Node(propertyChildren.Where(child => child.Name != "PropertyName")),
            Node(propertyChildren.Where(child => child.Name != "PropertyValue")),
            Node(propertyChildren.Select(child => child.Name == "PropertyName"
                ? child with { Name = "RenamedProperty" } : child)),
            Node(propertyChildren.Append(propertyChildren.Single(child => child.Name == "PropertyValue"))),
            Node(propertyChildren.Append(new Child("", SyntaxToken.Punctuation("", SyntaxKind.BarToken)))),
            Node(propertyChildren.Where(child => child.Element is not SyntaxToken { Kind: SyntaxKind.EqualToken }))
        })
        {
            var invalidClause = Replace(clause, list, Items(invalidProperty));
            Assert.Throws<SchemaParserCompatibilityException>(() =>
                SchemaCommandSyntaxAdapter.ReadFunction(Replace(syntax, clause, invalidClause)));
        }

        foreach (var invalidClause in new[]
        {
            Node(Children(clause).Where(child => child.Element is not SyntaxToken { Kind: SyntaxKind.WithKeyword })),
            Node(Children(clause).Append(new Child("", SyntaxToken.Identifier("", "unexpected")))),
            Node(Children(clause).Append(new Child("", list))),
            Node(Children(clause).Where(child => !ReferenceEquals(child.Element, list)))
        })
        {
            Assert.Throws<SchemaParserCompatibilityException>(() =>
                SchemaCommandSyntaxAdapter.ReadFunction(Replace(syntax, clause, invalidClause)));
        }
    }

    [Test]
    public void ColumnContract_RejectsUnrecognizedColumnFieldsInsteadOfCopyingThem()
    {
        var syntax = Parse(TableText);
        var columns = Children(syntax).Select(child => child.Element).OfType<CustomNode>().Single();
        var list = List(columns);
        var column = (CustomNode)list.First().Element;
        foreach (var invalidColumn in new[]
        {
            Node(Children(column).Where(child => child.Name != "ColumnType")),
            Node(Children(column).Append(new Child("UnknownColumnOption", SyntaxToken.Identifier("", "unexpected"))))
        })
        {
            var invalidColumns = Replace(columns, list, Items(invalidColumn));
            Assert.Throws<SchemaParserCompatibilityException>(() =>
                SchemaCommandSyntaxAdapter.ReadTable(Replace(syntax, columns, invalidColumns), false));
        }
    }

    [Test]
    public void AdditionalEmptyOptionalSlot_DoesNotDependOnTotalChildCount()
    {
        var syntax = Parse(FunctionText);
        var expanded = Node(Children(syntax).Append(new Child("", null, Optional: true)));
        Assert.That(SchemaCommandSyntaxAdapter.ReadFunction(expanded).Name.SimpleName, Is.EqualTo("a function"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void AbsentMetadata_DoesNotRequireAnEmptyPositionalSlot(bool function)
    {
        var syntax = Parse(function ? FunctionText : TableText);
        var compact = Node(Children(syntax).Where(child => child.Element != null));
        if (function)
        {
            var result = SchemaCommandSyntaxAdapter.ReadFunction(compact);
            Assert.That(result.Name.SimpleName, Is.EqualTo("a function"));
            Assert.That(result.Properties, Is.Empty);
        }
        else
        {
            var result = SchemaCommandSyntaxAdapter.ReadTable(compact, false);
            Assert.That(result.Name.SimpleName, Is.EqualTo("a table"));
            Assert.That(result.Properties, Is.Empty);
        }
    }

    [Test]
    public void UnexpectedListSeparators_AreNotCopiedIntoCanonicalCommands()
    {
        var syntax = Parse(TableText);
        var columns = Children(syntax).Select(child => child.Element).OfType<CustomNode>().Single();
        var list = List(columns);
        var changedList = new SyntaxList<SeparatedElement<SyntaxElement>>(list.Select((item, index) =>
            new SeparatedElement<SyntaxElement>(item.Element.Clone(),
                index == 0 ? SyntaxToken.Punctuation("", SyntaxKind.SemicolonToken) : null)));
        var changedColumns = Replace(columns, list, changedList);

        Assert.Throws<SchemaParserCompatibilityException>(() =>
            SchemaCommandSyntaxAdapter.ReadTable(Replace(syntax, columns, changedColumns), false));
    }

    [Test]
    public void NonCustomSyntax_IsACompatibilityErrorRatherThanAnInvalidCast()
    {
        Assert.Throws<SchemaParserCompatibilityException>(() =>
            SchemaCommandSyntaxAdapter.ReadFunction(SyntaxToken.Identifier("", "unexpected")));
    }

    private sealed record Child(string Name, SyntaxElement? Element, bool Optional = false);

    private static CustomNode Parse(string text)
    {
        var code = KustoCode.Parse(text);
        Assert.That(code.GetDiagnostics(), Is.Empty);
        var block = (CommandBlock)code.Syntax;
        return (CustomNode)((CustomCommand)((ExpressionStatement)block.Statements.Single().Element).Expression).Custom;
    }

    private static void Read(CustomNode syntax, bool function)
    {
        if (function)
            SchemaCommandSyntaxAdapter.ReadFunction(syntax);
        else
            SchemaCommandSyntaxAdapter.ReadTable(syntax, false);
    }

    private static IEnumerable<Child> Children(CustomNode node) =>
        Enumerable.Range(0, node.ChildCount)
            .Select(index => new Child(node.GetName(index), node.GetChild(index), node.IsOptional(index)));

    private static CustomNode Node(IEnumerable<Child> children)
    {
        var array = children.ToArray();
        return new CustomNode(
            array.Select(child => CustomElementDescriptor.From(child.Name, isOptional: child.Optional)).ToArray(),
            array.Select(child => child.Element?.Clone()!).ToArray());
    }

    private static CustomNode Replace(CustomNode parent, SyntaxElement original, SyntaxElement replacement) =>
        Node(Children(parent).Select(child => ReferenceEquals(child.Element, original)
            ? child with { Element = replacement } : child));

    private static SyntaxList<SeparatedElement<SyntaxElement>> List(CustomNode node) =>
        Children(node).Select(child => child.Element).OfType<SyntaxList<SeparatedElement<SyntaxElement>>>().Single();

    private static SyntaxList<SeparatedElement<SyntaxElement>> Items(params CustomNode[] nodes) =>
        new(nodes.Select((node, index) => new SeparatedElement<SyntaxElement>(
            node, index == nodes.Length - 1 ? null : SyntaxToken.Punctuation("", SyntaxKind.CommaToken))));

    private static string Text(string text, SyntaxElement node) => text.Substring(node.TextStart, node.Width);
}
