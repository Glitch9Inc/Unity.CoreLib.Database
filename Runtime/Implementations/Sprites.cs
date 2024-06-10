using UnityEngine;

namespace Glitch9.Database
{
    public class Sprites : AddressableDatabase<Sprites, Sprite>
    {
        public static Sprite Default => Get(0);
        public const int DEFAULT_PORTRAIT_ID = 3000;
        public static Sprite GetPortrait(int id) => Get(id, DEFAULT_PORTRAIT_ID);
    }
}