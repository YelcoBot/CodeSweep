using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Tests.Rules
{
    public class SortUsingsRuleTests
    {
        [Fact]
        public async Task ApplyAsync_ShouldPlaceSystemUsingsFirst()
        {
            string code = @"using Newtonsoft.Json;
using System;
using System.IO;

namespace TestNamespace
{
    public class TestClass { }
}";

            using (AdhocWorkspace workspace = new AdhocWorkspace())
            {
                Project project = workspace.AddProject("TestProject", LanguageNames.CSharp);

                // project.AddDocument (no workspace.AddDocument): así el documento queda en el proyecto con sus referencias.
                Document document = project.AddDocument("TestDoc.cs", SourceText.From(code));

                SortUsingsRule rule = new SortUsingsRule();
                Document updatedDoc = await rule.ApplyAsync(document, new SweepOptions());
                string updatedText = (await updatedDoc.GetTextAsync()).ToString();

                int systemIndex = updatedText.IndexOf("using System;");
                int jsonIndex = updatedText.IndexOf("using Newtonsoft.Json;");

                systemIndex.Should().BeLessThan(jsonIndex);
            }
        }
    }
}
