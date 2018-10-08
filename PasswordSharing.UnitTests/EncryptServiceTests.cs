using System;
using System.Security.Cryptography;
using PasswordSharing.Algorithms;
using PasswordSharing.Constants;
using Shouldly;
using Xunit;

namespace PasswordSharing.UnitTests
{
	public class EncryptServiceTests
	{
		private readonly EncryptService _service;

		public EncryptServiceTests()
		{
			_service = new EncryptService();
		}

		[Fact]
		public void EncryptionShouldWorkCorrect()
		{
			// Arrange
			using (var csp = new RSACryptoServiceProvider(AlgorithmConstants.KeySize))
			{
				var pubKey = csp.ExportParameters(false);
				var privKey = csp.ExportParameters(true);

				var str = "helloworld";

				// Act
				var encoded = _service.Encode(str, pubKey);
				var decoded = _service.Decode(encoded, privKey);

				// Assert
				decoded.ShouldBe(str);
			}
		}

		[Fact]
		public void EncryptionShouldWorkCorrectWithOnlyOnePrivateKey()
		{
			// Arrange
			using (var csp = new RSACryptoServiceProvider(AlgorithmConstants.KeySize))
			{
				var privKey = csp.ExportParameters(true);

				var str = "helloworld";

				// Act
				var encoded = _service.Encode(str, privKey);
				var decoded = _service.Decode(encoded, privKey);

				// Assert
				decoded.ShouldBe(str);
			}
		}

		[Fact]
		public void EncryptionShouldbeDeniedIfWrongKeyProvided()
		{
			// Arrange
			const string str = "helloworld";

			RSAParameters pubKey;
			using (var csp = new RSACryptoServiceProvider(AlgorithmConstants.KeySize))
			{
				pubKey = csp.ExportParameters(false);
			}

			RSAParameters privKey;
			using (var csp = new RSACryptoServiceProvider(AlgorithmConstants.KeySize))
			{
				privKey = csp.ExportParameters(true);
			}

			// Act
			var encoded = _service.Encode(str, pubKey);
			Action decodedAction = () => _service.Decode(encoded, privKey);

			// Assert
			decodedAction.ShouldThrow<Exception>();
		}
	}
}
