using Microsoft.CodeAnalysis;
using YelcoBot.CodeSweep.Domain.Options;
using VBSyntax = Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace YelcoBot.CodeSweep.Infrastructure.Roslyn.UnusedMethods
{
    /// <summary>
    /// Decides which methods may be removed when nothing references them.
    /// Fixed exclusions (never configurable, removing them breaks the build or the type contract):
    /// override, abstract, virtual, extern, partial, interface implementations, Main, pattern-based methods
    /// (GetEnumerator, Dispose, Deconstruct, LINQ query methods…) and names used in inactive #if code.
    /// Everything else depends on the accessibility checkboxes and the protections in <see cref="SweepOptions"/>.
    /// </summary>
    public static class UnusedMethodPolicy
    {
        private static readonly HashSet<string> PatternMethodNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "GetEnumerator", "GetAsyncEnumerator", "MoveNext", "MoveNextAsync",
            "GetAwaiter", "GetResult", "OnCompleted", "UnsafeOnCompleted",
            "Deconstruct", "Dispose", "DisposeAsync", "Add", "Slice", "GetPinnableReference",
            "Select", "SelectMany", "Where", "OrderBy", "OrderByDescending", "ThenBy", "ThenByDescending",
            "GroupBy", "Join", "GroupJoin", "Cast", "Finalize"
        };

        private static readonly HashSet<string> InformativeAttributes = new HashSet<string>(StringComparer.Ordinal)
        {
            "ObsoleteAttribute", "DebuggerStepThroughAttribute", "DebuggerHiddenAttribute", "DebuggerNonUserCodeAttribute",
            "SuppressMessageAttribute", "MethodImplAttribute", "PureAttribute", "ExcludeFromCodeCoverageAttribute",
            "DescriptionAttribute", "EditorBrowsableAttribute"
        };

        private static readonly string[] ConventionPrefixes =
        {
            "Page_", "Application_", "Session_", "FormsAuthentication_", "WindowsAuthentication_", "Profile_",
            "OnGet", "OnPost", "OnPut", "OnDelete", "OnPatch", "OnHead", "OnOptions"
        };

        private static readonly HashSet<string> EntryPointBaseTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "Microsoft.AspNetCore.Mvc.ControllerBase",
            "Microsoft.AspNetCore.Mvc.ViewComponent",
            "Microsoft.AspNetCore.Mvc.RazorPages.PageModel",
            "Microsoft.AspNetCore.Components.ComponentBase",
            "Microsoft.AspNetCore.SignalR.Hub",
            "Microsoft.AspNet.SignalR.Hub",
            "System.Web.Mvc.ControllerBase",
            "System.Web.Http.ApiController",
            "System.Web.UI.Page",
            "System.Web.UI.UserControl",
            "System.Web.UI.MasterPage",
            "System.Web.HttpApplication",
            "System.Web.Services.WebService"
        };

        private static readonly HashSet<string> UnityBaseTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "UnityEngine.MonoBehaviour", "UnityEngine.ScriptableObject", "UnityEditor.Editor", "UnityEditor.EditorWindow"
        };

        private static readonly HashSet<string> UnityMessages = new HashSet<string>(StringComparer.Ordinal)
        {
            "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", "Reset"
        };

        /// <summary>Kinds of method that can never be removed, whatever the options.</summary>
        public static bool IsRemovableKind(IMethodSymbol method)
        {
            if (method.MethodKind != MethodKind.Ordinary || method.IsImplicitlyDeclared)
                return false;

            if (method.IsOverride || method.IsAbstract || method.IsVirtual || method.IsExtern)
                return false;

            if (method.IsPartialDefinition || method.PartialDefinitionPart != null || method.PartialImplementationPart != null)
                return false;

            if (!method.ExplicitInterfaceImplementations.IsEmpty)
                return false;

            if (method.ContainingType is not { TypeKind: TypeKind.Class or TypeKind.Struct or TypeKind.Module })
                return false;

            if (method.IsStatic && string.Equals(method.Name, "Main", StringComparison.OrdinalIgnoreCase))
                return false;

            if (PatternMethodNames.Contains(method.Name))
                return false;

            return !ImplementsInterfaceMember(method);
        }

        /// <summary>
        /// Accessibility from the outside, combining the method with every containing type:
        /// a public method of a private nested class is private, protected in a sealed class is private.
        /// </summary>
        public static Accessibility GetEffectiveAccessibility(IMethodSymbol method)
        {
            Accessibility result = Narrow(method.DeclaredAccessibility, method.ContainingType);

            for (INamedTypeSymbol? type = method.ContainingType; type != null; type = type.ContainingType)
            {
                Accessibility typeAccessibility = type.ContainingType == null ? type.DeclaredAccessibility : Narrow(type.DeclaredAccessibility, type.ContainingType);
                result = Intersect(result, typeAccessibility);
            }

            return result;
        }

        public static bool IsAccessibilityIncluded(Accessibility accessibility, SweepOptions options, bool hasExternalFriends)
        {
            return accessibility switch
            {
                Accessibility.Private => options.UnusedMethodsIncludePrivate,
                Accessibility.ProtectedAndInternal => options.UnusedMethodsIncludePrivateProtected,
                Accessibility.Internal => options.UnusedMethodsIncludeInternal && !hasExternalFriends,
                Accessibility.Protected => options.UnusedMethodsIncludeProtected,
                Accessibility.ProtectedOrInternal => options.UnusedMethodsIncludeProtectedInternal && !hasExternalFriends,
                Accessibility.Public => options.UnusedMethodsIncludePublic,
                _ => false
            };
        }

        /// <summary>The configurable protections (event handlers, conventions, frameworks, attributes, strings, markup).</summary>
        public static bool IsProtected(IMethodSymbol method, SweepOptions options, SolutionNames solutionNames, ISet<string> markupNames)
        {
            if (solutionNames.DisabledCodeNames.Contains(method.Name))
                return true;

            if (options.UnusedMethodsProtectEventHandlers && IsEventHandler(method))
                return true;

            if (options.UnusedMethodsProtectNamingConventions && FollowsNamingConvention(method))
                return true;

            if (options.UnusedMethodsProtectFrameworkEntryPoints && IsFrameworkEntryPoint(method))
                return true;

            if (options.UnusedMethodsProtectAttributed && HasMeaningfulAttribute(method))
                return true;

            if (options.UnusedMethodsProtectStringReferences && solutionNames.StringNames.Contains(method.Name))
                return true;

            return options.UnusedMethodsProtectMarkupReferences && markupNames.Contains(method.Name);
        }

        /// <summary>An InternalsVisibleTo pointing to an assembly that is not in the solution: its references cannot be searched.</summary>
        public static bool HasExternalFriends(IAssemblySymbol assembly, Solution solution)
        {
            HashSet<string> solutionAssemblies = new HashSet<string>(solution.Projects.Select(p => p.AssemblyName), StringComparer.OrdinalIgnoreCase);

            foreach (AttributeData attribute in assembly.GetAttributes())
            {
                if (attribute.AttributeClass?.Name != "InternalsVisibleToAttribute" || attribute.ConstructorArguments.Length == 0)
                    continue;

                string friend = (attribute.ConstructorArguments[0].Value as string ?? string.Empty).Split(',')[0].Trim();
                if (friend.Length > 0 && !solutionAssemblies.Contains(friend))
                    return true;
            }

            return false;
        }

        private static bool ImplementsInterfaceMember(IMethodSymbol method)
        {
            INamedTypeSymbol type = method.ContainingType;
            foreach (INamedTypeSymbol @interface in type.AllInterfaces)
            {
                foreach (IMethodSymbol member in @interface.GetMembers(method.Name).OfType<IMethodSymbol>())
                {
                    if (SymbolEqualityComparer.Default.Equals(type.FindImplementationForInterfaceMember(member), method))
                        return true;
                }
            }

            return false;
        }

        private static bool IsEventHandler(IMethodSymbol method)
        {
            if (method.Parameters.Length == 2
                && method.Parameters[0].Type.SpecialType == SpecialType.System_Object
                && IsEventArgs(method.Parameters[1].Type))
                return true;

            return method.DeclaringSyntaxReferences
                .Select(r => r.GetSyntax())
                .Select(node => node is VBSyntax.MethodBlockSyntax block ? block.SubOrFunctionStatement : node)
                .OfType<VBSyntax.MethodStatementSyntax>()
                .Any(statement => statement.HandlesClause != null);
        }

        private static bool IsEventArgs(ITypeSymbol type)
        {
            for (ITypeSymbol? current = type; current != null; current = current.BaseType)
            {
                if (current.Name.EndsWith("EventArgs", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static bool FollowsNamingConvention(IMethodSymbol method)
        {
            if (ConventionPrefixes.Any(prefix => method.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                return true;

            string? property = method.Name.StartsWith("ShouldSerialize", StringComparison.OrdinalIgnoreCase) ? method.Name.Substring("ShouldSerialize".Length)
                : method.Name.StartsWith("Reset", StringComparison.OrdinalIgnoreCase) ? method.Name.Substring("Reset".Length)
                : null;

            return !string.IsNullOrEmpty(property) && HasProperty(method.ContainingType, property!);
        }

        private static bool HasProperty(INamedTypeSymbol type, string name)
        {
            for (INamedTypeSymbol? current = type; current != null; current = current.BaseType)
            {
                if (current.GetMembers().OfType<IPropertySymbol>().Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        private static bool IsFrameworkEntryPoint(IMethodSymbol method)
        {
            INamedTypeSymbol type = method.ContainingType;

            if (DerivesFrom(type, UnityBaseTypes))
                return UnityMessages.Contains(method.Name) || method.Name.StartsWith("On", StringComparison.Ordinal);

            if (method.DeclaredAccessibility == Accessibility.Private)
                return false;

            if (DerivesFrom(type, EntryPointBaseTypes))
                return true;

            return method.DeclaredAccessibility == Accessibility.Public
                && type.DeclaredAccessibility == Accessibility.Public
                && type.Name.EndsWith("Controller", StringComparison.Ordinal);
        }

        private static bool DerivesFrom(INamedTypeSymbol type, HashSet<string> baseTypes)
        {
            for (INamedTypeSymbol? current = type.BaseType; current != null; current = current.BaseType)
            {
                if (baseTypes.Contains(current.ContainingNamespace.ToDisplayString() + "." + current.Name))
                    return true;
            }

            return false;
        }

        private static bool HasMeaningfulAttribute(IMethodSymbol method)
        {
            return method.GetAttributes().Any(a => a.AttributeClass == null || !InformativeAttributes.Contains(a.AttributeClass.Name));
        }

        private static Accessibility Narrow(Accessibility accessibility, INamedTypeSymbol? containingType)
        {
            if (containingType == null || !(containingType.IsSealed || containingType.IsStatic || containingType.TypeKind is TypeKind.Struct or TypeKind.Module))
                return accessibility;

            return accessibility switch
            {
                Accessibility.Protected => Accessibility.Private,
                Accessibility.ProtectedAndInternal => Accessibility.Private,
                Accessibility.ProtectedOrInternal => Accessibility.Internal,
                _ => accessibility
            };
        }

        [Flags]
        private enum Audience
        {
            None = 0,
            DerivedInAssembly = 1,
            DerivedOutside = 2,
            Assembly = 4,
            Outside = 8
        }

        private static Accessibility Intersect(Accessibility left, Accessibility right)
        {
            return ToAccessibility(ToAudience(left) & ToAudience(right));
        }

        private static Audience ToAudience(Accessibility accessibility)
        {
            return accessibility switch
            {
                Accessibility.ProtectedAndInternal => Audience.DerivedInAssembly,
                Accessibility.Protected => Audience.DerivedInAssembly | Audience.DerivedOutside,
                Accessibility.Internal => Audience.DerivedInAssembly | Audience.Assembly,
                Accessibility.ProtectedOrInternal => Audience.DerivedInAssembly | Audience.DerivedOutside | Audience.Assembly,
                Accessibility.Public => Audience.DerivedInAssembly | Audience.DerivedOutside | Audience.Assembly | Audience.Outside,
                _ => Audience.None
            };
        }

        private static Accessibility ToAccessibility(Audience audience)
        {
            if (audience.HasFlag(Audience.Outside))
                return Accessibility.Public;

            if (audience.HasFlag(Audience.Assembly))
                return audience.HasFlag(Audience.DerivedOutside) ? Accessibility.ProtectedOrInternal : Accessibility.Internal;

            if (audience.HasFlag(Audience.DerivedOutside))
                return Accessibility.Protected;

            return audience.HasFlag(Audience.DerivedInAssembly) ? Accessibility.ProtectedAndInternal : Accessibility.Private;
        }
    }
}
