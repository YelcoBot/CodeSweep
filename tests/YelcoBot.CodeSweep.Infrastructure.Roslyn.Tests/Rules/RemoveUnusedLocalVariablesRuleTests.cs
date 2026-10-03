using System;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Tests.Rules
{
    public class RemoveUnusedLocalVariablesRuleTests
    {
        private static MetadataReference[] GetMetadataReferences()
        {
            string coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            return new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll"))
            };
        }

        [Fact]
        public async Task ApplyAsync_ShouldRemoveUnusedLocalVariable()
        {
            string code = @"
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            int unusedVar = 10;
        }
    }
}";

            using (AdhocWorkspace workspace = new AdhocWorkspace())
            {
                Project project = workspace.AddProject("TestProject", LanguageNames.CSharp)
                    .WithMetadataReferences(GetMetadataReferences());

                Document document = workspace.AddDocument(project.Id, "TestDoc.cs", SourceText.From(code));

                RemoveUnusedLocalVariablesRule rule = new RemoveUnusedLocalVariablesRule();
                SweepOptions options = new SweepOptions();

                Document updatedDoc = await rule.ApplyAsync(document, new SweepOptions());
                string updatedText = (await updatedDoc.GetTextAsync()).ToString();

                updatedText.Should().NotContain("int unusedVar = 10;");
            }
        }
    }
}
