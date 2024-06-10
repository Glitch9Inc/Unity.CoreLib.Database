using UnityEngine;

namespace Glitch9.Database
{
    public class AddressableAttribute : PropertyAttribute
    {
        public string AssetName;

        public AddressableAttribute(string assetName)
        {
            AssetName = assetName;
        }
    }
}
