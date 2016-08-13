

using Miyamasu;

public class EncryptTests : MiyamasuTestRunner {
	[MTest] public bool Sha256GetsEncrypt () {
		return false;
	}

	[MTest] public bool Sha512GetsEncrypt () {
		return false;
	}

	[MTest] public bool RIPEMD160GetsEncrypt () {
		return false;
	}
}
