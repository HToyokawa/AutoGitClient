using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoGitClient
{
    static internal class StringEnc
    {
        // The code in this class is based on an article published on the web site:
        // http://dobon.net/vb/dotnet/string/encryptstring.html

        // TODO: Make the way to manage the key more strongly.
        
        private static string[] basestr = {"fasfjlsfjockjlapwjf", "af", "oksjdlJJIekC$", "%(Ljcijlkajfi(", ")pkoajflasmfo$9892ik"};

        // Encrypt a string.

        public static string EncryptString(string sourceString, string password)
        {
            System.Security.Cryptography.RijndaelManaged rijndael =
                new System.Security.Cryptography.RijndaelManaged();

            byte[] key, iv;
            GenerateKeyFromPassword(
                password, rijndael.KeySize, out key, rijndael.BlockSize, out iv);
            rijndael.Key = key;
            rijndael.IV = iv;

            byte[] strBytes = System.Text.Encoding.UTF8.GetBytes(sourceString);

            System.Security.Cryptography.ICryptoTransform encryptor =
                rijndael.CreateEncryptor();

            byte[] encBytes = encryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);

            encryptor.Dispose();

            return System.Convert.ToBase64String(encBytes);
        }

        // Decrypt a string.
        public static string DecryptString(string sourceString, string password)
        {
            System.Security.Cryptography.RijndaelManaged rijndael =
                new System.Security.Cryptography.RijndaelManaged();

            byte[] key, iv;
            GenerateKeyFromPassword(
                password, rijndael.KeySize, out key, rijndael.BlockSize, out iv);
            rijndael.Key = key;
            rijndael.IV = iv;

            byte[] strBytes = System.Convert.FromBase64String(sourceString);

            System.Security.Cryptography.ICryptoTransform decryptor =
                rijndael.CreateDecryptor();
            byte[] decBytes = decryptor.TransformFinalBlock(strBytes, 0, strBytes.Length);
            decryptor.Dispose();

            return System.Text.Encoding.UTF8.GetString(decBytes);
        }

        /// <summary>
        /// パスワードから共有キーと初期化ベクタを生成する
        /// </summary>
        /// <param name="password">基になるパスワード</param>
        /// <param name="keySize">共有キーのサイズ（ビット）</param>
        /// <param name="key">作成された共有キー</param>
        /// <param name="blockSize">初期化ベクタのサイズ（ビット）</param>
        /// <param name="iv">作成された初期化ベクタ</param>
        private static void GenerateKeyFromPassword(string password,
            int keySize, out byte[] key, int blockSize, out byte[] iv)
        {
            //パスワードから共有キーと初期化ベクタを作成する
            //saltを決める
            byte[] salt = System.Text.Encoding.UTF8.GetBytes("oijei%iofjkl'kljk#HKcliM%$iac");
            //Rfc2898DeriveBytesオブジェクトを作成する
            System.Security.Cryptography.Rfc2898DeriveBytes deriveBytes =
                new System.Security.Cryptography.Rfc2898DeriveBytes(password, salt);
            //.NET Framework 1.1以下の時は、PasswordDeriveBytesを使用する
            //System.Security.Cryptography.PasswordDeriveBytes deriveBytes =
            //    new System.Security.Cryptography.PasswordDeriveBytes(password, salt);
            //反復処理回数を指定する デフォルトで1000回
            deriveBytes.IterationCount = 2000;

            //共有キーと初期化ベクタを生成する
            key = deriveBytes.GetBytes(keySize / 8);
            iv = deriveBytes.GetBytes(blockSize / 8);
        }

        internal static string TransStr()
        {
            return basestr[2] + basestr[4] + "sdfjsl" + basestr[1] + basestr[3];
        }
    }
}
