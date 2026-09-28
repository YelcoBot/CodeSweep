using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Tests.Rules
{
    public class RemoveUnusedUsingsRuleTests
    {
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
                Project project = workspace.AddProject("TestProject", LanguageNames.CSharp);
                Document document = workspace.AddDocument(project.Id, "TestDoc.cs", SourceText.From(code));

                RemoveUnusedUsingsRule rule = new RemoveUnusedUsingsRule();
                Document updatedDoc = await rule.ApplyAsync(document);
                string updatedText = (await updatedDoc.GetTextAsync()).ToString();

                updatedText.Should().Contain("using System;");
                updatedText.Should().NotContain("using System.Collections.Generic;");
            }
        }
    }
}
