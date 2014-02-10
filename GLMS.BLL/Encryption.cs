using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Web.Mvc;

namespace GLMS.BLL
{
    public interface IEncrypt
    {
        byte[] Encrypt(string plainText);
        bool CompareToEncrypted(string plainText, byte[] encryptedText);
    }

    public interface IDecrypt
    {
        string Decrypt(byte[] encryptedText);
        bool CompareToEncrypted(string plainText, byte[] encryptedText);
    }

    public static class EncryptionExtensions
    {
        /*
        const string chars = "abcdefhkrstyzABCDEFHKRSTXYZ23456789";
        const int CharCount = 25;
        static int Bits = chars.Length;

        public static string AsPrintable(this Guid? guid, int maxLength = CharCount)
        {
            if (guid == null) return null;
            if (maxLength > CharCount || maxLength <= CharCount / 4) maxLength = CharCount;
            char[] encoded = new char[maxLength];
            var bytes = new BigInteger(guid.Value.ToByteArray().Concat(new byte[] { 0 }).ToArray());
            for (int i = 0; i < maxLength; i++)
            {
                encoded[i] = chars[(int)(bytes % Bits)];
                bytes = bytes / Bits;
            }
            return new string(encoded);
        }
        */

        public static byte[] Encrypt(this string plainText)
        {
            if (plainText == null) return null;
            IEncrypt encrypt = DependencyResolver.Current.GetService<IEncrypt>();
            if (encrypt == null)
            {
                return System.Text.UnicodeEncoding.Unicode.GetBytes(plainText);
            }
            else
            {
                return encrypt.Encrypt(plainText);
            }
        }

        public static bool CompareToEncrypted(this string plainText, byte[] encryptedText)
        {
            if (encryptedText == null) return plainText == null;
            IEncrypt encrypt = DependencyResolver.Current.GetService<IEncrypt>();
            if (encrypt == null)
            {
                return System.Text.UnicodeEncoding.Unicode.GetBytes(plainText).Equals(encryptedText);
            }
            else
            {
                return encrypt.CompareToEncrypted(plainText, encryptedText);
            }
        }

        public static string Decrypt(this byte[] encryptedText)
        {
            if (encryptedText == null) return null;
            IDecrypt decrypt = DependencyResolver.Current.GetService<IDecrypt>();
            if (decrypt == null)
            {
                IEncrypt encrypt = DependencyResolver.Current.GetService<IEncrypt>();
                if (encrypt == null)
                {
                    return System.Text.UnicodeEncoding.Unicode.GetString(encryptedText);
                }
                else
                {
                    throw new NotImplementedException("Encryption is one-way.");
                }
            }
            else
            {
                return decrypt.Decrypt(encryptedText);
            }
        }
    }

    public class DefaultEncryption : IEncrypt
    {
        private const int SALTSIZE = 32;
        private const int MAXPWLEN = 32;

        public byte[] Encrypt(string plainText)
        {
            byte[] salt = new byte[SALTSIZE];
            RNGCryptoServiceProvider.Create().GetBytes(salt);
            return Encrypt(plainText, salt);
        }

        public bool CompareToEncrypted(string plainText, byte[] encryptedText)
        {
            byte[] salt = new byte[SALTSIZE];
            Buffer.BlockCopy(encryptedText, encryptedText.Length - SALTSIZE, salt, 0, SALTSIZE);
            return Encrypt(plainText, salt).SequenceEqual(encryptedText);
        }

        private byte[] Encrypt(string plainText, byte[] salt)
        {
            byte[] plainTextBytes;
            if (String.IsNullOrEmpty(plainText))
            {
                plainTextBytes = new byte[MAXPWLEN];
            }
            else
            {
                plainTextBytes = System.Text.UnicodeEncoding.Unicode.GetBytes(plainText);
            }
            byte[] combinedBytes = new byte[plainTextBytes.Length + salt.Length];
            Buffer.BlockCopy(plainTextBytes, 0, combinedBytes, 0, plainTextBytes.Length);
            Buffer.BlockCopy(salt, 0, combinedBytes, plainTextBytes.Length, salt.Length);
            HashAlgorithm hashAlgo = new SHA256Managed();
            byte[] hash = hashAlgo.ComputeHash(combinedBytes);
            byte[] hashPlusSalt = new byte[hash.Length + salt.Length];
            Buffer.BlockCopy(hash, 0, hashPlusSalt, 0, hash.Length);
            Buffer.BlockCopy(salt, 0, hashPlusSalt, hash.Length, salt.Length);
            return hashPlusSalt;
        }
    }
}

