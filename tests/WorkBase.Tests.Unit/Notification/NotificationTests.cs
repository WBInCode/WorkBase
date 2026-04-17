using NSubstitute;
using WorkBase.Modules.Notification.Application.Commands;
using WorkBase.Modules.Notification.Application.Contracts;
using WorkBase.Modules.Notification.Application.Queries;
using WorkBase.Shared.Domain;
using Xunit;

namespace WorkBase.Tests.Unit.Notification;

public class SendNotificationHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();

    [Fact]
    public async Task Handle_CreatesNotificationAndReturnsId()
    {
        var command = new SendNotificationCommand(
            Guid.NewGuid(), "Test Title", "Test Body", "test_category")
        { TenantId = Guid.NewGuid() };

        var handler = new SendNotificationHandler(_repository);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _repository.Received(1).AddAsync(
            Arg.Is<WorkBase.Modules.Notification.Domain.Entities.Notification>(
                n => n.Title == "Test Title" && n.Body == "Test Body" && n.Category == "test_category"),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}

public class MarkNotificationReadHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();

    [Fact]
    public async Task Handle_NotificationExists_MarksAsRead()
    {
        var tenantId = Guid.NewGuid();
        var notification = WorkBase.Modules.Notification.Domain.Entities.Notification.Create(
            tenantId, Guid.NewGuid(), "Title", "Body", "cat");

        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>())
            .Returns(notification);

        var command = new MarkNotificationReadCommand(notification.Id) { TenantId = tenantId };
        var handler = new MarkNotificationReadHandler(_repository);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(notification.IsRead);
        _repository.Received(1).Update(notification);
    }

    [Fact]
    public async Task Handle_NotificationNotFound_ReturnsFailure()
    {
        _repository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WorkBase.Modules.Notification.Domain.Entities.Notification?)null);

        var command = new MarkNotificationReadCommand(Guid.NewGuid()) { TenantId = Guid.NewGuid() };
        var handler = new MarkNotificationReadHandler(_repository);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task Handle_WrongTenant_ReturnsFailure()
    {
        var notification = WorkBase.Modules.Notification.Domain.Entities.Notification.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Title", "Body", "cat");

        _repository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>())
            .Returns(notification);

        var command = new MarkNotificationReadCommand(notification.Id) { TenantId = Guid.NewGuid() };
        var handler = new MarkNotificationReadHandler(_repository);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
    }
}

public class GetNotificationsHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();

    [Fact]
    public async Task Handle_ReturnsMappedDtos()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var notification = WorkBase.Modules.Notification.Domain.Entities.Notification.Create(
            tenantId, userId, "Title", "Body", "test");

        _repository.GetByRecipientAsync(tenantId, userId, false, 50, Arg.Any<CancellationToken>())
            .Returns([notification]);

        var query = new GetNotificationsQuery(userId) { TenantId = tenantId };
        var handler = new GetNotificationsHandler(_repository);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
        Assert.Equal("Title", result.Value[0].Title);
        Assert.False(result.Value[0].IsRead);
    }
}

public class GetUnreadCountHandlerTests
{
    private readonly INotificationRepository _repository = Substitute.For<INotificationRepository>();

    [Fact]
    public async Task Handle_ReturnsCount()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _repository.GetUnreadCountAsync(tenantId, userId, Arg.Any<CancellationToken>())
            .Returns(5);

        var query = new GetUnreadCountQuery(userId) { TenantId = tenantId };
        var handler = new GetUnreadCountHandler(_repository);

        var result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
    }
}

public class NotificationEntityTests
{
    [Fact]
    public void Create_SetsAllProperties()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var refId = Guid.NewGuid();

        var notification = WorkBase.Modules.Notification.Domain.Entities.Notification.Create(
            tenantId, userId, "Title", "Body", "cat", "workflow_instance", refId);

        Assert.Equal(tenantId, notification.TenantId);
        Assert.Equal(userId, notification.RecipientUserId);
        Assert.Equal("Title", notification.Title);
        Assert.Equal("Body", notification.Body);
        Assert.Equal("cat", notification.Category);
        Assert.Equal("workflow_instance", notification.ReferenceType);
        Assert.Equal(refId, notification.ReferenceId);
        Assert.False(notification.IsRead);
        Assert.Null(notification.ReadAt);
    }

    [Fact]
    public void MarkAsRead_SetsIsReadAndReadAt()
    {
        var notification = WorkBase.Modules.Notification.Domain.Entities.Notification.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Title", "Body", "cat");

        notification.MarkAsRead();

        Assert.True(notification.IsRead);
        Assert.NotNull(notification.ReadAt);
    }

    [Fact]
    public void MarkAsRead_AlreadyRead_NoChange()
    {
        var notification = WorkBase.Modules.Notification.Domain.Entities.Notification.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Title", "Body", "cat");

        notification.MarkAsRead();
        var readAt = notification.ReadAt;
        notification.MarkAsRead();

        Assert.Equal(readAt, notification.ReadAt);
    }
}
