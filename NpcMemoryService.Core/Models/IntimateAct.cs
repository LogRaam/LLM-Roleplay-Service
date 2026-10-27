// Code written by Gabriel Mailhot, 27/10/2026.

namespace NpcMemoryService.Core.Models
{
    /// <summary>
    ///   A concrete gesture of lovemaking, named from the CHARACTER's side ("OralGiving": they use their mouth on their
    ///   partner). Gabriel (26/10/2026): "le LLM a tendance a aimer tous les gestes sexuels que le joueur peut decrire.
    ///   Ce serait plus interessant si chaque NPC avait des preferences pour certains gestes sexuels, et meme parfois des
    ///   gestes qu'il n'aime pas." Each character loves a few (<see cref="RomanticProfile.LovedActs" />) and sometimes
    ///   dislikes one or two (<see cref="RomanticProfile.DislikedActs" />); and, since 27/10/2026, each is less fond of
    ///   one (<see cref="RomanticProfile.LessFondActs" />). Surfaced only at Hardcore.
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

    /// <summary>The gestures' written names, as a reader, an author or the console writes them ("oral_giving").</summary>
    public static class IntimateActNames
    {
        /// <summary>The gesture's id ("oral_giving").</summary>
        public static string IdOf(IntimateAct act)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in act.ToString())
            {
                if (char.IsUpper(c) && sb.Length > 0) sb.Append('_');
                sb.Append(char.ToLowerInvariant(c));
            }

            return sb.ToString();
        }

        /// <summary>The gesture a name writes ("oral_giving", "OralGiving", "oral giving"), or None.</summary>
        public static IntimateAct Parse(string? id)
        {
            string key = (id ?? "").Replace("_", "").Replace(" ", "").Trim();
            if (key.Length == 0) return IntimateAct.None;
            foreach (IntimateAct a in (IntimateAct[]) System.Enum.GetValues(typeof(IntimateAct)))
                if (a != IntimateAct.None && string.Equals(a.ToString(), key, System.StringComparison.OrdinalIgnoreCase))
                    return a;

            return IntimateAct.None;
        }
    }
}
