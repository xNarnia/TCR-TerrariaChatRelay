namespace TerrariaChatRelay.TMLHooks
{
    public abstract class BaseHook
    {
        public TerrariaChatRelay TCRMod { get; set; }
        public BaseHook(TerrariaChatRelay tcrMod) 
        {
            TCRMod = tcrMod;
        }

        public abstract void Attach();
        public abstract void Detach();
    }
}
