using ArchChallenge.CashFlow.Domain.Shared.Exceptions;
using FluentAssertions;

namespace ArchChallenge.CashFlow.Tests.Unit.Domain;

public class DomainExceptionTests
{
    [Fact]
    public void DomainException_Create_ShouldSetMessage()
    {
        const string message = "Business rule violated";

        var ex = new DomainException(message);

        ex.Message.Should().Be(message);
    }

    [Fact]
    public void DomainException_ShouldBeAnException()
    {
        var ex = new DomainException("error");

        ex.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void DomainException_ShouldBeThrownAndCaught()
    {
        Action act = () => throw new DomainException("invalid operation");

        act.Should().Throw<DomainException>().WithMessage("invalid operation");
    }
}
