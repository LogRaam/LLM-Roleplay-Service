// Code written by Gabriel Mailhot, 10/05/2026.

#region

#endregion

namespace System.Runtime.CompilerServices
{
   // Polyfill for init-only setters support on older frameworks
   internal static class IsExternalInit { }

   // Polyfill for 'required' member attribute
   [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
   internal sealed class RequiredMemberAttribute : Attribute { }

   // Polyfill for CompilerFeatureRequiredAttribute used by newer language features
   [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
   internal sealed class CompilerFeatureRequiredAttribute : Attribute
   {
      public CompilerFeatureRequiredAttribute(string feature) { }
      public CompilerFeatureRequiredAttribute(string feature, string version) { }
   }
}