# Purchase
thin wrapper for Unity's IAP.

## Contains
Apply Apple/Google ID dependent purchase control style.

Which means, treat Apple/Google's ID as primary factor.
When Player changed their machine from old one to new one, We can trace "uncompleted purchased information" with Apple/Google's notification.

Therefore this feature contains "purchased, but that duty is not yet deployed" information inside local storage.

## Caution
Basically, this feature **DOES NOT** contains any encrypting for purchased information which stored locally while puruchasing.

Please encrypt it, or take another method for security by yourself.