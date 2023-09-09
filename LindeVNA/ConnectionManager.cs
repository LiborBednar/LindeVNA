using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.ObjectModel;

namespace LindeVNA
{
    public class ConnectionManager
    {
        public ObservableCollection<Connection> Connections { get; set; }
        public string ActualConnection { get; set; }

        public ConnectionManager()
        {
            Connections = new ObservableCollection<Connection>();
        }

        public void Add(string name, string url)
        {
            if (String.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Název musí být zadán.");
            if (Connections.Any(x => x.Name == name))
                throw new Exception("Připojení se zadaným názvem již existuje.");
            Connection connection = new Connection(name, url);
            Connections.Add(connection);
            ActualConnection = connection.ToString();
        }

        public void Remove(Connection connection)
        {
            Connections.Remove(connection);
            ActualConnection = Connections.Count > 0 ? Connections.First().ToString() : null;
        }

        private static string GetConfigPath()
        {
            return Path.Combine(Srv.LocalApplicationDataPath(), "connections.config");
        }


        public void Save()
        {
            string path = GetConfigPath();
            XmlSerializer serializer = new XmlSerializer(this.GetType());
            using (StreamWriter sw = new StreamWriter(path))
            {
                serializer.Serialize(sw, this);
            }
        }

        public static ConnectionManager Load()
        {
            string path = GetConfigPath();
            ConnectionManager manager = new ConnectionManager();
            XmlSerializer serializer = new XmlSerializer(typeof(ConnectionManager));
            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    manager = (ConnectionManager)serializer.Deserialize(sr);
                }
            }
            return manager;
        }

    }

    public class Connection
    {
        public string Name { get; set; }
        public string Url { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Profil { get; set; }
        public string Language { get; set; }

        public bool RememberLogin { get; set; }
        public bool RememberPassword { get; set; }

        public Connection(string name, string url, string login, string password, string language, bool rememberLogin, bool rememberPassword)
        {
            Name = name;
            Url = url;
            RememberLogin = rememberLogin;
            if (RememberLogin)
                Login = login;
            RememberPassword = rememberPassword;
            if (RememberPassword)
                Password = password;
            Language = language;
        }

        public Connection(string name, string url) : this(name, url, "", "", "", true, false) { }

        public Connection(string name) : this(name, "") { }

        public Connection() { }

        public override string ToString()
        {
            return Name;
        }
    }


}
