using System;

namespace MirrorChronicles.Data
{
    public enum ItemType
    {
        HealingPill,
        QiRestorationPill
    }

    [Serializable]
    public class ItemData
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public ItemType Type { get; set; }
        public int Power { get; set; } // Amount healed or Qi restored
        public int Quantity { get; set; }

        public ItemData()
        {
            ID = Guid.NewGuid().ToString();
            Quantity = 1;
        }
    }
}
