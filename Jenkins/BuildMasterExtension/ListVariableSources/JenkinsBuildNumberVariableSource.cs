﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using Inedo.BuildMaster.Extensibility.Credentials;
using Inedo.BuildMaster.Extensibility.ListVariableSources;
using Inedo.BuildMaster.Web.Controls;
using Inedo.Documentation;
using Inedo.Extensions.Jenkins.Credentials;
using Inedo.Serialization;

namespace Inedo.Extensions.Jenkins.ListVariableSources
{
    [DisplayName("Jenkins Build Number")]
    [Description("Build numbers from a specified job in a Jenkins instance.")]
    public sealed class JenkinsBuildNumberVariableSource : ListVariableSource, IHasCredentials<JenkinsCredentials>
    {
        [Persistent]
        [DisplayName("Credentials")]
        [TriggerPostBackOnChange]
        [Required]
        public string CredentialName { get; set; }

        [Persistent]
        [DisplayName("Job name")]
        [SuggestibleValue(typeof(JobNameSuggestionProvider))]
        [Required]
        public string JobName { get; set; }

        public override async Task<IEnumerable<string>> EnumerateValuesAsync(ValueEnumerationContext context)
        {
            var credentials = ResourceCredentials.Create<JenkinsCredentials>(this.CredentialName);

            var client = new JenkinsClient(credentials);
            return await client.GetBuildNumbersAsync(this.JobName).ConfigureAwait(false);
        }

        public override RichDescription GetDescription()
        {
            return new RichDescription("Jenkins (", new Hilite(this.CredentialName), ") ", " builds for ", new Hilite(this.JobName), ".");
        }
    }
}