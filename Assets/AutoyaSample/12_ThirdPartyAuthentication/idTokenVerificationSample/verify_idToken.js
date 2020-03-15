const AUDIENCE_CLAIM = "com.kissaki.app";
const ID_TOKEN = "xxxxx";
const NONCE = "xxxxx";



const jwt = require('jsonwebtoken')
const jwksClient = require('jwks-rsa');

var client = jwksClient({
  jwksUri: 'https://appleid.apple.com/auth/keys'
});
 
function getApplePublicKey(header, callback) {
  client.getSigningKey(header.kid, function (err, key) {
    var signingKey = key.publicKey || key.rsaPublicKey;
    callback(null, signingKey);
  });
}
 
jwt.verify(ID_TOKEN, getApplePublicKey, null, function (err, decoded) {
  if (err) {
    console.error("validation err:" + err);
    process.exit(1);
  }
  if (decoded.nonce !== NONCE) {
    console.error("unexpected nonce (nonce claim): ", decoded.nonce);
    process.exit(1);
  }
  if (decoded.iss !== "https://appleid.apple.com") {
    console.error("unexpected issuer (iss claim): ", decoded.iss);
    process.exit(1);
  }
  if (decoded.aud !== AUDIENCE_CLAIM) {
    console.error("unexpected audience (aud claim): ", decoded.aud);
    process.exit(1);
  }

  console.log("succeeded to validate Apple token: ", decoded);
});