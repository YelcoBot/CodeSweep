namespace YelcoBot.CodeSweep.Application.Services
{
    /// <summary>
    /// Indica si hay un cleanup en curso. Evita que los guardados que hace el propio cleanup
    /// (archivos cerrados, estrategia invisible) disparen un "cleanup on save" anidado.
    /// </summary>
    public sealed class SweepActivity
    {
        private int _running;

        public bool IsRunning => Volatile.Read(ref _running) > 0;

        public IDisposable Begin()
        {
            Interlocked.Increment(ref _running);
            return new Scope(this);
        }

        private sealed class Scope : IDisposable
        {
            private SweepActivity? _owner;

            public Scope(SweepActivity owner) => _owner = owner;

            public void Dispose()
            {
                SweepActivity? owner = Interlocked.Exchange(ref _owner, null);
                if (owner != null)
                {
                    Interlocked.Decrement(ref owner._running);
                }
            }
        }
    }
}
