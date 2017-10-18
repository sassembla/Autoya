using UnityEngine;

namespace UnityEngine.Purchasing
{
	[AddComponentMenu("")]
	public class DemoInventory : MonoBehaviour
	{
		public void Fulfill (string productId)
		{
			switch (productId) {
			case "100.gold.coins":
				Debug.Log ("You Got Money!");
				break;
			default:
				Debug.Log (
					string.Format (
						"Unrecognized productId \"{0}\"",
						productId
					)
				);
				break;
			}
		}
	}
}
