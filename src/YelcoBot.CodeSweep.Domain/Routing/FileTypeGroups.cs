using System;
using System.Collections.Generic;
using YelcoBot.CodeSweep.Domain.Options;

namespace YelcoBot.CodeSweep.Domain.Routing
{
    /// <summary>
    /// Grupos de tipos de archivo formateados con el editor de VS (lo que Roslyn no soporta).
    /// </summary>
    public static class FileTypeGroups
    {
        public static readonly string[] WebForms = { ".aspx", ".ascx", ".master", ".asax" };

        /// <summary>Web Forms que tienen .designer.cs (.asax no tiene).</summary>
        public static readonly HashSet<string> WebFormsWithDesigner = new(StringComparer.OrdinalIgnoreCase) { ".aspx", ".ascx", ".master" };
        public static readonly string[] Razor = { ".cshtml", ".vbhtml", ".razor" };
        public static readonly string[] Html = { ".html", ".htm" };
        public static readonly string[] XmlConfig = { ".xml", ".config", ".webinfo" };
        public static readonly string[] Xaml = { ".xaml" };
        public static readonly string[] Styles = { ".css", ".less", ".scss" };
        public static readonly string[] Scripts = { ".js", ".ts" };
        public static readonly string[] Json = { ".json" };

        /// <summary>Extensiones de los grupos activos + las adicionales configuradas.</summary>
        public static HashSet<string> GetEditorExtensions(SweepOptions options)
        {
            HashSet<string> extensions = new(StringComparer.OrdinalIgnoreCase);

            if (options.IncludeWebForms) extensions.UnionWith(WebForms);
            if (options.IncludeRazor) extensions.UnionWith(Razor);
            if (options.IncludeHtml) extensions.UnionWith(Html);
            if (options.IncludeXmlConfig) extensions.UnionWith(XmlConfig);
            if (options.IncludeXaml) extensions.UnionWith(Xaml);
            if (options.IncludeStyles) extensions.UnionWith(Styles);
            if (options.IncludeScripts) extensions.UnionWith(Scripts);
            if (options.IncludeJson) extensions.UnionWith(Json);

            extensions.UnionWith(DocumentRouter.ParseExtensions(options.AdditionalFileExtensions));
            return extensions;
        }
    }
}
