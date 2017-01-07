# Purchase
thin wrapper for Unity's IAP. only supports remote IAP. Your web server should check purchase receipt and deploy the paid item to the player.

## Contains
Apply Apple/Google ID dependent purchase control style.

Which means, treat Apple/Google's ID as primary factor.
When Player changed their machine from old one to new one, We can trace "uncompleted purchased information" with Apple/Google's notification.

