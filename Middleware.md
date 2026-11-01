# Middleware: şəkillərlə addım-addım

Əvvəl bir sorğunu, sonra bir middleware-i, next-i və iki middleware-in işləmə sırasını şəkillərlə izah edirik.
Tam izah: [Middleware.pdf](Middleware.pdf).

## Sorğunun yolu
![Sorğunun yolu](Middleware_Images/01_sorgunun_yolu.png)

## next nə edir?
![next nə edir](Middleware_Images/02_next.png)

## İki middleware-in sırası
![İki middleware](Middleware_Images/03_iki_middleware.png)

## Davam dayandırılanda
![Davamın kəsilməsi](Middleware_Images/04_dayanma.png)

## Tam başlanğıc kodu
```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.Use(async (context, next) =>
{
    Console.WriteLine("A: evvel");
    await next(context);
    Console.WriteLine("A: sonra");
});

app.MapGet("/hello", () =>
{
    Console.WriteLine("Salam hazirlanir");
    return "Salam";
});

app.Run();
```

Müsahibə sualları PDF-in son səhifəsindədir.
Mənbələr: [ASP.NET Core middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/), [middleware və DI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/write), [IMiddleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/extensibility).
