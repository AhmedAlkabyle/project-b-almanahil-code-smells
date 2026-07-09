// ============================================
// UpdateUserStatusDto — the tiny parcel to switch a user on or off.
// A DTO is a simple 'data parcel' the website sends to the server.
// This one just carries whether the account should be active.
// ============================================
namespace AlmanahilAPI.DTOs;

// Travels: website -> server. Turns an account on or off (we never fully delete).
/// <summary>Payload for activating/deactivating a user (no hard delete).</summary>
public class UpdateUserStatusDto
{
    // true = account is on (active); false = account is switched off.
    /// <summary>Desired state: true = active, false = deactivated.</summary>
    public bool IsActive { get; set; }
}
