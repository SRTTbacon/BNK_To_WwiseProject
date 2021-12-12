using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BNK_To_WwiseProject.Class
{
    public class FileEncode
    {
        public const int KeyLength = 16;
        private static byte[] GenerateByteKey(string password)
        {
            byte[] bytesPassword = Encoding.UTF8.GetBytes(password);
            byte[] bytesKey = new byte[KeyLength];
            for (int i = 0; i < KeyLength; i++)
                bytesKey[i] = (i < bytesPassword.Length) ? bytesPassword[i] : (byte)0;
            return bytesKey;
        }
        public static void Encrypt(Stream ifs, Stream ofs, string password)
        {
            byte[] bytesKey = GenerateByteKey(password);
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider()
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = bytesKey,
            };
            aes.GenerateIV();
            byte[] bytesIV = aes.IV;
            ofs.Write(bytesIV, 0, 16);
            using (ICryptoTransform encrypt = aes.CreateEncryptor())
            {
                using (CryptoStream cs = new CryptoStream(ofs, encrypt, CryptoStreamMode.Write))
                {
                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        int len = ifs.Read(buffer, 0, buffer.Length);
                        if (len > 0)
                            cs.Write(buffer, 0, len);
                        else
                            break;
                    }
                }
            }
        }
        public static void Decrypt_To_File(Stream ifs, Stream ofs, string Password)
        {
            byte[] bytesKey = GenerateByteKey(Password);
            byte[] bytesIV = new byte[KeyLength];
            _ = ifs.Read(bytesIV, 0, KeyLength);
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider()
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = bytesKey,
                IV = bytesIV
            };
            using (ICryptoTransform encrypt = aes.CreateDecryptor())
            {
                using (CryptoStream cs = new CryptoStream(ofs, encrypt, CryptoStreamMode.Write))
                {
                    while (true)
                    {
                        byte[] buffer = new byte[1024];
                        int len = ifs.Read(buffer, 0, buffer.Length);
                        if (len > 0)
                            cs.Write(buffer, 0, len);
                        else
                            break;
                    }
                }
            }
        }
        public static StreamReader Decrypt_To_Stream(Stream ifs, string Password)
        {
            byte[] bytesKey = GenerateByteKey(Password);
            byte[] bytesIV = new byte[KeyLength];
            _ = ifs.Read(bytesIV, 0, KeyLength);
            AesCryptoServiceProvider aes = new AesCryptoServiceProvider()
            {
                BlockSize = 128,
                KeySize = 128,
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                Key = bytesKey,
                IV = bytesIV
            };
            MemoryStream sOutputFilename = new MemoryStream();
            ICryptoTransform desdecrypt = aes.CreateDecryptor();
            CryptoStream cryptostreamDecr = new CryptoStream(ifs, desdecrypt, CryptoStreamMode.Read);
            StreamWriter fsDecrypted = new StreamWriter(sOutputFilename);
            fsDecrypted.Write(new StreamReader(cryptostreamDecr).ReadToEnd());
            fsDecrypted.Flush();
            sOutputFilename.Position = 0;
            return new StreamReader(sOutputFilename);
        }
    }
}