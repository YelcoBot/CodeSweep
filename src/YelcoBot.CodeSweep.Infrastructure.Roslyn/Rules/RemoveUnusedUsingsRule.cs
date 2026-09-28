using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Elimina usings innecesarios (CS8019), conservando comentarios y directivas que estén encima (ej. el header del archivo).
    /// </summary>
    public class RemoveUnusedUsingsRule : ISweepRule
    {
        public string Id => "CS_REMOVE_UNUSED_USINGS";
        public int Order => 20;

        public bool IsEnabled(SweepOptions options) => options.EnableRemoveUnusedUsings;

        public async Task<Document> ApplyAsync(Document document, CancellationToken cancellationToken = default)
        {
            if (document.Project.Language != LanguageNames.CSharp)
                return document;

            // Sin parsear los comentarios XML, un using usado solo en <see cref="..."/> se vería como innecesario.
            if (document.Project.ParseOptions?.DocumentationMode == DocumentationMode.None)
                return document;

            SemanticModel? semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);

            if (semanticModel == null || syntaxRoot == null)
                return document;

            ImmutableArray<Diagnostic> diagnostics = semanticModel.GetDiagnostics(cancellationToken: cancellationToken);

            // Con errores de compilación un using necesario puede parecer innecesario: no tocar nada.
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
                return document;

            List<UsingDirectiveSyntax> usingsToRemove = diagnostics
                .Where(d => d.Id == "CS8019")
                .Select(d => syntaxRoot.FindNode(d.Location.SourceSpan).AncestorsAndSelf().OfType<UsingDirectiveSyntax>().FirstOrDefault())
                .Where(u => u != null)
                .Select(u => u!)
                .Distinct()
                .ToList();

            if (usingsToRemove.Count == 0)
                return document;

            // Los usings con comentarios/directivas encima conservan esa trivia; el resto se elimina con toda su línea.
            List<UsingDirectiveSyntax> withMeaningfulTrivia = usingsToRemove.Where(HasMeaningfulLeadingTrivia).ToList();
            List<UsingDirectiveSyntax> plain = usingsToRemove.Except(withMeaningfulTrivia).ToList();

            SyntaxNode newRoot = syntaxRoot.TrackNodes(usingsToRemove);
            newRoot = newRoot.RemoveNodes(newRoot.GetCurrentNodes(withMeaningfulTrivia), SyntaxRemoveOptions.KeepLeadingTrivia)!;
            newRoot = newRoot.RemoveNodes(newRoot.GetCurrentNodes(plain), SyntaxRemoveOptions.KeepNoTrivia)!;

            return document.WithSyntaxRoot(newRoot);
        }

        private static bool HasMeaningfulLeadingTrivia(UsingDirectiveSyntax usingDirective)
        {
            return usingDirective.GetLeadingTrivia().Any(t =>
                !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia));
        }
    }
}
