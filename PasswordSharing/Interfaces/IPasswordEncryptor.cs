﻿using System.Security.Cryptography;
using PasswordSharing.Models;

namespace PasswordSharing.Interfaces
{
	public interface IPasswordEncryptor
	{
		Password Encode(string password, RSAParameters key);
		string Decode(Password password, RSAParameters key);
	}
}