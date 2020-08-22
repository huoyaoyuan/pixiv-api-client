using System;

namespace Meowtrix
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class GenerateNullTestAttribute : Attribute
    {
    }
}
