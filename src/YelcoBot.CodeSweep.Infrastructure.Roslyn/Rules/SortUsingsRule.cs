using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    public class SortUsingsRule : ISweepRule
    {
        public string Id => "CS_SORT_USINGS";
        public int Order => 30;

        public bool IsEnabled(SweepOptions options) => options.EnableSortUsings;

        public async Task<Document> ApplyAsync(Document document, CancellationToken cancellationToken = default)
        {
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxRoot is not CompilationUnitSyntax compilationUnit)
                return document;

            SyntaxList<UsingDirectiveSyntax> usings = compilationUnit.Usings;
            if (usings.Count <= 1)
                return document;

            List<UsingDirectiveSyntax> sortedUsings = usings
                .OrderByDescending(u => u.Name?.ToString().StartsWith("System", StringComparison.Ordinal) == true)
                .ThenBy(u => u.Name?.ToString())
                .ToList();

            CompilationUnitSyntax newRoot = compilationUnit.WithUsings(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.List(sortedUsings));
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
