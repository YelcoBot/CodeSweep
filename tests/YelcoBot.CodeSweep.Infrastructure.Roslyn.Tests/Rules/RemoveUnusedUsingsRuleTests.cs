using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Tests.Rules
{
    public class RemoveUnusedUsingsRuleTests
    {
        private static MetadataReference[] GetMetadataReferences()
        {
            string coreDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            return new MetadataReference[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Path.Combine(coreDir, "System.Runtime.dll")),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            };
        }

        [Fact]
        public async Task ApplyAsync_ShouldRemoveUnusedUsingDirectives()
        {
            string code = @"using System;
using System.Collections.Generic;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            Console.WriteLine(""Hello"");
        }
    }
}";

            using (AdhocWorkspace workspace = new AdhocWorkspace())
            {
                Project project = workspace.AddProject("TestProject", LanguageNames.CSharp)
                    .WithMetadataReferences(GetMetadataReferences());

                // project.AddDocument (no workspace.AddDocument): así el documento queda en el proyecto con sus referencias.
                Document document = project.AddDocument("TestDoc.cs", SourceText.From(code));

                RemoveUnusedUsingsRule rule = new RemoveUnusedUsingsRule();
                Document updatedDoc = await rule.ApplyAsync(document, new SweepOptions());
                string updatedText = (await updatedDoc.GetTextAsync()).ToString();

                updatedText.Should().Contain("using System;");
                updatedText.Should().NotContain("using System.Collections.Generic;");
            }
        }
    }
}
