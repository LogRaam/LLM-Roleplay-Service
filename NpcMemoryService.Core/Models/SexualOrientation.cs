// Code written by Gabriel Mailhot, 24/05/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   Sexual orientation determined once per NPC at profile creation.
    ///   Used to gate whether romantic content surfaces for a given player.
    /// </summary>
    public enum SexualOrientation
    {
        Heterosexual,
        Bisexual,
        Homosexual,
        Pansexual,
        Asexual
    }
}
