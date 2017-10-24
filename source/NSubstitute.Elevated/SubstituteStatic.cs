using System;

namespace NSubstitute.Elevated
{
    public static class SubstituteStatic
    {
        // callers need an actual object in order to chain further arranging, so we return this placeholder for static substitutes
        public class Proxy : IDisposable
        {
            readonly IDisposable m_Forwarder;

            internal Proxy(IDisposable forwarder) => m_Forwarder = forwarder;
            public void Dispose() { m_Forwarder.Dispose(); }
        }

        public static Proxy For<T>() => For(typeof(T));
        public static Proxy For(Type staticType) => Substitute.For<Proxy>(staticType);
    }
}
