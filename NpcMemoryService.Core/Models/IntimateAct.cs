// Code written by Gabriel Mailhot, 27/10/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   A concrete gesture of lovemaking, named from the CHARACTER's side ("OralGiving": they use their mouth on their
    ///   partner). Gabriel (26/10/2026): "le LLM a tendance a aimer tous les gestes sexuels que le joueur peut decrire.
    ///   Ce serait plus interessant si chaque NPC avait des preferences pour certains gestes sexuels, et meme parfois des
    ///   gestes qu'il n'aime pas." Each character loves a few (<see cref="RomanticProfile.LovedActs" />) and sometimes
    ///   dislikes one or two (<see cref="RomanticProfile.DislikedActs" />). Surfaced only at Hardcore.
    ///
    ///   Saved as integers: APPEND ONLY, never insert or reorder (IntimateActOrdinalTests).
    /// </summary>
    public enum IntimateAct
    {
        None,
        Kissing,          // Long, deep kisses
        OralGiving,       // Their mouth on their partner
        OralReceiving,    // Their partner's mouth on them
        AnalGiving,       // They take their partner from behind, anally
        AnalReceiving,    // They are taken anally
        OnTop,            // They ride, above their partner
        Beneath,          // They lie beneath, taken from above
        FromBehind,       // They are taken from behind
        HandsAndFingers,  // Touching, stroking, fingers inside
        RoughHandling,    // Biting, scratching, hair pulled, held hard
        DirtyTalk,        // Crude words said and heard in the act
    }
}
