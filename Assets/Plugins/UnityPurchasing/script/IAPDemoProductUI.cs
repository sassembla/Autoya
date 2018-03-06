#if UNITY_PURCHASING

using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Purchasing;

public class IAPDemoProductUI : MonoBehaviour
{
    public Button purchaseButton;
    public Button receiptButton;
    public Text titleText;
    public Text descriptionText;
    public Text priceText;
    public Text statusText;

    private string m_ProductID;
    private Action<string> m_PurchaseCallback;
    private string m_Receipt;

    public void SetProduct(Product p, Action<string> purchaseCallback)
    {
        titleText.text = p.metadata.localizedTitle;
        descriptionText.text = p.metadata.localizedDescription;
        priceText.text = p.metadata.localizedPriceString;

        receiptButton.interactable = p.hasReceipt;
        m_Receipt = p.receipt;

        m_ProductID = p.definition.id;
        m_PurchaseCallback = purchaseCallback;

        statusText.text = p.availableToPurchase ? "Available" : "Unavailable";
    }

    public void SetPendingTime(int secondsRemaining)
    {
        statusText.text = "Pending " + secondsRemaining.ToString();
    }

    public void PurchaseButtonClick()
    {
        if (m_PurchaseCallback != null && !string.IsNullOrEmpty(m_ProductID))
        {
            m_PurchaseCallback(m_ProductID);
        }
    }

    public void ReceiptButtonClick()
    {
        if (!string.IsNullOrEmpty(m_Receipt))
            Debug.Log("Receipt for " + m_ProductID + ": " + m_Receipt);
    }
}

#endif
