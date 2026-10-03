using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Tests.Rules
{
    public class WhitespaceRuleTests
    {
        [Fact]
        public async Task ApplyAsync_ShouldRemoveMultipleConsecutiveBlankLines()
        {
            string code = "namespace TestNamespace\r\n{\r\n\r\n\r\n\r\n    public class TestClass { }\r\n}";

            using (AdhocWorkspace workspace = new AdhocWorkspace())
            {
                Project project = workspace.AddProject("TestProject", LanguageNames.CSharp);
                Document document = workspace.AddDocument(project.Id, "TestDoc.cs", SourceText.From(code));

                WhitespaceRule rule = new WhitespaceRule();
                Document updatedDoc = await rule.ApplyAsync(document, new SweepOptions());
                string updatedText = (await updatedDoc.GetTextAsync()).ToString();

                updatedText.Should().NotContain("\r\n\r\n\r\n");
            }
        }
    }
}
