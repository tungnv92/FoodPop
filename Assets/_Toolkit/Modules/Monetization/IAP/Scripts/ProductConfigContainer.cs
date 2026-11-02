using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace MonetToolkit
{
    [CreateAssetMenu(fileName = "Product Config Container", menuName = "SO/IAP/Product Config Container")]
    public class ProductConfigContainer : ScriptableObject
    {
        [TableList]
        public List<ProductConfig> Products;

        public int Count => Products.Count;
        public ProductConfig this[int i] => Products[i];

        public ProductConfig GetProductConfig(string id)
        {
            return Products.Find(p => string.Equals(p.Id, id));
        }
    }

    public static class IAPProductContainerExtensions
    {
        public static bool IsNullOrEmpty(this ProductConfigContainer container)
        {
            return container == null || container.Products == null || container.Products.Count == 0;
        }
    }
}