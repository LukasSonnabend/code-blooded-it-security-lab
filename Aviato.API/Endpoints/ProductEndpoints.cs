using Aviato.Core.Interfaces;

namespace Aviato.API.Endpoints;

public static class ProductEndpoints
{
    public static void MapProductEndpoints(this WebApplication app)
    {
        app.MapGet("/products", async (IProductService productService) =>
        {
            var products = await productService.GetAllAsync();
            return Results.Ok(products);
        });

        app.MapGet("/products/{id:int}", async (int id, IProductService productService) =>
        {
            var product = await productService.GetByIdAsync(id);
            return product is null ? Results.NotFound() : Results.Ok(product);
        });

        app.MapGet("/products/search", async (string q, IProductService productService) =>
        {
            var results = await productService.SearchAsync(q);
            return Results.Ok(results);
        });
    }
}
