using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.UnusedMethods;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Tests.Rules
{
    public class RemoveUnusedMethodsRuleTests
    {
        private static MetadataReference[] GetMetadataReferences()
        {
            string coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            return new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(typeof(Microsoft.VisualBasic.CompilerServices.StandardModuleAttribute).Assembly.Location)
            };
        }

        private static async Task<string> CleanAsync(string code, string language, SweepOptions? options = null, string? projectFolder = null)
        {
            using AdhocWorkspace workspace = new AdhocWorkspace();
            string extension = language == LanguageNames.CSharp ? ".cs" : ".vb";
            string? projectPath = projectFolder == null ? null : Path.Combine(projectFolder, "Test" + (language == LanguageNames.CSharp ? ".csproj" : ".vbproj"));

            ProjectInfo info = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Default, "TestProject", "TestProject", language, filePath: projectPath)
                .WithMetadataReferences(GetMetadataReferences());
            Project project = workspace.AddProject(info);
            Document document = project.AddDocument("Test" + extension, SourceText.From(code));

            RemoveUnusedMethodsRule rule = new RemoveUnusedMethodsRule(new MarkupIdentifierIndex(), new SolutionNameIndex());
            Document cleaned = await rule.ApplyAsync(document, options ?? new SweepOptions { EnableRemoveUnusedMethods = true });
            return (await cleaned.GetTextAsync()).ToString();
        }

        [Fact]
        public async Task CSharp_RemovesUnusedPrivateMethod_KeepsUsedOne()
        {
            string result = await CleanAsync(@"
public class Sample
{
    public void Run() { Used(); }
    private void Used() { }
    private void Unused() { }
}", LanguageNames.CSharp);

            result.Should().Contain("Used()").And.NotContain("Unused");
        }

        [Fact]
        public async Task VisualBasic_RemovesUnusedPrivateMethod_KeepsUsedOne()
        {
            string result = await CleanAsync(@"
Public Class Sample
    Public Sub Run()
        Used()
    End Sub

    Private Sub Used()
    End Sub

    Private Function Unused() As Integer
        Return 1
    End Function
End Class", LanguageNames.VisualBasic);

            result.Should().Contain("Sub Used()").And.NotContain("Unused");
        }

        [Fact]
        public async Task CSharp_RemovesChainOfUnusedMethods()
        {
            string result = await CleanAsync(@"
public class Sample
{
    private void First() { Second(); }
    private void Second() { Third(); }
    private void Third() { Third(); }
}", LanguageNames.CSharp);

            result.Should().NotContain("First").And.NotContain("Second").And.NotContain("Third");
        }

        [Fact]
        public async Task CSharp_PublicMethod_KeptByDefault_RemovedWhenEnabled()
        {
            string code = @"
public class Sample
{
    public void Unused() { }
}";

            (await CleanAsync(code, LanguageNames.CSharp)).Should().Contain("Unused");

            SweepOptions options = new SweepOptions { EnableRemoveUnusedMethods = true, UnusedMethodsIncludePublic = true };
            (await CleanAsync(code, LanguageNames.CSharp, options)).Should().NotContain("Unused");
        }

        [Fact]
        public async Task CSharp_PublicMethodOfPrivateNestedClass_CountsAsPrivate()
        {
            string result = await CleanAsync(@"
public class Outer
{
    private class Inner
    {
        public void Unused() { }
    }
}", LanguageNames.CSharp);

            result.Should().NotContain("Unused");
        }

        [Fact]
        public async Task CSharp_KeepsFixedExclusions()
        {
            string result = await CleanAsync(@"
using System;
using System.Collections;
public interface IWork { void Work(); }
public abstract class Base { protected abstract void Hook(); protected virtual void Virt() { } }
public sealed class Sample : Base, IWork, IDisposable
{
    public void Work() { }
    protected override void Hook() { }
    void IDisposable.Dispose() { }
    private static void Main() { }
    private IEnumerator GetEnumerator() { return null; }
}", LanguageNames.CSharp, new SweepOptions { EnableRemoveUnusedMethods = true, UnusedMethodsIncludePublic = true, UnusedMethodsIncludeProtected = true });

            result.Should().Contain("void Work()").And.Contain("void Hook()").And.Contain("Dispose()").And.Contain("Main()").And.Contain("GetEnumerator()");
        }

        [Fact]
        public async Task CSharp_KeepsEventHandler_UnlessProtectionIsOff()
        {
            string code = @"
using System;
public class Sample
{
    private void Save_Click(object sender, EventArgs e) { }
}";

            (await CleanAsync(code, LanguageNames.CSharp)).Should().Contain("Save_Click");

            SweepOptions options = new SweepOptions { EnableRemoveUnusedMethods = true, UnusedMethodsProtectEventHandlers = false };
            (await CleanAsync(code, LanguageNames.CSharp, options)).Should().NotContain("Save_Click");
        }

        [Fact]
        public async Task VisualBasic_KeepsMethodWithHandlesClause()
        {
            string result = await CleanAsync(@"
Public Class Source
    Public Event Done As System.Action
End Class

Public Class Sample
    Private WithEvents _source As New Source()

    Private Sub OnDone() Handles _source.Done
    End Sub
End Class", LanguageNames.VisualBasic);

            result.Should().Contain("OnDone");
        }

        [Fact]
        public async Task CSharp_KeepsAttributedMethod_ButNotInformativeAttributes()
        {
            string result = await CleanAsync(@"
using System;
public class MarkerAttribute : Attribute { }
public class Sample
{
    [Marker] private void Marked() { }
    [Obsolete] private void Old() { }
}", LanguageNames.CSharp);

            result.Should().Contain("Marked").And.NotContain("void Old");
        }

        [Fact]
        public async Task CSharp_KeepsMethodNamedInStringOrInactiveCode()
        {
            string result = await CleanAsync(@"
public class Sample
{
    public object Run() { return GetType().GetMethod(""ByReflection""); }
    private void ByReflection() { }
    private void ByDebug() { }
#if NEVER_DEFINED
    private void Trace() { ByDebug(); }
#endif
}", LanguageNames.CSharp);

            result.Should().Contain("void ByReflection").And.Contain("void ByDebug");
        }

        [Fact]
        public async Task CSharp_KeepsNamingConventions()
        {
            string result = await CleanAsync(@"
using System;
public class Sample
{
    public int Size { get; set; }
    private bool ShouldSerializeSize() { return true; }
    private void ResetSize() { }
    private void ResetOther() { }
    protected void Page_Load() { }
}", LanguageNames.CSharp, new SweepOptions { EnableRemoveUnusedMethods = true, UnusedMethodsIncludeProtected = true });

            result.Should().Contain("ShouldSerializeSize").And.Contain("ResetSize").And.Contain("Page_Load").And.NotContain("ResetOther");
        }

        [Fact]
        public async Task CSharp_KeepsMethodReferencedFromMarkup()
        {
            string folder = Path.Combine(Path.GetTempPath(), "CodeSweepTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                File.WriteAllText(Path.Combine(folder, "MainWindow.xaml"), "<Window><Button Click=\"FromXaml\" /></Window>");

                string result = await CleanAsync(@"
public class Sample
{
    private void FromXaml() { }
    private void NotInMarkup() { }
}", LanguageNames.CSharp, projectFolder: folder);

                result.Should().Contain("FromXaml").And.NotContain("NotInMarkup");
            }
            finally
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
