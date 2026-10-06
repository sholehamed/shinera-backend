using Application.SharedKernel.Abstractions.Messaging;
using Domain.SharedKernel.Common.Events;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Application.SharedKernel.Dispatcher;

public sealed class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    public async Task<TResult> Send<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, typeof(TResult));

        dynamic handler = serviceProvider.GetRequiredService(handlerType);
        dynamic actualCommand = command;

        TResult result = await handler.Handle(actualCommand, cancellationToken);
        return result;
    }

    public async Task Send(
        ICommand command,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(command, cancellationToken);

        var commandType = command.GetType();
        var handlerType = typeof(ICommandHandler<>).MakeGenericType(commandType);

        dynamic handler = serviceProvider.GetRequiredService(handlerType);
        dynamic actualCommand = command;

        await handler.Handle(actualCommand, cancellationToken);
    }

    public async Task<TResult> Query<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(query, cancellationToken);

        var queryType = query.GetType();
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, typeof(TResult));

        dynamic handler = serviceProvider.GetRequiredService(handlerType);
        dynamic actualQuery = query;

        TResult result = await handler.Handle(actualQuery, cancellationToken);
        return result;
    }

    public async Task Publish<TNotification>(
        TNotification notification,
        CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        var handlers = serviceProvider.GetServices<INotificationHandler<TNotification>>();

        foreach (var handler in handlers)
            await handler.Handle(notification, cancellationToken);
    }

    private async Task ValidateAsync<TRequest>(
        TRequest request,
        CancellationToken cancellationToken)
    {
        var validators = serviceProvider.GetServices<IValidator<TRequest>>().ToArray();

        if (validators.Length == 0)
            return;

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .ToArray();

        if (failures.Length > 0)
            throw new Application.SharedKernel.Exceptions.ValidationException(failures);
    }
}
