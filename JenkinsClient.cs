﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml.Linq;

namespace Inedo.BuildMasterExtensions.Jenkins
{
    internal sealed class JenkinsClient
    {
        JenkinsConfigurer config;

        public JenkinsClient(JenkinsConfigurer config)
        {
            this.config = config;
        }

        private WebClient CreateWebClient()
        {
            var wc = new WebClient();
            if (!string.IsNullOrEmpty(config.Username))
                wc.Credentials = new NetworkCredential(config.Username, config.Password);

            return wc;
        }

        private string Get(string url)
        {
            if (string.IsNullOrEmpty(this.config.ServerUrl)) return null;

            using (var wc = this.CreateWebClient())
            {
                return wc.DownloadString(this.config.BaseUrl + '/' + url.TrimStart('/'));
            }
        }
        private void Post(string url)
        {
            if (string.IsNullOrEmpty(this.config.ServerUrl))
                throw new InvalidOperationException("Jenkins ServerUrl has not been set.");

            using (var wc = this.CreateWebClient())
            {
                wc.UploadString(this.config.BaseUrl + '/' + url.TrimStart('/'), string.Empty);
            }
        }
        private void Download(string url, string toFileName)
        {
            if (string.IsNullOrEmpty(this.config.ServerUrl))
                throw new InvalidOperationException("Jenkins ServerUrl has not been set.");

            using (var wc = this.CreateWebClient())
            {
                wc.DownloadFile(this.config.BaseUrl + '/' + url.TrimStart('/'), toFileName);
            }
        }

        public string[] GetJobNames()
        {
            var xml = this.Get("/view/All/api/xml");
            if (xml == null) return new string[0];
            return XDocument.Parse(xml)
                .Element("allView")
                .Elements("job")
                .Select(x => x.Element("name").Value)
                .ToArray();
        }

        public string GetSpecialBuildNumber(string jobName, string buildNumber)
        {
            return XDocument.Parse(this.Get("job/" + Uri.EscapeUriString(jobName) + "/api/xml"))
                .Descendants(buildNumber)
                .Select(n => n.Element("number").Value)
                .FirstOrDefault();
        }

        public void DownloadArtifact(string jobName, string buildNumber, string fileName)
        {
            this.Download(
                "/job/" + Uri.EscapeUriString(jobName)  + '/' + Uri.EscapeUriString(buildNumber) + "/artifact/*zip*/archive.zip",
                fileName);
        }

        public List<JenkinsBuildArtifact> GetBuildArtifacts(string jobName, string buildNumber)
        {
            return XDocument.Parse(this.Get("job/" + Uri.EscapeUriString(jobName) + "/" + Uri.EscapeUriString(buildNumber) + "/api/xml"))
                .Descendants("artifact")
                .Select(n => new JenkinsBuildArtifact
                {
                    DisplayPath = (string)n.Element("displayPath"),
                    FileName = (string)n.Element("fileName"),
                    RelativePath = (string)n.Element("relativePath")
                })
                .ToList();
        }

        public void DownloadSingleArtifact(string jobName, string buildNumber, string fileName, JenkinsBuildArtifact artifact)
        {
            this.Download(
                "/job/" + Uri.EscapeUriString(jobName)  + '/' + Uri.EscapeUriString(buildNumber) + "/artifact/" + artifact.RelativePath,
                fileName);
        }

        public void TriggerBuild(string jobName, string additionalParameters = null)
        {
            var url = "/job/" + Uri.EscapeUriString(jobName) + "/build";
            if (!string.IsNullOrEmpty(additionalParameters))
                url += "WithParameters?" + additionalParameters;
            this.Post(url);
        }

        public string GetNextBuildNumber(string jobName)
        {
            return XDocument.Parse(this.Get("/job/" + Uri.EscapeUriString(jobName)  + "/api/xml?tree=nextBuildNumber"))
                .Descendants("nextBuildNumber")
                .Select(n => n.Value)
                .FirstOrDefault();
        }

        public JenkinsBuild GetBuildInfo(string jobName, string buildNumber)
        {
            try
            {
                var n = XDocument.Parse(this.Get(
                    "/job/" + Uri.EscapeUriString(jobName)  + '/' + Uri.EscapeUriString(buildNumber)
                    + "/api/xml?tree=building,result,number")
                ).Root;
                return new JenkinsBuild
                {
                    Building = (bool)n.Element("building"),
                    Result = n.Elements("result").Select(e => e.Value).FirstOrDefault(),
                    Number = n.Elements("number").Select(e => e.Value).FirstOrDefault()
                };
            }
            catch (WebException wex)
            {
                var status = wex.Response as HttpWebResponse;
                if (status != null && status.StatusCode == HttpStatusCode.NotFound) return null;

                throw;
            }
        }
    }

    [Serializable]
    internal sealed class JenkinsBuild
    {
        public bool Building { get; set; }
        public string Number { get; set; }
        public string Result { get; set; }
    }

    [Serializable]
    internal sealed class JenkinsBuildArtifact
    {
        public string DisplayPath { get; set; }
        public string FileName { get; set; }
        public string RelativePath { get; set; }
    }
}