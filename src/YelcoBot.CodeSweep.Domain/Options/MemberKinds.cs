using System;
using System.Collections.Generic;
using System.Linq;

namespace YelcoBot.CodeSweep.Domain.Options
{
    /// <summary>Tipos de miembro para las reglas 3.1 (línea en blanco entre miembros) y 5.1 (reorganizar).</summary>
    [Flags]
    public enum MemberKind
    {
        None = 0,
        Fields = 1,
        Constructors = 2,
        Properties = 4,
        Methods = 8,
        Events = 16,
        Enums = 32,
        Types = 64
    }

    public static class MemberKinds
    {
        public const string DefaultBlankLineKinds = "Methods;Properties;Constructors;Types;Enums;Events";
        public const string DefaultOrder = "Fields;Constructors;Properties;Events;Methods;Enums;Types";
        public const string DefaultAccessOrder = "public;internal;protected internal;protected;private protected;private";

        /// <summary>"Methods;Properties" → Methods | Properties. Se ignoran los nombres desconocidos.</summary>
        public static MemberKind Parse(string? value)
        {
            MemberKind result = MemberKind.None;
            foreach (string name in Split(value))
            {
                if (Enum.TryParse(name, ignoreCase: true, out MemberKind kind))
                {
                    result |= kind;
                }
            }

            return result;
        }

        /// <summary>Lista ordenada; los tipos que falten van al final en el orden por defecto.</summary>
        public static IReadOnlyList<MemberKind> ParseOrder(string? value)
        {
            List<MemberKind> order = new List<MemberKind>();
            foreach (string name in Split(value).Concat(Split(DefaultOrder)))
            {
                if (Enum.TryParse(name, ignoreCase: true, out MemberKind kind) && kind != MemberKind.None && !order.Contains(kind))
                {
                    order.Add(kind);
                }
            }

            return order;
        }

        /// <summary>Accesos en minúscula ("protected internal"); los que falten van al final en el orden por defecto.</summary>
        public static IReadOnlyList<string> ParseAccessOrder(string? value)
        {
            List<string> order = new List<string>();
            foreach (string name in Split(value).Concat(Split(DefaultAccessOrder)))
            {
                string normalized = string.Join(" ", name.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                if (!order.Contains(normalized))
                {
                    order.Add(normalized);
                }
            }

            return order;
        }

        private static IEnumerable<string> Split(string? value)
        {
            return (value ?? string.Empty)
                .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0);
        }
    }
}
