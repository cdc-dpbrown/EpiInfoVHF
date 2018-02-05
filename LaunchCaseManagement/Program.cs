namespace LaunchCaseManagement
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Windows.Forms;
    
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            string[] directories = Directory.GetDirectories(Environment.GetEnvironmentVariable("windir") + @"\Microsoft.NET\Framework\");
            bool needsVersionFour = false;
            foreach (string str2 in directories)
            {
                if (str2.Contains("v4.0"))
                {
                    needsVersionFour = true;
                    break;
                }
            }

            string currentFolder = Directory.GetCurrentDirectory();
            string cmPath = Path.Combine(currentFolder, "Case Management.exe");
            string lbPath = Path.Combine(currentFolder, "Lab.exe");

            if (File.Exists(cmPath))
            {
                File.Delete(cmPath);
            }
            if (File.Exists(lbPath))
            {
                File.Delete(lbPath);
            }

            if (needsVersionFour)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo("casemanagementmenu.exe") {
                    WorkingDirectory = @".\Epi Info 7"
                };
                Process.Start(startInfo);
            }
            else
            {
                MessageBox.Show("Epi Info 7 requires Microsoft .NET Framework version 4.0. Microsoft .NET Framework is free and can be downloaded from http://www.microsoft.com/net/", "Prerequisite Missing", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
