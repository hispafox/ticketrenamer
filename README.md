# TicketRenamer

Sistema de renombrado inteligente de tickets de compra para supermercados espanoles.

## Que hace

Lee fotos de tickets de compra, extrae automaticamente el **proveedor** y la **fecha** usando Groq Vision API (LLM con vision), y renombra los archivos al formato:

```
Proveedor-AA-MM-DD.ext
```

Por ejemplo: `IMG_20260215_0001.jpg` -> `Mercadona-26-02-15.jpg`

## Requisitos

- Windows 10/11
- .NET 8 Runtime
- API Key de [Groq](https://console.groq.com/)

## Instalacion

```bash
git clone https://github.com/hispafox/ticketrenamer.git
cd ticketrenamer
dotnet restore
```

Configura tu API key:
```bash
set GROQ_API_KEY=tu_api_key_aqui
```

## Uso

```bash
dotnet run --project src/TicketRenamer.Console -- --input C:\Tickets\entrada --output C:\Tickets\procesados --backup C:\Tickets\backup --verbose
```

### Parametros

| Parametro | Obligatorio | Descripcion |
|-----------|-------------|-------------|
| --input | Si | Carpeta con las fotos de tickets |
| --output | Si | Carpeta destino para archivos renombrados |
| --backup | Si | Carpeta de backup (nunca se borra) |
| --log | No | Ruta al archivo de registro (default: registro.txt) |
| --dry-run | No | Simular sin mover archivos |
| --verbose | No | Mostrar info detallada |

## Flujo de trabajo

1. Deposita fotos de tickets en la carpeta `entrada/`
2. Ejecuta TicketRenamer
3. Los originales se copian a `backup/` (seguridad)
4. Los archivos renombrados van a `procesados/`
5. Todo queda registrado en `registro.txt`

## Proveedores soportados

Mercadona, Carrefour, Lidl, Dia, Aldi, Ahorramas, Consum, BonArea, Eroski, Alcampo, El Corte Ingles, Coviran, Spar, BM, MasyMas.

Puedes anadir mas editando `proveedores.json`.
