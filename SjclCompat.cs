using System;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace DeskCryptoPad
{
    public class SJCLBlob
    {
        [JsonProperty("iv")]
        public string IV { get; set; }
        [JsonProperty("v")]
        public int V { get; set; }
        [JsonProperty("iter")]
        public int Iterations { get; set; }
        [JsonProperty("ks")]
        public int KeySize { get; set; }
        [JsonProperty("ts")]
        public int TagSize { get; set; }
        [JsonProperty("mode")]
        public string Mode { get; set; }
        [JsonProperty("cipher")]
        public string Cipher { get; set; }
        [JsonProperty("adata")]
        public string AuthData { get; set; }
        [JsonProperty("salt")]
        public string Salt { get; set; }
        [JsonProperty("ct")]
        public string CipherText { get; set; }
    }

    class SjclCompat
    {
        private static byte[] DecodeBase64(string encoded)
        {
            for (int i = 0; i < (encoded.Length%4); i++)
            {
                encoded += "=";
            }
            return Convert.FromBase64String(encoded);
        }

        public static string Decrypt(string password, string data)
        {
            SJCLBlob ctdata = JsonConvert.DeserializeObject<SJCLBlob>(data);
            if (ctdata.Cipher != "aes" || ctdata.Mode != "ccm")
                throw new InvalidOperationException("Unsupported cipher or mode.");
            byte[] cipherText = DecodeBase64(ctdata.CipherText);
            var derivedMacParameters = DeriveKey(password, ctdata);

            var l = FindIVLen(cipherText.Length);
            byte[] iv = new byte[l];
            Array.Copy((Array) DecodeBase64(ctdata.IV), (Array) iv, (int) l);

            var ccmparams = new CcmParameters(derivedMacParameters, ctdata.TagSize, iv, DecodeBase64(ctdata.AuthData));
            var ccmMode = new CcmBlockCipher(new AesFastEngine());
            ccmMode.Init(false, ccmparams);
            var plainBytes = new byte[ccmMode.GetOutputSize(cipherText.Length)];
            var res = ccmMode.ProcessBytes(cipherText, 0, cipherText.Length, plainBytes, 0);
            ccmMode.DoFinal(plainBytes, res);
            return Encoding.UTF8.GetString(plainBytes);
        }

        private static KeyParameter DeriveKey(string password, SJCLBlob ctdata)
        {
            var kdf = new Pkcs5S2Sha256ParametersGenerator();
            kdf.Init(Encoding.UTF8.GetBytes(password), DecodeBase64(ctdata.Salt), ctdata.Iterations);
            var derivedMacParameters = (KeyParameter) kdf.GenerateDerivedMacParameters(ctdata.KeySize);
            return derivedMacParameters;
        }

        public static string Encrypt(string password, string data)
        {
            RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
            byte[] salt = new byte[8];
            rngCsp.GetBytes(salt);
            byte[] iv = new byte[16];
            rngCsp.GetBytes(iv);

            SJCLBlob ctdata = new SJCLBlob()
                {
                    Mode = "ccm",
                    Cipher = "aes",
                    AuthData = "",
                    Iterations = 2000,
                    KeySize = 256,
                    TagSize = 64,
                    Salt = Convert.ToBase64String(salt),
                    IV = Convert.ToBase64String(iv),
                    V = 1
                };
            var key = DeriveKey(password, ctdata);
            byte[] rawdata = Encoding.UTF8.GetBytes(data);
            var l = FindIVLen(rawdata.Length);
            byte[] civ = new byte[l];
            Array.Copy((Array) iv, (Array) civ, (int) l);

            var ccmparams = new CcmParameters(key, ctdata.TagSize, civ, DecodeBase64(ctdata.AuthData));
            var ccmMode = new CcmBlockCipher(new AesFastEngine());
            ccmMode.Init(true, ccmparams);
            var encBytes = new byte[ccmMode.GetOutputSize(rawdata.Length)];
            var res = ccmMode.ProcessBytes(rawdata, 0, rawdata.Length, encBytes, 0);
            ccmMode.DoFinal(encBytes, res);
            ctdata.CipherText = Convert.ToBase64String(encBytes);

            return JsonConvert.SerializeObject(ctdata);
        }

        private static int FindIVLen(int bufferLength)
        {
            int i;
            for (i = 2; i < 4 && bufferLength > ((1 << (i*8)) - 1); i++) ;
            return 15 - i;
        }
    }
}