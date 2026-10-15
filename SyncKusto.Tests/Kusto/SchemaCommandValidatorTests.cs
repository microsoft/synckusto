// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Kusto.Data.Common;
using Kusto.Language;
using Kusto.Language.Syntax;
using NUnit.Framework;
using SyncKusto.Kusto.Exceptions;
using SyncKusto.Kusto.Services;

namespace SyncKusto.Tests.Kusto;

[TestFixture]
[Category("SchemaImport")]
public class SchemaCommandValidatorTests
{
    [TestCase(".create-or-alter function f() { T }")]
    [TestCase("// legacy\n.create-or-alter function\n['odd name'] (x:string='; } .drop table t', t:(a:long)) {\nlet g=(x:long){x+1};\nt | extend a=g(a)\n}; // end")]
    [TestCase(".create-or-alter function with (docstring=@'skipvalidation=true; .drop table t', folder='a/b') f(x:dynamic=dynamic({\"a\":\"}\"})) { print x }")]
    [TestCase(".create-or-alter function with (skipvalidation=false, view=true) f() { database('other').T }")]
    [TestCase(".create-or-alter function with (skipvalidation=@'true', folder='a',docstring=\"b\") f() { cluster('c').database('db').T }")]
    [TestCase(".create-or-alter function with(docstring='a' // keep literal trivia\n'b',folder=@'a''b',skipvalidation='tr' 'ue') f() {T}")]
    [TestCase(".create-or-alter function f() { let text = ```\n}; .drop table t\n```; print text }")]
    [TestCase(".create-or-alter function f() { // } ; .drop table t\nprint x='(\\'quoted\\')' } // .drop database x")]
    public void Function_PreservesDeclarationAndTarget(string input)
    {
        var original = Command(input, "CreateOrAlterFunction");
        var originalDeclaration = Function(original);
        string canonical = SchemaCommandValidator.Function(input);
        var parsed = Command(canonical, "CreateOrAlterFunction");
        var declaration = Function(parsed);

        Assert.That(Text(canonical, declaration), Is.EqualTo(Text(input, originalDeclaration)));
        Assert.That(NamedChild<NameDeclaration>(parsed.Custom, "FunctionName").SimpleName,
            Is.EqualTo(NamedChild<NameDeclaration>(original.Custom, "FunctionName").SimpleName));
        Assert.That(canonical, Does.Contain("skipvalidation = true"));
        Assert.That(SchemaCommandValidator.Function(canonical), Is.EqualTo(canonical));
    }

    [TestCase(".create table t(x:long)", "CreateTable")]
    [TestCase(".create-merge table ['a name'] (['x;y']:string, y:dynamic) with (folder=@'a', docstring='; .drop table t')", "CreateMergeTable")]
    [TestCase("// heading\n.create table t (\n['a']: datetime,\nb:guid\n) with (docstring=```a\n; b\n```) ; // end", "CreateTable")]
    [TestCase(".create table t(x:string) with(docstring='a' // literal comment\n'b',folder=@'one' 'two')", "CreateTable")]
    public void Table_PreservesExplicitColumnsAndCommandKind(string input, string kind)
    {
        var original = Command(input, kind);
        string canonical = SchemaCommandValidator.Table(input);
        var parsed = Command(canonical, kind);
        Assert.That(Text(canonical, Columns(parsed)), Is.EqualTo(Text(input, Columns(original))));
        Assert.That(NamedChild<NameDeclaration>(parsed.Custom, "TableName").SimpleName,
            Is.EqualTo(NamedChild<NameDeclaration>(original.Custom, "TableName").SimpleName));
        Assert.That(SchemaCommandValidator.Table(canonical), Is.EqualTo(canonical));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ExportedFunctions_WithMetadataAndDefaults_AreAccepted(bool skipValidation)
    {
        var schema = new FunctionSchema
        {
            Name = "quoted ' function",
            Folder = "Folder\\Nested",
            DocString = "Help \"text\"; { }",
            InputParameters = new List<FunctionParameterSchema>
            {
                new() { Name = "value", CslType = "string", CslDefaultValue = "'default'" }
            },
            Body = "{ DependentFunction(value) }",
            Kind = FunctionSchema.FunctionKind.ViewFunction
        };
        string exported = CslCommandGenerator.GenerateCreateOrAlterFunctionCommand(schema, skipValidation);
        string canonical = SchemaCommandValidator.Function(exported);
        var command = Command(canonical, "CreateOrAlterFunction");
        Assert.That(NamedChild<NameDeclaration>(command.Custom, "FunctionName").SimpleName, Is.EqualTo(schema.Name));
        Assert.That(canonical, Does.Contain("view = true"));
        Assert.That(canonical, Does.Contain("skipvalidation = true"));
        Assert.That(Function(command).Parameters.Parameters.Count, Is.EqualTo(1));
        Assert.That(canonical, Does.Contain(schema.Body));
        Assert.That(canonical, Does.Contain("default"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExportedTables_WithMetadataAndFormatting_AreAccepted(bool merge)
    {
        var schema = new TableSchema("a ' table",
            new[] { new ColumnSchema("a", "System.Int64") }, "folder", "help");
        string exported = FormattedCslCommandGenerator.GenerateTableCreateCommand(
            schema, true, merge, true);
        var command = Command(SchemaCommandValidator.Table(exported), merge ? "CreateMergeTable" : "CreateTable");
        Assert.That(NamedChild<NameDeclaration>(command.Custom, "TableName").SimpleName, Is.EqualTo(schema.Name));
    }

    [Test]
    public void VeryLongQuotedNames_AreNotRestrictedToFileNames()
    {
        string name = new string('a', 350) + " '; }";
        string input = $".create-or-alter function {CslSyntaxGenerator.NormalizeName(name)}() {{ T }}";
        var command = Command(SchemaCommandValidator.Function(input), "CreateOrAlterFunction");
        Assert.That(NamedChild<NameDeclaration>(command.Custom, "FunctionName").SimpleName, Is.EqualTo(name));
    }

    [Test]
    public void Properties_PreserveLiteralValuesAndQuotingWithoutTreatingStringContentAsProperties()
    {
        string input = ".create-or-alter function with(docstring=@'skipvalidation=true; {()}',folder='a' 'b',view='false') f() {T}";
        string canonical = SchemaCommandValidator.Function(input);
        var command = Command(canonical, "CreateOrAlterFunction");
        var clause = Children(command.Custom).OfType<CustomNode>().Single();
        var properties = Children(clause).OfType<SyntaxList<SeparatedElement<SyntaxElement>>>().Single();
        var values = properties.Select(item => item.Element).ToDictionary(
            item => NamedChild<SyntaxElement>(item, "PropertyName") switch
            {
                NameDeclaration name => name.SimpleName,
                SyntaxToken token => token.Text,
                _ => throw new AssertionException("Unexpected property shape")
            },
            item => NamedChild<Expression>(item, "PropertyValue").LiteralValue);
        Assert.That(values.Count, Is.EqualTo(4));
        Assert.That(values["docstring"], Is.EqualTo("skipvalidation=true; {()}"));
        Assert.That(values["folder"], Is.EqualTo("ab"));
        Assert.That(values["view"], Is.EqualTo(false));
        Assert.That(values["skipvalidation"], Is.EqualTo(true));
        Assert.That(canonical, Does.Contain("docstring = @'skipvalidation=true; {()}'"));
    }

    [Test]
    public void Function_CanonicalCommandRemainsUnchangedByAdapterExtraction()
    {
        string input = ".create-or-alter function with(folder='one',view='false',skipvalidation=false) ['a function'](x:long=1) {print x}";
        Assert.That(SchemaCommandValidator.Function(input), Is.EqualTo(
            ".create-or-alter function with (folder = 'one', view = false, skipvalidation = true) ['a function'] (x:long=1) {print x}"));
    }

    [Test]
    public void Table_CanonicalCommandRemainsUnchangedByAdapterExtraction()
    {
        string input = ".create-merge table ['a table'](x:long,y:string) with(folder='one',docstring='help')";
        Assert.That(SchemaCommandValidator.Table(input), Is.EqualTo(
            ".create-merge table ['a table'] (x:long,y:string) with (folder = 'one', docstring = 'help')"));
    }

    private static IEnumerable<string> InvalidFunctions()
    {
        yield return "";
        yield return "// .create-or-alter function f() {T}";
        yield return ".drop database other";
        yield return ".create table t(x:long)";
        yield return ".create function f() {T}";
        yield return ".alter function f() {T}";
        yield return ".execute database script <| .create-or-alter function f() {T}";
        yield return ".create-or-alter function database('other').f() {T}";
        yield return ".create-or-alter function cluster('c').database('other').f() {T}";
        yield return ".create-or-alter function db.f() {T}";
        yield return ".create-or-alter function f() {T}; .drop table t";
        yield return ".create-or-alter function f() {T}\n.drop table t";
        yield return ".create-or-alter function f() {T}; print 1";
        yield return ".create-or-alter function f() {T} | count";
        yield return ".create-or-alter function f() {T} ignored_text";
        yield return ".create-or-alter function f() {T}}";
        yield return ".create-or-alter function f(x:string {T}";
        yield return ".create-or-alter function f() { print '; .drop database other' ";
        yield return ".create-or-alter function with (folder=1) f() {T}";
        yield return ".create-or-alter function with (skipvalidation='true; .drop table t') f() {T}";
        yield return ".create-or-alter function with (folder='a',folder='b') f() {T}";
        yield return ".create-or-alter function with (unknown='a') f() {T}";
        yield return ".create-or-alter function with (view='anything') f() {T}";
        yield return ".create-or-alter function f() {T}; // harmless\n.drop table t";
        yield return ".create-or-alter function f() {T}\n\n.execute database script <| .drop table t";
        yield return "#set querytrace\n.create-or-alter function f() {T}";
    }

    private static IEnumerable<string> InvalidTables()
    {
        yield return ".drop table t";
        yield return ".create tables (t(x:long),u(y:string))";
        yield return ".alter table t(x:long)";
        yield return ".create table t based-on other";
        yield return ".create table database('other').t(x:long)";
        yield return ".create table ['other'].['t'] (x:long)";
        yield return ".create-merge table db.t(x:long)";
        yield return ".create table t(x:long); .drop database other";
        yield return ".create table t(x:long)\n.create table u(y:string)";
        yield return ".create table t(x:long); print 1";
        yield return ".create table t(x:long) | count";
        yield return ".create table t(x:long) unexpected";
        yield return ".create table t(x:long) with (folder='a', ingestionMapping='x')";
        yield return ".create table t(x:long) with (docstring='a', docstring='b')";
        yield return ".create table t(x:long) with (skipvalidation=true)";
        yield return ".create table t(x:long) with (folder=dynamic({\"a\":1}))";
        yield return ".create table t(x:)";
        yield return ".execute database script <| .create table t(x:long)";
        yield return ".create-or-alter function f() { print x='.create table t(x:long)' }";
    }

    [TestCaseSource(nameof(InvalidFunctions))]
    public void Function_RejectsUnsupportedOrIncompleteInput(string text) =>
        Assert.Throws<SchemaCommandException>(() => SchemaCommandValidator.Function(text));

    [TestCaseSource(nameof(InvalidTables))]
    public void Table_RejectsUnsupportedOrIncompleteInput(string text) =>
        Assert.Throws<SchemaCommandException>(() => SchemaCommandValidator.Table(text));

    [Test]
    public void SyntaxErrors_ReportRealOneBasedParserLocationWithoutPayload()
    {
        string input = ".create-or-alter function f() {\n  print private_payload = \n}";
        var code = KustoCode.Parse(input);
        var diagnostic = code.GetDiagnostics().First(d => d.Severity == DiagnosticSeverity.Error);
        Assert.That(code.TryGetLineAndOffset(diagnostic.Start, out int line, out int column), Is.True);
        var error = Assert.Throws<SchemaCommandException>(() => SchemaCommandValidator.Function(input))!;
        Assert.That(error.Line, Is.EqualTo(line).And.GreaterThan(0));
        Assert.That(error.Column, Is.EqualTo(column).And.GreaterThan(0));
        Assert.That(error.Message + error.Hint, Does.Not.Contain("private_payload"));
        Assert.That(error.Hint, Does.StartWith("Complete the expression"));
    }

    [TestCase(".create-or-alter function f() {print 1", "}")]
    [TestCase(".create-or-alter function f(x:long {T}", ")")]
    public void KnownMissingTokens_OfferSpecificCorrections(string text, string missing)
    {
        var error = Assert.Throws<SchemaCommandException>(() => SchemaCommandValidator.Function(text))!;
        Assert.That(error.Hint, Does.StartWith($"Add the missing '{missing}'"));
    }

    internal static CustomCommand Command(string text, string kind)
    {
        var code = KustoCode.Parse(text);
        Assert.That(code.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);
        Assert.That(code.Syntax, Is.TypeOf<CommandBlock>());
        var block = (CommandBlock)code.Syntax;
        Assert.That(block.Statements.Count, Is.EqualTo(1));
        Assert.That(block.SkippedTokens, Is.Null);
        var expression = ((ExpressionStatement)block.Statements[0].Element).Expression;
        Assert.That(expression, Is.TypeOf<CustomCommand>());
        var command = (CustomCommand)expression;
        Assert.That(command.CommandKind, Is.EqualTo(kind));
        return command;
    }

    private static string Text(string text, SyntaxElement syntax) =>
        text.Substring(syntax.TextStart, syntax.Width);

    private static IEnumerable<SyntaxElement> Children(SyntaxElement parent) =>
        Enumerable.Range(0, parent.ChildCount).Select(parent.GetChild).Where(child => child != null);

    private static T NamedChild<T>(SyntaxElement parent, string name) where T : SyntaxElement =>
        (T)Children(parent).Single(child => child.NameInParent == name);

    private static FunctionDeclaration Function(CustomCommand command) =>
        Children(command.Custom).OfType<FunctionDeclaration>().Single();

    private static CustomNode Columns(CustomCommand command) =>
        Children(command.Custom).OfType<CustomNode>()
            .Single(node => node.GetFirstToken().Kind == SyntaxKind.OpenParenToken);
}
