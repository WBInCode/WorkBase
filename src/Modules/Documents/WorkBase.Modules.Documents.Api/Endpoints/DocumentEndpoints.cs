using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WorkBase.Modules.Documents.Application.Commands;
using WorkBase.Modules.Documents.Application.Queries;
using WorkBase.Shared.Api;
using WorkBase.Shared.Auth;
using WorkBase.Shared.Storage;

namespace WorkBase.Modules.Documents.Api.Endpoints;

public static class DocumentEndpoints
{
    public static IEndpointRouteBuilder MapDocumentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/documents")
            .WithTags("Documents")
            .RequireAuthorization();

        group.MapGet("/", GetDocuments)
            .WithName("GetDocuments")
            .WithSummary("List documents")
            .RequirePermission("documents.view")
            .Produces<List<DocumentDto>>();

        group.MapPost("/", UploadDocument)
            .WithName("UploadDocument")
            .WithSummary("Upload a document")
            .RequirePermission("documents.create")
            .DisableAntiforgery();

        group.MapGet("/{id:guid}/download", DownloadDocument)
            .WithName("DownloadDocument")
            .WithSummary("Download a document")
            .RequirePermission("documents.view");

        group.MapDelete("/{id:guid}", DeleteDocument)
            .WithName("DeleteDocument")
            .WithSummary("Soft-delete a document")
            .RequirePermission("documents.delete");

        var catGroup = endpoints.MapGroup("/api/documents/categories")
            .WithTags("DocumentCategories")
            .RequireAuthorization();

        catGroup.MapGet("/", GetCategories)
            .WithName("GetDocumentCategories")
            .WithSummary("List document categories")
            .RequirePermission("documents.view")
            .Produces<List<DocumentCategoryDto>>();

        catGroup.MapPost("/", CreateCategory)
            .WithName("CreateDocumentCategory")
            .WithSummary("Create a document category")
            .RequirePermission("documents.create");

        catGroup.MapPut("/{id:guid}", UpdateCategory)
            .WithName("UpdateDocumentCategory")
            .WithSummary("Update a document category")
            .RequirePermission("documents.create");

        catGroup.MapDelete("/{id:guid}", DeleteCategory)
            .WithName("DeleteDocumentCategory")
            .WithSummary("Delete a document category")
            .RequirePermission("documents.delete");

        return endpoints;
    }

    private static async Task<IResult> GetDocuments(
        Guid? categoryId, string? entityType, Guid? entityId, bool? includeDeleted, ISender sender)
    {
        var query = new GetDocumentsQuery(categoryId, entityType, entityId, includeDeleted ?? false);
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UploadDocument(HttpRequest request, ISender sender)
    {
        if (!request.HasFormContentType || request.Form.Files.Count == 0)
            return Results.BadRequest(new { message = "No file uploaded" });

        var file = request.Form.Files[0];
        await using var stream = file.OpenReadStream();

        var command = new UploadDocumentCommand(
            file.FileName, file.ContentType, file.Length, stream,
            Guid.Empty, // will be overwritten by auth context if needed
            request.Form.ContainsKey("categoryId") ? Guid.Parse(request.Form["categoryId"]!) : null,
            request.Form.ContainsKey("entityType") ? request.Form["entityType"].ToString() : null,
            request.Form.ContainsKey("entityId") ? Guid.Parse(request.Form["entityId"]!) : null,
            request.Form.ContainsKey("description") ? request.Form["description"].ToString() : null);

        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DownloadDocument(Guid id, ISender sender, IFileStorage fileStorage,
        WorkBase.Modules.Documents.Application.Contracts.IDocumentRepository documentRepository)
    {
        var document = await documentRepository.GetByIdAsync(id);
        if (document is null || document.IsDeleted)
            return Results.NotFound();

        var stream = await fileStorage.DownloadAsync("workbase", document.StoragePath);
        return Results.File(stream, document.ContentType, document.FileName);
    }

    private static async Task<IResult> DeleteDocument(Guid id, ISender sender)
    {
        var command = new DeleteDocumentCommand(id);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetCategories(ISender sender)
    {
        var query = new GetDocumentCategoriesQuery();
        var result = await sender.Send(query);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateCategory(CreateDocumentCategoryCommand command, ISender sender)
    {
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateCategory(Guid id, UpdateDocumentCategoryCommand command, ISender sender)
    {
        var result = await sender.Send(command with { CategoryId = id });
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteCategory(Guid id, ISender sender)
    {
        var command = new DeleteDocumentCategoryCommand(id);
        var result = await sender.Send(command);
        return result.ToHttpResult();
    }
}
