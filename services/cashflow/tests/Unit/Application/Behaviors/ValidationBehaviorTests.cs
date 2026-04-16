using ArchChallenge.CashFlow.Application.Common.Behaviors;
using ArchChallenge.CashFlow.Application.Common.Enqueue;
using ArchChallenge.CashFlow.Application.Transactions.Commands.EnqueueTransaction;
using ArchChallenge.CashFlow.Domain.Enums;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;

namespace ArchChallenge.CashFlow.Tests.Unit.Application.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNextDirectly()
    {
        var behavior = new ValidationBehavior<EnqueueTransaction, EnqueueResult>(
            Enumerable.Empty<IValidator<EnqueueTransaction>>());

        var next   = Substitute.For<RequestHandlerDelegate<EnqueueResult>>();
        next(Arg.Any<CancellationToken>()).Returns(new EnqueueResult(Guid.NewGuid()));

        var command = BuildCommand();

        var result = await behavior.Handle(command, next, CancellationToken.None);

        result.Should().NotBeNull();
        await next.Received(1)(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidatorsAllPassing_ShouldCallNext()
    {
        var validator = Substitute.For<IValidator<EnqueueTransaction>>();
        validator.Validate(Arg.Any<ValidationContext<EnqueueTransaction>>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<EnqueueTransaction, EnqueueResult>(
            new[] { validator });

        var next = Substitute.For<RequestHandlerDelegate<EnqueueResult>>();
        next(Arg.Any<CancellationToken>()).Returns(new EnqueueResult(Guid.NewGuid()));

        var result = await behavior.Handle(BuildCommand(), next, CancellationToken.None);

        result.Should().NotBeNull();
        await next.Received(1)(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidatorFailure_ShouldThrowValidationException()
    {
        var failure   = new ValidationFailure("Amount", "Amount must be greater than zero");
        var validator = Substitute.For<IValidator<EnqueueTransaction>>();
        validator.Validate(Arg.Any<ValidationContext<EnqueueTransaction>>())
            .Returns(new ValidationResult(new[] { failure }));

        var behavior = new ValidationBehavior<EnqueueTransaction, EnqueueResult>(
            new[] { validator });

        var next = Substitute.For<RequestHandlerDelegate<EnqueueResult>>();

        var act = async () => await behavior.Handle(BuildCommand(), next, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Amount*");

        await next.DidNotReceive()(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithMultipleValidationFailures_ShouldThrowWithAllErrors()
    {
        var failures = new[]
        {
            new ValidationFailure("Amount",      "Amount error"),
            new ValidationFailure("Description", "Description error")
        };

        var validator = Substitute.For<IValidator<EnqueueTransaction>>();
        validator.Validate(Arg.Any<ValidationContext<EnqueueTransaction>>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<EnqueueTransaction, EnqueueResult>(
            new[] { validator });

        var next = Substitute.For<RequestHandlerDelegate<EnqueueResult>>();

        var ex = await Assert.ThrowsAsync<ValidationException>(
            () => behavior.Handle(BuildCommand(), next, CancellationToken.None));

        ex.Errors.Should().HaveCount(2);
    }

    private static EnqueueTransaction BuildCommand() =>
        new(TransactionType.Credit, 100m, "Test");
}
