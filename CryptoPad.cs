using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Macs;
using Org.BouncyCastle.Crypto.Parameters;

namespace DeskCryptoPad
{
    internal class CryptoPadPad
    {
        private readonly CryptoPad _cryptoPad;
        private string _data;
        private string _name;

        public CryptoPadPad(string name, CryptoPad cryptoPad)
        {
            _cryptoPad = cryptoPad;
            _name = name;
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                _cryptoPad.UpdatePads();
                Save();
            }
        }

        public string Data
        {
            get
            {
                Load();
                return _data;
            }
            set
            {
                _data = value;
                Save();
            }
        }

        private void Save()
        {
            _cryptoPad.EncryptKVS("pad:" + Name, _data);
        }

        private void Load()
        {
            _data = _cryptoPad.DecryptKVS("pad:" + Name);
        }

        public override string ToString()
        {
            return Name;
        }
    }

    internal class CryptoPad
    {
        private const string StorageUrl = "https://cryptopad.in/storage/";

        public CryptoPad(string password)
        {
            Password = password;
        }

        protected string Password { get; set; }
        protected BindingList<CryptoPadPad> Pads { get; set; }
        public event EventHandler<BindingList<CryptoPadPad>> PadsLoaded;

        protected virtual void OnPadsLoaded(BindingList<CryptoPadPad> e)
        {
            EventHandler<BindingList<CryptoPadPad>> handler = PadsLoaded;
            if (handler != null) handler(this, e);
        }

        public void LoadPads()
        {
            string json = DecryptKVS("pads");
            Pads =
                new BindingList<CryptoPadPad>(
                    JsonConvert.DeserializeObject<Collection<string>>(json)
                               .Where(x => x != null)
                               .Select(x => new CryptoPadPad(x, this))
                               .ToList());
            Pads.ListChanged += PadsOnListChanged;
            OnPadsLoaded(Pads);
        }

        private void PadsOnListChanged(object sender, ListChangedEventArgs listChangedEventArgs)
        {
            UpdatePads();
        }

        private static string ReadKVS(string key)
        {
            WebRequest wrq = WebRequest.Create(StorageUrl + key);
            Stream response = wrq.GetResponse().GetResponseStream();
            if (response == null)
                throw new Exception("Error reading response from web service.");
            var sr = new StreamReader(response);
            return sr.ReadToEnd();
        }

        public string DecryptKVS(string key)
        {
            return SjclCompat.Decrypt(Password, UnquoteString(ReadKVS(HashSomething(Password, key))));
        }

        public static string HashSomething(string password, string something)
        {
            var dig = new Sha256Digest();
            byte[] bpassword = Encoding.UTF8.GetBytes(password);
            dig.BlockUpdate(bpassword, 0, bpassword.Length);
            var key = new byte[dig.GetDigestSize()];
            dig.DoFinal(key, 0);

            var hmac = new HMac(new Sha256Digest());
            hmac.Init(new KeyParameter(key));
            byte[] input = Encoding.UTF8.GetBytes(something);
            hmac.BlockUpdate(input, 0, input.Length);
            var output = new byte[hmac.GetMacSize()];
            hmac.DoFinal(output, 0);

            var sb = new StringBuilder(output.Length*2);
            foreach (byte b in output)
            {
                sb.AppendFormat("{0:x2}", b);
            }
            return sb.ToString();
        }

        public static string UnquoteString(string str)
        {
            return str.Replace("\\", "").Trim(new[] {'\"'});
        }

        public void EncryptKVS(string key, string value)
        {
            WriteKVS(HashSomething(Password, key), SjclCompat.Encrypt(Password, value));
        }

        public void WriteKVS(string key, string value)
        {
            var client = new WebClient();
            client.UploadValues(StorageUrl + key, new NameValueCollection
                {
                    {"value", value}
                });
        }

        public void UpdatePads()
        {
            EncryptKVS("pads", JsonConvert.SerializeObject(Pads.Select(x => x.Name)));
        }
    }
}