# Purchase module
thin wrapper for Unity's IAP. supports both remote IAP and local IAP. 
Your web server should check purchase receipt and deploy the paid item to the player on remote IAP.

## ID based purchasing
Apple/Google's ID is the primary factor for purchasing.
When Player changed their machine from old one to new one when purchasing products, 
we can trace "uncompleted purchased information" by the purchase notification from Apple/Google.

