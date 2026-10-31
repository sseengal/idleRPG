using System;

namespace IdleRPG.Data
{
    /// <summary>
    /// One authored wave size. Used for tutorial or story fights that must not follow the recipe.
    /// A count of 0 is ignored.
    /// </summary>
    [Serializable]
    public struct WaveSizeOverride
    {
        public int stage;
        public int wave;
        public int count;

        public WaveSizeOverride(int stage, int wave, int count)
        {
            this.stage = stage;
            this.wave = wave;
            this.count = count;
        }
    }
}
