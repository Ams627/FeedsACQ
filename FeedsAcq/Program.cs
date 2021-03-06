﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FeedsAcq
{
    class FtpFilespec
    {
        /// <summary>
        /// Abritrary name - not used
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Regular expression to match files on the SERVER:
        /// </summary>
        public string Match { get; set; }

        /// <summary>
        /// Directory on the server into which to change on connection:
        /// </summary>
        public string Dir { get; set; }

        /// <summary>
        /// Destination directory into which downloaded files are placed:
        /// </summary>
        public string DestDir { get; set; }

        /// <summary>
        /// Ignore files older than this number of days:
        /// </summary>
        public int IgnoreOlderDays { get; set; }
    }

    class FtpSpec
    {
        public string Name { get; set; }
        public string Host { get; set; }
        public string Dir { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string Match { get; set; }
        public string DestDir { get; set; }

        public List<FtpFilespec> Filespecs { get; set; }
    }
    internal class Program
    {
        private static void DoFtpTransfer(List<FtpSpec> ftps)
        {
            var ftpclient = new FluentFTP.FtpClient(ftps.First().Host, ftps.First().Username, ftps.First().Password);
            ftpclient.DataConnectionType = FluentFTP.FtpDataConnectionType.PASV;
            ftpclient.Connect();
            foreach (var ftpspec in ftps)
            {
                ftpclient.SetWorkingDirectoryAsync(ftpspec.Dir);
            }
                
        }
        private static void Main(string[] args)
        {   
            try
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
                var companyName = versionInfo.CompanyName;
                var productName = versionInfo.ProductName;
                var appdataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var programFolder = Path.Combine(appdataFolder, companyName, productName);
                if (File.Exists(programFolder))
                {
                    throw new Exception($"File {programFolder} exists but this program requires that path as a folder. It must not be an existing file.");
                }
                // create the program folder if it does not exist. We should never need to do this but we will do
                // it as an emergency procedure:
                Directory.CreateDirectory(programFolder);
                var feedsFolder = Path.Combine(programFolder, "feeds");
                if (File.Exists(feedsFolder))
                {
                    throw new Exception($"File {feedsFolder} exists but this program requires that path as a folder. It must not be an existing file.");
                }
                Directory.CreateDirectory(feedsFolder);

                var acquirersFile = Path.Combine(programFolder, "acquirers.xml");
                var doc = XDocument.Load(acquirersFile);
                var ftplist = doc.Descendants("Feeds").Elements("Ftp").Select(ftp => new FtpSpec
                {
                    Name = (string)ftp.Attribute("Name"),
                    Host = (string)ftp.Attribute("Host"),
                    Username = (string)ftp.Attribute("Username"),
                    Password = (string)ftp.Attribute("Password"),
                    Filespecs = ftp.Elements("Files").Select(filespec => new FtpFilespec
                    {
                        Name = (string)filespec.Attribute("Name"),
                        Match = (string)filespec.Attribute("Match"),
                        Dir = (string)filespec.Attribute("Dir"),
                        DestDir = (string)filespec.Attribute("DestDir"),
                        IgnoreOlderDays = Convert.ToInt32((string)filespec.Attribute("IgnoreOlderDays"))
                    }).ToList()
                }).ToList();

                var hostLookup = ftplist.ToLookup(x => x.Host + x.Username + x.Password);
                var result = hostLookup["ftp.wonk.com"].ToList();

                foreach (var host in hostLookup)
                {
                    DoFtpTransfer(hostLookup[host.Key].ToList());
                }

                Console.WriteLine("");

            }
            catch (Exception ex)
            {
                var codeBase = System.Reflection.Assembly.GetEntryAssembly().CodeBase;
                var progname = Path.GetFileNameWithoutExtension(codeBase);
                Console.Error.WriteLine(progname + ": Error: " + ex.Message);
            }

        }
    }
}
