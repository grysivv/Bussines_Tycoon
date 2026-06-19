using System;

namespace Conglomerate.Economy
{
    public class ProductBatch
    {
        public string ResourceName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Quality { get; set; }
        public decimal BrandAwareness { get; set; }

        public ProductBatch() 
        { 
            ResourceName = "";
        }

        public ProductBatch(string name, decimal quantity, decimal quality = 10m, decimal brand = 0m)
        {
            ResourceName = name;
            Quantity = quantity;
            Quality = quality;
            BrandAwareness = brand;
        }

        public void MergeWith(ProductBatch other)
        {
            if (other.ResourceName != ResourceName) return;
            if (other.Quantity <= 0) return;

            decimal totalQuantity = Quantity + other.Quantity;
            if (totalQuantity > 0)
            {
                // Ważona średnia
                Quality = ((Quality * Quantity) + (other.Quality * other.Quantity)) / totalQuantity;
                BrandAwareness = ((BrandAwareness * Quantity) + (other.BrandAwareness * other.Quantity)) / totalQuantity;
                Quantity = totalQuantity;
            }
        }
    }
}
