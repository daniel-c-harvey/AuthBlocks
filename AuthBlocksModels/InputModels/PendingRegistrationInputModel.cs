using Models.Shared.InputModels;

namespace AuthBlocksModels.InputModels;

public class PendingRegistrationInputModel : InputModelBase
{
    private bool _isConsumed;
    public string Email { get; set; } = string.Empty;

    public DateTime ExpiresAt
    {
        get
        {
            if (ExpiresAtDate == null || ExpiresAtTime == null) return DateTime.Now;
            return ExpiresAtDate.Value.Add(ExpiresAtTime.Value);
        }
        set
        {
            ExpiresAtDate = value.Date;
            ExpiresAtTime = value.TimeOfDay;
        }
    }
    
    public DateTime? ExpiresAtDate { get; set; }
    public TimeSpan? ExpiresAtTime { get; set; }

    public bool IsConsumed
    {
        get => _isConsumed;
        set
        {
            _isConsumed = value;
            if (!_isConsumed)
            {
                ConsumedAt = null;
            }
            else if (ConsumedAt == null)
            {
                ConsumedAt = DateTime.Now;
            }
        }
    }

    public DateTime? ConsumedAt
    {
        get
        {
            if (ConsumedAtDate == null || ConsumedAtTime == null) return null;
            return ConsumedAtDate?.Add(ConsumedAtTime.Value);
        }
        set
        {
            if (value == null)
            {
                ConsumedAtDate = null;
                ConsumedAtTime = null;
                return;
            }
            
            ConsumedAtDate = value.Value.Date;
            ConsumedAtTime = value.Value.TimeOfDay;
        }
    }
    
    public DateTime? ConsumedAtDate { get; set; }
    
    public TimeSpan? ConsumedAtTime { get; set; }
    
    public IEnumerable<RoleInputModel>? Roles { get; set; }
}