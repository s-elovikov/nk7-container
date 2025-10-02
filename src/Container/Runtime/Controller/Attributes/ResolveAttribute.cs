using System;

namespace Nk7.Container
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class ResolveAttribute : Attribute { }
}