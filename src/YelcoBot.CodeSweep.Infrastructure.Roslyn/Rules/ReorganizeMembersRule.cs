using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using YelcoBot.CodeSweep.Domain.Options;
using YelcoBot.CodeSweep.Infrastructure.Roslyn.Abstractions;
using CS = Microsoft.CodeAnalysis.CSharp.Syntax;
using CSharpKind = Microsoft.CodeAnalysis.CSharp.SyntaxKind;
using VB = Microsoft.CodeAnalysis.VisualBasic.Syntax;
using VBKind = Microsoft.CodeAnalysis.VisualBasic.SyntaxKind;
using VBSyntaxFactory = Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.Rules
{
    /// <summary>
    /// 5.1: ordena los miembros de cada clase / módulo / interfaz por tipo (campos, constructores, propiedades…)
    /// y dentro de cada tipo por acceso (public, internal, protected, private). Orden configurable.
    /// Para no cambiar el comportamiento:
    /// - Los campos conservan su orden relativo (el orden de inicialización importa).
    /// - No toca structs ni tipos con [StructLayout] (el orden de los campos es el layout en memoria).
    /// - No toca tipos con directivas (#region, #if) entre sus miembros.
    /// - Dentro del mismo grupo se conserva el orden original.
    /// </summary>
    public class ReorganizeMembersRule : ISweepRule
    {
        public string Id => "REORGANIZE_MEMBERS";
        public int Order => 33;

        public bool IsEnabled(SweepOptions options) => options.EnableReorganizeMembers;

        public async Task<Document> ApplyAsync(Document document, SweepOptions options, CancellationToken cancellationToken = default)
        {
            SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken);
            if (syntaxRoot == null)
                return document;

            IReadOnlyList<MemberKind> kindOrder = MemberKinds.ParseOrder(options.MemberKindOrder);
            IReadOnlyList<string> accessOrder = MemberKinds.ParseAccessOrder(options.AccessOrder);

            SyntaxNode? newRoot = syntaxRoot.Language switch
            {
                LanguageNames.CSharp => ReorganizeCSharp(syntaxRoot, kindOrder, accessOrder),
                LanguageNames.VisualBasic => ReorganizeVisualBasic(syntaxRoot, kindOrder, accessOrder),
                _ => null
            };

            return newRoot == null || newRoot == syntaxRoot ? document : document.WithSyntaxRoot(newRoot);
        }

        private static SyntaxNode ReorganizeCSharp(SyntaxNode root, IReadOnlyList<MemberKind> kindOrder, IReadOnlyList<string> accessOrder)
        {
            IEnumerable<CS.TypeDeclarationSyntax> types = root.DescendantNodes()
                .OfType<CS.TypeDeclarationSyntax>()
                .Where(t => t is CS.ClassDeclarationSyntax or CS.InterfaceDeclarationSyntax || (t is CS.RecordDeclarationSyntax r && !r.ClassOrStructKeyword.IsKind(CSharpKind.StructKeyword)))
                .Where(t => !HasStructLayout(t.AttributeLists.SelectMany(l => l.Attributes).Select(a => a.Name.ToString())));

            return root.ReplaceNodes(types, (original, rewritten) =>
            {
                bool isInterface = rewritten is CS.InterfaceDeclarationSyntax;
                List<CS.MemberDeclarationSyntax>? sorted = Sort(
                    rewritten.Members,
                    m => GetCSharpKind(m),
                    m => GetCSharpAccess(m, isInterface),
                    kindOrder,
                    accessOrder,
                    rewritten.CloseBraceToken.LeadingTrivia);

                return sorted == null ? rewritten : rewritten.WithMembers(Microsoft.CodeAnalysis.CSharp.SyntaxFactory.List(sorted));
            });
        }

        private static SyntaxNode ReorganizeVisualBasic(SyntaxNode root, IReadOnlyList<MemberKind> kindOrder, IReadOnlyList<string> accessOrder)
        {
            IEnumerable<VB.TypeBlockSyntax> types = root.DescendantNodes()
                .OfType<VB.TypeBlockSyntax>()
                .Where(t => t is not VB.StructureBlockSyntax)
                .Where(t => !HasStructLayout(t.BlockStatement.AttributeLists.SelectMany(l => l.Attributes).Select(a => a.Name.ToString())));

            return root.ReplaceNodes(types, (original, rewritten) =>
            {
                bool isInterface = rewritten is VB.InterfaceBlockSyntax;
                List<VB.StatementSyntax>? sorted = Sort(
                    rewritten.Members,
                    m => GetVisualBasicKind(m),
                    m => GetVisualBasicAccess(m, isInterface),
                    kindOrder,
                    accessOrder,
                    rewritten.EndBlockStatement.GetLeadingTrivia());

                return sorted == null ? rewritten : rewritten.WithMembers(VBSyntaxFactory.List(sorted));
            });
        }

        /// <summary>Devuelve los miembros ordenados, o null si no hay nada que cambiar o no es seguro.</summary>
        private static List<TMember>? Sort<TMember>(
            SyntaxList<TMember> members,
            System.Func<TMember, MemberKind> getKind,
            System.Func<TMember, string> getAccess,
            IReadOnlyList<MemberKind> kindOrder,
            IReadOnlyList<string> accessOrder,
            SyntaxTriviaList closingTrivia) where TMember : SyntaxNode
        {
            if (members.Count < 2
                || members.Any(m => getKind(m) == MemberKind.None || m.GetLeadingTrivia().Any(t => t.IsDirective))
                || closingTrivia.Any(t => t.IsDirective))
                return null;

            List<TMember> original = members.ToList();
            List<TMember> sorted = original
                .Select((member, index) => (member, index))
                .OrderBy(x => IndexOf(kindOrder, getKind(x.member)))
                .ThenBy(x => getKind(x.member) == MemberKind.Fields ? 0 : IndexOf(accessOrder, getAccess(x.member)))
                .ThenBy(x => x.index)
                .Select(x => x.member)
                .ToList();

            if (sorted.SequenceEqual(original))
                return null;

            // El primer miembro no lleva línea en blanco arriba; los demás sí (3.1 / 1.1 ajustan después).
            SyntaxTriviaList firstLeading = original[0].GetLeadingTrivia();
            for (int i = 0; i < sorted.Count; i++)
            {
                if (sorted[i] == original[0] && i != 0)
                {
                    sorted[i] = sorted[i].WithLeadingTrivia(WithBlankLine(firstLeading, original[0]));
                }
            }

            if (sorted[0] != original[0])
            {
                sorted[0] = sorted[0].WithLeadingTrivia(WithoutLeadingBlankLines(sorted[0].GetLeadingTrivia()));
            }

            return sorted;
        }

        /// <summary>Agrega una línea en blanco al principio si no la tiene (copiando el salto de línea de otro miembro).</summary>
        private static SyntaxTriviaList WithBlankLine<TMember>(SyntaxTriviaList trivia, TMember sample) where TMember : SyntaxNode
        {
            SyntaxTrivia? endOfLine = sample.GetTrailingTrivia().LastOrDefault(t => IsEndOfLine(t));
            if (endOfLine == null || (trivia.Count > 0 && IsEndOfLine(trivia[0])))
                return trivia;

            return trivia.Insert(0, endOfLine.Value);
        }

        private static SyntaxTriviaList WithoutLeadingBlankLines(SyntaxTriviaList trivia)
        {
            int skip = 0;
            while (skip < trivia.Count && (IsEndOfLine(trivia[skip]) || (string.IsNullOrWhiteSpace(trivia[skip].ToFullString()) && skip + 1 < trivia.Count && IsEndOfLine(trivia[skip + 1]))))
            {
                skip++;
            }

            return new SyntaxTriviaList(trivia.Skip(skip));
        }

        private static bool IsEndOfLine(SyntaxTrivia trivia)
        {
            string text = trivia.ToFullString();
            return text == "\n" || text == "\r\n" || text == "\r";
        }

        private static int IndexOf<T>(IReadOnlyList<T> list, T value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (EqualityComparer<T>.Default.Equals(list[i], value))
                    return i;
            }

            return list.Count;
        }

        private static bool HasStructLayout(IEnumerable<string> attributeNames)
        {
            return attributeNames.Any(n => n.EndsWith("StructLayout") || n.EndsWith("StructLayoutAttribute"));
        }

        private static MemberKind GetCSharpKind(CS.MemberDeclarationSyntax member) => member switch
        {
            CS.FieldDeclarationSyntax => MemberKind.Fields,
            CS.ConstructorDeclarationSyntax or CS.DestructorDeclarationSyntax => MemberKind.Constructors,
            CS.PropertyDeclarationSyntax or CS.IndexerDeclarationSyntax => MemberKind.Properties,
            CS.MethodDeclarationSyntax or CS.OperatorDeclarationSyntax or CS.ConversionOperatorDeclarationSyntax => MemberKind.Methods,
            CS.EventDeclarationSyntax or CS.EventFieldDeclarationSyntax => MemberKind.Events,
            CS.EnumDeclarationSyntax => MemberKind.Enums,
            CS.BaseTypeDeclarationSyntax or CS.DelegateDeclarationSyntax => MemberKind.Types,
            _ => MemberKind.None
        };

        private static MemberKind GetVisualBasicKind(VB.StatementSyntax member) => member switch
        {
            VB.FieldDeclarationSyntax => MemberKind.Fields,
            VB.ConstructorBlockSyntax => MemberKind.Constructors,
            VB.PropertyBlockSyntax or VB.PropertyStatementSyntax => MemberKind.Properties,
            VB.MethodBlockSyntax or VB.OperatorBlockSyntax or VB.MethodStatementSyntax or VB.DeclareStatementSyntax => MemberKind.Methods,
            VB.EventBlockSyntax or VB.EventStatementSyntax => MemberKind.Events,
            VB.EnumBlockSyntax => MemberKind.Enums,
            VB.TypeBlockSyntax or VB.DelegateStatementSyntax => MemberKind.Types,
            _ => MemberKind.None
        };

        private static string GetCSharpAccess(CS.MemberDeclarationSyntax member, bool isInterface)
        {
            List<string> modifiers = member.Modifiers
                .Select(m => (CSharpKind)m.RawKind)
                .Where(k => k is CSharpKind.PublicKeyword or CSharpKind.InternalKeyword or CSharpKind.ProtectedKeyword or CSharpKind.PrivateKeyword)
                .Select(k => k switch
                {
                    CSharpKind.PublicKeyword => "public",
                    CSharpKind.InternalKeyword => "internal",
                    CSharpKind.ProtectedKeyword => "protected",
                    _ => "private"
                })
                .ToList();

            if (modifiers.Count == 0)
                return isInterface ? "public" : "private";

            return NormalizeAccess(modifiers);
        }

        private static string GetVisualBasicAccess(VB.StatementSyntax member, bool isInterface)
        {
            SyntaxTokenList modifiers = member switch
            {
                VB.FieldDeclarationSyntax field => field.Modifiers,
                VB.MethodBlockBaseSyntax method => method.BlockStatement.Modifiers,
                VB.PropertyBlockSyntax property => property.PropertyStatement.Modifiers,
                VB.EventBlockSyntax evt => evt.EventStatement.Modifiers,
                VB.TypeBlockSyntax type => type.BlockStatement.Modifiers,
                VB.EnumBlockSyntax enumBlock => enumBlock.EnumStatement.Modifiers,
                VB.DeclarationStatementSyntax declaration => GetModifiers(declaration),
                _ => default
            };

            List<string> access = modifiers
                .Select(m => (VBKind)m.RawKind)
                .Where(k => k is VBKind.PublicKeyword or VBKind.FriendKeyword or VBKind.ProtectedKeyword or VBKind.PrivateKeyword)
                .Select(k => k switch
                {
                    VBKind.PublicKeyword => "public",
                    VBKind.FriendKeyword => "internal",
                    VBKind.ProtectedKeyword => "protected",
                    _ => "private"
                })
                .ToList();

            if (access.Count == 0)
                return isInterface || member is not VB.FieldDeclarationSyntax ? "public" : "private";

            return NormalizeAccess(access);
        }

        private static SyntaxTokenList GetModifiers(VB.DeclarationStatementSyntax declaration) => declaration switch
        {
            VB.MethodBaseSyntax method => method.Modifiers,
            _ => default
        };

        /// <summary>"protected" + "internal" (en cualquier orden) → "protected internal"; "private" + "protected" → "private protected".</summary>
        private static string NormalizeAccess(List<string> modifiers)
        {
            if (modifiers.Contains("protected") && modifiers.Contains("internal"))
                return "protected internal";

            if (modifiers.Contains("private") && modifiers.Contains("protected"))
                return "private protected";

            return modifiers[0];
        }
    }
}
