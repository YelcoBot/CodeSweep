# CodeSweep — Reglas de limpieza

Reglas que hace CodeSweep, incluidas las que **no** hacen ni *Format Document* (`Ctrl+K, Ctrl+D`) ni *Code Cleanup* de VS (la mayoría vienen de CodeMaid).

**Estado:** ✅ hecho · ⚠️ parcial · ❌ pendiente
**Por defecto:** 🟢 activa · ⚪ apagada

## Cómo se configuran

- **Todas las reglas son parametrizables** en *Tools → Options → CodeSweep*: cada una tiene su casilla para activarla o apagarla, y algunas tienen parámetros (por ejemplo, el máximo de líneas en blanco).
- Las secciones de las opciones siguen la misma agrupación de este documento:
  - **A. Roslyn (C# y VB)**: limpieza de código.
  - **B. Universales**: cualquier archivo.
  - **C. Por lenguaje o tipo**: C# y VB, y markup.
  - **D. SQL (T-SQL)**: formato con ScriptDOM, en Visual Studio y en SSMS.
- Los grupos de **File Types** (C#, VB, Web Forms, Razor, HTML, XML/Config, XAML, Styles, Scripts, JSON, SQL) siguen decidiendo **qué archivos** entran al cleanup. Las reglas deciden **qué se hace** con ellos.
- **En SSMS** (22.7+) solo se limpia SQL: Tools → Options oculta los demás lenguajes. Las reglas universales (B) también aplican a los `.sql`.
- *(Opcional, más adelante)* las reglas de C#, VB y universales también en `.editorconfig` con claves `codesweep_*`, para fijarlas por repositorio. El formato SQL ya se puede fijar por repositorio con las claves de SSMS (ver D).

---

## A. Roslyn — C# y VB

Todas aplican **igual a C# y a VB**. Corren con Roslyn en segundo plano y en paralelo, sin abrir archivos.

| Regla | Por defecto | C# | VB | VS nativo |
|---|---|---|---|---|
| Formatear documento (respeta `.editorconfig`) | 🟢 | ✅ | ✅ | *Format Document* |
| Quitar `using` / `Imports` sin usar | 🟢 | ✅ | ✅ | *Code Cleanup* |
| Ordenar `using` / `Imports` (System primero) | 🟢 | ✅ | ✅ | *Code Cleanup* |
| Quitar variables locales sin usar | 🟢 | ✅ | ✅ | *Code Cleanup* (parcial, `IDE0059`) |

> VB: `Imports` sin usar = BC50000 / BC50001; variables sin usar = BC42024 / BC42099. Una variable con valor calculado (`Dim y = Calcular()`) no se quita, porque se perdería la llamada.

Ejemplo en VB (quitar y ordenar `Imports`, quitar variable sin usar).

Antes:
```vb
Imports System.Text
Imports System.Collections.Generic
Imports System

Public Sub Procesar()
    Dim temporal As Integer
    Console.WriteLine("Listo")
End Sub
```
Después:
```vb
Imports System

Public Sub Procesar()
    Console.WriteLine("Listo")
End Sub
```

---

## B. Universales — cualquier archivo

Aplican a **todos** los tipos de archivo (C#, VB, aspx, ascx, master, asax, cshtml, razor, html, xml, config, webinfo, xaml, css, js, ts, json…) y **en las tres vías**:
- **Roslyn**, en segundo plano.
- **Editor invisible**, después de *Format Document* y antes de guardar.
- **Editor visible**, después de *Format Document* y antes de guardar.

Ninguna toca el contenido de strings multilínea, template strings (`` `…` ``), código deshabilitado (`#if false`) ni bloques `<pre>`, `<textarea>`, `<script>` o CDATA.

| # | Regla | Por defecto | Parámetros | Estado | VS nativo |
|---|---|---|---|---|---|
| 1.1 | Quitar líneas en blanco consecutivas | 🟢 | Máximo de líneas en blanco seguidas (**1**) | ✅ | No (`IDE2000` experimental, solo C#/VB) |
| 1.2 | Quitar líneas en blanco al inicio del archivo | 🟢 | — | ✅ | No |
| 1.3 | Quitar líneas en blanco al final del archivo | 🟢 | — | ✅ | No |
| 2.1 | Quitar espacios al final de línea | 🟢 | — | ✅ | Parcial (`trim_trailing_whitespace`) |
| 2.2 | Exactamente un salto de línea al final del archivo | 🟢 | — | ✅ | Parcial (`insert_final_newline` solo agrega) |

### 1.1 Quitar líneas en blanco consecutivas

Antes:
```csharp
int a = 1;



int b = 2;
```
Después:
```csharp
int a = 1;

int b = 2;
```

### 1.2 Quitar líneas en blanco al inicio del archivo

Antes:
```xml


<configuration>
```
Después:
```xml
<configuration>
```

### 1.3 Quitar líneas en blanco al final del archivo

Antes:
```vb
    End Sub
End Class



```
Después:
```vb
    End Sub
End Class
```

### 2.1 Quitar espacios al final de línea

En el ejemplo, `·` representa un espacio.

Antes:
```html
<div class="panel">···
    <span>Total</span>··
</div>
```
Después:
```html
<div class="panel">
    <span>Total</span>
</div>
```

### 2.2 Exactamente un salto de línea al final del archivo

`⏎` representa un salto de línea.

Antes (sin salto final, o con varios):
```css
}⏎⏎⏎
```
Después:
```css
}⏎
```

---

## C. Por lenguaje o tipo

### C.1 C# y VB (Roslyn)

| # | Regla | Por defecto | Parámetros | Estado |
|---|---|---|---|---|
| 1.4 | Quitar línea en blanco después de abrir un bloque | 🟢 | — | ✅ |
| 1.5 | Quitar línea en blanco antes de cerrar un bloque | 🟢 | — | ✅ |
| 1.6 | Quitar líneas en blanco después de atributos | 🟢 | — | ✅ |
| 1.7 | Quitar líneas en blanco entre llamadas encadenadas | 🟢 | — | ✅ |
| 3.1 | Línea en blanco entre miembros | 🟢 | Tipos de miembro (métodos, propiedades, constructores, clases, enums, eventos) | ✅ |
| 3.2 | Línea en blanco alrededor de regiones | ⚪ | — | ✅ |
| 3.3 | Línea en blanco antes de cada `case` / `Case` | ⚪ | — | ✅ |
| 3.4 | Línea en blanco antes de comentarios de una línea | 🟢 | — | ✅ |
| 3.5 | Línea en blanco después de `using` / `Imports` | 🟢 | — | ✅ |
| 4.1 | Quitar todas las regiones | ⚪ | — | ✅ |
| 4.2 | Actualizar el texto de `#endregion` / `#End Region` | ⚪ | — | ✅ |
| 4.3 | Quitar regiones vacías | 🟢 | — | ✅ |
| 5.1 | Reorganizar miembros por tipo y acceso | ⚪ | Orden de tipos y de accesos | ✅ |
| 5.2 | Accessors todos en una línea o todos multilínea | ⚪ | — | ✅ |
| 6.1 | Reajustar comentarios al ancho de línea | ⚪ | Ancho (**120**) | ✅ |
| 6.2 | Espacio después de `//` / `'` | ⚪ | — | ✅ |

Notas por lenguaje:
- **1.6** en VB: una línea en blanco después de un atributo es error de compilación, así que en la práctica solo se ve en C#.
- **1.7** solo C#: en VB las líneas que empiezan con `.` son de bloques `With` y separarlas es válido.
- **5.1** no reordena los campos entre sí (su orden de inicialización importa), ni toca structs, tipos con `[StructLayout]` o tipos con directivas (`#region`, `#if`) entre sus miembros.
- **5.2** solo C#: en VB `Get` / `Set` siempre son multilínea.
- **6.1** solo parte líneas largas; nunca une líneas cortas.

#### 1.4 Quitar línea en blanco después de abrir un bloque

Antes:
```csharp
public void Guardar()
{

    Validar();
}
```
```vb
Public Sub Guardar()

    Validar()
End Sub
```
Después:
```csharp
public void Guardar()
{
    Validar();
}
```
```vb
Public Sub Guardar()
    Validar()
End Sub
```

#### 1.5 Quitar línea en blanco antes de cerrar un bloque

Antes:
```csharp
public void Guardar()
{
    Validar();

}
```
```vb
Public Sub Guardar()
    Validar()

End Sub
```
Después:
```csharp
public void Guardar()
{
    Validar();
}
```
```vb
Public Sub Guardar()
    Validar()
End Sub
```

#### 1.6 Quitar líneas en blanco después de atributos

Antes:
```csharp
[HttpPost]

public IActionResult Guardar()
```
```vb
<WebMethod()>

Public Function Guardar() As String
```
Después:
```csharp
[HttpPost]
public IActionResult Guardar()
```
```vb
<WebMethod()>
Public Function Guardar() As String
```

#### 1.7 Quitar líneas en blanco entre llamadas encadenadas

Antes:
```csharp
var activos = clientes
    .Where(c => c.Activo)

    .OrderBy(c => c.Nombre)
    .ToList();
```
Después:
```csharp
var activos = clientes
    .Where(c => c.Activo)
    .OrderBy(c => c.Nombre)
    .ToList();
```

#### 3.1 Línea en blanco entre miembros

Antes:
```csharp
public string Nombre { get; set; }
public void Guardar()
{
}
public void Eliminar()
{
}
```
Después:
```csharp
public string Nombre { get; set; }

public void Guardar()
{
}

public void Eliminar()
{
}
```

#### 3.2 Línea en blanco alrededor de regiones

Antes:
```csharp
private int _id;
#region Métodos
public void Guardar() { }
#endregion
}
```
Después:
```csharp
private int _id;

#region Métodos

public void Guardar() { }

#endregion
}
```

#### 3.3 Línea en blanco antes de cada `case` / `Case`

Antes:
```csharp
switch (estado)
{
    case 1:
        Activar();
        break;
    case 2:
        Desactivar();
        break;
}
```
Después:
```csharp
switch (estado)
{
    case 1:
        Activar();
        break;

    case 2:
        Desactivar();
        break;
}
```

#### 3.4 Línea en blanco antes de comentarios de una línea

Antes:
```csharp
Validar();
// Guardar en la base de datos
_repositorio.Guardar(cliente);
```
```vb
Validar()
' Guardar en la base de datos
_repositorio.Guardar(cliente)
```
Después:
```csharp
Validar();

// Guardar en la base de datos
_repositorio.Guardar(cliente);
```
```vb
Validar()

' Guardar en la base de datos
_repositorio.Guardar(cliente)
```

#### 3.5 Línea en blanco después de `using` / `Imports`

Antes:
```csharp
using System;
using System.Linq;
namespace Ventas
{
```
```vb
Imports System.Data
Public Class Cliente
```
Después:
```csharp
using System;
using System.Linq;

namespace Ventas
{
```
```vb
Imports System.Data

Public Class Cliente
```

#### 4.1 Quitar todas las regiones

Quita `#region` / `#endregion` (`#Region` / `#End Region` en VB) y conserva el contenido.

Antes:
```csharp
#region Propiedades
public string Nombre { get; set; }
#endregion
```
Después:
```csharp
public string Nombre { get; set; }
```

#### 4.2 Actualizar el texto de `#endregion` / `#End Region`

Antes:
```csharp
#region Validaciones
...
#endregion
```
Después:
```csharp
#region Validaciones
...
#endregion Validaciones
```

#### 4.3 Quitar regiones vacías

Antes:
```csharp
#region Eventos
#endregion
```
```vb
#Region "Eventos"
#End Region
```
Después: se eliminan por completo.

#### 5.1 Reorganizar miembros por tipo y acceso

Orden configurable. Por defecto: campos → constructores → propiedades → métodos; dentro de cada grupo, `public` → `private`.

Antes:
```csharp
public class Cliente
{
    public void Guardar() { }
    private int _id;
    public string Nombre { get; set; }
    public Cliente() { }
}
```
Después:
```csharp
public class Cliente
{
    private int _id;

    public Cliente() { }

    public string Nombre { get; set; }

    public void Guardar() { }
}
```

#### 5.2 Accessors todos en una línea o todos multilínea

Antes:
```csharp
public int Edad
{
    get { return _edad; }
    set
    {
        _edad = value;
    }
}
```
Después:
```csharp
public int Edad
{
    get { return _edad; }
    set { _edad = value; }
}
```

#### 6.1 Reajustar comentarios al ancho de línea

Ejemplo con ancho de 60 caracteres.

Antes:
```csharp
// Este método valida el cliente, revisa que tenga documento y correo, y luego lo guarda en la base de datos.
```
Después:
```csharp
// Este método valida el cliente, revisa que tenga
// documento y correo, y luego lo guarda en la base de
// datos.
```

#### 6.2 Espacio después de `//` / `'`

Antes:
```csharp
//Guardar cliente
```
```vb
'Guardar cliente
```
Después:
```csharp
// Guardar cliente
```
```vb
' Guardar cliente
```

### C.2 Markup — Web Forms, Razor, HTML, XML / Config, XAML

| # | Regla | Por defecto | Estado |
|---|---|---|---|
| 1.8 | Quitar líneas en blanco justo dentro de una etiqueta | 🟢 | ✅ |
| 7.2 | Quitar comentarios vacíos `<!-- -->` | 🟢 | ✅ |

#### 1.8 Quitar líneas en blanco justo dentro de una etiqueta

Antes:
```html
<div class="panel">

    <asp:Label ID="lblNombre" runat="server" />

</div>
```
Después:
```html
<div class="panel">
    <asp:Label ID="lblNombre" runat="server" />
</div>
```

#### 7.2 Quitar comentarios vacíos

Antes:
```html
<div>
    <!-- -->
    <span>Total</span>
</div>
```
Después:
```html
<div>
    <span>Total</span>
</div>
```

---

## D. SQL — T-SQL (Visual Studio y SSMS)

Los archivos `.sql` se formatean con **ScriptDOM**, el mismo motor del *Format SQL* de SSMS 22.7+: en segundo plano, sin abrir el editor. Después se aplican las reglas universales (B).

| Regla | Por defecto | Estado |
|---|---|---|
| Formatear T-SQL (las 58 opciones de ScriptDOM) | 🟢 | ✅ |
| Universales (B) | 🟢 | ✅ |

### Antes / después

Con las opciones por defecto de CodeSweep (mayúsculas, tabs, cláusulas con sangría, `;`):

Antes:
```sql
select a,b from dbo.Clientes where activo=1
GO
```
Después:
```sql
SELECT
	a,
	b
FROM
	dbo.Clientes
WHERE
	activo = 1;

GO
```

### De dónde sale cada opción

Para cada una de las 58 propiedades de ScriptDOM, gana la primera fuente que la define:

1. **`.editorconfig`**, sección `[*.sql]`, con las mismas claves que documenta SSMS: `keyword_casing`, `indentation_size`, `comma_placement`, `new_line_before_from_clause`… ([lista completa](https://learn.microsoft.com/ssms/scripting/format-t-sql)).
   - Las claves se comparan sin `_` ni mayúsculas: `newline_formatted_index_definition` y `new_line_formatted_index_definition` son la misma.
   - También acepta las que SSMS no documenta: `include_semicolons`, `persist_trailing_go`, `allow_external_library_paths` y `allow_external_language_paths`.
2. **Opciones del producto**: en SSMS, *Tools → Options → SQL Formatter*. Si el usuario no las cambió, cuenta el default de SSMS.
3. **Opciones de CodeSweep**: *Tools → Options → CodeSweep → Formateador SQL (ScriptDOM)*.

```ini
[*.sql]
keyword_casing = lowercase
indentation_mode = spaces
indentation_size = 2
```

### Qué opciones muestra CodeSweep

CodeSweep solo muestra las opciones que el producto **no** tiene: lo detecta solo al arrancar, sin lista fija en el código.

| Producto | Opciones de formato SQL propias | CodeSweep muestra |
|---|---|---|
| Visual Studio 2022 / 2026 | Ninguna | Las 58 |
| SSMS 22.10 | 54 (*SQL Formatter*) | 4: `IncludeSemicolons`, `PersistTrailingGo`, `AllowExternalLibraryPaths`, `AllowExternalLanguagePaths` |

### Protecciones

- **Versión de ScriptDOM:** siempre se usa el de CodeSweep (180.117), también en SSMS, que trae una versión anterior.
- **Scripts que no se formatean:** los que tienen errores de sintaxis o comandos SQLCMD (`:setvar`, `:r`). En el resumen aparece el motivo, con línea y columna, y las reglas universales se aplican igual.
- **Resultado descartado:** si perdería comentarios o no vuelve a compilar.
- **Strings y comentarios:** las reglas universales no tocan los strings (`'…'`), los identificadores `"…"` / `[…]` de varias líneas ni los comentarios `/* */` anidados.
- **Archivo abierto** (también una ventana de consulta de SSMS sin guardar): se cambia el texto del editor y el usuario guarda. **Archivo cerrado:** se escribe en disco con su misma codificación y BOM.

### Limitación conocida

`include_semicolons = false` no tiene efecto: ScriptDOM 180.117 agrega el `;` igual. Viene de la librería, no de CodeSweep.

---

## Fuera de alcance

| # | Regla | Motivo |
|---|---|---|
| 7.1 | Formatear XML / .config sin abrir ventana | Ya lo resuelve el **editor visible** (CodeSweep pregunta antes de abrirlo). Solo se pasaría a segundo plano si VS expone el mismo formateador XML para usarlo desde código; con un formateador propio el resultado no sería igual al de VS. |

## Ya cubierto por VS (CodeSweep no lo repite)

| Regla | Dónde está en VS |
|---|---|
| Sangría, espacios alrededor de operadores, posición de llaves | *Format Document* / `.editorconfig` (CodeSweep lo ejecuta con la regla *Formatear documento*) |
| `var` vs tipo explícito, `this.`, expresiones con `=>` | *Code Cleanup* + `.editorconfig` |
| Agregar modificadores de acceso explícitos | *Code Cleanup* (`IDE0040`) |
| Encabezado de archivo en C# / VB | *Code Cleanup* (`IDE0073`, `file_header_template`) |

## Dónde se configura cada grupo

*Tools → Options → CodeSweep* (VS 2026 y SSMS 22: interfaz moderna; VS 2022: página clásica):

| Sección | Reglas | En SSMS |
|---|---|---|
| Roslyn (C# y VB) | A | Oculta |
| Espacios (todos los archivos) | B: 1.1 (+ máximo de líneas), 1.2, 1.3, 2.1, 2.2 | Visible |
| SQL | D: activar el formato T-SQL | Visible |
| Formateador SQL (ScriptDOM) | D: las opciones que el producto no tiene | Solo las 4 que SSMS no tiene |
| C# y VB | C.1 (+ tipos de miembro, orden de tipos y accesos, ancho de comentarios) | Oculta |
| Markup | C.2: 1.8, 7.2 | Oculta |
| Tipos de archivo | Qué archivos entran al cleanup + exclusiones | Solo SQL y exclusiones |

Orden en que se aplican:
- **C# / VB:** variables → usings → ordenar usings → reorganizar (5.1) → accessors (5.2) → regiones (4.x) → líneas en blanco (1.4–1.7, 3.x) → comentarios (6.x) → formatear → universales (B).
- **SQL:** formatear con ScriptDOM (D) → universales (B).
- **Demás archivos:** *Format Document* del editor → markup (C.2) → universales (B), antes de guardar.
