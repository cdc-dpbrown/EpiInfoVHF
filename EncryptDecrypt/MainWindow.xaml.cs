using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Epi;
using Epi.Core;

namespace EncryptDecrypt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
        }

        private void btnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            string pt = plainText.Text;
            cipherText.Text = Epi.Configuration.Encrypt(pt);
        }

        private void btnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            string ct = cipherText.Text;
            plainText.Text = Epi.Configuration.Decrypt(ct);
        }

        private bool LoadConfig()
        {
            string configFilePath = Configuration.DefaultConfigurationPath;
            bool configurationOk = true;
            try
            {
                string directoryName = System.IO.Path.GetDirectoryName(configFilePath);
                if (!System.IO.Directory.Exists(directoryName))
                {
                    System.IO.Directory.CreateDirectory(directoryName);
                }

                if (!System.IO.File.Exists(configFilePath))
                {
                    Configuration defaultConfig = Configuration.CreateDefaultConfiguration();
                    Configuration.Save(defaultConfig);
                }

                Configuration.Load(configFilePath);
            }
            catch (Epi.ConfigurationException ex)
            {
            }
            catch (Exception ex)
            {
                configurationOk = ex.Message == "";
            }
            return configurationOk;
        }
    }
}
