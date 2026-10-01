using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace BusinessLayer
{
    /// <summary>
    /// Helper class for encryption.
    /// </summary>
    public static class HashingService
    {
        public static string HashPassword(string plainTextPassword)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Turn password to bytes and count hash. 
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(plainTextPassword));

                // Asseble the bytes to readable string
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
