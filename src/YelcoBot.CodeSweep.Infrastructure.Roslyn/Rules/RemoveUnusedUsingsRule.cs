using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using VB = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// Elimina usings (C#, CS8019) e Imports (VB, BC50000 / BC50001) innecesarios,
    /// conservando comentarios y directivas que estén encima (ej. el header del archivo).
    /// </summary>
    public class RemoveUnusedUsingsRule : ISweepRule
    {
        public string Id => "REMOVE_UNUSED_USINGS";

        public int Order => 20;

        public bool IsEnabled(SweepOptions options) => options.EnableRemoveUnusedUsings;

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
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

            List<SyntaxNode> nodesToRemove = document.Project.Language switch
            {
                LanguageNames.CSharp => FindUnusedUsings(syntaxRoot, diagnostics),
                LanguageNames.VisualBasic => FindUnusedImports(syntaxRoot, diagnostics),
                _ => new List<SyntaxNode>()
            };

            if (nodesToRemove.Count == 0)
                return document;

            // Los usings con comentarios/directivas encima conservan esa trivia; el resto se elimina con toda su línea.
            List<SyntaxNode> withMeaningfulTrivia = nodesToRemove.Where(HasMeaningfulLeadingTrivia).ToList();
            List<SyntaxNode> plain = nodesToRemove.Except(withMeaningfulTrivia).ToList();

            SyntaxNode newRoot = syntaxRoot.TrackNodes(nodesToRemove);
            newRoot = newRoot.RemoveNodes(newRoot.GetCurrentNodes(withMeaningfulTrivia), SyntaxRemoveOptions.KeepLeadingTrivia)!;
            newRoot = newRoot.RemoveNodes(newRoot.GetCurrentNodes(plain), SyntaxRemoveOptions.KeepNoTrivia)!;

            return document.WithSyntaxRoot(newRoot);
        }

        private static List<SyntaxNode> FindUnusedUsings(SyntaxNode syntaxRoot, ImmutableArray<Diagnostic> diagnostics)
        {
            return diagnostics
                .Where(d => d.Id == "CS8019")
                .Select(d => syntaxRoot.FindNode(d.Location.SourceSpan).AncestorsAndSelf().OfType<UsingDirectiveSyntax>().FirstOrDefault())
                .Where(u => u != null)
                .Distinct()
                .Cast<SyntaxNode>()
                .ToList();
        }

        /// <summary>
        /// BC50001: todo el Imports sobra. BC50000: sobra una cláusula de "Imports A, B";
        /// si sobran todas sus cláusulas se quita el Imports completo.
        /// </summary>
        private static List<SyntaxNode> FindUnusedImports(SyntaxNode syntaxRoot, ImmutableArray<Diagnostic> diagnostics)
        {
            HashSet<VB.ImportsStatementSyntax> statements = new HashSet<VB.ImportsStatementSyntax>();
            HashSet<VB.ImportsClauseSyntax> clauses = new HashSet<VB.ImportsClauseSyntax>();

            foreach (Diagnostic diagnostic in diagnostics.Where(d => d.Id is "BC50000" or "BC50001"))
            {
                SyntaxNode node = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
                VB.ImportsStatementSyntax? statement = node.AncestorsAndSelf().OfType<VB.ImportsStatementSyntax>().FirstOrDefault();
                if (statement == null)
                    continue;

                if (diagnostic.Id == "BC50001")
                {
                    statements.Add(statement);
                    continue;
                }

                VB.ImportsClauseSyntax? clause = node.AncestorsAndSelf().OfType<VB.ImportsClauseSyntax>().FirstOrDefault();
                if (clause != null)
                {
                    clauses.Add(clause);
                }
            }

            foreach (VB.ImportsStatementSyntax statement in clauses.Select(c => (VB.ImportsStatementSyntax)c.Parent!).Distinct().ToList())
            {
                if (statement.ImportsClauses.All(clauses.Contains))
                {
                    statements.Add(statement);
                }
            }

            clauses.RemoveWhere(c => statements.Contains((VB.ImportsStatementSyntax)c.Parent!));

            return statements.Cast<SyntaxNode>().Concat(clauses).ToList();
        }

        /// <summary>Comentarios o directivas encima (la trivia de espacios y saltos de línea no cuenta).</summary>
        private static bool HasMeaningfulLeadingTrivia(SyntaxNode node)
        {
            return node.GetLeadingTrivia().Any(t => !string.IsNullOrWhiteSpace(t.ToFullString()));
        }
    }
}
