using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace HololensIPDMeasurementTool
{
    public class DevPortalVM : INotifyPropertyChanged
    {
        private bool _useUSB;
        private string _ipAddress;
        private string _userName;
        private string _password;
        private double _ipd;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null)
                return;

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public static DevPortalVM LoadContext(string filename)
        {
            if (!File.Exists(filename))
            {
                return new DevPortalVM();
            }

            DevPortalVM returnValue;

            XmlSerializer serializer = new XmlSerializer(typeof(DevPortalVM));

            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                returnValue = (DevPortalVM)serializer.Deserialize(fs);

            return returnValue;
        }

        public void SaveContext(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(DevPortalVM));

            using (FileStream fs = new FileStream(filename, FileMode.Create))
            {
                serializer.Serialize(fs, this);
            }
        }

        public bool UseUSB
        {
            get
            {
                return _useUSB;
            }
            set
            {
                if (_useUSB != value)
                {
                    _useUSB = value;
                    OnPropertyChanged("UseUSB");
                };
            }
        }

        public string IpAddress {
            get { return _ipAddress; }
            set
            {
                if(_ipAddress != value)
                {
                    _ipAddress = value;
                    OnPropertyChanged("IPAddress");
                }
            }
        }

        public string UserName {
            get { return _userName; }
            set
            {
                if(_userName != value)
                {
                    _userName = value;
                    OnPropertyChanged("UserName");
                }
            }
        }

        public string Password {
            get { return _password; }
            set
            {
                if(_password != value)
                {
                    _password = value;
                    OnPropertyChanged("Password");
                }
            }
        }

        public double IPD
        {
            get { return _ipd; }
            set {
                if(_ipd != value)
                {
                    _ipd = value;
                    OnPropertyChanged("IPD");
                }
            }
        }
    }
}
