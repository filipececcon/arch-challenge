namespace ArchChallenge.CashFlow.Domain.Shared.Entities;

public abstract class Entity : Notifiable<Notification>
{
    public Guid Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; }
    public DateTime UpdatedAt { get; private set; }
    public bool Active { get; private set; }
    
    public bool IsFailure => !IsValid;

    protected Entity()
    {
        Id = Guid.NewGuid();
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Active = true;
    }

    public void Activate()
    {
        Active = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        Active = false;
        UpdatedAt = DateTime.UtcNow;
    }
}
