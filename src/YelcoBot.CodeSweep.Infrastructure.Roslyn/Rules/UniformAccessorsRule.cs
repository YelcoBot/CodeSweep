using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// 5.2 (C#): los accessors de una propiedad, indexer o evento quedan todos en una línea o todos multilínea.
    /// Si todos tienen como máximo una sentencia, van en una línea ("get { return _edad; }"); si no, todos multilínea.
    /// No toca accessors con comentarios o directivas adentro, ni los de expresión (get => …) o sin cuerpo (get;).
    /// En VB Get / Set siempre son multilínea: no aplica.
    /// </summary>
    public class UniformAccessorsRule : ISweepRule
    {
        public string Id => "UNIFORM_ACCESSORS";

        public int Order => 34;

        public bool IsEnabled(SweepOptions options) => options.EnableUniformAccessors;

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            if (document.Project.Language != LanguageNames.CSharp)
                return document;

            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxRoot == null)
                return document;

            Dictionary<AccessorListSyntax, AccessorListSyntax> replacements = new Dictionary<AccessorListSyntax, AccessorListSyntax>();

            foreach (AccessorListSyntax accessorList in syntaxRoot.DescendantNodes().OfType<AccessorListSyntax>())
            {
                List<AccessorDeclarationSyntax> accessors = accessorList.Accessors.ToList();
                if (accessors.Count < 2 || accessors.Any(a => a.Body == null || HasCommentsOrDirectives(a.Body)))
                    continue;

                List<bool> singleLine = accessors.Select(IsSingleLine).ToList();
                if (singleLine.All(s => s) || singleLine.All(s => !s))
                    continue;

                bool toSingleLine = accessors.All(a => a.Body!.Statements.Count <= 1);
                AccessorListSyntax updated = accessorList.ReplaceNodes(
                    accessors.Where((_, i) => singleLine[i] != toSingleLine),
                    (original, _) => toSingleLine ? ToSingleLine(original) : ToMultiLine(original));

                // Solo multilínea necesita el formateador (sangría); en una línea el formateador volvería a abrir las llaves.
                replacements[accessorList] = toSingleLine ? updated : updated.WithAdditionalAnnotations(Formatter.Annotation);
            }

            if (replacements.Count == 0)
                return document;

            Document updatedDocument = document.WithSyntaxRoot(syntaxRoot.ReplaceNodes(replacements.Keys, (original, _) => replacements[original]));
            return await Formatter.FormatAsync(updatedDocument, Formatter.Annotation, cancellationToken: cancellationToken);
        }

        private static bool IsSingleLine(AccessorDeclarationSyntax accessor)
        {
            return !accessor.ToString().Contains('\n');
        }

        private static bool HasCommentsOrDirectives(BlockSyntax body)
        {
            return body.DescendantTrivia().Any(t => !t.IsKind(SyntaxKind.WhitespaceTrivia) && !t.IsKind(SyntaxKind.EndOfLineTrivia));
        }

        /// <summary>get { return _edad; }</summary>
        private static AccessorDeclarationSyntax ToSingleLine(AccessorDeclarationSyntax accessor)
        {
            BlockSyntax body = accessor.Body!;
            SyntaxTrivia space = SyntaxFactory.Space;

            BlockSyntax singleLine = SyntaxFactory.Block(
                    SyntaxFactory.Token(SyntaxKind.OpenBraceToken).WithTrailingTrivia(space),
                    SyntaxFactory.List(body.Statements.Select(s => s.WithoutLeadingTrivia().WithTrailingTrivia(space))),
                    SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
                .WithTrailingTrivia(body.CloseBraceToken.TrailingTrivia);

            SyntaxToken keyword = accessor.Keyword.WithTrailingTrivia(space);
            return accessor.WithKeyword(keyword).WithBody(singleLine);
        }

        /// <summary>
        /// set
        /// {
        ///     _edad = value;
        /// }
        /// </summary>
        private static AccessorDeclarationSyntax ToMultiLine(AccessorDeclarationSyntax accessor)
        {
            BlockSyntax body = accessor.Body!;
            SyntaxTrivia newLine = SyntaxFactory.ElasticCarriageReturnLineFeed;

            BlockSyntax multiLine = body
                .WithOpenBraceToken(body.OpenBraceToken.WithLeadingTrivia(newLine).WithTrailingTrivia(newLine))
                .WithStatements(SyntaxFactory.List(body.Statements.Select(s => s.WithoutLeadingTrivia().WithTrailingTrivia(newLine))))
                .WithCloseBraceToken(body.CloseBraceToken.WithLeadingTrivia(SyntaxFactory.ElasticMarker));

            return accessor.WithKeyword(accessor.Keyword.WithTrailingTrivia()).WithBody(multiLine);
        }
    }
}
